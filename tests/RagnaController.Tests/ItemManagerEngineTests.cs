using System;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// FEAT-011: Unit-Tests für die ItemManagerEngine (Auto-Potion &amp; Item-Verwaltung).
    /// DoD: Schwelle exakt/unter/über, Cooldown blockiert Re-Fire, kein Fire wenn disconnected (gestoppt).
    /// </summary>
    public class ItemManagerEngineTests : IDisposable
    {
        // Deterministische Fake-Clock: startet bei 0, nur explizit vorwärtsdrehbar.
        private sealed class FakeClock
        {
            private long _now;
            public Func<long> Clock => () => _now;
            public void Advance(int ms) => _now += ms;
        }

        private InputCommandQueue? _queue;
        private ItemManagerEngine? _engine;

        public void Dispose()
        {
            // Queue-Consumer sauber beenden (Start/Stop-Lifecycle wie in InputCommandQueueTests).
            if (_queue != null)
            {
                _queue.Stop();
                _queue.Dispose();
            }
            _engine?.Dispose();
        }

        private (ItemManagerEngine engine, InputCommandQueue queue, FakeClock clock) Create(
            ItemConfig item, bool running = true)
        {
            var clock = new FakeClock();
            var queue = new InputCommandQueue();
            queue.Start(); // Consumer-Thread startet → Enqueue möglich
            var engine = new ItemManagerEngine(queue, clock.Clock);
            engine.Configure(new[] { item });
            if (running) engine.Start();
            _queue = queue;
            _engine = engine;
            return (engine, queue, clock);
        }

        private static ItemConfig DefaultItem() => new ItemConfig
        {
            Name = "Potion",
            Key = VirtualKey.A,
            HpThresholdPercent = 70,
            MinSpRequired = 0,
            CooldownMs = 3000,
            CheckIntervalMs = 1000,
            Enabled = true
        };

        // ── Schwelle: exakt / unter / über ──────────────────────────────

        [Fact]
        public void Update_HpExactlyAtThreshold_Fires()
        {
            var (engine, queue, _) = Create(DefaultItem());

            engine.Update(70, 100, 8); // exakt auf der Schwelle → feuert (<=)

            Assert.Equal(1, CountTaps(queue));
        }

        [Fact]
        public void Update_HpBelowThreshold_Fires()
        {
            var (engine, queue, _) = Create(DefaultItem());

            engine.Update(42, 100, 8); // deutlich unter der Schwelle → feuert

            Assert.Equal(1, CountTaps(queue));
        }

        [Fact]
        public void Update_HpAboveThreshold_DoesNotFire()
        {
            var (engine, queue, _) = Create(DefaultItem());

            engine.Update(95, 100, 8); // über der Schwelle → kein Fire

            Assert.Equal(0, CountTaps(queue));
        }

        [Fact]
        public void Update_HpRecovers_AboveThreshold_ThenFallsAgain_FiresOncePerCrossing()
        {
            var (engine, queue, clock) = Create(DefaultItem());

            engine.Update(95, 100, 8); // über → kein Fire
            Assert.Equal(0, CountTaps(queue));

            clock.Advance(1000); // Check-Intervall abgelaufen
            engine.Update(50, 100, 8); // fällt unter → feuert
            Assert.Equal(1, CountTaps(queue));
        }

        // ── SP-Bedingung ────────────────────────────────────────────────

        [Fact]
        public void Update_SpBelowMinimum_DoesNotFire()
        {
            var item = DefaultItem();
            item.MinSpRequired = 50;
            var (engine, queue, _) = Create(item);

            engine.Update(40, 30, 8); // HP ok, aber SP zu niedrig → kein Fire

            Assert.Equal(0, CountTaps(queue));
        }

        [Fact]
        public void Update_SpExactlyAtMinimum_Fires()
        {
            var item = DefaultItem();
            item.MinSpRequired = 50;
            var (engine, queue, _) = Create(item);

            engine.Update(40, 50, 8); // SP exakt am Minimum → feuert

            Assert.Equal(1, CountTaps(queue));
        }

        // ── Cooldown blockiert Re-Fire ───────────────────────────────────

        [Fact]
        public void Update_SecondFireWithinCooldown_IsBlocked()
        {
            var (engine, queue, clock) = Create(DefaultItem());

            engine.Update(40, 100, 8); // Fire #1 bei t=0 (nextCheck=1000, nextEligible=3000)
            Assert.Equal(1, CountTaps(queue));

            clock.Advance(1500); // t=1500: Check-Intervall abgelaufen, Cooldown (3000ms) aktiv
            engine.Update(40, 100, 8);
            Assert.Equal(1, CountTaps(queue)); // Cooldown blockiert Re-Fire

            clock.Advance(1600); // t=3100: Cooldown abgelaufen
            engine.Update(40, 100, 8);
            Assert.Equal(2, CountTaps(queue)); // Re-Fire erlaubt
        }

        [Fact]
        public void Update_FiresAgain_OnlyAfterCooldownElapsed()
        {
            var item = DefaultItem();
            item.CooldownMs = 5000;
            item.CheckIntervalMs = 100;
            var (engine, queue, clock) = Create(item);

            engine.Update(40, 100, 8); // Fire #1 bei t=0
            Assert.Equal(1, CountTaps(queue));

            clock.Advance(4900); // Cooldown läuft noch
            engine.Update(40, 100, 8);
            Assert.Equal(1, CountTaps(queue));

            clock.Advance(200); // t=5100 → Cooldown abgelaufen
            engine.Update(40, 100, 8);
            Assert.Equal(2, CountTaps(queue));
        }

        // ── Disconnected / gestoppt: kein Fire ───────────────────────────

        [Fact]
        public void Update_WhileStopped_DoesNotFire()
        {
            var (engine, queue, _) = Create(DefaultItem(), running: false);

            engine.Update(10, 100, 8); // HP kritisch, aber Engine gestoppt (z. B. disconnected)

            Assert.Equal(0, CountTaps(queue));
        }

        [Fact]
        public void Stop_AfterStart_SuppressesFiring()
        {
            var (engine, queue, _) = Create(DefaultItem());

            engine.Stop(); // z. B. Disconnect während des Kampfs
            engine.Update(10, 100, 8);

            Assert.Equal(0, CountTaps(queue));
        }

        [Fact]
        public void Start_AfterStop_ResumesFiring()
        {
            var (engine, queue, _) = Create(DefaultItem(), running: false);

            engine.Start();
            engine.Update(10, 100, 8);

            Assert.Equal(1, CountTaps(queue));
        }

        // ── Check-Intervall ─────────────────────────────────────────────

        [Fact]
        public void Update_CheckIntervalNotElapsed_DoesNotFireAgain()
        {
            var item = DefaultItem();
            item.CooldownMs = 0; // Cooldown ausschalten, Intervall isoliert testen
            item.CheckIntervalMs = 1000;
            var (engine, queue, clock) = Create(item);

            engine.Update(40, 100, 8); // Fire #1 bei t=0
            Assert.Equal(1, CountTaps(queue));

            clock.Advance(500); // Intervall (1000ms) noch nicht abgelaufen
            engine.Update(40, 100, 8);
            Assert.Equal(1, CountTaps(queue));

            clock.Advance(600); // t=1100 → Intervall abgelaufen
            engine.Update(40, 100, 8);
            Assert.Equal(2, CountTaps(queue));
        }

        // ── Disabled Items & Configure/Reset ────────────────────────────

        [Fact]
        public void Update_DisabledItem_DoesNotFire()
        {
            var item = DefaultItem();
            item.Enabled = false;
            var (engine, queue, _) = Create(item);

            engine.Update(10, 100, 8);

            Assert.Equal(0, CountTaps(queue));
        }

        [Fact]
        public void Configure_EmptyList_NoFiring()
        {
            var clock = new FakeClock();
            var queue = new InputCommandQueue();
            queue.Start();
            var engine = new ItemManagerEngine(queue, clock.Clock);
            engine.Configure(Array.Empty<ItemConfig>());
            engine.Start();
            _queue = queue;
            _engine = engine;

            engine.Update(10, 100, 8);

            Assert.Equal(0, CountTaps(queue));
        }

        [Fact]
        public void Reset_ClearsItemsAndStops()
        {
            var (engine, queue, _) = Create(DefaultItem());

            engine.Reset();
            Assert.False(engine.IsRunning);

            engine.Update(10, 100, 8); // nach Reset: keine Items mehr

            Assert.Equal(0, CountTaps(queue));
        }

        // ── ItemFired-Event ─────────────────────────────────────────────

        [Fact]
        public void Update_FiresItemFiredEvent_WithCorrectItem()
        {
            var (engine, _, _) = Create(DefaultItem());
            ItemConfig? fired = null;
            engine.ItemFired += i => fired = i;

            engine.Update(40, 100, 8);

            Assert.NotNull(fired);
            Assert.Equal("Potion", fired!.Name);
        }

        [Fact]
        public void Update_NoFire_EventNotRaised()
        {
            var (engine, _, _) = Create(DefaultItem());
            int raised = 0;
            engine.ItemFired += _ => raised++;

            engine.Update(95, 100, 8); // über Schwelle → kein Fire

            Assert.Equal(0, raised);
        }

        // ── Multiple Items: unabhängige Cooldowns ───────────────────────

        [Fact]
        public void Update_MultipleItems_IndependentCooldowns()
        {
            var clock = new FakeClock();
            var queue = new InputCommandQueue();
            queue.Start();
            var engine = new ItemManagerEngine(queue, clock.Clock);
            engine.Configure(new[]
            {
                new ItemConfig { Name = "HP-Potion", Key = VirtualKey.A, HpThresholdPercent = 70, CooldownMs = 3000, CheckIntervalMs = 1000 },
                new ItemConfig { Name = "SP-Potion", Key = VirtualKey.S, HpThresholdPercent = 50, MinSpRequired = 0, CooldownMs = 5000, CheckIntervalMs = 1000 }
            });
            engine.Start();
            _queue = queue;
            _engine = engine;

            engine.Update(40, 100, 8); // t=0: HP=40 → unter beiden Schwellen (70/50) → beide feuern
            Assert.Equal(2, CountTaps(queue));

            // t=3100: HP-Potion Cooldown (3000ms) abgelaufen → nur es feuert erneut.
            clock.Advance(3100);
            engine.Update(40, 100, 8);
            Assert.Equal(3, CountTaps(queue));

            // t=6200: HP-Potion (eligible 6100) UND SP-Potion (eligible 5000) feuern erneut.
            clock.Advance(3100);
            engine.Update(40, 100, 8);
            Assert.Equal(5, CountTaps(queue));
        }

        // ── Helper ───────────────────────────────────────────────────────

        /// <summary>Zählt KeyDown-Commands in der Queue (ein Tap = ein KeyDown).</summary>
        private static int CountTaps(InputCommandQueue queue)
        {
            int count = 0;
            foreach (var cmd in queue.Commands)
                if (cmd.Type == CmdType.KeyDown) count++;
            return count;
        }
    }
}
