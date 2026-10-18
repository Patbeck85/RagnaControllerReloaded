using System;

namespace RagnaController.Core
{
    /// <summary>
    /// Adaptiert <see cref="IFeedbackProvider"/> auf <see cref="IRumbleProvider"/>
    /// für Haptic Feedback im Radial Menu (UX-016).
    ///
    /// Mapping:
    ///   * Jeder nicht-Null-Rumble → TriggerSkillFired() (Rumble an)
    ///   * Beide null/0 → StopRumble() (Rumble aus)
    ///
    /// Dies ist eine vereinfachte Brücke: Die echte DualSense-Rumble-Steuerung
    /// erfolgt über den FeedbackProvider; hier wird nur ein/an aus unterschieden.
    /// </summary>
    internal sealed class FeedbackRumbleProvider : IRumbleProvider
    {
        private readonly IFeedbackProvider _feedback;

        public FeedbackRumbleProvider(IFeedbackProvider feedback)
        {
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
        }

        public void SetRumble(float left, float right)
        {
            if (left > 0.05f || right > 0.05f)
                _feedback.TriggerSkillFired();
            else
                _feedback.StopRumble();
        }
    }
}
