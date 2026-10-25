using System;
using System.Diagnostics;
using RagnaController.Models;
using RagnaController.Controller;
using RagnaController.Profiles;
using RagnaController.Views.Overlays;
using RagnaController.Core;
using RagnaController.Views.Windows;

namespace RagnaController.Core
{
    /// <summary>
    /// TASK-2102: EngineInstance — wraps a full EngineOrchestrator for a single RO client window (HWND).
    /// One instance per client, each with isolated InputCommandQueue, InputRouter, WindowTracker, and Overlay.
    /// Shared services (ControllerService, ProfileManager, Settings) are injected and shared across instances.
    /// </summary>
    public sealed class EngineInstance : IDisposable
    {
        private readonly EngineOrchestrator _orchestrator;
        private readonly IMessenger _messenger;
        private readonly Models.Settings _sharedSettings;
        private InGameOverlayWindow? _overlayWindow;
        private readonly object _lock = new();
        private bool _isDisposed;
        private bool _isActive;

        /// <summary>The HWND of the RO client window this instance controls.</summary>
        public IntPtr ClientHwnd { get; }

        /// <summary>The process name (e.g., "ragexe").</summary>
        public string ProcessName { get; }

        /// <summary>The process ID.</summary>
        public uint ProcessId { get; }

        /// <summary>The underlying EngineOrchestrator.</summary>
        public EngineOrchestrator Orchestrator => _orchestrator;

        /// <summary>The per-client overlay window (positioned on ClientHwnd).</summary>
        public InGameOverlayWindow? OverlayWindow => _overlayWindow;

        /// <summary>Whether this instance is actively running a profile.</summary>
        public bool IsActive
        {
            get => _isActive;
            private set
            {
                lock (_lock)
                {
                    _isActive = value;
                }
            }
        }

        /// <summary>Current engine status.</summary>
        public EngineStatus Status => _orchestrator.IsRunning ? EngineStatus.Running : EngineStatus.Stopped;

        /// <summary>Fired when the engine produces a new snapshot.</summary>
        public event Action<EngineInstance, ControllerSnapshot>? SnapshotUpdated;

        /// <summary>Fired when engine status changes.</summary>
        public event Action<EngineInstance, EngineStatus>? StatusChanged;

        /// <summary>Fired for log messages from this instance.</summary>
        public event Action<EngineInstance, string>? LogMessage;

        /// <summary>Fired when this instance is disposed.</summary>
        public event Action<EngineInstance>? Disposed;

        public EngineInstance(
            IntPtr hwnd,
            string processName,
            uint pid,
            ITickProvider tickProvider,
            IMessenger messenger,
            AdvancedLogger logger,
            ControllerService sharedControllerService,
            ProfileManager sharedProfileManager,
            Models.Settings sharedSettings,
            MultiClientInputRouter multiClientRouter)
        {
            ClientHwnd = hwnd;
            ProcessName = processName;
            ProcessId = pid;
            _messenger = messenger;
            _sharedSettings = sharedSettings;

            // Create per-instance InputCommandQueue bound to this client's HWND for PostMessage routing
            var instanceQueue = new InputCommandQueue(null, hwnd.ToString(), multiClientRouter);

            // Create EngineOrchestrator with per-instance dependencies
            _orchestrator = new EngineOrchestrator(tickProvider, messenger, instanceQueue, logger);

            // Configure WindowTracker for this specific client HWND
            _orchestrator.WinTracker.SetProcessName(processName);
            _orchestrator.WinTracker.StartHook();

            // Wire up MultiClientInputRouter for this client
            multiClientRouter.SetPreferredClientHwnd(processName, hwnd);
            multiClientRouter.SetActiveProcessName(processName);

            // Subscribe to orchestrator events for aggregation
            _orchestrator.SnapshotUpdated += snap => SnapshotUpdated?.Invoke(this, snap);
            _orchestrator.StatusChanged += status => StatusChanged?.Invoke(this, status);
            _orchestrator.LogMessage += msg => LogMessage?.Invoke(this, msg);

            // Overlay created lazily on first ShowOverlay() (avoids STA thread requirement in unit tests)
            _overlayWindow = null!;

            IsActive = false;
        }

        /// <summary>
        /// Activates this instance with a specific profile.
        /// </summary>
        public void Activate(Profile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            _orchestrator.ProfileApplier.LoadProfile(profile);
            IsActive = true;
        }

        /// <summary>
        /// Deactivates this instance (stops engine, clears profile).
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            _orchestrator.Stop();
        }

        /// <summary>Starts the engine tick loop.</summary>
        public void Start() => _orchestrator.Start();

        /// <summary>Stops the engine tick loop.</summary>
        public void Stop() => _orchestrator.Stop();

        /// <summary>Pauses the engine tick loop.</summary>
        public void Pause() => _orchestrator.Pause();

        /// <summary>Resumes the engine tick loop.</summary>
        public void Resume() => _orchestrator.Resume();

        /// <summary>Switches profile for this client with full state isolation.</summary>
        public void SwitchProfile(Profile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            _orchestrator.ProfileApplier.LoadProfile(profile);
            _orchestrator.IsolateProfileSwitch(); // Reset all engine state
        }

        /// <summary>Shows the overlay window for this client (creates it lazily on first call).</summary>
        public void ShowOverlay()
        {
            if (_overlayWindow == null)
            {
                _overlayWindow = new InGameOverlayWindow(
                    _messenger,
                    _orchestrator.WinTracker,
                    _orchestrator.ControllerManager,
                    _sharedSettings,
                    ClientHwnd,
                    _orchestrator.BuffManager)
                {
                    Owner = System.Windows.Application.Current?.MainWindow
                };
            }
            _overlayWindow.Show();
        }

        /// <summary>Hides the overlay window for this client.</summary>
        public void HideOverlay()
        {
            _overlayWindow?.Hide();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                _orchestrator.Stop(); // Don't dispose shared logger here — MultiClientEngineOrchestrator owns it
                _overlayWindow?.Close();
            }
            finally
            {
                Disposed?.Invoke(this);
            }
        }
    }
}