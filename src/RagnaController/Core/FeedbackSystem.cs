using System;
using RagnaController.Controller;

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

    /// <summary>
    /// FeedbackType enum - moved from FeedbackSystem for reuse across implementations.
    /// </summary>
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

    /// <summary>
    /// ARCH-005: SDL-backed feedback implementation.
    /// Implements IFeedbackProvider for testability and headless scenarios.
    /// </summary>
    public class FeedbackSystem : IFeedbackProvider
    {
        private readonly ControllerService _controller;
        private long _rumbleStopTime; // Environment.TickCount64 wann Rumble stoppen soll
        private long _nextRumbleStepTime;
        private int _rumbleStepSequence;

        public FeedbackSystem(ControllerService controller) => _controller = controller;

        // ── Aus dem HybridEngine-Tick aufgerufen (125Hz) ─────────────────
        public void StopAll()
        {
            _controller?.SetRumble(0f, 0f);
            _controller?.SetLED(0, 0, 0);
            _rumbleStopTime     = 0;
            _nextRumbleStepTime = 0;
            _rumbleStepSequence = 0;
        }

        public void SetLED(byte r, byte g, byte b)
        {
            _controller?.SetLED(r, g, b);
        }

        public void Tick()
        {
            long now = Environment.TickCount64;
            
            // Handle Heartbeat Sequence (BuffWarning)
            if (_rumbleStepSequence > 0 && now >= _nextRumbleStepTime)
            {
                if (_rumbleStepSequence == 2) // Pause between beats
                {
                    _controller.SetRumble(0f, 0f);
                    _nextRumbleStepTime = now + 100;
                    _rumbleStepSequence = 1;
                }
                else if (_rumbleStepSequence == 1) // Second beat
                {
                    _controller.SetRumble(0.4f, 0.4f);
                    _rumbleStopTime = now + 150;
                    _rumbleStepSequence = 0; // Sequence done
                }
            }

            if (_rumbleStopTime > 0 && now >= _rumbleStopTime)
            {
                _controller.SetRumble(0, 0);
                _rumbleStopTime = 0;
            }
        }

        public void Trigger(FeedbackType type)
        {
            // FIX: Null check for controller before accessing its methods
            if (_controller == null) return;
            
            switch (type)
            {
                case FeedbackType.TargetLocked:    _controller.SetRumble(0.4f, 0.2f); ScheduleStop(100); break;
                case FeedbackType.PhaseChange:     _controller.SetRumble(0.2f, 0.1f); ScheduleStop(100); break;
                case FeedbackType.CombatModeOn:    _controller.SetRumble(0.3f, 0.0f); ScheduleStop(80);  break;
                case FeedbackType.CombatModeOff:   _controller.SetRumble(0.1f, 0.0f); ScheduleStop(60);  break;
                case FeedbackType.PrecisionModeOn: _controller.SetRumble(0.0f, 0.2f); ScheduleStop(50);  break;
                case FeedbackType.Warning:         _controller.SetRumble(0.5f, 0.5f); ScheduleStop(150); break;
                case FeedbackType.BuffWarning:
                    _controller.SetRumble(0.6f, 0.6f); // First beat
                    _nextRumbleStepTime = Environment.TickCount64 + 150; // beat length
                    _rumbleStepSequence = 2; // trigger pause next
                    break;
                case FeedbackType.TurboPulse:
                    // Very sharp, 15ms pulse on right (light/fast) motor
                    _controller.SetRumble(0.0f, 0.5f);
                    ScheduleStop(15);
                    break;
                case FeedbackType.StandbyOn:
                    // Gentle pulse to indicate standby mode entered
                    _controller.SetRumble(0.2f, 0.2f);
                    ScheduleStop(200);
                    // Set LED to dim blue for standby
                    _controller.SetLED(0, 0, 80);
                    break;
                case FeedbackType.StandbyOff:
                    // Sharp pulse to indicate wake up
                    _controller.SetRumble(0.4f, 0.1f);
                    ScheduleStop(100);
                    // LED will be set by normal combat state logic
                    break;
            }
        }

        public void TriggerSkillFired()
        {
            if (_controller == null) return;
            _controller.SetRumble(0f, 0.3f);
            ScheduleStop(60);
        }

        public void StopRumble()
        {
            if (_controller == null) return;
            _rumbleStopTime = 0;
            _controller.SetRumble(0, 0);
        }

        public void Dispose()
        {
            StopAll();
        }

        private void ScheduleStop(int ms)
            => _rumbleStopTime = Environment.TickCount64 + ms;
    }
}