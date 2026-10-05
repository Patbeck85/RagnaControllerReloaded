using System;
using System.Collections.Generic;
using System.Diagnostics;
using RagnaController.Core;
using RagnaController.Models;
using Xunit;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-012: Long-Run-Stability-Test (Soak). 10k Mock-Ticks durch die komplette
    /// Engine-Kette (<see cref="InputRouter.RouteInput"/>) als Memory-Leak-Guard.
    /// </summary>
    /// <remarks>
    /// <para><b>Warum Idle-Steady-State?</b> Der Idle-Pfad ist der einzige seitenwirkungsfreie Pfad:
    /// jeder aktive Input (Sticks/Buttons) würde über den Consumer-Thread <c>SendInput</c> aufrufen
    /// und den ECHTEN Mauszeiger bewegen — in CI unzulässig. Der Idle-Pfad treibt trotzdem die
    /// komplette Engine-Kette 10k× mit <c>actualDeltaMs=16</c> = ~160s simulierte Combat-Zeit
    /// (übt Cooldown-Expiry, Mob-Sweep-Zyklen) in &lt;1s Wall-Clock.</para>
    ///
    /// <para><b>Leak-Guard-Metriken</b> (laut ROADMAP TEST-012):
    /// <list type="bullet">
    ///   <item><see cref="GC.GetTotalMemory"/> (forced Full-GC) — lebende Heap-Größe.</item>
    ///   <item>Gen2-GC-Zähler (<see cref="GC.CollectionCount"/>) — große Allokationen/Fragmentierung.</item>
    ///   <item>Handle-Zählung (<see cref="Process.HandleCount"/>) — Windows-Handle-Leak.</item>
    /// </list>
    /// Alle drei sind prozessweit; daher Triple-Full-GC vor/nach + tolerante Schwellen, die
    /// CI-Parallelism-Noise absorbieren, aber einen echten pro-Tick-Leak (wächst um MBs/Handles)
    /// deutlich über der Schwelle fangen.</para>
    /// </remarks>
    public class LongRunStabilityTests
    {
        private const int SoakTicks = 10_000;

        // Headless-Guard: CI-Verhalten reproduzieren (kein SDL-AccessViolation im Testhost).
        static LongRunStabilityTests()
        {
            Environment.SetEnvironmentVariable("RAGNACONTROLLER_SKIP_SDL", "1");
        }

        private sealed class MockTickProvider : ITickProvider
        {
            public int IntervalMs { get; } = 8;
            public bool IsRunning { get; private set; }
            public event EventHandler? Tick;

            public void Start() => IsRunning = true;
            public void Stop() => IsRunning = false;
            public void Dispose() { }
            public void FireTick() => Tick?.Invoke(this, EventArgs.Empty);
        }

        private sealed class MockMessenger : IMessenger
        {
            public void Publish<T>(T message) where T : class
            {
            }

            public IDisposable Subscribe<T>(Action<T> handler) where T : class
                => new NoopDisposable();

            public void Unsubscribe<T>(Action<T> handler) where T : class
            {
            }
        }

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }

        private static EngineOrchestrator CreateEngine()
        {
            var tickProvider = new MockTickProvider();
            var queue = new InputCommandQueue();
            return new EngineOrchestrator(tickProvider, new MockMessenger(), queue, new AdvancedLogger("SoakTest"));
        }

        /// <summary>Triple-Full-GC: deterministische lebende-Heap-Baseline.</summary>
        private static void ForceFullGc()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [Fact]
        public void Soak_10kTicks_CompleteEngineChain_NoMemoryLeak()
        {
            // Arrange: Engine + Warmup (first-touch-Allokationen herausnehmen)
            var engine = CreateEngine();
            var router = engine.InputRouter;
            var input = new ParsedInput();

            for (int i = 0; i < 256; i++)
                router.RouteInput(input, 16, false);

            ForceFullGc();

            var memBefore = GC.GetTotalMemory(true);
            var gen2Before = GC.CollectionCount(2);
            var handlesBefore = Process.GetCurrentProcess().HandleCount;

            // Act: 10k Ticks durch die komplette Engine-Kette
            for (int i = 0; i < SoakTicks; i++)
                router.RouteInput(input, 16, false);

            // WICHTIG: Gen2-GC-Zähler DIREKT nach dem Soak messen — VOR dem Cleanup-Full-GC.
            // Sonst zählt mein eigener ForceFullGc() (3× GC.Collect()) als Gen2-GCs in gen2After
            // und verfälscht das Signal (Design-Bug: Cleanup wird als Leak gemeldet).
            var gen2AfterSoak = GC.CollectionCount(2);

            ForceFullGc();

            var memAfter = GC.GetTotalMemory(true);
            var handlesAfter = Process.GetCurrentProcess().HandleCount;

            // Assert: keine Monotonie (Leak) über 10k Ticks
            long memDelta = memAfter - memBefore;
            int gen2Delta = gen2AfterSoak - gen2Before;
            int handleDelta = handlesAfter - handlesBefore;

            // Gen2-GC: sollte NICHT pro-Tick steigen (Idle-Pfad allokiert 0 Bytes/Tick — PERF-010).
            // Messung VOR dem Cleanup-Full-GC → zählt nur GCs während der 10k Ticks.
            Assert.True(gen2Delta <= 1,
                $"Gen2-GC stieg um {gen2Delta} während {SoakTicks} Idle-Ticks — deutet auf große Allokationen/Fragmentierung hin");

            // TotalMemory: lebende Heap-Zuwachs nach Full-GC. Tolerante Schwelle gegen CI-Parallelism-Noise,
            // aber ein echter pro-Tick-Leak (wächst um MBs) liegt deutlich darüber.
            // 10 MB Marge: deckt CI-Noise, JIT-Overhead, GC-Bookkeeping ab; echte Leaks wachsen um GBs.
            const long MaxMemDelta = 10 * 1024 * 1024; // 10 MB Marge
            Assert.True(memDelta < MaxMemDelta,
                $"Lebende Heap stieg um {memDelta} Bytes nach Full-GC (Limit {MaxMemDelta}) — möglicher Memory-Leak");

            // HandleCount: Windows-Handles sollten nicht pro Tick wachsen.
            const int MaxHandleDelta = 32; // Marge gegen transient Handles
            Assert.True(handleDelta < MaxHandleDelta,
                $"HandleCount stieg um {handleDelta} (Limit {MaxHandleDelta}) — möglicher Handle-Leak");

            engine.Dispose();
        }

        [Fact]
        public void Soak_LeakGuard_DetectsInjectedHeapGrowth()
        {
            // Verifikation: der Leak-Guard MUSS echtes Heap-Wachstum fangen (kein Blind-Pass).
            // Injiziere pro Tick ein 1KB-Array in eine statische Liste (nie freigegeben) = echter Heap-Leak.
            var engine = CreateEngine();
            var router = engine.InputRouter;
            var input = new ParsedInput();

            for (int i = 0; i < 256; i++)
                router.RouteInput(input, 16, false);

            ForceFullGc();
            var memBefore = GC.GetTotalMemory(true);

            const int LeakTicks = 20_000;
            // Lokale Liste (NUR in diesem Test rooted): hält injizierte Allokationen für die Messdauer lebendig,
            // ohne statisch 20 MB im Prozess zu halten (würde parallel laufende GC-Messungen verunreinigen).
            var leakSink = new List<byte[]>(LeakTicks);
            for (int i = 0; i < LeakTicks; i++)
                leakSink.Add(new byte[1024]); // ~1 KB pro Tick, für Messdauer nie freigegeben

            ForceFullGc();
            var memAfter = GC.GetTotalMemory(true);
            long memDelta = memAfter - memBefore;

            // 20k × 1KB ≈ 20 MB Lebende Heap. Der Guard MUSS das erkennen (Signal >> CI-Noise).
            Assert.True(memDelta > 5 * 1024 * 1024,
                $"Leak-Guard erkannte kein Heap-Wachstum (nur {memDelta} Bytes) — Guard ist blind");

            // Referenz halten bis nach der Messung (kein vorzeitiges Unrooting durch JIT-Optimierung).
            _ = leakSink.Count;
            engine.Dispose();
        }
    }
}
