using System;
using System.Collections.Generic;

namespace RagnaController.Models
{
    public enum ActionType { Key, LeftClick, RightClick, Scroll, Combo, SwitchWindow, RoFeature }
    public enum CombatState { Idle, Seeking, Engaged, Attacking }
    public enum EngineStatus { Stopped, Running, NoController }
    public enum MagePhase { Idle, GroundAiming, Casting, BoltSpamming }
    public enum SupportPhase { Idle, TargetingParty, Healing, SelfHealing, Rezzing, PlacingSanctuary, AutoCycling }
    public enum ActionFiredKind { Skill, Combo, Click, Special }

    public class ButtonAction
        {
            public ActionType Type { get; set; } = ActionType.Key;
            public VirtualKey Key { get; set; } = VirtualKey.None;
            public string Label { get; set; } = "";
            public bool IsGroundSpell { get; set; } = false;

            // NEW: Ground Spell Properties
            public int GroundSpellDurationSec { get; set; } = 10;      // How long the ground effect persists
            public int GroundSpellTickIntervalMs { get; set; } = 1000; // Damage/heal tick interval
            public float GroundSpellRadius { get; set; } = 3f;         // AoE radius in meters/tiles
            public bool GroundSpellFollowsTarget { get; set; } = false; // If true, follows target; if false, stationary at cast position
            public bool GroundSpellIsHealing { get; set; } = false;    // Healing vs damage ground spell

            // NEW: Self-Cast Toggle (bypass aiming, snap to center)
            public bool IsSelfCast { get; set; } = false;

            public string RoFeatureName { get; set; } = "";
            public string WindowTarget { get; set; } = "ragexe";
            public int    ScrollDelta  { get; set; } = 120; // Standard-Scrollweite (3 Zeilen)

            // NEW: Buff/Cooldown Tracking
            public bool TrackBuff { get; set; } = false;
            public int BuffDurationSec { get; set; } = 60;   // How long the buff lasts
            public int BuffWarningSec { get; set; } = 10;    // When to warn before expiration

            // NEW: Long-Press Action (v2.0.0)
            /// <summary>
            /// Optional action to trigger on long-press. If null, uses the same action as normal press.
            /// </summary>
            public ButtonAction? LongPressAction { get; set; }
        }

    // Macro definitions removed - macro functionality has been removed from the application
}