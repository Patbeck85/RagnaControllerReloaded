# 📚 API Documentation — RagnaController

Public API reference for `RagnaController.dll`. All types are in the `RagnaController.*` namespaces.

---

## Namespace: `RagnaController.Controller`

Hardware interaction and controller state.

| Type | Kind | Responsibility |
|------|------|----------------|
| `ControllerService` | class | XInput polling, WMI brand detection |
| `DualSenseHardwareService` | class | DualSense Lightbar & Adaptive Triggers via Raw HID |
| `DualSenseLightbarService` | class | PS5 LED colors via Raw HID USB reports |
| `GyroService` | class | Raw HID reading & low-pass filtering |
| `ControllerSnapshot` | class | Controller state snapshot for UI binding |

---

## Namespace: `RagnaController.Core`

Core business logic — engines, services, input, utilities.

### Engines (`RagnaController.Core`)

| Type | Responsibility |
|------|----------------|
| `AutoTargetEngine` | Melee smart-aim & auto-attack |
| `CombatEngine` | Combat state machine & targeting |
| `ComboEngine` | Class-aware sequential skill chains |
| `CursorEngine` | Right-stick free mouse movement |
| `GroundSpellEngine` | AoE ground spell management |
| `HybridEngine` | Legacy hybrid engine (deprecated) |
| `ItemManagerEngine` | Auto item usage & inventory |
| `KiteEngine` | Ranged hit-and-run FSM |
| `MageEngine` | Ground-spell aiming (Stick + Gyro) |
| `MobSweepEngine` | Auto TAB-cycle + attack while moving |
| `MovementEngine` | Left-stick click-to-move logic |
| `SkillOrchestrator` | Class-specific rotation execution |
| `SmartCursorEngine` | Smart cursor assistance |
| `SupportEngine` | Party targeting & healing cycle |

### Input (`RagnaController.Core`)

| Type | Responsibility |
|------|----------------|
| `InputReader` | Normalizes XInput gamepad data |
| `InputRouter` | Routes input to correct engine (Priority-Chain) |
| `InputCommandQueue` | Thread-safe SendInput queue |
| `JitterService` | Natural input timing variance (15-46ms) |
| `InputLatencyTracker` | Input latency monitoring |
| `BezierMouseService` | Smooth mouse movement curves |
| `KernelInputService` | Kernel-level input bypass |

### Services (`RagnaController.Core`)

| Type | Responsibility |
|------|----------------|
| `ClassDetector` | Weighted heuristic auto-class detection (FEAT-009) |
| `BuffManager` | Active buff/debuff tracking (FEAT-008) |
| `SessionRecorder` | JSONL session replay recorder |
| `SmartCursorService` | D-Pad grid-hopping & precision damping |
| `SystemMonitor` | Focus Lock evaluation (500ms polling) |
| `LocalizationManager` | i18n language switching |
| `ProfileService` | Profile CRUD operations |
| `TelemetryService` | Usage telemetry & analytics |

### Utilities (`RagnaController.Core`)

| Type | Responsibility |
|------|----------------|
| `EngineOrchestrator` | Main tick coordination, lifecycle management |
| `EngineOptimizationPool` | String pooling for hot-path allocations |
| `SnapshotBuilder` | Builds ControllerSnapshot from engine state |
| `AdvancedLogger` | High-performance logger |
| `NativeMethods` | Win32 P/Invoke declarations |
| `MainViewModel` | Main WPF ViewModel (MVVM) |

---

## Namespace: `RagnaController.Views.Windows`

| Type | Responsibility |
|------|----------------|
| `MainWindow` | Main UI, status display, tab navigation |
| `SettingsWindow` | General settings |
| `SplashWindow` | Startup splash screen |

### Namespace: `RagnaController.Views.Dialogs`

| Type | Responsibility |
|------|----------------|
| `ProfileWizardWindow` | Guided profile creation (FEAT-010) |
| `ProfileLibraryWindow` | Profile library management |
| `CommunityBrowserWindow` | GitHub Gists community hub |
| `DeveloperConsoleWindow` | Developer console |
| `DaisyWheelWindow` | Circular on-screen keyboard |
| `TutorialWindow` | Tutorial overlay |

### Namespace: `RagnaController.Views.Overlays`

| Type | Responsibility |
|------|----------------|
| `InGameOverlayWindow` | Borderless in-game state HUD |
| `HandheldWindow` | Handheld device UI |
| `MiniModeWindow` | Compact click-through HUD |

### Namespace: `RagnaController.Views.Editors`

| Type | Responsibility |
|------|----------------|
| `ButtonRemappingWindow` | Layer-based button mapping |
| `ComboEditorWindow` | Class combo chain editor |
| `RadialMenuWindow` | Radial emote menu editor |
| `RadialSetupWindow` | Radial emote setup |

---

## Namespace: `RagnaController.Models`

| Type | Kind | Responsibility |
|------|------|----------------|
| `ParsedInput` | record struct | Current/prev frame input |
| `ControllerState` | record struct | Controller state for UI |
| `VirtualKey` | enum | Win32 VK codes |
| `Settings` | class | Global app settings |

---

## Namespace: `RagnaController.Profiles`

| Type | Responsibility |
|------|----------------|
| `Profile` | JSON structure of character profile |
| `ProfileManager` | Load/Save/Import/Export/Backup |
| `AppJsonContext` | AOT-friendly System.Text.Json context |

---

## Architecture Rules

### 125Hz Tick Loop
```
InputReader → SystemMonitor → EngineOrchestrator.OnTick → InputRouter → CombatEngine → InputCommandQueue → Win32.SendInput
```

### Thread Safety
- Engine runs on background thread; UI runs on WPF Dispatcher
- **Never** touch WPF UI elements from engine code
- Use `Dispatcher.Invoke(() => { ... })` for UI updates

### Zero Allocation Hot Path
- No LINQ in `Update()` or `Tick()` methods
- No class allocations in tick loop
- Use `readonly record struct` or object pooling
