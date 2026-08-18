using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using RagnaController.Core;
using RagnaController.Models;

BenchmarkRunner.Run<EngineBenchmarks>();

/// <summary>
/// Minimale Queue-Implementierung für Benchmarks — kein Background-Thread,
/// keine Win32-Aufrufe, alle Operationen sind No-OPs.
/// </summary>
internal sealed class BenchmarkCommandQueue : RagnaController.Core.InputCommandQueue
{
    // Note: This inherits from InputCommandQueue for benchmarks - all operations are No-OPs
}

/// <summary>
/// Benchmarks für den 125Hz Game-Loop.
/// Ausführen: dotnet run -c Release --project benchmarks
///
/// Zielwerte (auf einem modernen CPU):
///   ControllerSnapshot Build:  < 50 ns
///   ComboEngine.Update:        < 100 ns
///   Messenger.Publish (10 sub): < 200 ns
///   MovementEngine.Update:     < 100 ns
/// </summary>
[MemoryDiagnoser]
[RPlotExporter]
[DisassemblyDiagnoser]
[ShortRunJob]
public class EngineBenchmarks
{
    private ComboEngine _combo = null!;
    private MovementEngine _movement = null!;
    private Messenger _messenger = null!;
    private ControllerSnapshot _snap = null!;
    private ParsedInput _input = new ParsedInput { IsConnected = true, LeftX = 0.8f, LeftY = 0.3f };

    [GlobalSetup]
    public void Setup()
    {
        var queue = new BenchmarkCommandQueue();
        
        _combo = new ComboEngine(queue)
        {
            Enabled  = true,
            Sequence = new() { VirtualKey.F1, VirtualKey.F2, VirtualKey.F3 },
            CurrentDelays = new() { 300, 300, 300 },
        };

        _movement = new MovementEngine(queue, null!);
        _movement.SetCenter(500, 400, 100, 1.0f);
        _movement.ActionRpgMode = true;

        _messenger = new Messenger();
        // 10 Subscriber wie in der echten App
        for (int i = 0; i < 10; i++)
            _messenger.Subscribe<SnapshotReadyMessage>(_ => { });

        _input = new ParsedInput { IsConnected = true, LeftX = 0.8f, LeftY = 0.3f };
        _snap  = new ControllerSnapshot { StateLabel = "IDLE", LayerText = "BASE" };
    }

    [Benchmark(Description = "ComboEngine.Update (button held)")]
    public void ComboEngineUpdate()
        => _combo.Update(isHeld: true, ms: 8);

    [Benchmark(Description = "MovementEngine.Update (stick forward)")]
    public void MovementEngineUpdate()
        => _movement.Update(0.8f, 0.3f);

    [Benchmark(Description = "Messenger.Publish<SnapshotReadyMessage> (10 subs)")]
    public void MessengerPublish()
        => _messenger.Publish(new SnapshotReadyMessage(_snap));

    [Benchmark(Description = "ControllerSnapshot init (readonly record struct)")]
    public ControllerSnapshot SnapshotInit()
        => new ControllerSnapshot
        {
            LeftX = 0.8f, LeftY = 0.3f,
            StateLabel = "IDLE", LayerText = "BASE",
            TickMs = 8,
        };

    [Benchmark(Description = "JitterService.Apply (Random.Shared)")]
    public int JitterApply()
        => JitterService.Apply(100, 20);

    // Zusätzliche Benchmarks für Edge Cases
    [Benchmark(Description = "ComboEngine.Update (button released)")]
    public void ComboEngineUpdateReleased()
        => _combo.Update(isHeld: false, ms: 8);

    [Benchmark(Description = "MovementEngine.Update (no movement)")]
    public void MovementEngineUpdateNoMove()
        => _movement.Update(0f, 0f);

    [Benchmark(Description = "Messenger.Publish (empty message)")]
    public void MessengerPublishEmpty()
        => _messenger.Publish(new SnapshotReadyMessage(_snap));

    [Benchmark(Description = "JitterService.Apply (low jitter)")]
    public int JitterApplyLow()
        => JitterService.Apply(50, 5);

    [Benchmark(Description = "JitterService.Apply (high jitter)")]
    public int JitterApplyHigh()
        => JitterService.Apply(200, 50);
}
