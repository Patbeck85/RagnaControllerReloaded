using System;
using System.Collections.Generic;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// ROB-002: Unit Tests für die Input-Emulation-Failover-State-Machine in InputRouter.
    /// Deterministisch über mockte IMouseEmulationStrategy-Implementierungen:
    ///   - N (TriggerCount) aufeinanderfolgende Flushes über der Latenz-Schwelle → Switch auf Fallback
    ///   - M (RecoveryCount) stabile Flushes danach → Recovery zurück zu Primär
    ///   - Unterbrechung des Slow-Zählers durch einen schnellen Flush
    ///   - Kein Crash ohne konfigurierten Fallback
    /// </summary>
    public class InputRouterFailoverTests
    {
        /// <summary>Mockte Emulations-Strategie mit konfigurierbarem DisplayName.</summary>
        private sealed class FakeStrategy : IMouseEmulationStrategy
        {
            public string DisplayName { get; }

            public FakeStrategy(string name) => DisplayName = name;

            public bool IsAvailable => true;

            public void MoveRelative(int dx, int dy) { }
            public void MoveAbsolute(int x, int y) { }
            public void LeftDown() { }
            public void LeftUp() { }
            public void RightDown() { }
            public void RightUp() { }
        }

        private static (InputRouter router, FakeStrategy primary, FakeStrategy fallback) CreateRouter(
            int thresholdMs = 5, int triggerCount = 3, int recoveryCount = 2)
        {
            var settings = new Settings
            {
                FailoverLatencyThresholdMs = thresholdMs,
                FailoverTriggerCount = triggerCount,
                FailoverRecoveryCount = recoveryCount
            };

            var primary = new FakeStrategy("SendInput");
            var fallback = new FakeStrategy("Interception (Kernel-Mode)");
            var router = new InputRouter(); // internal: Failover-only-Konstruktor (IVT)
            router.InitializeFailover(primary, fallback, settings);
            return (router, primary, fallback);
        }

        [Fact]
        public void ThreeConsecutiveSlowFlushes_SwitchToFallback()
        {
            var (router, primary, fallback) = CreateRouter(thresholdMs: 5, triggerCount: 3);

            var switches = new List<(string from, string to, int count)>();
            router.FailoverSwitched += (from, to, count) => switches.Add((from, to, count));

            router.RecordSendInputLatency(6.0);   // slow 1/3
            Assert.Equal(primary, router.ActiveStrategy);

            router.RecordSendInputLatency(7.5);   // slow 2/3
            Assert.Equal(primary, router.ActiveStrategy);

            router.RecordSendInputLatency(8.0);   // slow 3/3 → Switch
            Assert.Equal(fallback, router.ActiveStrategy);

            Assert.Single(switches);
            Assert.Equal("SendInput", switches[0].from);
            Assert.Equal("Interception (Kernel-Mode)", switches[0].to);
            Assert.Equal(3, switches[0].count);
        }

        [Fact]
        public void SlowFlushBelowThreshold_DoesNotSwitch()
        {
            var (router, primary, _) = CreateRouter(thresholdMs: 5, triggerCount: 3);

            // Latenz == Schwelle ist NICHT langsam (Strenge Ungleichheit)
            for (int i = 0; i < 10; i++) router.RecordSendInputLatency(5.0);

            Assert.Equal(primary, router.ActiveStrategy);
        }

        [Fact]
        public void FastFlushResetsSlowCounter()
        {
            var (router, primary, _) = CreateRouter(thresholdMs: 5, triggerCount: 3);

            for (int round = 0; round < 5; round++)
            {
                router.RecordSendInputLatency(9.0); // slow
                router.RecordSendInputLatency(9.0); // slow
                router.RecordSendInputLatency(1.0); // fast → Zähler resettet
            }

            Assert.Equal(primary, router.ActiveStrategy);
        }

        [Fact]
        public void TwoStableFlushesOnFallback_RecoverToPrimary()
        {
            var (router, primary, fallback) = CreateRouter(thresholdMs: 5, triggerCount: 3, recoveryCount: 2);

            // Erst in den Fallback schalten …
            router.RecordSendInputLatency(6.0);
            router.RecordSendInputLatency(6.0);
            router.RecordSendInputLatency(6.0);
            Assert.Equal(fallback, router.ActiveStrategy);

            // … dann 2 stabile Flushes → Recovery
            router.RecordSendInputLatency(1.0); // stabil 1/2
            Assert.Equal(fallback, router.ActiveStrategy);
            router.RecordSendInputLatency(1.0); // stabil 2/2 → Recovery

            Assert.Equal(primary, router.ActiveStrategy);
        }

        [Fact]
        public void SlowFlushOnFallback_ResetsRecoveryCounter()
        {
            var (router, _, fallback) = CreateRouter(thresholdMs: 5, triggerCount: 3, recoveryCount: 2);

            // In den Fallback schalten …
            router.RecordSendInputLatency(6.0);
            router.RecordSendInputLatency(6.0);
            router.RecordSendInputLatency(6.0);

            // … Recovery-Zähler durch langsamen Flush unterbrechen (kein endloser Ping-Pong)
            for (int round = 0; round < 3; round++)
            {
                router.RecordSendInputLatency(1.0); // stabil 1/2
                router.RecordSendInputLatency(9.0); // slow → Zähler zurück
            }

            Assert.Equal(fallback, router.ActiveStrategy);
        }

        [Fact]
        public void NoFallbackConfigured_RecordsAreIgnored()
        {
            var settings = new Settings
            {
                FailoverLatencyThresholdMs = 5,
                FailoverTriggerCount = 3,
                FailoverRecoveryCount = 2
            };

            var primary = new FakeStrategy("SendInput");
            var router = new InputRouter(); // internal: Failover-only-Konstruktor (IVT)
            router.InitializeFailover(primary, null, settings);

            // Darf weder werfen noch schalten — SendInput bleibt aktiv.
            for (int i = 0; i < 10; i++) router.RecordSendInputLatency(99.0);

            Assert.Equal(primary, router.ActiveStrategy);
        }
    }
}
