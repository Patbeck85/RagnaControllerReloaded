using System;
using System.Collections.Generic;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// PERF-010: Zero-Allokation-Gate im Tick-Pfad (CI-erzwingend).
    ///
    /// Der dominante per-Tick-Pfad ist <see cref="InputRouter.RouteInput"/> — exakt die Methode,
    /// die EngineOrchestrator.OnTick jeden Frame aufruft (EngineOrchestrator.cs: RouteInput(input,
    /// _actualDeltaMs, _rumbleEnabled)), um durch alle Engines zu routen. Das Gate treibt diese
    /// Methode headless mit identischer Produktions-Wiring und misst die Heap-Allokation pro Tick
    /// im Idle-Steady-State (kein Button gedrückt, kein Ziel gelockt, keine aktiven Buffs).
    ///
    /// ROADMAP-Ziel: "≤2 Allokationen/Tick". Wir erzwingen true zero-alloc (0 Bytes/Tick), was strikt
    /// ≤2 erfüllt. Jede verbleibende Steady-State-Allokation wird hier sichtbar und muss behoben werden
    /// (SOUL.md Self-Healing: Error → Analyse → Patch → Test → Validierung).
    /// </summary>
    public class TickAllocationGateTest : IDisposable
    {
        // Budget: Heap-Bytes pro Tick, die im Idle-Steady-State erlaubt sind.
        // true zero-alloc = 0 Bytes/Tick. Das ist STRIKTER als das ROADMAP-Ziel "≤2 Allokationen/Tick"
        // und erzwingt, dass der Tick-Pfad dauerhaft keine Heap-Allokation einführt (PERF-001/003).
        private const long MaxAllocBytesPerTick = 0;

        // Warmup-Ticks: JIT + first-touch-Allokationen (lazy List/Array-Init, String-Interning)
        // ausgleichen, damit das Mess-Fenster nur den echten Steady-State abbildet.
        // 128 Ticks = großzügige Marge gegen CI-JIT-Tiering-Unterschiede (first-touch-Absorption).
        private const int WarmupTicks = 128;

        // Mess-Fenster: genug Ticks, um per-Tick-Wert stabil zu machen (1 Tick = 1 RouteInput).
        private const int MeasureTicks = 512;

        // ── Headless-Guard (CI-erzwingend) ────────────────────────────────────────
        // Ohne diese Variable startet ControllerService im EngineOrchestrator-Konstruktor einen
        // SDL2-Thread. In headless-Umgebungen (GitHub Actions, Remote/SSH ohne Display) schlägt
        // SDL.Init(SDL_INIT_VIDEO) mit einem unabschreibbaren AccessViolation ab und crash-t den
        // Testhost — EXAKT wie in CI (wo CI=true automatisch IsHeadlessEnvironment=true macht).
        // RAGNACONTROLLER_SKIP_SDL=1 ist der vom Code selbst vorgesehene Headless-Escape-Hatch
        // (ControllerService.IsHeadlessEnvironment) und reproduziert exakt das CI-Verhalten.
        // Das Gate übt RouteInput (reine Engine-Logik, kein SDL/Controller) → Messung unbeeinflusst.
        static TickAllocationGateTest()
        {
            Environment.SetEnvironmentVariable("RAGNACONTROLLER_SKIP_SDL", "1");
        }

        private EngineOrchestrator? _engine;
        private InputCommandQueue? _queue;

        public void Dispose()
        {
            _engine?.Shutdown();
            _engine?.Dispose();
            if (_queue != null)
            {
                _queue.Stop();
                _queue.Dispose();
            }
            _engine = null;
            _queue = null;
        }

        // ── Mocks: identisch zu FullOverlayIntegrationTests (headless CI-tauglich) ────────────────

        private sealed class MockTickProvider : ITickProvider
        {
            public int IntervalMs { get; } = 8; // ~125Hz
            public bool IsRunning { get; private set; }
            public event EventHandler? Tick;

            public void Start() => IsRunning = true;
            public void Stop() => IsRunning = false;
            public void Dispose() { }

            public void FireTick() => Tick?.Invoke(this, EventArgs.Empty);
        }

        private sealed class MockMessenger : IMessenger
        {
            private readonly List<PublishedMessage> _published = new();

            public void Publish<T>(T message) where T : class
            {
                _published.Add(new PublishedMessage(typeof(T).Name, message));
            }

            public IDisposable Subscribe<T>(Action<T> handler) where T : class
                => new DisposableAction(() => { });

            public IReadOnlyList<PublishedMessage> Published => _published;

            public sealed record PublishedMessage(string TypeName, object Message);

            private sealed class DisposableAction : IDisposable
            {
                private Action? _action;
                public DisposableAction(Action action) => _action = action;
                public void Dispose() { _action?.Invoke(); _action = null; }
            }
        }

        /// <summary>
        /// Erzeugt einen headless EngineOrchestrator mit injiziertem CommandQueue —
        /// identisch zur Produktions-Wiring (vgl. FullOverlayIntegrationTests.CreateEngine).
        /// </summary>
        private static EngineOrchestrator CreateEngine(IMessenger messenger, out InputCommandQueue queue)
        {
            var tickProvider = new MockTickProvider();
            queue = new InputCommandQueue();
            return new EngineOrchestrator(tickProvider, messenger, queue, new AdvancedLogger("Test"));
        }

        [Fact]
        public void TickPath_RouteInput_IdleSteadyState_IsZeroAllocation()
        {
            // Arrange — headless Orchestrator mit Produktions-Wiring.
            var messenger = new MockMessenger();
            InputCommandQueue queue;
            var engine = CreateEngine(messenger, out queue);
            _engine = engine;
            _queue = queue;

            engine.Start();
            queue.Start(); // Consumer-Thread startet → Enqueue möglich (Idle: keine Commands).

            var router = engine.InputRouter;
            Assert.NotNull(router);

            // Warmup: JIT + first-touch-Allokationen ausgleichen (kein Messen).
            for (int i = 0; i < WarmupTicks; i++)
                router.RouteInput(new ParsedInput(), 16, false);

            // Act — Steady-State-Messfenster: GC-Bytes vor/nach N Ticks RouteInput (per-Tick-Pfad).
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < MeasureTicks; i++)
                router.RouteInput(new ParsedInput(), 16, false);
            long after = GC.GetAllocatedBytesForCurrentThread();

            // Assert — per-Tick-Heap-Allokation muss das Budget einhalten.
            long allocated = after - before;
            double perTick = (double)allocated / MeasureTicks;

            Assert.True(
                perTick <= MaxAllocBytesPerTick,
                $"PERF-010 Gate verletzt: {perTick:F2} Bytes/Tick im Idle-Steady-State " +
                $"(Budget {MaxAllocBytesPerTick} Bytes/Tick). Gesamt {allocated} Bytes in {MeasureTicks} Ticks. " +
                $"→ Tick-Pfad-Allokation identifizieren & eliminieren (Self-Healing Loop).");
        }
    }
}
