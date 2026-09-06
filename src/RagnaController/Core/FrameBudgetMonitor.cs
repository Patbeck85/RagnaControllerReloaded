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
    /// Frame-Time Budget Monitor — tracks tick loop latency percentiles (P50, P95, P99)
    /// and emits ETW events when budget is exceeded. Thread-safe, lock-free design.
    /// </summary>
    public sealed class FrameBudgetMonitor : IDisposable
    {
        private const int MaxSamples = 10000; // Rolling window size
        private const double BudgetMs = 2.0;  // Target: sub-2ms P99

        // Lock-free ring buffer for latency samples (microseconds)
        private readonly long[] _samples = new long[MaxSamples];
        private int _writeIndex = 0;
        private int _sampleCount = 0;

        // Percentile caches (updated periodically) - use Interlocked for thread-safe reads
        private long _p50Us = 0;
        private long _p95Us = 0;
        private long _p99Us = 0;
        private long _totalTicks = 0;
        private long _overBudgetCount = 0;

        // Configuration
        private readonly double _budgetMs;
        private readonly string _componentName;
        private readonly RagnaControllerETW _etw = RagnaControllerETW.Log;
        private readonly AdvancedLogger? _logger;

        // Background percentile calculation
        private readonly Timer _percentileTimer;
        private readonly CancellationTokenSource _cts = new();
        private bool _disposed;

        public FrameBudgetMonitor(string componentName, double budgetMs = BudgetMs, AdvancedLogger? logger = null)
        {
            _componentName = componentName;
            _budgetMs = budgetMs;
            _logger = logger;

            // Calculate percentiles every 1000 ticks (~8 seconds at 125Hz)
            _percentileTimer = new Timer(CalculatePercentiles, null, TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(8));

            _logger?.Info($"[FrameBudgetMonitor] Started for {_componentName} — Budget: {_budgetMs}ms");
        }

        /// <summary>
        /// Records a tick duration. Call at the end of each tick loop iteration.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecordTick(double elapsedMs)
        {
            if (_disposed) return;

            long elapsedUs = (long)(elapsedMs * 1000.0);
            int index = Interlocked.Increment(ref _writeIndex) % MaxSamples;
            _samples[index] = elapsedUs;
            int count = Interlocked.Increment(ref _sampleCount);
            if (count > MaxSamples) Interlocked.Exchange(ref _sampleCount, MaxSamples);

            Interlocked.Increment(ref _totalTicks);

            // Check budget exceedance
            if (elapsedMs > _budgetMs)
            {
                long over = Interlocked.Increment(ref _overBudgetCount);
                // Emit ETW event for budget exceeded (throttled to avoid spam)
                if (over % 10 == 1) // Log every 10th exceedance
                {
                    _etw.OverlayFrameBudgetExceeded(_componentName, elapsedMs, _budgetMs);
                    _logger?.Warn($"[FrameBudgetMonitor] {_componentName} tick budget exceeded: {elapsedMs:F3}ms > {_budgetMs}ms (count: {over})");
                }
            }

            // Emit tick end ETW event (verbose)
            _etw.CpuTime((long)elapsedMs, _componentName);
        }

        /// <summary>
        /// Gets current percentile snapshot (P50, P95, P99 in milliseconds).
        /// </summary>
        public (double P50, double P95, double P99) GetPercentiles()
        {
            return (Interlocked.Read(ref _p50Us) / 1000.0, 
                    Interlocked.Read(ref _p95Us) / 1000.0, 
                    Interlocked.Read(ref _p99Us) / 1000.0);
        }

        /// <summary>
        /// Gets total ticks recorded and budget exceedance count.
        /// </summary>
        public (long TotalTicks, long OverBudgetCount, double OverBudgetPercent) GetStats()
        {
            long total = Interlocked.Read(ref _totalTicks);
            long over = Interlocked.Read(ref _overBudgetCount);
            double percent = total > 0 ? (double)over / total * 100.0 : 0.0;
            return (total, over, percent);
        }

        private void CalculatePercentiles(object? state)
        {
            if (_disposed) return;

            int count = Math.Min(_sampleCount, MaxSamples);
            if (count < 10) return; // Not enough samples

            // Copy samples to local array for sorting (lock-free read)
            var localSamples = new long[count];
            int start = (_writeIndex + 1) % MaxSamples;
            
            if (start + count <= MaxSamples)
            {
                Array.Copy(_samples, start, localSamples, 0, count);
            }
            else
            {
                int firstPart = MaxSamples - start;
                Array.Copy(_samples, start, localSamples, 0, firstPart);
                Array.Copy(_samples, 0, localSamples, firstPart, count - firstPart);
            }

            Array.Sort(localSamples);

            Interlocked.Exchange(ref _p50Us, localSamples[(int)(count * 0.50)]);
            Interlocked.Exchange(ref _p95Us, localSamples[(int)(count * 0.95)]);
            Interlocked.Exchange(ref _p99Us, localSamples[(int)(count * 0.99)]);

            // Emit ETW percentile event
            _etw.InputLatencyP99(Interlocked.Read(ref _p99Us), _componentName);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _percentileTimer.Dispose();
            _cts.Cancel();

            var (p50, p95, p99) = GetPercentiles();
            var (total, over, percent) = GetStats();

            _logger?.Info($"[FrameBudgetMonitor] {_componentName} stopped — Ticks: {total}, OverBudget: {over} ({percent:F1}%), P50: {p50:F3}ms, P95: {p95:F3}ms, P99: {p99:F3}ms");
            _etw.MemoryStats(0, 0, 0, 0); // Placeholder for final stats
        }
    }

    /// <summary>
    /// Central registry for all FrameBudgetMonitors in the application.
    /// Provides aggregated view and global budget enforcement.
    /// </summary>
    public static class FrameBudgetRegistry
    {
        private static readonly ConcurrentDictionary<string, FrameBudgetMonitor> _monitors = new();
        private static readonly AdvancedLogger? _logger = null; // Set via Initialize

        public static void Initialize(AdvancedLogger? logger = null)
        {
            // Logger can be set for registry-level logging
        }

        /// <summary>
        /// Gets or creates a monitor for the given component.
        /// </summary>
        public static FrameBudgetMonitor GetOrCreate(string componentName, double budgetMs = 2.0)
        {
            return _monitors.GetOrAdd(componentName, name => new FrameBudgetMonitor(name, budgetMs));
        }

        /// <summary>
        /// Gets all registered monitors for dashboard/reporting.
        /// </summary>
        public static IReadOnlyDictionary<string, FrameBudgetMonitor> GetAll() => _monitors;

        /// <summary>
        /// Generates a summary report of all monitors.
        /// </summary>
        public static string GenerateReport()
        {
            var lines = new List<string>
            {
                "=== Frame Budget Report ===",
                $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}",
                ""
            };

            foreach (var kvp in _monitors)
            {
                var (p50, p95, p99) = kvp.Value.GetPercentiles();
                var (total, over, percent) = kvp.Value.GetStats();
                lines.Add($"{kvp.Key}: Ticks={total}, Over={over} ({percent:F1}%), P50={p50:F3}ms, P95={p95:F3}ms, P99={p99:F3}ms");
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Disposes all monitors.
        /// </summary>
        public static void DisposeAll()
        {
            foreach (var monitor in _monitors.Values)
            {
                monitor.Dispose();
            }
            _monitors.Clear();
        }
    }
}