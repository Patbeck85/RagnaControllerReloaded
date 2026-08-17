using System;
using System.Diagnostics;
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

        // ── Öffentliche Properties (Delegiert an Orchestrator) ────────────
        public bool IsRunning => _orchestrator.IsRunning;
        public bool IsPaused => _orchestrator.IsPaused;
        public string ControllerName => _orchestrator.ControllerName;
        public string ControllerType => _orchestrator.ControllerType;
        public IMessenger Messenger => _orchestrator.Messenger;
        public ControllerService ControllerSvc => _orchestrator.Controller;
        public WindowTracker WindowTracker => _orchestrator.WinTracker;

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
        public void LiveUpdateTurboInterval(float v) => _orchestrator.ProfileApplier.LiveUpdateTurboInterval(v);

        // ── Settings (Delegiert an ProfileApplier) ───────────────────────
        public void SetSoundEnabled(bool enabled) => _orchestrator.ProfileApplier.SetSoundEnabled(enabled);
        public void SetRumbleEnabled(bool enabled) => _orchestrator.ProfileApplier.SetRumbleEnabled(enabled);
        public void TurnOff() => _orchestrator.ProfileApplier.TurnOff();
        public void SetHapticMetronomeEnabled(bool enabled) => _orchestrator.ProfileApplier.SetHapticMetronomeEnabled(enabled);
        public void SetStandbySettings(bool enabled, int timeoutMins) => _orchestrator.ProfileApplier.SetStandbySettings(enabled, timeoutMins);
        public void AttachProfileManagerLogger(ProfileManager pm) => _orchestrator.ProfileApplier.AttachProfileManagerLogger(pm);
        public void ClearMacroCache() => _orchestrator.ProfileApplier.ClearMacroCache();

        // ── Lifecycle (Delegiert an Orchestrator) ────────────────────────
        public void Start() => _orchestrator.Start();
        public void Stop() => _orchestrator.Stop();
        public void Pause() => _orchestrator.Pause();
        public void Resume() => _orchestrator.Resume();
        public void Shutdown() => _orchestrator.Shutdown();
        public void Dispose() => _orchestrator.Dispose();
    }
}