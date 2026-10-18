# 📁 Folder Structure — RagnaController

This document provides a detailed breakdown of every folder in `src/RagnaController/`, its responsibility, and which files belong to it.

---

## Layer Overview

| Layer | Folder | Responsibility |
|-------|--------|----------------|
| **UI** | `Views/` | WPF Windows, Controls, Dialogs, Overlays, Editors |
| **Hardware** | `Controller/` | Controller polling, DualSense HID, Input abstraction |
| **Business Logic** | `Core/` | Engines, Services, Input, Windows, Interfaces, Utilities |
| **Data** | `Models/` | DTOs, record structs, settings, VirtualKey enum |
| **Profiles** | `Profiles/` | Profile JSON schema, load/save, AppJsonContext |
| **i18n** | `Locales/` | JSON language files |
| **Assets** | `Assets/` | Sprites, icons, class portraits, emotes |
| **Resources** | `Resources/` | Embedded resources, themes |

---

## Views/ — UI Layer

All WPF XAML windows and controls. Organized by function:

### Views/Windows/ — Main Application Windows

| File | Responsibility |
|------|----------------|
| `MainWindow.xaml` | Main UI, status display, tab navigation |
| `SettingsWindow.xaml` | General settings (sound, rumble, window mode, language) |
| `SplashWindow.xaml` | Animated startup splash screen |

### Views/Dialogs/ — Modal Dialogs & Wizards

| File | Responsibility |
|------|----------------|
| `ProfileWizardWindow.xaml` | Guided profile creation wizard (FEAT-010) |
| `ProfileLibraryWindow.xaml` | Profile library management |
| `CommunityBrowserWindow.xaml` | GitHub Gists community hub |
| `DeveloperConsoleWindow.xaml` | In-app developer console |
| `DaisyWheelWindow.xaml` | Circular on-screen keyboard |
| `TutorialWindow.xaml` | Interactive tutorial overlay |

### Views/Overlays/ — In-Game & Overlay Windows

| File | Responsibility |
|------|----------------|
| `InGameOverlayWindow.xaml` | Borderless in-game state HUD |
| `HandheldWindow.xaml` | Big-Picture UI for ROG Ally / Steam Deck |
| `MiniModeWindow.xaml` | Compact click-through HUD |

### Views/Editors/ — Configuration Editors

| File | Responsibility |
|------|----------------|
| `ButtonRemappingWindow.xaml` | Layer-based button mapping |
| `ComboEditorWindow.xaml` | Class combo chain editor |
| `RadialMenuWindow.xaml` | Radial emote menu editor |
| `RadialSetupWindow.xaml` | Radial emote menu configuration |

### Views/Controls/ — Reusable UI Controls

| File | Responsibility |
|------|----------------|
| `DarkComboBox.xaml` | Themed ComboBox control (Obsidian & Gold design) |

---

## Controller/ — Hardware Interaction Layer

| File | Responsibility |
|------|----------------|
| `ControllerService.cs` | XInput polling, WMI brand detection |
| `DualSenseHardwareService.cs` | DualSense Lightbar & Adaptive Triggers via Raw HID |
| `DualSenseLightbarService.cs` | PS5 LED colors via Raw HID USB reports |
| `GyroService.cs` | Raw HID reading & low-pass filtering |
| `ControllerSnapshot.cs` | Controller state snapshot for UI binding |

---

## Core/ — Business Logic Layer

All engine and service code. Split into 7 subfolders:

### Core/Engines/ — Gameplay Engines (125Hz Tick Loop)

| File | Responsibility |
|------|----------------|
| `AutoTargetEngine.cs` | Melee smart-aim & auto-attack |
| `CombatEngine.cs` | Combat state machine & targeting |
| `ComboEngine.cs` | Class-aware sequential skill chains |
| `CursorEngine.cs` | Right-stick free mouse movement |
| `GroundSpellEngine.cs` | AoE ground spell management with tick events |
| `HybridEngine.cs` | Legacy hybrid engine (deprecated) |
| `ItemManagerEngine.cs` | Auto item usage & inventory management |
| `KiteEngine.cs` | Ranged hit-and-run FSM |
| `MageEngine.cs` | Ground-spell aiming (Stick + Gyro) |
| `MobSweepEngine.cs` | Auto TAB-cycle + attack while moving |
| `MovementEngine.cs` | Left-stick click-to-move logic |
| `SkillOrchestrator.cs` | Class-specific rotation execution |
| `SmartCursorEngine.cs` | Smart cursor assistance engine |
| `SupportEngine.cs` | Party targeting & healing cycle |

### Core/Input/ — Input Processing & Routing

| File | Responsibility |
|------|----------------|
| `InputReader.cs` | Normalizes XInput gamepad data |
| `InputRouter.cs` | Routes input to correct engine (Priority-Chain) |
| `InputCommandQueue.cs` | Thread-safe SendInput queue |
| `InputChain.cs` | Input processing chain |
| `InputFailoverManager.cs` | Fallback between input methods |
| `InputLatencyTracker.cs` | Input latency monitoring |
| `JitterService.cs` | Natural input timing variance (15-46ms) |
| `KeyboardInputService.cs` | Keyboard input handling |
| `MouseInputService.cs` | Mouse input handling |
| `InterceptionKeyboardStrategy.cs` | Interception driver keyboard strategy |
| `SendInputKeyboardStrategy.cs` | Win32 SendInput keyboard strategy |
| `SendInputMouseStrategy.cs` | Win32 SendInput mouse strategy |
| `BezierMouseService.cs` | Smooth mouse movement curves |
| `KernelInputService.cs` | Kernel-level input bypass |
| `IInputHandler.cs` | Input handler interface |
| `IKeyboardEmulationStrategy.cs` | Keyboard emulation strategy interface |
| `IMouseEmulationStrategy.cs` | Mouse emulation strategy interface |

### Core/Windows/ — Window Management & Tracking

| File | Responsibility |
|------|----------------|
| `WindowTracker.cs` | WinEventHook for game client bounds/DPI |
| `MultiClientWindowTracker.cs` | Multi-client window tracking |
| `WindowSwitcher.cs` | Instant multi-client switching |
| `DeviceNotificationWindow.cs` | Device change notifications |
| `IWindowTracker.cs` | Window tracker interface |
| `EnumWindowsProcDelegate.cs` | Win32 enum windows callback |

### Core/Interfaces/ — Core Interfaces & Contracts

| File | Responsibility |
|------|----------------|
| `IControllerProvider.cs` | Controller provider interface |
| `ICommandQueue.cs` | Command queue interface |
| `ICombatState.cs` | Combat state interface |
| `IFileSystem.cs` | File system abstraction |
| `IFeedbackProvider.cs` | Feedback provider interface |
| `IMessenger.cs` | Messenger/IPC interface |
| `INativeMethods.cs` | Native methods interface |
| `IProcessNameResolver.cs` | Process name resolver interface |
| `IProfileManager.cs` | Profile manager interface |
| `ITickProvider.cs` | Tick provider interface |

### Core/Services/ — Background Services & Managers

| File | Responsibility |
|------|----------------|
| `TelemetryService.cs` | Usage telemetry & analytics |
| `ProfileService.cs` | Profile CRUD operations |
| `ControllerService.cs` | High-level controller orchestration |
| `GyroService.cs` | Gyro service wrapper |
| `SystemMonitor.cs` | Focus Lock evaluation (500ms polling) |
| `PowerMonitor.cs` | Windows Sleep/Resume & Battery state |
| `ProfileValidator.cs` | Profile validation logic |
| `ConfigWatcher.cs` | Configuration file watcher |
| `ClassDetector.cs` | Weighted heuristic auto-class detection (FEAT-009) |
| `BuffManager.cs` | Active buff/debuff tracking (FEAT-008) |
| `SessionRecorder.cs` | JSONL session replay recorder |
| `StandbyManager.cs` | Smart Standby AFK detection |
| `SmartCursorService.cs` | D-Pad grid-hopping & precision damping |
| `ActionLogService.cs` | Event tracking & Channel-based logging |
| `EngineWatchdog.cs` | Tick latency monitoring |
| `LocalizationManager.cs` | i18n language switching |
| `ProfileApplier.cs` | Profile live parameter updates |
| `ProfileShareService.cs` | GitHub Gists API for community profiles |
| `DefaultProfileLoader.cs` | Built-in profile loading |
| `DiscordRpcService.cs` | Discord Rich Presence integration |
| `RoUiMenuService.cs` | RO in-game menu integration |
| `RadialMenuService.cs` | Radial menu service |
| `TtsAnnouncerService.cs` | Text-to-speech announcements |
| `HapticFeedbackService.cs` | Haptic feedback control |
| `GamepadUiNavigator.cs` | Gamepad UI navigation |
| `FeedbackRumbleProvider.cs` | Rumble feedback provider |
| `NullFeedbackProvider.cs` | Null feedback provider (disabled) |
| `ETWProvider.cs` | Event Tracing for Windows provider |
| `BatteryOptimizationService.cs` | Battery optimization |
| `AutoStartManager.cs` | Auto-start on Windows boot |
| `BackgroundTickProvider.cs` | Background tick timing |
| `WpfTickProvider.cs` | WPF tick provider |
| `HandheldDetector.cs` | Handheld device detection |
| `HandheldModeManager.cs` | PC Handheld integration |
| `LongPressTracker.cs` | Long-press detection |
| `MemoryAllocationTracker.cs` | Memory allocation tracking |
| `MonitorHelper.cs` | Monitor/DPI helper |
| `UpdateChecker.cs` | Update check service |
| `VoiceChatService.cs` | Windows Speech Recognition (Voice-to-Chat) |
| `OverlayRouter.cs` | Overlay window routing |
| `PartyManager.cs` | Party management |
| `PowerModeService.cs` | Windows power mode service |
| `CooldownManager.cs` | Cooldown tracking |

### Core/Utilities/ — Utility Classes & Helpers

| File | Responsibility |
|------|----------------|
| `EngineOrchestrator.cs` | Main tick coordination, lifecycle management |
| `EngineOptimizationPool.cs` | String pooling for hot-path allocations |
| `AppVersion.cs` | Application version info |
| `Messages.cs` | App-wide message constants |
| `RoFeatures.cs` | RO feature flags & detection |
| `NativeMethods.cs` | Win32 P/Invoke declarations |
| `NativeMethodsImpl.cs` | Native methods implementation |
| `NativeStructs.cs` | Win32 struct definitions |
| `ProcessNameResolver.cs` | Process name resolution |
| `RagnaControllerETW.cs` | ETW event source |
| `ShareCodeValidator.cs` | Share code validation |
| `SnapshotBuilder.cs` | Builds ControllerSnapshot from engine state |
| `AdvancedLogger.cs` | High-performance logger |
| `FeedbackSystem.cs` | User feedback collection |
| `GestureRecognizer.cs` | Gesture recognition |
| `GpuOverlayProfiler.cs` | GPU overlay profiling |
| `KiteStates.cs` | Kite engine state definitions |
| `MainViewModel.cs` | Main WPF ViewModel (MVVM) |
| `ObjectPools.cs` | Object pooling utilities |
| `Pools.cs` | Generic pool implementations |
| `EngineWatchdog.cs` | Tick latency watchdog |

### Core/Benchmarks/ — Performance Benchmarks

| File | Responsibility |
|------|----------------|
| `BenchmarkHarness.cs` | Benchmark test harness |

---

## Models/ — Data Models & DTOs

| File | Responsibility |
|------|----------------|
| `ParsedInput.cs` | Readonly record struct (current/prev frame) |
| `ControllerState.cs` | Readonly record struct for UI binding |
| `VirtualKey.cs` | Enum matching Win32 VK codes |
| `Settings.cs` | Global app settings (AppData) |

---

## Profiles/ — Profile Management

| File | Responsibility |
|------|----------------|
| `Profile.cs` | JSON structure of character profile |
| `ProfileManager.cs` | Load/Save/Import/Export/Backup |
| `AppJsonContext.cs` | AOT-friendly System.Text.Json context |

---

## Locales/ — Internationalization

JSON language files for live language switching:
- `en.json` — English (Default)
- `de.json` — German
- `tl.json` — Tagalog (Filipino)
- 38+ additional community languages

---

## Assets/ — Game Assets

| Subfolder | Content |
|-----------|---------|
| `Classes/` | Ragnarok Online class portraits |
| `Emotes/` | Emote images |

---

## Other Folders

| Folder | Responsibility |
|--------|----------------|
| `DefaultProfiles/` | Built-in character profiles |
| `Controls/` | Shared UI controls (library level) |
| `Services/` | Shared services (library level) |
| `AntiCheat/` | Interception driver (optional) |
| `Resources/` | Embedded resources (images, themes) |
| `Properties/` | AssemblyInfo & project properties |

---

## Namespace Mapping

| Folder | Namespace |
|--------|-----------|
| `Views/Windows/` | `RagnaController.Views.Windows` |
| `Views/Dialogs/` | `RagnaController.Views.Dialogs` |
| `Views/Overlays/` | `RagnaController.Views.Overlays` |
| `Views/Editors/` | `RagnaController.Views.Editors` |
| `Views/Controls/` | `RagnaController.Views.Controls` |
| `Controller/` | `RagnaController.Controller` |
| `Core/Engines/` | `RagnaController.Core` |
| `Core/Input/` | `RagnaController.Core` |
| `Core/Windows/` | `RagnaController.Core` |
| `Core/Interfaces/` | `RagnaController.Core` |
| `Core/Services/` | `RagnaController.Core` |
| `Core/Utilities/` | `RagnaController.Core` |
| `Core/Benchmarks/` | `RagnaController.Core` |
| `Models/` | `RagnaController.Models` |
| `Profiles/` | `RagnaController.Profiles` |
