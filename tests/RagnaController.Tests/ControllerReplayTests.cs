using System.IO;
using System.Text.Json;
using Xunit;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-016: Controller-Replay als CI-Golden-Master (hardware-frei).
    ///
    /// Spielt eine aufgezeichnete Sitzung (<c>fixtures/controller_session_001.json</c>) Frame-für-Frame
    /// durch die ECHTE Pipeline — <see cref="FakeControllerProvider"/> → geteilte
    /// <see cref="PipelineStep"/> (identische State-Maschine wie <c>Core/InputReader.Read()</c>) — und
    /// vergleicht jedes Ergebnis bit-exakt mit dem unabhängigen Golden-Master
    /// (<c>fixtures/controller_session_001.golden.json</c>).
    ///
    /// Das ist der CI-Anker: eine Regression in Mapping, Transition-Maschine oder
    /// Disconnect/Reconnect-Reset bricht den Test, auch ohne Controller/Hardware.
    /// </summary>
    public class ControllerReplayTests
    {
        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        // Digitale Buttons, die in RawButtons stehen und damit JustPressed/JustReleased feuern.
        // L2/R2 sind analog (Trigger > 0.15) und NICHT in der Mask → werden separat geprüft.
        private static readonly (GamepadButtonFlags Flag, string Name)[] DigitalButtons =
        {
            (GamepadButtonFlags.BtnA, "BtnA"),
            (GamepadButtonFlags.BtnB, "BtnB"),
            (GamepadButtonFlags.BtnX, "BtnX"),
            (GamepadButtonFlags.BtnY, "BtnY"),
            (GamepadButtonFlags.L1, "L1"),
            (GamepadButtonFlags.R1, "R1"),
            (GamepadButtonFlags.L3, "L3"),
            (GamepadButtonFlags.R3, "R3"),
            (GamepadButtonFlags.DPadUp, "DPadUp"),
            (GamepadButtonFlags.DPadDown, "DPadDown"),
            (GamepadButtonFlags.DPadLeft, "DPadLeft"),
            (GamepadButtonFlags.DPadRight, "DPadRight"),
            (GamepadButtonFlags.Start, "Start"),
            (GamepadButtonFlags.Back, "Back"),
        };

        private sealed record SessionFixture(string Id, string Description, SessionFrame[] Frames);
        private sealed record SessionFrame(int Buttons, float Lx, float Ly, float Rx, float Ry, float Lt, float Rt, bool Connected);
        private sealed record GoldenFixture(string Id, string Description, GoldenFrame[] Frames);
        private sealed record GoldenFrame(
            int Index, bool Connected, string[] JustPressed, string[] JustReleased,
            bool L2, bool R2, float LeftX, float LeftY, float RightX, float RightY,
            float TriggerLeft, float TriggerRight);

        [Fact]
        public void Replay_Session001_MatchesGoldenMaster_FrameByFrame()
        {
            var (session, golden) = LoadFixtures();

            // Guard: Fixture und Golden müssen dieselbe Länge haben.
            Assert.Equal(golden.Frames.Length, session.Frames.Length);

            // 1) Session in den hardware-freien Provider überspielen.
            var provider = new FakeControllerProvider();
            foreach (var f in session.Frames)
            {
                if (!f.Connected)
                    provider.ScriptDisconnect();
                else
                    provider.ScriptFrame((GamepadButtonFlags)(uint)f.Buttons, f.Lx, f.Ly, f.Rx, f.Ry, f.Lt, f.Rt);
            }

            var step = new PipelineStep();

            // 2) Frame-für-Frame durch die echte Pipeline und gegen das Golden prüfen.
            for (int i = 0; i < golden.Frames.Length; i++)
            {
                var g = golden.Frames[i];
                var actual = step.Step(provider);

                AssertEq(g.Connected, actual.IsConnected, $"Frame {i}: connected erwartet {g.Connected}, war {actual.IsConnected}");

                if (!g.Connected) continue; // Disconnect-Frame: keine Button-Ergebnisse

                var pressed = DigitalButtons.Where(d => actual.JustPressed(d.Flag)).Select(d => d.Name).OrderBy(n => n).ToList();
                var released = DigitalButtons.Where(d => actual.JustReleased(d.Flag)).Select(d => d.Name).OrderBy(n => n).ToList();

                AssertSeq(g.JustPressed, pressed, $"Frame {i}: JustPressed erwartet [{string.Join(",", g.JustPressed)}], war [{string.Join(",", pressed)}]");
                AssertSeq(g.JustReleased, released, $"Frame {i}: JustReleased erwartet [{string.Join(",", g.JustReleased)}], war [{string.Join(",", released)}]");

                // Analoge Trigger-Flags (bit-exakt, > 0.15 → true).
                AssertEq(g.L2, actual.L2, $"Frame {i}: L2 erwartet {g.L2}, war {actual.L2}");
                AssertEq(g.R2, actual.R2, $"Frame {i}: R2 erwartet {g.R2}, war {actual.R2}");

                // Analogwerte: bit-exakter Durchlauf der zugeflossenen Rohwerte.
                AssertF(g.LeftX, actual.LeftX, $"Frame {i}: LeftX erwartet {g.LeftX}, war {actual.LeftX}");
                AssertF(g.LeftY, actual.LeftY, $"Frame {i}: LeftY erwartet {g.LeftY}, war {actual.LeftY}");
                AssertF(g.RightX, actual.RightX, $"Frame {i}: RightX erwartet {g.RightX}, war {actual.RightX}");
                AssertF(g.RightY, actual.RightY, $"Frame {i}: RightY erwartet {g.RightY}, war {actual.RightY}");
                AssertF(g.TriggerLeft, actual.TriggerLeft, $"Frame {i}: TriggerLeft erwartet {g.TriggerLeft}, war {actual.TriggerLeft}");
                AssertF(g.TriggerRight, actual.TriggerRight, $"Frame {i}: TriggerRight erwartet {g.TriggerRight}, war {actual.TriggerRight}");
            }
        }

        [Fact]
        public void Replay_Reconnect_FiresNoGhostRelease_AfterDisconnect()
        {
            // Generischer Invarianten-Check über das Golden: nach Disconnect/Reconnect darf
            // KEIN JustReleased für Buttons feuern, die vor dem Disconnect gehalten waren
            // (Prev-State wird beim Disconnect zurückgesetzt → sonst Ghost-Release).
            // Der Test findet das Szenario datengetrieben in der Fixture statt es zu hardcodieren:
            //   1) ersten Disconnect-Lauf finden (Lücke aus connected=false Frames),
            //   2) Buttons, die direkt DAVOR gehalten waren (letzte Maske != 0),
            //   3) Reconnect-Frame = erster connected=true Frame nach dem Lauf.
            var (session, golden) = LoadFixtures();

            // (1) + (2): ersten Disconnect-Lauf + vorher gehaltene Buttons ermitteln.
            int discStart = -1;
            for (int i = 0; i < session.Frames.Length; i++)
            {
                if (!session.Frames[i].Connected) { discStart = i; break; }
            }
            Assert.True(discStart > 0, "Fixture muss einen Disconnect enthalten (TEST-016 DoD).");
            uint heldBefore = (uint)session.Frames[discStart - 1].Buttons;
            Assert.NotEqual(0u, heldBefore); // Sonst wäre der Ghost-Test trivial (nichts gehalten).

            // Ende des Laufes / Reconnect-Frame bestimmen.
            int reconnectIdx = discStart;
            while (reconnectIdx < session.Frames.Length && !session.Frames[reconnectIdx].Connected)
                reconnectIdx++;
            Assert.True(reconnectIdx < session.Frames.Length, "Fixture muss nach dem Disconnect wieder verbinden.");

            // Replay durch die echte Pipeline fahren und den Reconnect-Frame abgreifen.
            var provider = new FakeControllerProvider();
            foreach (var f in session.Frames)
                if (!f.Connected) provider.ScriptDisconnect();
                else provider.ScriptFrame((GamepadButtonFlags)(uint)f.Buttons, f.Lx, f.Ly, f.Rx, f.Ry, f.Lt, f.Rt);

            var step = new PipelineStep();
            ParsedInput? rc = null;
            for (int i = 0; i < session.Frames.Length; i++)
                if (i == reconnectIdx) rc = step.Step(provider); else step.Step(provider);

            Assert.NotNull(rc);
            var gReconnect = golden.Frames[reconnectIdx];

            // Kern-Invariante: KEIN Ghost-Release auf dem Reconnect-Frame.
            Assert.Empty(gReconnect.JustReleased);
            foreach (var (flag, name) in DigitalButtons)
                Assert.False(rc!.Value.JustReleased(flag), $"Ghost-Release {name} auf Reconnect-Frame {reconnectIdx}.");

            // Und: die gehaltenen Buttons feuern stattdessen JustPressed (Hardware hält weiter).
            foreach (var (flag, name) in DigitalButtons)
                if ((heldBefore & (uint)flag) != 0)
                    Assert.True(rc!.Value.JustPressed(flag), $"Erwartet JustPressed {name} auf Reconnect-Frame {reconnectIdx}.");
        }

        private static (SessionFixture, GoldenFixture) LoadFixtures()
        {
            string FixturePath(string name) => Path.Combine(AppContext.BaseDirectory, "fixtures", name);

            var session = JsonSerializer.Deserialize<SessionFixture>(File.ReadAllText(FixturePath("controller_session_001.json")), Json)
                ?? throw new InvalidOperationException("Session-Fixture nicht gefunden/leer.");
            var golden = JsonSerializer.Deserialize<GoldenFixture>(File.ReadAllText(FixturePath("controller_session_001.golden.json")), Json)
                ?? throw new InvalidOperationException("Golden-Fixture nicht gefunden/leer.");
            return (session, golden);
        }

        // Eindeutige Helper — vermeiden xunit-Overload-Ambiguität und geben saubere Frame-Nachrichten.
        private static void AssertEq(bool exp, bool act, string msg) => Assert.True(exp == act, msg);
        private static void AssertF(float exp, float act, string msg) => Assert.True(exp == act, msg);
        private static void AssertSeq(string[] exp, List<string> act, string msg)
            => Assert.True(exp.OrderBy(n => n).SequenceEqual(act), msg);
    }
}
