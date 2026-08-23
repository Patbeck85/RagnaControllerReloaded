namespace RagnaController.Controller
{
    /// <summary>
    /// Unified button state structure for all controller backends (SDL2, XInput).
    /// This provides a consistent API regardless of the underlying controller provider.
    /// Using readonly record struct for zero-allocation, value-type semantics with built-in equality.
    /// </summary>
    public readonly record struct ButtonState(
        bool APressed = false,
        bool BPressed = false,
        bool XPressed = false,
        bool YPressed = false,
        bool L1Pressed = false,
        bool R1Pressed = false,
        bool L2Pressed = false,
        bool R2Pressed = false,
        bool StartPressed = false,
        bool BackPressed = false,
        bool DPadUp = false,
        bool DPadDown = false,
        bool DPadLeft = false,
        bool DPadRight = false,
        bool L3Pressed = false,
        bool R3Pressed = false
    )
    {
        /// <summary>
        /// Returns true if any face button (A/B/X/Y) is pressed.
        /// </summary>
        public readonly bool AnyFaceButtonPressed => APressed || BPressed || XPressed || YPressed;

        /// <summary>
        /// Returns true if any shoulder/trigger button is pressed.
        /// </summary>
        public readonly bool AnyShoulderPressed => L1Pressed || R1Pressed || L2Pressed || R2Pressed;

        /// <summary>
        /// Returns true if any D-Pad direction is pressed.
        /// </summary>
        public readonly bool AnyDPadPressed => DPadUp || DPadDown || DPadLeft || DPadRight;

        /// <summary>
        /// Returns true if any stick button (L3/R3) is pressed.
        /// </summary>
        public readonly bool AnyStickButtonPressed => L3Pressed || R3Pressed;

        /// <summary>
        /// Returns true if any button is pressed.
        /// </summary>
        public readonly bool AnyButtonPressed => 
            AnyFaceButtonPressed || AnyShoulderPressed || AnyDPadPressed || AnyStickButtonPressed || StartPressed || BackPressed;

        /// <summary>
        /// Creates a new ButtonState with the specified button toggled.
        /// </summary>
        public ButtonState With(string buttonName, bool value) => buttonName switch
        {
            "A"        => this with { APressed = value },
            "B"        => this with { BPressed = value },
            "X"        => this with { XPressed = value },
            "Y"        => this with { YPressed = value },
            "L1"       => this with { L1Pressed = value },
            "R1"       => this with { R1Pressed = value },
            "L2"       => this with { L2Pressed = value },
            "R2"       => this with { R2Pressed = value },
            "Start"    => this with { StartPressed = value },
            "Back"     => this with { BackPressed = value },
            "DPadUp"   => this with { DPadUp = value },
            "DPadDown" => this with { DPadDown = value },
            "DPadLeft" => this with { DPadLeft = value },
            "DPadRight"=> this with { DPadRight = value },
            "L3"       => this with { L3Pressed = value },
            "R3"       => this with { R3Pressed = value },
            _ => this
        };
    }
}