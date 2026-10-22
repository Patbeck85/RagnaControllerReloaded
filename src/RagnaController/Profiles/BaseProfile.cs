using System;
using System.Collections.Generic;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Profiles
{
    /// <summary>
    /// TASK-2301: BaseProfile — gemeinsame Basis für alle Profile.
    /// Enthält alle klassenübergreifenden Einstellungen, die von ClassProfile und CharProfile geerbt werden.
    /// </summary>
    public abstract class BaseProfile
    {
        public string Name { get; set; } = "New Profile";
        public string Class { get; set; } = "Melee";
        public string ControllerGuid { get; set; } = "";
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
        public bool KiteEnabled { get; set; } = false;
        public int KiteAttackKeyVK { get; set; } = 90;
        public int KiteAttackIntervalMs { get; set; } = 55;
        public bool AutoAttackEnabled { get; set; } = false;
        public bool AutoRetargetEnabled { get; set; } = false;
        public bool SmartSkillEnabled { get; set; } = true;
        public int AutoAttackKeyVK { get; set; } = 90;
        public int TabCycleMs { get; set; } = 80;
        public float AimSensitivity { get; set; } = 22f;
        public float AimDeadzone { get; set; } = 0.20f;
        public bool MageEnabled { get; set; } = false;
        public int MageBoltKeyVK { get; set; } = 86;
        public int MageBoltCastDelayMs { get; set; } = 1200;
        public bool SupportEnabled { get; set; } = false;
        public int SupportHealKeyVK { get; set; } = 90;
        public bool PartyManagerEnabled { get; set; } = false;
        public int PartyMemberCount { get; set; } = 1;
        public int PartyHealThresholdPercent { get; set; } = 70;
        public int PartyHealIntervalMs { get; set; } = 5000;
        public int PartyHealKeyVK { get; set; } = 90;
        public bool ItemManagerEnabled { get; set; } = false;
        public List<ItemConfig> ManagedItems { get; set; } = new();
        public bool SupportPartyTabCycle { get; set; } = false;
        public bool ComboEnabled { get; set; } = false;
        public List<string> ComboSkillNames { get; set; } = new();
        public List<VirtualKey> ComboSequenceVK { get; set; } = new();
        public List<int> PreRenewalComboDelays { get; set; } = new();
        public List<int> RenewalComboDelays { get; set; } = new();
        public int SmartCursorSlotSizeX { get; set; } = 32;
        public int SmartCursorSlotSizeY { get; set; } = 32;
        public bool HandheldModeEnabled { get; set; } = false;
        public bool GyroEnabled { get; set; } = true;
        public float GyroSensitivity { get; set; } = 1.0f;
        public float GyroBlend { get; set; } = 0.6f;
        public bool BatteryThrottleEnabled { get; set; } = true;
        public bool EnableSmartStandby { get; set; } = true;
        public int StandbyTimeoutMinutes { get; set; } = 5;
        public bool EnableDynamicTickReduction { get; set; } = true;
        public int LightIdleIntervalMs { get; set; } = 16;
        public int DeepIdleIntervalMs { get; set; } = 50;
        public AdaptiveTriggerMode LeftTriggerMode { get; set; } = AdaptiveTriggerMode.Off;
        public AdaptiveTriggerMode RightTriggerMode { get; set; } = AdaptiveTriggerMode.Off;
        public bool ComboAutoLoop { get; set; } = false;
        public int ComboChainCooldownMs { get; set; } = 800;
        public int LongPressThresholdMs { get; set; } = 500;
        public int LongPressRepeatIntervalMs { get; set; } = 0;
        public List<ButtonKey> LongPressButtons { get; set; } = new();
        public float StickDeadzone { get; set; } = 0.2f;
        public float TriggerDeadzone { get; set; } = 0.1f;
        public float StickNormalization { get; set; } = 1.0f;
        public Dictionary<ButtonKey, ButtonAction> ButtonMappings { get; set; } = new();
        public List<string> SkillRecommendations { get; set; } = new();
        public string ClassTips { get; set; } = "";
        public bool IsBuiltIn { get; set; }
        public int Priority { get; set; } = 0;
        public bool IsEnabled { get; set; } = true;
        public Dictionary<string, ControllerConfig> ControllerConfigs { get; set; } = new();
        public string LastControllerGuid { get; set; } = "";
        public bool AutoActivateOnControllerConnect { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int DownloadCount { get; set; } = 0;
        public Settings.OverlayThemeType OverlayTheme { get; set; } = Settings.OverlayThemeType.Neon;
        public string LightbarColor { get; set; } = "#005AD2";

        // TASK-2302: Profile Versioning — Schema-Version für automatische Migration beim Load
        /// <summary>Schema-Version dieses Profils. Aktuell: 2. Ältere Versionen werden beim Load automatisch migriert.</summary>
        public int SchemaVersion { get; set; } = 2;
    }
}
