using System;

namespace RagnaController.Core
{
    /// <summary>
    /// ARCH-005: No-op feedback provider for headless/testing scenarios.
    /// Allows running without SDL controller dependency.
    /// </summary>
    public class NullFeedbackProvider : IFeedbackProvider
    {
        /// <summary>
        /// StopAll method.
        /// </summary>
        public void StopAll() { }
        /// <summary>
        /// SetLED method.
        /// <param name="r">r parameter.</param>
        /// <param name="g">g parameter.</param>
        /// <param name="b">b parameter.</param>
        /// </summary>
        public void SetLED(byte r, byte g, byte b) { }
        /// <summary>
        /// Tick method.
        /// </summary>
        public void Tick() { }
        /// <summary>
        /// Trigger method.
        /// <param name="type">type parameter.</param>
        /// </summary>
        public void Trigger(FeedbackType type) { }
        /// <summary>
        /// TriggerSkillFired method.
        /// </summary>
        public void TriggerSkillFired() { }
        /// <summary>
        /// StopRumble method.
        /// </summary>
        public void StopRumble() { }
        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose() { }
    }
}