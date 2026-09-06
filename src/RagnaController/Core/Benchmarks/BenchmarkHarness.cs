// BenchmarkHarness.cs — PERF-007 BenchmarkDotNet Integration (Library - no entry point)
// Place in: src/RagnaController.Core/Benchmarks/
// Run via separate console app or: dotnet run -c Release --project RagnaController.Benchmarks

using System;
using System.Collections.Generic;
using System.IO;
using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Columns;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Core.Benchmarks
{
    [Config(typeof(BenchmarkConfig))]
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, baseline: true)]
    public class MemoryAllocationBenchmarks
    {
        private ObjectPool<ControllerSnapshot> _pool = null!;

        [GlobalSetup]
        public void Setup() => _pool = new ObjectPool<ControllerSnapshot>(() => new ControllerSnapshot());

        [Benchmark(Baseline = true, Description = "Struct allocation (no pooling)")]
        public ControllerSnapshot AllocateStruct()
        {
            return new ControllerSnapshot
            {
                LeftX = 1.0f,
                LeftY = -1.0f,
                RightX = 0.5f,
                RightY = 0.5f,
                LT = 1.0f,
                RT = 1.0f
            };
        }

        [Benchmark(Description = "ObjectPool rent/return")]
        public ControllerSnapshot PoolRentReturn()
        {
            var snap = _pool.Rent();
            snap.LeftX = 1.0f;
            _pool.Return(snap);
            return snap;
        }

        [Benchmark(Description = "List allocation + clear (100 items)")]
        public void ListAllocateClear()
        {
            var list = new List<ControllerSnapshot>(100);
            for (int i = 0; i < 100; i++)
                list.Add(new ControllerSnapshot());
            list.Clear();
        }

        [Benchmark(Description = "Array pool rent/return (100 ControllerSnapshot)")]
        public void ArrayPoolRentReturn()
        {
            var array = ArrayPool<ControllerSnapshot>.Shared.Rent(100);
            for (int i = 0; i < 100; i++)
                array[i] = new ControllerSnapshot();
            ArrayPool<ControllerSnapshot>.Shared.Return(array);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, baseline: true)]
    public class InputCommandQueueBenchmarks
    {
        private InputCommandQueue _queue = null!;

        [GlobalSetup]
        public void Setup() => _queue = new InputCommandQueue();

        [GlobalCleanup]
        public void Cleanup() => _queue?.Dispose();

        [Benchmark(Description = "Enqueue KeyDown/Up (VirtualKey.A)")]
        public void EnqueueKeyTap()
        {
            _queue.KeyDown(VirtualKey.A);
            _queue.KeyUp(VirtualKey.A);
        }

        [Benchmark(Description = "Enqueue MouseMoveRelative")]
        public void EnqueueMouseMove()
        {
            _queue.MoveMouseRelative(10, 5);
        }

        [Benchmark(Description = "Enqueue MouseAbs + Click")]
        public void EnqueueMouseAbsClick()
        {
            _queue.MoveMouseAbsolute(100, 100);
            _queue.LeftClick();
        }

        [Benchmark(Description = "Enqueue KeyDown + Wait + KeyUp (TapKey)")]
        public void EnqueueTapKey()
        {
            _queue.TapKey(VirtualKey.A);
        }

        [Benchmark(Description = "Enqueue 10 commands batch")]
        public void EnqueueBatch10()
        {
            for (int i = 0; i < 10; i++)
            {
                _queue.KeyDown(VirtualKey.A);
                _queue.KeyUp(VirtualKey.A);
            }
        }
    }

    [Config(typeof(BenchmarkConfig))]
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, baseline: true)]
    public class ProfileManagerBenchmarks
    {
        private ProfileManager _profileManager = null!;

        [GlobalSetup]
        public void Setup() => _profileManager = new ProfileManager();

        [Benchmark(Description = "GetAllProfiles (empty)")]
        public void GetAllProfiles() => _ = _profileManager.Profiles;

        [Benchmark(Description = "GetActiveProfile")]
        public void GetActiveProfile() => _ = _profileManager.ActiveProfileName;
    }

    [Config(typeof(BenchmarkConfig))]
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80, baseline: true)]
    public class EngineOrchestratorBenchmarks
    {
        private EngineOrchestrator _orchestrator = null!;
        private InputCommandQueue _queue = null!;

        [GlobalSetup]
        public void Setup()
        {
            var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RagnaController", "bench.log");
            var logger = new AdvancedLogger(logPath);
            var messenger = new SimpleMessenger();
            var tickProvider = new BackgroundTickProvider();
            _queue = new InputCommandQueue();

            _orchestrator = new EngineOrchestrator(tickProvider, messenger, _queue, logger);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _orchestrator?.Stop();
            _orchestrator?.Dispose();
            _queue?.Dispose();
        }

        [Benchmark(Description = "Start/Stop Orchestrator")]
        public void StartStopOrchestrator()
        {
            _orchestrator.Start();
            _orchestrator.Stop();
        }

        [Benchmark(Description = "Get CommandQueue")]
        public void GetCommandQueue() => _ = _orchestrator.CommandQueue;
    }

    // Simple messenger for benchmarking (no external dependencies)
    internal class SimpleMessenger : IMessenger
    {
        public void Publish<T>(T message) where T : class { }
        public IDisposable Subscribe<T>(Action<T> handler) where T : class => new EmptyDisposable();
        
        private sealed class EmptyDisposable : IDisposable { public void Dispose() { } }
    }

    // Configuration shared by all benchmarks
    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddLogger(ConsoleLogger.Default);
            AddDiagnoser(MemoryDiagnoser.Default);

            AddJob(Job.Default
                .WithRuntime(CoreRuntime.Core80)
                .WithIterationCount(5)
                .WithWarmupCount(3)
                .WithLaunchCount(1));
        }
    }
}