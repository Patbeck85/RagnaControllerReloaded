using System;
using System.Diagnostics;
using RagnaController.Models;
using RagnaController.Core;

namespace RagnaController.Core
{
    /// <summary>
    /// TECH-064: Extracted from InputRouter — Input-Emulation Failover State Machine (ROB-002).
    ///
    /// Manages the switch between SendInput (primary) and Kernel/Interception (fallback)
    /// mouse emulation strategies based on observed input latency.
    ///
    /// N consecutive slow flushes → switch to Kernel fallback.
    /// M stable flushes after switch → recover back to SendInput.
    /// </summary>
    /// <summary>
    /// Input processing and routing component.
    /// </summary>
    public class InputFailoverManager
    {
        private IMouseEmulationStrategy? _primary;
        private IMouseEmulationStrategy? _fallback;
        private int _thresholdMs;
        private int _triggerCount;
        private int _recoveryCount;
        private int _consecutiveSlow;
        private int _consecutiveStable;
        private bool _usingFallback;
        private readonly object _lock = new();

        /// <summary>Currently active emulation strategy.</summary>
        public IMouseEmulationStrategy? ActiveStrategy => _usingFallback ? _fallback : _primary;

        /// <summary>Fires on every failover switch (from, to, trigger count).</summary>
        public event Action<string, string, int>? FailoverSwitched;

        public void Initialize(IMouseEmulationStrategy primary, IMouseEmulationStrategy? fallback, Models.Settings settings)
        {
            lock (_lock)
            {
                _primary = primary;
                _fallback = fallback;
                _thresholdMs = Math.Max(1, settings.FailoverLatencyThresholdMs);
                _triggerCount = Math.Max(1, settings.FailoverTriggerCount);
                _recoveryCount = Math.Max(1, settings.FailoverRecoveryCount);
            }
        }

        /// <summary>
        /// Called by InputCommandQueue.SendInputLatencyRecorded on the consumer thread.
        /// Thread-safe by design (no UI access).
        /// </summary>
        public void RecordLatency(double latencyMs)
        {
            IMouseEmulationStrategy? primary;
            lock (_lock)
            {
                primary = _primary;
                if (primary == null || _fallback == null) return;

                bool slow = latencyMs > _thresholdMs;

                if (!_usingFallback)
                {
                    // Normal operation: observe SendInput latency
                    if (slow)
                    {
                        _consecutiveSlow++;
                        _consecutiveStable = 0;
                        if (_consecutiveSlow >= _triggerCount)
                        {
                            _usingFallback = true;
                            _consecutiveSlow = 0;
                            SwitchTo(primary.DisplayName, _fallback.DisplayName, _triggerCount);
                        }
                    }
                    else
                    {
                        _consecutiveSlow = 0;
                    }
                }
                else
                {
                    // Fallback active: wait for stable SendInput latency
                    if (!slow)
                    {
                        _consecutiveStable++;
                        _consecutiveSlow = 0;
                        if (_consecutiveStable >= _recoveryCount)
                        {
                            _usingFallback = false;
                            _consecutiveStable = 0;
                            SwitchTo(_fallback.DisplayName, primary.DisplayName, _recoveryCount);
                        }
                    }
                    else
                    {
                        _consecutiveStable = 0;
                    }
                }
            }
        }

        private void SwitchTo(string from, string to, int triggerCount)
        {
            RagnaControllerETW.Log.InputFailoverSwitched(from, to, triggerCount);
            FailoverSwitched?.Invoke(from, to, triggerCount);
        }
    }
}
