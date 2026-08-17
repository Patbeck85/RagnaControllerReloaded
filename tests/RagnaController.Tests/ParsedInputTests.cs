using System;
using Xunit;
using RagnaController.Models;

namespace RagnaController.Tests
{
    public class ParsedInputTests
    {
        [Fact]
        public void Disconnected_ReturnsDefaultDisconnectedState()
        {
            var input = ParsedInput.Disconnected;

            Assert.False(input.IsConnected);
            Assert.Equal(0f, input.LeftX);
            Assert.Equal(0f, input.LeftY);
            Assert.Equal(0f, input.RightX);
            Assert.Equal(0f, input.RightY);
            Assert.False(input.L1);
            Assert.False(input.R1);
            Assert.Equal(GamepadButtonFlags.None, input.RawButtons);
            Assert.Equal(GamepadButtonFlags.None, input.PrevRawButtons);
        }

        [Fact]
        public void With_CreatesNewInstanceWithUpdatedValues()
        {
            var original = ParsedInput.Disconnected;

            var updated = original.With(
                leftX: 1.0f,
                leftY: 2.0f,
                isConnected: true,
                l1: true,
                rawButtons: GamepadButtonFlags.BtnA);

            Assert.Equal(1.0f, updated.LeftX);
            Assert.Equal(2.0f, updated.LeftY);
            Assert.True(updated.IsConnected);
            Assert.True(updated.L1);
            Assert.Equal(GamepadButtonFlags.BtnA, updated.RawButtons);

            // Original unchanged (immutability)
            Assert.Equal(0f, original.LeftX);
            Assert.False(original.IsConnected);
            Assert.False(original.L1);
        }

        [Fact]
        public void With_PreservesUnchangedFields()
        {
            var original = new ParsedInput
            {
                LeftX = 10.0f,
                LeftY = 20.0f,
                RightX = 30.0f,
                RightY = 40.0f,
                L1 = true,
                R1 = false,
                IsConnected = true,
                TriggerLeft = 0.5f,
                TriggerRight = 0.8f,
                RawButtons = GamepadButtonFlags.L1 | GamepadButtonFlags.BtnA,
                PrevRawButtons = GamepadButtonFlags.None
            };

            var updated = original.With(l2: true);

            Assert.Equal(10.0f, updated.LeftX);
            Assert.Equal(20.0f, updated.LeftY);
            Assert.Equal(30.0f, updated.RightX);
            Assert.Equal(40.0f, updated.RightY);
            Assert.True(updated.L1);
            Assert.False(updated.R1);
            Assert.True(updated.L2); // Changed
            Assert.True(updated.IsConnected);
            Assert.Equal(0.5f, updated.TriggerLeft);
            Assert.Equal(0.8f, updated.TriggerRight);
            Assert.Equal(GamepadButtonFlags.L1 | GamepadButtonFlags.BtnA, updated.RawButtons);
        }

        [Fact]
        public void JustPressed_DetectsNewButtonPress()
        {
            var input = new ParsedInput
            {
                RawButtons = GamepadButtonFlags.BtnA | GamepadButtonFlags.L1,
                PrevRawButtons = GamepadButtonFlags.L1 // Only L1 was pressed before
            };

            Assert.True(input.JustPressed(GamepadButtonFlags.BtnA)); // BtnA is new
            Assert.False(input.JustPressed(GamepadButtonFlags.L1)); // L1 was already pressed
            Assert.False(input.JustPressed(GamepadButtonFlags.R1)); // R1 never pressed
        }

        [Fact]
        public void JustReleased_DetectsButtonRelease()
        {
            var input = new ParsedInput
            {
                RawButtons = GamepadButtonFlags.L1, // Only L1 currently pressed
                PrevRawButtons = GamepadButtonFlags.L1 | GamepadButtonFlags.BtnA // Both were pressed
            };

            Assert.True(input.JustReleased(GamepadButtonFlags.BtnA)); // BtnA released
            Assert.False(input.JustReleased(GamepadButtonFlags.L1)); // L1 still pressed
            Assert.False(input.JustReleased(GamepadButtonFlags.R1)); // R1 never pressed
        }

        [Fact]
        public void LT_RT_Aliases_Work()
        {
            var input = new ParsedInput
            {
                TriggerLeft = 0.5f,
                TriggerRight = 0.75f
            };

            Assert.Equal(0.5f, input.LT);
            Assert.Equal(0.75f, input.RT);
        }

        [Fact]
        public void ButtonsPressed_DetectsAnyButton()
        {
            var input = new ParsedInput { IsConnected = true };

            Assert.False(input.ButtonsPressed); // No buttons

            input = input with { L1 = true };
            Assert.True(input.ButtonsPressed);

            input = input with { L1 = false, BtnA = true };
            Assert.True(input.ButtonsPressed);
        }

        [Fact]
        public void ValueEquality_Works()
        {
            var a = ParsedInput.Disconnected;
            var b = ParsedInput.Disconnected;
            var c = a with { IsConnected = true };

            Assert.Equal(a, b); // Same values
            Assert.NotEqual(a, c); // Different values
            Assert.True(a == b);
            Assert.False(a == c);
        }

        [Fact]
        public void Immutability_OriginalUnchangedAfterWith()
        {
            var original = ParsedInput.Disconnected;
            var modified = original.With(isConnected: true, leftX: 1.0f);

            Assert.True(modified.IsConnected);
            Assert.Equal(1.0f, modified.LeftX);

            // Original completely unchanged
            Assert.False(original.IsConnected);
            Assert.Equal(0f, original.LeftX);
        }
    }
}
