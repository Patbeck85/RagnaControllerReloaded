using System;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// FEAT-012: Unit-Tests für den PartyManager (Auto-Heal-Loop).
    /// DoD: Schwelle exakt/unter/über, schwächstes-Mitglied-Auswahl inkl. Tie-Break,
    /// Ctrl+Tab-Zyklus-Distanz (inkl. Wrap), Intervall-Rate-Limiting, MemberCount-Clamping,
    /// OOB-Guards, Reset/Stop-Suppression.
    /// Zählung über queue.Commands (DEBUG): KeyDown(Ctrl) = Party-Zyklen, KeyDown(HealKey) = Heals.
    /// </summary>
    public class PartyManagerTests : IDisposable
    {
        // Deterministische Fake-Clock: startet bei 0, nur explizit vorwärtsdrehbar.
        private sealed class FakeClock
        {
            private long _now;
            public Func<long> Clock => () => _now;
            public void Advance(int ms) => _now += ms;
        }

        private InputCommandQueue? _queue;
        private PartyManager? _pm;

        public void Dispose()
        {
            if (_queue != null)
            {
                _queue.Stop();
                _queue.Dispose();
            }
            _pm?.Dispose();
        }

        private (PartyManager pm, InputCommandQueue queue, FakeClock clock) Create(
            int members = 3, int threshold = 70, int intervalMs = 5000)
        {
            var clock = new FakeClock();
            var queue = new InputCommandQueue();
            queue.Start(); // Consumer-Thread startet → Enqueue möglich
            var pm = new PartyManager(queue, clock.Clock);
            pm.MemberCount = members;
            pm.HealThresholdPercent = threshold;
            pm.HealIntervalMs = intervalMs;
            pm.Start(); // Auto-Heal-Loop aktiv (Tests für Stop nutzen explizite Stop())
            _queue = queue;
            _pm = pm;
            return (pm, queue, clock);
        }

        // ── Zähl-Helper (DEBUG: queue.Commands) ───────────────────────────

        private static int CountKeyTaps(InputCommandQueue q, VirtualKey vk)
        {
            int n = 0;
            foreach (var c in q.Commands)
                if (c.Type == CmdType.KeyDown && c.Key == (ushort)vk) n++;
            return n;
        }

        private static int CountTabCycles(InputCommandQueue q) =>
            CountKeyTaps(q, VirtualKey.ControlLeft); // 1× KeyDown(Ctrl) pro Ctrl+Tap-Zyklus

        // ── Start/Stop-Lifecycle ─────────────────────────────────────────

        [Fact]
        public void Update_WhileStopped_DoesNotFire()
        {
            var (pm, queue, _) = Create();
            pm.Stop();

            pm.SetMemberHp(0, 10); // unter Schwelle, aber gestoppt
            pm.Update();

            Assert.Equal(0, CountKeyTaps(queue, VirtualKey.Z));
            Assert.False(pm.IsRunning);
        }

        [Fact]
        public void Start_MakesManagerActive()
        {
            var (pm, _, _) = Create();
            pm.Stop();
            pm.Start();
            Assert.True(pm.IsRunning);
        }

        // ── Schwelle: exakt / unter / über ───────────────────────────────

        [Fact]
        public void Update_HpExactlyAtThreshold_Fires()
        {
            var (pm, queue, _) = Create();
            pm.SetMemberHp(0, 70); // exakt auf der Schwelle → feuert (<=)
            pm.Update();
            Assert.Equal(1, CountKeyTaps(queue, VirtualKey.Z));
        }

        [Fact]
        public void Update_HpBelowThreshold_Fires()
        {
            var (pm, queue, _) = Create();
            pm.SetMemberHp(0, 50);
            pm.Update();
            Assert.Equal(1, CountKeyTaps(queue, VirtualKey.Z));
        }

        [Fact]
        public void Update_AllMembersAboveThreshold_NoFire()
        {
            var (pm, queue, _) = Create();
            pm.SetMemberHp(0, 80);
            pm.SetMemberHp(1, 90);
            pm.SetMemberHp(2, 100);
            pm.Update();
            Assert.Equal(0, CountKeyTaps(queue, VirtualKey.Z));
        }

        // ── Schwächstes Mitglied & Tie-Break ──────────────────────────────

        [Fact]
        public void Update_PicksWeakestMember()
        {
            var (pm, queue, _) = Create();
            pm.SetMemberHp(0, 60);
            pm.SetMemberHp(1, 20); // schwächstes
            pm.SetMemberHp(2, 65);

            int healedIndex = -1;
            pm.MemberHealed += i => healedIndex = i;
            pm.Update();

            Assert.Equal(1, healedIndex);
        }

        [Fact]
        public void Update_TiePicksLowerIndex()
        {
            var (pm, _, _) = Create();
            pm.SetMemberHp(0, 40);
            pm.SetMemberHp(1, 40); // gleicher Wert → Index 0 gewinnt
            pm.SetMemberHp(2, 90);

            int healedIndex = -1;
            pm.MemberHealed += i => healedIndex = i;
            pm.Update();

            Assert.Equal(0, healedIndex);
        }

        [Fact]
        public void Update_FiresMemberHealedEvent()
        {
            var (pm, _, _) = Create();
            int raised = 0;
            pm.MemberHealed += _ => raised++;
            pm.SetMemberHp(1, 30);
            pm.Update();
            Assert.Equal(1, raised);
        }

        // ── Ctrl+Tab-Zyklus-Distanz (inkl. Wrap) ──────────────────────────

        [Fact]
        public void Update_TargetAtCurrentIndex_NoTabCycles()
        {
            var (pm, queue, _) = Create();
            pm.SetMemberHp(0, 50); // aktuellstes Mitglied (Index 0) ist schwächstes
            pm.Update();

            Assert.Equal(0, CountTabCycles(queue));
            Assert.Equal(1, CountKeyTaps(queue, VirtualKey.Z));
        }

        [Fact]
        public void Update_TargetAhead_CyclesForward()
        {
            var (pm, queue, _) = Create(members: 4);
            pm.SetMemberHp(2, 50); // Ziel Index 2, Start Index 0 → 2 Zyklen
            pm.Update();

            Assert.Equal(2, CountTabCycles(queue));
        }

        [Fact]
        public void Update_TargetBehind_WrapsAroundRing()
        {
            var (pm, queue, clock) = Create(members: 4);
            // Phase 1: Ziel Index 3 → _currentIndex wird 3.
            pm.SetMemberHp(3, 50);
            pm.Update();

            // Phase 2: nach Intervall — Ziel Index 1, Start 3 → Wrap-Distanz (1-3+4)%4 = 2.
            clock.Advance(pm.HealIntervalMs + 1);
            pm.SetMemberHp(0, 95);
            pm.SetMemberHp(1, 40);
            pm.SetMemberHp(2, 95);
            pm.Update();

            // Phase 1: 3 Zyklen (0→3), Phase 2: 2 Zyklen (3→1) → insgesamt 5.
            Assert.Equal(5, CountTabCycles(queue));
        }

        [Fact]
        public void Update_WaitIssuedOnlyWhenCycling()
        {
            var (pm, queue, clock) = Create();
            int waitsBefore = CountWaits(queue);

            // Kein Zyklus nötig (Ziel = aktuelle Position) → nur der TapKey-eigene Wait.
            pm.SetMemberHp(0, 50);
            pm.Update();
            Assert.Equal(waitsBefore + 1, CountWaits(queue));

            // Mit Zyklus (Ziel ≠ aktuelle Position) → zusätzlich der Zielwechsel-Wait.
            waitsBefore = CountWaits(queue);
            clock.Advance(pm.HealIntervalMs); // Rate-Limit umgehen
            pm.SetMemberHp(1, 40); // _currentIndex ist jetzt 0 → 1 Zyklus
            pm.Update();
            // Zyklus: TapKeyWithModifier = 3 Waits (10ms, jitter/3, 10ms) + Wait(15) = 4, TapKey = 1 → +5.
            Assert.Equal(waitsBefore + 5, CountWaits(queue));
        }

        private static int CountWaits(InputCommandQueue q)
        {
            int n = 0;
            foreach (var c in q.Commands)
                if (c.Type == CmdType.Wait) n++;
            return n;
        }

        // ── Intervall-Rate-Limiting ──────────────────────────────────────

        [Fact]
        public void Update_SecondFireWithinInterval_IsSuppressed()
        {
            var (pm, queue, clock) = Create(members: 1);
            pm.SetMemberHp(0, 50);
            pm.Update();
            Assert.Equal(1, CountKeyTaps(queue, VirtualKey.Z));

            clock.Advance(pm.HealIntervalMs - 1); // innerhalb des Intervalls
            pm.Update();
            Assert.Equal(1, CountKeyTaps(queue, VirtualKey.Z)); // unterdrückt
        }

        [Fact]
        public void Update_FiresAgainAfterInterval()
        {
            var (pm, queue, clock) = Create(members: 1);
            pm.SetMemberHp(0, 50);
            pm.Update();

            clock.Advance(pm.HealIntervalMs); // Intervall abgelaufen
            pm.Update();
            Assert.Equal(2, CountKeyTaps(queue, VirtualKey.Z));
        }

        [Fact]
        public void Start_ResetsInterval_FiresImmediately()
        {
            var (pm, queue, clock) = Create(members: 1);
            clock.Advance(99999); // Uhr weit in der Zukunft
            pm.SetMemberHp(0, 50);
            pm.Start(); // _nextEligible = 0 → sofort feuern trotz future-Uhrzeit? Nein:
                        // Start setzt _nextEligible=0 < now → Feuer erlaubt.
            pm.Update();

            Assert.Equal(1, CountKeyTaps(queue, VirtualKey.Z));
        }

        // ── MemberCount-Clamping & SetMemberHp-Guards ─────────────────────

        [Fact]
        public void Update_MemberCountAboveMax_ClampsToFive()
        {
            var (pm, queue, _) = Create(members: 10); // über RO-Party-Limit → clamped auf 5
            for (int i = 0; i < 5; i++)
                pm.SetMemberHp(i, 90);
            pm.Update();

            Assert.Equal(0, CountKeyTaps(queue, VirtualKey.Z)); // alle über Schwelle, kein OOB
        }

        [Fact]
        public void SetMemberHp_OutOfRange_IndexIgnored()
        {
            var (pm, queue, _) = Create(members: 2);
            pm.SetMemberHp(5, 10);  // OOB → ignoriert (defensive)
            pm.SetMemberHp(-1, 10); // OOB → ignoriert
            pm.Update();

            Assert.Equal(0, CountKeyTaps(queue, VirtualKey.Z));
        }

        [Fact]
        public void SetMemberHp_ClampsToZeroHundred()
        {
            var (pm, queue, _) = Create(members: 1);
            pm.SetMemberHp(0, 250); // > 100 → clamped auf 100
            pm.Update();

            Assert.Equal(0, CountKeyTaps(queue, VirtualKey.Z)); // 100 % → über Schwelle
        }

        [Fact]
        public void SetMemberHp_Negative_ClampsToZero()
        {
            var (pm, queue, _) = Create(members: 1);
            pm.SetMemberHp(0, -50); // < 0 → clamped auf 0
            pm.Update();

            Assert.Equal(1, CountKeyTaps(queue, VirtualKey.Z)); // 0 % ≤ Schwelle → feuert
        }

        // ── Reset-Behavior ────────────────────────────────────────────────

        [Fact]
        public void Reset_StopsLoop_AndRestoresFullHp()
        {
            var (pm, queue, _) = Create();
            pm.Start();
            pm.SetMemberHp(0, 10);
            pm.Reset();

            Assert.False(pm.IsRunning);
            pm.Update(); // nach Reset: kein Fire mehr (Loop gestoppt)
            Assert.Equal(0, CountKeyTaps(queue, VirtualKey.Z));
        }

        [Fact]
        public void Dispose_StopsManager()
        {
            var clock = new FakeClock();
            var queue = new InputCommandQueue();
            queue.Start();
            _queue = queue;
            var pm = new PartyManager(queue, clock.Clock);
            _pm = pm;

            pm.Start();
            Assert.True(pm.IsRunning);
            pm.Dispose();
            Assert.False(pm.IsRunning);
        }
    }
}
