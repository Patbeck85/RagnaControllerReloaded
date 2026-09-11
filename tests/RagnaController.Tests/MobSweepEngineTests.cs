using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-10: MobSweepEngine Unit Tests.
    /// Deckt Aktivierung/Deaktivierung, Phasenlogik (R1 → TargetingParty, Y → Healing),
    /// Cooldown-Gating und Reset ab.
    /// </summary>
    public class MobSweepEngineTests
    {
        /// <summary>
        /// Test-Context: Engine + Queue mit Command-Log (OnCommandEnqueued).
        /// Queue läuft wie in den übrigen Tests über Start/Stop/Dispose.
        /// </summary>
        private sealed class TestContext : IDisposable
        {
            public readonly MobSweepEngine Engine;
            public readonly InputCommandQueue Queue;
            public readonly List<InputCmd> Log = new();

            public TestContext()
            {
                Queue = new InputCommandQueue();
                Queue.OnCommandEnqueued += cmd => Log.Add(cmd);
                Queue.Start();
                Engine = new MobSweepEngine(Queue);
            }

            public void Dispose()
            {
                Queue.Stop();
                Queue.Dispose();
            }

            public List<InputCmd> KeyDowns(VirtualKey key)
                => Log.Where(c => c.Type == CmdType.KeyDown && c.Key == (ushort)key).ToList();
        }

        [Fact]
        public void MobSweep_InitialState_HasExpectedDefaults()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;

            Assert.False(engine.MobSweepEnabled);
            Assert.False(engine.IsActive);
            Assert.Equal(SupportPhase.Idle, engine.Phase);
            Assert.Equal(0, engine.TabCooldown);
            Assert.Equal(0, engine.HealCooldown);
            Assert.Equal((int)VirtualKey.F1, engine.AttackKeyVK);
            Assert.Equal(300, engine.AttackDelayMs);
            Assert.Equal(500, engine.TabIntervalMs);
        }

        [Fact]
        public void Activate_Deactivate_TogglesState()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;

            engine.Activate();
            Assert.True(engine.MobSweepEnabled);
            Assert.True(engine.IsActive);

            engine.Deactivate();
            Assert.False(engine.MobSweepEnabled);
            Assert.False(engine.IsActive);
            Assert.Equal(SupportPhase.Idle, engine.Phase);
        }

        [Fact]
        public void Handle_WhenDisabled_DoesNothing()
        {
            using var ctx = new TestContext();

            bool handled = ctx.Engine.Handle(new ParsedInput { R1 = true, BtnY = true }, 16);

            Assert.False(handled);
            Assert.Empty(ctx.Log);
        }

        [Fact]
        public void Handle_R1_ActivatesTargetingParty_AndFiresAttackKey()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;
            engine.Activate();

            engine.Handle(new ParsedInput { R1 = true }, 16);

            Assert.Equal(SupportPhase.TargetingParty, engine.Phase);
            Assert.Equal(engine.AttackDelayMs, engine.HealCooldown);
            Assert.Single(ctx.KeyDowns((VirtualKey)engine.AttackKeyVK));
        }

        [Fact]
        public void Handle_R1_DuringHealCooldown_DoesNotRefire()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;
            engine.Activate();

            engine.Handle(new ParsedInput { R1 = true }, 16);
            int firedAfterFirst = ctx.KeyDowns((VirtualKey)engine.AttackKeyVK).Count;

            // healCooldown läuft noch → kein zweiter Attack-Key
            engine.Handle(new ParsedInput { R1 = true }, 16);

            Assert.Equal(firedAfterFirst, ctx.KeyDowns((VirtualKey)engine.AttackKeyVK).Count);
        }

        [Fact]
        public void Handle_AfterR1Activation_NextTick_FiresTabAndReturnsToIdle()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;
            engine.Activate();

            // Tick 1: R1 aktiviert → TargetingParty
            engine.Handle(new ParsedInput { R1 = true }, 16);
            Assert.Equal(SupportPhase.TargetingParty, engine.Phase);

            // Tick 2: Tab-Cooldown ist 0 → Tab-Tap wird gefeuert, Phase zurück zu Idle
            engine.Handle(new ParsedInput(), 16);

            Assert.Equal(SupportPhase.Idle, engine.Phase);
            Assert.Single(ctx.KeyDowns(VirtualKey.Tab));
        }

        [Fact]
        public void Handle_BtnY_FiresHealKey_AndSetsTabCooldown()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;
            engine.Activate();

            engine.Handle(new ParsedInput { BtnY = true }, 16);

            Assert.Equal(SupportPhase.Healing, engine.Phase);
            Assert.Equal(engine.TabIntervalMs, engine.TabCooldown);
            Assert.Single(ctx.KeyDowns((VirtualKey)32)); // Space = Heal-Key
        }

        [Fact]
        public void Handle_BtnY_DuringTabCooldown_DoesNotRefire()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;
            engine.Activate();

            engine.Handle(new ParsedInput { BtnY = true }, 16);
            int firedAfterFirst = ctx.KeyDowns((VirtualKey)32).Count;

            // tabCooldown läuft noch → kein zweiter Heal-Key
            engine.Handle(new ParsedInput { BtnY = true }, 16);

            Assert.Equal(firedAfterFirst, ctx.KeyDowns((VirtualKey)32).Count);
        }

        [Fact]
        public void Update_DecrementsCooldowns_ByDelta()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;
            engine.Activate();

            engine.Handle(new ParsedInput { R1 = true }, 0);   // healCooldown = AttackDelayMs
            int healBefore = engine.HealCooldown;

            engine.Update(100);

            Assert.Equal(Math.Max(0, healBefore - 100), engine.HealCooldown);
        }

        [Fact]
        public void Update_ClampsCooldownsAtZero()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;
            engine.Activate();

            engine.Handle(new ParsedInput { R1 = true }, 0);
            engine.Handle(new ParsedInput { BtnY = true }, 0);

            engine.Update(10_000);

            Assert.Equal(0, engine.HealCooldown);
            Assert.Equal(0, engine.TabCooldown);
        }

        [Fact]
        public void Reset_RestoresInitialState()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;
            engine.Activate();
            engine.Handle(new ParsedInput { R1 = true }, 0);

            engine.Reset();

            Assert.False(engine.MobSweepEnabled);
            Assert.False(engine.IsActive);
            Assert.Equal(SupportPhase.Idle, engine.Phase);
            Assert.Equal(0, engine.TabCooldown);
            Assert.Equal(0, engine.HealCooldown);
        }
    }
}
