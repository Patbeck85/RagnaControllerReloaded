using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RagnaController.Core
{
    /// <summary>
    /// Input Latency Tracker — tracks end-to-end latency from hardware event → SendInput completion.
    /// Measures P50/P95/P99 percentiles with target P99 < 5ms.
    /// Integrates with ETW for real-time monitoring and regression detection.
    /// Thread-safe, lock-free design for production use at 125Hz tick rate.
    /// </summary>
    public sealed class InputLatencyTracker : IDisposable
    {
        private const int MaxSamples = 50000; // Rolling window (~6.6 minutes at 125Hz)
        private const double TargetP99Ms = 5.0; // Target: P99 < 5ms

        // Lock-free ring buffer for latency samples (microseconds)
        private readonly long[] _enqueueLatencyUs = new long[MaxSamples];
        private readonly long[] _dispatchLatencyUs = new long[MaxSamples];
        private readonly long[] _totalLatencyUs = new long[MaxSamples];
        private readonly long[] _sendInputLatencyUs = new long[MaxSamples];

        private int _writeIndex = 0;
        private long _sampleCount = 0;

        // Percentile caches (updated periodically) - use Interlocked for thread-safe reads
        private long _p50EnqueueUs, _p95EnqueueUs, _p99EnqueueUs;
        private long _p50DispatchUs, _p95DispatchUs, _p99DispatchUs;
        private long _p50TotalUs, _p95TotalUs, _p99TotalUs;
        private long _p50SendInputUs, _p95SendInputUs, _p99SendInputUs;

        private long _totalSamples = 0;
        private long _overBudgetCount = 0; // Exceeds 5ms total latency

        // Per-controller tracking
        private readonly ConcurrentDictionary<string, ControllerLatencyStats> _controllerStats = new();

        // Configuration
        private readonly string _componentName;
        private readonly RagnaControllerETW _etw = RagnaControllerETW.Log;
        private readonly AdvancedLogger? _logger;

        // Background percentile calculation
        private readonly Timer _percentileTimer;
        private readonly CancellationTokenSource _cts = new();
        private bool _disposed;

        public InputLatencyTracker(string componentName, AdvancedLogger? logger = null)
        {
            _componentName = componentName;
            _logger = logger;

            // Calculate percentiles every 10 seconds
            _percentileTimer = new Timer(CalculatePercentiles, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            _logger?.Info($"[InputLatencyTracker] Started for {_componentName} — Target P99: {TargetP99Ms}ms");
        }

        /// <summary>
        /// Records the enqueue latency (time from hardware event to queue enqueue).
        /// Call when a controller event is received and enqueued to InputCommandQueue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecordEnqueueLatency(double elapsedMs, string controllerId = "Default")
        {
            if (_disposed) return;

            long elapsedUs = (long)(elapsedMs * 1000.0);
            int index = Interlocked.Increment(ref _writeIndex) % MaxSamples;
            _enqueueLatencyUs[index] = elapsedUs;

            // Update per-controller stats
            var stats = _controllerStats.GetOrAdd(controllerId, _ => new ControllerLatencyStats());
            Interlocked.Add(ref stats.TotalEnqueueLatencyUs, elapsedUs);
            Interlocked.Increment(ref stats.EnqueueCount);

            // Update max (lock-free)
            long currentMax = Interlocked.Read(ref stats.MaxEnqueueLatencyUs);
            while (elapsedUs > currentMax)
            {
                if (Interlocked.CompareExchange(ref stats.MaxEnqueueLatencyUs, elapsedUs, currentMax) == currentMax)
                    break;
                currentMax = Interlocked.Read(ref stats.MaxEnqueueLatencyUs);
            }

            long count = Interlocked.Increment(ref _sampleCount);
            if (count > MaxSamples) Interlocked.Exchange(ref _sampleCount, MaxSamples);
            Interlocked.Increment(ref _totalSamples);

            // Emit ETW event for high enqueue latency
            if (elapsedMs > 1.0) // > 1ms enqueue latency
            {
                _etw.InputLatencyP99(elapsedUs, $"{_componentName}-Enqueue-{controllerId}");
            }
        }

        /// <summary>
        /// Records the dispatch latency (time from queue dequeue to SendInput call).
        /// Call in InputCommandQueue consumer thread before SendInput.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecordDispatchLatency(double elapsedMs, string controllerId = "Default")
        {
            if (_disposed) return;

            long elapsedUs = (long)(elapsedMs * 1000.0);
            int index = Interlocked.Increment(ref _writeIndex) % MaxSamples;
            _dispatchLatencyUs[index] = elapsedUs;

            // Update per-controller stats
            var stats = _controllerStats.GetOrAdd(controllerId, _ => new ControllerLatencyStats());
            Interlocked.Add(ref stats.TotalDispatchLatencyUs, elapsedUs);
            Interlocked.Increment(ref stats.DispatchCount);

            long currentMax = Interlocked.Read(ref stats.MaxDispatchLatencyUs);
            while (elapsedUs > currentMax)
            {
                if (Interlocked.CompareExchange(ref stats.MaxDispatchLatencyUs, elapsedUs, currentMax) == currentMax)
                    break;
                currentMax = Interlocked.Read(ref stats.MaxDispatchLatencyUs);
            }

            // Emit ETW event for high dispatch latency
            if (elapsedMs > 2.0) // > 2ms dispatch latency
            {
                _etw.InputLatencyP99(elapsedUs, $"{_componentName}-Dispatch-{controllerId}");
            }
        }

        /// <summary>
        /// Records the SendInput latency (time taken by SendInput API call).
        /// Call immediately after SendInput returns.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecordSendInputLatency(double elapsedMs, int inputCount, string controllerId = "Default")
        {
            if (_disposed) return;

            long elapsedUs = (long)(elapsedMs * 1000.0);
            int index = Interlocked.Increment(ref _writeIndex) % MaxSamples;
            _sendInputLatencyUs[index] = elapsedUs;

            // Update per-controller stats
            var stats = _controllerStats.GetOrAdd(controllerId, _ => new ControllerLatencyStats());
            Interlocked.Add(ref stats.TotalSendInputLatencyUs, elapsedUs);
            Interlocked.Increment(ref stats.SendInputCount);
            Interlocked.Add(ref stats.TotalInputsSent, inputCount);

            long currentMax = Interlocked.Read(ref stats.MaxSendInputLatencyUs);
            while (elapsedUs > currentMax)
            {
                if (Interlocked.CompareExchange(ref stats.MaxSendInputLatencyUs, elapsedUs, currentMax) == currentMax)
                    break;
                currentMax = Interlocked.Read(ref stats.MaxSendInputLatencyUs);
            }

            // Emit ETW event for high SendInput latency
            if (elapsedMs > 1.0) // > 1ms SendInput call
            {
                _etw.InputLatencyP99(elapsedUs, $"{_componentName}-SendInput-{controllerId}");
            }
        }

        /// <summary>
        /// Records the total end-to-end latency (hardware event → SendInput completion).
        /// Call after command execution completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecordTotalLatency(double elapsedMs, string controllerId = "Default")
        {
            if (_disposed) return;

            long elapsedUs = (long)(elapsedMs * 1000.0);
            int index = Interlocked.Increment(ref _writeIndex) % MaxSamples;
            _totalLatencyUs[index] = elapsedUs;

            // Update per-controller stats
            var stats = _controllerStats.GetOrAdd(controllerId, _ => new ControllerLatencyStats());
            Interlocked.Add(ref stats.TotalEndToEndLatencyUs, elapsedUs);
            Interlocked.Increment(ref stats.EndToEndCount);

            long currentMax = Interlocked.Read(ref stats.MaxEndToEndLatencyUs);
            while (elapsedUs > currentMax)
            {
                if (Interlocked.CompareExchange(ref stats.MaxEndToEndLatencyUs, elapsedUs, currentMax) == currentMax)
                    break;
                currentMax = Interlocked.Read(ref stats.MaxEndToEndLatencyUs);
            }

            // Check budget exceedance
            if (elapsedMs > TargetP99Ms)
            {
                long over = Interlocked.Increment(ref _overBudgetCount);
                // Emit ETW event for budget exceeded (throttled)
                if (over % 5 == 1) // Log every 5th exceedance
                {
                    _logger?.Warn($"[InputLatencyTracker] {_componentName} end-to-end latency budget exceeded: {elapsedMs:F3}ms > {TargetP99Ms}ms (count: {over})");
                    _etw.OverlayFrameBudgetExceeded($"{_componentName}-{controllerId}", elapsedMs, TargetP99Ms);
                }
            }

            // Emit regular ETW event for percentile tracking
            _etw.InputLatencyP99(elapsedUs, $"{_componentName}-Total-{controllerId}");
        }

        /// <summary>
        /// Gets current percentile snapshot for all latency stages (in milliseconds).
        /// </summary>
        public InputLatencyPercentiles GetPercentiles()
        {
            return new InputLatencyPercentiles
            {
                Enqueue = (Interlocked.Read(ref _p50EnqueueUs) / 1000.0, Interlocked.Read(ref _p95EnqueueUs) / 1000.0, Interlocked.Read(ref _p99EnqueueUs) / 1000.0),
                Dispatch = (Interlocked.Read(ref _p50DispatchUs) / 1000.0, Interlocked.Read(ref _p95DispatchUs) / 1000.0, Interlocked.Read(ref _p99DispatchUs) / 1000.0),
                SendInput = (Interlocked.Read(ref _p50SendInputUs) / 1000.0, Interlocked.Read(ref _p95SendInputUs) / 1000.0, Interlocked.Read(ref _p99SendInputUs) / 1000.0),
                Total = (Interlocked.Read(ref _p50TotalUs) / 1000.0, Interlocked.Read(ref _p95TotalUs) / 1000.0, Interlocked.Read(ref _p99TotalUs) / 1000.0),
                SampleCount = Interlocked.Read(ref _sampleCount),
                TotalSamples = Interlocked.Read(ref _totalSamples),
                OverBudgetCount = Interlocked.Read(ref _overBudgetCount),
                OverBudgetPercent = Interlocked.Read(ref _totalSamples) > 0 
                    ? (double)Interlocked.Read(ref _overBudgetCount) / Interlocked.Read(ref _totalSamples) * 100.0 
                    : 0.0
            };
        }

        /// <summary>
        /// Gets per-controller latency statistics.
        /// </summary>
        public IReadOnlyDictionary<string, ControllerLatencySnapshot> GetControllerStats()
        {
            var snapshot = new Dictionary<string, ControllerLatencySnapshot>();
            foreach (var kvp in _controllerStats)
            {
                var stats = kvp.Value;
                long enqueueCount = Interlocked.Read(ref stats.EnqueueCount);
                long dispatchCount = Interlocked.Read(ref stats.DispatchCount);
                long sendInputCount = Interlocked.Read(ref stats.SendInputCount);
                long endToEndCount = Interlocked.Read(ref stats.EndToEndCount);

                snapshot[kvp.Key] = new ControllerLatencySnapshot
                {
                    ControllerId = kvp.Key,
                    EnqueueCount = enqueueCount,
                    DispatchCount = dispatchCount,
                    SendInputCount = sendInputCount,
                    EndToEndCount = endToEndCount,
                    TotalInputsSent = Interlocked.Read(ref stats.TotalInputsSent),
                    AvgEnqueueMs = enqueueCount > 0 ? Interlocked.Read(ref stats.TotalEnqueueLatencyUs) / (double)enqueueCount / 1000.0 : 0,
                    AvgDispatchMs = dispatchCount > 0 ? Interlocked.Read(ref stats.TotalDispatchLatencyUs) / (double)dispatchCount / 1000.0 : 0,
                    AvgSendInputMs = sendInputCount > 0 ? Interlocked.Read(ref stats.TotalSendInputLatencyUs) / (double)sendInputCount / 1000.0 : 0,
                    AvgTotalMs = endToEndCount > 0 ? Interlocked.Read(ref stats.TotalEndToEndLatencyUs) / (double)endToEndCount / 1000.0 : 0,
                    MaxEnqueueMs = Interlocked.Read(ref stats.MaxEnqueueLatencyUs) / 1000.0,
                    MaxDispatchMs = Interlocked.Read(ref stats.MaxDispatchLatencyUs) / 1000.0,
                    MaxSendInputMs = Interlocked.Read(ref stats.MaxSendInputLatencyUs) / 1000.0,
                    MaxTotalMs = Interlocked.Read(ref stats.MaxEndToEndLatencyUs) / 1000.0
                };
            }
            return snapshot;
        }

        private void CalculatePercentiles(object? state)
        {
            if (_disposed) return;

            int count = Math.Min((int)Interlocked.Read(ref _sampleCount), MaxSamples);
            if (count < 10) return; // Not enough samples

            // Copy samples for sorting (lock-free read)
            var enqueueLocal = new long[count];
            var dispatchLocal = new long[count];
            var sendInputLocal = new long[count];
            var totalLocal = new long[count];

            int start = (_writeIndex + 1) % MaxSamples;
            CopyRingBuffer(_enqueueLatencyUs, enqueueLocal, start, count, MaxSamples);
            CopyRingBuffer(_dispatchLatencyUs, dispatchLocal, start, count, MaxSamples);
            CopyRingBuffer(_sendInputLatencyUs, sendInputLocal, start, count, MaxSamples);
            CopyRingBuffer(_totalLatencyUs, totalLocal, start, count, MaxSamples);

            Array.Sort(enqueueLocal);
            Array.Sort(dispatchLocal);
            Array.Sort(sendInputLocal);
            Array.Sort(totalLocal);

            Interlocked.Exchange(ref _p50EnqueueUs, enqueueLocal[(int)(count * 0.50)]);
            Interlocked.Exchange(ref _p95EnqueueUs, enqueueLocal[(int)(count * 0.95)]);
            Interlocked.Exchange(ref _p99EnqueueUs, enqueueLocal[(int)(count * 0.99)]);

            Interlocked.Exchange(ref _p50DispatchUs, dispatchLocal[(int)(count * 0.50)]);
            Interlocked.Exchange(ref _p95DispatchUs, dispatchLocal[(int)(count * 0.95)]);
            Interlocked.Exchange(ref _p99DispatchUs, dispatchLocal[(int)(count * 0.99)]);

            Interlocked.Exchange(ref _p50SendInputUs, sendInputLocal[(int)(count * 0.50)]);
            Interlocked.Exchange(ref _p95SendInputUs, sendInputLocal[(int)(count * 0.95)]);
            Interlocked.Exchange(ref _p99SendInputUs, sendInputLocal[(int)(count * 0.99)]);

            Interlocked.Exchange(ref _p50TotalUs, totalLocal[(int)(count * 0.50)]);
            Interlocked.Exchange(ref _p95TotalUs, totalLocal[(int)(count * 0.95)]);
            Interlocked.Exchange(ref _p99TotalUs, totalLocal[(int)(count * 0.99)]);

            // Emit ETW percentile events
            _etw.InputLatencyP99(Interlocked.Read(ref _p99TotalUs), $"{_componentName}-Total-P99");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyRingBuffer(long[] source, long[] dest, int start, int count, int maxSamples)
        {
            if (start + count <= maxSamples)
            {
                Array.Copy(source, start, dest, 0, count);
            }
            else
            {
                int firstPart = maxSamples - start;
                Array.Copy(source, start, dest, 0, firstPart);
                Array.Copy(source, 0, dest, firstPart, count - firstPart);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _percentileTimer.Dispose();
            _cts.Cancel();

            var percentiles = GetPercentiles();
            _logger?.Info($"[InputLatencyTracker] {_componentName} stopped — Samples: {percentiles.SampleCount}, " +
                $"Enqueue P99: {percentiles.Enqueue.P99:F3}ms, " +
                $"Dispatch P99: {percentiles.Dispatch.P99:F3}ms, " +
                $"SendInput P99: {percentiles.SendInput.P99:F3}ms, " +
                $"Total P99: {percentiles.Total.P99:F3}ms, " +
                $"OverBudget: {percentiles.OverBudgetCount} ({percentiles.OverBudgetPercent:F1}%)");

            // Log per-controller stats
            foreach (var controller in GetControllerStats())
            {
                _logger?.Info($"[InputLatencyTracker] Controller '{controller.Key}': " +
                    $"Enqueue avg={controller.Value.AvgEnqueueMs:F3}ms max={controller.Value.MaxEnqueueMs:F3}ms, " +
                    $"Dispatch avg={controller.Value.AvgDispatchMs:F3}ms max={controller.Value.MaxDispatchMs:F3}ms, " +
                    $"SendInput avg={controller.Value.AvgSendInputMs:F3}ms max={controller.Value.MaxSendInputMs:F3}ms, " +
                    $"Total avg={controller.Value.AvgTotalMs:F3}ms max={controller.Value.MaxTotalMs:F3}ms");
            }

            // Emit final summary ETW event
            _etw.InputLatencyP99(Interlocked.Read(ref _p99TotalUs), $"{_componentName}-Final");
        }

        /// <summary>
        /// Generates a human-readable report for dashboard/logging.
        /// </summary>
        public string GenerateReport()
        {
            var p = GetPercentiles();
            var controllers = GetControllerStats();

            var lines = new List<string>
            {
                $"=== Input Latency Report: {_componentName} ===",
                $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}",
                $"Target P99: {TargetP99Ms}ms",
                $"",
                $"=== End-to-End Latency ===",
                $"  Samples: {p.SampleCount} / Total: {p.TotalSamples}",
                $"  P50: {p.Total.P50:F3}ms, P95: {p.Total.P95:F3}ms, P99: {p.Total.P99:F3}ms",
                $"  Over Budget (> {TargetP99Ms}ms): {p.OverBudgetCount} ({p.OverBudgetPercent:F1}%)",
                $"",
                $"=== Stage Breakdown ===",
                $"  Enqueue (HW→Queue):   P50={p.Enqueue.P50:F3}ms, P95={p.Enqueue.P95:F3}ms, P99={p.Enqueue.P99:F3}ms",
                $"  Dispatch (Queue→Send): P50={p.Dispatch.P50:F3}ms, P95={p.Dispatch.P95:F3}ms, P99={p.Dispatch.P99:F3}ms",
                $"  SendInput (API call):  P50={p.SendInput.P50:F3}ms, P95={p.SendInput.P95:F3}ms, P99={p.SendInput.P99:F3}ms",
                $"",
                $"=== Per-Controller ==="
            };

            foreach (var c in controllers)
            {
                lines.Add($"  {c.Key}:");
                lines.Add($"    Enqueue:   avg={c.Value.AvgEnqueueMs:F3}ms max={c.Value.MaxEnqueueMs:F3}ms (n={c.Value.EnqueueCount})");
                lines.Add($"    Dispatch:  avg={c.Value.AvgDispatchMs:F3}ms max={c.Value.MaxDispatchMs:F3}ms (n={c.Value.DispatchCount})");
                lines.Add($"    SendInput: avg={c.Value.AvgSendInputMs:F3}ms max={c.Value.MaxSendInputMs:F3}ms (n={c.Value.SendInputCount}, inputs={c.Value.TotalInputsSent})");
                lines.Add($"    Total:     avg={c.Value.AvgTotalMs:F3}ms max={c.Value.MaxTotalMs:F3}ms (n={c.Value.EndToEndCount})");
            }

            if (controllers.Count == 0)
            {
                lines.Add("  (no controller data recorded)");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private sealed class ControllerLatencyStats
        {
            public long EnqueueCount;
            public long DispatchCount;
            public long SendInputCount;
            public long EndToEndCount;
            public long TotalInputsSent;
            public long TotalEnqueueLatencyUs;
            public long TotalDispatchLatencyUs;
            public long TotalSendInputLatencyUs;
            public long TotalEndToEndLatencyUs;
            public long MaxEnqueueLatencyUs;
            public long MaxDispatchLatencyUs;
            public long MaxSendInputLatencyUs;
            public long MaxEndToEndLatencyUs;
        }

        public readonly struct ControllerLatencySnapshot
        {
            public string ControllerId { get; init; }
            public long EnqueueCount { get; init; }
            public long DispatchCount { get; init; }
            public long SendInputCount { get; init; }
            public long EndToEndCount { get; init; }
            public long TotalInputsSent { get; init; }
            public double AvgEnqueueMs { get; init; }
            public double AvgDispatchMs { get; init; }
            public double AvgSendInputMs { get; init; }
            public double AvgTotalMs { get; init; }
            public double MaxEnqueueMs { get; init; }
            public double MaxDispatchMs { get; init; }
            public double MaxSendInputMs { get; init; }
            public double MaxTotalMs { get; init; }
        }

        public readonly struct InputLatencyPercentiles
        {
            public (double P50, double P95, double P99) Enqueue { get; init; }
            public (double P50, double P95, double P99) Dispatch { get; init; }
            public (double P50, double P95, double P99) SendInput { get; init; }
            public (double P50, double P95, double P99) Total { get; init; }
            public long SampleCount { get; init; }
            public long TotalSamples { get; init; }
            public long OverBudgetCount { get; init; }
            public double OverBudgetPercent { get; init; }
        }
    }

    /// <summary>
    /// Central registry for all InputLatencyTrackers in the application.
    /// Provides aggregated view and global latency monitoring.
    /// </summary>
    public static class InputLatencyRegistry
    {
        private static readonly ConcurrentDictionary<string, InputLatencyTracker> _trackers = new();
        private static AdvancedLogger? _logger;

        public static void Initialize(AdvancedLogger? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets or creates a tracker for the given component.
        /// </summary>
        public static InputLatencyTracker GetOrCreate(string componentName)
        {
            return _trackers.GetOrAdd(componentName, name => new InputLatencyTracker(name, _logger));
        }

        /// <summary>
        /// Gets all registered trackers for dashboard/reporting.
        /// </summary>
        public static IReadOnlyDictionary<string, InputLatencyTracker> GetAll() => _trackers;

        /// <summary>
        /// Generates a consolidated report of all trackers.
        /// </summary>
        public static string GenerateConsolidatedReport()
        {
            var lines = new List<string>
            {
                "=== Consolidated Input Latency Report ===",
                $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}",
                $"Active Trackers: {_trackers.Count}",
                ""
            };

            foreach (var kvp in _trackers)
            {
                lines.Add(kvp.Value.GenerateReport());
                lines.Add("");
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Disposes all trackers.
        /// </summary>
        public static void DisposeAll()
        {
            foreach (var tracker in _trackers.Values)
            {
                tracker.Dispose();
            }
            _trackers.Clear();
        }
    }
}