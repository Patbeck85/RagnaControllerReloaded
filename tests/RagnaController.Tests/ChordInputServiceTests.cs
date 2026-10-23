using System;
using System.Threading;
using Xunit;
using RagnaController.Core.Services;
using RagnaController.Models;

namespace RagnaController.Tests
{
    public class ChordInputServiceTests
    {
        [Fact]
        public void Chord_MatchingButtons_FiresAction()
        {
            var service = new ChordInputService();
            ButtonAction? fired = null;
            service.ChordFired += (s, a) => fired = a;

            var chord = new ChordConfig
            {
                Name = "TestChord",
                Buttons = GamepadButtonFlags.L1 | GamepadButtonFlags.R1,
                Action = new ButtonAction { Label = "ChordAction", Key = VirtualKey.F1 },
                Enabled = true
            };
            service.AddChord(chord);

            var input = new ParsedInput
            {
                IsConnected = true,
                RawButtons = GamepadButtonFlags.L1 | GamepadButtonFlags.R1,
                PrevRawButtons = GamepadButtonFlags.None
            };

            var result = service.Tick(input, 8, out var action);
            Assert.True(result);
            Assert.NotNull(action);
            Assert.Equal("ChordAction", action?.Label);
        }

        [Fact]
        public void Chord_PartialButtons_DoesNotFire()
        {
            var service = new ChordInputService();
            ButtonAction? fired = null;
            service.ChordFired += (s, a) => fired = a;

            var chord = new ChordConfig
            {
                Name = "TestChord",
                Buttons = GamepadButtonFlags.L1 | GamepadButtonFlags.R1,
                Action = new ButtonAction { Label = "ChordAction" },
                Enabled = true
            };
            service.AddChord(chord);

            // Nur L1 gedrückt, nicht R1
            var input = new ParsedInput
            {
                IsConnected = true,
                RawButtons = GamepadButtonFlags.L1,
                PrevRawButtons = GamepadButtonFlags.None
            };

            var result = service.Tick(input, 8, out var action);
            Assert.False(result);
            Assert.Null(action);
        }

        [Fact]
        public void Chord_Disabled_DoesNotFire()
        {
            var service = new ChordInputService();
            ButtonAction? fired = null;
            service.ChordFired += (s, a) => fired = a;

            var chord = new ChordConfig
            {
                Name = "DisabledChord",
                Buttons = GamepadButtonFlags.L1 | GamepadButtonFlags.R1,
                Action = new ButtonAction { Label = "ChordAction" },
                Enabled = false
            };
            service.AddChord(chord);

            var input = new ParsedInput
            {
                IsConnected = true,
                RawButtons = GamepadButtonFlags.L1 | GamepadButtonFlags.R1,
                PrevRawButtons = GamepadButtonFlags.None
            };

            var result = service.Tick(input, 8, out var action);
            Assert.False(result);
            Assert.Null(action);
        }

        [Fact]
        public void Sequence_OrderedButtons_FiresAction()
        {
            var service = new ChordInputService();
            ButtonAction? fired = null;
            service.SequenceFired += (s, a) => fired = a;

            var seq = new SequenceConfig
            {
                Name = "A→B→X",
                Sequence = new[] { GamepadButtonFlags.BtnA, GamepadButtonFlags.BtnB, GamepadButtonFlags.BtnX },
                Action = new ButtonAction { Label = "SeqAction", Key = VirtualKey.F2 },
                Enabled = true,
                MaxIntervalMs = 500
            };
            service.AddSequence(seq);

            // Frame 1: A gedrückt
            var input1 = new ParsedInput
            {
                IsConnected = true,
                RawButtons = GamepadButtonFlags.BtnA,
                PrevRawButtons = GamepadButtonFlags.None
            };
            Assert.False(service.Tick(input1, 8, out _));

            // Frame 2: B gedrückt (A noch gehalten)
            var input2 = new ParsedInput
            {
                IsConnected = true,
                RawButtons = GamepadButtonFlags.BtnA | GamepadButtonFlags.BtnB,
                PrevRawButtons = GamepadButtonFlags.BtnA
            };
            Assert.False(service.Tick(input2, 8, out _));

            // Frame 3: X gedrückt (A+B noch gehalten)
            var input3 = new ParsedInput
            {
                IsConnected = true,
                RawButtons = GamepadButtonFlags.BtnA | GamepadButtonFlags.BtnB | GamepadButtonFlags.BtnX,
                PrevRawButtons = GamepadButtonFlags.BtnA | GamepadButtonFlags.BtnB
            };
            var result = service.Tick(input3, 8, out var action);
            Assert.True(result);
            Assert.Equal("SeqAction", action?.Label);
        }

        [Fact]
        public void Sequence_WrongOrder_DoesNotFire()
        {
            var service = new ChordInputService();
            ButtonAction? fired = null;
            service.SequenceFired += (s, a) => fired = a;

            var seq = new SequenceConfig
            {
                Name = "A→B",
                Sequence = new[] { GamepadButtonFlags.BtnA, GamepadButtonFlags.BtnB },
                Action = new ButtonAction { Label = "SeqAction" },
                Enabled = true
            };
            service.AddSequence(seq);

            // Frame 1: B gedrückt (falsche Reihenfolge)
            var input1 = new ParsedInput
            {
                IsConnected = true,
                RawButtons = GamepadButtonFlags.BtnB,
                PrevRawButtons = GamepadButtonFlags.None
            };
            Assert.False(service.Tick(input1, 8, out _));

            // Frame 2: A gedrückt (B noch gehalten, aber A kommt nach B)
            var input2 = new ParsedInput
            {
                IsConnected = true,
                RawButtons = GamepadButtonFlags.BtnA | GamepadButtonFlags.BtnB,
                PrevRawButtons = GamepadButtonFlags.BtnB
            };
            var result = service.Tick(input2, 8, out var action);
            Assert.False(result);
            Assert.Null(action);
        }

        [Fact]
        public void HoldTap_ShortPress_FiresTapAction()
        {
            var service = new ChordInputService();
            ButtonAction? fired = null;
            service.HoldTapFired += (s, a) => fired = a;

            var ht = new HoldTapConfig
            {
                Button = GamepadButtonFlags.BtnA,
                TapAction = new ButtonAction { Label = "TapAction" },
                HoldAction = new ButtonAction { Label = "HoldAction" },
                HoldThresholdMs = 300,
                Enabled = true
            };
            service.AddHoldTap(ht);

            // Frame 1: A gedrückt
            var input1 = new ParsedInput
            {
                IsConnected = true,
                RawButtons = GamepadButtonFlags.BtnA,
                PrevRawButtons = GamepadButtonFlags.None
            };
            Assert.False(service.Tick(input1, 8, out _));

            // Frame 2: A losgelassen (nach nur 8ms, unter Hold-Threshold)
            var input2 = new ParsedInput
            {
                IsConnected = true,
                RawButtons = GamepadButtonFlags.None,
                PrevRawButtons = GamepadButtonFlags.BtnA
            };
            var result = service.Tick(input2, 8, out var action);
            Assert.True(result);
            Assert.Equal("TapAction", action?.Label);
        }

        [Fact]
        public void HoldTap_LongPress_FiresHoldAction()
        {
            var service = new ChordInputService();
            ButtonAction? fired = null;
            service.HoldTapFired += (s, a) => fired = a;

            var ht = new HoldTapConfig
            {
                Button = GamepadButtonFlags.BtnA,
                TapAction = new ButtonAction { Label = "TapAction" },
                HoldAction = new ButtonAction { Label = "HoldAction" },
                HoldThresholdMs = 300,
                Enabled = true
            };
            service.AddHoldTap(ht);

            // Frame 1: A gedrückt
            var input1 = new ParsedInput
            {
                IsConnected = true,
                RawButtons = GamepadButtonFlags.BtnA,
                PrevRawButtons = GamepadButtonFlags.None
            };
            Assert.False(service.Tick(input1, 8, out _));

            // Simuliere 350ms Halten (mehrere Frames mit echten Zeitabständen)
            for (int i = 0; i < 45; i++)
            {
                Thread.Sleep(10); // 10ms pro Frame = ~450ms insgesamt
                var inputHold = new ParsedInput
                {
                    IsConnected = true,
                    RawButtons = GamepadButtonFlags.BtnA,
                    PrevRawButtons = GamepadButtonFlags.BtnA
                };
                var result = service.Tick(inputHold, 8, out var action);
                if (result && action?.Label == "HoldAction")
                {
                    Assert.Equal("HoldAction", action.Label);
                    return; // Hold feuerte korrekt
                }
            }

            Assert.Fail("HoldAction sollte nach 350ms feuern");
        }

        [Fact]
        public void HoldTap_Disabled_DoesNotFire()
        {
            var service = new ChordInputService();
            ButtonAction? fired = null;
            service.HoldTapFired += (s, a) => fired = a;

            var ht = new HoldTapConfig
            {
                Button = GamepadButtonFlags.BtnA,
                TapAction = new ButtonAction { Label = "TapAction" },
                HoldAction = new ButtonAction { Label = "HoldAction" },
                Enabled = false
            };
            service.AddHoldTap(ht);

            var input1 = new ParsedInput
            {
                IsConnected = true,
                RawButtons = GamepadButtonFlags.BtnA,
                PrevRawButtons = GamepadButtonFlags.None
            };
            Assert.False(service.Tick(input1, 8, out _));

            var input2 = new ParsedInput
            {
                IsConnected = true,
                RawButtons = GamepadButtonFlags.None,
                PrevRawButtons = GamepadButtonFlags.BtnA
            };
            var result = service.Tick(input2, 8, out var action);
            Assert.False(result);
            Assert.Null(action);
        }

        [Fact]
        public void DisconnectedInput_DoesNotFire()
        {
            var service = new ChordInputService();

            var chord = new ChordConfig
            {
                Buttons = GamepadButtonFlags.L1 | GamepadButtonFlags.R1,
                Action = new ButtonAction { Label = "Test" },
                Enabled = true
            };
            service.AddChord(chord);

            var input = new ParsedInput
            {
                IsConnected = false,
                RawButtons = GamepadButtonFlags.L1 | GamepadButtonFlags.R1,
                PrevRawButtons = GamepadButtonFlags.None
            };

            var result = service.Tick(input, 8, out var action);
            Assert.False(result);
            Assert.Null(action);
        }

        [Fact]
        public void Dispose_ClearsCollections()
        {
            var service = new ChordInputService();
            service.AddChord(new ChordConfig { Buttons = GamepadButtonFlags.L1, Enabled = true });
            service.AddSequence(new SequenceConfig { Sequence = new[] { GamepadButtonFlags.BtnA }, Enabled = true });
            service.AddHoldTap(new HoldTapConfig { Button = GamepadButtonFlags.BtnA, Enabled = true });

            service.Dispose();

            // Nach Dispose sollte nichts feuern
            var input = new ParsedInput
            {
                IsConnected = true,
                RawButtons = GamepadButtonFlags.L1,
                PrevRawButtons = GamepadButtonFlags.None
            };
            var result = service.Tick(input, 8, out var action);
            Assert.False(result);
            Assert.Null(action);
        }
    }
}
