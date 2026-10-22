using BenchmarkDotNet.Running;
using RagnaController.Core.Benchmarks;

namespace RagnaController.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        // Run all performance benchmarks for regression detection
        _ = BenchmarkRunner.Run<MemoryAllocationBenchmarks>();
        _ = BenchmarkRunner.Run<InputCommandQueueBenchmarks>();
        _ = BenchmarkRunner.Run<ProfileManagerBenchmarks>();
        _ = BenchmarkRunner.Run<EngineOrchestratorBenchmarks>();
        _ = BenchmarkRunner.Run<ControllerSnapshotPoolBenchmarks>();
        _ = BenchmarkRunner.Run<StandbyManagerBenchmarks>();
        _ = BenchmarkRunner.Run<PerformanceProfilerBenchmarks>();
    }
}