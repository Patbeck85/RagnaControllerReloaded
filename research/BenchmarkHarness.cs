// BenchmarkHarness.cs — PERF-007 BenchmarkDotNet Integration
// Place in: src/RagnaController.Core/Benchmarks/
// Run: dotnet run -c Release --project RagnaController.Core --filter "*Benchmark*"

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using System.Runtime.CompilerServices;

namespace RagnaController.Benchmarks;

/// <summary>
/// BenchmarkDotNet configuration for headless CI runs.
/// Produces: JSON, Markdown, CSV, HTML reports in ./BenchmarkDotNet.Artifacts/
/// </summary>
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core80)
            .WithGcServer(true)
            .WithIterationCount(10)
            .WithWarmupCount(3)
            .WithLaunchCount(1)
        );
        
        AddDiagnoser(MemoryDiagnoser.Default);
        AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(maxDepth: 3, printSource: true)));
        
        AddExporter(MarkdownExporter.GitHub);
        AddExporter(CsvExporter.Default);
        AddExporter(HtmlExporter.Default);
        AddExporter(JsonExporter.Full);
        
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
    private ControllerService _controllerService = null!;
    private readonly int _iterations = 1000;

    [GlobalSetup]
    public void Setup()
    {
        // Headless mode: skip SDL_Init, use mock
        Environment.SetEnvironmentVariable("RAGNACONTROLLER_SKIP_SDL", "1");
        _controllerService = new ControllerService();
        _controllerService.Initialize();
    }

    [GlobalCleanup]
    public void Cleanup() => _controllerService?.Dispose();

    [Benchmark(Baseline = true, Description = "Single controller poll (no device connected)")]
    public void PollSingleController_NoDevice()
    {
        for (int i = 0; i < _iterations; i++)
        {
            _ = _controllerService.GetState(0);
        }
    }

    [Benchmark(Description = "Poll 4 controllers simultaneously")]
    public void PollFourControllers()
    {
        for (int i = 0; i < _iterations; i++)
        {
            for (int c = 0; c < 4; c++)
            {
                _ = _controllerService.GetState(c);
            }
        }
    }

    [Benchmark(Description = "Poll + button state check (hot path)")]
    public void PollWithButtonCheck()
    {
        for (int i = 0; i < _iterations; i++)
        {
            var state = _controllerService.GetState(0);
            _ = state.IsButtonPressed(ControllerButton.A);
            _ = state.IsButtonPressed(ControllerButton.B);
            _ = state.LeftStickX;
            _ = state.LeftStickY;
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
    private InputEmulationService _emulation = null!;

    [GlobalSetup]
    public void Setup()
    {
        _emulation = new InputEmulationService();
        _emulation.Initialize();
    }

    [GlobalCleanup]
    public void Cleanup() => _emulation?.Dispose();

    [Benchmark(Baseline = true, Description = "Single keystroke emulation (SendInput)")]
    public void EmulateSingleKeystroke()
    {
        _emulation.SendKey(WindowsInput.Native.VirtualKeyCode.VK_A);
    }

    [Benchmark(Description = "Key chord (Ctrl+Shift+A)")]
    public void EmulateKeyChord()
    {
        _emulation.SendKey(WindowsInput.Native.VirtualKeyCode.CONTROL);
        _emulation.SendKey(WindowsInput.Native.VirtualKeyCode.SHIFT);
        _emulation.SendKey(WindowsInput.Native.VirtualKeyCode.VK_A);
        _emulation.ReleaseKey(WindowsInput.Native.VirtualKeyCode.VK_A);
        _emulation.ReleaseKey(WindowsInput.Native.VirtualKeyCode.SHIFT);
        _emulation.ReleaseKey(WindowsInput.Native.VirtualKeyCode.CONTROL);
    }

    [Benchmark(Description = "Mouse move + click (absolute coordinates)")]
    public void EmulateMouseClick()
    {
        _emulation.MoveMouseAbsolute(960, 540); // Center of 1920x1080
        _emulation.ClickMouseButton(WindowsInput.Native.MouseButton.Left);
    }

    [Benchmark(Description = "10 rapid keystrokes (macro simulation)")]
    public void EmulateRapidKeystrokes()
    {
        for (int i = 0; i < 10; i++)
        {
            _emulation.SendKey(WindowsInput.Native.VirtualKeyCode.VK_1 + i);
        }
    }

    [Benchmark(Description = "Unicode text input (chat message)")]
    public void EmulateUnicodeText()
    {
        _emulation.SendText("RagnaController test message", delayMs: 0);
    }
}

/// <summary>
/// Memory allocation benchmarks — tracks GC pressure from pooling/struct usage.
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class MemoryAllocationBenchmarks
{
    private ObjectPool<ControllerState> _pool = null!;

    [GlobalSetup]
    public void Setup()
    {
        _pool = new ObjectPool<ControllerState>(() => new ControllerState(), 100);
    }

    [Benchmark(Baseline = true, Description = "Struct allocation (no pooling)")]
    public ControllerState AllocateStruct()
    {
        return new ControllerState
        {
            Buttons = 0xFFFF,
            LeftStickX = 1.0f,
            LeftStickY = -1.0f,
            RightStickX = 0.5f,
            RightStickY = 0.5f,
            LeftTrigger = 1.0f,
            RightTrigger = 1.0f
        };
    }

    [Benchmark(Description = "Object pool rent/return")]
    public void PoolRentReturn()
    {
        var state = _pool.Rent();
        state.Buttons = 0x1234;
        _pool.Return(state);
    }

    [Benchmark(Description = "List allocation + clear (100 items)")]
    public void ListAllocateClear()
    {
        var list = new List<ControllerState>(100);
        for (int i = 0; i < 100; i++)
            list.Add(new ControllerState());
        list.Clear();
    }

    [Benchmark(Description = "Array pool rent/return (100 ControllerState)")]
    public void ArrayPoolRentReturn()
    {
        var array = ArrayPool<ControllerState>.Shared.Rent(100);
        for (int i = 0; i < 100; i++)
            array[i] = new ControllerState();
        ArrayPool<ControllerState>.Shared.Return(array);
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
    private MacroEngine _macroEngine = null!;
    private Profile _testProfile = null!;

    [GlobalSetup]
    public void Setup()
    {
        _profileManager = new ProfileManager();
        _macroEngine = new MacroEngine();
        
        _testProfile = new Profile
        {
            Name = "BenchmarkProfile",
            Mappings = new Dictionary<string, ActionBinding>
            {
                ["A"] = new ActionBinding { Key = "Space", Priority = 1 },
                ["B"] = new ActionBinding { Key = "Enter", Priority = 1 },
                ["LeftStickUp"] = new ActionBinding { Key = "W", Priority = 1 },
                ["LeftStickDown"] = new ActionBinding { Key = "S", Priority = 1 },
                ["LeftStickLeft"] = new ActionBinding { Key = "A", Priority = 1 },
                ["LeftStickRight"] = new ActionBinding { Key = "D", Priority = 1 },
            }
        };
    }

    [Benchmark(Baseline = true, Description = "Profile load from memory")]
    public Profile LoadProfile()
    {
        return _profileManager.LoadProfile(_testProfile);
    }

    [Benchmark(Description = "Profile switch (unload old, load new)")]
    public void SwitchProfile()
    {
        _profileManager.SetActiveProfile(_testProfile);
    }

    [Benchmark(Description = "Execute 5-action macro sequence")]
    public void ExecuteMacro()
    {
        var macro = new Macro
        {
            Steps = new[]
            {
                new MacroStep { Action = "KeyPress", Parameter = "W", DelayMs = 16 },
                new MacroStep { Action = "KeyPress", Parameter = "A", DelayMs = 16 },
                new MacroStep { Action = "KeyRelease", Parameter = "W", DelayMs = 16 },
                new MacroStep { Action = "KeyRelease", Parameter = "A", DelayMs = 16 },
                new MacroStep { Action = "MouseClick", Parameter = "Left", DelayMs = 16 }
            }
        };
        _macroEngine.Execute(macro);
    }

    [Benchmark(Description = "Execute 20-action complex macro")]
    public void ExecuteComplexMacro()
    {
        var steps = new List<MacroStep>();
        for (int i = 0; i < 20; i++)
        {
            steps.Add(new MacroStep { Action = i % 2 == 0 ? "KeyPress" : "KeyRelease", Parameter = "Space", DelayMs = 10 });
        }
        _macroEngine.Execute(new Macro { Steps = steps.ToArray() });
    }
}

/// <summary>
/// Entry point for running benchmarks from CLI or CI.
/// Usage: dotnet run -c Release --project RagnaController.Core --filter "*Benchmark*"
/// </summary>
public static class BenchmarkRunner
{
    public static void RunAll()
    {
        var summary = BenchmarkDotNet.Running.BenchmarkRunner.Run<BenchmarkConfig>(new[]
        {
            typeof(ControllerPollingBenchmarks),
            typeof(InputEmulationBenchmarks),
            typeof(MemoryAllocationBenchmarks),
            typeof(ProfileAndMacroBenchmarks)
        });
    }

    public static void RunCategory(string category)
    {
        var type = category.ToLowerInvariant() switch
        {
            "polling" => typeof(ControllerPollingBenchmarks),
            "input" => typeof(InputEmulationBenchmarks),
            "memory" => typeof(MemoryAllocationBenchmarks),
            "profile" => typeof(ProfileAndMacroBenchmarks),
            _ => throw new ArgumentException($"Unknown category: {category}")
        };
        
        BenchmarkDotNet.Running.BenchmarkRunner.Run<BenchmarkConfig>(type);
    }
}

/// <summary>
/// CI-friendly benchmark summary parser — extracts P99 latency for gating.
/// </summary>
public static class BenchmarkGate
{
    /// <summary>
    /// Parses BenchmarkDotNet JSON output and validates P99 < thresholdMs.
    /// Returns exit code 0 on pass, 1 on fail.
    /// </summary>
    public static int ValidateLatencyGate(string jsonPath, double thresholdMs = 2.0)
    {
        var json = File.ReadAllText(jsonPath);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        
        foreach (var benchmark in doc.RootElement.GetProperty("Benchmarks").EnumerateArray())
        {
            var stats = benchmark.GetProperty("Statistics");
            var p99 = stats.GetProperty("Percentile99").GetDouble();
            var name = benchmark.GetProperty("DisplayInfo").GetString();
            
            if (p99 > thresholdMs)
            {
                Console.Error.WriteLine($"❌ GATE FAILED: {name} P99 = {p99:F3}ms > {thresholdMs}ms");
                return 1;
            }
            Console.WriteLine($"✅ {name} P99 = {p99:F3}ms");
        }
        
        Console.WriteLine($"🎉 All benchmarks pass P99 < {thresholdMs}ms");
        return 0;
    }
}

// ============================================================================
// Supporting types (copy into actual project namespaces)
// ============================================================================

public sealed class ControllerService : IDisposable
{
    public ControllerState GetState(int index) => new();
    public void Initialize() { }
    public void Dispose() { }
}

public readonly struct ControllerState
{
    public ushort Buttons;
    public float LeftStickX, LeftStickY;
    public float RightStickX, RightStickY;
    public float LeftTrigger, RightTrigger;
    
    public bool IsButtonPressed(ControllerButton btn) => (Buttons & (ushort)btn) != 0;
}

public enum ControllerButton : ushort { A = 1, B = 2, X = 4, Y = 8, LB = 16, RB = 32, Back = 64, Start = 128, LS = 256, RS = 512 }

public sealed class InputEmulationService : IDisposable
{
    public void Initialize() { }
    public void Dispose() { }
    public void SendKey(WindowsInput.Native.VirtualKeyCode key) { }
    public void ReleaseKey(WindowsInput.Native.VirtualKeyCode key) { }
    public void MoveMouseAbsolute(int x, int y) { }
    public void ClickMouseButton(WindowsInput.Native.MouseButton btn) { }
    public void SendText(string text, int delayMs) { }
}

public sealed class ProfileManager
{
    public Profile LoadProfile(Profile p) => p;
    public void SetActiveProfile(Profile p) { }
}

public sealed class MacroEngine
{
    public void Execute(Macro macro) { }
}

public sealed class Profile
{
    public string Name { get; set; } = "";
    public Dictionary<string, ActionBinding> Mappings { get; set; } = new();
}

public sealed class ActionBinding
{
    public string Key { get; set; } = "";
    public int Priority { get; set; }
}

public sealed class Macro
{
    public MacroStep[] Steps { get; set; } = Array.Empty<MacroStep>();
}

public sealed class MacroStep
{
    public string Action { get; set; } = "";
    public string Parameter { get; set; } = "";
    public int DelayMs { get; set; }
}

public sealed class ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> _bag = new();
    private readonly Func<T> _factory;
    
    public ObjectPool(Func<T> factory, int initialCount)
    {
        _factory = factory;
        for (int i = 0; i < initialCount; i++) _bag.Add(factory());
    }
    
    public T Rent() => _bag.TryTake(out var item) ? item : _factory();
    public void Return(T item) => _bag.Add(item);
}