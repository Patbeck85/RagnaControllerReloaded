using System;
using System.Collections.Generic;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// Tracks button hold times for long-press detection.
    /// Optimized: Zero allocations in hot path (Update called every tick).
    /// </summary>
    public sealed class LongPressTracker
    {
        private readonly Dictionary<GamepadButtonFlags, long> _pressStartTicks = new();
        private readonly Dictionary<GamepadButtonFlags, long> _lastRepeatTicks = new();
        private long _thresholdTicks;
        private long _repeatIntervalTicks;
        
        // Cached values to avoid allocations
        private static readonly GamepadButtonFlags[] _allFlags = Enum.GetValues<GamepadButtonFlags>();
        private readonly List<(GamepadButtonFlags Button, bool IsFirstLongPress, bool IsRepeat)> _reusableResults = new();

        public LongPressTracker(int thresholdMs = 500, int repeatIntervalMs = 0)
        {
            _thresholdTicks = TimeSpan.TicksPerMillisecond * thresholdMs;
            _repeatIntervalTicks = repeatIntervalMs > 0 ? TimeSpan.TicksPerMillisecond * repeatIntervalMs : 0;
        }

        /// <summary>
        /// Update the threshold and repeat interval at runtime.
        /// </summary>
        public void UpdateConfig(int thresholdMs, int repeatIntervalMs)
        {
            _thresholdTicks = TimeSpan.TicksPerMillisecond * thresholdMs;
            _repeatIntervalTicks = repeatIntervalMs > 0 ? TimeSpan.TicksPerMillisecond * repeatIntervalMs : 0;
        }

        /// <summary>
        /// Process button state changes and return long-press events.
        /// </summary>
        /// <param name="currentButtons">Currently pressed buttons.</param>
        /// <param name="prevButtons">Previously pressed buttons.</param>
        /// <returns>List of (button, isLongPress, isRepeat) tuples for buttons that triggered long-press.</returns>
        public List<(GamepadButtonFlags Button, bool IsFirstLongPress, bool IsRepeat)> Update(GamepadButtonFlags currentButtons, GamepadButtonFlags prevButtons)
        {
            _reusableResults.Clear();
            long now = DateTime.UtcNow.Ticks;

            // Check for newly pressed buttons
            var pressedNow = currentButtons & ~prevButtons;
            foreach (GamepadButtonFlags flag in _allFlags)
            {
                if (pressedNow.HasFlag(flag) && flag != GamepadButtonFlags.None)
                {
                    _pressStartTicks[flag] = now;
                    _lastRepeatTicks.Remove(flag);
                }
            }

            // Check for released buttons
            var releasedNow = prevButtons & ~currentButtons;
            foreach (GamepadButtonFlags flag in _allFlags)
            {
                if (releasedNow.HasFlag(flag) && flag != GamepadButtonFlags.None)
                {
                    _pressStartTicks.Remove(flag);
                    _lastRepeatTicks.Remove(flag);
                }
            }

            // Check currently held buttons for long-press
            foreach (GamepadButtonFlags flag in _allFlags)
            {
                if (flag == GamepadButtonFlags.None) continue;
                
                if (currentButtons.HasFlag(flag) && _pressStartTicks.TryGetValue(flag, out long startTicks))
                {
                    long heldTicks = now - startTicks;
                    
                    // First long-press detection
                    if (heldTicks >= _thresholdTicks)
                    {
                        // Check if this is the first long-press or a repeat
                        bool isFirstLongPress = !_lastRepeatTicks.ContainsKey(flag);
                        
                        if (isFirstLongPress)
                        {
                            _lastRepeatTicks[flag] = now;
                            _reusableResults.Add((flag, true, false));
                        }
                        else if (_repeatIntervalTicks > 0)
                        {
                            long sinceLastRepeat = now - _lastRepeatTicks[flag];
                            if (sinceLastRepeat >= _repeatIntervalTicks)
                            {
                                _lastRepeatTicks[flag] = now;
                                _reusableResults.Add((flag, false, true));
                            }
                        }
                    }
                }
            }

            return _reusableResults;
        }

        /// <summary>
        /// Get the current hold time for a specific button.
        /// </summary>
        public TimeSpan GetHoldTime(GamepadButtonFlags flag)
        {
            if (_pressStartTicks.TryGetValue(flag, out long startTicks))
            {
                return new TimeSpan(DateTime.UtcNow.Ticks - startTicks);
            }
            return TimeSpan.Zero;
        }

        /// <summary>
        /// Check if a button is currently in long-press state.
        /// </summary>
        public bool IsLongPress(GamepadButtonFlags flag)
        {
            if (_pressStartTicks.TryGetValue(flag, out long startTicks))
            {
                return (DateTime.UtcNow.Ticks - startTicks) >= _thresholdTicks;
            }
            return false;
        }

        /// <summary>
        /// Clear all tracking state.
        /// </summary>
        public void Clear()
        {
            _pressStartTicks.Clear();
            _lastRepeatTicks.Clear();
        }
    }
}