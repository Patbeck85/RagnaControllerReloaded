using System.Diagnostics;
using RagnaController.Core;
using Xunit;

namespace RagnaController.Tests
{
    /// <summary>
    /// Deterministic performance tests (TEST-014).
    ///
    /// Design rules that make these non-flaky:
    ///  - NO wall-clock comparison of two sub-millisecond loops (pure noise).
    ///  - Allocations are measured via GC.GetAllocatedBytesForCurrentThread(),
    ///    which is exact and independent of CPU load.
    ///  - Latency is asserted on median/p95 over many samples, so a single GC
    ///    pause cannot fail the test; only a sustained regression does.
    ///  - Uses only EngineOptimizationPool (headless-safe, no Win32/SDL).
    /// </summary>
    public class PerformanceTests : IDisposable
    {
        private readonly EngineOptimizationPool _pool = EngineOptimizationPool.Instance;

        // Sink to prevent dead-code elimination of measured strings.
        private string? _sink;

        [Fact]
        public void StringPooling_ShouldReduceAllocations()
        {
            // Warm up JIT + first-touch so both paths run in steady state.
            for (int i = 0; i < 100; i++)
            {
                _sink = "READY" + i;
                _sink = _pool.GetString("READY");
            }

            // Non-pooled: a fresh string is allocated on every iteration.
            long beforeUnpooled = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                _sink = "READY" + i; // forces allocation each call
            }
            long unpooledAllocated = GC.GetAllocatedBytesForCurrentThread() - beforeUnpooled;

            // Pooled: returns the cached instance, no steady-state allocation.
            long beforePooled = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                _sink = _pool.GetString("READY");
            }
            long pooledAllocated = GC.GetAllocatedBytesForCurrentThread() - beforePooled;

            // The pool must allocate strictly less than the non-pooled path,
            // and ideally nothing at all in steady state.
            Assert.True(pooledAllocated < unpooledAllocated,
                $"Expected pooled ({pooledAllocated} B) to allocate less than unpooled ({unpooledAllocated} B).");
            Assert.True(pooledAllocated <= 64,
                $"Pooled path should be allocation-free in steady state, allocated {pooledAllocated} B.");
        }

        [Fact]
        public void MemoryLatency_ShouldBeUnderThreshold()
        {
            const int Warmup = 100;
            const int Samples = 1000;
            const double P95BudgetMs = 1.0; // generous per-call budget

            for (int i = 0; i < Warmup; i++)
            {
                _sink = _pool.GetString("READY");
            }

            var latenciesMs = new double[Samples];
            var sw = new Stopwatch();
            for (int i = 0; i < Samples; i++)
            {
                sw.Restart();
                _sink = _pool.GetString("READY");
                sw.Stop();
                latenciesMs[i] = sw.Elapsed.TotalMilliseconds;
            }

            Array.Sort(latenciesMs);
            double median = latenciesMs[Samples / 2];
            double p95 = latenciesMs[(int)(Samples * 0.95)];

            // A single slow sample is fine; only a sustained regression fails.
            Assert.True(median < P95BudgetMs,
                $"Median pool latency {median:0.###} ms exceeds budget {P95BudgetMs} ms.");
            Assert.True(p95 < P95BudgetMs,
                $"p95 pool latency {p95:0.###} ms exceeds budget {P95BudgetMs} ms.");
        }

        public void Dispose() => _sink = null;
    }
}
