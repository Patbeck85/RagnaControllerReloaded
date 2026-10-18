using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RagnaController.Core
{
    /// <summary>
    /// Self-healing watchdog that monitors engine tick durations AND hang state.
    ///
    /// Performance heuristic (legacy):
    ///   If 5 consecutive ticks each exceed MaxTickMs (default 20ms on an 8ms base),
    ///   the system is experiencing sustained CPU starvation. The watchdog fires
    ///   PerformanceWarning and optionally engages battery-style throttling to reduce load.
    ///
    /// Reset heuristic:
    ///   After 3 consecutive ticks under GoodTickMs (default 12ms), the warning clears.
    ///
    /// Hang heuristic (ROB-001):
    ///   An external poll loop (Task.Delay, default 100ms) checks the last tick timestamp.
    ///   If no tick arrives within HangThresholdMs (default 500ms), the engine thread is
    ///   assumed hung (deadlock / infinite block) and HangDetected fires exactly once
    ///   until a tick resumes. The consumer (EngineOrchestrator) restarts the engine.
    /// </summary>
    public sealed class EngineWatchdog : IDisposable
    {
        // ── Tuning ────────────────────────────────────────────────────────
        public int  MaxTickMs       { get; set; } = 20;  // warn threshold
        public int  GoodTickMs      { get; set; } = 12;  // clear threshold
        public int  SlowRunRequired { get; set; } = 5;   // consecutive slow ticks to warn
        public int  GoodRunRequired { get; set; } = 3;   // consecutive good ticks to clear

        // ── Hang detection tuning (ROB-001) ───────────────────────────────
        /// <summary>Time without a tick before the engine is considered hung (ms).</summary>
        public int HangThresholdMs { get; set; } = 500;
        /// <summary>External poll interval for hang checks (ms).</summary>
        public int HangPollIntervalMs { get; set; } = 100;

        // ── State ─────────────────────────────────────────────────────────
        public bool IsWarning { get; private set; }
        /// <summary>True while a hang condition is active (no tick within threshold).</summary>
        public bool IsHung { get => _isHung; private set => _isHung = value; }

        private int _slowStreak;
        private int _goodStreak;
        private long _warnStartTick;

        /// <summary>Fired when 5+ slow ticks detected. Parameter = average tick ms.</summary>
        public event Action<double>? PerformanceWarning;

        /// <summary>Fired when performance recovers after a warning.</summary>
        public event Action? PerformanceRecovered;

        /// <summary>
        /// Fired exactly once when no tick arrives within HangThresholdMs.
        /// Parameter = elapsed time without tick (ms). Cleared automatically when ticks resume.
        /// </summary>
        public event Action<double>? HangDetected;

        // Sliding window for average tick time (last 20 ticks)
        private readonly double[] _window = new double[20];
        private int _wi;

        // ── Hang detection state ───────────────────────────────────────────
        private long _lastTickTicks;          // Stopwatch timestamp of last MarkTick()
        private volatile bool _isHung;
        private CancellationTokenSource? _hangCts;
        private Task? _hangTask;
        private readonly object _hangLock = new();

        /// <summary>
        /// Injizierbare Uhr (Stopwatch-Timestamp) für deterministische Tests. Default: Stopwatch.GetTimestamp().
        /// ROB-001 CI-Härtung: Tests können die Zeit vorwärts treiben, statt Wall-Clock-Sleeps zu riskieren.
        /// </summary>
        public Func<long>? TimeSource { get; set; }

        private long NowTicks() => TimeSource?.Invoke() ?? Stopwatch.GetTimestamp();

        /// <summary>
        /// Call once per engine tick with the measured tick duration.
        /// Thread-safe via Interlocked (called from BackgroundTickProvider's thread).
        /// </summary>
        public void RecordTick(double tickMs)
        {
            MarkTick();

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

        /// <summary>
        /// Lightweight per-tick heartbeat for hang detection. Called at the TOP of every
        /// engine tick (before any early-return paths), so a live tick loop never looks hung.
        /// </summary>
        public void MarkTick()
        {
            long now = NowTicks();
            Interlocked.Exchange(ref _lastTickTicks, now);

            if (_isHung)
            {
                // Ticks resumed → hang cleared (no event; consumer sees normal operation).
                lock (_hangLock)
                {
                    _isHung = false;
                }
            }
        }

        /// <summary>
        /// Starts the external hang-detection poll loop. Idempotent.
        /// </summary>
        public void Start()
        {
            if (_hangTask != null) return;

            var cts = new CancellationTokenSource();
            _hangCts = cts;
            _hangTask = Task.Run(() => PollLoopAsync(cts.Token));
        }

        /// <summary>
        /// Stops the hang-detection poll loop deterministically (IDisposable contract).
        /// </summary>
        public void Stop()
        {
            var cts = _hangCts;
            var task = _hangTask;
            if (cts == null) return;

            cts.Cancel();

            if (task != null)
            {
                try
                {
                    task.Wait(500); // bounded: Task.Delay is cancelled, loop exits promptly
                }
                catch (AggregateException)
                {
                    // Poll loop only observes/invokes events; cancellation exceptions are expected.
                }
            }

            cts.Dispose();
            _hangCts = null;
            _hangTask = null;
        }

        public void Dispose() => Stop();

        private async Task PollLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(HangPollIntervalMs, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                CheckForHang();
            }
        }

        /// <summary>
        /// Evaluates the hang condition. Public so tests can drive it deterministically
        /// without waiting for the poll interval.
        /// </summary>
        public void CheckForHang()
        {
            long last = Interlocked.Read(ref _lastTickTicks);
            if (last == 0) return; // no tick recorded yet — nothing to judge

            double msWithoutTick = (NowTicks() - last) * 1000.0 / Stopwatch.Frequency;
            if (msWithoutTick <= HangThresholdMs) return;

            bool shouldFire = false;
            lock (_hangLock)
            {
                if (!_isHung)
                {
                    _isHung = true;
                    shouldFire = true;
                }
            }

            if (shouldFire)
                HangDetected?.Invoke(msWithoutTick);
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
