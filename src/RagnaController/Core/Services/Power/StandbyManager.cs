using System;
using System.Diagnostics;
using RagnaController.Models;
using RagnaController.Profiles;
using RagnaController.Core;

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

        // PERF-007: Multi-level idle detection
        private long _lastLightIdleTime;
        private const double LightIdleSeconds = 30.0;  // 30s no input → light idle
        private const double DeepIdleSeconds = 120.0;  // 2min no input → deep idle

        public bool IsStandby => _isStandby;
        public int StandbyTimeoutMinutes => _standbyTimeoutMinutes;

        /// <summary>Current idle level for dynamic tick throttling.</summary>
        public IdleLevel CurrentIdleLevel { get; private set; } = IdleLevel.Active;

        public event Action? StandbyEntered;
        public event Action? StandbyExited;
        public event Action<IdleLevel>? IdleLevelChanged;

        public StandbyManager()
        {
            _lastInputTime = Stopwatch.GetTimestamp();
            _lastLightIdleTime = _lastInputTime;
        }

        /// <summary>
        /// Called every tick. Returns true if tick should be skipped (standby active).
        /// Sets shouldSkip=true when in standby to throttle to ~20Hz.
        /// </summary>
        public bool Tick(
            ParsedInput input,
            Profile? currentProfile,
            bool rumbleEnabled,
            IFeedbackProvider feedback,
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

            var previousLevel = CurrentIdleLevel;

            if (hasInput)
            {
                if (_isStandby)
                {
                    ExitStandby(rumbleEnabled, feedback, logger);
                }
                _lastInputTime = now;
                _lastLightIdleTime = now;
                CurrentIdleLevel = IdleLevel.Active;
            }
            else if (currentProfile?.EnableSmartStandby ?? true)
            {
                double idleSeconds = (now - _lastInputTime) / (double)Stopwatch.Frequency;

                int timeoutMins = currentProfile?.StandbyTimeoutMinutes > 0
                    ? currentProfile.StandbyTimeoutMinutes
                    : _standbyTimeoutMinutes;

                // Determine idle level
                if (!_isStandby && idleSeconds >= timeoutMins * 60.0)
                {
                    EnterStandby(rumbleEnabled, feedback, movement, logger);
                    CurrentIdleLevel = IdleLevel.Standby;
                }
                else if (idleSeconds >= DeepIdleSeconds)
                {
                    CurrentIdleLevel = IdleLevel.DeepIdle;
                }
                else if (idleSeconds >= LightIdleSeconds)
                {
                    CurrentIdleLevel = IdleLevel.LightIdle;
                }
                else
                {
                    CurrentIdleLevel = IdleLevel.Active;
                }
            }
            else
            {
                CurrentIdleLevel = IdleLevel.Active;
            }

            // Fire IdleLevelChanged event if level changed
            if (previousLevel != CurrentIdleLevel)
            {
                IdleLevelChanged?.Invoke(CurrentIdleLevel);
                logger?.Info($"[StandbyManager] Idle level changed: {previousLevel} → {CurrentIdleLevel}");
            }

            if (_isStandby)
            {
                shouldSkip = true; // Signal to throttle to ~20Hz
                return true;
            }

            return false;
        }

        private void EnterStandby(bool rumbleEnabled, IFeedbackProvider feedback, MovementEngine movement, AdvancedLogger? logger)
        {
            _isStandby = true;
            movement.ForceStop();
            if (rumbleEnabled) feedback.Trigger(FeedbackType.StandbyOn);
            logger?.Info("Entering Smart Standby to save battery.");
            StandbyEntered?.Invoke();
        }

        private void ExitStandby(bool rumbleEnabled, IFeedbackProvider feedback, AdvancedLogger? logger)
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

    /// <summary>
    /// PERF-007: Idle levels for dynamic tick frequency reduction.
    /// Active = 125Hz, LightIdle = ~62Hz, DeepIdle = ~20Hz, Standby = ~20Hz (skip).
    /// </summary>
    public enum IdleLevel
    {
        Active,      // Normal input — full 125Hz tick rate
        LightIdle,   // 30s+ no input — throttle to ~62Hz
        DeepIdle,    // 2min+ no input — throttle to ~20Hz
        Standby,     // Timeout reached — skip ticks entirely (~20Hz via Thread.Sleep)
    }
}