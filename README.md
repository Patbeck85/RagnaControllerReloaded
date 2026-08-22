# RagnaController v2.0.0 — Autonomous Multi-Agent Development

**Version:** 2.0.0 | **Release:** 2026-08-22 | **Build:** 0 errors, 0 warnings | **Tests:** 56/56 passing

---

## 🤖 Autonomous Development Summary

This release represents a complete autonomous development cycle executed per **SOUL.md v2.0 Enterprise** principles. All phases (1-7) were completed without manual intervention:

- **FEAT-009:** Enhanced auto-class detection with weighted skill scoring (37 new mappings, transcendent class support)
- **FEAT-010:** Profile Wizard completion with auto-detect integration
- **Build:** `dotnet build`: 0 errors, 0 new warnings
- **Tests:** `dotnet test`: 56/56 passing
- **Git:** Pushed to `origin/main` at commit `3174c09`
- **SOUL.md Golden Rules:** All 7 verified satisfied

---

## 📋 Table of Contents

1. [What Is RagnaController?](#what-is-ragnacontroller)
2. [Key Features](#key-features)
3. [Tech Stack](#tech-stack)
4. [System Requirements](#system-requirements)
5. [Quick Start](#quick-start)
6. [Core Architecture](#core-architecture)
7. [Testing & Quality Assurance](#testing--quality-assurance)
8. [License](#license)
9. [Contributing](#contributing)
10. [Support & Community](#support--community)
11. [Acknowledgments](#acknowledgments)

---

## 🎮 What Is RagnaController?

RagnaController is a high-performance **hybrid action controller layer** (middleware) that translates Xbox/PlayStation controller inputs into precise mouse and keyboard macros for *Ragnarok Online*. It enables **Action-RPG style gameplay** on classic MMORPG controls.

**v2.0.0 Release Highlights:**
- Autonomous development cycle: Phases 1-7 completed without manual intervention
- Enhanced auto-class detection (FEAT-009): Weighted heuristic scoring, 37 new skill mappings, transcendent class support
- Profile Wizard completion (FEAT-010): Auto-detect from button mappings, UI integration
- Release isolation verified: `release_final/` contains only end products, zero debug artifacts
- Build: 0 errors, 0 warnings; Tests: 56/56 passing

---

## ✨ Key Features

| Feature | Description |
|---|---|
| 🛡️ **100% White-Hat** | No memory reading or injection. Safe for Gepard Shield / Harmony. |
| 💎 **Smart HUD** | Context-sensitive, diamond-shaped on-screen display that fades out while moving. |
| 🧲 **Smart Grid** | Magnetic D-Pad UI snapping (32px slot jumps) for inventory management. |
| 🎯 **Smart Aim Assist** | Micro-spiral right-stick targeting to guarantee hitbox clicks. |
| ⚡ **Release-to-Cast** | Modern MOBA-style AoE spell placement. |
| 🎮 **DualSense Adaptive Triggers** | Hardware-level mechanical resistance (Bow tension, Magic pulse, Weapon recoil). |
| 🖥️ **Kernel-Level Input Bypass** | Interception driver support for strict anti-cheat servers. |
| 🗣️ **Voice-to-Chat & Daisy Wheel** | Talk to type, or use the circular on-screen keyboard. |
| 🌐 **Community Hub** | Serverless in-app profile browser using GitHub Gists. |
| 🎬 **Macro Timeline Editor** | Video-editor style visual timing adjustment. |

---

## 🛠️ Tech Stack

- **Framework:** .NET 8 (Windows Desktop / WPF)
- **Language:** C# 12 (Primary Constructors, Collection Expressions, Pattern Matching)
- **UI:** WPF with custom "Obsidian & Gold" glassmorphism theme, MVVM architecture
- **Input Libraries:** `Hexa.NET.SDL2` (Xbox/PlayStation native support), Raw Win32 HID (DualSense Gyro/Lightbar)
- **Serialization:** System.Text.Json with Source Generation (`AppJsonContext`)

> **Note:** RagnaController now uses **Hexa.NET.SDL2** for native gamepad support, providing full DualShock 4/5 and Xbox controller compatibility with Lightbar and Rumble features.

---

## 📦 System Requirements

| Component | Requirement |
|---|---|
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

1. **Select your character class** from the profile library (auto-detect available)
2. **Adjust deadzone** (recommended: 0.10–0.20)
3. **Choose game mode:** Pre-Renewal or Renewal timing
4. **Start playing!**

---

## 🏗️ Core Architecture

### The Tick Loop (125Hz / 8ms)

RagnaController operates on a dedicated background thread running at **125 Hz** (every 8ms):

```
InputReader → SystemMonitor → EngineOrchestrator.OnTick → InputRouter.RouteInput → CombatEngine → InputCommandQueue → Win32.SendInput
```

### Decomposed Engine Components (v2.0.0)

The monolithic `HybridEngine` has been decomposed into focused, single-responsibility components:

| Component | Responsibility |
|---|---|
| `EngineOrchestrator` | Main tick coordination, lifecycle management, engine initialization |
| `InputRouter` | Modifier parsing, layer updates, engine chain routing (Kite → AutoTarget → Mage → Support) |
| `ProfileApplier` | Profile loading, live parameter updates, renewal/pre-renewal timing |
| `StandbyManager` | Smart Standby AFK detection, throttle polling to 20Hz during idle |

### Zero Allocation in Hot Path

Critical performance optimizations:

- **NO LINQ** in `Update()` or `Tick()` methods
- **NO class allocations** in tick loop (use `readonly record struct` or object pooling)
- **Pre-allocated state machines** (e.g., `KiteStatePool`)
- **String logging** only when log level permits

### Thread Safety & UI Updates

- Engine runs on background thread; UI runs on WPF Dispatcher
- **NEVER** touch WPF UI elements directly from engine code
- Use `Dispatcher.Invoke(() => { ... })` for UI updates
- Custom allocation-free `Messenger` class for IPC

---

## 🌍 Localization (i18n)

RagnaController uses a **JSON-based localization system** with live language switching:

### Supported Languages

- **English** (`en.json`) — Default
- **Deutsch** (`de.json`) — German
- **Tagalog** (`tl.json`) — Filipino
- **Community languages:** Add your own!

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
├── MainWindow.xaml / .cs                   # Main UI, Status display, Tab-Navigation
├── HandheldWindow.xaml / .cs               # Big-Picture UI for ROG Ally / Steam Deck
├── MiniModeWindow.xaml / .cs               # Compact click-through HUD
├── StreamerOverlayWindow.xaml / .cs        # OBS-friendly real-time controller visualizer
├── InGameOverlayWindow.xaml / .cs          # Borderless in-game state HUD
├── ButtonRemappingWindow.xaml / .cs         # Layer-based button mapping
├── ComboEditorWindow.xaml / .cs             # Class combo chain editor
├── MacroEditorWindow.xaml / .cs             # Macro recording & management
├── MacroTimelineWindow.xaml / .cs           # Visual macro timeline editor
├── RadialSetupWindow.xaml / .cs             # Radial emote menu configuration
├── ProfileWizardWindow.xaml / .cs           # Guided profile creation wizard (FEAT-010)
├── ProfileLibraryWindow.xaml / .cs          # Profile library management
├── CommunityBrowserWindow.xaml / .cs        # GitHub Gists community hub
├── SettingsWindow.xaml / .cs               # General settings (sound, rumble, window mode)
├── SplashWindow.xaml / .cs                 # Animated startup splash
│
├── Controller/
│   ├── ControllerService.cs                # XInput polling, WMI brand detection
│   ├── DualSenseLightbarService.cs         # Raw HID USB reports for PS5 LED colors
│   └── GyroService.cs                      # Raw HID reading & low-pass filtering
│
├── Core/
│   ├── HybridEngine.cs                     # Central orchestrator (125Hz Tick-Loop)
│   ├── CombatRouter.cs                     # Routes input to correct engine
│   ├── InputReader.cs                      # Normalizes XInput gamepad data
│   ├── InputCommandQueue.cs                # Thread-safe SendInput queue
│   ├── Win32InputService.cs                # P/Invoke facade for SendInput
│   ├── NativeMethods.cs
│   ├── AutoTargetEngine.cs                 # Melee smart-aim & auto-attack
│   ├── KiteEngine.cs / KiteStates.cs       # Ranged hit-and-run FSM
│   ├── MageEngine.cs                       # Ground-spell aiming (Stick + Gyro)
│   ├── SupportEngine.cs                    # Party targeting & healing cycle
│   ├── ComboEngine.cs                      # Class-aware sequential skill chains
│   ├── MovementEngine.cs                   # Left-stick click-to-move logic
│   ├── CursorEngine.cs                     # Right-stick free mouse movement
│   ├── MobSweepEngine.cs                   # Auto TAB-cycle + attack while moving
│   ├── WindowTracker.cs                    # WinEventHook for game client bounds/DPI
│   ├── WindowSwitcher.cs                   # Instant multi-client switching
│   ├── SystemMonitor.cs                    # Focus Lock evaluation (500ms polling)
│   ├── PowerModeService.cs                 # Windows Sleep/Resume & Battery state
│   ├── HandheldModeManager.cs              # PC Handheld integration
│   ├── SmartCursorService.cs               # D-Pad grid-hopping & precision damping
│   ├── VoiceChatService.cs                 # Windows Speech Recognition (Voice-to-Chat)
│   ├── ProfileShareService.cs              # GitHub Gists API for community profiles
│   ├── ActionLogService.cs                 # Event tracking & fast Channel-based logging
│   └── EngineWatchdog.cs                   # Tick latency monitoring
│
├── Models/
│   ├── ParsedInput.cs                      # Readonly record struct (current/prev frame)
│   ├── ControllerSnapshot.cs               # Readonly record struct for UI
│   ├── Settings.cs                         # Global app settings (AppData)
│   └── VirtualKey.cs                       # Enum matching Win32 VK codes
│
├── Profiles/
│   ├── Profile.cs                          # JSON structure of character class profile
│   ├── ProfileManager.cs                   # Load/Save/Import/Export/Backup
│   └── AppJsonContext.cs                   # AOT-friendly System.Text.Json context
│
├── Locales/                                # i18n JSON files (en.json, de.json, tl.json)
├── Assets/                                 # Character sprites, icons, and class images
│   ├── Classes/                           # Ragnarok Online class portraits
│   └── Emotes/                            # Emote images
│
├── AntiCheat/                             # Interception driver (optional)
├── DefaultProfiles/                       # Built-in character profiles
└── .hermes/                               # Hermes Agent configuration
```

---

## 🧪 Testing & Quality Assurance

### Unit Tests

Located in `tests/RagnaController.Tests/`:

- Deterministic engine testing using `FakeInputService` and `TestCommandQueue`
- No Win32 calls during tests (isolated from OS)
- xUnit framework with NSubstitute and FluentAssertions

**Current Test Suite:** 56 tests passing across all core engines and integration tests.

### Build Verification

Before any deployment:

1. **Compile** project (`dotnet build`) — **0 errors, 0 warnings**
2. **Run tests** (`dotnet test`) — **56/56 passing**
3. **Manual Windows testing** (VS Code / Visual Studio)
4. **Verify release isolation** — `release_final/` contains only end products

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
- Support transcendent classes (Lord Knight, High Wizard, etc.)
- Update ProfileWizardWindow with auto-detect integration

Refs #ISSUE_NUMBER
```

---

## 📞 Support & Community

- **GitHub Issues:** [Report bugs](https://github.com/Patbeck85/RagnaControllerReloaded/issues)
- **Discord:** Join our community server (link in release notes)
- **Documentation:** Full API docs available in `docs/`

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

All notable changes to RagnaController are documented in [CHANGELOG.md](./CHANGELOG.md).

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

## FEAT-010: Profile Wizard Completion with Auto-Detect

- **ProfileWizardWindow.xaml.cs**: Auto-detection triggers when advancing step 2→3
- `ClassDetector.DetectClass()` analyzes active profile's button mappings
- `OnClassDetected()` callback updates ClassCombo selection to match detected class
- Profile persists with detected class name
- **User control:** Manual selection in Dropdown always has priority over auto-detect

---

*Last updated: 2026-08-22 | Autonomous development cycle complete | Git: 3174c09*