using System;
using System.IO;
using System.Diagnostics;






using RagnaController.Controller;

using RagnaController.Models;
using RagnaController.Profiles;
using RagnaController.Core;
using RagnaController.Coordinators;

namespace RagnaController.Coordinators
{
    /// <summary>
    /// Coordinates system monitoring and overlay engines:
    /// - SystemMonitor: System health monitoring
    /// - OverlayRouter: Overlay management
    /// - VoiceChatService: Voice chat
    /// - EngineWatchdog: Hang detection
    /// - MemoryAllocationTracker: Memory profiling
    /// - InputLatencyTracker: Latency measurement
    /// - PerformanceProfiler: Per-engine profiling
    /// - ConfigWatcher: Config hot-reload
    /// - ProfileManager: Profile management
    /// - ProfileApplier: Profile application
    /// - StandbyManager: Idle detection
    /// </summary>
    public sealed class SystemCoordinator : EngineCoordinatorBase
    {
        private readonly AdvancedLogger _logger;
        private readonly WindowTracker _winTracker;

        // Engines
        private SystemMonitor _sysMonitor;
        private OverlayRouter _overlayRouter;
        private VoiceChatService _voiceChat;
        private EngineWatchdog _watchdog;
        private MemoryAllocationTracker _memoryTracker;
        private InputLatencyTracker _latencyTracker;
        private PerformanceProfiler _perfProfiler;
        private ConfigWatcher _configWatcher;
        private ProfileManager _profileManager;
        private ProfileApplier _profileApplier;
        private StandbyManager _standbyManager;
        private HandheldModeManager _handheldModeManager;

        public SystemMonitor SysMonitor => _sysMonitor;
        public OverlayRouter OverlayRouter => _overlayRouter;
        public VoiceChatService VoiceChat => _voiceChat;
        public EngineWatchdog Watchdog => _watchdog;
        public MemoryAllocationTracker MemoryTracker => _memoryTracker;
        public InputLatencyTracker LatencyTracker => _latencyTracker;
        public PerformanceProfiler PerfProfiler => _perfProfiler;
        public ConfigWatcher ConfigWatcher => _configWatcher;
        public ProfileManager ProfileManager => _profileManager;
        public ProfileApplier ProfileApplier => _profileApplier;
        public StandbyManager StandbyManager => _standbyManager;
        public HandheldModeManager HandheldMode => _handheldModeManager;

        public SystemCoordinator(AdvancedLogger logger, WindowTracker winTracker)
        {
            _logger = logger;
            _winTracker = winTracker;
        }

        public override void Initialize()
        {
            _logger?.Info("[SystemCoordinator] Initializing system engines...");

            _sysMonitor = new SystemMonitor(_winTracker, null); // movement set later
            _overlayRouter = new OverlayRouter(null); // voice set later
            _voiceChat = new VoiceChatService(null); // queue set later
            _watchdog = new EngineWatchdog();
            _memoryTracker = MemoryAllocationRegistry.GetOrCreate("EngineOrchestrator");
            _latencyTracker = InputLatencyRegistry.GetOrCreate("EngineOrchestrator");
            _perfProfiler = new PerformanceProfiler("EngineOrchestrator", _logger);
            _configWatcher = new ConfigWatcher(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RagnaController", "settings.json"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RagnaController", "Profiles"));
            _profileManager = new ProfileManager();
            _standbyManager = new StandbyManager();
            _handheldModeManager = new HandheldModeManager(null, null, null, null, null, null); // dependencies set later
        }

        public void InitializeDependencies(
            MovementEngine movement,
            SnapshotBuilder snapshot,
            MageEngine mage,
            OverlayRouter overlayRouter,
            CombatEngine combat,
            InputCommandQueue queue,
            BackgroundTickProvider tickProvider)
        {
            _sysMonitor = new SystemMonitor(_winTracker, movement);
            _overlayRouter = new OverlayRouter(_voiceChat);
            _voiceChat = new VoiceChatService(queue);
            _voiceChat.StatusChanged += msg => VoiceStatusChanged?.Invoke(msg);
            _overlayRouter.ProfileQuickSwitch += delta => ProfileQuickSwitch?.Invoke(delta);
            _overlayRouter.RestoreMainWindowRequested += () => RestoreMainWindowRequested?.Invoke();

            _watchdog.PerformanceWarning += avgMs =>
            {
                string msg = $"⚠ CPU/Thread Overload detected! Engine running slow: {avgMs:F1}ms per tick.";
                _logger?.Warn(msg);
                LogMessage?.Invoke(msg);
            };
            _watchdog.PerformanceRecovered += () =>
            {
                string msg = "✓ CPU/Thread performance recovered to normal levels.";
                _logger?.Info(msg);
                LogMessage?.Invoke(msg);
            };
            _watchdog.HangDetected += msWithoutTick =>
            {
                RagnaControllerETW.Log.EngineHangDetected(msWithoutTick, _watchdog.HangThresholdMs);
                string msg = $"⚠ Engine-Hang erkannt: {msWithoutTick:F0} ms ohne Tick — starte Engine neu.";
                _logger?.Warn(msg);
                LogMessage?.Invoke(msg);
            };

            _configWatcher.ConfigChanged += (path) => OnConfigChanged();

            _handheldModeManager = new HandheldModeManager(
                tickProvider, snapshot, mage, _overlayRouter, combat, queue);
        }

        public override bool Tick()
        {
            if (!_isRunning || _isPaused) return false;

            bool anyWork = false;

            // Watchdog - called every tick
            _watchdog?.MarkTick();
    
            // Memory tracker - called every tick
            _memoryTracker?.RecordTick();
    
            // Standby manager - driven by EngineOrchestrator with full game state.
            // (Requires ParsedInput, Profile, IFeedbackProvider, MovementEngine — not available here.)

            return anyWork;
        }

        public override void ApplyProfile(Profile profile)
        {
            // ProfileApplier.ApplyProfileSettings is internal to the orchestrator.
            // Profile application is handled by EngineOrchestrator directly.
        }

        public override void Dispose()
        {
            if (_disposed) return;
            base.Dispose();

            _voiceChat?.Dispose();
            _watchdog?.Dispose();
            _configWatcher?.Dispose();
            _handheldModeManager?.Dispose();
        }

        // Events forwarded from internal components
        public event Action<string>? VoiceStatusChanged;
        public event Action<int>? ProfileQuickSwitch;
        public event Action? RestoreMainWindowRequested;
        public event Action<string>? LogMessage;

        private void OnConfigChanged()
        {
            _logger?.Info("[SystemCoordinator] Config changed, reloading...");
            // Trigger profile reload etc.
        }
    }
}