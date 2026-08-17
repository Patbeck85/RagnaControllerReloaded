using System;
using System.Diagnostics;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Core
{
    /// <summary>
    /// ARCH-001: Extracted from HybridEngine - Smart Standby AFK detection & power management.
    /// Responsible for: Idle detection, standby enter/exit, polling throttling, battery saving.
    /// </summary>
    public class StandbyManager
    {
        private long _lastInputTime;
        private bool _isStandby;
        private int _standbyTimeoutMinutes = 5;

        public bool IsStandby => _isStandby;
        public int StandbyTimeoutMinutes => _standbyTimeoutMinutes;

        public event Action? StandbyEntered;
        public event Action? StandbyExited;

        public StandbyManager()
        {
            _lastInputTime = Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// Called every tick. Returns true if tick should be skipped (standby active).
        /// Sets shouldSkip=true when in standby to throttle to ~20Hz.
        /// </summary>
        public bool Tick(
            ParsedInput input,
            Profile? currentProfile,
            bool rumbleEnabled,
            FeedbackSystem feedback,
            MovementEngine movement,
            AdvancedLogger? logger,
            out bool shouldSkip)
        {
            shouldSkip = false;

            long now = Stopwatch.GetTimestamp();
            bool hasInput = (input.RawButtons != GamepadButtonFlags.None) ||
                            Math.Abs(input.LeftX) > 0.15f || Math.Abs(input.LeftY) > 0.15f ||
                            Math.Abs(input.RightX) > 0.15f || Math.Abs(input.RightY) > 0.15f ||
                            input.L2 || input.R2;

            if (hasInput)
            {
                if (_isStandby)
                {
                    ExitStandby(rumbleEnabled, feedback, logger);
                }
                _lastInputTime = now;
            }
            else if (currentProfile?.EnableSmartStandby ?? true)
            {
                double idleMinutes = (now - _lastInputTime) / (double)Stopwatch.Frequency / 60.0;

                int timeoutMins = currentProfile?.StandbyTimeoutMinutes > 0 
                    ? currentProfile.StandbyTimeoutMinutes 
                    : _standbyTimeoutMinutes;

                if (!_isStandby && idleMinutes >= timeoutMins)
                {
                    EnterStandby(rumbleEnabled, feedback, movement, logger);
                }
            }

            if (_isStandby)
            {
                shouldSkip = true; // Signal to throttle to ~20Hz
                return true;
            }

            return false;
        }

        private void EnterStandby(bool rumbleEnabled, FeedbackSystem feedback, MovementEngine movement, AdvancedLogger? logger)
        {
            _isStandby = true;
            movement.ForceStop();
            if (rumbleEnabled) feedback.Trigger(FeedbackType.StandbyOn);
            logger?.Info("Entering Smart Standby to save battery.");
            StandbyEntered?.Invoke();
        }

        private void ExitStandby(bool rumbleEnabled, FeedbackSystem feedback, AdvancedLogger? logger)
        {
            _isStandby = false;
            if (rumbleEnabled) feedback.Trigger(FeedbackType.StandbyOff);
            logger?.Info("Waking up from Smart Standby.");
            StandbyExited?.Invoke();
        }

        public void SetStandbySettings(bool enabled, int timeoutMins)
        {
            _standbyTimeoutMinutes = enabled ? timeoutMins : 0;
            if (!enabled && _isStandby)
            {
                _isStandby = false;
                _lastInputTime = Stopwatch.GetTimestamp();
            }
        }

        public void ResetTimer()
        {
            _lastInputTime = Stopwatch.GetTimestamp();
            if (_isStandby)
            {
                _isStandby = false;
            }
        }
    }
}