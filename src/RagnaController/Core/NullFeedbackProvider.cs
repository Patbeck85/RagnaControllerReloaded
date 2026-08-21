using System;

namespace RagnaController.Core
{
    /// <summary>
    /// ARCH-005: No-op feedback provider for headless/testing scenarios.
    /// Allows running without SDL controller dependency.
    /// </summary>
    public class NullFeedbackProvider : IFeedbackProvider
    {
        public void StopAll() { }
        public void SetLED(byte r, byte g, byte b) { }
        public void Tick() { }
        public void Trigger(FeedbackType type) { }
        public void TriggerSkillFired() { }
        public void StopRumble() { }
        public void Dispose() { }
    }
}