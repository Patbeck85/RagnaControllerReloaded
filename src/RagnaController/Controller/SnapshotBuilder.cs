using System;
using System.Windows.Media;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Controller
{
    public sealed class SnapshotBuilder
    {
        private readonly AutoTargetEngine _autoTarget;
        private readonly MageEngine _mage;
        private readonly ComboEngine _combo;
        private readonly Core.WindowTracker _winTracker;
        private readonly CursorEngine _cursor;
        private readonly SmartCursorService _smartCursor;
        private bool _batteryThrottle = false;

        public SnapshotBuilder(AutoTargetEngine autoTarget, MageEngine mage, ComboEngine combo, 
                               Core.WindowTracker winTracker, CursorEngine cursor, SmartCursorService smartCursor)
        {
            _autoTarget = autoTarget;
            _mage = mage;
            _combo = combo;
            _winTracker = winTracker;
            _cursor = cursor;
            _smartCursor = smartCursor;
        }

        public bool BatteryThrottle
        {
            get => _batteryThrottle;
            set => _batteryThrottle = value;
        }

        public ControllerSnapshot Build(ParsedInput input, int deltaMs, bool batteryThrottle)
        {
            var snap = new ControllerSnapshot
            {
                LayerText = _combo.Enabled ? "COMBO" : (_mage.MageEnabled ? "MAGE" : (_autoTarget.AutoAttackEnabled ? "AUTO" : "")),
                StateLabel = _combo.Enabled ? "COMBO ACTIVE" : (_mage.MageEnabled ? "MAGE MODE" : (_autoTarget.AutoAttackEnabled ? "AUTO ATTACK" : "IDLE")),
                DeltaMs = deltaMs,
                LeftX = input.LeftX,
                LeftY = input.LeftY,
                RightX = input.RightX,
                RightY = input.RightY,
                L1 = input.L1,
                R1 = input.R1,
                L2 = input.L2,
                R2 = input.R2,
                L3 = input.L3,
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
                BatteryThrottle = batteryThrottle
            };

            // State foreground color based on mode
            snap.StateForeground = _smartCursor.IsMenuMode 
                ? new SolidColorBrush(Color.FromRgb(229, 184, 66)) // Gold for Grid Mode
                : GetStateBrush(snap);

            return snap;
        }

        private SolidColorBrush GetStateBrush(ControllerSnapshot snap)
        {
            if (_combo.Enabled && _combo.CurrentActionLabel != "")
                return new SolidColorBrush(Color.FromRgb(0, 255, 0)); // Green for combo
            if (_mage.MageEnabled && _mage.IsActive)
                return new SolidColorBrush(Color.FromRgb(128, 0, 128)); // Purple for mage
            if (_autoTarget.AutoAttackEnabled && _autoTarget.IsAutoAttacking)
                return new SolidColorBrush(Color.FromRgb(0, 128, 0)); // Green for auto attack
            return new SolidColorBrush(Color.FromRgb(255, 255, 255)); // White default
        }
    }
}
