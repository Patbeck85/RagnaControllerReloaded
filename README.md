# RagnaController — Smart Cursor Edition

<div align="center">

![Version](https://img.shields.io/badge/version-1.4.0-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2B%20%7C%20Steam%20Deck%20%7C%20ROG%20Ally-blue.svg)

**Premium, e-sports-ready middleware for Ragnarok Online — 100% white-hat, zero anti-cheat risk.**

</div>

---

## 🎮 What Is RagnaController?

RagnaController is a high-performance **hybrid action controller layer** (middleware) that translates Xbox/PlayStation controller inputs into precise mouse and keyboard macros for *Ragnarok Online*. It enables **Action-RPG style gameplay** on classic MMORPG controls.

### ✨ Key Features

| Feature | Description |
|---------|-------------|
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

- **Framework**: .NET 8 (Windows Desktop / WPF)
- **Language**: C# 12 (Primary Constructors, Collection Expressions, Pattern Matching)
- **UI**: WPF with custom "Obsidian & Gold" glassmorphism theme, MVVM architecture
- **Input Libraries**: `Hexa.NET.SDL2` (Xbox/PlayStation native support), Raw Win32 HID (DualSense Gyro/Lightbar)
- **Serialization**: System.Text.Json with Source Generation (`AppJsonContext`)

> **Note**: RagnaController now uses **Hexa.NET.SDL2** for native gamepad support, replacing SharpDX.XInput. This provides full DualShock 4/5 and Xbox controller compatibility with Lightbar and Rumble features.

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

1. **Download** the latest release from [GitHub Releases](https://github.com/RagnaController/RagnaController/releases)
2. **Extract** to a folder (e.g., `C:\RagnaController`)
3. **Run as Administrator**: Right-click → Properties → Compatibility → Run as administrator
4. **Connect your controller** via USB or Bluetooth
5. **Launch** `RagnaController.exe`

### First Configuration

1. **Select your character class** from the profile library
2. **Adjust deadzone** (recommended: 0.10–0.20)
3. **Choose game mode**: Pre-Renewal or Renewal timing
4. **Start playing!**

---

## 🎯 Core Architecture

### The Tick Loop (125Hz / 8ms)

RagnaController operates on a dedicated background thread running at **125 Hz** (every 8ms):

```
InputReader → SystemMonitor → EngineOrchestrator.OnTick → InputRouter.RouteInput → CombatEngine → InputCommandQueue → Win32.SendInput
```

### Decomposed Engine Components (v1.4.0)

The monolithic `HybridEngine` has been decomposed into focused, single-responsibility components:

| Component | Responsibility |
|-----------|----------------|
| `EngineOrchestrator` | Main tick coordination, lifecycle management, engine initialization |
| `InputRouter` | Modifier parsing, layer updates, engine chain routing (Kite → AutoTarget → Mage → Support) |
| `ProfileApplier` | Profile loading, live parameter updates, renewal/pre-renewal timing |
| `StandbyManager` | Smart Standby AFK detection, throttle polling to 20Hz during idle |
| `HybridEngine` | Thin façade maintaining full backward compatibility |

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
- **Community languages**: Add your own!

### How It Works

1. **JSON files** in `Locales/` folder (e.g., `en.json`, `de.json`)
2. **MarkupExtension** `{core:Loc KeyName}` in XAML
3. **Live switching**: Change language in Settings → No restart required!

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
├── ProfileWizardWindow.xaml / .cs           # Guided profile creation wizard
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
│   ├── NativeMethods.cs                    # P/Invoke signatures
│   │
│   ├── AutoTargetEngine.cs                 # Melee smart-aim & auto-attack
│   ├── KiteEngine.cs / KiteStates.cs       # Ranged hit-and-run FSM
│   ├── MageEngine.cs                       # Ground-spell aiming (Stick + Gyro)
│   ├── SupportEngine.cs                    # Party targeting & healing cycle
│   ├── ComboEngine.cs                      # Class-aware sequential skill chains
│   ├── MovementEngine.cs                   # Left-stick click-to-move logic
│   ├── CursorEngine.cs                     # Right-stick free mouse movement
│   ├── MobSweepEngine.cs                   # Auto TAB-cycle + attack while moving
│   │
│   ├── WindowTracker.cs                    # WinEventHook for game client bounds/DPI
│   ├── WindowSwitcher.cs                   # Instant multi-client switching
│   ├── SystemMonitor.cs                    # Focus Lock evaluation (500ms polling)
│   ├── PowerModeService.cs                 # Windows Sleep/Resume & Battery state
│   ├── HandheldModeManager.cs              # PC Handheld integration
│   │
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

### Build Verification

Before any deployment:
1. **Compile** project (`dotnet build`)
2. **Run tests** (`dotnet test`)
3. **Manual Windows testing** (VS Code / Visual Studio)
4. **Tooltip coverage verification** (Python script + manual inspection)

---

## 📜 License

MIT License — See [LICENSE](LICENSE) file for details.

### Community Guidelines

- ✅ **White-hat only**: No anti-cheat bypasses, no memory injection
- ✅ **Open contributions**: Add languages, profiles, features
- ✅ **Respect RoH**: Follow Ragnarok Online Handbook rules
- ❌ **No commercial use**: Personal/non-commercial projects only

---

## 🤝 Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md) for detailed guidelines.

### Quick Contribution Checklist

- [ ] Fork the repository
- [ ] Create feature branch (`git checkout -b feature/amazing-feature`)
- [ ] Ensure all tests pass (`dotnet test`)
- [ ] Update documentation
- [ ] Create pull request with clear description

---

## 📞 Support & Community

- **GitHub Issues**: [Report bugs](https://github.com/RagnaController/RagnaController/issues)
- **Discord**: Join our community server (link in release notes)
- **Documentation**: Full API docs coming soon

---

## 🙏 Acknowledgments

- **Ragnarok Online** — Classic MMORPG that inspired this project
- **Hexa.NET.SDL2** — Native gamepad support for Xbox and PlayStation controllers
- **Microsoft WPF** — UI framework
- **All contributors** — Community-driven development

---

<div align="center">

**Made with ❤️ for the Ragnarok Online community**

</div>
