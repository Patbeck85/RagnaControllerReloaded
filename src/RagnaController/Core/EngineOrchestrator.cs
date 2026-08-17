using System;
using System.Diagnostics;
using RagnaController.Models;
using RagnaController.Controller;
using RagnaController.Profiles;

namespace RagnaController.Core
{
    /// <summary>
    /// ARCH-001: Extracted from HybridEngine - Main tick coordination & lifecycle management.
    /// Responsible for: Engine initialization, tick loop orchestration, Start/Stop/Pause/Resume/Shutdown.
    /// </summary>
    public class EngineOrchestrator : IDisposable
    {
        private readonly ITickProvider _tickProvider;
        private readonly IMessenger _messenger;
        private readonly AdvancedLogger _logger;
        private readonly ControllerService _ctrl;
        private readonly WindowTracker _winTracker;
        private readonly InputReader _inputReader;
        private readonly SystemMonitor _sysMonitor;
        private readonly SnapshotBuilder _snapshot;
        private readonly InputCommandQueue? _queue;

        // ── Engines ──
        private readonly MovementEngine _movement;
        private readonly CombatEngine _combat;
        private readonly AutoTargetEngine _autoTarget;
        private readonly MageEngine _mage;
        private readonly ComboEngine _combo;
        private readonly CursorEngine _cursor;
        private readonly SmartCursorService _smartCursor;
        private readonly KiteEngine _kite;
        private readonly SupportEngine _support;
        private readonly FeedbackSystem _feedback;
        private readonly OverlayRouter _overlayRouter;
        private readonly VoiceChatService _voice;
        private readonly MobSweepEngine _mobSweep;
        private readonly HandheldModeManager _handheld;
        private readonly EngineWatchdog _watchdog;
        private readonly CooldownManager _cooldownManager;
        private readonly DualSenseHardwareService _dualSense;

        // ── Runtime ──
        private volatile bool _isRunning;
        public bool IsRunning { get => _isRunning; private set => _isRunning = value; }
        private volatile bool _isPaused;
        public bool IsPaused { get => _isPaused; private set => _isPaused = value; }

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
            _winTracker = new WindowTracker();
            _inputReader = new InputReader(_ctrl);

            var engineQueue = _queue ?? new InputCommandQueue();
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
            _snapshot = new SnapshotBuilder(_autoTarget, _mage, _combo, _winTracker, _cursor, _smartCursor);

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

            _cooldownManager = new CooldownManager(_messenger, _feedback);

            _overlayRouter.ProfileQuickSwitch += delta => ProfileQuickSwitch?.Invoke(delta);
            _overlayRouter.RestoreMainWindowRequested += () => RestoreMainWindowRequested?.Invoke();

            // Initialize decomposed components FIRST (before using them in event handlers)
            _standbyManager = new StandbyManager();
            _inputRouter = new InputRouter(
                _combat, _movement, _autoTarget, _mage, _combo, _cursor, _smartCursor,
                _kite, _support, _overlayRouter, _mobSweep, _handheld, _feedback, _cooldownManager);
            _profileApplier = new ProfileApplier(this, _messenger);

            // NOW subscribe to events - _inputRouter is guaranteed non-null
            _combat.ActionFired += action =>
            {
                if (_rumbleEnabled) _feedback.TriggerSkillFired();
                _messenger.Publish(new ActionFiredMessage(action.Label, ActionFiredKind.Skill));
                _cooldownManager.RegisterAction(action);
                // Also notify InputRouter
                _inputRouter.OnActionFired(action, _rumbleEnabled);
            };

            _combat.TurboPulsed += () =>
            {
                if (_rumbleEnabled && _hapticMetronomeEnabled)
                {
                    _feedback.Trigger(FeedbackType.TurboPulse);
                }
                // Also notify InputRouter
                _inputRouter.OnTurboPulsed(_rumbleEnabled, _hapticMetronomeEnabled);
            };

            _tickProvider.Tick += OnTick;
        }

        // These will be set via ProfileApplier
        private bool _soundEnabled = true;
        private bool _rumbleEnabled = true;
        private bool _hapticMetronomeEnabled = true;
        private bool _isRenewal = true;
        private Profile? _currentProfile;

        // New decomposed components
        private readonly StandbyManager _standbyManager;
        private readonly InputRouter _inputRouter;
        private readonly ProfileApplier _profileApplier;

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
        public FeedbackSystem Feedback => _feedback;
        public OverlayRouter OverlayRouter => _overlayRouter;
        public MobSweepEngine MobSweep => _mobSweep;
        public HandheldModeManager Handheld => _handheld;
        public CooldownManager CooldownManager => _cooldownManager;
        public DualSenseHardwareService DualSense => _dualSense;
        public SystemMonitor SysMonitor => _sysMonitor;
        public SnapshotBuilder Snapshot => _snapshot;
        public ControllerService Controller => _ctrl;
        public WindowTracker WinTracker => _winTracker;

        // Expose runtime flags for ProfileApplier
        public bool SoundEnabled { get => _soundEnabled; set => _soundEnabled = value; }
        public bool RumbleEnabled { get => _rumbleEnabled; set => _rumbleEnabled = value; }
        public bool HapticMetronomeEnabled { get => _hapticMetronomeEnabled; set => _hapticMetronomeEnabled = value; }
        public bool IsRenewal { get => _isRenewal; set => _isRenewal = value; }
        public Profile? CurrentProfile { get => _currentProfile; set => _currentProfile = value; }
        public InputCommandQueue? CommandQueue => _queue;
        public ITickProvider TickProvider => _tickProvider;
        public IMessenger Messenger => _messenger;

        // Expose new components
        public StandbyManager StandbyManager => _standbyManager;
        public InputRouter InputRouter => _inputRouter;
        public ProfileApplier ProfileApplier => _profileApplier;

        // Public method for external log subscription - takes a message and invokes the event
        public void SubscribeToLog(string message) => LogMessage?.Invoke(message);

        private void OnTick(object? sender, EventArgs e)
        {
            try
            {
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
                        System.Threading.Thread.Sleep(50);
                        return; // Skip all combat routing and UI updates!
                    }
                }

                // Routing - delegate to InputRouter
                _inputRouter.RouteInput(input, _actualDeltaMs, _rumbleEnabled, _hapticMetronomeEnabled);

                // UI Update
                if (++_uiTick < UI_INTERVAL) return;
                _uiTick = 0;

                var snap = _snapshot.Build(input, _sysMonitor.IsFocusLocked, sw.Elapsed.TotalMilliseconds);
                SnapshotUpdated?.Invoke(snap);
                _messenger.Publish(new SnapshotReadyMessage(snap));
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

            string bat = _ctrl.GetBatteryLevel();
            BatteryChanged?.Invoke(bat);
            _messenger.Publish(new BatteryChangedMessage(bat));
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

        public void Start()
        {
            _tickProvider.Start();
            IsRunning = true;
            StatusChanged?.Invoke(EngineStatus.Running);
        }

        public void Stop()
        {
            _tickProvider.Stop();
            IsRunning = false;
            StatusChanged?.Invoke(EngineStatus.Stopped);
        }

        public void Pause()
        {
            IsPaused = true;
            _feedback.StopRumble();
            StatusChanged?.Invoke(EngineStatus.Stopped);
        }

        public void Resume()
        {
            IsPaused = false;
            StatusChanged?.Invoke(EngineStatus.Running);
        }

        public void Shutdown()
        {
            Stop();
            _feedback.StopRumble();
            _ctrl.Dispose();
            _handheld.Dispose();
            _voice.Dispose();
            _logger?.Info("=== Engine Shutdown ===");
            _logger?.Dispose();
        }

        public void Dispose()
        {
            Shutdown();
            _tickProvider.Tick -= OnTick;
        }
    }
}