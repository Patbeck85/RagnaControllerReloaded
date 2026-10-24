using System;
using System.Collections.Generic;
using global::RagnaController.Core;
using global::RagnaController.Profiles;
using global::RagnaController.Controller;
using global::RagnaController.Models;
using global::RagnaController.Core.Services;

namespace RagnaController.Coordinators
{
    /// <summary>
    /// Coordinates navigation and input-related engines:
    /// - MovementEngine: Character movement
    /// - CursorEngine: Mouse cursor control
    /// - SmartCursorService: Smart cursor logic
    /// - RadialMenuService: Radial menu navigation
    /// - GestureRecognizer: Gesture recognition
    /// - InputRouter: Routes inputs to engines
    /// </summary>
    public sealed class NavigationCoordinator : EngineCoordinatorBase
    {
        private readonly InputCommandQueue _queue;
        private readonly AdvancedLogger _logger;
        private readonly WindowTracker _winTracker;
        private readonly IFeedbackProvider _feedbackProvider;
        private readonly PerformanceProfiler _perfProfiler;

        // Engines
        private MovementEngine _movement;
        private CursorEngine _cursor;
        private SmartCursorService _smartCursor;
        private RadialMenuService _radialMenu;
        private GestureRecognizer _gestureRecognizer;
        private InputRouter _inputRouter;

        public MovementEngine Movement => _movement;
        public CursorEngine Cursor => _cursor;
        public SmartCursorService SmartCursor => _smartCursor;
        public RadialMenuService RadialMenu => _radialMenu;
        public GestureRecognizer GestureRecognizer => _gestureRecognizer;
        public InputRouter InputRouter => _inputRouter;

        // State passed from EngineOrchestrator for Tick
        private ParsedInput _lastInput;
        private int _lastDeltaMs;
        private bool _lastRumbleEnabled;

        public NavigationCoordinator(InputCommandQueue queue, WindowTracker winTracker, AdvancedLogger logger, IFeedbackProvider feedbackProvider, PerformanceProfiler perfProfiler)
        {
            _queue = queue;
            _winTracker = winTracker;
            _logger = logger;
            _feedbackProvider = feedbackProvider;
            _perfProfiler = perfProfiler;
        }

        public override void Initialize()
        {
            _logger?.Info("[NavigationCoordinator] Initializing navigation engines...");

            _movement = new MovementEngine(_queue, _winTracker);
            _cursor = new CursorEngine(_winTracker, _queue);
            _smartCursor = new SmartCursorService(_queue, _winTracker, _feedbackProvider);
            _radialMenu = new RadialMenuService(null); // rumble provider set later
            _gestureRecognizer = new GestureRecognizer();
        }

        public void InitializeDependencies(
            CombatEngine combat,
            AutoTargetEngine autoTarget,
            MageEngine mage,
            ComboEngine combo,
            KiteEngine kite,
            SupportEngine support,
            OverlayRouter overlayRouter,
            MobSweepEngine mobSweep,
            HandheldModeManager handheld,
            CooldownManager cooldownManager,
            IRumbleProvider rumbleProvider)
        {
            _radialMenu = new RadialMenuService(rumbleProvider);

            _inputRouter = new InputRouter(
                combat, _movement, autoTarget, mage, combo, _cursor, _smartCursor,
                kite, support, overlayRouter, mobSweep, handheld, _feedbackProvider, cooldownManager,
                _radialMenu, _gestureRecognizer, _perfProfiler);
        }

        public override bool Tick()
        {
            if (!_isRunning || _isPaused) return false;

            bool anyWork = false;

            // MovementEngine.Update needs stick values
            anyWork |= _movement != null; // Update called via Handle
            _movement?.Tick(_lastInput, _lastDeltaMs);
            
            // CursorEngine.Handle calls Update internally
            anyWork |= _cursor?.Handle(_lastInput, _lastDeltaMs) ?? false;
            
            // SmartCursorService.Tick
            anyWork |= _smartCursor?.Tick(_lastInput) ?? false;
            
            // RadialMenuService - no Tick, state managed via events
            
            // GestureRecognizer.Tick needs right stick and R3
            _gestureRecognizer?.Tick(_lastInput.RightX, _lastInput.RightY, _lastInput.R3);
            anyWork |= _gestureRecognizer != null;
            
            // InputRouter.RouteInput
            anyWork |= _inputRouter?.RouteInput(_lastInput, _lastDeltaMs, _lastRumbleEnabled) ?? false;

            return anyWork;
        }

        public void SetTickState(ParsedInput input, int deltaMs, bool rumbleEnabled)
        {
            _lastInput = input;
            _lastDeltaMs = deltaMs;
            _lastRumbleEnabled = rumbleEnabled;
        }

        public override void ApplyProfile(Profile profile)
        {
            // Only MovementEngine implements IProfileApplicable.
            _movement?.ApplyProfile(profile);
        }

        public override void Dispose()
        {
            if (_disposed) return;
            base.Dispose();

            _movement?.Dispose();
            _radialMenu?.Dispose();
            _gestureRecognizer?.Dispose();
            _inputRouter?.Dispose();
        }
    }
}