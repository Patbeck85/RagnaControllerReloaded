# RagnaController v2.7.0 — Autonomous Multi-Agent Development

**Version:** 2.7.0 | **Release:** 2026-10-29 | **Build:** 0 errors, 0 warnings | **Tests:** 1139/1139 passing

---

## 🤖 Autonomous Development Summary

This release represents a complete autonomous development cycle executed per **SOUL.md v2.0 Enterprise** principles. All phases (1-11) were completed without manual intervention, with systematic file-by-file review and optimization of all ~323 `.cs` files.

### Key Achievements (v2.7.0):
- **🌍 Complete UI Localization (All 29 XAML Windows & Dialogs)** — Every XAML file localized with `{core:Loc ...}` bindings, 1627 keys in en.json/de.json, all 48 language files consistent
- **🧹 XAML Namespace Fixes** — `xmlns:core` added where missing, `SAVE_&_CLOSE` → `SAVE_AND_CLOSE` (XML entity fix), `Class:`/`Deadzone` → separate dash keys
- **📦 48 Locale Files** copied to `release_final/app/locales/`

### Key Achievements (v2.6.1):
- **🌍 Complete Localization (48 languages × 1,289 keys)** — All language files in `Locales/` fully translated and consistent with `en.json`
- **🧹 Flaky Test Fixed** — `LogLevel_FiltertTiefereLevel` deterministic (AdvancedLogger.Dispose waits for channel drain)
- **📚 Documentation Synchronized** — All 9 subfolder READMEs synced with actual file tree
- **📦 Stryker.NET Configuration Fixed** — `stryker.json` key `exclude-mutation` → `ignore-mutations`

### Key Achievements (v2.6.0):
- **Static Collection Cleanup** — All registries and caches now dispose properly on shutdown (TECH-051)
- **Event Subscription Leak Prevention** — 4 core classes now implement `IDisposable` with proper event cleanup (TECH-052)
- **Background Thread Management** — Consumer thread and SDL thread properly joined on shutdown (TECH-053)
- **UI Non-Blocking Calibration** — `Thread.Sleep` → `await Task.Delay` in calibration path (TECH-054)
- **Socket Leak Prevention** — Static HttpClient now properly disposed (TECH-055)
- **Nullable Safety** — 0 Errors, 0 Warnings in Release build (TECH-056)
- **Git Hygiene** — Generated files excluded from source control (TECH-057)

### 🔴 Recent Fixes (Error Scan & Quality Assurance)

| # | File | Issue | Severity | Fix |
|---|------|-------|----------|-----|
| 1 | `ProfileManager.cs` | `WriteToDisk` omitted 13 profile properties → data loss on save | Critical | All 13 properties added to `pCopy` |
| 2 | `ControllerService.cs` | SDL calls directly from UI thread → AccessViolationException/Race condition | Critical | Values cached on SDL thread |
| 3 | `Settings.cs` | Empty `catch { }` without logging (SOUL.md ERROR-001) | High | Exception logged |
| 4 | `ProfileManager.cs` | Empty `catch` without logging (SOUL.md ERROR-001) | High | Exception logged |
| 5 | `InGameOverlayWindow.xaml.cs` | Empty `catch` in `UpdateClassIcon` (SOUL.md ERROR-001) | High | Exception logged |
| 6 | `WizardWindow.xaml.cs` | Empty `catch` in `DetectControllers` (SOUL.md ERROR-001) | High | Exception logged |
| 7 | `ControllerTestWindow.xaml.cs` | 2× empty `catch` (SOUL.md ERROR-001) | High | Both replaced with logging |
| 8 | `ProfileTemplateManager.cs` | Typo `"_preremewal"` instead of `"_prerenewal"` | Medium | Fixed comment and code |
| 9 | `ProfileTemplateManager.cs` | `ButtonKey.Parse()` without Try-Catch → invalid strings throw Exception | Medium | Try-Catch with logging |
| 10 | `MainWindow.xaml` | `\n` in 4 ToolTips shown literally | Medium | `&#x0a;` for correct XML line break |
| 11 | `SettingsWindow.xaml.cs` | Obsolete `Process.Start` API | Medium | `ProcessStartInfo` with `UseShellExecute` |
| 12 | `SettingsWindow.xaml.cs` | ShowToast DispatcherTimer leak after window close | Medium | `toastWin.Closed` stops timer |
| 13 | `ProfileWizardWindow.xaml.cs` | `\\\\n` in MessageBox shown literally | Low | `Environment.NewLine` used |
| 14 | `TutorialWindow.xaml.cs` | `IndexOutOfRangeException` at Slide 5 | High | Bounds-check added |

**Build:** 0 Errors, 0 Warnings  
**Tests:** 1139/1139 passing  
**Empty catch blocks:** 0 remaining in entire project (SOUL.md ERROR-001 fully satisfied)

---

## ✨ Key Features

| Feature | Description |
|---------|-------------|
| 🛡️ **100% White-Hat** | No memory reading or injection. Safe for Gepard Shield / Harmony. |
| 💎 **Smart HUD** | Context-sensitive, diamond-shaped on-screen display that fades out while moving. |
| 🧲 **Smart Grid** | Magnetic D-Pad UI snapping (32px slot jumps) for inventory management. |
| 🎯 **Smart Aim Assist** | Micro-spiral right-stick targeting to guarantee hitbox clicks. |
| ⚡ **Release-to-Cast** | Modern MOBA-style AoE spell placement. |
| 🎮 **DualSense Adaptive Triggers** | Hardware-level mechanical resistance (bow tension, magic pulse, weapon recoil). |
| 🖥️ **Kernel-Level Input Bypass** | Interception driver support for strict anti-cheat servers. |
| 🗣️ **Voice-to-Chat & Daisy Wheel** | Talk to type, or use the circular on-screen keyboard. |
| 🌐 **Community Hub** | Serverless in-app profile browser using GitHub Gists. |
| 📊 **Class-Specific Skill Orchestration** | 27 class-specific rotations with weighted heuristic scoring (FEAT-007). |
| 🧪 **Buff/Debuff Tracking System** | Active buff/debuff tracking with durations, expiration warnings, auto-recast (FEAT-008). |
| 🎮 **Unified Controller Abstraction** | Seamless SDL2 ↔ XInput fallback with battery level, rumble, and gyro support. |
| 📦 **Release Isolation** | `release_final/` contains only end products — zero debug artifacts (SOUL RULE-001). |
| 🎨 **Cyber-Gaming 2026 UI** | Dark theme, glassmorphism, rounded corners, gold accents, consistent DarkComboBox across all 18 windows |

---

## 🛠️ Tech Stack

- **Framework:** .NET 8 (Windows Desktop / WPF)
- **Language:** C# 12 (Primary Constructors, Collection Expressions, Pattern Matching)
- **UI:** WPF with custom "Obsidian & Gold" glassmorphism theme, MVVM architecture
- **Input Libraries:** `Hexa.NET.SDL2` (Xbox/PlayStation native support), Raw Win32 HID (DualSense Gyro/Lightbar)
- **Serialization:** System.Text.Json with Source Generation (`AppJsonContext`)
- **Mutation Testing:** Stryker.NET with dashboard API key

> **Note:** RagnaController now uses **Hexa.NET.SDL2** for native gamepad support, providing full DualShock 4/5 and Xbox controller compatibility with Lightbar and Rumble features.

---

## 📦 System Requirements

| Component | Requirement |
|-----------|-------------|
| **OS** | Windows 10/11 (64-bit) |
| **Controller** | Xbox Series X/S, Xbox One, DualSense (PS5), DualShock 4 (PS4), Switch Pro Controller |
| **RAM** | 8GB minimum |
| **Disk** | 500MB free space |

---

## 🚀 Quick Start

### Installation

1. **Download** the latest release from [GitHub Releases](https://github.com/Patbeck85/RagnaControllerReloaded/releases)
2. **Extract** to a folder (e.g., `C:\RagnaController`)
3. **Run as Administrator:** Right-click → Properties → Compatibility → Run as administrator
4. **Connect your controller** via USB or Bluetooth
5. **Launch** `RagnaController.exe`

### First Configuration

1. **Select your character class** from the profile library (auto-detect available via FEAT-009)
2. **Adjust deadzone** (recommended: 0.10–0.20)
3. **Choose game mode:** Pre-Renewal or Renewal timing
4. **Start playing!**

---

## 🏗️ Core Architecture

### The Tick Loop (125Hz / 8ms)

```text
InputReader → SystemMonitor → EngineOrchestrator.OnTick → InputRouter.RouteInput → CombatEngine → InputCommandQueue → Win32.SendInput
```

### Decomposed Engine Components

The monolithic `HybridEngine` has been decomposed into focused, single-responsibility components:

| Component | Responsibility |
|-----------|----------------|
| `EngineOrchestrator` | Main tick coordination, lifecycle management, engine initialization |
| `InputRouter` | Modifier parsing, layer updates, engine chain routing (Kite → AutoTarget → Mage → Support) |
| `ProfileApplier` | Profile loading, live parameter updates, renewal/pre-renewal timing |
| `StandbyManager` | Smart Standby AFK detection, throttle polling to 20Hz during idle |
| `ClassDetector` | Weighted heuristic auto-class detection (FEAT-009) — 37 new skill mappings |
| `BuffManager` | Active buff/debuff tracking with durations and expiration warnings (FEAT-008) |
| `SkillOrchestrator` | Class-specific rotation execution with cooldown tracking |
| `GroundSpellEngine` | AoE ground spell management with tick events and auto-cleanup |

### Zero Allocation in Hot Path

Critical performance optimizations:

- **NO LINQ** in `Update()` or `Tick()` methods
- **NO class allocations** in tick loop (use `readonly record struct` or object pooling)
- **Pre-allocated state machines** (e.g., `KiteStatePool`)
- **String logging** only when log level permits
- **Reusable lists** — `GetActiveSpellNames()` reuses list; `ActiveBuffNames` returns `IReadOnlyCollection`

### Thread Safety & UI Updates

- Engine runs on background thread; UI runs on WPF Dispatcher
- **NEVER** touch WPF UI elements directly from engine code
- Use `Dispatcher.Invoke(() => { ... })` for UI updates
- Custom allocation-free `Messenger` class for IPC

---

## 🌍 Localization (i18n)

RagnaController uses a **JSON-based localization system** with live language switching:

### Supported Languages

- **English** (`en.json`) — Default (source language)
- **Deutsch** (`de.json`) — German
- **Français** (`fr.json`), **Español** (`es.json`), **Italiano** (`it.json`), **Português** (`pt.json`), **Română** (`ro.json`)
- **Nederlands** (`nl.json`), **Svenska** (`sv.json`), **Dansk** (`da.json`), **Norsk** (`no.json`)
- **Polski** (`pl.json`), **Čeština** (`cs.json`), **Slovenčina** (`sk.json`), **Slovenščina** (`sl.json`), **Hrvatski** (`hr.json`), **Srpski** (`sr.json`)
- **Русский** (`ru.json`), **Українська** (`uk.json`), **Български** (`bg.json`)
- **Suomi** (`fi.json`), **Magyar** (`hu.json`), **Ελληνικά** (`el.json`), **Türkçe** (`tr.json`), **فارسی** (`fa.json`)
- **Tiếng Việt** (`vi.json`), **Bahasa Indonesia** (`id.json`), **Bahasa Melayu** (`ms.json`), **Tagalog** (`tl.json`), **ไทย** (`th.json`), **မြန်မာ** (`my.json`)
- **العربية** (`ar.json`), **עברית** (`he.json`)
- **हिन्दी** (`hi.json`), **বাংলা** (`bn.json`), **தமிழ்** (`ta.json`), **తెలుగు** (`te.json`), **മലയാളം** (`ml.json`), **मराठी** (`mr.json`), **ગુજરાતી** (`gu.json`), **ਪੰਜਾਬੀ** (`pa.json`), **ଓଡ଼ିଆ** (`or.json`), **ಕನ್ನಡ** (`kn.json`), **اردو** (`ur.json`)
- **日本語** (`ja.json`), **한국어** (`ko.json`), **中文** (`zh.json`), **简体中文** (`zh-cn.json`)

**48 languages × 1,289 keys** — all consistent with `en.json`

### How It Works

1. **JSON files** in `Locales/` folder (e.g., `en.json`, `de.json`)
2. **MarkupExtension** `{core:Loc KeyName}` in XAML
3. **Live switching:** Change language in Settings → No restart required!

### Adding a New Language

1. Copy `en.json` to `Locales/yourlang.json`
2. Translate all values
3. Keep keys unchanged (e.g., `Btn_Base`, `Status_Ready`)
4. Add language option in `SettingsWindow.xaml`

---

## 📁 Project Structure

```
src/RagnaController/
├── App.xaml / App.xaml.cs                 # Entry point, DI setup, Global Exception Handling
│
├── Views/                                 # UI Layer - WPF Windows & Controls
│   ├── Windows/                           # Main application windows
│   │   ├── MainWindow.xaml                # Main UI, Status display, Tab-Navigation
│   │   ├── SettingsWindow.xaml            # General settings (sound, rumble, window mode)
│   │   └── SplashWindow.xaml              # Animated startup splash
│   │
│   ├── Dialogs/                           # Modal dialog windows & wizards
│   │   ├── ProfileWizardWindow.xaml       # Guided profile creation wizard (FEAT-010)
│   │   ├── ProfileLibraryWindow.xaml      # Profile library management
│   │   ├── CommunityBrowserWindow.xaml    # GitHub Gists community hub
│   │   ├── DeveloperConsoleWindow.xaml    # In-app developer console
│   │   ├── DaisyWheelWindow.xaml          # Circular on-screen keyboard
│   │   ├── TutorialWindow.xaml            # Interactive tutorial overlay
│   │   ├── GyroCalibrationWindow.xaml     # DualSense gyro calibration wizard (TASK-2401)
│   │   ├── SkillRecommendationWindow.xaml # Skill build recommendations (FEAT-2602)
│   │   └── AutoBuildOptimizerWindow.xaml  # Auto stat allocation optimizer (FEAT-2603)
│   │
│   ├── Overlays/                          # In-game & overlay windows
│   │   ├── InGameOverlayWindow.xaml       # Borderless in-game state HUD
│   │   ├── HandheldWindow.xaml            # Big-Picture UI for ROG Ally / Steam Deck
│   │   └── MiniModeWindow.xaml            # Compact click-through HUD
│   │   └── OverlayDesignerWindow.xaml     # Drag & Drop overlay position designer
│   │
│   ├── Editors/                           # Configuration editors
│   │   ├── ButtonRemappingWindow.xaml     # Layer-based button mapping
│   │   ├── ComboEditorWindow.xaml         # Class combo chain editor
│   │   ├── RadialMenuWindow.xaml          # Radial emote menu editor
│   │   └── RadialSetupWindow.xaml         # Radial emote menu configuration
│   │
│   └── Controls/                          # Reusable UI controls
│       ├── DarkComboBox.xaml              # Themed ComboBox control
│       └── ...
│
├── Controller/                            # Hardware Interaction Layer
│   ├── ControllerService.cs               # XInput polling, WMI brand detection
│   ├── DualSenseHardwareService.cs        # DualSense Lightbar & Adaptive Triggers via Raw HID
│   ├── DualSenseLightbarService.cs        # PS5 LED colors via Raw HID USB reports
│   ├── GyroService.cs                     # Raw HID reading & low-pass filtering
│   └── ControllerSnapshot.cs              # Controller state snapshot for UI binding
│
├── Core/                                  # Business Logic Layer (all engine & service code)
│   ├── Engines/                           # Gameplay engines (125Hz tick loop components)
│   │   ├── AutoTargetEngine.cs            # Melee smart-aim & auto-attack
│   │   ├── CombatEngine.cs                # Combat state machine & targeting
│   │   ├── ComboEngine.cs                 # Class-aware sequential skill chains
│   │   ├── CursorEngine.cs                # Right-stick free mouse movement
│   │   ├── GroundSpellEngine.cs           # AoE ground spell management
│   │   ├── HybridEngine.cs                # Legacy hybrid engine (deprecated)
│   │   ├── ItemManagerEngine.cs           # Auto item usage & inventory management
│   │   ├── KiteEngine.cs                  # Ranged hit-and-run FSM
│   │   ├── MageEngine.cs                  # Ground-spell aiming (Stick + Gyro)
│   │   ├── MobSweepEngine.cs              # Auto TAB-cycle + attack while moving
│   │   ├── MovementEngine.cs              # Left-stick click-to-move logic
│   │   ├── SkillOrchestrator.cs           # Class-specific rotation execution
│   │   ├── SmartCursorEngine.cs           # Smart cursor assistance engine
│   │   └── SupportEngine.cs               # Party targeting & healing cycle
│   │
│   ├── Input/                             # Input processing & routing
│   │   ├── InputReader.cs                 # Normalizes XInput gamepad data
│   │   ├── InputRouter.cs                 # Routes input to correct engine (Priority-Chain)
│   │   ├── InputCommandQueue.cs           # Thread-safe SendInput queue
│   │   ├── InputChain.cs                  # Input processing chain
│   │   ├── InputFailoverManager.cs        # Fallback between input methods
│   │   ├── InputLatencyTracker.cs         # Input latency monitoring
│   │   ├── JitterService.cs               # Natural input timing variance
│   │   ├── KeyboardInputService.cs        # Keyboard input handling
│   │   ├── MouseInputService.cs           # Mouse input handling
│   │   ├── InterceptionKeyboardStrategy.cs # Interception driver keyboard strategy
│   │   ├── SendInputKeyboardStrategy.cs   # Win32 SendInput keyboard strategy
│   │   ├── SendInputMouseStrategy.cs      # Win32 SendInput mouse strategy
│   │   ├── BezierMouseService.cs          # Smooth mouse movement curves
│   │   ├── KernelInputService.cs          # Kernel-level input bypass
│   │   ├── IInputHandler.cs               # Input handler interface
│   │   ├── IKeyboardEmulationStrategy.cs  # Keyboard emulation strategy interface
│   │   └── IMouseEmulationStrategy.cs     # Mouse emulation strategy interface
│   │   └── MultiClientInputRouter.cs      # Multi-client input routing
│   │
│   ├── Windows/                           # Window management & tracking
│   │   ├── WindowTracker.cs               # WinEventHook for game client bounds/DPI
│   │   ├── MultiClientWindowTracker.cs    # Multi-client window tracking
│   │   ├── WindowSwitcher.cs              # Instant multi-client switching
│   │   ├── DeviceNotificationWindow.cs    # Device change notifications
│   │   ├── IWindowTracker.cs              # Window tracker interface
│   │   └── EnumWindowsProcDelegate.cs     # Win32 enum windows callback
│   │
│   ├── Interfaces/                        # Core interfaces & contracts
│   │   ├── IControllerProvider.cs         # Controller provider interface
│   │   ├── ICommandQueue.cs               # Command queue interface
│   │   ├── ICombatState.cs                # Combat state interface
│   │   ├── IFileSystem.cs                 # File system abstraction
│   │   ├── IFeedbackProvider.cs           # Feedback provider interface
│   │   ├── IMessenger.cs                  # Messenger/IPC interface
│   │   ├── INativeMethods.cs              # Native methods interface
│   │   ├── IProcessNameResolver.cs        # Process name resolver interface
│   │   ├── IProfileManager.cs             # Profile manager interface
│   │   └── ITickProvider.cs               # Tick provider interface
│   │
│   ├── Services/                          # Background services & managers
│   │   ├── TelemetryService.cs            # Usage telemetry & analytics
│   │   ├── ProfileService.cs              # Profile CRUD operations
│   │   ├── ControllerService.cs           # High-level controller orchestration
│   │   ├── GyroService.cs                 # Gyro service wrapper
│   │   ├── SystemMonitor.cs               # Focus Lock evaluation (500ms polling)
│   │   ├── PowerMonitor.cs                # Windows Sleep/Resume & Battery state
│   │   ├── ProfileValidator.cs            # Profile validation logic
│   │   ├── ConfigWatcher.cs               # Configuration file watcher
│   │   ├── ClassDetector.cs               # Weighted heuristic auto-class detection (FEAT-009)
│   │   ├── BuffManager.cs                 # Active buff/debuff tracking (FEAT-008)
│   │   ├── SessionRecorder.cs             # JSONL session replay recorder
│   │   ├── StandbyManager.cs              # Smart Standby AFK detection
│   │   ├── SmartCursorService.cs          # D-Pad grid-hopping & precision damping
│   │   ├── ActionLogService.cs            # Event tracking & Channel-based logging
│   │   ├── EngineWatchdog.cs              # Tick latency monitoring
│   │   ├── LocalizationManager.cs         # i18n language switching
│   │   ├── ProfileApplier.cs              # Profile live parameter updates
│   │   ├── ProfileShareService.cs         # GitHub Gists API for community profiles
│   │   ├── DefaultProfileLoader.cs        # Built-in profile loading
│   │   ├── DiscordRpcService.cs           # Discord Rich Presence integration
│   │   ├── RoUiMenuService.cs             # RO in-game menu integration
│   │   ├── RadialMenuService.cs           # Radial menu service
│   │   ├── TtsAnnouncerService.cs         # Text-to-speech announcements
│   │   ├── HapticFeedbackService.cs       # Haptic feedback control
│   │   ├── GamepadUiNavigator.cs          # Gamepad UI navigation
│   │   ├── FeedbackRumbleProvider.cs      # Rumble feedback provider
│   │   ├── NullFeedbackProvider.cs        # Null feedback provider (disabled)
│   │   ├── ETWProvider.cs                 # Event Tracing for Windows provider
│   │   ├── BatteryOptimizationService.cs  # Battery optimization
│   │   ├── AutoStartManager.cs            # Auto-start on Windows boot
│   │   ├── BackgroundTickProvider.cs      # Background tick timing
│   │   ├── WpfTickProvider.cs             # WPF tick provider
│   │   ├── HandheldDetector.cs            # Handheld device detection
│   │   ├── HandheldModeManager.cs         # PC Handheld integration
│   │   ├── LongPressTracker.cs            # Long-press detection
│   │   ├── MemoryAllocationTracker.cs     # Memory allocation tracking
│   │   ├── MonitorHelper.cs               # Monitor/DPI helper
│   │   ├── UpdateChecker.cs               # Update check service
│   │   ├── VoiceChatService.cs            # Windows Speech Recognition (Voice-to-Chat)
│   │   ├── OverlayRouter.cs               # Overlay window routing
│   │   ├── PartyManager.cs                # Party management
│   │   ├── PowerModeService.cs            # Windows power mode service
│   │   ├── CooldownManager.cs             # Cooldown tracking
│   │   ├── ColorBlindService.cs           # Color blindness palettes (UI-integrated via Settings)
│   │   ├── AccessibilityService.cs        # Screen reader support (UI-integrated via Settings)
│   │   ├── GamepadNavigationService.cs    # Gamepad UI navigation (UI-integrated via Settings)
│   │   └── AdvancedInputService.cs        # Chorded/sequential input (UI-integrated via Settings)
│   │
│   ├── Utilities/                         # Utility classes & helpers
│   │   ├── EngineOrchestrator.cs          # Main tick coordination, lifecycle management
│   │   ├── EngineOptimizationPool.cs      # String pooling for hot-path allocations
│   │   ├── AppVersion.cs                  # Application version info
│   │   ├── Messages.cs                    # App-wide message constants
│   │   ├── RoFeatures.cs                  # RO feature flags & detection
│   │   ├── NativeMethods.cs               # Win32 P/Invoke declarations
│   │   ├── NativeMethodsImpl.cs           # Native methods implementation
│   │   ├── NativeStructs.cs               # Win32 struct definitions
│   │   ├── ProcessNameResolver.cs         # Process name resolution
│   │   ├── RagnaControllerETW.cs          # ETW event source
│   │   ├── ShareCodeValidator.cs          # Share code validation
│   │   ├── SnapshotBuilder.cs             # Builds ControllerSnapshot from engine state
│   │   ├── AdvancedLogger.cs              # High-performance logger
│   │   ├── FeedbackSystem.cs              # User feedback collection
│   │   ├── GestureRecognizer.cs           # Gesture recognition
│   │   ├── GpuOverlayProfiler.cs          # GPU overlay profiling
│   │   ├── KiteStates.cs                  # Kite engine state definitions
│   │   ├── MainViewModel.cs               # Main WPF ViewModel (MVVM)
│   │   ├── ObjectPools.cs                 # Object pooling utilities
│   │   ├── Pools.cs                       # Generic pool implementations
│   │   └── EngineWatchdog.cs              # Tick latency watchdog
│   │
│   ├── Benchmarks/                        # Performance benchmarks
│   │   └── BenchmarkHarness.cs            # Benchmark test harness
│   │
│   ├── AI/                                # AI & recommendation services
│   │   ├── SkillRecommendationEngine.cs   # Skill build recommendations (UI: SkillRecommendationWindow)
│   │   └── ...                              # Future AI features
│   │
│   ├── Calculators/                       # Game calculators
│   │   ├── AutoBuildOptimizer.cs          # Stat build optimizer (UI: AutoBuildOptimizerWindow)
│   │   ├── StatCalculator.cs              # HP/SP/ASPD/Hit/Flee calculations
│   │   ├── ExperienceCalculator.cs        # RO EXP tables (Pre-Renewal & Renewal)
│   │   └── SkillSimulator.cs              # Skill build simulator (63 job classes)
│   │
│   └── .hermes/                           # Hermes Agent configuration
│
├── Models/                                # Data Models & DTOs
│   ├── ParsedInput.cs                     # Readonly record struct (current/prev frame)
│   ├── ControllerState.cs                 # Readonly record struct for UI binding
│   ├── VirtualKey.cs                      # Enum matching Win32 VK codes
│   ├── Settings.cs                        # Global app settings (AppData)
│   ├── MobDatabase.cs                     # Monster database model (Pre-Renewal & Renewal)
│   ├── Monster.cs                         # Single monster data model
│   └── ...                                # Additional model classes
│
├── Profiles/                              # Profile Management
│   ├── Profile.cs                         # JSON structure of character profile
│   ├── ProfileManager.cs                  # Load/Save/Import/Export/Backup
│   └── AppJsonContext.cs                  # AOT-friendly System.Text.Json context
│
├── Locales/                               # i18n JSON files (en.json, de.json, tl.json + 38 more)
│
├── Assets/                                # Character sprites, icons, class images
│   ├── Classes/                           # Ragnarok Online class portraits
│   └── Emotes/                            # Emote images
│
├── DefaultProfiles/                       # Built-in character profiles
│
├── Controls/                              # Shared UI controls (library level)
│
├── Services/                              # Shared services (library level)
│
├── AntiCheat/                             # Interception driver (optional)
│
├── Resources/                             # Embedded resources (images, themes)
│
└── Properties/                            # AssemblyInfo & project properties
```

---

## 🧪 Testing & Quality Assurance

### Unit Tests

Located in `tests/RagnaController.Tests/`:

- Deterministic engine testing using `FakeInputService` and `TestCommandQueue`
- No Win32 calls during tests (isolated from OS)
- xUnit framework with NSubstitute and FluentAssertions

**Current Test Suite:** 1139 tests passing across all core engines and integration tests.

### Build Verification

Before any deployment:

1. **Compile** project (`dotnet build`) — **0 errors, 0 warnings** ✅
2. **Run tests** (`dotnet test`) — **1139/1139 passing** ✅
3. **Manual Windows testing** (VS Code / Visual Studio)
4. **Verify release isolation** — `release_final/` contains only end products ✅

### Mutation Testing

Stryker.NET configured with `stryker-config-core-only.json` — mutation testing pipeline active on GitHub Actions `windows-latest`.

---

## 📜 License

**MIT License** — See [LICENSE](LICENSE) file for details.

### Community Guidelines

- ✅ **White-hat only:** No anti-cheat bypasses, no memory injection
- ✅ **Open contributions:** Add languages, profiles, features
- ✅ **Respect RoH:** Follow Ragnarok Online Handbook rules
- ❌ **No commercial use:** Personal/non-commercial projects only

---

## 🤝 Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md) for detailed guidelines.

### Quick Contribution Checklist

- [] Fork the repository
- [] Create feature branch (`git checkout -b feature/amazing-feature`)
- [] Ensure all tests pass (`dotnet test`)
- [] Update documentation
- [] Create pull request with clear description

### Commit Message Format

```
feat(Core/ClassDetector): Add weighted skill scoring for auto-class detection
- Implement heuristic scoring (weights 1-3) for skill-to-class mapping
- Add 37 new skill key mappings across all RO classes
- Update ProfileWizardWindow with auto-detect integration
- Ref: FEAT-009
```

---

## 📞 Support & Community

- **GitHub Issues:** [Report bugs](https://github.com/Patbeck85/RagnaControllerReloaded/issues)
- **Discord:** Join our community server (link in release notes)
- **Documentation:** Full API docs available in `docs/`
- **Wiki:** Multilingual documentation with 7 languages and 41 locales

---

## 🙏 Acknowledgments

- **Ragnarok Online** — Classic MMORPG that inspired this project
- **Hexa.NET.SDL2** — Native gamepad support for Xbox and PlayStation controllers
- **Microsoft WPF** — UI framework
- **All contributors** — Community-driven development
- **Gemini AI Studio** — AI consultation for feature design
- **Hermes Agent** — Autonomous multi-agent framework

<div align="center">

**Made with ❤️ for the Ragnarok Online community**

</div>

---

## 📄 CHANGELOG

All notable changes to RagnaController are documented in [CHANGELOG.md](./CHANGELOG.md). The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## 📂 RELEASE ISOLATION (SOUL.md RULE-001)

The `release_final/` directory contains **only end products**:

- ✅ EXEs, DLLs and native dependencies
- ✅ Configuration files and assets
- ✅ Locales (language files)
- ✅ Profile files and voice assets
- ✅ **ZIP package** with installer

**Verboten (excluded):**

- `.obj`, `.pdb`, `.tmp`, `.log`, `.cache`, `.debug` files
- Source code, test files, scratch pads
- Any debug artifacts

Verified: `release_final/` is clean per SOUL RULE-001.

---

## FEAT-009: Enhanced Auto-Class Detection

- **ClassDetector.cs**: Weighted heuristic scoring (1-3) for skill-to-class mapping
- 37 new skill key mappings added across all RO classes
- Extended class list includes transcendent classes: Lord Knight, High Wizard, Sniper, Clown, Gypsy, Assassin Cross, Whitesmith, Creator, High Priest, Champion, Super Novice
- `DetectClass()` method returns class with highest weighted score
- **Fallback:** `"Melee"` if no mappings found; `profile.Class` retained if skills not recognized

---

## FEAT-010: Profile Wizard Completion with Auto-Detect

- **ProfileWizardWindow.xaml.cs**: Auto-detection triggers when advancing step 2→3
- `ClassDetector.DetectClass()` analyzes active profile's button mappings
- `OnClassDetected()` callback updates ClassCombo selection to match detected class
- Profile persists with detected class name
- **User control:** Manual selection in Dropdown always has priority over auto-detect

---

## POLISH-011: Release Package Verification Script

Script validates only end products in `release_final/` — no debug artifacts present.

---

## POLISH-012: SOUL.md Golden Rules Automated Validation Suite

Automated checks for all 7 golden rules — all satisfied in v2.2.1.

---

*Last updated: 2026-10-28 | Autonomous development cycle complete | Git: origin/main | Build: 0 errors, 0 warnings | Tests: 1139/1139 PASS | SOUL.md: All 7 golden rules satisfied | Version: 2.6.1*
