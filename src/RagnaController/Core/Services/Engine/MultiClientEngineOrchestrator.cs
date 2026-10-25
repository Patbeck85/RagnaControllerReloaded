using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using RagnaController.Models;
using RagnaController.Controller;
using RagnaController.Profiles;
using RagnaController.Core;

namespace RagnaController.Core
{
    /// <summary>
    /// TASK-2102: MultiClientEngineOrchestrator — manages multiple EngineInstance objects (one per RO client).
    /// Provides unified API for UI: start/stop clients, switch profiles, get aggregated status/snapshots.
    /// </summary>
    public sealed class MultiClientEngineOrchestrator : IDisposable
    {
        private readonly Dictionary<IntPtr, EngineInstance> _instances = new();
        private readonly object _lock = new();
        private bool _isDisposed;

        // Shared services (singletons)
        private readonly ControllerService _sharedControllerService;
        private readonly ProfileManager _sharedProfileManager;
        private readonly Models.Settings _sharedSettings;
        private readonly IMessenger _messenger;
        private readonly AdvancedLogger _logger;
        private readonly MultiClientInputRouter _multiClientRouter;
        private readonly MultiClientWindowTracker _windowTracker;

        /// <summary>All managed engine instances (thread-safe read via lock).</summary>
        public IReadOnlyDictionary<IntPtr, EngineInstance> Instances
        {
            get
            {
                lock (_lock)
                {
                    return new Dictionary<IntPtr, EngineInstance>(_instances);
                }
            }
        }

        /// <summary>Number of active (running) client engines.</summary>
        public int ActiveCount
        {
            get
            {
                lock (_lock)
                {
                    return _instances.Count(i => i.Value.IsActive);
                }
            }
        }

        // Public accessors for shared services
        public ControllerService SharedControllerService => _sharedControllerService;
        public ProfileManager SharedProfileManager => _sharedProfileManager;
        public Models.Settings SharedSettings => _sharedSettings;
        public IMessenger Messenger => _messenger;
        public AdvancedLogger Logger => _logger;
        public MultiClientInputRouter MultiClientRouter => _multiClientRouter;
        public MultiClientWindowTracker WindowTracker => _windowTracker;

        /// <summary>Total number of managed instances.</summary>
        public int TotalCount
        {
            get
            {
                lock (_lock)
                {
                    return _instances.Count;
                }
            }
        }

        /// <summary>Fired when a new client engine is started.</summary>
        public event Action<MultiClientEngineOrchestrator, EngineInstance>? ClientStarted;

        /// <summary>Fired when a client engine is stopped and removed.</summary>
        public event Action<MultiClientEngineOrchestrator, IntPtr>? ClientStopped;

        /// <summary>Fired when a client's profile is switched.</summary>
        public event Action<MultiClientEngineOrchestrator, EngineInstance, Profile>? ProfileSwitched;

        /// <summary>Aggregated log messages from all instances.</summary>
        public event Action<EngineInstance, string>? LogMessage;

        public MultiClientEngineOrchestrator(
            ControllerService controllerService,
            ProfileManager profileManager,
            Models.Settings settings,
            IMessenger messenger,
            AdvancedLogger logger,
            MultiClientInputRouter multiClientRouter,
            MultiClientWindowTracker windowTracker)
        {
            _sharedControllerService = controllerService ?? throw new ArgumentNullException(nameof(controllerService));
            _sharedProfileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
            _sharedSettings = settings ?? throw new ArgumentNullException(nameof(settings));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _multiClientRouter = multiClientRouter ?? throw new ArgumentNullException(nameof(multiClientRouter));
            _windowTracker = windowTracker ?? throw new ArgumentNullException(nameof(windowTracker));
        }

        /// <summary>
        /// Starts (or restarts) an engine for a specific client HWND.
        /// If already exists, activates with new profile (if provided) and starts.
        /// </summary>
        /// <param name="hwnd">Target RO client window handle.</param>
        /// <param name="processName">Process name (e.g., "ragexe").</param>
        /// <param name="pid">Process ID.</param>
        /// <param name="profile">Optional profile to load. If null, uses current or default.</param>
        /// <returns>The EngineInstance for this client.</returns>
        public EngineInstance StartClient(IntPtr hwnd, string processName, uint pid, Profile? profile = null)
        {
            if (hwnd == IntPtr.Zero) throw new ArgumentException("HWND cannot be zero.", nameof(hwnd));
            if (string.IsNullOrWhiteSpace(processName)) throw new ArgumentException("Process name required.", nameof(processName));

            lock (_lock)
            {
                // Check if already exists
                if (_instances.TryGetValue(hwnd, out var existing))
                {
                    if (profile != null)
                    {
                        existing.Activate(profile);
                        ProfileSwitched?.Invoke(this, existing, profile);
                    }
                    if (!existing.Orchestrator.IsRunning)
                    {
                        existing.Start();
                    }
                    ClientStarted?.Invoke(this, existing);
                    return existing;
                }

                // Create new tick provider for this instance (125Hz = 8ms)
                var tickProvider = new BackgroundTickProvider(125);

                var instance = new EngineInstance(
                    hwnd, processName, pid,
                    tickProvider, _messenger, _logger,
                    _sharedControllerService, _sharedProfileManager, _sharedSettings,
                    _multiClientRouter);

                // Subscribe to instance events for aggregation
                instance.LogMessage += (inst, msg) => LogMessage?.Invoke(inst, msg);
                instance.Disposed += inst => RemoveInstance(inst.ClientHwnd);

                if (profile != null)
                {
                    instance.Activate(profile);
                }

                instance.Start();
                _instances[hwnd] = instance;

                // Ensure MultiClientInputRouter is configured for this client
                _multiClientRouter.SetPreferredClientHwnd(processName, hwnd);
                _multiClientRouter.SetActiveProcessName(processName);

                _logger?.Info($"[MultiClientEngineOrchestrator] Started client: {processName} (PID {pid}) HWND={hwnd}");

                ClientStarted?.Invoke(this, instance);
                return instance;
            }
        }

        /// <summary>
        /// Stops and removes a client engine.
        /// </summary>
        public void StopClient(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;

            EngineInstance? instance;
            lock (_lock)
            {
                if (!_instances.TryGetValue(hwnd, out instance)) return;
            }

            instance.Stop();
            instance.Dispose(); // Will fire Disposed event which removes from dictionary
            _logger?.Info($"[MultiClientEngineOrchestrator] Stopped client: HWND={hwnd}");
            ClientStopped?.Invoke(this, hwnd);
        }

        /// <summary>
        /// Internal: removes instance from dictionary (called from Disposed event).
        /// </summary>
        private void RemoveInstance(IntPtr hwnd)
        {
            lock (_lock)
            {
                _instances.Remove(hwnd);
            }
        }

        /// <summary>
        /// Switches profile for a specific client with full state isolation.
        /// </summary>
        public void SwitchProfile(IntPtr hwnd, Profile profile)
        {
            if (hwnd == IntPtr.Zero) throw new ArgumentException("HWND cannot be zero.", nameof(hwnd));
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            EngineInstance? instance;
            lock (_lock)
            {
                if (!_instances.TryGetValue(hwnd, out instance))
                    throw new KeyNotFoundException($"No engine instance for HWND {hwnd}");
            }

            instance.SwitchProfile(profile);
            _logger?.Info($"[MultiClientEngineOrchestrator] Switched profile for HWND={hwnd} to '{profile.Name}'");
            ProfileSwitched?.Invoke(this, instance, profile);
        }

        /// <summary>
        /// Gets the EngineInstance for a specific client HWND.
        /// </summary>
        public EngineInstance? GetInstance(IntPtr hwnd)
        {
            lock (_lock)
            {
                _instances.TryGetValue(hwnd, out var instance);
                return instance;
            }
        }

        /// <summary>
        /// Gets the EngineInstance for the currently active client (from MultiClientInputRouter).
        /// </summary>
        public EngineInstance? GetActiveInstance()
        {
            var hwnd = _multiClientRouter.ResolveActiveClientHwnd();
            if (hwnd == IntPtr.Zero) return null;
            return GetInstance(hwnd);
        }

        /// <summary>
        /// Gets all active instances with their latest snapshots.
        /// </summary>
        public List<(EngineInstance Instance, ControllerSnapshot Snapshot)> GetAllSnapshots()
        {
            lock (_lock)
            {
                var result = new List<(EngineInstance, ControllerSnapshot)>();
                foreach (var instance in _instances.Values.Where(i => i.IsActive))
                {
                    // Note: Snapshot is pushed via event; this returns null for polling fallback
                    // The actual snapshot is available via SnapshotUpdated event
                    result.Add((instance, null!));
                }
                return result;
            }
        }

        /// <summary>
        /// Checks for dead client windows and stops their engines.
        /// </summary>
        public void CleanupDeadClients()
        {
            List<IntPtr> deadHwnds;
            lock (_lock)
            {
                deadHwnds = _instances
                    .Where(kvp => !_windowTracker.IsAlive(kvp.Key))
                    .Select(kvp => kvp.Key)
                    .ToList();
            }

            foreach (var hwnd in deadHwnds)
            {
                _logger?.Warn($"[MultiClientEngineOrchestrator] Client HWND={hwnd} no longer alive — stopping engine.");
                StopClient(hwnd);
            }
        }

        /// <summary>
        /// Stops all client engines.
        /// </summary>
        public void StopAll()
        {
            List<IntPtr> hwnds;
            lock (_lock)
            {
                hwnds = _instances.Keys.ToList();
            }

            foreach (var hwnd in hwnds)
            {
                StopClient(hwnd);
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            StopAll();

            lock (_lock)
            {
                _instances.Clear();
            }
        }
    }
}