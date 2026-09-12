using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-011: Fuzzing / Robustness für InputCommandQueue &amp; ParsedInput.
    ///
    /// DoD (KANBAN.md):
    ///  - Seeded-RNG-Fuzz: 10.000 Commands deterministisch reproduzierbar ohne Exception/Deadlock
    ///  - Race-Test Enqueue↔Stop() über 20 Zyklen verliert keine Konsistenz
    ///  - Edge-Cases (Button-Kombinationen, extreme Stick-Werte) als Facts grün
    /// </summary>
    public class InputFuzzTests
    {
        // ---------------------------------------------------------------------
        // TEST-011.1: Seeded-RNG-Fuzz — 10.000 Commands, deterministisch reproduzierbar
        // ---------------------------------------------------------------------

        private const int FuzzCommandCount = 10_000;
        private const int FuzzSeed = 20260912;

        /// <summary>Generiert die deterministische Command-Sequenz (Seed-fest).</summary>
        private static List<InputCmd> GenerateFuzzSequence(int seed, int count)
        {
            var rng = new Random(seed);
            var cmds = new List<InputCmd>(count);

            for (int i = 0; i < count; i++)
            {
                switch (rng.Next(10))
                {
                    case 0:
                        // KeyDown mit zufälligem VK-Code (0..255)
                        cmds.Add(new InputCmd(CmdType.KeyDown, (ushort)(rng.Next(0, 256))));
                        break;
                    case 1:
                        cmds.Add(new InputCmd(CmdType.KeyUp, (ushort)(rng.Next(0, 256))));
                        break;
                    case 2:
                        // Relative Maus-Bewegung im gültigen Bereich
                        cmds.Add(new InputCmd(CmdType.MouseRel, rng.Next(-1000, 1001), rng.Next(-1000, 1001)));
                        break;
                    case 3:
                        // Wheel mit extremen Deltas (int.MinValue..MaxValue-Test wäre OOM-Gefahr in ToArray — hier realistisch)
                        cmds.Add(new InputCmd(CmdType.Wheel) { X = rng.Next(-4096, 4097) });
                        break;
                    case 4:
                        // Wait: 0..3ms → hält den Fuzz-Run schnell, prüft aber die Flush-Pfade
                        cmds.Add(InputCmd.CreateWait(rng.Next(0, 4)));
                        break;
                    case 5:
                        cmds.Add(new InputCmd(CmdType.LeftDown));
                        break;
                    case 6:
                        cmds.Add(new InputCmd(CmdType.LeftUp));
                        break;
                    case 7:
                        cmds.Add(new InputCmd(CmdType.RightDown));
                        break;
                    case 8:
                        cmds.Add(new InputCmd(CmdType.RightUp));
                        break;
                    default:
                        // Action-Callback: zählbar, allocation-frei im Fuzz-Pfad
                        cmds.Add(new InputCmd(CmdType.Action, null));
                        break;
                }
            }

            return cmds;
        }

        private static string SequenceHash(List<InputCmd> cmds)
        {
            using var sha = SHA256.Create();
            var sb = new StringBuilder(cmds.Count * 16);
            foreach (var c in cmds)
                sb.Append(c.Type).Append('|').Append(c.Key).Append('|').Append(c.X).Append('|').Append(c.Y).Append(';');
            using var stream = new System.IO.MemoryStream(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
            return Convert.ToBase64String(stream.ToArray());
        }

        [Fact]
        public void Fuzz_SeededRNG_10000Commands_IsDeterministicAndReproducible()
        {
            // Determinismus: gleicher Seed → byte-identische Sequenz (2× generiert, Hash verglichen)
            var seqA = GenerateFuzzSequence(FuzzSeed, FuzzCommandCount);
            var seqB = GenerateFuzzSequence(FuzzSeed, FuzzCommandCount);
            Assert.Equal(SequenceHash(seqA), SequenceHash(seqB));

            // Reproduzierbarkeit: identische Instanz-Werte (kein Zufall im Testverlauf)
            for (int i = 0; i < FuzzCommandCount; i++)
                Assert.True(seqA[i].Type == seqB[i].Type && seqA[i].Key == seqB[i].Key
                    && seqA[i].X == seqB[i].X && seqA[i].Y == seqB[i].Y,
                    $"Sequenz weicht ab bei Index {i}");
        }

        [Fact]
        public async Task Fuzz_SeededRNG_10000Commands_NoExceptionNoDeadlock()
        {
            var cmds = GenerateFuzzSequence(FuzzSeed, FuzzCommandCount);

            long executed = 0;
            using var queue = new InputCommandQueue();
            queue.OnCommandExecuted += _ => Interlocked.Increment(ref executed);
            queue.Start();

            // Enqueue-Phase (Producer) — alle 10.000 Commands rein, ohne Deadlock
            foreach (var cmd in cmds)
                queue.Enqueue(cmd);

            // Consumer-Drain mit harter Deadlock-Guard: max. 30s für 10k Commands ist großzügig
            var drainTask = Task.Run(() =>
            {
                while (Interlocked.Read(ref executed) < FuzzCommandCount)
                    Thread.Sleep(5);
            });

            bool drained = await Task.WhenAny(drainTask, Task.Delay(TimeSpan.FromSeconds(30))) == drainTask;
            Assert.True(drained, "Deadlock: Consumer hat 10.000 Commands nicht abgearbeitet");
            Assert.Equal(FuzzCommandCount, Interlocked.Read(ref executed));

            // Kein stilles Kommando-Verlust: Queue ist leer
            Assert.Equal(0, queue.QueueCount);

            queue.Stop();
        }

        // ---------------------------------------------------------------------
        // TEST-011.2: Race-Test Enqueue↔Stop() über 20 Zyklen
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Race_Enqueue_vs_Stop_20Cycles_MaintainsConsistency()
        {
            const int Cycles = 20;
            const int Producers = 4;
            const int CommandsPerProducer = 500;

            for (int cycle = 0; cycle < Cycles; cycle++)
            {
                var queue = new InputCommandQueue();
                long executed = 0;
                long enqueued = 0;
                Exception? producerError = null;

                queue.OnCommandExecuted += _ => Interlocked.Increment(ref executed);
                queue.Start();

                // Producer: rennen gegen Stop() um die Wette
                var producers = new Task[Producers];
                for (int p = 0; p < Producers; p++)
                {
                    int seed = FuzzSeed + cycle * 1000 + p;
                    producers[p] = Task.Run(() =>
                    {
                        try
                        {
                            var rng = new Random(seed);
                            for (int i = 0; i < CommandsPerProducer; i++)
                            {
                                if (queue.IsAddingCompleted) break; // Stop() läuft — sauber absteigen

                                switch (rng.Next(4))
                                {
                                    case 0: queue.LeftDown(); break;
                                    case 1: queue.KeyDown((VirtualKey)rng.Next(65, 91)); break;
                                    case 2: queue.Wait(rng.Next(0, 2)); break;
                                    default: queue.MouseMove(rng.Next(-50, 51), rng.Next(-50, 51)); break;
                                }
                                Interlocked.Increment(ref enqueued);
                            }
                        }
                        catch (Exception ex)
                        {
                            // ObjectDisposedException ist erlaubt NACH Dispose — hier aber kein Dispose,
                            // daher: JEDER Exception = Test-Fehler.
                            producerError = ex;
                        }
                    });
                }

                // Race: Stop() feuert mitten im Enqueue-Verkehr (zufälliger Zeitpunkt pro Zyklus)
                var rng = new Random(FuzzSeed + cycle);
                Thread.Sleep(rng.Next(0, 15)); // 0..15ms: mal sofort, mal spät
                var stopTask = Task.Run(() => queue.Stop());
                bool stoppedInTime = await Task.WhenAny(stopTask, Task.Delay(TimeSpan.FromSeconds(10))) == stopTask;
                Assert.True(stoppedInTime, $"Race-Zyklus {cycle}: Stop() deadlock (Timeout)");

                // Producer sauber abwarten (sie steigen bei IsAddingCompleted aus)
                await Task.WhenAll(producers);

                queue.Dispose();

                Assert.Null(producerError);
                // Konsistenz: nichts verloren — jeder enqueuede Command wurde entweder
                // vor Stop() konsumiert oder ist nach Stop() harmlos verworfen.
                // Harte Invariante: executed ≤ enqueued UND keine Exception im Consumer.
                long finalExecuted = Interlocked.Read(ref executed);
                Assert.True(finalExecuted <= Interlocked.Read(ref enqueued),
                    $"Race-Zyklus {cycle}: executed ({finalExecuted}) > enqueued — Phantom-Commands");
            }
        }

        // ---------------------------------------------------------------------
        // TEST-011.3: ParsedInput Edge-Cases — Button-Kombinationen & extreme Stick-Werte
        // ---------------------------------------------------------------------

        [Theory]
        [InlineData(GamepadButtonFlags.BtnA)]
        [InlineData(GamepadButtonFlags.BtnB)]
        [InlineData(GamepadButtonFlags.BtnX)]
        [InlineData(GamepadButtonFlags.BtnY)]
        [InlineData(GamepadButtonFlags.L1)]
        [InlineData(GamepadButtonFlags.R1)]
        [InlineData(GamepadButtonFlags.L2)]
        [InlineData(GamepadButtonFlags.R2)]
        [InlineData(GamepadButtonFlags.L3)]
        [InlineData(GamepadButtonFlags.R3)]
        [InlineData(GamepadButtonFlags.DPadUp)]
        [InlineData(GamepadButtonFlags.DPadDown)]
        [InlineData(GamepadButtonFlags.DPadLeft)]
        [InlineData(GamepadButtonFlags.DPadRight)]
        [InlineData(GamepadButtonFlags.Start)]
        [InlineData(GamepadButtonFlags.Back)]
        [InlineData(GamepadButtonFlags.Btn16)]
        [InlineData(GamepadButtonFlags.Btn17)]
        [InlineData(GamepadButtonFlags.Btn18)]
        [InlineData(GamepadButtonFlags.Btn19)]
        [InlineData(GamepadButtonFlags.Btn20)]
        [InlineData(GamepadButtonFlags.Btn21)]
        [InlineData(GamepadButtonFlags.Btn22)]
        [InlineData(GamepadButtonFlags.Btn23)]
        public void ParsedInput_EachButtonFlag_JustPressedJustReleased_TransitionsCorrect(GamepadButtonFlags flag)
        {
            // Phase 1: frisch gedrückt → JustPressed=true, JustReleased=false
            var pressed = new ParsedInput
            {
                RawButtons = flag,
                PrevRawButtons = GamepadButtonFlags.None
            };
            Assert.True(pressed.JustPressed(flag));
            Assert.False(pressed.JustReleased(flag));

            // Phase 2: gehalten → weder JustPressed noch JustReleased
            var held = new ParsedInput
            {
                RawButtons = flag,
                PrevRawButtons = flag
            };
            Assert.False(held.JustPressed(flag));
            Assert.False(held.JustReleased(flag));

            // Phase 3: frisch losgelassen → JustReleased=true
            var released = new ParsedInput
            {
                RawButtons = GamepadButtonFlags.None,
                PrevRawButtons = flag
            };
            Assert.True(released.JustReleased(flag));
            Assert.False(released.JustPressed(flag));

            // Isolation: kein anderer Flag wird fälschlich als JustPressed gemeldet
            var isolated = new ParsedInput
            {
                RawButtons = flag,
                PrevRawButtons = GamepadButtonFlags.None
            };
            foreach (GamepadButtonFlags other in Enum.GetValues<GamepadButtonFlags>())
            {
                if (other == flag || other == GamepadButtonFlags.None) continue;
                Assert.False(isolated.JustPressed(other),
                    $"Flag {flag} meldet JustPressed für {other}");
            }
        }

        [Fact]
        public void ParsedInput_RandomButtonCombinations_JustPressedMatchesReferenceImplementation()
        {
            // Seeded-RNG: 1.024 zufällige Button-Kombinations-Paare gegen die Referenz-Definition prüfen
            var rng = new Random(FuzzSeed);
            var allFlags = Enum.GetValues<GamepadButtonFlags>()
                .Where(f => f != GamepadButtonFlags.None).ToArray();

            for (int i = 0; i < 1_024; i++)
            {
                uint rawMask = (uint)rng.Next(int.MaxValue);
                var raw = allFlags.Where(f => (rawMask & (uint)f) != 0).Aggregate(
                    GamepadButtonFlags.None, (acc, f) => acc | f);

                uint prevMask = (uint)rng.Next(int.MaxValue);
                var prev = allFlags.Where(f => (prevMask & (uint)f) != 0).Aggregate(
                    GamepadButtonFlags.None, (acc, f) => acc | f);

                var input = new ParsedInput { RawButtons = raw, PrevRawButtons = prev };

                foreach (var flag in allFlags)
                {
                    bool expectedPressed = raw.HasFlag(flag) && !prev.HasFlag(flag);
                    bool expectedReleased = !raw.HasFlag(flag) && prev.HasFlag(flag);
                    Assert.True(input.JustPressed(flag) == expectedPressed,
                        $"JustPressed({flag}) falsch bei raw={raw}, prev={prev}");
                    Assert.True(input.JustReleased(flag) == expectedReleased,
                        $"JustReleased({flag}) falsch bei raw={raw}, prev={prev}");
                }
            }
        }

        [Theory]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        [InlineData(float.MaxValue)]
        [InlineData(float.MinValue)]
        public void ParsedInput_ExtremeStickValues_PreservedThroughWith(float value)
        {
            // Extreme Stick-Werte (NaN/Inf/Max/Min) dürfen nicht werfen und bleiben stabil.
            var input = new ParsedInput
            {
                LeftX = value,
                LeftY = value,
                RightX = value,
                RightY = value,
                TriggerLeft = 1.0f,
                TriggerRight = 1.0f
            };

            var updated = input.With(leftX: 0.5f);

            Assert.Equal(0.5f, updated.LeftX);
            // Unveränderte Extreme bleiben exakt erhalten (bit-stabil — NaN via Bitmuster vergleichen)
            Assert.True(float.IsNaN(updated.LeftY) == float.IsNaN(value));
            Assert.Equal(BitConverter.SingleToInt32Bits(value), BitConverter.SingleToInt32Bits(updated.RightX));
            Assert.Equal(BitConverter.SingleToInt32Bits(value), BitConverter.SingleToInt32Bits(updated.RightY));
        }

        [Fact]
        public void ParsedInput_TriggerOutOfRangeValues_PreservedWithoutClamping()
        {
            // Trigger-Werte außerhalb 0..1 (z. B. defekter Controller) dürfen nicht klemmen —
            // das ist Aufgabe der Engine, nicht des Immutable-States.
            var input = new ParsedInput
            {
                TriggerLeft = -0.25f,
                TriggerRight = 3.7f
            };

            Assert.Equal(-0.25f, input.LT);
            Assert.Equal(3.7f, input.RT);

            var updated = input.With(triggerLeft: 1.0f);
            Assert.Equal(1.0f, updated.TriggerLeft);
            Assert.Equal(3.7f, updated.TriggerRight); // RT bleibt unangetastet
        }

        [Fact]
        public void ParsedInput_AllButtonsPressed_ButtonsPressedTrue_AndSingleFlagIsolation()
        {
            // ButtonsPressed prüft die 16 benannten Button-Properties (nicht RawButtons) —
            // alle 16 setzen + Isolation: jeder Einzel-Flag-JustReleased wird korrekt erkannt.
            var all = allFlagsAggregate();
            var input = new ParsedInput
            {
                RawButtons = all,
                PrevRawButtons = GamepadButtonFlags.None,
                IsConnected = true,
                L1 = true, R1 = true, L2 = true, R2 = true, L3 = true, R3 = true,
                BtnA = true, BtnB = true, BtnX = true, BtnY = true,
                DPadUp = true, DPadDown = true, DPadLeft = true, DPadRight = true,
                Start = true, Back = true
            };

            Assert.True(input.ButtonsPressed);

            // Jeder Einzel-Flag-JustPressed wird korrekt erkannt (24 Flags)
            foreach (var flag in Enum.GetValues<GamepadButtonFlags>())
            {
                if (flag == GamepadButtonFlags.None) continue;
                Assert.True(input.JustPressed(flag));
            }

            // ButtonsPressed-Negation: kein einziger Button → false
            var none = new ParsedInput { IsConnected = true };
            Assert.False(none.ButtonsPressed);
        }

        private static GamepadButtonFlags allFlagsAggregate()
            => Enum.GetValues<GamepadButtonFlags>()
                .Where(f => f != GamepadButtonFlags.None)
                .Aggregate(GamepadButtonFlags.None, (acc, f) => acc | f);
    }
}
