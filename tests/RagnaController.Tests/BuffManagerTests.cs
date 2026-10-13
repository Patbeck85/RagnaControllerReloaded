using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-010: BuffManager Unit Tests.
    /// Deckt RegisterBuff/HasBuff, GetBuffRemainingSec, RemoveBuff (Event),
    /// Debuff-Lifecycle (RegisterDebuff → DebuffApplied, Update → DebuffExpired),
    /// Auto-Recast bei Ablauf und ClearAll ab.
    /// Hinweis: Buff-Timing ist wall-clock-basiert (DateTime.UtcNow) —
    /// Ablauf-Szenarien nutzen minimale Durations + Thread.Sleep.
    /// </summary>
    public class BuffManagerTests
    {
        private sealed class TestContext : IDisposable
        {
            public readonly BuffManager Manager;
            public readonly InputCommandQueue Queue;
            public readonly List<InputCmd> Log = new();

            public readonly List<string> ExpiringWarnings = new();
            public readonly List<string> ExpiredBuffs = new();
            public readonly List<(string Name, int Sec)> AppliedDebuffs = new();
            public readonly List<string> ExpiredDebuffs = new();

            public TestContext()
            {
                Queue = new InputCommandQueue();
                Queue.OnCommandEnqueued += cmd => Log.Add(cmd);
                Queue.Start();
                Manager = new BuffManager(Queue, new CooldownManager(new NoOpMessenger(), new NoOpFeedback()));

                Manager.BuffExpiringWarning += (name, sec) => ExpiringWarnings.Add(name);
                Manager.BuffExpired += name => ExpiredBuffs.Add(name);
                Manager.DebuffApplied += (name, sec) => AppliedDebuffs.Add((name, sec));
                Manager.DebuffExpired += name => ExpiredDebuffs.Add(name);
            }

            public void Dispose()
            {
                Queue.Stop();
                Queue.Dispose();
            }
        }

        private sealed class NoOpMessenger : IMessenger
        {
            public void Publish<T>(T message) where T : class { }
            public IDisposable Subscribe<T>(Action<T> handler) where T : class => NoOpDisposable.Instance;
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public static readonly NoOpDisposable Instance = new();
            public void Dispose() { }
        }

        private sealed class NoOpFeedback : IFeedbackProvider
        {
            public void StopAll() { }
            public void SetLED(byte r, byte g, byte b) { }
            public void Tick() { }
            public void Trigger(FeedbackType type) { }
            public void TriggerSkillFired() { }
            public void StopRumble() { }
            public void Dispose() { }
        }

        [Fact]
        public void RegisterBuff_MakesBuffActive_AndReportsRemainingTime()
        {
            using var ctx = new TestContext();

            // Use explicit for test with known duration
            ctx.Manager.RegisterBuffExplicit("Haste", 60);

            Assert.True(ctx.Manager.HasBuff("Haste"));
            Assert.Contains("Haste", ctx.Manager.ActiveBuffNames);
            float remaining = ctx.Manager.GetBuffRemainingSec("Haste");
            Assert.InRange(remaining, 59f, 60f);
        }

        [Fact]
        public void RegisterBuff_EmptyName_IsIgnored()
        {
            using var ctx = new TestContext();

            ctx.Manager.RegisterBuffExplicit("", 60);
            ctx.Manager.RegisterBuffExplicit(null!, 60);

            Assert.Empty(ctx.Manager.ActiveBuffNames);
        }

        [Fact]
        public void GetBuffRemainingSec_UnknownBuff_ReturnsZero()
        {
            using var ctx = new TestContext();

            Assert.Equal(0f, ctx.Manager.GetBuffRemainingSec("NichtDa"));
            Assert.False(ctx.Manager.HasBuff("NichtDa"));
        }

        [Fact]
        public void RemoveBuff_FiresBuffExpiredEvent()
        {
            using var ctx = new TestContext();
            ctx.Manager.RegisterBuffExplicit("Bless", 60);

            ctx.Manager.RemoveBuff("Bless");

            Assert.False(ctx.Manager.HasBuff("Bless"));
            Assert.Contains("Bless", ctx.ExpiredBuffs);
        }

        [Fact]
        public void RegisterDebuff_FiresDebuffAppliedEvent()
        {
            using var ctx = new TestContext();

            ctx.Manager.RegisterDebuff("Slow", 30);

            Assert.True(ctx.Manager.HasDebuff("Slow"));
            Assert.Contains(("Slow", 30), ctx.AppliedDebuffs);
            float remaining = ctx.Manager.GetDebuffRemainingSec("Slow");
            Assert.InRange(remaining, 29f, 30f);
        }

        [Fact]
        public void Update_AfterExpiration_FiresBuffExpired_AndAutoRecastKey()
        {
            using var ctx = new TestContext();

            // DurationSec=1 mit Auto-Recast: nach ~1.2s läuft der Buff ab
            ctx.Manager.RegisterBuffExplicit("Haste", 1, warningSec: 0, autoRecast: true, recastKey: VirtualKey.F5);

            Thread.Sleep(1200);
            ctx.Manager.Update(16);

            Assert.False(ctx.Manager.HasBuff("Haste"));
            Assert.Contains("Haste", ctx.ExpiredBuffs);
            // Auto-Recast feuert den Recast-Key (TapKey → KeyDown)
            Assert.Contains(ctx.Log, c => c.Type == CmdType.KeyDown && c.Key == (ushort)VirtualKey.F5);
        }

        [Fact]
        public void Update_AfterExpiration_FiresDebuffExpired()
        {
            using var ctx = new TestContext();

            ctx.Manager.RegisterDebuff("Slow", 1);

            Thread.Sleep(1200);
            ctx.Manager.Update(16);

            Assert.False(ctx.Manager.HasDebuff("Slow"));
            Assert.Contains("Slow", ctx.ExpiredDebuffs);
        }

        [Fact]
        public void Update_WhileActive_DoesNotExpireBuff()
        {
            using var ctx = new TestContext();
            ctx.Manager.RegisterBuffExplicit("Haste", 60);

            ctx.Manager.Update(16);

            Assert.True(ctx.Manager.HasBuff("Haste"));
            Assert.Empty(ctx.ExpiredBuffs);
        }

        [Fact]
        public void ClearAll_RemovesAllBuffsAndDebuffs()
        {
            using var ctx = new TestContext();
            ctx.Manager.RegisterBuffExplicit("Haste", 60);
            ctx.Manager.RegisterDebuff("Slow", 30);

            ctx.Manager.ClearAll();

            Assert.Empty(ctx.Manager.ActiveBuffNames);
            Assert.Empty(ctx.Manager.ActiveDebuffNames);
        }

        // ── GEX-011: Registry Integration Tests ──

        [Fact]
        public void RegisterBuff_UsesRegistryDefaults_ForKnownBuff()
        {
            using var ctx = new TestContext();

            // "Blessing" is in registry with 240s duration
            ctx.Manager.RegisterBuff("Blessing", recastKey: VirtualKey.F2, autoRecast: true);

            Assert.True(ctx.Manager.HasBuff("Blessing"));
            float remaining = ctx.Manager.GetBuffRemainingSec("Blessing");
            Assert.InRange(remaining, 239f, 240f); // Registry default: 240s
        }

        [Fact]
        public void RegisterBuff_UnknownBuff_UsesFallbackDuration()
        {
            using var ctx = new TestContext();

            // Unknown buff should use fallback (60s)
            ctx.Manager.RegisterBuff("TotallyUnknownBuff123", recastKey: VirtualKey.F1);

            Assert.True(ctx.Manager.HasBuff("TotallyUnknownBuff123"));
            float remaining = ctx.Manager.GetBuffRemainingSec("TotallyUnknownBuff123");
            Assert.InRange(remaining, 59f, 60f);
        }

        [Fact]
        public void RegisterDebuffFromRegistry_UsesRegistryDuration()
        {
            using var ctx = new TestContext();

            // "Poison" is in debuffs with 60s duration
            ctx.Manager.RegisterDebuffFromRegistry("Poison");

            Assert.True(ctx.Manager.HasDebuff("Poison"));
            float remaining = ctx.Manager.GetDebuffRemainingSec("Poison");
            Assert.InRange(remaining, 59f, 60f);
        }

        [Fact]
        public void RegisterBuffExplicit_OverridesRegistryDefaults()
        {
            using var ctx = new TestContext();

            // Blessing normally 240s, but we explicitly set 10s
            ctx.Manager.RegisterBuffExplicit("Blessing", 10, warningSec: 5);

            float remaining = ctx.Manager.GetBuffRemainingSec("Blessing");
            Assert.InRange(remaining, 9f, 10f);
        }
    }
}
