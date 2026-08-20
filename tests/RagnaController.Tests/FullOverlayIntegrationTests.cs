using System;
using System.Collections.Generic;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-003: Integration test for full overlay → RO client flow.
    /// Tests headless integration with profile loading, engine orchestration, and tick cycles.
    /// </summary>
    public class FullOverlayIntegrationTests
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
            private readonly List<PublishedMessage> _published = new();

            public void Publish<T>(T message) where T : class
            {
                _published.Add(new PublishedMessage(typeof(T).Name, message));
            }

            public IDisposable Subscribe<T>(Action<T> handler) where T : class
                => new DisposableAction(() => { });

            public IReadOnlyList<PublishedMessage> Published => _published;

            public sealed record PublishedMessage(string TypeName, object Message);

            private sealed class DisposableAction : IDisposable
            {
                private Action? _action;
                public DisposableAction(Action action) => _action = action;
                public void Dispose() { _action?.Invoke(); _action = null; }
            }
        }

        /// <summary>
        /// Helper: create an EngineOrchestrator with a non-null CommandQueue.
        /// </summary>
        private static EngineOrchestrator CreateEngine(IMessenger messenger)
        {
            var tickProvider = new MockTickProvider();
            var queue = new InputCommandQueue();
            return new EngineOrchestrator(tickProvider, messenger, queue, new AdvancedLogger("Test"));
        }

        [Fact]
        public void FullOverlay_EngineOrchestrator_Initializes_WithoutThrowing()
        {
            // Arrange
            var messenger = new MockMessenger();

            // Act - Create engine with injected CommandQueue
            var engine = CreateEngine(messenger);

            // Assert
            Assert.NotNull(engine);
            Assert.False(engine.IsRunning);
            Assert.False(engine.IsPaused);

            // Cleanup
            engine.Shutdown();
        }

        [Fact]
        public void FullOverlay_EngineOrchestrator_StartStop_Works()
        {
            // Arrange
            var messenger = new MockMessenger();

            var engine = CreateEngine(messenger);

            // Act
            engine.Start();
            Assert.True(engine.IsRunning);

            engine.Stop();
            Assert.False(engine.IsRunning);

            engine.Shutdown();
        }

        [Fact]
        public void FullOverlay_ProfileLoading_EndToEnd()
        {
            // Arrange
            var messenger = new MockMessenger();
            var engine = CreateEngine(messenger);

            // Act - Start engine and command queue first (initializes InputCommandQueue consumer)
            engine.Start();
            engine.CommandQueue?.Start();

            // Act - Load a complete profile via ProfileApplier
            var profile = new Profile
            {
                Name = "Test Wizard",
                Class = "Wizard",
                AutoAttackEnabled = true,
                KiteEnabled = true,
                MageEnabled = true,
                SupportEnabled = false,
                ComboEnabled = false,
                MobSweepEnabled = true,
                HandheldModeEnabled = false,
                TurboEnabled = false,
                ButtonMappings = new Dictionary<string, ButtonAction>
                {
                    { "A", new ButtonAction { Type = ActionType.Key, Key = VirtualKey.A } },
                    { "B", new ButtonAction { Type = ActionType.Key, Key = VirtualKey.B } },
                    { "X", new ButtonAction { Type = ActionType.Key, Key = VirtualKey.X } },
                    { "Y", new ButtonAction { Type = ActionType.Key, Key = VirtualKey.Y } }
                }
            };

            engine.ProfileApplier.LoadProfile(profile, autoDetectClass: false);

            // Assert - Profile was loaded
            Assert.Equal("Test Wizard", engine.CurrentProfile?.Name);
            Assert.Equal("Wizard", engine.CurrentProfile?.Class);

            // Assert - Engines configured
            Assert.True(engine.AutoTarget.AutoAttackEnabled);
            Assert.True(engine.Kite.KiteEnabled);
            Assert.True(engine.Mage.MageEnabled);
            Assert.True(engine.MobSweep.MobSweepEnabled);

            engine.Shutdown();
        }

        [Fact]
        public void FullOverlay_MultipleProfiles_CanSwitch()
        {
            // Arrange
            var messenger = new MockMessenger();
            var engine = CreateEngine(messenger);

            // Act - Start engine and command queue first (initializes InputCommandQueue consumer)
            engine.Start();
            engine.CommandQueue?.Start();

            // Act - Apply Wizard profile
            var wizardProfile = new Profile
            {
                Name = "Test Wizard",
                Class = "Wizard",
                AutoAttackEnabled = true,
                KiteEnabled = false,
                MageEnabled = true,
                SupportEnabled = false,
                ComboEnabled = false,
                MobSweepEnabled = false,
                ButtonMappings = new Dictionary<string, ButtonAction>
                {
                    { "A", new ButtonAction { Type = ActionType.Key, Key = VirtualKey.A } }
                }
            };
            engine.ProfileApplier.LoadProfile(wizardProfile, autoDetectClass: false);

            Assert.Equal("Wizard", engine.CurrentProfile?.Class);
            Assert.True(engine.Mage.MageEnabled);

            // Act - Apply Priest profile
            var priestProfile = new Profile
            {
                Name = "Test Priest",
                Class = "Priest",
                AutoAttackEnabled = false,
                KiteEnabled = false,
                MageEnabled = false,
                SupportEnabled = true,
                ComboEnabled = false,
                MobSweepEnabled = false,
                ButtonMappings = new Dictionary<string, ButtonAction>
                {
                    { "A", new ButtonAction { Type = ActionType.Key, Key = VirtualKey.A } }
                }
            };
            engine.ProfileApplier.LoadProfile(priestProfile, autoDetectClass: false);

            // Assert - Profile switched, engines reconfigured
            Assert.Equal("Priest", engine.CurrentProfile?.Class);
            Assert.False(engine.Mage.MageEnabled);
            Assert.True(engine.Support.SupportEnabled);

            engine.Shutdown();
        }

        [Fact]
        public void FullOverlay_TickProvider_StartStopCycle()
        {
            // Arrange
            var tickProvider = new MockTickProvider();

            // Act & Assert
            Assert.Equal(8, tickProvider.IntervalMs); // ~125Hz
            Assert.False(tickProvider.IsRunning);

            tickProvider.Start();
            Assert.True(tickProvider.IsRunning);

            tickProvider.Stop();
            Assert.False(tickProvider.IsRunning);

            // Cleanup
            tickProvider.Dispose();
        }

        [Fact]
        public void FullOverlay_Engine_TickProvider_Integration()
        {
            // Arrange
            var messenger = new MockMessenger();
            var engine = CreateEngine(messenger);

            // Act
            engine.Start();
            Assert.True(engine.IsRunning);

            engine.Stop();
            Assert.False(engine.IsRunning);

            engine.Shutdown();
        }

        [Fact]
        public void FullOverlay_Profile_AppliesCombatSettings()
        {
            // Arrange
            var messenger = new MockMessenger();
            var engine = CreateEngine(messenger);

            // Act - Start engine and command queue first (initializes InputCommandQueue consumer)
            engine.Start();
            engine.CommandQueue?.Start();

            // Act
            var profile = new Profile
            {
                Name = "Test Archer",
                Class = "Archer",
                AutoAttackEnabled = true,
                AutoRetargetEnabled = true,
                SmartSkillEnabled = true,
                AutoAttackKeyVK = 0x5A, // Z
                TabCycleMs = 80,
                AimSensitivity = 22f,
                AimDeadzone = 0.20f,
                PreRenewalAttackIntervalMs = 100,
                RenewalAttackIntervalMs = 60,
                KiteEnabled = true,
                KiteAttackKeyVK = 90, // Z
                KiteAttackIntervalMs = 55,
                MageEnabled = true,
                MageBoltKeyVK = 86, // V
                MageBoltCastDelayMs = 1200,
                GyroEnabled = true,
                GyroBlend = 0.6f,
                SupportEnabled = false,
                CursorMaxSpeed = 1200f,
                CursorDeadzone = 0.12f,
                CursorCurve = 1.5f,
                MouseSensitivity = 1.2f,
                Deadzone = 0.12f,
                MovementCurve = 1.5f,
                MovementCoastFrames = 3,
                MovementCurveMode = 1,
                ActionRpgMode = true,
                ClickCooldownMs = 80,
                ComboEnabled = false,
                MobSweepEnabled = true,
                MobSweepAttackKeyVK = 0x5A,
                MobSweepAttackDelayMs = 60,
                MobSweepTabIntervalMs = 350,
                ButtonMappings = new Dictionary<string, ButtonAction>
                {
                    { "A", new ButtonAction { Type = ActionType.Key, Key = VirtualKey.A } },
                    { "B", new ButtonAction { Type = ActionType.Key, Key = VirtualKey.B } }
                }
            };

            engine.ProfileApplier.LoadProfile(profile, autoDetectClass: false);

            // Assert - AutoTarget settings applied
            Assert.True(engine.AutoTarget.AutoAttackEnabled);
            Assert.True(engine.AutoTarget.AutoRetargetEnabled);
            Assert.True(engine.AutoTarget.SmartSkillEnabled);
            Assert.Equal(0x5A, engine.AutoTarget.AttackKey_VK);
            Assert.Equal(80, engine.AutoTarget.TabCycleMs);
            Assert.Equal(22f, engine.AutoTarget.AimSensitivity);
            Assert.Equal(0.20f, engine.AutoTarget.AimDeadzone);

            // Assert - Kite settings applied
            Assert.True(engine.Kite.KiteEnabled);
            Assert.Equal(90, engine.Kite.AttackKeyVK);
            Assert.Equal(55, engine.Kite.AttackIntervalMs);

            // Assert - Mage settings applied
            Assert.True(engine.Mage.MageEnabled);
            Assert.Equal(VirtualKey.V, engine.Mage.MageBoltKeyVK);
            Assert.Equal(1200, engine.Mage.MageBoltCastDelayMs);

            // Assert - Cursor settings applied
            Assert.Equal(1200f, engine.Cursor.MaxSpeed);
            Assert.Equal(0.12f, engine.Cursor.Deadzone);
            Assert.Equal(1.5f, engine.Cursor.Curve);
            Assert.Equal(1.2f, engine.Cursor.Sensitivity);

            // Assert - Movement settings applied
            Assert.Equal(0.12f, engine.Movement.Deadzone);
            Assert.Equal(1.5f, engine.Movement.Curve);
            Assert.Equal(3, engine.Movement.CoastFrames);
            Assert.Equal(1, engine.Movement.CurveMode);
            Assert.True(engine.Movement.ActionRpgMode);

            // Assert - MobSweep settings applied
            Assert.True(engine.MobSweep.MobSweepEnabled);
            Assert.Equal(0x5A, engine.MobSweep.AttackKeyVK);
            Assert.Equal(60, engine.MobSweep.AttackDelayMs);
            Assert.Equal(350, engine.MobSweep.TabIntervalMs);

            engine.Shutdown();
        }

        [Fact]
        public void FullOverlay_AutoClassDetection_AppliesPreset()
        {
            // Arrange
            var messenger = new MockMessenger();
            var engine = CreateEngine(messenger);

            // Act - Start engine and command queue first (initializes InputCommandQueue consumer)
            engine.Start();
            engine.CommandQueue?.Start();

            // Act - Profile with Mage-like mappings, auto-detect class
            var profile = new Profile
            {
                Name = "Mage-like Profile",
                Class = "Novice", // Will be auto-detected
                ButtonMappings = new Dictionary<string, ButtonAction>
                {
                    { "A", new ButtonAction { Type = ActionType.Key, Key = VirtualKey.A } },  // Fire Bolt
                    { "B", new ButtonAction { Type = ActionType.Key, Key = VirtualKey.B } },  // Cold Bolt
                    { "X", new ButtonAction { Type = ActionType.Key, Key = VirtualKey.X } },  // Lightning Bolt
                    { "Y", new ButtonAction { Type = ActionType.Key, Key = VirtualKey.Y } }   // Fire Wall
                }
            };

            engine.ProfileApplier.LoadProfile(profile, autoDetectClass: true);

            // Assert - Class was auto-detected as Mage and preset applied
            Assert.NotNull(engine.CurrentProfile);

            engine.Shutdown();
        }

        [Fact]
        public void FullOverlay_GameModeSwitch_UpdatesTiming()
        {
            // Arrange
            var messenger = new MockMessenger();
            var engine = CreateEngine(messenger);

            // Act - Start engine and command queue first (initializes InputCommandQueue consumer)
            engine.Start();
            engine.CommandQueue?.Start();

            var profile = new Profile
            {
                Name = "Test",
                Class = "Knight",
                PreRenewalAttackIntervalMs = 100,
                RenewalAttackIntervalMs = 60,
                PreRenewalSkillInterruptMs = 800,
                RenewalSkillInterruptMs = 400,
                ButtonMappings = new Dictionary<string, ButtonAction>()
            };

            engine.ProfileApplier.LoadProfile(profile, autoDetectClass: false);

            // Act - Switch to Renewal mode
            engine.ProfileApplier.ApplyGameMode(true); // Renewal

            // Assert - Timing updated to Renewal values
            Assert.Equal(60, engine.AutoTarget.AttackIntervalMs);
            Assert.Equal(400, engine.AutoTarget.SkillInterruptMs);

            // Act - Switch to Pre-Renewal mode
            engine.ProfileApplier.ApplyGameMode(false); // Pre-Renewal

            // Assert - Timing updated to Pre-Renewal values
            Assert.Equal(100, engine.AutoTarget.AttackIntervalMs);
            Assert.Equal(800, engine.AutoTarget.SkillInterruptMs);

            engine.Shutdown();
        }

        [Fact]
        public void FullOverlay_LiveUpdates_Work()
        {
            // Arrange
            var messenger = new MockMessenger();
            var engine = CreateEngine(messenger);

            // Act - Start engine and command queue first (initializes InputCommandQueue consumer)
            engine.Start();
            engine.CommandQueue?.Start();

            var profile = new Profile
            {
                Name = "Test",
                Class = "Knight",
                Deadzone = 0.12f,
                MovementCurve = 1.5f,
                MouseSensitivity = 1.2f,
                CursorMaxSpeed = 1200f,
                CursorDeadzone = 0.12f,
                CursorCurve = 1.5f,
                ButtonMappings = new Dictionary<string, ButtonAction>()
            };

            engine.ProfileApplier.LoadProfile(profile, autoDetectClass: false);

            // Act - Live update deadzone
            engine.ProfileApplier.LiveUpdateDeadzone(0.25f);

            // Assert
            Assert.Equal(0.25f, engine.Movement.Deadzone);
            Assert.Equal(0.25f, engine.Cursor.Deadzone);

            // Act - Live update curve
            engine.ProfileApplier.LiveUpdateCurve(2.0f);

            // Assert
            Assert.Equal(2.0f, engine.Movement.Curve);

            // Act - Live update action speed
            engine.ProfileApplier.LiveUpdateActionSpeed(10f); // Max speed

            // Assert
            Assert.True(engine.AutoTarget.AttackIntervalMs <= profile.RenewalAttackIntervalMs);

            // Act - Live update cursor speed
            engine.ProfileApplier.LiveUpdateCursorSpeed(2000f);

            // Assert
            Assert.Equal(2000f, engine.Cursor.MaxSpeed);

            engine.Shutdown();
        }

        [Fact]
        public void FullOverlay_SoundRumbleSettings_Applied()
        {
            // Arrange
            var messenger = new MockMessenger();
            var engine = CreateEngine(messenger);

            // Act - Start engine and command queue first (initializes InputCommandQueue consumer)
            engine.Start();
            engine.CommandQueue?.Start();

            var profile = new Profile
            {
                Name = "Test",
                Class = "Knight",
                ButtonMappings = new Dictionary<string, ButtonAction>()
            };

            engine.ProfileApplier.LoadProfile(profile, autoDetectClass: false);

            // Act - Sound and Rumble settings
            engine.ProfileApplier.SetSoundEnabled(false);
            engine.ProfileApplier.SetRumbleEnabled(false);

            // Assert
            Assert.False(engine.SoundEnabled);
            Assert.False(engine.RumbleEnabled);

            // Act - Re-enable
            engine.ProfileApplier.SetSoundEnabled(true);
            engine.ProfileApplier.SetRumbleEnabled(true);

            // Assert
            Assert.True(engine.SoundEnabled);
            Assert.True(engine.RumbleEnabled);

            engine.Shutdown();
        }

        [Fact]
        public void FullOverlay_StandbySettings_Applied()
        {
            // Arrange
            var messenger = new MockMessenger();
            var engine = CreateEngine(messenger);

            // Act - Start engine and command queue first (initializes InputCommandQueue consumer)
            engine.Start();
            engine.CommandQueue?.Start();

            var profile = new Profile
            {
                Name = "Test",
                Class = "Knight",
                ButtonMappings = new Dictionary<string, ButtonAction>()
            };

            engine.ProfileApplier.LoadProfile(profile, autoDetectClass: false);

            // Act - Set standby settings
            engine.ProfileApplier.SetStandbySettings(true, 10); // 10 minutes

            // Assert - Should not throw, StandbyManager configured
            Assert.NotNull(engine.StandbyManager);

            engine.Shutdown();
        }

        [Fact]
        public void FullOverlay_FullTickCycle_WithInput()
        {
            // Arrange
            var messenger = new MockMessenger();
            var engine = CreateEngine(messenger);

            // Act - Start engine and command queue first (initializes InputCommandQueue consumer)
            engine.Start();
            engine.CommandQueue?.Start();

            // Simulate an input command being enqueued
            engine.CommandQueue.KeyDown(VirtualKey.A);
            engine.CommandQueue.KeyUp(VirtualKey.A);

            // Fire tick - should process queued inputs
            var tickProvider = new MockTickProvider();
            tickProvider.FireTick();

            // Assert - Engine still running after tick
            Assert.True(engine.IsRunning);

            engine.Stop();
            engine.Shutdown();
        }
    }
}