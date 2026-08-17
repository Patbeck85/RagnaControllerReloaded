using System;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Core
{
    /// <summary>
    /// ARCH-001: Extracted from HybridEngine - Profile loading & live-update logic.
    /// Responsible for: Full profile application, live parameter updates, renewal/pre-renewal timing.
    /// FEAT-004: Auto-class detection from keybinds.
    /// </summary>
    public class ProfileApplier
    {
        private readonly EngineOrchestrator _orchestrator;
        private readonly IMessenger _messenger;

        public ProfileApplier(EngineOrchestrator orchestrator, IMessenger messenger)
        {
            _orchestrator = orchestrator;
            _messenger = messenger;
        }

        /// <summary>
        /// Load a complete profile - resets all engines and applies all settings.
        /// </summary>
        public void LoadProfile(Profile p, bool autoDetectClass = false)
        {
            // CRITICAL: Reset ALL state machines HARD!
            // Problem: If Sniper was in 'Kite' mode (running backwards),
            // this state remains active in background. New Knight would
            // suddenly start running backwards uncontrollably!

            // 0. FeedbackSystem stop (prevents Rumble/LED ghosting on profile switch)
            _orchestrator.Feedback?.StopAll();

            // 1. MovementEngine stop and clear queue
            _orchestrator.Movement.ForceStop();
            var queue = _orchestrator.CommandQueue;
            queue?.LeftUp();
            queue?.KeyUp(VirtualKey.ArrowLeft);
            queue?.KeyUp(VirtualKey.ArrowRight);
            queue?.KeyUp(VirtualKey.ArrowUp);
            queue?.KeyUp(VirtualKey.ArrowDown);

            // 2. Disable all Combat-Engines (not null - they're readonly!)
            // Just don't call them anymore - GC cleans up when no refs exist
            _orchestrator.AutoTarget.Reset();
            _orchestrator.Kite.Reset();
            _orchestrator.Mage.Reset();
            _orchestrator.Support.Reset();
            _orchestrator.Combo.Reset();
            _orchestrator.MobSweep.Reset();

            // 3. Load profile configurations
            _orchestrator.CurrentProfile = p;
            _orchestrator.Combat.LoadProfile(p);

            // NEW: Reset CooldownManager
            _orchestrator.CooldownManager.ResetAll();

            // FEAT-004: Auto-class detection if requested
            if (autoDetectClass)
            {
                var detectedClass = ClassDetector.DetectClass(p);
                var preset = ClassDetector.GetPresetForClass(detectedClass);
                ClassDetector.ApplyClassPreset(_orchestrator, p, preset);
                _orchestrator.SubscribeToLog($"[Engine] Auto-class detected: {detectedClass} → {preset}");
            }
            else
            {
                ApplyProfileSettings(p);
            }

            _orchestrator.SubscribeToLog($"[Engine] Profil geladen: {p.Name} ({p.Class})");
        }

        private void ApplyProfileSettings(Profile p)
        {
            // AutoTarget
            _orchestrator.AutoTarget.AutoAttackEnabled = p.AutoAttackEnabled;
            _orchestrator.AutoTarget.AutoRetargetEnabled = p.AutoRetargetEnabled;
            _orchestrator.AutoTarget.SmartSkillEnabled = p.SmartSkillEnabled;
            _orchestrator.AutoTarget.AttackKey_VK = p.AutoAttackKeyVK;
            _orchestrator.AutoTarget.TabCycleMs = p.TabCycleMs;
            _orchestrator.AutoTarget.AimSensitivity = p.AimSensitivity;
            _orchestrator.AutoTarget.AimDeadzone = p.AimDeadzone;
            ApplyRenewalTiming(p, _orchestrator.IsRenewal);

            // Kite
            _orchestrator.Kite.KiteEnabled = p.KiteEnabled;
            _orchestrator.Kite.AttackKeyVK = p.KiteAttackKeyVK;
            _orchestrator.Kite.AttackIntervalMs = p.KiteAttackIntervalMs;

            // Mage
            _orchestrator.Mage.MageEnabled = p.MageEnabled;
            _orchestrator.Mage.MageBoltKeyVK = (VirtualKey)p.MageBoltKeyVK;
            _orchestrator.Mage.MageBoltCastDelayMs = p.MageBoltCastDelayMs;
            _orchestrator.Mage.GyroEnabled = p.GyroEnabled;
            _orchestrator.Mage.GyroBlend = p.GyroBlend;

            // Support
            _orchestrator.Support.SupportEnabled = p.SupportEnabled;
            _orchestrator.Support.HealKeyVK = (VirtualKey)p.SupportHealKeyVK;
            _orchestrator.Support.PartyTabCycle = p.SupportPartyTabCycle;

            // Cursor
            _orchestrator.Cursor.MaxSpeed = p.CursorMaxSpeed;
            _orchestrator.Cursor.Deadzone = p.CursorDeadzone;
            _orchestrator.Cursor.Curve = p.CursorCurve;
            _orchestrator.Cursor.Sensitivity = p.MouseSensitivity;

            // Movement
            _orchestrator.Movement.Deadzone = p.Deadzone;
            _orchestrator.Movement.Curve = p.MovementCurve;
            _orchestrator.Movement.CoastFrames = p.MovementCoastFrames;
            _orchestrator.Movement.CurveMode = p.MovementCurveMode;
            _orchestrator.Movement.ActionRpgMode = p.ActionRpgMode;

            // Combo
            _orchestrator.Combo.Enabled = p.ComboEnabled;
            _orchestrator.Combo.Sequence = p.ComboSequenceVK;
            _orchestrator.Combo.CurrentDelays = _orchestrator.IsRenewal
                ? p.RenewalComboDelays
                : p.PreRenewalComboDelays;
            _orchestrator.Combo.AutoLoop = p.ComboAutoLoop;
            _orchestrator.Combo.ChainCooldownMs = p.ComboChainCooldownMs;

            // Mage Gyro
            _orchestrator.Mage.GyroSensitivity = p.GyroSensitivity;

            // MobSweep
            _orchestrator.MobSweep.MobSweepEnabled = p.MobSweepEnabled;
            _orchestrator.MobSweep.AttackKeyVK = p.MobSweepAttackKeyVK;
            _orchestrator.MobSweep.AttackDelayMs = p.MobSweepAttackDelayMs;
            _orchestrator.MobSweep.TabIntervalMs = p.MobSweepTabIntervalMs;

            // DualSense Adaptive Triggers
            _orchestrator.DualSense?.SetAdaptiveTriggerModes(p.LeftTriggerMode, p.RightTriggerMode);

            // Handheld / ActionRpgMode
            _orchestrator.Handheld.IsEnabled = p.HandheldModeEnabled;

            // Battery Throttle - check if tick provider is BackgroundTickProvider
            if (_orchestrator.TickProvider is BackgroundTickProvider btp)
                btp.BatteryThrottle = p.BatteryThrottleEnabled;
        }

        private void ApplyRenewalTiming(Profile p, bool renewal)
        {
            _orchestrator.AutoTarget.AttackIntervalMs = renewal
                ? p.RenewalAttackIntervalMs
                : p.PreRenewalAttackIntervalMs;
            _orchestrator.AutoTarget.SkillInterruptMs = renewal
                ? p.RenewalSkillInterruptMs
                : p.PreRenewalSkillInterruptMs;
            _orchestrator.Combo.CurrentDelays = renewal
                ? p.RenewalComboDelays
                : p.PreRenewalComboDelays;
        }

        /// <summary>
        /// Live update: Game mode (Renewal / Pre-Renewal)
        /// </summary>
        public void ApplyGameMode(bool isRenewal)
        {
            _orchestrator.IsRenewal = isRenewal;
            if (_orchestrator.CurrentProfile != null)
                ApplyRenewalTiming(_orchestrator.CurrentProfile, isRenewal);
        }

        /// <summary>
        /// Live update: Action RPG mode toggle
        /// </summary>
        public void LiveUpdateActionRpg(bool enabled)
        {
            _orchestrator.AutoTarget.AutoAttackEnabled = enabled
                && (_orchestrator.CurrentProfile?.AutoAttackEnabled ?? false);
        }

        /// <summary>
        /// Live update: Deadzone (movement + cursor)
        /// </summary>
        public void LiveUpdateDeadzone(float v)
        {
            _orchestrator.Movement.Deadzone = v;
            _orchestrator.Cursor.Deadzone = v;
        }

        /// <summary>
        /// Live update: Curve (movement)
        /// </summary>
        public void LiveUpdateCurve(float v)
        {
            _orchestrator.Movement.Curve = v;
        }

        /// <summary>
        /// Live update: Action Speed (1-10: higher = smaller interval = faster attacks)
        /// </summary>
        public void LiveUpdateActionSpeed(float v)
        {
            if (_orchestrator.CurrentProfile == null) return;
            float factor = Math.Clamp(v / 5f, 0.3f, 3.0f);
            int baseMs = _orchestrator.IsRenewal
                ? _orchestrator.CurrentProfile.RenewalAttackIntervalMs
                : _orchestrator.CurrentProfile.PreRenewalAttackIntervalMs;
            _orchestrator.AutoTarget.AttackIntervalMs = Math.Max(20, (int)(baseMs / factor));
        }

        /// <summary>
        /// Live update: Cursor speed
        /// </summary>
        public void LiveUpdateCursorSpeed(float v) => _orchestrator.Cursor.MaxSpeed = v;

        /// <summary>
        /// Live updates for v1.7.0 stick/trigger settings
        /// </summary>
        public void LiveUpdateLeftStick(float v) => _orchestrator.Movement.Deadzone = v;
        public void LiveUpdateRightStick(float v) => _orchestrator.Cursor.Deadzone = v;
        public void LiveUpdateLeftTrigger(float v) { /* Adaptive Trigger L2 - Placeholder */ }
        public void LiveUpdateRightTrigger(float v) { /* Adaptive Trigger R2 - Placeholder */ }
        public void LiveUpdateTurboInterval(float v) { /* Turbo-Intervall - Placeholder für v1.7.0 */ }

        /// <summary>
        /// Settings: Sound
        /// </summary>
        public void SetSoundEnabled(bool enabled) => _orchestrator.SoundEnabled = enabled;

        /// <summary>
        /// Settings: Rumble
        /// </summary>
        public void SetRumbleEnabled(bool enabled)
        {
            _orchestrator.RumbleEnabled = enabled;
            if (!enabled) _orchestrator.Feedback.StopRumble();
        }

        /// <summary>
        /// DualSense Lightbar off
        /// </summary>
        public void TurnOff() => _orchestrator.Controller?.SetLED(0, 0, 0);

        /// <summary>
        /// Settings: Haptic Metronome
        /// </summary>
        public void SetHapticMetronomeEnabled(bool enabled) => _orchestrator.HapticMetronomeEnabled = enabled;

        /// <summary>
        /// Smart Standby settings
        /// </summary>
        public void SetStandbySettings(bool enabled, int timeoutMins)
        {
            _orchestrator.StandbyManager?.SetStandbySettings(enabled, timeoutMins);
        }

        /// <summary>
        /// Attach ProfileManager logger
        /// </summary>
        public void AttachProfileManagerLogger(ProfileManager pm)
        {
            pm.ProfileSaved += name => _orchestrator.SubscribeToLog($"[Profile] Gespeichert: {name}");
        }

        /// <summary>
        /// Clear macro cache
        /// </summary>
        public void ClearMacroCache() => _orchestrator.Combat.ClearMacroCache();
    }
}