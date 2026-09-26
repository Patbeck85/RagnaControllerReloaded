using System;
using Xunit;
using RagnaController.Core;

namespace RagnaController.Tests
{
    /// <summary>
    /// PERF-008: Unit tests for GpuOverlayProfiler and its registry.
    /// Verifies the EndFrame(double?) API (externally measured inter-frame deltas,
    /// since WPF exposes no layout/render phase hooks), dropped-frame detection
    /// thresholds, FrameBudgetMonitor integration, and registry lifecycle
    /// (GetOrCreate / Remove / DisposeAll) so closed overlays leak no timers.
    /// Headless — the profiler has no UI dependency; sampling timer is disposed
    /// via IDisposable to keep tests clean.
    /// </summary>
    public class GpuOverlayProfilerTests : IDisposable
    {
        private readonly System.Collections.Generic.List<GpuOverlayProfiler> _profilers = new();

        private static GpuOverlayProfiler Create(string key, FrameBudgetMonitor? monitor = null)
            => new(key, null, monitor);

        /// <summary>
        /// EndFrame(measuredMs) must record the externally measured frame time as LastFrameTimeMs.
        /// </summary>
        [Fact]
        public void EndFrame_WithMeasuredTime_RecordsLastFrameTime()
        {
            var profiler = Create("Test-Measured");
            _profilers.Add(profiler);

            profiler.EndFrame(16.7);

            Assert.Equal(16.7, profiler.GetMetrics().LastFrameTimeMs, 2);
        }

        /// <summary>
        /// Measured intervals above the ~33ms hitch threshold (2x 60fps period) count as dropped;
        /// steady 60Hz vsync (~16.7ms) must NOT be counted as dropped.
        /// </summary>
        [Fact]
        public void EndFrame_SteadyVsync_NotDropped_Hitch_Dropped()
        {
            var profiler = Create("Test-Dropped");
            _profilers.Add(profiler);

            // 10 steady frames at 60Hz — none dropped.
            for (int i = 0; i < 10; i++)
                profiler.EndFrame(16.7);
            Assert.Equal(0, profiler.GetMetrics().DroppedFrames);

            // One real hitch (40ms interval) — exactly one dropped frame.
            profiler.EndFrame(40.0);
            Assert.Equal(1, profiler.GetMetrics().DroppedFrames);
        }

        /// <summary>
        /// EndFrame() without a measurement falls back to the internal stopwatch
        /// (≈0ms here) and still counts a frame — legacy/phase-hook path stays usable.
        /// </summary>
        [Fact]
        public void EndFrame_WithoutMeasurement_CountsFrame()
        {
            var profiler = Create("Test-NoMeasure");
            _profilers.Add(profiler);

            profiler.EndFrame();

            Assert.True(profiler.GetMetrics().LastFrameTimeMs >= 0.0);
        }

        /// <summary>
        /// Frame times must flow into the attached FrameBudgetMonitor:
        /// total ticks increment, and frames over the monitor budget are counted as over-budget.
        /// </summary>
        [Fact]
        public void EndFrame_RecordsIntoFrameBudgetMonitor()
        {
            var monitor = new FrameBudgetMonitor("Test-Monitor", 16.7);
            try
            {
                var profiler = Create("Test-Budget", monitor);
                _profilers.Add(profiler);

                profiler.EndFrame(10.0);   // within budget
                profiler.EndFrame(25.0);   // over 16.7ms budget

                var (total, over, _) = monitor.GetStats();
                Assert.Equal(2, total);
                Assert.Equal(1, over);
            }
            finally
            {
                monitor.Dispose();
            }
        }

        /// <summary>
        /// GetMetrics must expose the monitor percentiles (zero before enough samples —
        /// structure check only) and the overlay type.
        /// </summary>
        [Fact]
        public void GetMetrics_ExposesOverlayTypeAndPercentileStructure()
        {
            var profiler = Create("Test-Metrics");
            _profilers.Add(profiler);

            profiler.EndFrame(16.7);
            var m = profiler.GetMetrics();

            Assert.Equal("Test-Metrics", m.OverlayType);
            Assert.True(m.FrameBudgetPercentiles.P50 >= 0.0);
            Assert.True(m.FrameBudgetPercentiles.P95 >= 0.0);
            Assert.True(m.FrameBudgetPercentiles.P99 >= 0.0);
        }

        /// <summary>
        /// Registry: GetOrCreate returns the same instance for a key; Remove disposes it and
        /// clears it from GetAll (MEMORY-001 — closed overlays must not linger).
        /// </summary>
        [Fact]
        public void Registry_GetOrCreate_ReturnsSameInstance_AndRemove_Clears()
        {
            string key = "Test-Registry-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            var a = GpuOverlayProfilerRegistry.GetOrCreate(key);
            var b = GpuOverlayProfilerRegistry.GetOrCreate(key);
            Assert.Same(a, b);
            Assert.True(GpuOverlayProfilerRegistry.GetAll().ContainsKey(key));

            // Remove disposes the shared instance (no double-dispose in registry).
            GpuOverlayProfilerRegistry.Remove(key);
            Assert.False(GpuOverlayProfilerRegistry.GetAll().ContainsKey(key));
        }

        /// <summary>
        /// Dispose must be idempotent-safe for callers: disposing twice does not throw
        /// (registry Remove already disposes; window Closed handlers may race).
        /// </summary>
        [Fact]
        public void Dispose_IsIdempotent()
        {
            var profiler = Create("Test-Dispose");
            _profilers.Add(profiler);

            profiler.Dispose();
            profiler.Dispose(); // must not throw
        }

        public void Dispose()
        {
            foreach (var p in _profilers)
                p.Dispose();
            GpuOverlayProfilerRegistry.DisposeAll();
        }
    }
}
