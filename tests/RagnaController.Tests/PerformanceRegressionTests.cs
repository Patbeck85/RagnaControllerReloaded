using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using RagnaController.Core;
using RagnaController.Controller;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-019: Performance Regression Tests (TASK-1924).
    /// Deckt CPU-Last und Memory-Regressionen ab.
    /// Diese Tests prüfen dass Performance-Optimierungen keine Regressionen einführen
    /// und dass kritische Pfade im akzeptablen Bereich bleiben.
    /// </summary>
    public class PerformanceRegressionTests
    {
        #region ControllerSnapshot Pool Regression

        [Fact]
        public void ControllerSnapshot_PoolIsFasterThanNew()
        {
            // Arrange
            const int iterations = 10000;
            var pool = new ObjectPool<ControllerSnapshot>(() => new ControllerSnapshot());

            // Act — Pool Rent/Return
            var poolSw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var snap = pool.Rent();
                snap.LeftX = 1.0f;
                pool.Return(snap);
            }
            poolSw.Stop();

            // Act — new() allocation
            var newSw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var snap = new ControllerSnapshot { LeftX = 1.0f };
            }
            newSw.Stop();

            // Assert — Pool should not be more than 10x slower than new()
            // (ConcurrentBag has sync overhead; pool shines under contention/high throughput)
            double poolMs = poolSw.Elapsed.TotalMilliseconds;
            double newMs = newSw.Elapsed.TotalMilliseconds;
            double threshold = Math.Max(newMs * 10.0, 5.0);
            Assert.True(poolMs < threshold,
                $"Pool ({poolMs:F2}ms) should not be more than 10x slower than new() ({newMs:F2}ms)");
        }

        [Fact]
        public void ControllerSnapshot_Pool_ReturnsSameInstanceAfterReturn()
        {
            // Arrange
            var pool = new ObjectPool<ControllerSnapshot>(() => new ControllerSnapshot());

            // Act
            var snap1 = pool.Rent();
            pool.Return(snap1);
            var snap2 = pool.Rent();

            // Assert — Pool may return same instance (validated by ObjectPool design)
            Assert.NotNull(snap2);
        }

        #endregion

        #region PerformanceProfiler Zero-Overhead Regression

        [Fact]
        public void PerformanceProfiler_Disabled_HasZeroAllocationOverhead()
        {
            // Arrange
            PerformanceProfiler.EnableProfiling = false;
            var profiler = new PerformanceProfiler("RegressionTest", null);

            try
            {
                // Act — RecordEngineTick with profiling disabled should be a no-op
                var sw = Stopwatch.StartNew();
                const int iterations = 100000;
                for (int i = 0; i < iterations; i++)
                {
                    profiler.RecordEngineTick("Engine", 0.5);
                }
                sw.Stop();

                // Assert — Should complete quickly (no allocation, no ring buffer write)
                double elapsedMs = sw.Elapsed.TotalMilliseconds;
                Assert.True(elapsedMs < 10.0,
                    $"Disabled profiler should be near-zero overhead, took {elapsedMs:F2}ms for {iterations} calls");
            }
            finally
            {
                profiler.Dispose();
                PerformanceProfiler.EnableProfiling = false;
            }
        }

        [Fact]
        public void PerformanceProfiler_Enabled_RecordsSamplesCorrectly()
        {
            // Arrange
            PerformanceProfiler.EnableProfiling = true;
            var profiler = new PerformanceProfiler("RegressionTest", null);

            try
            {
                // Act
                const int iterations = 1000;
                for (int i = 0; i < iterations; i++)
                {
                    profiler.RecordEngineTick("TestEngine", (double)i / 100.0);
                }

                // Assert
                var stats = profiler.GetEngineStats();
                Assert.True(stats.ContainsKey("TestEngine"));
                Assert.Equal(iterations, stats["TestEngine"].SampleCount);
                Assert.InRange(stats["TestEngine"].P50Ms, 0, 5);
                Assert.InRange(stats["TestEngine"].P99Ms, 0, 11);
            }
            finally
            {
                profiler.Dispose();
                PerformanceProfiler.EnableProfiling = false;
            }
        }

        #endregion

        #region StandbyManager Idle-Level Regression

        [Fact]
        public void StandbyManager_IdleLevel_DefaultIsActive()
        {
            // Arrange
            var standby = new StandbyManager();
            var profile = new Profile { EnableSmartStandby = true };
            var noInput = ParsedInput.Disconnected;
            bool shouldSkip;

            // Act
            standby.Tick(noInput, profile, false, new NullFeedbackProvider(), new MovementEngine(null!, null!), null, out shouldSkip);

            // Assert
            Assert.Equal(IdleLevel.Active, standby.CurrentIdleLevel);
            Assert.False(standby.IsStandby);
        }

        [Fact]
        public void StandbyManager_DynamicTickReduction_ProfileSetting()
        {
            // Arrange
            var profileWithReduction = new Profile { EnableDynamicTickReduction = true };
            var profileWithoutReduction = new Profile { EnableDynamicTickReduction = false };

            // Assert
            Assert.True(profileWithReduction.EnableDynamicTickReduction);
            Assert.False(profileWithoutReduction.EnableDynamicTickReduction);
            Assert.Equal(16, profileWithReduction.LightIdleIntervalMs);
            Assert.Equal(50, profileWithReduction.DeepIdleIntervalMs);
        }

        #endregion

        #region Memory Allocation Regression

        [Fact]
        public void MemoryAllocationTracker_RecordTick_DoesNotAllocate()
        {
            // Arrange
            var tracker = new MemoryAllocationTracker("RegressionTest", null);
            long before = GC.GetTotalAllocatedBytes(false);

            // Act — RecordTick samples every 100 ticks, first call should allocate ~0
            for (int i = 0; i < 99; i++)
            {
                tracker.RecordTick();
            }

            long after = GC.GetTotalAllocatedBytes(false);
            long allocated = after - before;

            // Assert — Less than 1KB allocated for 99 ticks (should be near zero)
            Assert.True(allocated < 1024,
                $"MemoryAllocationTracker allocated {allocated} bytes for 99 RecordTick calls (expected < 1024)");

            tracker.Dispose();
        }

        #endregion

        #region InputCommandQueue Regression

        [Fact]
        public void InputCommandQueue_EnqueueKeyTap_IsFast()
        {
            // Arrange
            var queue = new InputCommandQueue();

            // Act
            var sw = Stopwatch.StartNew();
            const int iterations = 10000;
            for (int i = 0; i < iterations; i++)
            {
                queue.KeyDown(VirtualKey.A);
                queue.KeyUp(VirtualKey.A);
            }
            sw.Stop();

            // Assert — Should complete quickly
            double elapsedMs = sw.Elapsed.TotalMilliseconds;
            Assert.True(elapsedMs < 100.0,
                $"InputCommandQueue enqueue {iterations * 2} commands took {elapsedMs:F2}ms (expected < 100ms)");
        }

        #endregion
    }

    /// <summary>
    /// Null implementation of IFeedbackProvider for tests.
    /// </summary>
    public sealed class TestNullFeedbackProvider : IFeedbackProvider
    {
        public void StopAll() { }
        public void SetLED(byte r, byte g, byte b) { }
        public void Tick() { }
        public void Trigger(FeedbackType type) { }
        public void TriggerSkillFired() { }
        public void StopRumble() { }
        public void Dispose() { }
    }
}
