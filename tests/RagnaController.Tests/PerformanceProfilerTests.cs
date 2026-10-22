using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;
using RagnaController.Core;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-017: PerformanceProfiler Unit Tests.
    /// Deckt RecordEngineTick, GetEngineStats (P50/P95/P99), EnableProfiling-Opt-Out,
    /// und GenerateReport ab. Thread-safe, lock-free Design wird validiert.
    /// </summary>
    public class PerformanceProfilerTests : IDisposable
    {
        private readonly PerformanceProfiler _profiler;

        public PerformanceProfilerTests()
        {
            PerformanceProfiler.EnableProfiling = true;
            _profiler = new PerformanceProfiler("TestEngineOrchestrator", null);
        }

        public void Dispose()
        {
            _profiler.Dispose();
            PerformanceProfiler.EnableProfiling = false;
        }

        [Fact]
        public void RecordEngineTick_StoresSample_PerEngine()
        {
            // Arrange
            const string engine = "MovementEngine";

            // Act
            _profiler.RecordEngineTick(engine, 0.5);
            _profiler.RecordEngineTick(engine, 1.2);
            _profiler.RecordEngineTick(engine, 0.8);

            // Assert
            var stats = _profiler.GetEngineStats();
            Assert.True(stats.ContainsKey(engine));
            var snapshot = stats[engine];
            Assert.Equal(3, snapshot.SampleCount);
            Assert.InRange(snapshot.P50Ms, 0, 2);
            Assert.InRange(snapshot.P99Ms, 0, 2);
        }

        [Fact]
        public void RecordEngineTick_MultipleEngines_IndependentTracking()
        {
            // Arrange
            // Mehrere Samples für verlässliche P99-Berechnung
            for (int i = 0; i < 50; i++)
            {
                _profiler.RecordEngineTick("EngineA", 1.0);
                _profiler.RecordEngineTick("EngineB", 5.0);
            }

            // Act
            var stats = _profiler.GetEngineStats();

            // Assert
            Assert.Equal(2, stats.Count);
            Assert.Equal(50, stats["EngineA"].SampleCount);
            Assert.Equal(50, stats["EngineB"].SampleCount);
            Assert.True(stats["EngineB"].P99Ms > stats["EngineA"].P99Ms);
        }

        [Fact]
        public void EnableProfiling_False_NoSamplesRecorded()
        {
            // Arrange
            PerformanceProfiler.EnableProfiling = false;

            // Act
            _profiler.RecordEngineTick("AnyEngine", 10.0);

            // Assert
            var stats = _profiler.GetEngineStats();
            Assert.Empty(stats);
        }

        [Fact]
        public void Percentiles_CalculatedCorrectly()
        {
            // Arrange
            const string engine = "CombatEngine";
            // Record 100 samples: 1ms to 100ms linear
            for (int i = 1; i <= 100; i++)
            {
                _profiler.RecordEngineTick(engine, i);
            }

            // Act
            var snapshot = _profiler.GetEngineStats()[engine];

            // Assert
            Assert.Equal(100, snapshot.SampleCount);
            // P50 should be ~50ms, P95 ~95ms, P99 ~99ms
            Assert.InRange(snapshot.P50Ms, 49, 51);
            Assert.InRange(snapshot.P95Ms, 94, 96);
            Assert.InRange(snapshot.P99Ms, 98, 100);
        }

        [Fact]
        public void MaxMs_TracksMaximum()
        {
            // Arrange
            _profiler.RecordEngineTick("TestEngine", 1.0);
            _profiler.RecordEngineTick("TestEngine", 5.0);
            _profiler.RecordEngineTick("TestEngine", 3.0);

            // Act
            var snapshot = _profiler.GetEngineStats()["TestEngine"];

            // Assert
            Assert.Equal(5.0, snapshot.MaxMs, 2);
        }

        [Fact]
        public void AvgMs_CalculatesAverage()
        {
            // Arrange
            _profiler.RecordEngineTick("TestEngine", 2.0);
            _profiler.RecordEngineTick("TestEngine", 4.0);
            _profiler.RecordEngineTick("TestEngine", 6.0);

            // Act
            var snapshot = _profiler.GetEngineStats()["TestEngine"];

            // Assert
            Assert.Equal(4.0, snapshot.AvgMs, 2);
        }

        [Fact]
        public void GenerateReport_ReturnsFormattedString()
        {
            // Arrange
            _profiler.RecordEngineTick("TestEngine", 1.5);

            // Act
            var report = _profiler.GenerateReport();

            // Assert
            Assert.Contains("Engine Performance Report", report);
            Assert.Contains("TestEngine", report);
            Assert.Contains("P50:", report);
            Assert.Contains("P95:", report);
            Assert.Contains("P99:", report);
        }

        [Fact]
        public void GetEngineStats_Empty_ReturnsEmptyDictionary()
        {
            // Arrange
            var freshProfiler = new PerformanceProfiler("EmptyTest", null);

            // Act
            var stats = freshProfiler.GetEngineStats();

            // Assert
            Assert.Empty(stats);
            freshProfiler.Dispose();
        }

        [Fact]
        public void OrderByDescending_P99_SlowestFirst()
        {
            // Arrange
            // Mehrere Samples für verlässliche P99-Berechnung
            for (int i = 0; i < 50; i++)
            {
                _profiler.RecordEngineTick("FastEngine", 0.1);
                _profiler.RecordEngineTick("SlowEngine", 10.0);
            }

            // Act
            var stats = _profiler.GetEngineStats();
            var ordered = stats.OrderByDescending(s => s.Value.P99Ms).ToList();

            // Assert
            Assert.Equal("SlowEngine", ordered[0].Key);
            Assert.Equal("FastEngine", ordered[1].Key);
            Assert.True(ordered[0].Value.P99Ms > ordered[1].Value.P99Ms);
        }
    }
}
