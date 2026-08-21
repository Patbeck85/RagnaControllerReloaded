using System;
using System.Collections.Generic;

namespace RagnaController.Models
{
    public enum ActionType { Key, LeftClick, RightClick, Scroll, Combo, SwitchWindow, RoFeature }
    public enum TurboMode { Standard, Burst, Rhythmic, Adaptive }
    public enum MacroStepType { KeyPress, LeftClick, RightClick, Delay }
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
            public bool TurboEnabled { get; set; } = false;
            public int TurboIntervalMs { get; set; } = 100;
            public TurboMode Mode { get; set; } = TurboMode.Standard;
            public string? MacroFilePath { get; set; }
            public bool IsMacro => !string.IsNullOrEmpty(MacroFilePath);
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
        }

    // Die Makro-Definitionen im Models-Namespace
    /// <summary>Value-Type: Cache-lokale Speicherung in List&lt;MacroStep&gt;, 0 Heap-Allokationen.</summary>
    public struct MacroStep
    {
        public int Index { get; set; }
        public MacroStepType Type { get; set; }
        public VirtualKey Key { get; set; }
        public int DelayMs { get; set; }
    }

    public class Macro
    {
        public string Name { get; set; } = "Untitled";
        public List<MacroStep> Steps { get; set; } = new List<MacroStep>();
        public int LoopCount { get; set; } = 1;
    }
}