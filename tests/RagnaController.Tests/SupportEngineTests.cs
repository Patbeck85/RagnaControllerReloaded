using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-010: SupportEngine Unit Tests.
    /// Deckt Initialzustand, Party-Tab (R1) mit/ohne Control-Modifier,
    /// Heal (BtnY), Cooldown-Gating und Reset ab.
    /// </summary>
    public class SupportEngineTests
    {
        private sealed class TestContext : IDisposable
        {
            public readonly SupportEngine Engine;
            public readonly InputCommandQueue Queue;
            public readonly List<InputCmd> Log = new();

            public TestContext(bool activate = true)
            {
                Queue = new InputCommandQueue();
                Queue.OnCommandEnqueued += cmd => Log.Add(cmd);
                Queue.Start();
                Engine = new SupportEngine(Queue);
                Engine.SupportEnabled = true;
                if (activate) Engine.ToggleSupportMode(); // IsActive = true
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
        public void SupportEngine_InitialState_HasExpectedDefaults()
        {
            var engine = new SupportEngine(new InputCommandQueue());

            Assert.False(engine.SupportEnabled);
            Assert.False(engine.IsActive);
            Assert.Equal(SupportPhase.Idle, engine.Phase);
            Assert.Equal(VirtualKey.F1, engine.HealKeyVK);
            Assert.False(engine.PartyTabCycle);
            Assert.Equal(0, engine.TabCooldown);
            Assert.Equal(0, engine.HealCooldown);
            Assert.Equal(50, engine.Priority);
        }

        [Fact]
        public void Handle_WhenInactive_DoesNothing()
        {
            using var ctx = new TestContext(activate: false);

            bool handled = ctx.Engine.Handle(new ParsedInput { R1 = true, BtnY = true }, 16);

            Assert.False(handled);
            Assert.Empty(ctx.Log);
        }

        [Fact]
        public void Handle_R1_FiresTab_AndSetsTargetingPartyPhase()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;

            engine.Handle(new ParsedInput { R1 = true }, 16);

            Assert.Equal(SupportPhase.TargetingParty, engine.Phase);
            Assert.Equal(300, engine.TabCooldown);
            Assert.Single(ctx.KeyDowns(VirtualKey.Tab));
        }

        [Fact]
        public void Handle_R1_WithPartyTabCycle_FiresControlPlusTab()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;
            engine.PartyTabCycle = true;

            engine.Handle(new ParsedInput { R1 = true }, 16);

            Assert.Single(ctx.KeyDowns(VirtualKey.ControlLeft));
            Assert.Single(ctx.KeyDowns(VirtualKey.Tab));
        }

        [Fact]
        public void Handle_R1_DuringTabCooldown_DoesNotRefire()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;

            engine.Handle(new ParsedInput { R1 = true }, 16);
            int firedAfterFirst = ctx.KeyDowns(VirtualKey.Tab).Count;

            // tabCooldown läuft noch (300 - 16 > 0) → kein zweiter Tab-Tap
            engine.Handle(new ParsedInput { R1 = true }, 16);

            Assert.Equal(firedAfterFirst, ctx.KeyDowns(VirtualKey.Tab).Count);
        }

        [Fact]
        public void Handle_BtnY_FiresHealKey_AndSetsHealingPhase()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;

            engine.Handle(new ParsedInput { BtnY = true }, 16);

            Assert.Equal(SupportPhase.Healing, engine.Phase);
            Assert.Equal(800, engine.HealCooldown);
            Assert.Single(ctx.KeyDowns(VirtualKey.F1)); // Default-HealKeyVK
        }

        [Fact]
        public void Handle_BtnY_DuringHealCooldown_DoesNotRefire()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;

            engine.Handle(new ParsedInput { BtnY = true }, 16);
            int firedAfterFirst = ctx.KeyDowns(VirtualKey.F1).Count;

            // healCooldown läuft noch → kein zweiter Heal-Key
            engine.Handle(new ParsedInput { BtnY = true }, 16);

            Assert.Equal(firedAfterFirst, ctx.KeyDowns(VirtualKey.F1).Count);
        }

        [Fact]
        public void ToggleSupportMode_Deactivate_ResetsPhaseToIdle()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;

            engine.Handle(new ParsedInput { R1 = true }, 16);
            Assert.Equal(SupportPhase.TargetingParty, engine.Phase);

            engine.ToggleSupportMode();

            Assert.False(engine.IsActive);
            Assert.Equal(SupportPhase.Idle, engine.Phase);
        }

        [Fact]
        public void Reset_RestoresInitialState()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;
            engine.PartyTabCycle = true;
            engine.Handle(new ParsedInput { R1 = true, BtnY = true }, 0);

            engine.Reset();

            Assert.False(engine.SupportEnabled);
            Assert.False(engine.IsActive);
            Assert.Equal(SupportPhase.Idle, engine.Phase);
            Assert.Equal(0, engine.TabCooldown);
            Assert.Equal(0, engine.HealCooldown);
        }
    }
}
