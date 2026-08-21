using System;
using System.Collections.Generic;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Core
{
    /// <summary>
    /// FEAT-006: Ground Spell / AoE Engine
    /// Manages persistent ground effects (Fire Wall, Frozen Ground, Heal Circle, Traps, etc.)
    /// Tracks position, duration, tick intervals, and auto-cleanup.
    /// </summary>
    public class GroundSpellEngine : IInputHandler
    {
        private readonly InputCommandQueue _queue;
        private readonly List<ActiveGroundSpell> _activeSpells = new();
        private int _lastTickMs;

        public bool Enabled { get; set; } = true;
        public int Priority => 15; // Run after combat engines but before UI

        public GroundSpellEngine(InputCommandQueue queue)
        {
            _queue = queue;
        }

        public bool Handle(ParsedInput input, int deltaMs)
        {
            if (!Enabled) return false;
            _lastTickMs += deltaMs;
            UpdateGroundSpells(deltaMs);
            return false; // Don't consume input
        }

        /// <summary>
        /// Called when a ground spell skill is fired - registers the effect
        /// </summary>
        public void RegisterGroundSpell(ButtonAction action, float worldX, float worldY, string skillName)
        {
            if (action == null || !action.IsGroundSpell) return;

            var spell = new ActiveGroundSpell
            {
                SkillName = skillName,
                WorldX = worldX,
                WorldY = worldY,
                DurationMs = action.GroundSpellDurationSec * 1000,
                TickIntervalMs = action.GroundSpellTickIntervalMs,
                Radius = action.GroundSpellRadius,
                IsHealing = action.GroundSpellIsHealing,
                FollowsTarget = action.GroundSpellFollowsTarget,
                LastTickMs = 0,
                CreatedAt = DateTime.UtcNow
            };

            _activeSpells.Add(spell);
        }

        private void UpdateGroundSpells(int deltaMs)
        {
            var now = DateTime.UtcNow;
            for (int i = _activeSpells.Count - 1; i >= 0; i--)
            {
                var spell = _activeSpells[i];
                spell.ElapsedMs += deltaMs;
                spell.LastTickMs += deltaMs;

                // Tick damage/heal
                if (spell.LastTickMs >= spell.TickIntervalMs)
                {
                    spell.LastTickMs = 0;
                    OnGroundSpellTick(spell);
                }

                // Check expiration
                if (spell.ElapsedMs >= spell.DurationMs)
                {
                    OnGroundSpellExpired(spell);
                    _activeSpells.RemoveAt(i);
                }
            }
        }

        private void OnGroundSpellTick(ActiveGroundSpell spell)
        {
            // Fire tick event - can be subscribed by combat engine for AoE damage/heal
            GroundSpellTick?.Invoke(spell);
        }

        private void OnGroundSpellExpired(ActiveGroundSpell spell)
        {
            GroundSpellExpired?.Invoke(spell);
        }

        /// <summary>
        /// Get all active ground spells for UI rendering
        /// </summary>
        public IReadOnlyList<ActiveGroundSpell> GetActiveSpells() => _activeSpells.AsReadOnly();

        /// <summary>
        /// Clear all ground spells (e.g., on map change)
        /// </summary>
        public void ClearAll() => _activeSpells.Clear();

        public event Action<ActiveGroundSpell>? GroundSpellTick;
        public event Action<ActiveGroundSpell>? GroundSpellExpired;
        
        /// <summary>
        /// FEAT-007: Get active spell names for SkillOrchestrator condition evaluation
        /// </summary>
        public List<string> GetActiveSpellNames()
        {
            var names = new List<string>();
            foreach (var spell in _activeSpells)
            {
                names.Add(spell.SkillName);
            }
            return names;
        }
    }

    public class ActiveGroundSpell
    {
        public string SkillName { get; set; } = "";
        public float WorldX { get; set; }
        public float WorldY { get; set; }
        public int DurationMs { get; set; }
        public int TickIntervalMs { get; set; }
        public float Radius { get; set; }
        public bool IsHealing { get; set; }
        public bool FollowsTarget { get; set; }
        public int ElapsedMs { get; set; }
        public int LastTickMs { get; set; }
        public DateTime CreatedAt { get; set; }

        public float RemainingPercent => 1f - (float)ElapsedMs / DurationMs;
        public bool IsExpired => ElapsedMs >= DurationMs;
    }
}