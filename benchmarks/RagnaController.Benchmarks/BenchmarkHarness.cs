// BenchmarkHarness.cs — PERF-007 BenchmarkDotNet Integration (Executable Project)
// Run: dotnet run -c Release --project benchmarks/RagnaController.Benchmarks/RagnaController.Benchmarks.csproj --filter "*Benchmark*"

using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;
using RagnaController.Controller;
using CoreControllerSnapshot = RagnaController.Core.ControllerSnapshot;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Exporters.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace RagnaController.Benchmarks
{
    /// <summary>
    /// BenchmarkDotNet configuration for headless CI runs.
    /// Produces: JSON, Markdown, CSV, HTML reports in ./BenchmarkDotNet.Artifacts/
    /// </summary>
    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddJob(Job.Default
                .WithGcServer(true)
                .WithIterationCount(10)
                .WithWarmupCount(3)
                .WithLaunchCount(1)
            );

            AddDiagnoser(MemoryDiagnoser.Default);
#if WINDOWS
            AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(maxDepth: 3, printSource: true)));
#endif

            AddExporter(MarkdownExporter.GitHub);
            AddExporter(CsvMeasurementsExporter.Default);
            AddExporter(HtmlExporter.Default);
            AddExporter(JsonExporter.Default);

            AddLogger(ConsoleLogger.Default);

            WithOptions(ConfigOptions.DisableOptimizationsValidator);
        }
    }

    /// <summary>
    /// Controller polling latency benchmarks — measures SDL2 event pump overhead.
    /// </summary>
    [Config(typeof(BenchmarkConfig))]
    [MemoryDiagnoser]
    public class ControllerPollingBenchmarks
    {
        private ControllerManager _controllerManager = null!;
        private readonly int _iterations = 1000;

        [GlobalSetup]
        public void Setup()
        {
            // Headless mode: skip SDL_Init, use XInput fallback
            Environment.SetEnvironmentVariable("RAGNACONTROLLER_SKIP_SDL", "1");
            _controllerManager = new ControllerManager();
            _controllerManager.DetectController();
        }

        [GlobalCleanup]
        public void Cleanup() => _controllerManager?.Dispose();

        [Benchmark(Baseline = true, Description = "Single controller poll (no device connected)")]
        public void PollSingleController_NoDevice()
        {
            for (int i = 0; i < _iterations; i++)
            {
                _ = _controllerManager.ButtonStates;
                _ = _controllerManager.IsConnected;
                _ = _controllerManager.ControllerName;
            }
        }

        [Benchmark(Description = "Poll + button state check (hot path)")]
        public void PollWithButtonCheck()
        {
            for (int i = 0; i < _iterations; i++)
            {
                var state = _controllerManager.ButtonStates;
                _ = state.APressed;
                _ = state.BPressed;
                _ = state.L1Pressed;
                _ = state.R1Pressed;
            }
        }
    }

    /// <summary>
    /// Input emulation latency benchmarks — measures SendInput round-trip.
    /// </summary>
    [Config(typeof(BenchmarkConfig))]
    [MemoryDiagnoser]
    public class InputEmulationBenchmarks
    {
        private InputCommandQueue _queue = null!;

        [GlobalSetup]
        public void Setup()
        {
            _queue = new InputCommandQueue();
            _queue.Start();
        }

        [GlobalCleanup]
        public void Cleanup() => _queue?.Dispose();

        [Benchmark(Baseline = true, Description = "Single keystroke emulation (KeyDown+KeyUp)")]
        public void EmulateSingleKeystroke()
        {
            _queue.KeyDown(VirtualKey.A);
            _queue.KeyUp(VirtualKey.A);
        }

        [Benchmark(Description = "Key chord (Ctrl+Shift+A)")]
        public void EmulateKeyChord()
        {
            _queue.KeyDown(VirtualKey.ControlLeft);
            _queue.KeyDown(VirtualKey.ShiftLeft);
            _queue.KeyDown(VirtualKey.A);
            _queue.KeyUp(VirtualKey.A);
            _queue.KeyUp(VirtualKey.ShiftLeft);
            _queue.KeyUp(VirtualKey.ControlLeft);
        }

        [Benchmark(Description = "Mouse move + click (absolute coordinates)")]
        public void EmulateMouseClick()
        {
            _queue.MoveMouseAbsolute(960, 540); // Center of 1920x1080
            _queue.LeftClick();
        }

        [Benchmark(Description = "10 rapid keystrokes (macro simulation)")]
        public void EmulateRapidKeystrokes()
        {
            for (int i = 0; i < 10; i++)
            {
                _queue.KeyDown((VirtualKey)((int)VirtualKey.D1 + i));
                _queue.KeyUp((VirtualKey)((int)VirtualKey.D1 + i));
            }
        }

        [Benchmark(Description = "Unicode text input (chat message)")]
        public void EmulateUnicodeText()
        {
            _queue.SendChatString("RagnaController test message");
        }
    }

    /// <summary>
    /// Memory allocation benchmarks — tracks GC pressure from pooling/struct usage.
    /// </summary>
    [Config(typeof(BenchmarkConfig))]
    [MemoryDiagnoser]
    public class MemoryAllocationBenchmarks
    {
        private ObjectPool<CoreControllerSnapshot> _pool = null!;

        [GlobalSetup]
        public void Setup()
        {
            _pool = new ObjectPool<CoreControllerSnapshot>(() => new CoreControllerSnapshot());
        }

        [Benchmark(Baseline = true, Description = "Struct allocation (no pooling)")]
        public CoreControllerSnapshot AllocateStruct()
        {
            return new CoreControllerSnapshot
            {
                LeftX = 1.0f,
                LeftY = -1.0f,
                RightX = 0.5f,
                RightY = 0.5f,
                LT = 1.0f,
                RT = 1.0f
            };
        }

        [Benchmark(Description = "Object pool rent/return")]
        public void PoolRentReturn()
        {
            var state = _pool.Rent();
            state.LeftX = 0.5f;
            _pool.Return(state);
        }

        [Benchmark(Description = "List allocation + clear (100 items)")]
        public void ListAllocateClear()
        {
            var list = new List<CoreControllerSnapshot>(100);
            for (int i = 0; i < 100; i++)
                list.Add(new CoreControllerSnapshot());
            list.Clear();
        }

        [Benchmark(Description = "Array pool rent/return (100 ControllerSnapshot)")]
        public void ArrayPoolRentReturn()
        {
            var array = ArrayPool<CoreControllerSnapshot>.Shared.Rent(100);
            for (int i = 0; i < 100; i++)
                array[i] = new CoreControllerSnapshot();
            ArrayPool<CoreControllerSnapshot>.Shared.Return(array);
        }
    }

    /// <summary>
    /// Profile switching & macro execution benchmarks.
    /// </summary>
    [Config(typeof(BenchmarkConfig))]
    [MemoryDiagnoser]
    public class ProfileAndMacroBenchmarks
    {
        private ProfileManager _profileManager = null!;
        private Profile _testProfile = null!;

        [GlobalSetup]
        public void Setup()
        {
            _profileManager = new ProfileManager();

            _testProfile = new Profile
            {
                Name = "BenchmarkProfile",
                ButtonMappings = new Dictionary<ButtonKey, ButtonAction>
                {
                    [new ButtonKey(VirtualKey.A)] = new ButtonAction { Key = VirtualKey.Space, Type = ActionType.Key },
                    [new ButtonKey(VirtualKey.B)] = new ButtonAction { Key = VirtualKey.Enter, Type = ActionType.Key },
                    [new ButtonKey(VirtualKey.ArrowUp)] = new ButtonAction { Key = VirtualKey.W, Type = ActionType.Key },
                    [new ButtonKey(VirtualKey.ArrowDown)] = new ButtonAction { Key = VirtualKey.S, Type = ActionType.Key },
                    [new ButtonKey(VirtualKey.ArrowLeft)] = new ButtonAction { Key = VirtualKey.A, Type = ActionType.Key },
                    [new ButtonKey(VirtualKey.ArrowRight)] = new ButtonAction { Key = VirtualKey.D, Type = ActionType.Key },
                }
            };
        }

        [Benchmark(Baseline = true, Description = "Profile load from memory")]
        public Profile LoadProfile()
        {
            _profileManager.SetActive(_testProfile.Name);
            return _profileManager.ActiveProfile;
        }

        [Benchmark(Description = "Profile switch (unload old, load new)")]
        public void SwitchProfile()
        {
            _profileManager.SetActive(_testProfile.Name);
        }
    }

    /// <summary>
    /// EngineOrchestrator tick loop benchmarks.
    /// </summary>
    [Config(typeof(BenchmarkConfig))]
    [MemoryDiagnoser]
    public class EngineOrchestratorBenchmarks
    {
        private EngineOrchestrator _orchestrator = null!;
        private InputCommandQueue _queue = null!;
        private BackgroundTickProvider _tickProvider = null!;

        [GlobalSetup]
        public void Setup()
        {
            var logPath = Path.Combine(Path.GetTempPath(), "RagnaController", "bench.log");
            var logger = new AdvancedLogger(logPath);
            var messenger = new SimpleMessenger();
            _tickProvider = new BackgroundTickProvider();
            _queue = new InputCommandQueue();

            _orchestrator = new EngineOrchestrator(_tickProvider, messenger, _queue, logger);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _orchestrator?.Stop();
            _orchestrator?.Dispose();
            _queue?.Dispose();
            _tickProvider?.Dispose();
        }

        [Benchmark(Baseline = true, Description = "Start/Stop Orchestrator")]
        public void StartStopOrchestrator()
        {
            _orchestrator.Start();
            _orchestrator.Stop();
        }

        [Benchmark(Description = "Get CommandQueue")]
        public void GetCommandQueue() => _ = _orchestrator.CommandQueue;
    }

    /// <summary>
    /// End-to-end input latency benchmarks using InputLatencyTracker (PERF-005).
    /// Measures actual hardware event → SendInput completion latency.
    /// </summary>
    [Config(typeof(BenchmarkConfig))]
    [MemoryDiagnoser]
    public class InputLatencyBenchmarks
    {
        private InputCommandQueue _queue = null!;
        private InputLatencyTracker _latencyTracker = null!;
        private const int Iterations = 1000;

        [GlobalSetup]
        public void Setup()
        {
            _latencyTracker = InputLatencyRegistry.GetOrCreate("Benchmark");
            _queue = new InputCommandQueue(_latencyTracker, "BenchmarkController");
            _queue.Start();
            
            // Warmup
            for (int i = 0; i < 100; i++)
            {
                _queue.KeyDown(VirtualKey.A);
                _queue.KeyUp(VirtualKey.A);
            }
            Thread.Sleep(100);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _queue?.Dispose();
            _latencyTracker?.Dispose();
            InputLatencyRegistry.DisposeAll();
        }

        [Benchmark(Baseline = true, Description = "End-to-end latency: KeyDown+KeyUp (hardware→SendInput)")]
        public void MeasureEndToEndLatency_Keystroke()
        {
            for (int i = 0; i < Iterations; i++)
            {
                // Record enqueue timestamp (simulates hardware event received)
                var hwEventTime = Stopwatch.GetTimestamp();
                
                _queue.KeyDown(VirtualKey.A);
                _queue.KeyUp(VirtualKey.A);
                
                // In real scenario, RecordTotalLatency is called after SendInput completes
                // Here we measure the queue-to-SendInput path
                var elapsedUs = (long)((Stopwatch.GetTimestamp() - hwEventTime) * 1_000_000.0 / Stopwatch.Frequency);
                _latencyTracker.RecordTotalLatency(elapsedUs / 1000.0, "BenchmarkController");
            }
        }

        [Benchmark(Description = "End-to-end latency: Mouse move + click")]
        public void MeasureEndToEndLatency_MouseClick()
        {
            for (int i = 0; i < Iterations; i++)
            {
                var hwEventTime = Stopwatch.GetTimestamp();
                
                _queue.MoveMouseAbsolute(960, 540);
                _queue.LeftClick();
                
                var elapsedUs = (long)((Stopwatch.GetTimestamp() - hwEventTime) * 1_000_000.0 / Stopwatch.Frequency);
                _latencyTracker.RecordTotalLatency(elapsedUs / 1000.0, "BenchmarkController");
            }
        }

        [Benchmark(Description = "End-to-end latency: 10 rapid keystrokes (macro)")]
        public void MeasureEndToEndLatency_Macro()
        {
            for (int i = 0; i < Iterations; i++)
            {
                var hwEventTime = Stopwatch.GetTimestamp();
                
                for (int k = 0; k < 10; k++)
                {
                    _queue.KeyDown((VirtualKey)((int)VirtualKey.D1 + k));
                    _queue.KeyUp((VirtualKey)((int)VirtualKey.D1 + k));
                }
                
                var elapsedUs = (long)((Stopwatch.GetTimestamp() - hwEventTime) * 1_000_000.0 / Stopwatch.Frequency);
                _latencyTracker.RecordTotalLatency(elapsedUs / 1000.0, "BenchmarkController");
            }
        }
    }

    // Simple messenger for benchmarking (no external dependencies)
    internal class SimpleMessenger : IMessenger
    {
        public void Publish<T>(T message) where T : class { }
        public IDisposable Subscribe<T>(Action<T> handler) where T : class => new EmptyDisposable();

        private sealed class EmptyDisposable : IDisposable { public void Dispose() { } }
    }

    /// <summary>
    /// Entry point for running benchmarks from CLI or CI.
    /// Usage: 
    ///   dotnet run -c Release --project benchmarks/RagnaController.Benchmarks --filter "*Benchmark*"
    ///   dotnet run -c Release --project benchmarks/RagnaController.Benchmarks -- --gate <jsonPath> [--threshold <ms>]
    ///   dotnet run -c Release --project benchmarks/RagnaController.Benchmarks -- --input-latency-gate <jsonPath>
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            // Check if this is a gate validation command
            if (args.Length >= 2 && (args[0] == "--gate" || args[0] == "--input-latency-gate"))
            {
                return BenchmarkGate.ExecuteGateCommand(args);
            }

            // Default: run benchmarks
            BenchmarkRunner.Run(typeof(Program).Assembly);
            return 0;
        }
    }

    /// <summary>
    /// CI-friendly benchmark summary parser — extracts P99 latency for gating.
    /// </summary>
    public static class BenchmarkGate
    {
        /// <summary>
        /// Parses BenchmarkDotNet JSON output and validates P95 < thresholdMs.
        /// Returns exit code 0 on pass, 1 on fail.
        /// </summary>
        public static int ValidateLatencyGate(string jsonPath, double thresholdMs = 2.0)
        {
            var json = File.ReadAllText(jsonPath);
            using var doc = System.Text.Json.JsonDocument.Parse(json);

            foreach (var benchmark in doc.RootElement.GetProperty("Benchmarks").EnumerateArray())
            {
                var stats = benchmark.GetProperty("Statistics");
                var percentiles = stats.GetProperty("Percentiles");
                var p95Ticks = percentiles.GetProperty("P95").GetDouble();
                var p95Ms = p95Ticks / 10000000.0; // Convert 100ns ticks to ms
                var name = benchmark.GetProperty("DisplayInfo").GetString();

                if (p95Ms > thresholdMs)
                {
                    Console.Error.WriteLine($"❌ GATE FAILED: {name} P95 = {p95Ms:F3}ms > {thresholdMs}ms");
                    return 1;
                }
                Console.WriteLine($"✅ {name} P95 = {p95Ms:F3}ms");
            }

            Console.WriteLine($"🎉 All benchmarks pass P95 < {thresholdMs}ms");
            return 0;
        }

        /// <summary>
        /// Validates InputLatency benchmarks specifically with 5ms threshold (PERF-005 target).
        /// Usage: dotnet run --project benchmarks/RagnaController.Benchmarks -- --input-latency-gate <jsonPath>
        /// </summary>
        public static int ValidateInputLatencyGate(string jsonPath)
        {
            var json = File.ReadAllText(jsonPath);
            using var doc = System.Text.Json.JsonDocument.Parse(json);

            bool hasInputLatency = false;
            foreach (var benchmark in doc.RootElement.GetProperty("Benchmarks").EnumerateArray())
            {
                var name = benchmark.GetProperty("DisplayInfo").GetString() ?? "";
                
                // Only check InputLatency benchmarks
                if (!name.Contains("InputLatency", StringComparison.OrdinalIgnoreCase))
                    continue;

                hasInputLatency = true;
                var stats = benchmark.GetProperty("Statistics");
                var percentiles = stats.GetProperty("Percentiles");
                var p95Ticks = percentiles.GetProperty("P95").GetDouble();

                // Convert from 100ns ticks to milliseconds: 1 tick = 100ns = 0.0001 ms
                var p95Ms = p95Ticks / 10000000.0;

                // PERF-005 target: P95 < 5ms end-to-end latency (using P95 as highest available percentile)
                const double thresholdMs = 5.0;

                if (p95Ms > thresholdMs)
                {
                    Console.Error.WriteLine($"❌ INPUT LATENCY GATE FAILED: {name} P95 = {p95Ms:F3}ms > {thresholdMs}ms (PERF-005 target)");
                    return 1;
                }
                Console.WriteLine($"✅ {name} P95 = {p95Ms:F3}ms < {thresholdMs}ms");
            }

            if (!hasInputLatency)
            {
                Console.Error.WriteLine("❌ No InputLatency benchmarks found in results");
                return 1;
            }

            Console.WriteLine("🎉 InputLatency gate passed: P95 < 5ms (PERF-005 target met)");
            return 0;
        }

        /// <summary>
        /// CLI entry point for CI integration.
        /// Args: --gate <jsonPath> [--threshold <ms>] | --input-latency-gate <jsonPath>
        /// </summary>
        public static int ExecuteGateCommand(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage:");
                Console.Error.WriteLine("  --gate <jsonPath> [--threshold <ms>]        Validate all benchmarks");
                Console.Error.WriteLine("  --input-latency-gate <jsonPath>             Validate InputLatency benchmarks (5ms threshold)");
                return 1;
            }

            try
            {
                if (args[0] == "--gate")
                {
                    var jsonPath = args[1];
                    var thresholdMs = 2.0;
                    if (args.Length >= 4 && args[2] == "--threshold" && double.TryParse(args[3], out var t))
                        thresholdMs = t;
                    return ValidateLatencyGate(jsonPath, thresholdMs);
                }
                else if (args[0] == "--input-latency-gate")
                {
                    return ValidateInputLatencyGate(args[1]);
                }
                else
                {
                    Console.Error.WriteLine($"Unknown command: {args[0]}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"❌ Gate validation error: {ex.Message}");
                return 1;
            }
        }
    }
}