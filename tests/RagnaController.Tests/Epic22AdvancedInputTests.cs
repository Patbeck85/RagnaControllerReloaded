using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using RagnaController.Core;

namespace RagnaController.Tests
{
    /// <summary>
    /// TASK-3106: Unit-Tests für Epic 22 (Advanced Input Features).
    /// Deckt AdvancedInputService ab.
    /// </summary>
    public class Epic22AdvancedInputTests
    {
        #region Chorded Input Tests

        [Fact]
        public void AdvancedInputService_ButtonDownUp_ShouldTrackState()
        {
            var input = new AdvancedInputService();
            input.ButtonDown(GamepadButton.L1);
            input.ButtonDown(GamepadButton.R1);

            Assert.True(input.IsChordPressed(GamepadButton.L1, GamepadButton.R1));
            Assert.False(input.IsChordPressed(GamepadButton.L1, GamepadButton.BtnA));

            input.ButtonUp(GamepadButton.L1);
            Assert.False(input.IsChordPressed(GamepadButton.L1, GamepadButton.R1));
        }

        [Fact]
        public void AdvancedInputService_IsChordPressedWithinInterval_ShouldWork()
        {
            var input = new AdvancedInputService(chordMaxIntervalMs: 1000);
            input.ButtonDown(GamepadButton.L1);
            input.ButtonDown(GamepadButton.R1);

            Assert.True(input.IsChordPressedWithinInterval(GamepadButton.L1, GamepadButton.R1));
        }

        [Fact]
        public void AdvancedInputService_ChordDisabled_ShouldReturnFalse()
        {
            var input = new AdvancedInputService(chordedInputEnabled: false);
            input.ButtonDown(GamepadButton.L1);
            input.ButtonDown(GamepadButton.R1);

            Assert.False(input.IsChordPressed(GamepadButton.L1, GamepadButton.R1));
        }

        #endregion

        #region Sequential Chords Tests

        [Fact]
        public void AdvancedInputService_Sequence_ShouldDetect()
        {
            var input = new AdvancedInputService(chordMaxIntervalMs: 1000);
            input.RecordSequenceInput(GamepadButton.BtnA);
            input.RecordSequenceInput(GamepadButton.BtnB);
            input.RecordSequenceInput(GamepadButton.BtnX);

            Assert.True(input.IsSequenceDetected(GamepadButton.BtnA, GamepadButton.BtnB, GamepadButton.BtnX));
        }

        [Fact]
        public void AdvancedInputService_Sequence_WrongOrder_ShouldNotDetect()
        {
            var input = new AdvancedInputService(chordMaxIntervalMs: 1000);
            input.RecordSequenceInput(GamepadButton.BtnX);
            input.RecordSequenceInput(GamepadButton.BtnB);
            input.RecordSequenceInput(GamepadButton.BtnA);

            Assert.False(input.IsSequenceDetected(GamepadButton.BtnA, GamepadButton.BtnB, GamepadButton.BtnX));
        }

        #endregion

        #region Hold+Tap Tests

        [Fact]
        public void AdvancedInputService_ButtonTap_ShouldDetect()
        {
            var input = new AdvancedInputService();
            input.ButtonDown(GamepadButton.BtnA);
            Thread.Sleep(50);
            input.ButtonUp(GamepadButton.BtnA);

            Assert.True(input.WasButtonTapped(GamepadButton.BtnA, tapMaxMs: 200));
        }

        [Fact]
        public void AdvancedInputService_ButtonHold_ShouldDetect()
        {
            var input = new AdvancedInputService();
            input.ButtonDown(GamepadButton.BtnA);
            Thread.Sleep(600);

            Assert.True(input.IsButtonHeld(GamepadButton.BtnA, holdMinMs: 500));
        }

        #endregion

        #region Trigger Zone Tests

        [Fact]
        public void AdvancedInputService_TriggerZone_Walk_ShouldDetect()
        {
            var input = new AdvancedInputService();
            input.SetLeftTrigger(0.1f);
            Assert.Equal(TriggerZone.Walk, input.GetLeftTriggerZone());
        }

        [Fact]
        public void AdvancedInputService_TriggerZone_Run_ShouldDetect()
        {
            var input = new AdvancedInputService();
            input.SetLeftTrigger(0.5f);
            Assert.Equal(TriggerZone.Run, input.GetLeftTriggerZone());
        }

        [Fact]
        public void AdvancedInputService_TriggerZone_Sprint_ShouldDetect()
        {
            var input = new AdvancedInputService();
            input.SetLeftTrigger(0.9f);
            Assert.Equal(TriggerZone.Sprint, input.GetLeftTriggerZone());
        }

        #endregion

        #region Stick Gesture Tests

        [Fact]
        public void AdvancedInputService_CircleGesture_ShouldDetect()
        {
            var input = new AdvancedInputService();
            // Simulate circular movement through all quadrants (need 10+ points)
            input.RecordStickPosition(1, 0);
            input.RecordStickPosition(0.7f, 0.7f);
            input.RecordStickPosition(0, 1);
            input.RecordStickPosition(-0.7f, 0.7f);
            input.RecordStickPosition(-1, 0);
            input.RecordStickPosition(-0.7f, -0.7f);
            input.RecordStickPosition(0, -1);
            input.RecordStickPosition(0.7f, -0.7f);
            input.RecordStickPosition(1, 0);
            input.RecordStickPosition(0.7f, 0.7f);

            Assert.True(input.IsCircleGestureDetected());
        }

        [Fact]
        public void AdvancedInputService_ZigZagGesture_ShouldDetect()
        {
            var input = new AdvancedInputService();
            // Simulate zig-zag movement (need 6+ points)
            input.RecordStickPosition(0, 0);
            input.RecordStickPosition(1, 0);
            input.RecordStickPosition(0, 1);
            input.RecordStickPosition(1, 0);
            input.RecordStickPosition(0, 1);
            input.RecordStickPosition(1, 0);

            Assert.True(input.IsZigZagGestureDetected());
        }

        [Fact]
        public void AdvancedInputService_StickTap_ShouldDetect()
        {
            var input = new AdvancedInputService();
            input.RecordStickPosition(0, 0);
            input.RecordStickPosition(1, 0);
            input.RecordStickPosition(0, 0);

            Assert.True(input.IsStickTapDetected());
        }

        #endregion
    }
}
