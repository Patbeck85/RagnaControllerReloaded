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
        public string ControllerGuid { get; set; } = "";  // GUID of the controller this profile is mapped to
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

        // ── Long-Press Detection (v2.0.0) ───────────────────────────────────────
        /// <summary>
        /// Time in milliseconds before a button press is considered a "long press".
        /// Default: 500ms (half a second).
        /// </summary>
        public int LongPressThresholdMs { get; set; } = 500;

        /// <summary>
        /// Time in milliseconds between repeated long-press triggers (auto-repeat).
        /// Set to 0 to disable auto-repeat. Default: 0 (disabled).
        /// </summary>
        public int LongPressRepeatIntervalMs { get; set; } = 0;

        /// <summary>
        /// Which buttons should trigger long-press actions. Empty = all buttons.
        /// Configure via ButtonMappings with LongPressAction.
        /// </summary>
        public List<ButtonKey> LongPressButtons { get; set; } = new();

        // ── Analog Stick Normalization for RO (v2.0.0) ──────────────────────────
        /// <summary>
        /// Deadzone for analog sticks (0.0 to 1.0). Lower values increase sensitivity.
        /// Default: 0.2 (20%).
        /// </summary>
        public float StickDeadzone { get; set; } = 0.2f;

        /// <summary>
        /// Deadzone for triggers (0.0 to 1.0).
        /// Default: 0.1 (10%).
        /// </summary>
        public float TriggerDeadzone { get; set; } = 0.1f;

        /// <summary>
        /// Normalization factor for analog sticks on RO (0.0 - 1.0, 1.0 = normalized, 0.0 = raw values).
        /// Default: 1.0 (full normalization).
        /// </summary>
        public float StickNormalization { get; set; } = 1.0f;

        /// <summary>
        /// v1.7.2: Key-to-action mappings using ButtonKey for compile-time safety.
        /// </summary>
        /// <remarks>Keys are typed as ButtonKey (VirtualKey + optional modifier) instead of string keys for better IntelliSense and error prevention.
        /// JSON persistence uses string keys for backward compatibility with existing profile files.</remarks>
        public Dictionary<ButtonKey, ButtonAction> ButtonMappings { get; set; } = new();

        public List<string> SkillRecommendations { get; set; } = new();
        public string ClassTips { get; set; } = "";
        public bool IsBuiltIn { get; set; }
        public int Priority { get; set; } = 0;
        public bool IsEnabled { get; set; } = true;


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

        // ── Advanced Controller Mapping (v2.0.0) ──────────────────────────────
        /// <summary>
        /// Controller-specific configuration. Key = controller GUID/name, Value = controller-specific settings.
        /// Allows different deadzones, curves, mappings per physical controller.
        /// </summary>
        public Dictionary<string, ControllerConfig> ControllerConfigs { get; set; } = new();

        /// <summary>
        /// The GUID/name of the controller this profile was last used with.
        /// Used for auto-switching when the same controller is reconnected.
        /// </summary>
        public string LastControllerGuid { get; set; } = "";

        /// <summary>
        /// Whether this profile should auto-activate when its associated controller is connected.
        /// </summary>
        public bool AutoActivateOnControllerConnect { get; set; } = false;
    }

    /// <summary>
    /// Controller-specific configuration for a profile.
    /// Allows per-controller tuning of deadzones, curves, and button mappings.
    /// </summary>
    public class ControllerConfig
    {
        /// <summary>Unique identifier for the controller (GUID or stable name).</summary>
        public string ControllerGuid { get; set; } = "";

        /// <summary>Display name of the controller (e.g., "DualSense Wireless Controller").</summary>
        public string ControllerName { get; set; } = "";

        /// <summary>Controller type (PS5, PS4, Switch, Xbox, etc.).</summary>
        public string ControllerType { get; set; } = "Unknown";

        /// <summary>Left stick deadzone (0.0 - 1.0). Overrides profile default if set.</summary>
        public float? LeftStickDeadzone { get; set; }

        /// <summary>Right stick deadzone (0.0 - 1.0). Overrides profile default if set.</summary>
        public float? RightStickDeadzone { get; set; }

        /// <summary>Left trigger deadzone (0.0 - 1.0). Overrides profile default if set.</summary>
        public float? LeftTriggerDeadzone { get; set; }

        /// <summary>Right trigger deadzone (0.0 - 1.0). Overrides profile default if set.</summary>
        public float? RightTriggerDeadzone { get; set; }

        /// <summary>Left stick response curve (1.0 = linear, >1.0 = exponential). Overrides profile default if set.</summary>
        public float? LeftStickCurve { get; set; }

        /// <summary>Right stick response curve (1.0 = linear, >1.0 = exponential). Overrides profile default if set.</summary>
        public float? RightStickCurve { get; set; }

        /// <summary>Controller-specific button mappings. Overrides profile ButtonMappings for this controller.</summary>
        public Dictionary<ButtonKey, ButtonAction> ButtonMappings { get; set; } = new();

        /// <summary>DualSense adaptive trigger modes for this controller.</summary>
        public AdaptiveTriggerMode LeftTriggerMode { get; set; } = AdaptiveTriggerMode.Off;
        public AdaptiveTriggerMode RightTriggerMode { get; set; } = AdaptiveTriggerMode.Off;

        /// <summary>Whether rumble is enabled for this controller.</summary>
        public bool RumbleEnabled { get; set; } = true;

        /// <summary>Rumble intensity multiplier (0.0 - 1.0).</summary>
        public float RumbleIntensity { get; set; } = 1.0f;

        /// <summary>LED color for this controller (hex RGB).</summary>
        public string LedColor { get; set; } = "#005AD2";

        /// <summary>When this config was last updated.</summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
