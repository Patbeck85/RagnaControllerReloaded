# TASK-2101 Research: Multi-Client EngineOrchestrator Architecture

**Date:** 2026-10-20  
**Status:** RESEARCH_DONE  
**Goal:** Define architecture for running multiple `EngineOrchestrator` instances (one per RO client) with full isolation.

---

## 1. CURRENT STATE ANALYSIS

### 1.1 What Already Exists ✅

| Component | Status | Description |
|-----------|--------|-------------|
| `MultiClientWindowTracker` | ✅ Done | EnumWindows, PID→Name cache, TTL, thread-safe, tracks multiple HWNDs per process |
| `MultiClientInputRouter` | ✅ Done | PostMessage to specific HWND, preferred HWND per process, active process name |
| MainWindow Client Switcher UI | ✅ Done | `ClientCombo`, `BtnRefreshClients`, `ClientCombo_SelectionChanged` → calls `_multiClientRouter.SetPreferredClientHwnd()` |
| Window Switching (FEAT-015) | ✅ Done | Profile presets per character, quick-switch via D-Pad |

### 1.2 What Is Missing ❌

| Missing | Impact |
|---------|--------|
| **Multiple EngineOrchestrator instances** | Only ONE `HybridEngine`/`EngineOrchestrator` exists globally |
| **Per-client Profile** | `EngineOrchestrator.CurrentProfile` is single — needs `Dictionary<HWND, Profile>` |
| **Per-client InputRouter** | `InputRouter` is single global — needs per-instance |
| **Per-client WindowTracker** | `WindowTracker` is single — needs per-HWND tracking |
| **Per-client Overlay** | `InGameOverlayWindow` is single — needs per-HWND overlay |
| **Tick-Loop Multi-Client** | Single tick loop — needs to iterate all active client engines |
| **Resource Sharing Strategy** | ControllerService, ProfileManager, Settings — what's shared vs. isolated? |

---

## 2. ARCHITECTURAL DECISIONS

### 2.1 Core Principle: Composition over Inheritance

Create a **`MultiClientEngineOrchestrator`** that *manages* multiple `EngineInstance` objects, each containing a full `EngineOrchestrator`. The `EngineOrchestrator` class remains largely unchanged — it just becomes **instantiable per client**.

### 2.2 Shared vs. Isolated Resources

| Resource | Strategy | Rationale |
|----------|----------|-----------|
| **ControllerService** | **SHARED** (1 per app) | Hardware is singleton; multiple clients share same physical controller. Battery, rumble, gyro are global. |
| **ProfileManager** | **SHARED** (1 per app) | Profile storage is global; `EngineInstance` holds reference to *its* `Profile` |
| **Settings** | **SHARED** (1 per app) | User preferences are global |
| **InputCommandQueue** | **ISOLATED** (1 per client) | Each client needs its own command queue to SendInput to correct HWND |
| **EngineOrchestrator** | **ISOLATED** (1 per client) | Full engine state per client |
| **InputRouter** | **ISOLATED** (1 per client) | Routes input to correct client's engines |
| **WindowTracker** | **ISOLATED** (1 per client) | Tracks specific HWND geometry |
| **InGameOverlayWindow** | **ISOLATED** (1 per client) | Overlay positioned on specific client window |
| **SnapshotBuilder** | **ISOLATED** (1 per client) | Builds snapshot for specific client |
| **BuffManager, PartyManager, ItemManager** | **ISOLATED** (1 per client) | Game state is per-client |

### 2.3 Thread Model

```
Main UI Thread (WPF)
    │
    ├─► MultiClientEngineOrchestrator (Manager)
    │       │
    │       ├─► EngineInstance #1 (Client A: HWND 0x1234)
    │       │       └─► Background Tick Thread (125Hz)
    │       │               └─► EngineOrchestrator.OnTick()
    │       │
    │       ├─► EngineInstance #2 (Client B: HWND 0x5678)
    │       │       └─► Background Tick Thread (125Hz)
    │       │               └─► EngineOrchestrator.OnTick()
    │       │
    │       └─► EngineInstance #N ...
    │
    └─► ControllerService (SDL Thread) — SHARED, single thread
```

**Key Insight:** Each `EngineInstance` runs its **own tick thread** (via its own `ITickProvider`). This ensures:
- No cross-client interference in hot path
- Independent pause/resume/stop per client
- Natural load balancing (OS schedules threads)

---

## 3. PROPOSED CLASS STRUCTURE

### 3.1 EngineInstance (New)

```csharp
/// <summary>
/// Holds all per-client engine state. One instance per RO client window (HWND).
/// </summary>
public sealed class EngineInstance : IDisposable
{
    public IntPtr ClientHwnd { get; }
    public string ProcessName { get; }
    public uint ProcessId { get; }
    
    // Core engine
    public EngineOrchestrator Orchestrator { get; }
    
    // Per-client overlays & UI
    public InGameOverlayWindow? OverlayWindow { get; private set; }
    
    // State
    public bool IsActive { get; private set; }
    public EngineStatus Status => Orchestrator.IsRunning ? EngineStatus.Running : EngineStatus.Stopped;
    
    public EngineInstance(
        IntPtr hwnd,
        string processName,
        uint pid,
        ITickProvider tickProvider,
        IMessenger messenger,
        InputCommandQueue queue,
        AdvancedLogger logger,
        ControllerService sharedControllerService,
        ProfileManager sharedProfileManager,
        Settings sharedSettings)
    {
        ClientHwnd = hwnd;
        ProcessName = processName;
        ProcessId = pid;
        
        // Create per-instance InputCommandQueue with client-specific HWND for SendInput routing
        var instanceQueue = queue ?? new InputCommandQueue(latencyTracker, hwnd.ToString());
        
        // Create EngineOrchestrator with per-instance dependencies
        Orchestrator = new EngineOrchestrator(
            tickProvider,
            messenger,
            instanceQueue,
            logger);
        
        // Configure for this specific client
        Orchestrator.WinTracker.SetProcessName(processName);
        Orchestrator.WinTracker.StartHook(); // Will track this specific HWND
        
        // Wire up events for UI aggregation
        Orchestrator.SnapshotUpdated += snap => SnapshotUpdated?.Invoke(this, snap);
        Orchestrator.StatusChanged += status => StatusChanged?.Invoke(this, status);
        Orchestrator.LogMessage += msg => LogMessage?.Invoke(this, msg);
    }
    
    public void Activate(Profile profile) 
    { 
        Orchestrator.LoadProfile(profile); 
        IsActive = true; 
    }
    
    public void Deactivate() 
    { 
        IsActive = false; 
    }
    
    public void Start() => Orchestrator.Start();
    public void Stop() => Orchestrator.Stop();
    public void Pause() => Orchestrator.Pause();
    public void Resume() => Orchestrator.Resume();
    
    public void Dispose() 
    { 
        Orchestrator.Dispose(); 
        OverlayWindow?.Close(); 
    }
    
    // Events for UI aggregation
    public event Action<EngineInstance, ControllerSnapshot>? SnapshotUpdated;
    public event Action<EngineInstance, EngineStatus>? StatusChanged;
    public event Action<EngineInstance, string>? LogMessage;
}
```

### 3.2 MultiClientEngineOrchestrator (New Manager)

```csharp
/// <summary>
/// Manages multiple EngineInstance objects — one per RO client.
/// Entry point for UI: Start/Stop clients, switch profiles, get aggregated status.
/// </summary>
public sealed class MultiClientEngineOrchestrator : IDisposable
{
    private readonly Dictionary<IntPtr, EngineInstance> _instances = new();
    private readonly object _lock = new();
    
    // Shared services (singletons)
    private readonly ControllerService _sharedControllerService;
    private readonly ProfileManager _sharedProfileManager;
    private readonly Settings _sharedSettings;
    private readonly IMessenger _messenger;
    private readonly AdvancedLogger _logger;
    
    // Multi-client input routing (already exists)
    private readonly MultiClientInputRouter _multiClientRouter;
    private readonly MultiClientWindowTracker _windowTracker;
    
    public IReadOnlyDictionary<IntPtr, EngineInstance> Instances => _instances;
    public int ActiveCount => _instances.Count(i => i.Value.IsActive);
    
    public MultiClientEngineOrchestrator(
        ControllerService controllerService,
        ProfileManager profileManager,
        Settings settings,
        IMessenger messenger,
        AdvancedLogger logger,
        MultiClientInputRouter multiClientRouter,
        MultiClientWindowTracker windowTracker)
    {
        _sharedControllerService = controllerService;
        _sharedProfileManager = profileManager;
        _sharedSettings = settings;
        _messenger = messenger;
        _logger = logger;
        _multiClientRouter = multiClientRouter;
        _windowTracker = windowTracker;
    }
    
    /// <summary>
    /// Starts engine for a specific client HWND.
    /// </summary>
    public EngineInstance StartClient(IntPtr hwnd, string processName, uint pid, Profile? profile = null)
    {
        lock (_lock)
        {
            if (_instances.TryGetValue(hwnd, out var existing))
            {
                if (profile != null) existing.Activate(profile);
                existing.Start();
                return existing;
            }
            
            // Create new tick provider for this instance
            var tickProvider = new BackgroundTickProvider(125); // 125Hz = 8ms
            
            var instance = new EngineInstance(
                hwnd, processName, pid,
                tickProvider, _messenger, null, _logger,
                _sharedControllerService, _sharedProfileManager, _sharedSettings);
            
            if (profile != null) instance.Activate(profile);
            instance.Start();
            
            _instances[hwnd] = instance;
            
            // Wire up MultiClientInputRouter for this client
            _multiClientRouter.SetPreferredClientHwnd(processName, hwnd);
            _multiClientRouter.SetActiveProcessName(processName);
            
            // Create per-client overlay
            instance.OverlayWindow = new InGameOverlayWindow(instance.Orchestrator);
            instance.OverlayWindow.Show();
            
            ClientStarted?.Invoke(this, instance);
            return instance;
        }
    }
    
    /// <summary>
    /// Stops and removes a client engine.
    /// </summary>
    public void StopClient(IntPtr hwnd)
    {
        lock (_lock)
        {
            if (_instances.TryGetValue(hwnd, out var instance))
            {
                instance.Stop();
                instance.Dispose();
                _instances.Remove(hwnd);
                ClientStopped?.Invoke(this, hwnd);
            }
        }
    }
    
    /// <summary>
    /// Switches profile for a specific client.
    /// </summary>
    public void SwitchProfile(IntPtr hwnd, Profile profile)
    {
        lock (_lock)
        {
            if (_instances.TryGetValue(hwnd, out var instance))
            {
                instance.Orchestrator.LoadProfile(profile);
                instance.Orchestrator.IsolateProfileSwitch(); // Reset engine state
            }
        }
    }
    
    /// <summary>
    /// Gets aggregated snapshot for UI (all active clients).
    /// </summary>
    public List<(EngineInstance Instance, ControllerSnapshot Snapshot)> GetAllSnapshots()
    {
        lock (_lock)
        {
            var result = new List<(EngineInstance, ControllerSnapshot)>();
            foreach (var instance in _instances.Values.Where(i => i.IsActive))
            {
                // Note: Snapshot is pushed via event; this is for polling fallback
                result.Add((instance, instance.Orchestrator.Snapshot.Build(...)));
            }
            return result;
        }
    }
    
    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var instance in _instances.Values)
                instance.Dispose();
            _instances.Clear();
        }
    }
    
    // Events
    public event Action<MultiClientEngineOrchestrator, EngineInstance>? ClientStarted;
    public event Action<MultiClientEngineOrchestrator, IntPtr>? ClientStopped;
}
```

---

## 4. INTEGRATION POINTS

### 4.1 App.xaml.cs / Startup

```csharp
// Current: Single HybridEngine
// New: MultiClientEngineOrchestrator

var controllerService = new ControllerService();
var profileManager = new ProfileManager();
var settings = Settings.Load();
var messenger = new Messenger();
var logger = new AdvancedLogger();
var windowTracker = new MultiClientWindowTracker(new NativeMethodsImpl(), new ProcessNameResolver());
var multiClientRouter = new MultiClientInputRouter(new NativeMethodsImpl(), new ProcessNameResolver(), windowTracker);

var multiEngine = new MultiClientEngineOrchestrator(
    controllerService, profileManager, settings, messenger, logger,
    multiClientRouter, windowTracker);

// MainWindow receives MultiClientEngineOrchestrator instead of HybridEngine
```

### 4.2 MainWindow Changes

```csharp
// Current: private readonly HybridEngine _engine;
// New: private readonly MultiClientEngineOrchestrator _multiEngine;

// Client switching:
private void ClientCombo_SelectionChanged(...)
{
    var item = ClientCombo.SelectedItem as ClientComboItem;
    _multiEngine.SwitchClient(item.Hwnd); // New method
    _multiClientRouter.SetPreferredClientHwnd(item.ProcessName, item.Hwnd);
}

// Profile switching per client:
private void ProfileCombo_SelectionChanged(...)
{
    var hwnd = _multiClientRouter.ResolveActiveClientHwnd(); // or from selected client
    var profile = ProfileCombo.SelectedItem as Profile;
    _multiEngine.SwitchProfile(hwnd, profile);
}

// UI: Show tabs for each active client with individual status
```

### 4.3 Input Routing Integration

The `InputCommandQueue` needs to know target HWND for PostMessage:

```csharp
// InputCommandQueue constructor takes clientId/HWND
// SendInput methods use _multiClientRouter.SendKeyToClient(hwnd, key, down)
// instead of global SendInput
```

---

## 5. MIGRATION STRATEGY (Phased)

### Phase 1: EngineInstance + MultiClientEngineOrchestrator (Core)
- [ ] Create `EngineInstance` class wrapping `EngineOrchestrator`
- [ ] Create `MultiClientEngineOrchestrator` manager
- [ ] Make `EngineOrchestrator` constructor take `IntPtr clientHwnd` for queue routing
- [ ] Unit tests: 2 instances, independent start/stop, profile switch

### Phase 2: Per-Client Overlays & UI
- [ ] `InGameOverlayWindow` per `EngineInstance` (positioned on client HWND)
- [ ] MainWindow: Client tabs with individual status panels
- [ ] Profile switching per client (not global)

### Phase 3: Input Routing Integration
- [ ] `InputCommandQueue` uses `MultiClientInputRouter` for PostMessage
- [ ] `InputRouter.RouteInput()` receives client HWND context
- [ ] Test: 2 RO clients, different profiles, simultaneous control

### Phase 4: Polish & Edge Cases
- [ ] Client death detection → auto-stop engine
- [ ] Resource cleanup on client disconnect
- [ ] Settings hot-reload per client
- [ ] Performance: CPU/memory with 2-4 clients

---

## 6. RISKS & MITIGATIONS

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| **Thread explosion** (1 thread/client) | Medium | High | Max 4 clients; use thread pool for tick providers |
| **ControllerService contention** | Low | Medium | ControllerService is thread-safe (cached reads); single SDL thread |
| **ProfileManager concurrent access** | Medium | Medium | ProfileManager already thread-safe (locks); read-mostly |
| **Overlay window Z-order** | Medium | Low | `Owner = MainWindow`, `WindowStyle = None`, `Topmost = true` per client |
| **Memory leak on client stop** | Low | High | `EngineInstance.Dispose()` calls `Orchestrator.Shutdown()` + overlay close |
| **Input routing to wrong client** | Medium | High | `InputCommandQueue` bound to HWND at creation; integration tests |

---

## 7. TEST PLAN

| Test | Description |
|------|-------------|
| `MultiClientEngineOrchestrator_StartStopClient` | Start 2 clients, verify independent IsRunning |
| `MultiClientEngineOrchestrator_ProfileIsolation` | Client A: Mage profile, Client B: Archer profile — verify no cross-contamination |
| `MultiClientEngineOrchestrator_InputRouting` | Send key to Client A HWND → verify PostMessage called with correct HWND |
| `MultiClientEngineOrchestrator_ClientDeath` | Close RO client → engine auto-stops, resources freed |
| `MultiClientEngineOrchestrator_OverlayPerClient` | 2 overlays visible, each on correct client window |
| `MultiClientEngineOrchestrator_LoadTest` | 4 clients running 60s — CPU < 15%, no memory leak |

---

## 8. ESTIMATED EFFORT

| Phase | Tasks | Estimate |
|-------|-------|----------|
| Phase 1: Core | 4 tasks | 1.5 weeks |
| Phase 2: Overlays & UI | 3 tasks | 1 week |
| Phase 3: Input Routing | 2 tasks | 1 week |
| Phase 4: Polish | 2 tasks | 0.5 weeks |
| **Total** | **11 tasks** | **~4 weeks** |

---

## 9. DECISION LOG

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-10-20 | Composition (EngineInstance) over inheritance | EngineOrchestrator stays testable, minimal changes |
| 2026-10-20 | Per-instance tick threads | Simpler than shared tick loop with client iteration |
| 2026-10-20 | ControllerService shared | Hardware is physically single; SDL thread is global |
| 2026-10-20 | InputCommandQueue per client | Required for PostMessage to correct HWND |

---

**Next Step:** Implement Phase 1 — `EngineInstance` and `MultiClientEngineOrchestrator` classes with unit tests.