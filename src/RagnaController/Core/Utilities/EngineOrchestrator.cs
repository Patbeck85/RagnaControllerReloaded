using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RagnaController.Models;
using RagnaController.Controller;
using RagnaController.Profiles;

namespace RagnaController.Core
{
    /// <summary>
    /// ARCH-001: Extracted from HybridEngine - Main tick coordination & lifecycle management.
    /// Responsible for: Engine initialization, tick loop orchestration, Start/Stop/Pause/Resume/Shutdown.
    /// </summary>
    /// <remarks>
    /// Thread safety: Safe to call from background engine thread.
    /// UI access must go through Dispatcher.Invoke.
    /// All methods are thread-safe against concurrent calls.
    /// </remarks>
    /// <seealso cref="TECH-061"/>
    public class EngineOrchestrator : IDisposable
#pragma warning disable CS8618 // All engine fields are assigned in constructor (see TECH-061 startup validation)
    {
        private readonly ITickProvider _tickProvider = default!;
        private readonly IMessenger _messenger = default!;
        private readonly AdvancedLogger _logger = null!;
        private readonly ControllerService _ctrl = default!;
        private readonly WindowTracker _winTracker = default!;
        private readonly InputReader _inputReader = default!;
        private readonly SystemMonitor _sysMonitor = default!;
        private readonly SnapshotBuilder _snapshot = default!;
        private readonly InputCommandQueue? _queue = default!;

        // ROB-002: input emulation failover — kernel strategy holds an interception context
        // (P/Invoke-Handle) and muss beim Shutdown deterministisch freigegeben are.
        private InterceptionMouseStrategy? _mouseEmulationFallback;

        // FEAT-014: Session-Replay — JSONL-Aufzeichnung pro Engine-Session (only if in Settings enabled).
        private SessionRecorder? _sessionRecorder;

        // ── Engines ──
#pragma warning disable CS8618 // All engine fields are assigned in constructor (see TECH-061 startup validation)
                private readonly MovementEngine _movement = default!;
                private readonly CombatEngine _combat = default!;
                private readonly AutoTargetEngine _autoTarget = default!;
                private readonly MageEngine _mage = default!;
                private readonly ComboEngine _combo = default!;
                private readonly CursorEngine _cursor = default!;
                private readonly SmartCursorService _smartCursor = default!;
                private readonly KiteEngine _kite = default!;
                private readonly SupportEngine _support = default!;
                private readonly IFeedbackProvider _feedback = default!;
                private readonly OverlayRouter _overlayRouter = default!;
                private readonly VoiceChatService _voice = default!;
                private readonly MobSweepEngine _mobSweep = default!;
                private readonly HandheldModeManager _handheld = default!;
                private readonly EngineWatchdog _watchdog = default!;
                private readonly CooldownManager _cooldownManager = default!;
                private readonly DualSenseHardwareService _dualSense = default!;

                // FEAT-006: Groand Spell Engine
                                private readonly GroundSpellEngine _groundSpell = default!;

                                // FEAT-007: Skill Orchestrator
                                private readonly SkillOrchestrator _skillOrchestrator = default!;
                                private readonly DefaultRotationProvider _rotationProvider = default!;

                                // FEAT-008: Buff/Debuff Tracking
                                                                        private readonly BuffManager _buffManager = default!;

                                                                        // FEAT-011: Item Manager (Auto-Potion & Item-Verwaltung)
                                                                        private readonly ItemManagerEngine _itemManager = default!;

                                                                        // FEAT-012: Party Manager (Auto-Heal-Loop for Party-Mitglieder)
                                                                        private readonly PartyManager _partyManager = default!;

                                                                // PERF-004: Memory Allocation Tracking
                                                                                                                                private readonly MemoryAllocationTracker _memoryTracker = default!;
                                                                                                                                                // PERF-005: Input Latency Tracking
                                                                                                                                                private readonly InputLatencyTracker _latencyTracker = default!;
                    
                                                                                                                                                                // ProfileManager for auto-switching profiles based on controller
                                                                                                                                                                private readonly ProfileManager _profileManager = default!;

                                                                                                                                                                // UX-016: Radial Menu + Gesten (Handheld-First Navigation)
                                                                                                                                                                private readonly RadialMenuService _radialMenu = default!;
                                                                                                                                                                private readonly GestureRecognizer _gestureRecognizer = default!;
#pragma warning restore CS8618

                                                                                                                                                                // ── Runtime ──
                private volatile bool _isRunning;
                public bool IsRunning { get => _isRunning; private set => _isRunning = value; }
                private volatile bool _isPaused;
                public bool IsPaused { get => _isPaused; private set => _isPaused = value; }

                private readonly ControllerManager _controllerManager = default!;

        public string ControllerName => _ctrl?.ControllerName ?? "Kein Controller";
        public string ControllerType => _ctrl?.ControllerType ?? "XBOX";

        // ── Events ──
        public event Action<EngineStatus>? StatusChanged;
        public event Action<ControllerSnapshot>? SnapshotUpdated;
        public event Action<string>? LogMessage;
        public event Action<string>? BatteryChanged;
        public event Action<string>? ControllerConnected;
        public event Action? ControllerDisconnected;
        public event Action<int>? ProfileQuickSwitch;
        public event Action? RestoreMainWindowRequested;
        public event Action<string>? VoiceStatusChanged;
        public event Action<string>? ProfileSwitched;

        // ── Constants ──
        private const int UI_INTERVAL = 4;
        private int _uiTick;
        private int _reconnectTick;
        private const int RECONNECT_INTERVAL = 375;
        private int _actualDeltaMs;

        public EngineOrchestrator(
                    ITickProvider tickProvider,
                    IMessenger messenger,
                    InputCommandQueue? queue,
                    AdvancedLogger logger)
                {
                    _tickProvider = tickProvider;
                    _messenger = messenger;
                    _logger = logger;
                    _queue = queue;

                    this.LogMessage += msg => _logger?.Info(msg);

                    _ctrl = new ControllerService();
                                        // Subscribe to controller events for immediate UI sync
                                        _ctrl.ControllerDetected += (_, __) =>
                                        {
                                            ControllerConnected?.Invoke(ControllerName);
                                            _messenger.Publish(new EngineStatusMessage(EngineStatus.Running, ControllerName));
                                        };
                                        _ctrl.ControllerDisconnected += (_, __) =>
                                        {
                                            ControllerDisconnected?.Invoke();
                                            _messenger.Publish(new EngineStatusMessage(EngineStatus.NoController, "Kein Controller"));
                                        };
                    
                                        // Create unified ControllerManager (SDL2 + XInput fallback)
                                        _controllerManager = new ControllerManager();
                                        _controllerManager.ControllerConnected += (_, __) =>
                                        {
                                            ControllerConnected?.Invoke(_controllerManager.ControllerName);
                                            _messenger.Publish(new EngineStatusMessage(EngineStatus.Running, _controllerManager.ControllerName));
                                        };
                                        _controllerManager.ControllerDisconnected += (_, __) =>
                                        {
                                            ControllerDisconnected?.Invoke();
                                            _messenger.Publish(new EngineStatusMessage(EngineStatus.NoController, "Kein Controller"));
                                        };
                    
                                        _profileManager = new ProfileManager();
                    _profileManager.Logger += msg => _logger?.Info(msg);
            
                    // Register default controller-to-profile mapping
                    // Auto-map Xbox controller to the "Tactician" profile if available
                    var profiles = _profileManager.GetAllNames();
                    if (profiles.Contains("Tactician"))
                    {
                        _profileManager.RegisterControllerMapping("", "Tactician");
                    }
                    else if (profiles.Contains("Novice"))
                    {
                        _profileManager.RegisterControllerMapping("", "Novice");
                    }

                    _winTracker = new WindowTracker();
                    _inputReader = new InputReader(_ctrl);

                                // PERF-005: Initialize Input Latency Tracker for end-to-end latency measurement
                                _latencyTracker = InputLatencyRegistry.GetOrCreate("EngineOrchestrator");
                                _logger?.Info("[EngineOrchestrator] InputLatencyTracker initialized — Target P99: 5ms");

                                var engineQueue = _queue ?? new InputCommandQueue(_latencyTracker, _ctrl.ControllerGuid ?? "Default");
            _movement = new MovementEngine(engineQueue, _winTracker);
            _combat = new CombatEngine(_winTracker, engineQueue);
            _autoTarget = new AutoTargetEngine(engineQueue);
            _mage = new MageEngine();
            _combo = new ComboEngine(engineQueue);
            _cursor = new CursorEngine(_winTracker, engineQueue);
            _feedback = new FeedbackSystem(_ctrl);
#pragma warning disable CS8604
            _smartCursor = new SmartCursorService(queue, _winTracker, _feedback);
#pragma warning restore CS8604
            _kite = new KiteEngine(engineQueue);
            _support = new SupportEngine(engineQueue);
            _voice = new VoiceChatService(engineQueue);
            _voice.StatusChanged += msg => VoiceStatusChanged?.Invoke(msg);
            _overlayRouter = new OverlayRouter(_voice);
            _mobSweep = new MobSweepEngine(engineQueue);
            _dualSense = new DualSenseHardwareService();

                        _sysMonitor = new SystemMonitor(_winTracker, _movement);
                        _snapshot = new SnapshotBuilder(_autoTarget, _mage, _combo, _winTracker, _cursor, _smartCursor, _skillOrchestrator);

                        _handheld = new HandheldModeManager(
                            _tickProvider as BackgroundTickProvider, _snapshot, _mage, _overlayRouter, _combat, engineQueue);

                        _watchdog = new EngineWatchdog();
                        _watchdog.PerformanceWarning += (avgMs) =>
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

                        // ROB-001: Hang-Erkennung → Auto-Restart des Engines.
                        // WICHTIG: Restart läuft on eigenem Task — der Handler feuert vom
                        // Watchdog-Poll-Loop; ein direkter Stop() dort würde den eigenen
                        // Poll-Task blockieren (Watchdog.Stop wartet on den Loop).
                        _watchdog.HangDetected += msWithoutTick =>
                        {
                            RagnaControllerETW.Log.EngineHangDetected(msWithoutTick, _watchdog.HangThresholdMs);
                            string msg = $"⚠ Engine-Hang erkannt: {msWithoutTick:F0} ms ohne Tick — starte Engine neu.";
                            _logger?.Warn(msg);
                            LogMessage?.Invoke(msg);
                            _ = Task.Run(() => Restart()).ContinueWith(t =>
                            {
                                if (t.IsFaulted)
                                {
                                    _logger?.Error($"[EngineOrchestrator] Engine restart failed: {t.Exception}");
                                    LogMessage?.Invoke($"⚠ Engine-Restart fehlgeschlagen: {t.Exception.Message}");
                                }
                            }, TaskContinuationOptions.OnlyOnFaulted);
                        };

                        _cooldownManager = new CooldownManager(_messenger, _feedback);

                        // FEAT-007: Initialize Skill Orchestrator
                                                _rotationProvider = new DefaultRotationProvider();
                                                _skillOrchestrator = new SkillOrchestrator(engineQueue, _rotationProvider);
                                                _skillOrchestrator.SetDependencies(
                                                    autoTarget: _autoTarget,
                                                    mage: _mage,
                                                    support: _support,
                                                    combo: _combo,
                                                    cooldownManager: _cooldownManager,
                                                    groundSpell: _groundSpell);

                        _overlayRouter.ProfileQuickSwitch += delta => ProfileQuickSwitch?.Invoke(delta);
                        _overlayRouter.RestoreMainWindowRequested += () => RestoreMainWindowRequested?.Invoke();

            // Initialize decomposed components FIRST (before using them in event handlers)
            _standbyManager = new StandbyManager();

            // UX-016: Radial Menu + Gesten (Handheld-First Navigation)
            _radialMenu = new RadialMenuService(new FeedbackRumbleProvider(_feedback));
            _gestureRecognizer = new GestureRecognizer();

            _inputRouter = new InputRouter(
                _combat, _movement, _autoTarget, _mage, _combo, _cursor, _smartCursor,
                _kite, _support, _overlayRouter, _mobSweep, _handheld, _feedback, _cooldownManager,
                _radialMenu, _gestureRecognizer);
            _profileApplier = new ProfileApplier(this, _messenger);

            // TECH-059: Config Hot-Reload — überwacht settings.json and profiles.json
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RagnaController", "settings.json");
            var profilesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RagnaController", "Profiles");
            _configWatcher = new ConfigWatcher(settingsPath, profilesPath);
            _configWatcher.ConfigChanged += OnConfigChanged;

            // ROB-002: Input-Emulation-Failover (SendInput ↔ Kernel).
            // Primär = SendInput (Standard); Fallback = Interception-Treiber, only if it
            // on this Maschine verfügbar is. Ohne Treiber bleibt alles bei SendInput —
            // der Failover-Schalter is then nie ausgelöst (KISS: no No-Op-Overhead).
            var primaryStrategy = new SendInputMouseStrategy();
            try { _mouseEmulationFallback = new InterceptionMouseStrategy(); }
                        catch (Exception ex)
                        {
                            _mouseEmulationFallback = null; // DLL/Treiber fehlt → bewusst ohne Fallback
                            System.Diagnostics.Debug.WriteLine($"[EngineOrchestrator] InterceptionMouseStrategy init failed: {ex.Message}");
                        }
            if (_mouseEmulationFallback != null && !_mouseEmulationFallback.IsAvailable)
            {
                _mouseEmulationFallback.Dispose();
                _mouseEmulationFallback = null;
            }
            _inputRouter.InitializeFailover(primaryStrategy, _mouseEmulationFallback, Models.Settings.Load());
            engineQueue.SendInputLatencyRecorded += _inputRouter.RecordSendInputLatency;

            // FEAT-014: Session-Replay — JSONL-Aufzeichnung pro Engine-Session (opt-in via Settings).
            // Overhead <1ms: wiederverwendeter StringBuilder, ein Lock, 50-MB-Rotation.
            if (Models.Settings.Load().EnableSessionRecording)
            {
                string sessionDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RagnaController", "Replays",
                    $"session-{DateTime.Now:yyyyMMdd-HHmmss}");
                _sessionRecorder = new SessionRecorder(sessionDir, "EngineOrchestrator");
                _sessionRecorder.Start();
                engineQueue.OnCommandEnqueued += _sessionRecorder.RecordInput;
            }

            // NOW subscribe to events - _inputRouter is guaranteed non-null
                                    _combat.ActionFired += action =>
                                    {
                                        if (_rumbleEnabled) _feedback.TriggerSkillFired();
                                        _messenger.Publish(new ActionFiredMessage(action.Label, ActionFiredKind.Skill));
                                        _cooldownManager.RegisterAction(action);
                                        // Also notify InputRouter
                                        _inputRouter.OnActionFired(action, _rumbleEnabled);

                                        // FEAT-014: Session-Replay — gefeuerte Aktion aufzeichnen (deterministische Wiedergabe)
                                        _sessionRecorder?.RecordAction(action.Label, ActionFiredKind.Skill);

                                        // FEAT-006: Register groand spell if applicable
                                                                    if (action.IsGroundSpell && _winTracker.IsTracking && _groundSpell != null)
                                                                    {
                                                                        // Get current world position (mouse cursor position in game world)
                                                                        var centerX = _winTracker.CenterX;
                                                                        var centerY = _winTracker.CenterY;
                                                                        _groundSpell.RegisterGroundSpell(action, centerX, centerY, action.Label);
                                                                    }

                                        // FEAT-008: Register buff tracking if enabled
                                                                                                            if (action.TrackBuff && _buffManager != null)
                                                                                                            {
                                                                                                                _buffManager.RegisterBuffExplicit(
                                                                                                                    action.Label,
                                                                                                                    action.BuffDurationSec,
                                                                                                                    action.BuffWarningSec,
                                                                                                                    autoRecast: false, // Auto-recast not yet implemented in ButtonAction
                                                                                                                    recastKey: action.Key
                                                                                                                );
                                                                                                            }
                                    };


            _tickProvider.Tick += OnTick;
                                    _groundSpell = new GroundSpellEngine(engineQueue);

                                    // FEAT-008: Initialize Buff Manager
                                    _buffManager = new BuffManager(engineQueue, _cooldownManager);

                                    // FEAT-011: Initialize Item Manager (Auto-Potion & Item-Verwaltung)
                                    _itemManager = new ItemManagerEngine(engineQueue);
                                    _itemManager.ItemFired += item =>
                                    {
                                        string msg = $"[Item] Auto-Item eingesetzt: {item.Name}";
                                        _logger?.Info(msg);
                                        LogMessage?.Invoke(msg);
                                        _messenger.Publish(new ItemFiredMessage(item.Name));
                                    };

                                    // FEAT-012: Initialize Party Manager (Auto-Heal-Loop)
                                    _partyManager = new PartyManager(engineQueue);
                                    _partyManager.MemberHealed += index =>
                                    {
                                        string msg = $"[Party] Auto-Heal: Mitglied #{index + 1} geheilt";
                                        _logger?.Info(msg);
                                        LogMessage?.Invoke(msg);
                                    };

                                    // FEAT-013: Target-Management — Zielverlust (Tod/Reichweite) loggen.
                                    // LostTarget feuert vom AutoTargetEngine.Update() on dem Tick-Thread;
                                    // only Log + UI-Nachricht, keine Engine-Aktionen (KISS).
                                    _autoTarget.LostTarget += reason =>
                                    {
                                        string label = reason == "dead" ? "Ziel tot" : "Ziel außerhalb der Reichweite";
                                        string msg = $"[Target] Ziel verloren ({label}) — starte Auto-Retargeting.";
                                        _logger?.Info(msg);
                                        LogMessage?.Invoke(msg);
                                    };

                                                                        // PERF-004: Initialize Memory Allocation Tracker
                                                                        _memoryTracker = new MemoryAllocationTracker("EngineOrchestrator", _logger);

	// TECH-061: Startup validation — ensure all engines are initialized.
	// Throws clear InvalidOperationException instead of silent NullReferenceException at runtime.
	var missingEngines = new List<string>();
	if (_movement == null) missingEngines.Add(nameof(MovementEngine));
	if (_combat == null) missingEngines.Add(nameof(CombatEngine));
	if (_autoTarget == null) missingEngines.Add(nameof(AutoTargetEngine));
	if (_mage == null) missingEngines.Add(nameof(MageEngine));
	if (_combo == null) missingEngines.Add(nameof(ComboEngine));
	if (_cursor == null) missingEngines.Add(nameof(CursorEngine));
	if (_smartCursor == null) missingEngines.Add(nameof(SmartCursorService));
	if (_kite == null) missingEngines.Add(nameof(KiteEngine));
	if (_support == null) missingEngines.Add(nameof(SupportEngine));
	if (_overlayRouter == null) missingEngines.Add(nameof(OverlayRouter));
	if (_mobSweep == null) missingEngines.Add(nameof(MobSweepEngine));
	if (_handheld == null) missingEngines.Add(nameof(HandheldModeManager));
	if (_feedback == null) missingEngines.Add(nameof(IFeedbackProvider));
	if (_cooldownManager == null) missingEngines.Add(nameof(CooldownManager));
	if (_groundSpell == null) missingEngines.Add(nameof(GroundSpellEngine));
	if (_buffManager == null) missingEngines.Add(nameof(BuffManager));
	if (_itemManager == null) missingEngines.Add(nameof(ItemManagerEngine));
	if (_partyManager == null) missingEngines.Add(nameof(PartyManager));
	if (_inputRouter == null) missingEngines.Add(nameof(InputRouter));
	if (_radialMenu == null) missingEngines.Add(nameof(RadialMenuService));
	if (_gestureRecognizer == null) missingEngines.Add(nameof(GestureRecognizer));

	if (missingEngines.Count > 0)
	{
	string msg = $"[EngineOrchestrator] Initialization incomplete — missing engines: {string.Join(", ", missingEngines)}";
	_logger?.Error(msg);
	throw new InvalidOperationException(msg);
	}

	_logger?.Info("[EngineOrchestrator] All engines initialized successfully.");

                                                        }

                                                        // These will be set via ProfileApplier
        private bool _soundEnabled = true;
        private bool _rumbleEnabled = true;
        private bool _isRenewal = true;
        private Profile? _currentProfile;

        // New decomposed components
        private readonly StandbyManager _standbyManager = default!;
        private readonly InputRouter _inputRouter = default!;
        private readonly ProfileApplier _profileApplier = default!;

        // TECH-059: Config Hot-Reload Watcher
        private readonly ConfigWatcher? _configWatcher = default!;

        // Expose engines for ProfileApplier
                public MovementEngine Movement => _movement;
                public CombatEngine Combat => _combat;
                public AutoTargetEngine AutoTarget => _autoTarget;
                public MageEngine Mage => _mage;
                public ComboEngine Combo => _combo;
                public CursorEngine Cursor => _cursor;
                public SmartCursorService SmartCursor => _smartCursor;
                public KiteEngine Kite => _kite;
                public SupportEngine Support => _support;
                public IFeedbackProvider Feedback => _feedback;
                public OverlayRouter OverlayRouter => _overlayRouter;
                public MobSweepEngine MobSweep => _mobSweep;
                public HandheldModeManager Handheld => _handheld;
                public CooldownManager CooldownManager => _cooldownManager;
                public DualSenseHardwareService DualSense => _dualSense;
                public SystemMonitor SysMonitor => _sysMonitor;
                public SnapshotBuilder Snapshot => _snapshot;
                public ControllerService Controller => _ctrl;
                public WindowTracker WinTracker => _winTracker;
                public ControllerManager ControllerManager => _controllerManager;

        // Expose runtime flags for ProfileApplier
        public bool SoundEnabled { get => _soundEnabled; set => _soundEnabled = value; }
        public bool RumbleEnabled { get => _rumbleEnabled; set => _rumbleEnabled = value; }
        public bool IsRenewal { get => _isRenewal; set => _isRenewal = value; }
        public Profile? CurrentProfile { get => _currentProfile; set => _currentProfile = value; }
        public InputCommandQueue? CommandQueue => _queue;
        public ITickProvider TickProvider => _tickProvider;
        public IMessenger Messenger => _messenger;

        // Expose new components
                        public StandbyManager StandbyManager => _standbyManager;
                        public InputRouter InputRouter => _inputRouter;
                        public ProfileApplier ProfileApplier => _profileApplier;

                        // FEAT-006: Groand Spell Engine
                        public GroundSpellEngine GroundSpell => _groundSpell;

                        // FEAT-007: Skill Orchestrator
                                                public SkillOrchestrator SkillOrchestrator => _skillOrchestrator;
                                                public DefaultRotationProvider RotationProvider => _rotationProvider;

                                                // FEAT-008: Buff Manager
                                                                                                public BuffManager BuffManager => _buffManager;

                                                                                                // FEAT-011: Item Manager (Auto-Potion & Item-Verwaltung)
                                                                                                public ItemManagerEngine ItemManager => _itemManager;

                                                                                                // FEAT-012: Party Manager (Auto-Heal-Loop)
                                                                                                public PartyManager PartyManager => _partyManager;

                                                                                                // PERF-005: Input Latency Tracker
                                                                                                                                                                                                                                public InputLatencyTracker LatencyTracker => _latencyTracker;

                                                                                                                                                                                                                                // PERF-004: Memory Allocation Tracker
                                                                                                                                                                                                                                public MemoryAllocationTracker MemoryTracker => _memoryTracker;

                                // Public method for external log subscription - takes a message and invokes the event
        /// <summary>
        /// SubscribeToLog method.
        /// <param name="message">message parameter.</param>
        /// </summary>
        public void SubscribeToLog(string message) => LogMessage?.Invoke(message);

        private void OnTick(object? sender, EventArgs e)
        {
            try
            {
                // ROB-001: Heartbeat ZUERST — auch bei Disconnect/Pause/Focus-Lost muss der
                // Watchdog sehen, dass die Tick-Schlebe lebt (sonst false-positive Hang).
                _watchdog.MarkTick();

                var sw = Stopwatch.StartNew();
                var input = _inputReader.Read();

                if (!input.IsConnected)
                {
                    HandleDisconnected();
                    return;
                }

                _reconnectTick = 0;

                if (!IsRunning)
                {
                    HandleConnected(input);
                }

                if (IsPaused) return;

                bool wasFocusLocked = _sysMonitor.IsFocusLocked;
                _sysMonitor.Update();

                if (!_sysMonitor.IsTracking)
                {
                    HandleFocusLost();
                    return;
                }

                if (_sysMonitor.IsFocusLocked)
                {
                    HandleFocusLocked(sw);
                    return;
                }

                // Exact delta calculation
                int baseDelta = _tickProvider.IntervalMs;
                if (_tickProvider is BackgroundTickProvider btp && btp.BatteryThrottle)
                    baseDelta *= 2;

                _actualDeltaMs = (int)sw.Elapsed.TotalMilliseconds;
                                if (_actualDeltaMs == 0) _actualDeltaMs = baseDelta;
                                if (_actualDeltaMs > 32) _actualDeltaMs = baseDelta;

                                // Smart Standby check - delegate to StandbyManager
                                if (_standbyManager.Tick(input, _currentProfile, _rumbleEnabled, _feedback, _movement, _logger, out bool shouldSkip))
                                {
                                    if (shouldSkip)
                                    {
                                        // Throttle polling to ~20Hz instead of 125Hz to save CPU
                                        Thread.Sleep(50);
                                        return; // Skip all combat routing and UI updates!
                                    }
                                }

                // Routing - delegate to InputRouter
                                _inputRouter.RouteInput(input, _actualDeltaMs, _rumbleEnabled);

                                // FEAT-006: Update groand spells
                                                                _groundSpell?.Handle(input, _actualDeltaMs);

                                                                // FEAT-007: Update skill orchestrator (class-specific rotations)
                                                                                                                                _skillOrchestrator?.Update(input, _actualDeltaMs, 
                                                                                                                                                                                                    _autoTarget?.CurrentTarget != null,
                                                                                                                                                                                                    _autoTarget?.CurrentTargetDistance ?? 0f,
                                                                                                                                                                                                    _combat.CurrentSP,
                                                                                                                                                                                                    _combat.CurrentHPPercent,
                                                                                                                                                                                                    _movement.IsMoving,
                                                                                                                                                                                                    _autoTarget?.IsFacingTarget ?? false,
                                                                                                                                                                                                    _support?.ActiveBuffs ?? new(),
                                                                                                                                                                                                    _support?.ActiveDebuffs ?? new(),
                                                                                                                                                                                                    _autoTarget?.NearbyEnemyCount ?? 0,
                                                                                                                                                                                                    _groundSpell?.GetActiveSpellNames() ?? new());

                                                                                                                                                                                                // FEAT-008: Update buff/debuff tracking
                                                                                                                                                                                                                                                                                                                                _buffManager?.Update(_actualDeltaMs);

                                                                                                                                                                                                // FEAT-011: Update item manager (Auto-Potion bei HP/SP-Schwelle)
                                                                                                                                                                                                _itemManager.Update(_combat.CurrentHPPercent, _combat.CurrentSP, _actualDeltaMs);

                                                                                                                                                                                                // FEAT-012: Update party manager (Auto-Heal-Loop)
                                                                                                                                                                                                _partyManager.Update();

                                                                                                                                                                                                // PERF-004: Record memory allocation stats
                                                                                                                                                                                                _memoryTracker?.RecordTick();

                                // UI Update
                if (++_uiTick < UI_INTERVAL) return;
                _uiTick = 0;

                var snap = _snapshot.Build(input, _sysMonitor.IsFocusLocked, sw.Elapsed.TotalMilliseconds);
                SnapshotUpdated?.Invoke(snap);
                _messenger.Publish(new SnapshotReadyMessage(snap));

                // FEAT-014: Session-Replay — Engine-Zustand (intern 10 Hz gedrosselt)
                _sessionRecorder?.RecordState(snap);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"[Engine] Tick error: {ex.Message}");
            }
        }

        private void HandleDisconnected()
        {
            if (IsRunning)
            {
                IsRunning = false;
                ControllerDisconnected?.Invoke();
                _messenger.Publish(new EngineStatusMessage(EngineStatus.NoController, "Kein Controller"));
                _movement.ForceStop();
                _queue?.LeftUp();
                _queue?.KeyUp(VirtualKey.ArrowLeft);
                _queue?.KeyUp(VirtualKey.ArrowRight);
                _queue?.KeyUp(VirtualKey.ArrowUp);
                _queue?.KeyUp(VirtualKey.ArrowDown);
                LogMessage?.Invoke("[Engine] Controller disconnected — alle Engines deaktiviert");
            }

            if (++_reconnectTick >= RECONNECT_INTERVAL)
            {
                _reconnectTick = 0;
                _ctrl.DetectController();
            }
        }

        private void HandleConnected(ParsedInput input)
                {
                    IsRunning = true;
                    ControllerConnected?.Invoke(ControllerName);
                    _messenger.Publish(new EngineStatusMessage(EngineStatus.Running, ControllerName));
                    string bat = _ctrl.BatteryLevel;
                    BatteryChanged?.Invoke(bat);
                    _messenger.Publish(new BatteryChangedMessage(bat));
            
                    // FEAT-009: Profile auto-switch based on connected controller
                    _profileManager.OnControllerConnected(
                        _ctrl.ControllerGuid,
                        _ctrl.ControllerName,
                        _ctrl.ControllerType);
                    ProfileSwitched?.Invoke(_profileManager.ActiveProfileName);
            
                    // Update InputReader with new profile settings (deadzone, normalization)
                    _inputReader.UpdateProfileSettings(_profileManager.ActiveProfile);
                }

        private void HandleFocusLost()
        {
            _movement.ForceStop();
            _queue?.LeftUp();
        }

        private void HandleFocusLocked(Stopwatch sw)
        {
            if (++_uiTick >= UI_INTERVAL)
            {
                _uiTick = 0;
                var lockedSnap = _snapshot.Build(new ParsedInput { IsConnected = true }, true, sw.Elapsed.TotalMilliseconds);
                SnapshotUpdated?.Invoke(lockedSnap);
                _messenger.Publish(new SnapshotReadyMessage(lockedSnap));
            }
        }

        /// <summary>
        /// Starts the 125Hz tick loop and all engine subsystems.
        /// </summary>
        /// <exception cref="InvalidOperationException">If already running.</exception>
        public void Start()
                {
                    _tickProvider.Start();
                    IsRunning = true;
                    StatusChanged?.Invoke(EngineStatus.Running);
                    _queue?.Start(); // Ensure InputCommandQueue consumer is running
                    _watchdog.Start(); // ROB-001: Hang-Erkennung nur bei aktivem Engine
                    _itemManager.Start(); // FEAT-011: Item-Prüfung nur bei aktivem Engine
                    _partyManager.Start(); // FEAT-012: Auto-Heal nur bei aktivem Engine
                }

        /// <summary>
        /// Stops the 125Hz tick loop and all engine subsystems.
        /// </summary>
        public void Stop()
        {
            _tickProvider.Stop();
            IsRunning = false;
            StatusChanged?.Invoke(EngineStatus.Stopped);
            _watchdog.Stop(); // ROB-001: kein Hang-Check ohne aktive Tick-Schleife
            _itemManager.Stop(); // FEAT-011: keine Auto-Items ohne aktive Engine
            _partyManager.Stop(); // FEAT-012: kein Auto-Heal ohne aktive Engine
        }

        /// <summary>
        /// ROB-001: Self-healing Restart of the engine (e.g., after detected hang).
        /// Stops the tick loop and starts it again immediately; Input-Queue keeps running
        /// so no keys get "stuck". Thread-safe against parallel restarts.
        /// </summary>
        public void Restart()
        {
            if (_isRestarting) return;
            _isRestarting = true;
            try
            {
                Stop();
                Start();
                int count = Interlocked.Increment(ref _restartCount);
                RagnaControllerETW.Log.EngineWatchdogRestart("hang-detected", count);
                string msg = $"✓ Engine erfolgreich neu gestartet (Watchdog-Restart #{count}).";
                _logger?.Info(msg);
                LogMessage?.Invoke(msg);
            }
            finally
            {
                _isRestarting = false;
            }
        }

        private volatile bool _isRestarting;
        private int _restartCount;
        private volatile bool _isShutDown;

        /// <summary>
        /// Pauses the tick loop without stopping subsystems.
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
            _feedback.StopRumble();
            StatusChanged?.Invoke(EngineStatus.Stopped);
        }

        /// <summary>
        /// Resumes the tick loop after a pause.
        /// </summary>
        public void Resume()
        {
            IsPaused = false;
            StatusChanged?.Invoke(EngineStatus.Running);
        }

        /// <summary>
        /// Shuts down the engine and releases all resources.
        /// </summary>
        public void Shutdown()
                                        {
                                            if (_isShutDown) return; // idempotent: Dispose() ruft Shutdown() erneut auf
                                            _isShutDown = true;
                                            Stop();
                                            _feedback.StopRumble();
                                            _ctrl.Dispose();
                                            _controllerManager?.Dispose();
                                            _handheld.Dispose();
                                            _voice.Dispose();
                                            _memoryTracker?.Dispose();
                                            _latencyTracker?.Dispose();
                                            _watchdog.Dispose(); // ROB-001: deterministisch stoppbar
                                            _mouseEmulationFallback?.Dispose(); // ROB-002: Interception-Kontext freigeben
                                            _sessionRecorder?.Dispose(); // FEAT-014: JSONL-Aufzeichnung flushen + Writer schließen
                                            _itemManager.Dispose(); // FEAT-011: Item-Prüfung endgültig stoppen
                                            _partyManager.Dispose(); // FEAT-012: Auto-Heal endgültig stoppen

                                            // TECH-005: Clean up static registries to prevent memory leaks on restart
                                                                                        FrameBudgetRegistry.DisposeAll();
                                                                                        GpuOverlayProfilerRegistry.DisposeAll();
                                                                                        InputLatencyRegistry.DisposeAll();
                                                                                        MemoryAllocationRegistry.DisposeAll();

                                                                                        // TECH-051: Clean up static collections
                                                                                        ProfileShareService.DisposeHttpClient();
                                                                                        ProfileShareService.ClearShareCodeCache();
                                                                                        WindowSwitcher.Clear();

                                                                                        // TECH-053: Dispose InputCommandQueue to join consumer thread
                                                                                        _queue?.Dispose();

                                            _logger?.Info("=== Engine Shutdown ===");
                                            _logger?.Dispose();
                                        }

        /// <summary>
        /// TASK-1613: Isoliert den Profil-Switch, indem alle Engine-Zustände zurückgesetzt werden.
        /// Verhindert, dass Zustand aus dem vorherigen Profil in das neue übergeht (z.B. laufender
        /// Kampf, aktive Buffs, gebliebene Tasten, etc.).
        /// </summary>
        public void IsolateProfileSwitch()
        {
            // 1. Alle Gameplay-Engines reseten
            _movement.Reset();
            _autoTarget.Reset();
            _mage.Reset();
            _combo.Reset();
            _cursor.Reset();
            _smartCursor.Reset();
            _kite.Reset();
            _support.Reset();
            _mobSweep.Reset();
            _itemManager.Reset();
            _partyManager.Reset();
            _groundSpell.Reset();
            _skillOrchestrator?.Reset();

            // 2. InputCommandQueue leeren - alle Tasten loslassen
            _queue?.LeftUp();
            _queue?.RightUp();
            _queue?.KeyUp(VirtualKey.A);
            _queue?.KeyUp(VirtualKey.B);
            _queue?.KeyUp(VirtualKey.C);
            _queue?.KeyUp(VirtualKey.D);
            _queue?.KeyUp(VirtualKey.E);
            _queue?.KeyUp(VirtualKey.F);
            _queue?.KeyUp(VirtualKey.G);
            _queue?.KeyUp(VirtualKey.H);
            _queue?.KeyUp(VirtualKey.I);
            _queue?.KeyUp(VirtualKey.J);
            _queue?.KeyUp(VirtualKey.K);
            _queue?.KeyUp(VirtualKey.L);
            _queue?.KeyUp(VirtualKey.M);
            _queue?.KeyUp(VirtualKey.N);
            _queue?.KeyUp(VirtualKey.O);
            _queue?.KeyUp(VirtualKey.P);
            _queue?.KeyUp(VirtualKey.Q);
            _queue?.KeyUp(VirtualKey.R);
            _queue?.KeyUp(VirtualKey.S);
            _queue?.KeyUp(VirtualKey.T);
            _queue?.KeyUp(VirtualKey.U);
            _queue?.KeyUp(VirtualKey.V);
            _queue?.KeyUp(VirtualKey.W);
            _queue?.KeyUp(VirtualKey.X);
            _queue?.KeyUp(VirtualKey.Y);
            _queue?.KeyUp(VirtualKey.Z);
            _queue?.KeyUp(VirtualKey.F1);
            _queue?.KeyUp(VirtualKey.F2);
            _queue?.KeyUp(VirtualKey.F3);
            _queue?.KeyUp(VirtualKey.F4);
            _queue?.KeyUp(VirtualKey.F5);
            _queue?.KeyUp(VirtualKey.F6);
            _queue?.KeyUp(VirtualKey.F7);
            _queue?.KeyUp(VirtualKey.F8);
            _queue?.KeyUp(VirtualKey.F9);
            _queue?.KeyUp(VirtualKey.F10);
            _queue?.KeyUp(VirtualKey.F11);
            _queue?.KeyUp(VirtualKey.F12);
            // Arrow keys
            _queue?.KeyUp(VirtualKey.ArrowLeft);
            _queue?.KeyUp(VirtualKey.ArrowRight);
            _queue?.KeyUp(VirtualKey.ArrowUp);
            _queue?.KeyUp(VirtualKey.ArrowDown);

            // 3. CooldownManager resetet
            _cooldownManager.ResetAll();

            // 4. FeedbackSystem stoppen
            _feedback?.StopAll();

            _logger?.Info("[EngineOrchestrator] Profil-Switch isoliert - alle Engine-Zustände zurückgesetzt");
        }

        /// <summary>
        /// Disposes all engine resources and unhooks event handlers.
        /// </summary>
        public void Dispose()
        {
            Shutdown();
            _tickProvider.Tick -= OnTick;
            _configWatcher?.Dispose();
        }

        // TECH-059: Config Hot-Reload Handler
        private void OnConfigChanged(string fileName)
        {
            try
            {
                if (fileName == "settings.json")
                {
                    var newSettings = Settings.Load();
                    _logger?.Info($"[EngineOrchestrator] Settings hot-reloaded: {fileName} (Sound={newSettings.SoundEnabled}, Rumble={newSettings.RumbleEnabled})");
                }
                else if (fileName == "profiles.json")
                {
                    if (_currentProfile != null)
                    {
                        var currentName = _currentProfile.Name;
                        if (_profileManager.ActiveProfile != null)
                            _profileApplier.LoadProfile(_profileManager.ActiveProfile, false);
                        _logger?.Info($"[EngineOrchestrator] Profile hot-reloaded: {currentName}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"[EngineOrchestrator] Config reload failed ({fileName}): {ex.Message}");
            }
        }
#pragma warning restore CS8618
    }
}