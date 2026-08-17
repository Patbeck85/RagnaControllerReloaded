# RagnaController — Architecture v1.4.0

## System Overview

```
EXE startup
    │
    └── App.xaml.cs: OnStartup()
           │  SetHighDpiMode(PerMonitorV2)
           └── StartWorkflow()
                  │
                  ├── SplashWindow (3 s animated intro)
                  │
                  └── MainWindow (Obsidian & Gold UI)
                         │
                         ├── HybridEngine (Facade)  ←──── 125 Hz DispatcherTimer (1 ms precision)
                         │     │
                         │     ├── EngineOrchestrator   ← Main tick coordination & lifecycle
                         │     │     ├── ControllerService    (XInput poll + WMI brand detection)
                         │     │     ├── MovementEngine       (left stick → SendInput click)
                         │     │     ├── CursorEngine         (right stick → smooth mouse move)
                         │     │     ├── CombatEngine         (5-layer · turbo · macro playback)
                         │     │     ├── ComboEngine          (class-aware skill chains)
                         │     │     ├── AutoTargetEngine     (Melee FSM + Smart Skill juggling)
                         │     │     ├── KiteEngine           (Ranged 5-phase FSM)
                         │     │     ├── MageEngine           (ground-target / bolt FSM)
                         │     │     ├── SupportEngine        (heal / party cycle FSM)
                         │     │     ├── VoiceChatService     (System.Speech → chat string)
                         │     │     ├── WindowSwitcher       (Win32 AttachThreadInput, bg thread)
                         │     │     ├── WindowTracker        (GetClientRect + GetDpiForMonitor)
                         │     │     ├── FeedbackSystem       (rumble patterns + SystemSounds)
                         │     │     ├── AdvancedLogger       (tick metrics + ring buffer)
                         │     │     ├── MobSweepEngine       (AoE sweep FSM)
                         │     │     ├── HandheldModeManager  (Steam Deck / handheld support)
                         │     │     ├── CooldownManager      (skill cooldown tracking)
                         │     │     ├── DualSenseHardwareService (adaptive triggers, lightbar)
                         │     │     ├── EngineWatchdog       (performance monitoring)
                         │     │     └── EngineOptimizationPool (object pooling)
                         │     │
                         │     ├── InputRouter          ← Modifier parsing & engine routing
                         │     ├── ProfileApplier       ← Profile loading & live updates
                         │     └── StandbyManager       ← Smart Standby AFK detection
                         │
                         ├── Overlay Windows
                         │     ├── StreamerOverlayWindow
                         │     ├── RadialMenuWindow
                         │     ├── DaisyWheelWindow
                         │     └── MiniModeWindow
                         ├── ProfileManager      (39 built-ins + user JSON + bak recovery)
                         └── Settings            (AppData JSON persistence)
```

---

## Tick Loop (`EngineOrchestrator.OnTick` — 8 ms / 125 Hz)

```
1.  Poll gamepad              ← ControllerService.GetGamepad() via SharpDX XInput
2.  Battery check             → every ~10 s → BatteryChanged event
3.  Focus Lock + WindowTracker → every ~500 ms (volatile _focusCheckCounter):
        a. WindowTracker.Refresh()        → foreground-first, cached HWND fast-path
        b. MovementEngine.SetCenter()     → DPI-corrected client centre
        c. IsFocusLocked check            → suppress all input if RO not foreground
4.  If FocusLocked → return early
5.  Read modifiers            ← L1, R1, L2 (>50), R2 (>50)
6.  Engine toggles            ← L3+L1=Mage, L3+R1=Kite, L3+L2=Support
7.  Alt hold/release          ← X button (no modifier)
8.  Daisy Wheel routing       ← Back+R1 toggle; UpdateInput() while open; return early
9.  Voice Chat toggle         ← Back+L1
10. Radial menu               ← LT+RT hold → Reopen(); RT+LT release → ExecuteAndClose()
11. Panic heal                ← L3+R3 + cooldown
12. Loot vacuum               ← LB+RB → PerformLootVacuum(8ms), throttled 50 ms/click
13. Combo engine tick         ← _combo.Update(comboHeld, 8)
14. Movement / engine routing ← active FSM or CursorEngine
15. CombatEngine.ProcessButton() for each changed button flag
16. _prevButtons = pad.Buttons
17. Every UISkip ticks → SnapshotUpdated event
        → MainWindow UI refresh
        → StreamerOverlayWindow.UpdateSnapshot()
        → MiniModeWindow.UpdateState()
18. AdvancedLogger.LogPerformance()

Smart Standby (StandbyManager):
    → Checks idle time, throttles polling to ~20Hz during AFK
    → Auto-pauses engines, reduces CPU/rumble when inactive

Input Routing (InputRouter):
    → Parses modifiers (L1/R1/L2/R2) into layer states
    → Routes to: KiteEngine → AutoTargetEngine → MageEngine → SupportEngine
    → Handles SmartCursor overlay, DaisyWheel, RadialMenu
```

---

## Streamer Overlay Data Flow (v1.3.0)

```
HybridEngine.OnTick()
    └── SnapshotUpdated?.Invoke(new ControllerSnapshot {
            LeftX, LeftY, RightX, RightY,
            L1, R1, L2, R2,
            BtnA, BtnB, BtnX, BtnY,       ← NEW fields
            BtnL3, BtnR3, BtnStart, BtnBack,
            LT (0–1), RT (0–1),
            LayerText, StateLabel,
            WindowTracked, WindowDpiScale,
            ...
        })
            │
            ├── MainWindow.SnapshotUpdated handler (UI update)
            ├── StreamerOverlayWindow.UpdateSnapshot()
            │       ├── SetBtn(BtnA, snap.BtnA, ColA)  × 12 buttons
            │       ├── LTBar.Width = snap.LT * 22
            │       ├── Canvas.SetLeft(LStickDot, 14.5 + snap.LeftX * 14.5)
            │       └── StateText, LayerText, ProfileText
            └── MiniModeWindow.UpdateState()
```

---

## WindowTracker Refresh Priority

```
Refresh() called every ~500 ms
    │
    ├─ GetForegroundWindow() == _hwnd && IsWindow(_hwnd)?
    │       YES → UpdateGeometry() only  (fast path, zero allocation)
    │
    ├─ GetForegroundWindow() is an RO process?
    │       YES → _hwnd = fgHwnd; UpdateGeometry()
    │
    ├─ _hwnd still valid? (IsWindow check)
    │       YES → UpdateGeometry()  (RO backgrounded, reuse handle)
    │
    └─ Full Process.GetProcesses() scan  (only when handle lost)
            └── UpdateGeometry(newHwnd)
```

After `WindowSwitcher.Toggle()`: `_focusCheckCounter` set to 63 from background thread (`volatile int`) → immediate refresh on next tick.

---

## Input Flow

```
Physical controller press
    ↓
Windows XInput driver
    ↓
SharpDX.XInput (GetState)
    ↓
HybridEngine.OnTick()
    ↓
[Focus Lock check — if locked, stop here]
    ↓
CombatEngine → resolves L1+A to VirtualKey.F1
    ↓
InputSimulator.TapKey / SendInput
    INPUT struct: LayoutKind.Explicit, FieldOffset(8) — 64-bit aligned
    MoveMouseRelative: MOUSEEVENTF_MOVE | MOUSEEVENTF_MOVE_NOCOALESCE
    ↓
Windows input queue → ragexe.exe
```

---

## Key Files

| File | Responsibility |
|---|---|
| `App.xaml.cs` | Startup, `SetHighDpiMode`, splash, global styles |
| `MainWindow.xaml.cs` | Main UI, engine wiring, streamer overlay management |
| `StreamerOverlayWindow.xaml.cs` | Real-time controller visualiser for streaming |
| `Core/HybridEngine.cs` | Facade over decomposed components, backward compatibility |
| `Core/EngineOrchestrator.cs` | **NEW v1.4.0** Main tick coordination, lifecycle, engine initialization |
| `Core/InputRouter.cs` | **NEW v1.4.0** Modifier parsing, engine chain routing, overlay handling |
| `Core/ProfileApplier.cs` | **NEW v1.4.0** Profile loading, live parameter updates, renewal timing |
| `Core/StandbyManager.cs` | **NEW v1.4.0** Smart Standby AFK detection, power management |
| `Core/WindowTracker.cs` | DPI-aware window geometry, foreground-first for multi-client |
| `Core/CombatEngine.cs` | 5-layer mapping, turbo, macro playback, window switch |
| `Core/AutoTargetEngine.cs` | Melee FSM + Smart Skill cursor juggling |
| `Core/InputSimulator.cs` | `SendInput` P/Invoke (64-bit aligned, chat serialisation) |
| `Core/FeedbackSystem.cs` | Rumble patterns with pause-aware cancellation |
| `Core/VoiceChatService.cs` | Local speech-to-text, 8-second timeout |
| `Core/WindowSwitcher.cs` | Win32 focus management (background thread) |
| `Core/MobSweepEngine.cs` | AoE sweep FSM for farming |
| `Core/HandheldModeManager.cs` | Steam Deck / handheld mode support |
| `Core/CooldownManager.cs` | Skill cooldown tracking & event publishing |
| `Core/DualSenseHardwareService.cs` | Adaptive triggers, lightbar, haptic metronome |
| `Core/EngineWatchdog.cs` | Performance monitoring (overload detection) |
| `Core/EngineOptimizationPool.cs` | Object pooling for zero-allocation hot paths |
| `Profiles/ProfileManager.cs` | 39 built-ins, duplicate protection, `.bak.json` recovery |
| `Models/Settings.cs` | App settings JSON persistence |
