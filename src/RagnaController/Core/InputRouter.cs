using System;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Core
{
    /// <summary>
    /// ARCH-001: Extracted from HybridEngine - Input routing & modifier parsing.
    /// Responsible for: Layer updates, overlay routing, smart cursor, engine chain routing.
    /// </summary>
    public class InputRouter
    {
        private readonly CombatEngine _combat;
        private readonly MovementEngine _movement;
        private readonly AutoTargetEngine _autoTarget;
        private readonly MageEngine _mage;
        private readonly ComboEngine _combo;
        private readonly CursorEngine _cursor;
        private readonly SmartCursorService _smartCursor;
        private readonly KiteEngine _kite;
        private readonly SupportEngine _support;
        private readonly OverlayRouter _overlayRouter;
        private readonly MobSweepEngine _mobSweep;
        private readonly HandheldModeManager _handheld;
        private readonly IFeedbackProvider _feedback;
        private readonly CooldownManager _cooldownManager;

        public InputRouter(
            CombatEngine combat,
            MovementEngine movement,
            AutoTargetEngine autoTarget,
            MageEngine mage,
            ComboEngine combo,
            CursorEngine cursor,
            SmartCursorService smartCursor,
            KiteEngine kite,
            SupportEngine support,
            OverlayRouter overlayRouter,
            MobSweepEngine mobSweep,
            HandheldModeManager handheld,
            IFeedbackProvider feedback,
            CooldownManager cooldownManager)
        {
            _combat = combat;
            _movement = movement;
            _autoTarget = autoTarget;
            _mage = mage;
            _combo = combo;
            _cursor = cursor;
            _smartCursor = smartCursor;
            _kite = kite;
            _support = support;
            _overlayRouter = overlayRouter;
            _mobSweep = mobSweep;
            _handheld = handheld;
            _feedback = feedback;
            _cooldownManager = cooldownManager;
        }

        /// <summary>
        /// Route input through the engine chain. Returns true if input was consumed.
        /// </summary>
        public bool RouteInput(ParsedInput input, int actualDeltaMs, bool rumbleEnabled, bool hapticMetronomeEnabled)
        {
            // Update combat layers (L1, R1, L2, R2)
            _combat.UpdateLayers(input.L1, input.R1, input.L2, input.R2);

            // Update movement (left stick)
            _movement.Update(input.LeftX, input.LeftY);

            // OverlayRouter has priority (Start+DPad shortcuts, Mini-Button)
            if (_overlayRouter.TryHandleInput(input)) return true;

            // SmartCursor: D-Pad Grid-Hopping + Precision-Aiming in menus
            if (_smartCursor.Tick(input)) return true; // Menu input consumed → skip combat

            // Cursor (right stick) only when:
            // a) no target-lock active AND
            // b) movement (left stick) not active
            if (!_autoTarget.IsTargetLocked && !_movement.IsWalking)
            {
                _cursor.Handle(input, actualDeltaMs);
            }

            // Engine chain: first engine returning true consumes the input
            if (!_kite.Handle(input, actualDeltaMs))
                if (!_autoTarget.Handle(input, actualDeltaMs))
                    if (!_mage.Handle(input, actualDeltaMs))
                        _support.Handle(input, actualDeltaMs);

            // Combo engine (Y button hold)
            _combo.Update(input.BtnY, actualDeltaMs);

            // Cooldown tracking
            _cooldownManager.Tick();

            // Combat macro playback
            _combat.UpdateMacroPlayback(actualDeltaMs);

            // Mob sweep
            _mobSweep.Update(actualDeltaMs);

            // Handheld mode
            _handheld.Tick(input, actualDeltaMs);

            // Feedback tick (rumble stop via timestamp)
            _feedback.Tick();

            return false; // Input not consumed by router (processed by engines)
        }

        /// <summary>
        /// Handle combat action fired event - triggers feedback and cooldown tracking
        /// </summary>
        public void OnActionFired(ButtonAction action, bool rumbleEnabled)
        {
            if (rumbleEnabled) _feedback.TriggerSkillFired();
            _cooldownManager.RegisterAction(action);
        }

        /// <summary>
        /// Handle turbo pulse event for haptic metronome
        /// </summary>
        public void OnTurboPulsed(bool rumbleEnabled, bool hapticMetronomeEnabled)
        {
            if (rumbleEnabled && hapticMetronomeEnabled)
            {
                _feedback.Trigger(FeedbackType.TurboPulse);
            }
        }
    }
}