using System;
using System.Collections.Generic;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// FEAT-008: Buff/Debuff tracking system.
    /// Tracks active buffs/debuffs with durations, provides warnings before expiration,
    /// and supports auto-recast when configured.
    /// </summary>
    public class BuffManager
    {
        private readonly InputCommandQueue _queue;
        private readonly CooldownManager _cooldownManager;
        private readonly Dictionary<string, BuffEntry> _activeBuffs = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, BuffEntry> _activeDebuffs = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Fired when a buff is about to expire (within warning threshold)</summary>
        public event Action<string, int>? BuffExpiringWarning;

        /// <summary>Fired when a buff has expired</summary>
        public event Action<string>? BuffExpired;

        /// <summary>Fired when a debuff is applied</summary>
        public event Action<string, int>? DebuffApplied;

        /// <summary>Fired when a debuff expires</summary>
        public event Action<string>? DebuffExpired;

        /// <summary>Current active buff names (for SkillOrchestrator condition evaluation)</summary>
        public IReadOnlyList<string> ActiveBuffNames => _activeBuffs.Keys.ToList();

        /// <summary>Current active debuff names (for SkillOrchestrator condition evaluation)</summary>
        public IReadOnlyList<string> ActiveDebuffNames => _activeDebuffs.Keys.ToList();

        public BuffManager(InputCommandQueue queue, CooldownManager cooldownManager)
        {
            _queue = queue;
            _cooldownManager = cooldownManager;
        }

        /// <summary>
        /// Register a new buff or refresh existing one.
        /// Called when a buff skill is cast or refreshed.
        /// </summary>
        /// <param name="buffName">Name of the buff (must match ButtonAction.Label in profile)</param>
        /// <param name="durationSec">Duration in seconds</param>
        /// <param name="warningSec">Warning threshold in seconds before expiration</param>
        /// <param name="autoRecast">Whether to auto-recast when expired</param>
        /// <param name="recastKey">VirtualKey to press for recast (if autoRecast)</param>
        public void RegisterBuff(string buffName, int durationSec, int warningSec = 10, bool autoRecast = false, VirtualKey recastKey = VirtualKey.None)
        {
            if (string.IsNullOrEmpty(buffName))
                return;

            var now = DateTime.UtcNow;
            var entry = new BuffEntry
            {
                Name = buffName,
                StartTime = now,
                DurationSec = durationSec,
                WarningSec = warningSec,
                AutoRecast = autoRecast,
                RecastKey = recastKey,
                WarningFired = false
            };

            _activeBuffs[buffName] = entry;
        }

        /// <summary>
        /// Register a debuff (negative effect on self or party)
        /// </summary>
        public void RegisterDebuff(string debuffName, int durationSec)
        {
            if (string.IsNullOrEmpty(debuffName))
                return;

            var entry = new BuffEntry
            {
                Name = debuffName,
                StartTime = DateTime.UtcNow,
                DurationSec = durationSec,
                WarningSec = 0,
                AutoRecast = false,
                RecastKey = VirtualKey.None,
                WarningFired = false
            };

            _activeDebuffs[debuffName] = entry;
            DebuffApplied?.Invoke(debuffName, durationSec);
        }

        /// <summary>
        /// Remove a buff manually (e.g., when dispelled)
        /// </summary>
        public void RemoveBuff(string buffName)
        {
            if (_activeBuffs.Remove(buffName))
            {
                BuffExpired?.Invoke(buffName);
            }
        }

        /// <summary>
        /// Remove a debuff manually (e.g., when cleansed)
        /// </summary>
        public void RemoveDebuff(string debuffName)
        {
            if (_activeDebuffs.Remove(debuffName))
            {
                DebuffExpired?.Invoke(debuffName);
            }
        }

        /// <summary>
        /// Check if a specific buff is active
        /// </summary>
        public bool HasBuff(string buffName) => _activeBuffs.ContainsKey(buffName);

        /// <summary>
        /// Check if a specific debuff is active
        /// </summary>
        public bool HasDebuff(string debuffName) => _activeDebuffs.ContainsKey(debuffName);

        /// <summary>
        /// Get remaining time for a buff in seconds
        /// </summary>
        public float GetBuffRemainingSec(string buffName)
        {
            if (_activeBuffs.TryGetValue(buffName, out var entry))
            {
                var elapsed = (float)(DateTime.UtcNow - entry.StartTime).TotalSeconds;
                return Math.Max(0, entry.DurationSec - elapsed);
            }
            return 0f;
        }

        /// <summary>
        /// Get remaining time for a debuff in seconds
        /// </summary>
        public float GetDebuffRemainingSec(string debuffName)
        {
            if (_activeDebuffs.TryGetValue(debuffName, out var entry))
            {
                var elapsed = (float)(DateTime.UtcNow - entry.StartTime).TotalSeconds;
                return Math.Max(0, entry.DurationSec - elapsed);
            }
            return 0f;
        }

        /// <summary>
        /// Update all tracked buffs/debuffs - call every tick
        /// </summary>
        public void Update(int deltaMs)
        {
            var now = DateTime.UtcNow;
            var expiredBuffs = new List<string>();
            var expiredDebuffs = new List<string>();

            // Update buffs
            foreach (var kvp in _activeBuffs)
            {
                var entry = kvp.Value;
                var elapsed = (float)(now - entry.StartTime).TotalSeconds;
                var remaining = entry.DurationSec - elapsed;

                // Fire warning if within threshold and not yet fired
                if (!entry.WarningFired && remaining <= entry.WarningSec && remaining > 0)
                {
                    entry.WarningFired = true;
                    BuffExpiringWarning?.Invoke(entry.Name, (int)Math.Ceiling(remaining));
                }

                // Check expiration
                if (remaining <= 0)
                {
                    expiredBuffs.Add(entry.Name);
                    
                    // Auto-recast if enabled
                    if (entry.AutoRecast && entry.RecastKey != VirtualKey.None)
                    {
                        // Check if not on cooldown (CooldownManager doesn't have key-based cooldowns yet)
                        // For now, always allow recast since autoRecast is false in current implementation
                        _queue.TapKey(entry.RecastKey);
                    }
                }
            }

            // Update debuffs
            foreach (var kvp in _activeDebuffs)
            {
                var entry = kvp.Value;
                var elapsed = (float)(now - entry.StartTime).TotalSeconds;
                var remaining = entry.DurationSec - elapsed;

                if (remaining <= 0)
                {
                    expiredDebuffs.Add(entry.Name);
                }
            }

            // Remove expired
            foreach (var name in expiredBuffs)
            {
                _activeBuffs.Remove(name);
                BuffExpired?.Invoke(name);
            }

            foreach (var name in expiredDebuffs)
            {
                _activeDebuffs.Remove(name);
                DebuffExpired?.Invoke(name);
            }
        }

        /// <summary>
        /// Clear all tracked buffs and debuffs (e.g., on profile change)
        /// </summary>
        public void ClearAll()
        {
            _activeBuffs.Clear();
            _activeDebuffs.Clear();
        }

        /// <summary>
        /// Internal entry for tracking buff/debuff state
        /// </summary>
        private class BuffEntry
        {
            public string Name { get; set; } = "";
            public DateTime StartTime { get; set; }
            public int DurationSec { get; set; }
            public int WarningSec { get; set; }
            public bool AutoRecast { get; set; }
            public VirtualKey RecastKey { get; set; }
            public bool WarningFired { get; set; }
        }
    }
}