using System;
using System.Collections.Generic;
using RagnaController.Models;

namespace RagnaController.Core
{
    public sealed class CooldownManager
    {
        private readonly IMessenger _messenger;
        private readonly IFeedbackProvider _feedback;

        // Tracks: ActionLabel -> WarningTime (TickCount64)
        private readonly Dictionary<string, long> _activeTrackers = new();
        private readonly List<string> _keysToRemove = new();

        public CooldownManager(IMessenger messenger, IFeedbackProvider feedback)
        {
            _messenger = messenger;
            _feedback = feedback;
        }

        public void RegisterAction(ButtonAction action)
        {
            if (!action.TrackBuff) return;

            long warnTimeMs = (action.BuffDurationSec - action.BuffWarningSec) * 1000L;
            if (warnTimeMs <= 0) warnTimeMs = 1000; // Sanity check

            long targetTick = Environment.TickCount64 + warnTimeMs;
            
            // Add or overwrite (recasting resets the timer)
            _activeTrackers[action.Label] = targetTick;
        }

        public void Tick()
        {
            if (_activeTrackers.Count == 0) return;

            long now = Environment.TickCount64;
            _keysToRemove.Clear();

            foreach (var kvp in _activeTrackers)
            {
                if (now >= kvp.Value)
                {
                    // Timer hit the warning threshold!
                    _feedback.Trigger(FeedbackType.BuffWarning);
                    _messenger.Publish(new BuffWarningMessage(kvp.Key));
                    _keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in _keysToRemove)
            {
                _activeTrackers.Remove(key);
            }
        }

        public void ResetAll() => _activeTrackers.Clear();
    }
}
