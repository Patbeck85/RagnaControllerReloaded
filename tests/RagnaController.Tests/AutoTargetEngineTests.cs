using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// FEAT-013: AutoTargetEngine Unit-Tests — Target-Management.
    /// Deckt alle Zustandsübergänge ab: Tab mit/ohne Lock, Lock-Persistenz über
    /// Skill-Interrupts, Tod-/Reichweitenverlust → Auto-Retarget (LostTarget-Event),
    /// Sticky vs. Nearest, und Reset.
    /// </summary>
    public class AutoTargetEngineTests
    {
        private sealed class TestContext : IDisposable
        {
            public readonly AutoTargetEngine Engine;
            public readonly InputCommandQueue Queue;
            public readonly List<InputCmd> Log = new();

            public TestContext()
            {
                Queue = new InputCommandQueue();
                Queue.OnCommandEnqueued += cmd => Log.Add(cmd);
                Queue.Start();
                Engine = new AutoTargetEngine(Queue);
            }

            public void Dispose()
            {
                Queue.Stop();
                Queue.Dispose();
            }

            public int KeyDowns(VirtualKey key)
                => Log.Count(c => c.Type == CmdType.KeyDown && c.Key == (ushort)key);

            public int RightClicks()
                => Log.Count(c => c.Type == CmdType.RightDown);
        }

        [Fact]
        public void InitialState_HasExpectedDefaults()
        {
            using var ctx = new TestContext();
            var e = ctx.Engine;

            Assert.True(e.AutoAttackEnabled);
            Assert.True(e.AutoRetargetEnabled);
            Assert.Equal(TargetingMode.Sticky, e.TargetingMode);
            Assert.Equal(15f, e.MaxTargetDistance);
            Assert.False(e.TargetDataValid);
            Assert.False(e.IsTargetLocked);
            Assert.Equal(CombatState.Idle, e.State);
        }

        [Fact]
        public void Tab_WithLock_NearestMode_CyclesTarget()
        {
            using var ctx = new TestContext();
            var e = ctx.Engine;
            e.TargetingMode = TargetingMode.Nearest;

            e.OnTargetLocked();
            e.Handle(new ParsedInput(), 16); // Engaged → Attacking
            int before = ctx.KeyDowns(VirtualKey.Tab);

            // Nearest zykliert im Attacking per Tab durch Ziele.
            e.Handle(new ParsedInput(), 100);

            Assert.True(ctx.KeyDowns(VirtualKey.Tab) > before,
                "Nearest-Modus muss im Attacking-State per Tab zyklisieren.");
        }

        [Fact]
        public void Tab_WithLock_StickyMode_DoesNotCycle()
        {
            using var ctx = new TestContext();
            var e = ctx.Engine;
            e.TargetingMode = TargetingMode.Sticky;

            e.OnTargetLocked();
            e.Handle(new ParsedInput(), 16); // Engaged → Attacking
            int tabsAfterEngage = ctx.KeyDowns(VirtualKey.Tab);

            // Sticky hält das Ziel: kein Tab-Cycling im Attacking.
            e.Handle(new ParsedInput(), 100);
            e.Handle(new ParsedInput(), 100);

            Assert.Equal(tabsAfterEngage, ctx.KeyDowns(VirtualKey.Tab));
        }

        [Fact]
        public void Lock_PersistsAcrossSkillInterrupt()
        {
            using var ctx = new TestContext();
            var e = ctx.Engine;

            e.OnTargetLocked();
            Assert.True(e.IsTargetLocked);

            // Skill-Interrupt pausiert nur den Angriff, nicht das Ziel-Lock.
            e.NotifySkillFired();

            e.Handle(new ParsedInput(), 16);

            Assert.True(e.IsTargetLocked,
                "Lock muss einen Skill-Interrupt überstehen (DoD).");
        }

        [Fact]
        public void TargetDeath_FiresLostTarget_AndRetargets()
        {
            using var ctx = new TestContext();
            var e = ctx.Engine;
            string? reason = null;
            e.LostTarget += r => reason = r;

            e.OnTargetLocked();
            e.SetTarget("Poring", 5f);
            e.Handle(new ParsedInput(), 16); // Engaged → Attacking

            // Ziel stirbt: leerer Name, Datenquelle liefert weiterhin.
            e.SetTarget("", 0f);
            e.Handle(new ParsedInput(), 16);

            Assert.Equal("dead", reason);
            Assert.False(e.IsTargetLocked);
            Assert.Equal(CombatState.Seeking, e.State);
        }

        [Fact]
        public void TargetOutOfRange_FiresLostTarget_AndRetargets()
        {
            using var ctx = new TestContext();
            var e = ctx.Engine;
            string? reason = null;
            e.LostTarget += r => reason = r;

            e.OnTargetLocked();
            e.SetTarget("Poring", 5f);
            e.Handle(new ParsedInput(), 16); // Engaged → Attacking

            // Ziel läuft aus der Reichweite.
            e.SetTarget("Poring", 99f);
            e.Handle(new ParsedInput(), 16);

            Assert.Equal("out_of_range", reason);
            Assert.False(e.IsTargetLocked);
            Assert.Equal(CombatState.Seeking, e.State);
        }

        [Fact]
        public void NoFeed_NoFalsePositive_LostTarget()
        {
            using var ctx = new TestContext();
            var e = ctx.Engine;
            string? reason = null;
            e.LostTarget += r => reason = r;

            // Lock OHNE Datenquelle: TargetDataValid=false → keine falschen Positives.
            e.OnTargetLocked();
            e.Handle(new ParsedInput(), 16);
            e.Handle(new ParsedInput(), 100);

            Assert.Null(reason);
            Assert.True(e.IsTargetLocked);
        }

        [Fact]
        public void ClearTarget_DisablesValidityCheck()
        {
            using var ctx = new TestContext();
            var e = ctx.Engine;
            string? reason = null;
            e.LostTarget += r => reason = r;

            e.OnTargetLocked();
            e.SetTarget("Poring", 5f);
            e.ClearTarget(); // Datenquelle liefert nichts mehr.

            Assert.False(e.TargetDataValid);

            // Leeres TargetName wird NICHT als Tod gewertet (keine falschen Positives).
            e.Handle(new ParsedInput(), 16);

            Assert.Null(reason);
            Assert.True(e.IsTargetLocked);
        }

        [Fact]
        public void Reset_RestoresFeAT013Fields()
        {
            using var ctx = new TestContext();
            var e = ctx.Engine;

            e.OnTargetLocked();
            e.TargetingMode = TargetingMode.Nearest;
            e.MaxTargetDistance = 42f;
            e.SetTarget("Poring", 5f);

            e.Reset();

            Assert.Equal(TargetingMode.Sticky, e.TargetingMode);
            Assert.Equal(15f, e.MaxTargetDistance);
            Assert.False(e.TargetDataValid);
            Assert.False(e.IsTargetLocked);
            Assert.Equal(CombatState.Idle, e.State);
        }
    }
}
