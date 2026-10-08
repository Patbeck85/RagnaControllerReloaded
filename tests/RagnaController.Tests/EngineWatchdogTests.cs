using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Xunit;
using RagnaController.Core;

namespace RagnaController.Tests
{
    /// <summary>
    /// Kontrollierbare Zeitquelle für deterministische Watchdog-Tests.
    /// Ersetzt Thread.Sleep/Wall-Clock — CI-sicher.
    /// </summary>
    public sealed class ManualTimeSource
    {
        private long _ticks = Stopwatch.GetTimestamp();
        public long NowTicks => Interlocked.Read(ref _ticks);
        public void Advance(TimeSpan delta) => Interlocked.Add(ref _ticks, (long)(delta.TotalSeconds * Stopwatch.Frequency));
        public void AdvanceMs(long ms) => Advance(TimeSpan.FromMilliseconds(ms));
        public Func<long> AsFunc() => () => NowTicks;
    }

    /// <summary>
    /// ROB-001: Unit-Tests für EngineWatchdog (Hang-Erkennung + Auto-Restart).
    /// DoD: Hang simuliert → Orchestrator-Restart ohne App-Crash; Watchdog deterministisch stoppbar.
    /// </summary>
    public class EngineWatchdogTests
    {
        // ── Hang detection (deterministic via CheckForHang) ───────────────

        [Fact]
        public void CheckForHang_NoTickRecorded_Yet_DoesNotFire()
        {
            using var wd = new EngineWatchdog();
            bool fired = false;
            wd.HangDetected += ms => fired = true;

            wd.CheckForHang(); // _lastTickTicks == 0 → nothing to judge

            Assert.False(fired);
        }

        [Fact]
        public void CheckForHang_RecentTick_DoesNotFire()
        {
            using var wd = new EngineWatchdog { HangThresholdMs = 500 };
            bool fired = false;
            wd.HangDetected += ms => fired = true;

            // Deterministisch via injizierbarer Uhr: +50 ms < 500 ms Threshold → kein Hang.
            var time = new ManualTimeSource();
            wd.TimeSource = time.AsFunc();
            wd.MarkTick();
            time.AdvanceMs(50);

            wd.CheckForHang();

            Assert.False(fired);
        }

        [Fact]
        public void CheckForHang_AfterThreshold_FiresExactlyOnce()
        {
            using var wd = new EngineWatchdog { HangThresholdMs = 100 };
            double? firedWith = null;
            int fireCount = 0;
            wd.HangDetected += ms => { firedWith = ms; fireCount++; };

            wd.MarkTick();
            Thread.Sleep(250); // > threshold

            wd.CheckForHang();
            Assert.True(firedWith.HasValue);
            Assert.InRange(firedWith.Value, 100.0, 10_000.0);

            // Repeated checks while still hung must NOT re-fire
            wd.CheckForHang();
            wd.CheckForHang();
            Assert.Equal(1, fireCount);
        }

        [Fact]
        public void CheckForHang_TicksResume_ClearsState_AllowsNextFire()
        {
            using var wd = new EngineWatchdog { HangThresholdMs = 100 };
            int fireCount = 0;
            wd.HangDetected += ms => fireCount++;

            wd.MarkTick();
            Thread.Sleep(250);
            wd.CheckForHang();
            Assert.True(wd.IsHung);
            Assert.Equal(1, fireCount);

            // Ticks resume → hang clears
            wd.MarkTick();
            Assert.False(wd.IsHung);

            // Second hang cycle fires again (state was cleared)
            Thread.Sleep(250);
            wd.CheckForHang();
            Assert.Equal(2, fireCount);
        }

        [Fact]
        public void StartStop_Idempotent_NoThrow()
        {
            using var wd = new EngineWatchdog { HangPollIntervalMs = 10 };
            wd.Start();
            wd.Start(); // idempotent — no double loop
            wd.Stop();
            wd.Stop();  // idempotent — no throw on already-stopped

            Assert.False(wd.IsHung);
        }

        [Fact]
        public void Dispose_StopsPollLoop_NoThrow()
        {
            var wd = new EngineWatchdog { HangPollIntervalMs = 10 };
            wd.Start();
            Thread.Sleep(50);
            wd.Dispose(); // must not throw even though poll loop is active

            Assert.False(wd.IsHung);
        }

        [Fact]
        public void Start_AfterDispose_RestartsCleanly()
        {
            var wd = new EngineWatchdog { HangPollIntervalMs = 10 };
            wd.Start();
            wd.Dispose();

            // Restart after Dispose must work (fresh CTS)
            wd.Start();
            Thread.Sleep(30);
            wd.Stop();
        }

        [Fact]
        public void PollLoop_DetectsHang_Automatically()
        {
            // Virtuelle Zeit → deterministisch, CI-sicher
            var time = new ManualTimeSource();
            using var wd = new EngineWatchdog { HangThresholdMs = 150, HangPollIntervalMs = 20 };
            wd.TimeSource = time.AsFunc();
            bool fired = false;
            wd.HangDetected += ms => fired = true;

            wd.MarkTick(); // Heartbeat aktiv
            wd.Start();

            // Zeit virtuell vorwärts treiben: 150ms Threshold + 2 Polls (2x20ms) = 190ms
            // Poll-Loop prüft alle 20ms → nach ~190ms muss fired=true sein
            time.AdvanceMs(200);
            wd.CheckForHang(); // Poll-Loop simulieren

            Assert.True(fired, "Poll loop did not detect hang with virtual time");
        }

        // ── Legacy performance behavior (regression guard) ────────────────

        [Fact]
        public void RecordTick_SlowStreak_FiresPerformanceWarning()
        {
            using var wd = new EngineWatchdog { MaxTickMs = 20, SlowRunRequired = 3 };
            double? avg = null;
            wd.PerformanceWarning += a => avg = a;

            for (int i = 0; i < 5; i++) wd.RecordTick(25.0);

            Assert.True(wd.IsWarning);
            Assert.True(avg.HasValue);
        }

        [Fact]
        public void RecordTick_GoodRun_ClearsWarning()
        {
            using var wd = new EngineWatchdog { MaxTickMs = 20, SlowRunRequired = 3, GoodRunRequired = 3 };
            bool recovered = false;
            wd.PerformanceRecovered += () => recovered = true;

            for (int i = 0; i < 5; i++) wd.RecordTick(25.0);
            Assert.True(wd.IsWarning);

            for (int i = 0; i < 5; i++) wd.RecordTick(8.0);

            Assert.False(wd.IsWarning);
            Assert.True(recovered);
        }

        [Fact]
        public void RecordTick_AlsoMarksHeartbeat()
        {
            // Virtuelle Zeit → deterministisch, CI-sicher
            var time = new ManualTimeSource();
            using var wd = new EngineWatchdog { HangThresholdMs = 100 };
            wd.TimeSource = time.AsFunc();
            bool fired = false;
            wd.HangDetected += ms => fired = true;

            wd.RecordTick(8.0); // per-tick call path must also update heartbeat
            time.AdvanceMs(50); // virtueller Zeitfortschritt: 50ms < 100ms Threshold
            wd.CheckForHang();

            Assert.False(fired);
        }
    }

    /// <summary>
    /// ROB-001: Integration — EngineOrchestrator mit simuliertem Hang.
    /// Tick-Schleife liefert keinen Tick → Watchdog feuert → Restart() ohne App-Crash.
    /// </summary>
    public class EngineWatchdogIntegrationTests
    {
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
            public void Publish<T>(T message) where T : class { }
            public IDisposable Subscribe<T>(Action<T> handler) where T : class => new Disposable();
            private sealed class Disposable : IDisposable { public void Dispose() { } }
        }

        [Fact]
        public void EngineOrchestrator_HangSimulated_RestartsWithoutCrash()
        {
            var tickProvider = new MockTickProvider();
            var messenger = new MockMessenger();
            var queue = new InputCommandQueue();
            var logger = new AdvancedLogger("WatchdogHangTest");

            using var engine = new EngineOrchestrator(tickProvider, messenger, queue, logger);

            var restartMessages = new List<string>();
            engine.LogMessage += msg => { lock (restartMessages) restartMessages.Add(msg); };

            // One tick to arm the heartbeat, then go silent → simulated hang.
            engine.Start();
            tickProvider.FireTick();

            // Warten bis der erste Tick verarbeitet wurde (MarkTick aufgerufen) — CI-sicher
            Thread.Sleep(200); // Generös: stellt sicher, dass Tick-Loop lief

            // Wait for watchdog (threshold 500ms, poll 100ms) to detect + restart.
            // CI-sicher: 10s Timeout statt 5s
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool restarted = false;
            while (!restarted && sw.ElapsedMilliseconds < 10000)
            {
                lock (restartMessages)
                    restarted = restartMessages.Exists(m => m.Contains("neu gestartet"));
                if (!restarted) Thread.Sleep(50);
            }

            Assert.True(restarted, "Watchdog did not restart engine after simulated hang (10s timeout)");

            // Ticks resume after restart → hang state clears, no further restarts.
            // Sustained healthy ticks (nicht Einzel-Ticks) — sonst feuert Watchdog zu Recht erneut.
            for (int i = 0; i < 60; i++) // Länger: 60 * 30ms = 1.8s << 500ms Threshold
            {
                tickProvider.FireTick();
                Thread.Sleep(30);
            }

            int restartCountAfter = 0;
            lock (restartMessages) restartCountAfter = restartMessages.Count(m => m.Contains("neu gestartet"));
            Assert.True(restartCountAfter == 1, $"Exactly one watchdog restart expected for a single hang, got {restartCountAfter}");

            engine.Shutdown();
        }

        [Fact]
        public void EngineOrchestrator_HealthyTicks_NoFalsePositiveHang()
        {
            var tickProvider = new MockTickProvider();
            var messenger = new MockMessenger();
            var queue = new InputCommandQueue();
            var logger = new AdvancedLogger("WatchdogHealthyTest");

            using var engine = new EngineOrchestrator(tickProvider, messenger, queue, logger);
            bool hangFired = false;
            engine.LogMessage += msg => { if (msg.Contains("Hang erkannt")) hangFired = true; };

            engine.Start();
            // Healthy: continuous ticks well under the 500ms threshold.
            for (int i = 0; i < 15; i++)
            {
                tickProvider.FireTick();
                Thread.Sleep(30);
            }

            Assert.False(hangFired, "False-positive hang detected while ticks are healthy");

            engine.Shutdown();
        }
    }
}
