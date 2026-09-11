using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-010: MageEngine Unit Tests.
    /// Deckt Initialzustand, Bolt-Spam (R2) mit Cooldown-Gating,
    /// Idle-Rückkehr, Gyro-Injection und Reset ab.
    /// Hinweis: SmartCursorService bleibt null — die Engine nutzt ?. auf dem
    /// Cursor-Service, daher ist das sicher (wie der parametrische Konstruktor).
    /// </summary>
    public class MageEngineTests
    {
        private sealed class TestContext : IDisposable
        {
            public readonly MageEngine Engine;
            public readonly InputCommandQueue Queue;
            public readonly List<InputCmd> Log = new();

            public TestContext()
            {
                Queue = new InputCommandQueue();
                Queue.OnCommandEnqueued += cmd => Log.Add(cmd);
                Queue.Start();
                Engine = new MageEngine(Queue, null!); // null-safe: Engine nutzt ?.-Zugriff
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
        public void MageEngine_InitialState_HasExpectedDefaults()
        {
            var engine = new MageEngine();

            Assert.False(engine.MageEnabled);
            Assert.False(engine.IsActive);
            Assert.Equal(MagePhase.Idle, engine.Phase);
            Assert.Equal(VirtualKey.F1, engine.MageBoltKeyVK);
            Assert.Equal(1200, engine.MageBoltCastDelayMs);
            Assert.False(engine.GyroAimEnabled);
            Assert.Equal(40, engine.Priority);
        }

        [Fact]
        public void Handle_WhenInactive_DoesNothing()
        {
            using var ctx = new TestContext();

            bool handled = ctx.Engine.Handle(new ParsedInput { R2 = true }, 16);

            Assert.False(handled);
            Assert.Empty(ctx.Log);
        }

        [Fact]
        public void Handle_R2_FiresBoltKey_AndSetsBoltSpammingPhase()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;
            engine.MageEnabled = true;
            engine.ToggleMageMode(); // IsActive = true

            engine.Handle(new ParsedInput { R2 = true }, 16);

            Assert.Equal(MagePhase.BoltSpamming, engine.Phase);
            Assert.Single(ctx.KeyDowns(VirtualKey.F1)); // Default-Bolt-Key
        }

        [Fact]
        public void Handle_R2_DuringCastCooldown_DoesNotRefire()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;
            engine.MageEnabled = true;
            engine.ToggleMageMode();

            engine.Handle(new ParsedInput { R2 = true }, 16);
            int firedAfterFirst = ctx.KeyDowns(VirtualKey.F1).Count;

            // Cast-Cooldown (~1200±50ms) läuft noch → kein zweiter Bolt-Cast
            engine.Handle(new ParsedInput { R2 = true }, 16);

            Assert.Equal(firedAfterFirst, ctx.KeyDowns(VirtualKey.F1).Count);
        }

        [Fact]
        public void Handle_R2Released_ReturnsToIdlePhase()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;
            engine.MageEnabled = true;
            engine.ToggleMageMode();

            engine.Handle(new ParsedInput { R2 = true }, 16);
            Assert.Equal(MagePhase.BoltSpamming, engine.Phase);

            engine.Handle(new ParsedInput(), 16);

            Assert.Equal(MagePhase.Idle, engine.Phase);
        }

        [Fact]
        public void InjectGyroDelta_WhenGyroDisabled_DoesNotMoveMouse()
        {
            using var ctx = new TestContext();
            // GyroAimEnabled ist default false

            ctx.Engine.InjectGyroDelta(10, -5);

            Assert.DoesNotContain(ctx.Log, c => c.Type == CmdType.MouseRel);
        }

        [Fact]
        public void InjectGyroDelta_WhenGyroEnabled_MovesMouseRelative()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;
            engine.GyroAimEnabled = true;
            engine.GyroSensitivity = 1.0f;

            engine.InjectGyroDelta(10, -5);

            Assert.Contains(ctx.Log, c => c.Type == CmdType.MouseRel && c.X == 10 && c.Y == -5);
        }

        [Fact]
        public void Reset_RestoresInitialState()
        {
            using var ctx = new TestContext();
            var engine = ctx.Engine;
            engine.MageEnabled = true;
            engine.GyroAimEnabled = true;
            engine.ToggleMageMode();
            engine.Handle(new ParsedInput { R2 = true }, 16);

            engine.Reset();

            Assert.False(engine.MageEnabled);
            Assert.False(engine.IsActive);
            Assert.Equal(MagePhase.Idle, engine.Phase);
        }
    }
}
