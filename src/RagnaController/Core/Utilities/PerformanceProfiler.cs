using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RagnaController.Core
{
    /// <summary>
    /// PERF-006: Per-Engine Tick Performance Profiler.
    /// Tracks execution time (ms) per engine per tick with P50/P95/P99 percentiles.
    /// Opt-in via EnableProfiling (zero-allocation hot path when disabled per PERF-010).
    /// Thread-safe, lock-free design for production use at 125Hz tick rate.
    /// </summary>
    public sealed class PerformanceProfiler : IDisposable
    {
        private const int MaxSamples = 10000;

        // Per-engine timing stats (microseconds)
        private readonly ConcurrentDictionary<string, EngineTimingStats> _engineStats = new();

        private readonly string _componentName;
        private readonly RagnaControllerETW _etw = RagnaControllerETW.Log;
        private readonly AdvancedLogger? _logger;
        private readonly Timer _percentileTimer;
        private readonly CancellationTokenSource _cts = new();
        private bool _disposed;

        // Global enable flag — opt-in, zero-allocation when disabled (PERF-010)
        public static bool EnableProfiling { get; set; } = false;

        public PerformanceProfiler(string componentName, AdvancedLogger? logger = null)
        {
            _componentName = componentName;
            _logger = logger;

            // Calculate percentiles every 10 seconds
            _percentileTimer = new Timer(CalculatePercentiles, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

            _logger?.Info($"[PerformanceProfiler] Started for {_componentName}");
        }

        /// <summary>
        /// Records an engine tick execution time.
        /// Call immediately after each engine Update/Handle/Tick call in the hot path.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecordEngineTick(string engineName, double elapsedMs)
        {
            if (_disposed || !EnableProfiling) return;

            long elapsedUs = (long)(elapsedMs * 1000.0);

            var stats = _engineStats.GetOrAdd(engineName, _ => new EngineTimingStats());
            stats.RecordSample(elapsedUs);

            // Emit ETW for slow engines (> 2ms per tick)
            if (elapsedMs > 2.0)
            {
                _etw.InputLatencyP99(elapsedUs, $"{_componentName}-{engineName}-SlowTick");
            }
        }

        /// <summary>
        /// Gets timing snapshot for all engines.
        /// </summary>
        public IReadOnlyDictionary<string, EngineTimingSnapshot> GetEngineStats()
        {
            var snapshot = new Dictionary<string, EngineTimingSnapshot>();
            foreach (var kvp in _engineStats)
            {
                snapshot[kvp.Key] = kvp.Value.GetSnapshot();
            }
            return snapshot;
        }

        /// <summary>
        /// Generates a human-readable report for dashboard/logging.
        /// </summary>
        public string GenerateReport()
        {
            var stats = GetEngineStats();
            var lines = new List<string>
            {
                $"=== Engine Performance Report: {_componentName} ===",
                $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}",
                $"",
                $"Engines tracked: {stats.Count}",
                $""
            };

            foreach (var kvp in stats.OrderByDescending(s => s.Value.P99Ms))
            {
                lines.Add($"{kvp.Key}:");
                lines.Add($"  P50: {kvp.Value.P50Ms:F3}ms, P95: {kvp.Value.P95Ms:F3}ms, P99: {kvp.Value.P99Ms:F3}ms");
                lines.Add($"  Max: {kvp.Value.MaxMs:F3}ms, Avg: {kvp.Value.AvgMs:F3}ms, Samples: {kvp.Value.SampleCount}");
                lines.Add($"");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private void CalculatePercentiles(object? state)
        {
            if (_disposed) return;

            foreach (var kvp in _engineStats)
            {
                // Percentiles are calculated lazily in GetSnapshot; this timer tick
                // just ensures the background calculation path is exercised.
                _ = kvp.Value.GetSnapshot();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _percentileTimer.Dispose();
            _cts.Cancel();

            _logger?.Info($"[PerformanceProfiler] {_componentName} stopped");
            _logger?.Info(GenerateReport());
        }
    }

    /// <summary>
    /// Thread-safe timing statistics for a single engine.
    /// Uses a ring buffer for lock-free sample recording and lazy percentile calculation.
    /// </summary>
    public sealed class EngineTimingStats
    {
        private const int MaxSamples = 10000;
        private readonly long[] _samples = new long[MaxSamples]; // microseconds
        private long _writeIndex = 0;
        private long _sampleCount = 0;
        private long _maxUs = 0;
        private long _totalUs = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecordSample(long elapsedUs)
        {
            int index = (int)(Interlocked.Increment(ref _writeIndex) % MaxSamples);
            _samples[index] = elapsedUs;

            long count = Interlocked.Increment(ref _sampleCount);
            if (count > MaxSamples) Interlocked.Exchange(ref _sampleCount, MaxSamples);

            Interlocked.Add(ref _totalUs, elapsedUs);

            // Track max (lock-free CAS loop)
            long currentMax = Interlocked.Read(ref _maxUs);
            while (elapsedUs > currentMax)
            {
                if (Interlocked.CompareExchange(ref _maxUs, elapsedUs, currentMax) == currentMax)
                    break;
                currentMax = Interlocked.Read(ref _maxUs);
            }
        }

        public EngineTimingSnapshot GetSnapshot()
        {
            int count = Math.Min((int)Interlocked.Read(ref _sampleCount), MaxSamples);
            if (count == 0)
                return new EngineTimingSnapshot(0, 0, 0, 0, 0, 0);

            // Copy samples for sorting (lock-free read from ring buffer)
            // oldest sample index = (writeIndex - count + 1 + MaxSamples) % MaxSamples
            long writeIndex = Interlocked.Read(ref _writeIndex);
            int start = (int)((writeIndex - count + 1 + MaxSamples) % MaxSamples);
            var local = new long[count];
            if (start + count <= MaxSamples)
            {
                Array.Copy(_samples, start, local, 0, count);
            }
            else
            {
                int firstPart = MaxSamples - start;
                Array.Copy(_samples, start, local, 0, firstPart);
                Array.Copy(_samples, 0, local, firstPart, count - firstPart);
            }

            Array.Sort(local);

            double p50 = local[(int)(count * 0.50)] / 1000.0;
            double p95 = local[(int)(count * 0.95)] / 1000.0;
            double p99 = local[(int)(count * 0.99)] / 1000.0;
            double max = Interlocked.Read(ref _maxUs) / 1000.0;
            double avg = Interlocked.Read(ref _totalUs) / (double)count / 1000.0;

            return new EngineTimingSnapshot(p50, p95, p99, max, avg, count);
        }
    }

    /// <summary>
    /// Immutable timing snapshot for a single engine (milliseconds).
    /// </summary>
    public readonly struct EngineTimingSnapshot
    {
        public double P50Ms { get; }
        public double P95Ms { get; }
        public double P99Ms { get; }
        public double MaxMs { get; }
        public double AvgMs { get; }
        public long SampleCount { get; }

        public EngineTimingSnapshot(double p50, double p95, double p99, double max, double avg, long count)
        {
            P50Ms = p50;
            P95Ms = p95;
            P99Ms = p99;
            MaxMs = max;
            AvgMs = avg;
            SampleCount = count;
        }
    }
}
