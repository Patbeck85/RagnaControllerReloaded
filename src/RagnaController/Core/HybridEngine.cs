using System;
using System.Diagnostics;
using System.Threading;
using RagnaController.Models;
using RagnaController.Controller;
using RagnaController.Profiles;

namespace RagnaController.Core
{
    /// <summary>
    /// ARCH-001: Refactored HybridEngine - Facade over decomposed components.
    /// This class maintains backward compatibility while delegating to:
    /// - EngineOrchestrator: tick coordination & lifecycle
    /// - InputRouter: input routing & engine chain
    /// - ProfileApplier: profile loading & live updates
    /// - StandbyManager: AFK detection & power management
    /// </summary>
    public class HybridEngine
    {
        private readonly EngineOrchestrator _orchestrator;

        // ── Konstruktor ────────────────────────────────────────────────────
        public HybridEngine(ITickProvider tickProvider, IMessenger messenger, InputCommandQueue queue, AdvancedLogger logger)
        {
            _orchestrator = new EngineOrchestrator(tickProvider, messenger, queue, logger);
            
            // Forward all events
            _orchestrator.StatusChanged += s => StatusChanged?.Invoke(s);
            _orchestrator.SnapshotUpdated += s => SnapshotUpdated?.Invoke(s);
            _orchestrator.LogMessage += m => LogMessage?.Invoke(m);
            _orchestrator.BatteryChanged += b => BatteryChanged?.Invoke(b);
            _orchestrator.ControllerConnected += c => ControllerConnected?.Invoke(c);
            _orchestrator.ControllerDisconnected += () => ControllerDisconnected?.Invoke();
            _orchestrator.ProfileQuickSwitch += p => ProfileQuickSwitch?.Invoke(p);
            _orchestrator.RestoreMainWindowRequested += () => RestoreMainWindowRequested?.Invoke();
            _orchestrator.VoiceStatusChanged += v => VoiceStatusChanged?.Invoke(v);
        }

        // ── Public Properties (Delegiert an Orchestrator) ────────────
                        public bool IsRunning => _orchestrator.IsRunning;
                        public bool IsPaused => _orchestrator.IsPaused;
                        public string ControllerName => _orchestrator.ControllerName;
                        public string ControllerType => _orchestrator.ControllerType;
                        public IMessenger Messenger => _orchestrator.Messenger;
                        public ControllerService ControllerSvc => _orchestrator.Controller;
                        public ControllerManager ControllerManager => _orchestrator.ControllerManager;
                        public WindowTracker WindowTracker => _orchestrator.WinTracker;
                        public Profile? CurrentProfile => _orchestrator.CurrentProfile;
                        public InputCommandQueue? CommandQueue => _orchestrator.CommandQueue;

                public bool FocusLockEnabled
        {
            get => _orchestrator.SysMonitor.FocusLockEnabled;
            set => _orchestrator.SysMonitor.FocusLockEnabled = value;
        }
        public string FocusLockProcess
        {
            get => _orchestrator.SysMonitor.FocusLockProcess;
            set => _orchestrator.SysMonitor.FocusLockProcess = value;
        }

        // ── Events ────────────────────────────────────────────────────────
        public event Action<EngineStatus>? StatusChanged;
        public event Action<ControllerSnapshot>? SnapshotUpdated;
        public event Action<string>? LogMessage;
        public event Action<string>? BatteryChanged;
        public event Action<string>? ControllerConnected;
        public event Action? ControllerDisconnected;
        public event Action<int>? ProfileQuickSwitch;
        public event Action? RestoreMainWindowRequested;
        public event Action<string>? VoiceStatusChanged;

        // ── Profile Management (Delegiert an ProfileApplier) ─────────────
        public void LoadProfile(Profile p) => _orchestrator.ProfileApplier.LoadProfile(p);
        public void ApplyGameMode(bool isRenewal) => _orchestrator.ProfileApplier.ApplyGameMode(isRenewal);
        public void LiveUpdateActionRpg(bool enabled) => _orchestrator.ProfileApplier.LiveUpdateActionRpg(enabled);
        public void LiveUpdateDeadzone(float v) => _orchestrator.ProfileApplier.LiveUpdateDeadzone(v);
        public void LiveUpdateCurve(float v) => _orchestrator.ProfileApplier.LiveUpdateCurve(v);
        public void LiveUpdateActionSpeed(float v) => _orchestrator.ProfileApplier.LiveUpdateActionSpeed(v);
        public void LiveUpdateCursorSpeed(float v) => _orchestrator.ProfileApplier.LiveUpdateCursorSpeed(v);
        public void LiveUpdateLeftStick(float v) => _orchestrator.ProfileApplier.LiveUpdateLeftStick(v);
        public void LiveUpdateRightStick(float v) => _orchestrator.ProfileApplier.LiveUpdateRightStick(v);
        public void LiveUpdateLeftTrigger(float v) => _orchestrator.ProfileApplier.LiveUpdateLeftTrigger(v);
        public void LiveUpdateRightTrigger(float v) => _orchestrator.ProfileApplier.LiveUpdateRightTrigger(v);

        // ── Settings (Delegiert an ProfileApplier) ───────────────────────
        public void SetSoundEnabled(bool enabled) => _orchestrator.ProfileApplier.SetSoundEnabled(enabled);
        public void SetRumbleEnabled(bool enabled) => _orchestrator.ProfileApplier.SetRumbleEnabled(enabled);
        public void TurnOff() => _orchestrator.ProfileApplier.TurnOff();
        public void SetStandbySettings(bool enabled, int timeoutMins) => _orchestrator.ProfileApplier.SetStandbySettings(enabled, timeoutMins);
        public void AttachProfileManagerLogger(ProfileManager pm) => _orchestrator.ProfileApplier.AttachProfileManagerLogger(pm);

        // ── Lifecycle (Delegiert an Orchestrator) ────────────────────────
                public void Start() => _orchestrator.Start();
                public void Stop() => _orchestrator.Stop();
                public void Pause() => _orchestrator.Pause();
                public void Resume() => _orchestrator.Resume();
                public void Shutdown() => _orchestrator.Shutdown();
                public void Dispose() => _orchestrator.Dispose();

                // ── Calibration (Delegiert an InputReader via Orchestrator) ────────────
                public void StartCalibration()
                {
                    // Calibration is handled by InputReader - delegate to SettingsWindow approach
                    // We'll run a short calibration directly on the controller
                    var controller = _orchestrator.Controller;
                    if (controller == null) return;
            
                    // Create a temporary InputReader for calibration
                    var inputReader = new InputReader(controller);
                    float maxDrift = 0f;
            
                    // Sample for 3 seconds
                    for (int i = 0; i < 30; i++)
                    {
                        var input = inputReader.Read();
                        if (input.IsConnected)
                        {
                            float highest = Math.Max(Math.Max(Math.Abs(input.LeftX), Math.Abs(input.LeftY)),
                                                     Math.Max(Math.Abs(input.RightX), Math.Abs(input.RightY)));
                            if (highest > maxDrift) maxDrift = highest;
                        }
                        System.Threading.Thread.Sleep(100);
                    }
            
                    // Add safety buffer and clamp
                    float finalDeadzone = (float)Math.Round(maxDrift + 0.02f, 2);
                    if (finalDeadzone > 0.40f) finalDeadzone = 0.40f;
                    if (finalDeadzone < 0.05f) finalDeadzone = 0.05f;
            
                    // Apply to current profile if available
                    if (_orchestrator.CurrentProfile != null)
                    {
                        _orchestrator.CurrentProfile.Deadzone = finalDeadzone;
                        _orchestrator.CurrentProfile.CursorDeadzone = finalDeadzone;
                        _orchestrator.Movement.Deadzone = finalDeadzone;
                        _orchestrator.Cursor.Deadzone = finalDeadzone;
                
                        // Save the profile
                        var pm = new ProfileManager();
                        pm.SaveProfile(_orchestrator.CurrentProfile);
                    }
            
                    LogMessage?.Invoke($"Calibration complete: {finalDeadzone} deadzone");
                }
            }
        }