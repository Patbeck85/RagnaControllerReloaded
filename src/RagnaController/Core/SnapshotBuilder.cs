using System;
using System.Windows.Media;
using RagnaController.Models;

namespace RagnaController.Core
{
    public sealed class SnapshotBuilder
    {
        private readonly AutoTargetEngine _autoTarget;
        private readonly MageEngine _mage;
        private readonly ComboEngine _combo;
        private readonly WindowTracker _winTracker;
        private readonly CursorEngine _cursor;
        private readonly SmartCursorService _smartCursor;
        private readonly SkillOrchestrator? _skillOrchestrator;
        private bool _batteryThrottle = false;

        public SnapshotBuilder(AutoTargetEngine autoTarget, MageEngine mage, ComboEngine combo, 
                               WindowTracker winTracker, CursorEngine cursor, SmartCursorService smartCursor,
                               SkillOrchestrator? skillOrchestrator = null)
        {
            _autoTarget = autoTarget;
            _mage = mage;
            _combo = combo;
            _winTracker = winTracker;
            _cursor = cursor;
            _smartCursor = smartCursor;
            _skillOrchestrator = skillOrchestrator;
        }

        public bool BatteryThrottle
        {
            get => _batteryThrottle;
            set => _batteryThrottle = value;
        }

        public ControllerSnapshot Build(ParsedInput input, bool focusLocked, double elapsedMs)
        {
            // FIX: Use cached strings to prevent allocations in hot path!
            string layerKey = _combo.Enabled ? "COMBO" : (_mage.MageEnabled ? "MAGE" : (_autoTarget.AutoAttackEnabled ? "AUTO" : ""));
            string layerText = EngineOptimizationPool.Instance.GetString(layerKey);
            
            string stateRaw = _combo.Enabled ? "COMBO ACTIVE" : (_mage.MageEnabled ? "MAGE MODE" : (_autoTarget.AutoAttackEnabled ? "AUTO ATTACK" : "IDLE"));
            string stateLabel = EngineOptimizationPool.Instance.GetString(stateRaw);

            var snap = new ControllerSnapshot
            {
                LayerText = layerText,
                StateLabel = stateLabel,
                DeltaMs = (int)elapsedMs,
                TickMs = (int)elapsedMs,
                LeftX = input.LeftX,
                LeftY = input.LeftY,
                RightX = input.RightX,
                RightY = input.RightY,
                L1 = input.L1,
                R1 = input.R1,
                L2 = input.L2,
                R2 = input.R2,
                BtnL3 = input.L3,
                BtnA = input.BtnA,
                BtnB = input.BtnB,
                BtnX = input.BtnX,
                BtnY = input.BtnY,
                DPadUp = input.DPadUp,
                DPadDown = input.DPadDown,
                DPadLeft = input.DPadLeft,
                DPadRight = input.DPadRight,
                Start = input.Start,
                Back = input.Back,
                ActionLabel = _combo.Enabled ? _combo.CurrentActionLabel : "",
                ActionId = _combo.CurrentActionId,
                
                // Skill cooldown tracking (HW-005)
                SkillCooldownMs = _skillOrchestrator?.GetActiveSkillCooldownMs() ?? 0,
                ActiveSkillId = _skillOrchestrator?.GetActiveSkillId() ?? 0,

                TargetName = _autoTarget.TargetName,
                TargetType = _autoTarget.TargetType,
                TargetDistance = _autoTarget.TargetDistance,
                Buffs = _mage.Buffs,
                Cooldowns = _mage.Cooldowns,
                MobSweepLabel = _combo.MobSweepLabel,
                HandheldModeLabel = _combo.HandheldModeLabel,
                HandheldModeActive = _combo.HandheldModeActive,
                OverlayText = _combo.OverlayText,
                MiniModeLabel = _combo.MiniModeLabel,
                SmartCursorMenuMode = _smartCursor.IsMenuMode,
                WindowTracked = _winTracker.WindowTracked,
                BatteryThrottle = _batteryThrottle
            };

            // State foreground color based on mode
            snap.StateForeground = _smartCursor.IsMenuMode 
                ? new SolidColorBrush(Color.FromRgb(229, 184, 66)) // Gold for Grid Mode
                : _combo.Enabled 
                    ? new SolidColorBrush(Color.FromRgb(229, 66, 66)) // Red for Combo
                    : _mage.MageEnabled
                        ? new SolidColorBrush(Color.FromRgb(66, 166, 229)) // Blue for Mage
                        : _autoTarget.AutoAttackEnabled
                            ? new SolidColorBrush(Color.FromRgb(66, 229, 126)) // Green for Auto
                            : new SolidColorBrush(Color.FromRgb(255, 255, 255)); // White for Idle

            snap.CombatState = snap.StateLabel;
            snap.ComboActive = _combo.IsActive;

            return snap;
        }

        private SolidColorBrush GetStateBrush(ControllerSnapshot snap)
        {
            return snap.StateForeground;
        }
    }
}