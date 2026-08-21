using System;
using System.Collections.Generic;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;
using RagnaController.Controller;

namespace RagnaController.Tests
{
    /// <summary>
    /// POLISH-003: Integration test scaffold with mocked RO window.
    /// Tests core engine orchestration paths headlessly.
    /// Target: >90% stability across multiple runs.
    /// </summary>
    public class EngineIntegrationTests
    {
        private sealed class MockTickProvider : ITickProvider
        {
            public int IntervalMs { get; } = 8; // ~125Hz
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
            public IDisposable Subscribe<T>(Action<T> handler) where T : class => new DisposableAction(() => { });
            
            private sealed class DisposableAction : IDisposable
            {
                private Action? _action;
                public DisposableAction(Action action) => _action = action;
                public void Dispose() { _action?.Invoke(); _action = null; }
            }
        }

        [Fact]
        public void EngineOrchestrator_Initializes_WithoutThrowing()
        {
            var tickProvider = new MockTickProvider();
            var messenger = new MockMessenger();
            var queue = new InputCommandQueue();
            var logger = new AdvancedLogger("IntegrationTest");

            // This tests if the engine can be constructed with real WindowTracker and ControllerService
            var engine = new EngineOrchestrator(tickProvider, messenger, queue, logger);

            Assert.NotNull(engine);
            Assert.False(engine.IsRunning);
            Assert.False(engine.IsPaused);

            // Let engine dispose the logger via Shutdown()
            engine.Shutdown();
        }

        [Fact]
        public void EngineOrchestrator_StartStop_Works()
        {
            var tickProvider = new MockTickProvider();
            var messenger = new MockMessenger();
            var queue = new InputCommandQueue();
            var logger = new AdvancedLogger("IntegrationTest");

            var engine = new EngineOrchestrator(tickProvider, messenger, queue, logger);

            engine.Start();
            Assert.True(engine.IsRunning);

            engine.Stop();
            Assert.False(engine.IsRunning);

            engine.Shutdown();
        }

        [Fact]
        public void EngineOrchestrator_PauseResume_Works()
        {
            var tickProvider = new MockTickProvider();
            var messenger = new MockMessenger();
            var queue = new InputCommandQueue();
            var logger = new AdvancedLogger("IntegrationTest");

            var engine = new EngineOrchestrator(tickProvider, messenger, queue, logger);

            engine.Start();
            engine.Pause();
            Assert.True(engine.IsPaused);

            engine.Resume();
            Assert.False(engine.IsPaused);

            engine.Stop();
            engine.Shutdown();
        }

        [Fact]
        public void EngineOrchestrator_ProfileApplier_CanBeAccessed()
        {
            var tickProvider = new MockTickProvider();
            var messenger = new MockMessenger();
            var queue = new InputCommandQueue();
            var logger = new AdvancedLogger("IntegrationTest");

            var engine = new EngineOrchestrator(tickProvider, messenger, queue, logger);

            // ProfileApplier should be accessible
            Assert.NotNull(engine.ProfileApplier);

            engine.Shutdown();
        }

        [Fact]
        public void EngineOrchestrator_CommandQueue_CanBeAccessed()
        {
            var tickProvider = new MockTickProvider();
            var messenger = new MockMessenger();
            var queue = new InputCommandQueue();
            var logger = new AdvancedLogger("IntegrationTest");

            var engine = new EngineOrchestrator(tickProvider, messenger, queue, logger);

            // Queue should be accessible
            Assert.NotNull(engine.CommandQueue);

            engine.Shutdown();
        }

        [Fact]
        public void EngineOrchestrator_MultipleCycles_StabilityTest()
        {
            // Run multiple start/stop cycles to verify stability
            for (int cycle = 0; cycle < 5; cycle++)
            {
                var tickProvider = new MockTickProvider();
                var messenger = new MockMessenger();
                var queue = new InputCommandQueue();
                var logger = new AdvancedLogger($"IntegrationTest_Cycle{cycle}");

                var engine = new EngineOrchestrator(tickProvider, messenger, queue, logger);

                engine.Start();
                Assert.True(engine.IsRunning);

                engine.Pause();
                Assert.True(engine.IsPaused);

                engine.Resume();
                Assert.False(engine.IsPaused);

                engine.Stop();
                Assert.False(engine.IsRunning);

                engine.Shutdown();
            }

            // If we reach here without exceptions, stability is confirmed
            Assert.True(true);
        }

        [Fact]
        public void EngineOrchestrator_ExposesEngines_ForProfileApplier()
        {
            var tickProvider = new MockTickProvider();
            var messenger = new MockMessenger();
            var queue = new InputCommandQueue();
            var logger = new AdvancedLogger("IntegrationTest");

            var engine = new EngineOrchestrator(tickProvider, messenger, queue, logger);

            // Verify all engines are exposed
            Assert.NotNull(engine.Movement);
            Assert.NotNull(engine.Combat);
            Assert.NotNull(engine.AutoTarget);
            Assert.NotNull(engine.Mage);
            Assert.NotNull(engine.Combo);
            Assert.NotNull(engine.Cursor);
            Assert.NotNull(engine.SmartCursor);
            Assert.NotNull(engine.Kite);
            Assert.NotNull(engine.Support);
            Assert.NotNull(engine.Feedback);
            Assert.NotNull(engine.OverlayRouter);
            Assert.NotNull(engine.MobSweep);
            Assert.NotNull(engine.Handheld);
            Assert.NotNull(engine.CooldownManager);
            Assert.NotNull(engine.DualSense);
            Assert.NotNull(engine.SysMonitor);
            Assert.NotNull(engine.Snapshot);
            Assert.NotNull(engine.Controller);
            Assert.NotNull(engine.WinTracker);
            Assert.NotNull(engine.StandbyManager);
            Assert.NotNull(engine.InputRouter);
            Assert.NotNull(engine.ProfileApplier);

            engine.Shutdown();
        }

        [Fact]
                public void EngineOrchestrator_SnapshotBuilder_CanBuild()
                {
                    var tickProvider = new MockTickProvider();
                    var messenger = new MockMessenger();
                    var queue = new InputCommandQueue();
                    var logger = new AdvancedLogger("IntegrationTest");

                    var engine = new EngineOrchestrator(tickProvider, messenger, queue, logger);

                    // SnapshotBuilder should be able to build a snapshot
                    var input = new ParsedInput { IsConnected = true };
                    var snapshot = engine.Snapshot.Build(input, false, 8.0);

                    Assert.NotNull(snapshot);

                    engine.Shutdown();
                }

                [Fact]
                public void GroundSpellEngine_RegisterAndUpdate_SpellsAreTracked()
                {
                    var queue = new InputCommandQueue();
                    var engine = new GroundSpellEngine(queue);

                    // Register a ground spell
                    var action = new ButtonAction
                    {
                        IsGroundSpell = true,
                        GroundSpellDurationSec = 10,
                        GroundSpellTickIntervalMs = 1000,
                        GroundSpellRadius = 3f,
                        GroundSpellIsHealing = true,
                        Label = "Heal Circle"
                    };
                    engine.RegisterGroundSpell(action, 100f, 200f, "Heal Circle");

                    // Update with 500ms - spell should still be active
                    var input = new ParsedInput { IsConnected = true };
                    engine.Handle(input, 500);

                    var spells = engine.GetActiveSpells();
                    Assert.Single(spells);
                    Assert.Equal("Heal Circle", spells[0].SkillName);
                    Assert.Equal(100f, spells[0].WorldX);
                    Assert.Equal(200f, spells[0].WorldY);
                    Assert.False(spells[0].IsExpired);

                    // Update with 10 more seconds - spell should expire
                    engine.Handle(input, 10000);

                    spells = engine.GetActiveSpells();
                    Assert.Empty(spells);

                    engine.ClearAll();
                }

                [Fact]
                public void GroundSpellEngine_TickEvent_FiresAtInterval()
                {
                    var queue = new InputCommandQueue();
                    var engine = new GroundSpellEngine(queue);

                    int tickCount = 0;
                    engine.GroundSpellTick += spell => tickCount++;

                    var action = new ButtonAction
                    {
                        IsGroundSpell = true,
                        GroundSpellDurationSec = 5,
                        GroundSpellTickIntervalMs = 1000, // 1 second
                        GroundSpellRadius = 2f,
                        GroundSpellIsHealing = false,
                        Label = "Fire Wall"
                    };
                    engine.RegisterGroundSpell(action, 50f, 50f, "Fire Wall");

                    var input = new ParsedInput { IsConnected = true };

                    // 500ms - no tick yet
                    engine.Handle(input, 500);
                    Assert.Equal(0, tickCount);

                    // Another 600ms - should fire first tick at ~1000ms total
                    engine.Handle(input, 600);
                    Assert.Equal(1, tickCount);

                    // Another 1000ms - should fire second tick
                    engine.Handle(input, 1000);
                    Assert.Equal(2, tickCount);

                    engine.ClearAll();
                }

                [Fact]
                public void GroundSpellEngine_ClearAll_RemovesAllSpells()
                {
                    var queue = new InputCommandQueue();
                    var engine = new GroundSpellEngine(queue);

                    var action = new ButtonAction
                    {
                        IsGroundSpell = true,
                        GroundSpellDurationSec = 30,
                        GroundSpellTickIntervalMs = 1000,
                        GroundSpellRadius = 3f,
                        GroundSpellIsHealing = true,
                        Label = "Sanctuary"
                    };
                    engine.RegisterGroundSpell(action, 0f, 0f, "Sanctuary");

                    var input = new ParsedInput { IsConnected = true };
                    engine.Handle(input, 100);
                    Assert.Single(engine.GetActiveSpells());

                    engine.ClearAll();
                    Assert.Empty(engine.GetActiveSpells());
                }
            }
        }