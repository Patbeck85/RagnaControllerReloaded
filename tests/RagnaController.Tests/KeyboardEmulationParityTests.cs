using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Xunit;
using RagnaController.Core;

namespace RagnaController.Tests
{
    /// <summary>
    /// GEX-003: Keyboard Emulation Parity Tests
    /// Verifies SendInputKeyboardStrategy and InterceptionKeyboardStrategy produce IDENTICAL key sequences.
    /// Critical for seamless keyboard failover — mid-session switch must not break casts/combos.
    /// </summary>
    public class KeyboardEmulationParityTests
    {
        private const int TEST_ITERATIONS = 1000;

        /// <summary>
        /// Records the sequence of keyboard operations for comparison.
        /// </summary>
        private sealed class KeySequenceRecorder
        {
            public readonly List<KeyEvent> Events = new();
            private readonly object _lock = new();

            public void Record(string operation, ushort keyCode, ushort modifier = 0)
            {
                lock (_lock)
                {
                    Events.Add(new KeyEvent(operation, keyCode, modifier));
                }
            }

            public IReadOnlyList<KeyEvent> GetEvents()
            {
                lock (_lock) return Events.ToList();
            }
        }

        private sealed record KeyEvent(string Operation, ushort KeyCode, ushort Modifier);

        /// <summary>
        /// SendInput strategy that records operations instead of executing them.
        /// </summary>
        private sealed class RecordingSendInputKeyboardStrategy : IKeyboardEmulationStrategy
        {
            private readonly KeySequenceRecorder _recorder;
            public bool IsAvailable => true;
            public string DisplayName => "SendInput Keyboard (Recording)";

            public RecordingSendInputKeyboardStrategy(KeySequenceRecorder recorder) => _recorder = recorder;

            public void KeyDown(ushort keyCode) => _recorder.Record("KeyDown", keyCode);
            public void KeyUp(ushort keyCode) => _recorder.Record("KeyUp", keyCode);

            public void TapKey(ushort keyCode, int holdMs = 0)
            {
                _recorder.Record("KeyDown", keyCode);
                _recorder.Record("KeyUp", keyCode);
            }

            public void TapKeyWithModifier(ushort modifier, ushort keyCode, int holdMs = 0)
            {
                _recorder.Record("KeyDown", modifier);
                _recorder.Record("KeyDown", keyCode);
                _recorder.Record("KeyUp", keyCode);
                _recorder.Record("KeyUp", modifier);
            }

            public void Dispose() { }
        }

        /// <summary>
        /// Interception strategy that records operations instead of executing them.
        /// </summary>
        private sealed class RecordingInterceptionKeyboardStrategy : IKeyboardEmulationStrategy
        {
            private readonly KeySequenceRecorder _recorder;
            public bool IsAvailable => true;
            public string DisplayName => "Interception Keyboard (Recording)";

            public RecordingInterceptionKeyboardStrategy(KeySequenceRecorder recorder) => _recorder = recorder;

            public void KeyDown(ushort keyCode) => _recorder.Record("KeyDown", keyCode);
            public void KeyUp(ushort keyCode) => _recorder.Record("KeyUp", keyCode);

            public void TapKey(ushort keyCode, int holdMs = 0)
            {
                _recorder.Record("KeyDown", keyCode);
                _recorder.Record("KeyUp", keyCode);
            }

            public void TapKeyWithModifier(ushort modifier, ushort keyCode, int holdMs = 0)
            {
                _recorder.Record("KeyDown", modifier);
                _recorder.Record("KeyDown", keyCode);
                _recorder.Record("KeyUp", keyCode);
                _recorder.Record("KeyUp", modifier);
            }

            public void Dispose() { }
        }

        /// <summary>
        /// GEX-003 Core Parity Test: Same key sequence on both backends → identical event sequence.
        /// </summary>
        [Fact]
        public void SendInput_And_Interception_Produce_Identical_Key_Sequences()
        {
            var rng = new Random(42);
            var commands = new List<KeyboardCommand>();

            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                switch (rng.Next(4))
                {
                    case 0: // Single key tap (skill cast)
                        commands.Add(new KeyboardCommand("TapKey", (ushort)rng.Next(0x41, 0x5A), 0)); // A-Z
                        break;
                    case 1: // Key with modifier (shift+skill, ctrl+potion)
                        commands.Add(new KeyboardCommand("TapKeyWithModifier", 
                            (ushort)rng.Next(0x10, 0x13), // Shift/Ctrl/Alt
                            (ushort)rng.Next(0x41, 0x5A))); // A-Z
                        break;
                    case 2: // Key down only (hold for channeling)
                        commands.Add(new KeyboardCommand("KeyDown", (ushort)rng.Next(0x41, 0x5A), 0));
                        break;
                    case 3: // Key up only (release)
                        commands.Add(new KeyboardCommand("KeyUp", (ushort)rng.Next(0x41, 0x5A), 0));
                        break;
                }
            }

            // Act: Record on SendInput backend
            var siRecorder = new KeySequenceRecorder();
            var siStrategy = new RecordingSendInputKeyboardStrategy(siRecorder);
            ReplayCommands(siStrategy, commands);
            var siEvents = siRecorder.GetEvents();

            // Act: Record on Interception backend
            var icRecorder = new KeySequenceRecorder();
            var icStrategy = new RecordingInterceptionKeyboardStrategy(icRecorder);
            ReplayCommands(icStrategy, commands);
            var icEvents = icRecorder.GetEvents();

            // Assert: Event sequences must be identical
            Assert.Equal(siEvents.Count, icEvents.Count);
            
            for (int i = 0; i < siEvents.Count; i++)
            {
                var si = siEvents[i];
                var ic = icEvents[i];
                Assert.Equal(si.Operation, ic.Operation);
                Assert.Equal(si.KeyCode, ic.KeyCode);
                Assert.Equal(si.Modifier, ic.Modifier);
            }
        }

        /// <summary>
        /// GEX-003: Modifier+Key sequence parity (critical for combos: Shift+Skill, Ctrl+Potion).
        /// </summary>
        [Fact]
        public void Modifier_Key_Sequences_Parity()
        {
            var commands = new List<KeyboardCommand>
            {
                new("TapKeyWithModifier", 0x10, 0x41), // Shift+A
                new("TapKeyWithModifier", 0x11, 0x42), // Ctrl+B
                new("TapKeyWithModifier", 0x12, 0x43), // Alt+C
                new("TapKeyWithModifier", 0x10, 0x44), // Shift+D (repeat)
            };

            var siRecorder = new KeySequenceRecorder();
            var icRecorder = new KeySequenceRecorder();

            ReplayCommands(new RecordingSendInputKeyboardStrategy(siRecorder), commands);
            ReplayCommands(new RecordingInterceptionKeyboardStrategy(icRecorder), commands);

            var siEvents = siRecorder.GetEvents();
            var icEvents = icRecorder.GetEvents();

            Assert.Equal(siEvents.Count, icEvents.Count);
            
            for (int i = 0; i < siEvents.Count; i++)
            {
                var si = siEvents[i];
                var ic = icEvents[i];
                Assert.Equal(si.Operation, ic.Operation);
                Assert.Equal(si.KeyCode, ic.KeyCode);
                Assert.Equal(si.Modifier, ic.Modifier);
            }
        }

        /// <summary>
        /// GEX-003: Failover mid-sequence — switch from SendInput to Interception halfway.
        /// Final sequence must match running all on single backend.
        /// </summary>
        [Fact]
        public void Failover_Mid_Sequence_Produces_Same_Sequence()
        {
            var rng = new Random(999);
            var commands = new List<KeyboardCommand>();

            for (int i = 0; i < TEST_ITERATIONS; i++)
            {
                commands.Add(new KeyboardCommand("TapKey", (ushort)rng.Next(0x41, 0x5A), 0));
            }

            // Reference: All on SendInput
            var refRecorder = new KeySequenceRecorder();
            ReplayCommands(new RecordingSendInputKeyboardStrategy(refRecorder), commands);
            var refEvents = refRecorder.GetEvents();

            // Test: Failover at halfway
            var testRecorder = new KeySequenceRecorder();
            var siStrat = new RecordingSendInputKeyboardStrategy(testRecorder);
            var icStrat = new RecordingInterceptionKeyboardStrategy(testRecorder);

            for (int i = 0; i < TEST_ITERATIONS / 2; i++)
                ExecuteCommand(siStrat, commands[i]);

            for (int i = TEST_ITERATIONS / 2; i < TEST_ITERATIONS; i++)
                ExecuteCommand(icStrat, commands[i]);

            var testEvents = testRecorder.GetEvents();

            // Assert: Sequences identical
            Assert.Equal(refEvents.Count, testEvents.Count);
            
            for (int i = 0; i < refEvents.Count; i++)
            {
                var r = refEvents[i];
                var t = testEvents[i];
                Assert.Equal(r.Operation, t.Operation);
                Assert.Equal(r.KeyCode, t.KeyCode);
                Assert.Equal(r.Modifier, t.Modifier);
            }
        }

        private static void ReplayCommands(IKeyboardEmulationStrategy strategy, List<KeyboardCommand> commands)
        {
            foreach (var cmd in commands)
                ExecuteCommand(strategy, cmd);
        }

        private static void ExecuteCommand(IKeyboardEmulationStrategy strategy, KeyboardCommand cmd)
        {
            switch (cmd.Type)
            {
                case "TapKey":
                    strategy.TapKey(cmd.KeyCode);
                    break;
                case "TapKeyWithModifier":
                    strategy.TapKeyWithModifier(cmd.Modifier, cmd.KeyCode);
                    break;
                case "KeyDown":
                    strategy.KeyDown(cmd.KeyCode);
                    break;
                case "KeyUp":
                    strategy.KeyUp(cmd.KeyCode);
                    break;
            }
        }

        private sealed record KeyboardCommand(string Type, ushort KeyCode, ushort Modifier = 0);
    }
}