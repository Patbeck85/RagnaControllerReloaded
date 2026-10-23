using System;
using RagnaController.Core.Input;

namespace RagnaController.Core.Utilities.Coordinators
{
    /// <summary>
    /// Coordinates hardware and controller-related engines:
    /// - ControllerService: XInput/Steam controller management
    /// - ControllerManager: Unified controller abstraction
    /// - DualSenseHardwareService: PS5 DualSense features
    /// - FeedbackSystem: Haptic/visual feedback
    /// - SnapshotBuilder: Builds controller snapshots
    /// </summary>
    public sealed class HardwareCoordinator : EngineCoordinatorBase
    {
        private readonly AdvancedLogger _logger;

        // Engines
        private ControllerService _controllerService;
        private ControllerManager _controllerManager;
        private DualSenseHardwareService _dualSense;
        private FeedbackSystem _feedback;
        private SnapshotBuilder _snapshot;
        private InterceptionMouseStrategy? _mouseEmulationFallback;
        private SendInputMouseStrategy _primaryMouseStrategy;

        public ControllerService ControllerService => _controllerService;
        public ControllerManager ControllerManager => _controllerManager;
        public DualSenseHardwareService DualSense => _dualSense;
        public FeedbackSystem Feedback => _feedback;
        public SnapshotBuilder Snapshot => _snapshot;
        public InterceptionMouseStrategy? MouseEmulationFallback => _mouseEmulationFallback;

        public HardwareCoordinator(AdvancedLogger logger)
        {
            _logger = logger;
        }

        public override void Initialize()
        {
            _logger?.Info("[HardwareCoordinator] Initializing hardware engines...");

            _controllerService = new ControllerService();
            _controllerManager = new ControllerManager();
            _dualSense = new DualSenseHardwareService();
            _feedback = new FeedbackSystem(null); // ctrl set later
            _snapshot = new SnapshotBuilder(null, null, null, null, null, null, null); // dependencies set later
            _primaryMouseStrategy = new SendInputMouseStrategy();
            _mouseEmulationFallback = null; // lazy init
        }

        public void InitializeDependencies(ControllerService ctrl, SnapshotBuilder snapshot)
        {
            _feedback = new FeedbackSystem(ctrl);
            _snapshot = snapshot;
        }

        public override bool Tick()
        {
            if (!_isRunning || _isPaused) return false;

            bool anyWork = false;

            anyWork |= _controllerService?.Tick() ?? false;
            anyWork |= _controllerManager?.Tick() ?? false;
            anyWork |= _dualSense?.Tick() ?? false;
            anyWork |= _feedback?.Tick() ?? false;
            anyWork |= _snapshot?.Tick() ?? false;

            return anyWork;
        }

        public override void ApplyProfile(Profile profile)
        {
            _dualSense?.ApplyProfile(profile);
            _feedback?.ApplyProfile(profile);
        }

        public override void Dispose()
        {
            if (_disposed) return;
            base.Dispose();

            _controllerService?.Dispose();
            _controllerManager?.Dispose();
            _dualSense?.Dispose();
            _feedback?.Dispose();
            _snapshot?.Dispose();
            _mouseEmulationFallback?.Dispose();
            _primaryMouseStrategy?.Dispose();
        }
    }
}