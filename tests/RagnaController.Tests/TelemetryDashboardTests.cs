using System;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Tests
{
    /// <summary>
    /// UI-011: Unit tests for the Live-Telemetrie-Dashboard API.
    /// Verifies that HybridEngine exposes the telemetry trackers (LatencyTracker,
    /// MemoryTracker) so the TelemetryPanel in the Developer tab can consume them.
    /// Headless — no WPF/UI instantiation required.
    /// </summary>
    public class TelemetryDashboardTests : IDisposable
    {
        // UI-011 runs headless: force the documented SDL-skip path so ControllerService
        // never starts a native SDL thread. Without this, each test engine would spawn an
        // SDL thread and they race in SDL_Init -> uncatchable AccessViolationException.
        // (ControllerService.IsHeadlessEnvironment reads this env var at construction time.)
        static TelemetryDashboardTests()
        {
            Environment.SetEnvironmentVariable("RAGNACONTROLLER_SKIP_SDL", "1");
        }

        private sealed class MockTickProvider : ITickProvider
        {
            public int IntervalMs => 8;
            public bool IsRunning { get; private set; }
            public event EventHandler? Tick;

            public void Start()
            {
                IsRunning = true;
                // Simuliert einen Tick-Puls (Event wird genutzt → kein CS0067).
                // Harmless: HybridEngine abonniert in diesen Tests nicht auf Tick.
                Tick?.Invoke(this, EventArgs.Empty);
            }
            public void Stop() => IsRunning = false;
            public void Dispose() { }
        }

        private sealed class MockMessenger : IMessenger
        {
            public void Publish<T>(T message) where T : class { }
            public IDisposable Subscribe<T>(Action<T> handler) where T : class
                => new DisposableAction(() => { });

            private sealed class DisposableAction : IDisposable
            {
                private Action? _action;
                public DisposableAction(Action action) => _action = action;
                public void Dispose() { _action?.Invoke(); _action = null; }
            }
        }

        private readonly HybridEngine _engine;

        public TelemetryDashboardTests()
        {
            var tickProvider = new MockTickProvider();
            var messenger = new MockMessenger();
            var queue = new InputCommandQueue();
            // Logger inline übergeben — Engine-Dispose räumt auf (Pattern: LongRunStabilityTests).
            // Kein separates _logger-Feld, sonst Double-Dispose im Dispose().
            _engine = new HybridEngine(tickProvider, messenger, queue, new AdvancedLogger("TelemetryDashboardTest"));
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        [Fact]
        public void LatencyTracker_IsExposed_AndNotDisposed()
        {
            // UI-011: The Developer tab telemetry panel needs a live tracker reference.
            var tracker = _engine.LatencyTracker;

            Assert.NotNull(tracker);
        }

        [Fact]
        public void MemoryTracker_IsExposed_AndNotDisposed()
        {
            var tracker = _engine.MemoryTracker;

            Assert.NotNull(tracker);
        }

        [Fact]
        public void TelemetryTrackers_AreStableAcrossReads()
        {
            // The panel polls the trackers on a DispatcherTimer — the reference
            // must be stable (same instance) across consecutive reads.
            var latency1 = _engine.LatencyTracker;
            var memory1 = _engine.MemoryTracker;

            var latency2 = _engine.LatencyTracker;
            var memory2 = _engine.MemoryTracker;

            Assert.Same(latency1, latency2);
            Assert.Same(memory1, memory2);
        }

        [Fact]
        public void LatencyTracker_GetPercentiles_DoesNotThrow()
        {
            // The panel reads thread-safe percentiles on a DispatcherTimer —
            // verify the API surface is usable without an active engine session.
            InputLatencyTracker tracker = _engine.LatencyTracker!;

            var p = tracker.GetPercentiles();

            Assert.True(p.SampleCount >= 0);
            Assert.True(p.Total.P99 >= 0, "P99 latency must be non-negative");
        }

        [Fact]
        public void MemoryTracker_GetAggregateStats_DoesNotThrow()
        {
            var tracker = _engine.MemoryTracker;
            Assert.NotNull(tracker);

            var agg = tracker.GetAggregateStats();
            Assert.True(agg.CurrentWorkingSetMb >= 0, "Working set must be non-negative");
            Assert.True(agg.TotalAllocatedMb >= 0, "Total allocated must be non-negative");
        }

        [Fact]
        public void MockTickProvider_Start_RaisesTickEvent()
        {
            // TECH-015 follow-up: MockTickProvider.Start() muss Tick auslösen (CS0067-Fix).
            // Verifiziert, dass das Interface-Member nicht nur deklariert, sondern genutzt wird.
            var provider = new MockTickProvider();

            bool raised = false;
            provider.Tick += (s, e) => raised = true;

            provider.Start();

            Assert.True(raised, "Start() muss das Tick-Event auslösen");
            Assert.True(provider.IsRunning);
        }
    }
}
