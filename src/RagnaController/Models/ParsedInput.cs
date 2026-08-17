using System;

namespace RagnaController.Models
{
    /// <summary>
    /// SDL2-compatible gamepad button flags (replaces SharpDX.XInput.GamepadButtonFlags)
    /// </summary>
    [Flags]
    public enum GamepadButtonFlags : uint
    {
        None = 0,

        // Face buttons
        BtnA = 1 << 0,   // A / X
        BtnB = 1 << 1,   // B / Circle
        BtnX = 1 << 2,   // X / Triangle
        BtnY = 1 << 3,   // Y / Square

        // Shoulder buttons
        L1 = 1 << 4,     // L1 / L1
        R1 = 1 << 5,     // R1 / R1
        L2 = 1 << 6,     // L2 / L2 (analog trigger)
        R2 = 1 << 7,     // R2 / R2 (analog trigger)

        // Stick buttons
        L3 = 1 << 8,     // Left stick press
        R3 = 1 << 9,     // Right stick press

        // D-pad
        DPadUp = 1 << 10,
        DPadDown = 1 << 11,
        DPadLeft = 1 << 12,
        DPadRight = 1 << 13,

        // System buttons
        Start = 1 << 14,
        Back = 1 << 15,

        // Extra buttons (for advanced controllers)
        Btn16 = 1 << 16,
        Btn17 = 1 << 17,
        Btn18 = 1 << 18,
        Btn19 = 1 << 19,
        Btn20 = 1 << 20,
        Btn21 = 1 << 21,
        Btn22 = 1 << 22,
        Btn23 = 1 << 23,
        Btn24 = 1 << 24,
    }

    /// <summary>
    /// Immutable parsed input state for the current frame.
    /// readonly record struct provides value semantics, immutability, and zero heap allocation.
    /// </summary>
    public readonly record struct ParsedInput
    {
        // Analog sticks
        public float LeftX { get; init; }
        public float LeftY { get; init; }
        public float RightX { get; init; }
        public float RightY { get; init; }

        // Digital buttons
        public bool L1 { get; init; }
        public bool R1 { get; init; }
        public bool L2 { get; init; }
        public bool R2 { get; init; }
        public bool L3 { get; init; }
        public bool R3 { get; init; }
        public bool R3Prev { get; init; }
        public bool BtnA { get; init; }
        public bool BtnB { get; init; }
        public bool BtnX { get; init; }
        public bool BtnY { get; init; }
        public bool DPadUp { get; init; }
        public bool DPadDown { get; init; }
        public bool DPadLeft { get; init; }
        public bool DPadRight { get; init; }
        public bool Start { get; init; }
        public bool Back { get; init; }

        // Triggers (0.0 to 1.0)
        public float TriggerLeft { get; init; }
        public float TriggerRight { get; init; }

        // Aliases for compatibility with existing code
        public float LT => TriggerLeft;
        public float RT => TriggerRight;

        // Raw button flags for reflection-based processing (SDL2-compatible)
        public GamepadButtonFlags RawButtons { get; init; }

        // Previous frame raw buttons for JustPressed/JustReleased detection
        public GamepadButtonFlags PrevRawButtons { get; init; }

        // Connection state
        public bool IsConnected { get; init; }

        // Helper methods
        public bool JustPressed(GamepadButtonFlags flag)
            => RawButtons.HasFlag(flag) && !PrevRawButtons.HasFlag(flag);

        public bool JustReleased(GamepadButtonFlags flag)
            => !RawButtons.HasFlag(flag) && PrevRawButtons.HasFlag(flag);

        // Button states for testing
        public bool ButtonsPressed =>
            L1 || R1 || L2 || R2 || L3 || R3 || BtnA || BtnB || BtnX || BtnY ||
            DPadUp || DPadDown || DPadLeft || DPadRight || Start || Back;

        /// <summary>Create a default (disconnected) input state.</summary>
        public static ParsedInput Disconnected => new() { IsConnected = false };

        /// <summary>Create a new input state with updated values (with-expressions).</summary>
        public ParsedInput With(
            float? leftX = null, float? leftY = null, float? rightX = null, float? rightY = null,
            bool? l1 = null, bool? r1 = null, bool? l2 = null, bool? r2 = null,
            bool? l3 = null, bool? r3 = null, bool? r3Prev = null,
            bool? btnA = null, bool? btnB = null, bool? btnX = null, bool? btnY = null,
            bool? dPadUp = null, bool? dPadDown = null, bool? dPadLeft = null, bool? dPadRight = null,
            bool? start = null, bool? back = null,
            float? triggerLeft = null, float? triggerRight = null,
            GamepadButtonFlags? rawButtons = null, GamepadButtonFlags? prevRawButtons = null,
            bool? isConnected = null)
        {
            return this with
            {
                LeftX = leftX ?? LeftX,
                LeftY = leftY ?? LeftY,
                RightX = rightX ?? RightX,
                RightY = rightY ?? RightY,
                L1 = l1 ?? L1,
                R1 = r1 ?? R1,
                L2 = l2 ?? L2,
                R2 = r2 ?? R2,
                L3 = l3 ?? L3,
                R3 = r3 ?? R3,
                R3Prev = r3Prev ?? R3Prev,
                BtnA = btnA ?? BtnA,
                BtnB = btnB ?? BtnB,
                BtnX = btnX ?? BtnX,
                BtnY = btnY ?? BtnY,
                DPadUp = dPadUp ?? DPadUp,
                DPadDown = dPadDown ?? DPadDown,
                DPadLeft = dPadLeft ?? DPadLeft,
                DPadRight = dPadRight ?? DPadRight,
                Start = start ?? Start,
                Back = back ?? Back,
                TriggerLeft = triggerLeft ?? TriggerLeft,
                TriggerRight = triggerRight ?? TriggerRight,
                RawButtons = rawButtons ?? RawButtons,
                PrevRawButtons = prevRawButtons ?? PrevRawButtons,
                IsConnected = isConnected ?? IsConnected
            };
        }
    }
}