using System;
using RagnaController.Core.Input;

namespace RagnaController.Core.Utilities.Coordinators
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

        public NavigationCoordinator(InputCommandQueue queue, WindowTracker winTracker, AdvancedLogger logger)
        {
            _queue = queue;
            _winTracker = winTracker;
            _logger = logger;
        }

        public override void Initialize()
        {
            _logger?.Info("[NavigationCoordinator] Initializing navigation engines...");

            _movement = new MovementEngine(_queue, _winTracker);
            _cursor = new CursorEngine(_winTracker, _queue);
            _smartCursor = new SmartCursorService(_queue, _winTracker, null); // feedback set later
            _radialMenu = new RadialMenuService(null); // feedback provider set later
            _gestureRecognizer = new GestureRecognizer();
        }

        public void InitializeDependencies(IFeedbackProvider feedback, PerformanceProfiler perfProfiler)
        {
            _smartCursor = new SmartCursorService(_queue, _winTracker, feedback);
            _radialMenu = new RadialMenuService(feedback);

            _inputRouter = new InputRouter(
                null, // combat
                null, // movement (set below)
                null, // autoTarget
                null, // mage
                null, // combo
                null, // cursor
                null, // smartCursor
                null, // kite
                null, // support
                null, // overlayRouter
                null, // mobSweep
                null, // handheld
                feedback, // feedback
                null, // cooldownManager
                _radialMenu,
                _gestureRecognizer,
                perfProfiler);
        }

        public override bool Tick()
        {
            if (!_isRunning || _isPaused) return false;

            bool anyWork = false;

            anyWork |= _movement?.Tick() ?? false;
            anyWork |= _cursor?.Tick() ?? false;
            anyWork |= _smartCursor?.Tick() ?? false;
            anyWork |= _radialMenu?.Tick() ?? false;
            anyWork |= _gestureRecognizer?.Tick() ?? false;
            anyWork |= _inputRouter?.Tick() ?? false;

            return anyWork;
        }

        public override void ApplyProfile(Profile profile)
        {
            _movement?.ApplyProfile(profile);
            _cursor?.ApplyProfile(profile);
            _smartCursor?.ApplyProfile(profile);
            _radialMenu?.ApplyProfile(profile);
        }

        public override void Dispose()
        {
            if (_disposed) return;
            base.Dispose();

            _movement?.Dispose();
            _cursor?.Dispose();
            _smartCursor?.Dispose();
            _radialMenu?.Dispose();
            _gestureRecognizer?.Dispose();
            _inputRouter?.Dispose();
        }
    }
}