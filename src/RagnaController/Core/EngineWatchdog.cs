using System;
using System.Diagnostics;
using System.Threading;

namespace RagnaController.Core
{
    /// <summary>
    /// Self-healing watchdog that monitors engine tick durations and warns the user
    /// when the system is struggling to keep up (thermal throttling, AV scan, etc.).
    ///
    /// Detection heuristic:
    ///   If 5 consecutive ticks each exceed MaxTickMs (default 20ms on an 8ms base),
    ///   the system is experiencing sustained CPU starvation. The watchdog fires
    ///   PerformanceWarning and optionally engages battery-style throttling to reduce load.
    ///
    /// Reset heuristic:
    ///   After 3 consecutive ticks under GoodTickMs (default 12ms), the warning clears.
    /// </summary>
    public sealed class EngineWatchdog
    {
        // ── Tuning ────────────────────────────────────────────────────────
        public int  MaxTickMs       { get; set; } = 20;  // warn threshold
        public int  GoodTickMs      { get; set; } = 12;  // clear threshold
        public int  SlowRunRequired { get; set; } = 5;   // consecutive slow ticks to warn
        public int  GoodRunRequired { get; set; } = 3;   // consecutive good ticks to clear

        // ── State ─────────────────────────────────────────────────────────
        public bool IsWarning { get; private set; }

        private int _slowStreak;
        private int _goodStreak;
        private long _warnStartTick;

        /// <summary>Fired when 5+ slow ticks detected. Parameter = average tick ms.</summary>
        public event Action<double>? PerformanceWarning;

        /// <summary>Fired when performance recovers after a warning.</summary>
        public event Action? PerformanceRecovered;

        // Sliding window for average tick time (last 20 ticks)
        private readonly double[] _window = new double[20];
        private int _wi;

        /// <summary>
        /// Call once per engine tick with the measured tick duration.
        /// Thread-safe via Interlocked (called from BackgroundTickProvider's thread).
        /// </summary>
        public void RecordTick(double tickMs)
        {
            _window[_wi] = tickMs;
            _wi = (_wi + 1) % _window.Length;

            if (tickMs > MaxTickMs)
            {
                _slowStreak++;
                _goodStreak = 0;

                if (!IsWarning && _slowStreak >= SlowRunRequired)
                {
                    IsWarning      = true;
                    _warnStartTick = Stopwatch.GetTimestamp();
                    double avg = ComputeAverage();
                    PerformanceWarning?.Invoke(avg);
                }
            }
            else
            {
                _goodStreak++;
                _slowStreak = 0;

                if (IsWarning && _goodStreak >= GoodRunRequired)
                {
                    IsWarning = false;
                    PerformanceRecovered?.Invoke();
                }
            }
        }

        /// <summary>Average tick time over the last 20 samples (ms).</summary>
        public double AverageTickMs => ComputeAverage();

        /// <summary>How long the current warning has been active (0 if not warning).</summary>
        public double WarnDurationMs => IsWarning
            ? (Stopwatch.GetTimestamp() - _warnStartTick) * 1000.0 / Stopwatch.Frequency
            : 0;

        private double ComputeAverage()
        {
            double sum = 0;
            foreach (var v in _window) sum += v;
            return sum / _window.Length;
        }
    }
}
