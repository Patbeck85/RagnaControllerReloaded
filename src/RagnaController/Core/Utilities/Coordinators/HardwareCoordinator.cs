using System;






using RagnaController.Controller;

using RagnaController.Profiles;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Coordinators;

namespace RagnaController.Coordinators
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

            // Hardware engines run on their own threads (SDL2 thread for ControllerService,
            // XInput polling for ControllerManager). No per-tick work needed here.
            return false;
        }

        public override void ApplyProfile(Profile profile)
        {
            // DualSenseHardwareService and FeedbackSystem do not implement IProfileApplicable.
        }

        public override void Dispose()
        {
            if (_disposed) return;
            base.Dispose();

            _controllerService?.Dispose();
            _controllerManager?.Dispose();
            _dualSense?.Dispose();
            _feedback?.Dispose();
            _mouseEmulationFallback?.Dispose();
            _primaryMouseStrategy?.Dispose();
        }
    }
}