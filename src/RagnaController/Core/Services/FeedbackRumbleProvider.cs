using System;

namespace RagnaController.Core
{
    /// <summary>
    /// Adaptiert <see cref="IFeedbackProvider"/> on <see cref="IRumbleProvider"/>
    /// for Haptic Feedback im Radial Menu (UX-016).
    ///
    /// Mapping:
    ///   * Every nicht-Null-Rumble → TriggerSkillFired() (Rumble an)
    ///   * Beide null/0 → StopRumble() (Rumble aus)
    ///
    /// Dies is eine vereinfachte Brücke: The echte DualSense-Rumble-Steuerung
    /// erfolgt about den FeedbackProvider; hier is only ein/an aus unterschieden.
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
