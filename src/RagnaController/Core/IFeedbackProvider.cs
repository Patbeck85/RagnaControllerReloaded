using System;

namespace RagnaController.Core
{
    /// <summary>
    /// ARCH-005: Abstract feedback provider interface.
    /// Allows headless/testing implementations without SDL audio dependency.
    /// </summary>
    public interface IFeedbackProvider : IDisposable
    {
        void StopAll();
        void SetLED(byte r, byte g, byte b);
        void Tick();
        void Trigger(FeedbackType type);
        void TriggerSkillFired();
        void StopRumble();
    }

    public enum FeedbackType 
    { 
        CombatModeOn, 
        CombatModeOff, 
        TargetLocked, 
        PhaseChange, 
        Warning, 
        PrecisionModeOn, 
        BuffWarning, 
        TurboPulse, 
        StandbyOn, 
        StandbyOff 
    }
}