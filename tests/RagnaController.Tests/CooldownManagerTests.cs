using System;
using System.Collections.Generic;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-010: CooldownManager Unit Tests.
    /// Deckt RegisterAction (TrackBuff-Gating), Warning-Fire mit Messenger-Publish,
    /// Einmal-Semantik (kein Re-Fire) und ResetAll ab.
    /// Hinweis: Warnzeitpunkt ist wall-clock-basiert (Environment.TickCount64),
    /// minimale Warnverzögerung = 1000ms (Sanity-Clamp) → Tests mit ~1.2s Wait.
    /// </summary>
    public class CooldownManagerTests
    {
        private sealed class RecordingMessenger : IMessenger
        {
            public readonly List<BuffWarningMessage> BuffWarnings = new();

            public void Publish<T>(T message) where T : class
            {
                if (message is BuffWarningMessage bwm)
                    BuffWarnings.Add(bwm);
            }

            public IDisposable Subscribe<T>(Action<T> handler) where T : class => NoOpDisposable.Instance;
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public static readonly NoOpDisposable Instance = new();
            public void Dispose() { }
        }

        private sealed class RecordingFeedback : IFeedbackProvider
        {
            public readonly List<FeedbackType> Triggers = new();
            public void StopAll() { }
            public void SetLED(byte r, byte g, byte b) { }
            public void Tick() { }
            public void Trigger(FeedbackType type) => Triggers.Add(type);
            public void TriggerSkillFired() { }
            public void StopRumble() { }
            public void Dispose() { }
        }

        private sealed class TestContext : IDisposable
        {
            public readonly CooldownManager Manager;
            public readonly RecordingMessenger Messenger = new();
            public readonly RecordingFeedback Feedback = new();

            public TestContext()
            {
                Manager = new CooldownManager(Messenger, Feedback);
            }

            public void Dispose() => Manager.ResetAll();
        }

        private static ButtonAction Tracked(string label, int durationSec, int warningSec)
            => new()
            {
                Label = label,
                TrackBuff = true,
                BuffDurationSec = durationSec,
                BuffWarningSec = warningSec
            };

        [Fact]
        public void RegisterAction_WithoutTrackBuff_IsIgnored()
        {
            using var ctx = new TestContext();

            ctx.Manager.RegisterAction(new ButtonAction { Label = "NoTrack", TrackBuff = false });
            ctx.Manager.Tick();

            Assert.Empty(ctx.Messenger.BuffWarnings);
            Assert.Empty(ctx.Feedback.Triggers);
        }

        [Fact]
        public void Tick_BeforeWarningThreshold_DoesNotFire()
        {
            using var ctx = new TestContext();

            // Warnzeitpunkt = jetzt + (2-1)*1000ms → sofortiges Tick ist zu früh
            ctx.Manager.RegisterAction(Tracked("Haste", 2, 1));
            ctx.Manager.Tick();

            Assert.Empty(ctx.Messenger.BuffWarnings);
        }

        [Fact]
        public void Tick_AfterWarningThreshold_FiresOnce_WithLabel_AndFeedback()
        {
            using var ctx = new TestContext();

            ctx.Manager.RegisterAction(Tracked("Haste", 2, 1));
            Thread.Sleep(1200);

            ctx.Manager.Tick();

            Assert.Single(ctx.Messenger.BuffWarnings);
            Assert.Equal("Haste", ctx.Messenger.BuffWarnings[0].ActionLabel);
            Assert.Contains(FeedbackType.BuffWarning, ctx.Feedback.Triggers);
        }

        [Fact]
        public void Tick_AfterFire_DoesNotRefire()
        {
            using var ctx = new TestContext();

            ctx.Manager.RegisterAction(Tracked("Haste", 2, 1));
            Thread.Sleep(1200);
            ctx.Manager.Tick();
            int firedAfterFirst = ctx.Messenger.BuffWarnings.Count;

            // Tracker-Eintrag wurde entfernt → zweites Tick darf nichts feuern
            ctx.Manager.Tick();

            Assert.Equal(firedAfterFirst, ctx.Messenger.BuffWarnings.Count);
        }

        [Fact]
        public void ResetAll_ClearsPendingTrackers()
        {
            using var ctx = new TestContext();

            ctx.Manager.RegisterAction(Tracked("Haste", 2, 1));
            ctx.Manager.ResetAll();
            Thread.Sleep(1200);
            ctx.Manager.Tick();

            Assert.Empty(ctx.Messenger.BuffWarnings);
        }

        [Fact]
        public void RegisterAction_SameLabel_OverwritesTimer()
        {
            using var ctx = new TestContext();

            // Erster Timer läuft fast ab …
            ctx.Manager.RegisterAction(Tracked("Haste", 2, 1));
            Thread.Sleep(900);

            // … dann Re-Cast: Timer wird zurückgesetzt (neue Warnzeit +1000ms)
            ctx.Manager.RegisterAction(Tracked("Haste", 2, 1));
            ctx.Manager.Tick();

            // Nur ~100ms nach Re-Register → noch keine Warnung
            Assert.Empty(ctx.Messenger.BuffWarnings);
        }
    }
}
