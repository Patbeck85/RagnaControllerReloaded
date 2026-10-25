using System;
using System.Windows.Media;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;
using RagnaController.Core.Hardware;

namespace RagnaController.Controller
{
    /// <summary>
    /// SnapshotBuilder — Builds ControllerSnapshot from current engine state.
    /// Aggregates data from AutoTargetEngine, MageEngine, ComboEngine, WindowTracker,
    /// CursorEngine, SmartCursorService, and SkillOrchestrator into a single snapshot
    /// for UI binding. Uses EngineOptimizationPool for zero-allocation string caching.
    /// </summary>
    /// <remarks>
    /// Thread safety: Safe to call from background engine thread.
    /// Result is immutable and safe for UI thread binding.
    /// </remarks>
    public sealed class SnapshotBuilder
    {
        private readonly AutoTargetEngine _autoTarget;
        private readonly MageEngine _mage;
        private readonly ComboEngine _combo;
        private readonly Core.WindowTracker _winTracker;
        private readonly CursorEngine _cursor;
        private readonly SmartCursorService _smartCursor;
        private readonly SkillOrchestrator? _skillOrchestrator;
        private bool _batteryThrottle = false;

        // TASK-2402: Profil-Referenz für Drift-Kompensation
        private Profile? _activeProfile;
        private StickDriftCompensation? _driftCompensation;

        // TASK-2403: Trigger-Kurven für LT und RT
        private TriggerDeadzoneCurve _leftTriggerCurve = new TriggerDeadzoneCurve { Type = TriggerDeadzoneCurve.CurveType.Linear, Deadzone = 0.05f, Saturation = 1.00f };
        private TriggerDeadzoneCurve _rightTriggerCurve = new TriggerDeadzoneCurve { Type = TriggerDeadzoneCurve.CurveType.Linear, Deadzone = 0.05f, Saturation = 1.00f };

        /// <summary>Setzt das aktive Profil für Drift-Kompensation und Trigger-Kurven.</summary>
        public Profile? ActiveProfile
        {
            get => _activeProfile;
            set
            {
                _activeProfile = value;
                _driftCompensation = value != null && value.StickDriftCalibrated
                    ? new StickDriftCompensation()
                    : null;
                if (_driftCompensation != null)
                {
                    _driftCompensation.SetOffsets(
                        value.LeftStickDriftX, value.LeftStickDriftY,
                        value.RightStickDriftX, value.RightStickDriftY);
                }

                // Trigger-Kurven aus Profil laden
                _leftTriggerCurve = new TriggerDeadzoneCurve
                {
                    Type = ParseCurveType(value.LeftTriggerCurveType),
                    Deadzone = value.LeftTriggerDeadzone,
                    Saturation = value.LeftTriggerSaturation,
                };
                _rightTriggerCurve = new TriggerDeadzoneCurve
                {
                    Type = ParseCurveType(value.RightTriggerCurveType),
                    Deadzone = value.RightTriggerDeadzone,
                    Saturation = value.RightTriggerSaturation,
                };
            }
        }

        /// <summary>Wandelt einen String in einen CurveType um.</summary>
        private static TriggerDeadzoneCurve.CurveType ParseCurveType(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return TriggerDeadzoneCurve.CurveType.Linear;
            return Enum.TryParse<TriggerDeadzoneCurve.CurveType>(typeName, ignoreCase: true, out var result)
                ? result
                : TriggerDeadzoneCurve.CurveType.Linear;
        }

        public SnapshotBuilder(AutoTargetEngine autoTarget, MageEngine mage, ComboEngine combo,
                               Core.WindowTracker winTracker, CursorEngine cursor, SmartCursorService smartCursor,
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

            // Rent from pool instead of new() to avoid Gen0 allocation every UI tick (~30/sec)
            var snap = MessagePools.ControllerSnapshotPool.Rent();
            snap.LayerText = layerText;
            snap.StateLabel = stateLabel;
            snap.DeltaMs = (int)elapsedMs;
            snap.TickMs = (int)elapsedMs;
            snap.LeftX = ApplyDriftCompensation(input.LeftX, true, true);
            snap.LeftY = ApplyDriftCompensation(input.LeftY, true, false);
            snap.RightX = ApplyDriftCompensation(input.RightX, false, true);
            snap.RightY = ApplyDriftCompensation(input.RightY, false, false);
            snap.L1 = input.L1;
            snap.R1 = input.R1;
            snap.L2 = input.L2;
            snap.R2 = input.R2;
            snap.LT = _leftTriggerCurve.Apply(input.LT);
            snap.RT = _rightTriggerCurve.Apply(input.RT);
            snap.BtnL3 = input.L3;
            snap.BtnA = input.BtnA;
            snap.BtnB = input.BtnB;
            snap.BtnX = input.BtnX;
            snap.BtnY = input.BtnY;
            snap.DPadUp = input.DPadUp;
            snap.DPadDown = input.DPadDown;
            snap.DPadLeft = input.DPadLeft;
            snap.DPadRight = input.DPadRight;
            snap.Start = input.Start;
            snap.Back = input.Back;
            snap.ActionLabel = _combo.Enabled ? _combo.CurrentActionLabel : "";
            snap.ActionId = _combo.CurrentActionId;

            // Skill cooldown tracking (HW-005)
            snap.SkillCooldownMs = _skillOrchestrator?.GetActiveSkillCooldownMs() ?? 0;
            snap.ActiveSkillId = _skillOrchestrator?.GetActiveSkillId() ?? 0;

            snap.TargetName = _autoTarget.TargetName;
            snap.TargetType = _autoTarget.TargetType;
            snap.TargetDistance = _autoTarget.TargetDistance;
            snap.Buffs = _mage.Buffs;
            snap.Cooldowns = _mage.Cooldowns;
            snap.MobSweepLabel = _combo.MobSweepLabel;
            snap.HandheldModeLabel = _combo.HandheldModeLabel;
            snap.HandheldModeActive = _combo.HandheldModeActive;
            snap.OverlayText = _combo.OverlayText;
            snap.MiniModeLabel = _combo.MiniModeLabel;
            snap.SmartCursorMenuMode = _smartCursor.IsMenuMode;
            snap.WindowTracked = _winTracker.WindowTracked;
            snap.BatteryThrottle = _batteryThrottle;
            snap.FocusLocked = focusLocked;

            // State foregroand color based on mode
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

        // TASK-2402: Wendet die Drift-Kompensation auf einen Stick-Wert an
        private float ApplyDriftCompensation(float rawValue, bool isLeftStick, bool isXAxis)
        {
            if (_driftCompensation == null || !_driftCompensation.IsCalibrated)
                return rawValue;

            float offset = (isLeftStick, isXAxis) switch
            {
                (true, true)  => _driftCompensation.LeftStickDriftX,
                (true, false) => _driftCompensation.LeftStickDriftY,
                (false, true) => _driftCompensation.RightStickDriftX,
                (false, false) => _driftCompensation.RightStickDriftY,
            };

            return StickDriftCompensation.Compensate(rawValue, offset);
        }

        private SolidColorBrush GetStateBrush(ControllerSnapshot snap)
        {
            return snap.StateForeground;
        }
    }
}