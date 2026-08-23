namespace RagnaController.Controller
{
    /// <summary>
    /// Unified button state structure for all controller backends (SDL2, XInput).
    /// This provides a consistent API regardless of the underlying controller provider.
    /// </summary>
    public struct ButtonState
    {
        public bool APressed { get; set; }
        public bool BPressed { get; set; }
        public bool XPressed { get; set; }
        public bool YPressed { get; set; }
        public bool L1Pressed { get; set; }
        public bool R1Pressed { get; set; }
        public bool L2Pressed { get; set; }
        public bool R2Pressed { get; set; }
        public bool StartPressed { get; set; }
        public bool BackPressed { get; set; }
        public bool DPadUp { get; set; }
        public bool DPadDown { get; set; }
        public bool DPadLeft { get; set; }
        public bool DPadRight { get; set; }
        public bool L3Pressed { get; set; }
        public bool R3Pressed { get; set; }
    }
}