using System;
using System.Collections.Generic;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Profiles
{
    public class RadialItem
    {
        public string     Name      { get; set; } = "Empty";
        public string     Command   { get; set; } = "";
        public VirtualKey Key       { get; set; } = VirtualKey.None;
        /// <summary>v1.5.0: optional modifier (e.g. Alt for Alt+S). Default None = no modifier.</summary>
        public VirtualKey ModifierKey { get; set; } = VirtualKey.None;
        public bool       IsEmote   { get; set; } = true;
        public string     ImagePath { get; set; } = "";
    }

    public class Profile
    {
        public string Name { get; set; } = "New Profile";
        public string Class { get; set; } = "Melee";
        public float MouseSensitivity { get; set; } = 1.2f;
        public float Deadzone { get; set; } = 0.12f;
        public float MovementCurve { get; set; } = 1.5f;
        public float CursorMaxSpeed { get; set; } = 1200f;
        public float CursorDeadzone { get; set; } = 0.12f;
        public float CursorCurve { get; set; } = 1.5f;
        public int MovementCoastFrames { get; set; } = 3;
        public bool ActionRpgMode { get; set; } = true;
        public float ActionSpeed { get; set; } = 5.0f;
        public int MovementCurveMode { get; set; } = 1;
        public int ClickCooldownMs { get; set; } = 80;
        public bool MobSweepEnabled { get; set; } = true;
        public int MobSweepTabIntervalMs { get; set; } = 350;
        public int MobSweepAttackDelayMs { get; set; } = 60;
        public int MobSweepAttackKeyVK { get; set; } = 0x5A;
        public int PreRenewalAttackIntervalMs { get; set; } = 100;
        public int RenewalAttackIntervalMs { get; set; } = 60;
        public int PreRenewalSkillInterruptMs { get; set; } = 800;
        public int RenewalSkillInterruptMs { get; set; } = 400;
        /// <summary>v1.7.2: Kite Mode - Auto-hide UI and focus RO. Default: false (disabled).</summary>
        public bool KiteEnabled { get; set; } = false;
        public int KiteAttackKeyVK { get; set; } = 90;
        public int KiteAttackIntervalMs { get; set; } = 55;
        /// <summary>v1.7.2: Auto-Attack - Auto-target nearest enemy. Default: false (manual).</summary>
        public bool AutoAttackEnabled   { get; set; } = false;
        /// <summary>v1.7.2: Auto-Retarget - Auto-switch target on death/loss. Default: false (manual).</summary>
        public bool AutoRetargetEnabled { get; set; } = false;
        public bool SmartSkillEnabled   { get; set; } = true;
        public int AutoAttackKeyVK { get; set; } = 90;
        public int TabCycleMs { get; set; } = 80;
        public float AimSensitivity { get; set; } = 22f;
        public float AimDeadzone { get; set; } = 0.20f;
        /// <summary>v1.7.2: Mage Engine - Auto-cast mage skills. Default: false (disabled).</summary>
        public bool MageEnabled { get; set; } = false;
        public int MageBoltKeyVK { get; set; } = 86;
        public int MageBoltCastDelayMs { get; set; } = 1200;
        /// <summary>v1.7.2: Support Engine - Auto-heal party members. Default: false (disabled).</summary>
        public bool SupportEnabled { get; set; } = false;
        public int  SupportHealKeyVK     { get; set; } = 90;
        /// <summary>FIX #5: true = Ctrl+Tab (party cycle), false = Tab (mob target).</summary>
        public bool SupportPartyTabCycle { get; set; } = false;

        /// <summary>v1.7.2: Combo Engine - Auto-combo execution. Default: false (manual).</summary>
        public bool ComboEnabled { get; set; } = false;
        public List<string>     ComboSkillNames       { get; set; } = new();
        public List<VirtualKey> ComboSequenceVK       { get; set; } = new();
        public List<int>        PreRenewalComboDelays { get; set; } = new();
        public List<int>        RenewalComboDelays    { get; set; } = new();

        // ── Handheld Mode (v1.5.0) ────────────────────────────────────────
        // ── Smart Cursor System (v1.6.1) ─────────────────────────────────
        /// <summary>Inventory slot width in logical pixels (default 32 for standard RO clients).</summary>
        public int  SmartCursorSlotSizeX   { get; set; } = 32;
        /// <summary>Inventory slot height in logical pixels.</summary>
        public int  SmartCursorSlotSizeY   { get; set; } = 32;

        public bool  HandheldModeEnabled    { get; set; } = false;
        public bool  GyroEnabled            { get; set; } = true;
        public float GyroSensitivity        { get; set; } = 1.0f;
        public float GyroBlend              { get; set; } = 0.6f;
        public bool BatteryThrottleEnabled { get; set; } = true;

        // ── Smart Standby (AFK Battery Saver) ──────────────────────────────
        public bool EnableSmartStandby { get; set; } = true;
        public int StandbyTimeoutMinutes { get; set; } = 5;

        // ── DualSense Adaptive Triggers (v1.7.0) ───────────────────────────
        public AdaptiveTriggerMode LeftTriggerMode  { get; set; } = AdaptiveTriggerMode.Off;
        public AdaptiveTriggerMode RightTriggerMode { get; set; } = AdaptiveTriggerMode.Off;

        // --- Combo Engine - AutoLoop (v1.5.1) ---
        /// <summary>When false (default), combo chain requires button release between repetitions.
        /// Set true for monk snap-combo and other classes where auto-looping is intentional.</summary>
        public bool ComboAutoLoop       { get; set; } = false;
        /// <summary>Cooldown after a full combo chain (ms). Lower for high-speed builds.</summary>
        public int  ComboChainCooldownMs { get; set; } = 800;

        /// <summary>
        /// v1.7.2: Key-to-action mappings using VirtualKey for compile-time safety.
        /// </summary>
        /// <remarks>Keys are typed as VirtualKey enum values instead of string keys for better IntelliSense and error prevention.
        /// JSON persistence uses string keys for backward compatibility with existing profile files.</remarks>
        public Dictionary<string, ButtonAction> ButtonMappings { get; set; } = new();

        public List<string> SkillRecommendations { get; set; } = new();
        public string ClassTips { get; set; } = "";
        public bool IsBuiltIn { get; set; }
        public int Priority { get; set; } = 0;
        public bool IsEnabled { get; set; } = true;

        // Turbo Mode (v1.7.3)
        public bool TurboEnabled { get; set; } = false;
        /// <summary>v1.7.3: Radial Menu Items - Custom radial menu commands for the profile.</summary>
        public List<RadialItem> RadialMenuItems { get; set; } = new();

        // Emote Commands (v1.7.3) - Static collection for quick access
        public static List<RadialItem> EmoteCommands => new()
        {
            new RadialItem { Name = "❤ LOVE",  Command = "/lv",  IsEmote = true },
            new RadialItem { Name = "💋 KISS",  Command = "/kis", IsEmote = true },
            new RadialItem { Name = "😂 HAHA",  Command = "/heh", IsEmote = true },
            new RadialItem { Name = "😢 CRY",   Command = "/sob", IsEmote = true },
            new RadialItem { Name = "😰 SWEAT", Command = "/swt", IsEmote = true },
            new RadialItem { Name = "😱 OMG",   Command = "/omg", IsEmote = true },
            new RadialItem { Name = "🙏 SORRY", Command = "/sry", IsEmote = true },
            new RadialItem { Name = "👍 NICE",  Command = "/thx", IsEmote = true },
        };
    }
}
