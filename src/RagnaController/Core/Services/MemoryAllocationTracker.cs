using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RagnaController.Core
{
    /// <summary>
    /// Memory Allocation Tracker — tracks Gen0/1/2 GC collections, Large Object Heap pressure,
    /// and object pool hit rates per tick. Thread-safe, lock-free design for production use.
    /// Integrates with ETW for performance analysis and regression detection.
    /// </summary>
    public sealed class MemoryAllocationTracker : IDisposable
    {
        private const int MaxSamples = 5000;
        private const int SampleIntervalTicks = 100; // Sample every 100 ticks (~0.8s at 125Hz)

        // Ring buffer for GC stats snapshots
        private readonly long[] _gen0Collections = new long[MaxSamples];
        private readonly long[] _gen1Collections = new long[MaxSamples];
        private readonly long[] _gen2Collections = new long[MaxSamples];
        private readonly long[] _lohSizeBytes = new long[MaxSamples];
        private readonly long[] _totalAllocatedBytes = new long[MaxSamples];
        private readonly long[] _workingSetBytes = new long[MaxSamples];
        
        // Object Pool tracking
        private readonly ConcurrentDictionary<string, ObjectPoolStats> _poolStats = new();
        
        // Current snapshot
        private int _writeIndex = 0;
        private long _sampleCount = 0;
        private long _tickCounter = 0;
        private long _lastGen0, _lastGen1, _lastGen2;
        private long _lastAllocatedBytes;
        
        // Percentile caches (updated periodically)
        private long _p50Gen0, _p95Gen0, _p99Gen0;
        private long _p50Gen1, _p95Gen1, _p99Gen1;
        private long _p50Gen2, _p95Gen2, _p99Gen2;
        private long _p50LohKb, _p95LohKb, _p99LohKb;
        private long _p50AllocKb, _p95AllocKb, _p99AllocKb;
        private long _p50WsMb, _p95WsMb, _p99WsMb;

        private readonly string _componentName;
        private readonly RagnaControllerETW _etw = RagnaControllerETW.Log;
        private readonly AdvancedLogger? _logger;
        private readonly Timer _percentileTimer;
        private readonly CancellationTokenSource _cts = new();
        private bool _disposed;

        public MemoryAllocationTracker(string componentName, AdvancedLogger? logger = null)
        {
            _componentName = componentName;
            _logger = logger;

            // Initialize baseline GC counts
            _lastGen0 = GC.CollectionCount(0);
            _lastGen1 = GC.CollectionCount(1);
            _lastGen2 = GC.CollectionCount(2);
            _lastAllocatedBytes = GC.GetTotalAllocatedBytes(false);

            // Calculate percentiles every 30 seconds
            _percentileTimer = new Timer(CalculatePercentiles, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            _logger?.Info($"[MemoryAllocationTracker] Started for {_componentName}");
        }

        /// <summary>
        /// Records a tick — call at end of each tick loop iteration.
        /// Samples GC stats every SampleIntervalTicks.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecordTick()
        {
            if (_disposed) return;

            long currentTick = Interlocked.Increment(ref _tickCounter);
            
            // Only sample periodically to reduce overhead
            if (currentTick % SampleIntervalTicks != 0) return;

            // Capture GC stats
            long gen0 = GC.CollectionCount(0);
            long gen1 = GC.CollectionCount(1);
            long gen2 = GC.CollectionCount(2);
            long totalAllocated = GC.GetTotalAllocatedBytes(false);
            
            // Calculate deltas since last sample
            long deltaGen0 = gen0 - Interlocked.Read(ref _lastGen0);
            long deltaGen1 = gen1 - Interlocked.Read(ref _lastGen1);
            long deltaGen2 = gen2 - Interlocked.Read(ref _lastGen2);
            long deltaAllocated = totalAllocated - Interlocked.Read(ref _lastAllocatedBytes);

            // Update baselines atomically
            Interlocked.Exchange(ref _lastGen0, gen0);
            Interlocked.Exchange(ref _lastGen1, gen1);
            Interlocked.Exchange(ref _lastGen2, gen2);
            Interlocked.Exchange(ref _lastAllocatedBytes, totalAllocated);

            // Working set
            long workingSet = Process.GetCurrentProcess().WorkingSet64;

            // LOH size approximation (objects > 85KB end up in LOH)
            long lohSize = GC.GetGCMemoryInfo().HeapSizeBytes > 0 
                ? GC.GetGCMemoryInfo().HighMemoryLoadThresholdBytes / 1024 
                : 0; // Fallback if GCMemoryInfo not available

            // Store in ring buffer
            int index = Interlocked.Increment(ref _writeIndex) % MaxSamples;
            _gen0Collections[index] = deltaGen0;
            _gen1Collections[index] = deltaGen1;
            _gen2Collections[index] = deltaGen2;
            _lohSizeBytes[index] = lohSize;
            _totalAllocatedBytes[index] = deltaAllocated;
            _workingSetBytes[index] = workingSet;
            
            long count = Interlocked.Increment(ref _sampleCount);
            if (count > MaxSamples) Interlocked.Exchange(ref _sampleCount, MaxSamples);

            // Emit ETW events for real-time monitoring (verbose)
            _etw.MemoryStats(
                workingSet / (1024 * 1024), 
                (int)deltaGen0, (int)deltaGen1, (int)deltaGen2);
        }

        /// <summary>
        /// Records an object pool operation (rent/return/hit/miss).
        /// </summary>
        public void RecordPoolOperation(string poolName, bool wasHit)
        {
            if (_disposed) return;
            
            var stats = _poolStats.GetOrAdd(poolName, _ => new ObjectPoolStats());
            if (wasHit)
                Interlocked.Increment(ref stats.Hits);
            else
                Interlocked.Increment(ref stats.Misses);
        }

        /// <summary>
        /// Records a pool rent/return cycle.
        /// </summary>
        public void RecordPoolRentReturn(string poolName)
        {
            if (_disposed) return;
            
            var stats = _poolStats.GetOrAdd(poolName, _ => new ObjectPoolStats());
            Interlocked.Increment(ref stats.RentReturns);
        }

        /// <summary>
        /// Gets current GC collection delta percentiles (per sample interval).
        /// </summary>
        public (long P50Gen0, long P95Gen0, long P99Gen0,
                long P50Gen1, long P95Gen1, long P99Gen1,
                long P50Gen2, long P95Gen2, long P99Gen2) GetGcPercentiles()
        {
            return (
                Interlocked.Read(ref _p50Gen0), Interlocked.Read(ref _p95Gen0), Interlocked.Read(ref _p99Gen0),
                Interlocked.Read(ref _p50Gen1), Interlocked.Read(ref _p95Gen1), Interlocked.Read(ref _p99Gen1),
                Interlocked.Read(ref _p50Gen2), Interlocked.Read(ref _p95Gen2), Interlocked.Read(ref _p99Gen2)
            );
        }

        /// <summary>
        /// Gets memory pressure percentiles.
        /// </summary>
        public (long P50LohKb, long P95LohKb, long P99LohKb,
                long P50AllocKb, long P95AllocKb, long P99AllocKb,
                long P50WsMb, long P95WsMb, long P99WsMb) GetMemoryPercentiles()
        {
            return (
                Interlocked.Read(ref _p50LohKb), Interlocked.Read(ref _p95LohKb), Interlocked.Read(ref _p99LohKb),
                Interlocked.Read(ref _p50AllocKb), Interlocked.Read(ref _p95AllocKb), Interlocked.Read(ref _p99AllocKb),
                Interlocked.Read(ref _p50WsMb), Interlocked.Read(ref _p95WsMb), Interlocked.Read(ref _p99WsMb)
            );
        }

        /// <summary>
        /// Gets object pool hit rate statistics.
        /// </summary>
        public IReadOnlyDictionary<string, ObjectPoolSnapshot> GetPoolStats()
        {
            var snapshot = new Dictionary<string, ObjectPoolSnapshot>();
            foreach (var kvp in _poolStats)
            {
                long hits = Interlocked.Read(ref kvp.Value.Hits);
                long misses = Interlocked.Read(ref kvp.Value.Misses);
                long rentReturns = Interlocked.Read(ref kvp.Value.RentReturns);
                long total = hits + misses;
                double hitRate = total > 0 ? (double)hits / total * 100.0 : 0.0;
                
                snapshot[kvp.Key] = new ObjectPoolSnapshot
                {
                    PoolName = kvp.Key,
                    Hits = hits,
                    Misses = misses,
                    RentReturns = rentReturns,
                    HitRatePercent = hitRate
                };
            }
            return snapshot;
        }

        /// <summary>
        /// Gets aggregate stats since start.
        /// </summary>
        public (long TotalGen0, long TotalGen1, long TotalGen2, 
                long TotalAllocatedMb, long CurrentWorkingSetMb,
                long SamplesCollected) GetAggregateStats()
        {
            return (
                GC.CollectionCount(0),
                GC.CollectionCount(1),
                GC.CollectionCount(2),
                GC.GetTotalAllocatedBytes(false) / (1024 * 1024),
                Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024),
                (long)Interlocked.Read(ref _sampleCount)
            );
        }

        private void CalculatePercentiles(object? state)
        {
            if (_disposed) return;

            int count = Math.Min((int)Interlocked.Read(ref _sampleCount), MaxSamples);
            if (count < 10) return;

            // Copy samples for sorting (lock-free read)
            var gen0Local = new long[count];
            var gen1Local = new long[count];
            var gen2Local = new long[count];
            var lohLocal = new long[count];
            var allocLocal = new long[count];
            var wsLocal = new long[count];

            int start = (_writeIndex + 1) % MaxSamples;
            CopyRingBuffer(_gen0Collections, gen0Local, start, count, MaxSamples);
            CopyRingBuffer(_gen1Collections, gen1Local, start, count, MaxSamples);
            CopyRingBuffer(_gen2Collections, gen2Local, start, count, MaxSamples);
            CopyRingBuffer(_lohSizeBytes, lohLocal, start, count, MaxSamples);
            CopyRingBuffer(_totalAllocatedBytes, allocLocal, start, count, MaxSamples);
            CopyRingBuffer(_workingSetBytes, wsLocal, start, count, MaxSamples);

            Array.Sort(gen0Local);
            Array.Sort(gen1Local);
            Array.Sort(gen2Local);
            Array.Sort(lohLocal);
            Array.Sort(allocLocal);
            Array.Sort(wsLocal);

            Interlocked.Exchange(ref _p50Gen0, gen0Local[(int)(count * 0.50)]);
            Interlocked.Exchange(ref _p95Gen0, gen0Local[(int)(count * 0.95)]);
            Interlocked.Exchange(ref _p99Gen0, gen0Local[(int)(count * 0.99)]);
            
            Interlocked.Exchange(ref _p50Gen1, gen1Local[(int)(count * 0.50)]);
            Interlocked.Exchange(ref _p95Gen1, gen1Local[(int)(count * 0.95)]);
            Interlocked.Exchange(ref _p99Gen1, gen1Local[(int)(count * 0.99)]);
            
            Interlocked.Exchange(ref _p50Gen2, gen2Local[(int)(count * 0.50)]);
            Interlocked.Exchange(ref _p95Gen2, gen2Local[(int)(count * 0.95)]);
            Interlocked.Exchange(ref _p99Gen2, gen2Local[(int)(count * 0.99)]);

            Interlocked.Exchange(ref _p50LohKb, lohLocal[(int)(count * 0.50)] / 1024);
            Interlocked.Exchange(ref _p95LohKb, lohLocal[(int)(count * 0.95)] / 1024);
            Interlocked.Exchange(ref _p99LohKb, lohLocal[(int)(count * 0.99)] / 1024);

            Interlocked.Exchange(ref _p50AllocKb, allocLocal[(int)(count * 0.50)] / 1024);
            Interlocked.Exchange(ref _p95AllocKb, allocLocal[(int)(count * 0.95)] / 1024);
            Interlocked.Exchange(ref _p99AllocKb, allocLocal[(int)(count * 0.99)] / 1024);

            Interlocked.Exchange(ref _p50WsMb, wsLocal[(int)(count * 0.50)] / (1024 * 1024));
            Interlocked.Exchange(ref _p95WsMb, wsLocal[(int)(count * 0.95)] / (1024 * 1024));
            Interlocked.Exchange(ref _p99WsMb, wsLocal[(int)(count * 0.99)] / (1024 * 1024));

            // Emit ETW percentile events
            _etw.InputLatencyP99(Interlocked.Read(ref _p99AllocKb), $"{_componentName}-AllocKb");
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

            var (totalGen0, totalGen1, totalGen2, totalAllocMb, currentWsMb, samples) = GetAggregateStats();
            var gcPercentiles = GetGcPercentiles();
            var memPercentiles = GetMemoryPercentiles();

            _logger?.Info($"[MemoryAllocationTracker] {_componentName} stopped — " +
                $"Samples: {samples}, Gen0: {totalGen0} (P50:{gcPercentiles.P50Gen0}, P99:{gcPercentiles.P99Gen0}), " +
                $"Gen1: {totalGen1} (P50:{gcPercentiles.P50Gen1}, P99:{gcPercentiles.P99Gen1}), " +
                $"Gen2: {totalGen2} (P50:{gcPercentiles.P50Gen2}, P99:{gcPercentiles.P99Gen2}), " +
                $"Alloc: {totalAllocMb}MB (P99:{memPercentiles.P99AllocKb}KB), " +
                $"WS: {currentWsMb}MB (P99:{memPercentiles.P99WsMb}MB), " +
                $"LOH P99: {memPercentiles.P99LohKb}KB");

            // Emit final summary ETW event
            _etw.MemoryStats(currentWsMb, (int)totalGen0, (int)totalGen1, (int)totalGen2);

            // Log pool stats
            foreach (var pool in GetPoolStats())
            {
                _logger?.Info($"[MemoryAllocationTracker] Pool '{pool.Key}': " +
                    $"Hits={pool.Value.Hits}, Misses={pool.Value.Misses}, " +
                    $"RentReturns={pool.Value.RentReturns}, HitRate={pool.Value.HitRatePercent:F1}%");
            }
        }

        /// <summary>
        /// Generates a human-readable report for dashboard/logging.
        /// </summary>
        public string GenerateReport()
        {
            var gc = GetGcPercentiles();
            var mem = GetMemoryPercentiles();
            var agg = GetAggregateStats();
            var pools = GetPoolStats();

            var lines = new System.Collections.Generic.List<string>
            {
                $"=== Memory Allocation Report: {_componentName} ===",
                $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}",
                $"Samples Collected: {agg.SamplesCollected}",
                $"",
                $"GC Collections (per sample interval):",
                $"  Gen0: Total={agg.TotalGen0}, P50={gc.P50Gen0}, P95={gc.P95Gen0}, P99={gc.P99Gen0}",
                $"  Gen1: Total={agg.TotalGen1}, P50={gc.P50Gen1}, P95={gc.P95Gen1}, P99={gc.P99Gen1}",
                $"  Gen2: Total={agg.TotalGen2}, P50={gc.P50Gen2}, P95={gc.P95Gen2}, P99={gc.P99Gen2}",
                $"",
                $"Memory Pressure:",
                $"  Allocated: Total={agg.TotalAllocatedMb}MB, P50={mem.P50AllocKb}KB, P95={mem.P95AllocKb}KB, P99={mem.P99AllocKb}KB",
                $"  Working Set: Current={agg.CurrentWorkingSetMb}MB, P50={mem.P50WsMb}MB, P95={mem.P95WsMb}MB, P99={mem.P99WsMb}MB",
                $"  LOH (est): P50={mem.P50LohKb}KB, P95={mem.P95LohKb}KB, P99={mem.P99LohKb}KB",
                $"",
                $"Object Pool Stats:"
            };

            foreach (var pool in pools)
            {
                lines.Add($"  {pool.Key}: Hits={pool.Value.Hits}, Misses={pool.Value.Misses}, " +
                    $"RentReturns={pool.Value.RentReturns}, HitRate={pool.Value.HitRatePercent:F1}%");
            }

            if (pools.Count == 0)
            {
                lines.Add("  (no pool data recorded)");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private sealed class ObjectPoolStats
        {
            public long Hits;
            public long Misses;
            public long RentReturns;
        }

        public readonly struct ObjectPoolSnapshot
        {
            public string PoolName { get; init; }
            public long Hits { get; init; }
            public long Misses { get; init; }
            public long RentReturns { get; init; }
            public double HitRatePercent { get; init; }
        }
    }

    /// <summary>
    /// Central registry for all MemoryAllocationTrackers in the application.
    /// Provides aggregated view and global memory pressure monitoring.
    /// </summary>
    public static class MemoryAllocationRegistry
    {
        private static readonly ConcurrentDictionary<string, MemoryAllocationTracker> _trackers = new();
        private static AdvancedLogger? _logger;

        public static void Initialize(AdvancedLogger? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets or creates a tracker for the given component.
        /// </summary>
        public static MemoryAllocationTracker GetOrCreate(string componentName)
        {
            return _trackers.GetOrAdd(componentName, name => new MemoryAllocationTracker(name, _logger));
        }

        /// <summary>
        /// Gets all registered trackers for dashboard/reporting.
        /// </summary>
        public static IReadOnlyDictionary<string, MemoryAllocationTracker> GetAll() => _trackers;

        /// <summary>
        /// Generates a consolidated report of all trackers.
        /// </summary>
        public static string GenerateConsolidatedReport()
        {
            var lines = new System.Collections.Generic.List<string>
            {
                "=== Consolidated Memory Allocation Report ===",
                $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}",
                $"Active Trackers: {_trackers.Count}",
                ""
            };

            foreach (var kvp in _trackers)
            {
                lines.Add(kvp.Value.GenerateReport());
                lines.Add("");
            }

            // Global GC stats
            lines.Add("=== Global GC Stats ===");
            lines.Add($"Gen0 Collections: {GC.CollectionCount(0)}");
            lines.Add($"Gen1 Collections: {GC.CollectionCount(1)}");
            lines.Add($"Gen2 Collections: {GC.CollectionCount(2)}");
            lines.Add($"Total Allocated: {GC.GetTotalAllocatedBytes(false) / (1024 * 1024)} MB");
            lines.Add($"Working Set: {Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024)} MB");

            try
            {
                var gcInfo = GC.GetGCMemoryInfo();
                lines.Add($"Heap Size: {gcInfo.HeapSizeBytes / (1024 * 1024)} MB");
                lines.Add($"High Memory Load Threshold: {gcInfo.HighMemoryLoadThresholdBytes / (1024 * 1024)} MB");
                lines.Add($"Memory Load: {gcInfo.MemoryLoadBytes / (1024 * 1024)} MB");
            }
            catch
            {
                lines.Add("GCMemoryInfo not available on this runtime");
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