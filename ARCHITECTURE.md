# RagnaController Architecture Reference

**Version:** 2.3.0 | **Status:** Production | **Last updated:** 2026-10-20

---

## 1. OVERVIEW

RagnaController is a .NET 8 WPF desktop application for Ragnarok Online (Classic / Pre-Renewal / Renewal). It provides controller-to-keyboard/mouse mapping, auto-attack, kite, mage, support, and combo engines, plus profile management, community hub integration, and telemetry.

**Guiding principles (SOUL.md):**
1. Maximale Autonomie — selbstständige Planung, Entwicklung, Tests, Dokumentation, Release.
2. Strukturierte Zusammenarbeit — klare Rollen, Kanban, Handover-Format.
3. Reproduzierbarkeit — deterministische Builds, SemVer, CHANGELOG.
4. Automatische QA — 0 Fehler, 0 Warnungen, 100% Tests grün vor jedem Release.
5. Release-Isolation — `release_final/` enthält nur Endprodukte.
6. Kontinuierliche Selbstverbesserung — Fehler werden analysiert, behoben, re-getestet.

---

## 2. LAYERED ARCHITECTURE

```
┌─────────────────────────────────────────┐
│  Presentation Layer (WPF / XAML)        │
│  Views/Windows/, Views/Dialogs/,        │
│  Views/Overlays/, Views/Editors/        │
└─────────────────────────────────────────┘
                    │
┌─────────────────────────────────────────┐
│  Business Logic Layer (Core/)           │
│  Engines/, Input/, Services/,           │
│  Windows/, Interfaces/, Utilities/      │
└─────────────────────────────────────────┘
                    │
┌─────────────────────────────────────────┐
│  Hardware Interaction Layer             │
│  Controller/ (SDL2, XInput, Raw HID)    │
└─────────────────────────────────────────┘
                    │
┌─────────────────────────────────────────┐
│  OS / Win32 APIs                        │
│  user32.dll, kernel32.dll, setupapi.dll │
└─────────────────────────────────────────┘
```

---

## 3. CORE COMPONENTS

### 3.1 EngineOrchestrator (`Core/Utilities/EngineOrchestrator.cs`)
Main tick coordinator. Runs at 125Hz (8ms). Delegates to:
- InputRouter — input routing & engine chain
- ProfileApplier — profile loading & live updates
- StandbyManager — AFK detection & power management

### 3.2 InputRouter (`Core/Input/InputRouter.cs`)
Routes parsed input to the correct engine based on priority chain:
`KiteEngine → AutoTargetEngine → MageEngine → SupportEngine → ComboEngine → MovementEngine`

### 3.3 ProfileApplier (`Core/Services/ProfileApplier.cs`)
Loads profile configurations into all engines. Handles Pre-Renewal / Renewal timing switching via `ApplyGameMode(bool)`.

### 3.4 InputCommandQueue (`Core/Input/InputCommandQueue.cs`)
Thread-safe SendInput queue with batching. Consumer thread processes commands in order. Supports mouse, keyboard, wheel, chat, wait, and action commands.

### 3.5 ControllerService (`Controller/ControllerService.cs`)
SDL2 GameController wrapper. All SDL calls run on a dedicated background thread (`_sdlThread`) because SDL2 is NOT thread-safe for event pumping. Battery level and button states are cached on the SDL thread for thread-safe UI access.

### 3.6 ControllerManager (`Core/Services/ControllerManager.cs`)
Unified controller manager. Tries SDL2 first, falls back to XInput. Implements `IControllerProvider`.

### 3.7 WindowTracker (`Core/Windows/WindowTracker.cs`)
Tracks RO client windows via WinEventHook. Provides CenterX/Y, ClientW/H, DpiScale for mouse coordinate mapping.

---

## 4. THREAD SAFETY (THREAD-001, MEMORY-001)

| Rule | Requirement |
|------|-------------|
| SDL2 access | Only on dedicated SDL thread (`ControllerService._sdlThread`) |
| UI updates | Only via `Dispatcher.Invoke` / `Dispatcher.BeginInvoke` |
| Battery / Button states | Read from SDL-thread cache, not direct SDL calls |
| Resource cleanup | Every `IDisposable` implemented; events unsubscribed in `Dispose` / `Window_Closing` |
| No silent errors | Empty `catch { }` forbidden (SOUL.md ERROR-001) |

---

## 5. PROFILE SYSTEM

### 5.1 Profile (`Profiles/Profile.cs`)
JSON-serializable character profile. Contains:
- Class, sensitivity, deadzone, curve settings
- Engine toggles (AutoAttack, Kite, Mage, Support, Combo, ItemManager, PartyManager)
- ButtonMappings (`Dictionary<ButtonKey, ButtonAction>`)
- ControllerConfigs (per-controller settings)
- CreatedAt, DownloadCount, Priority, IsEnabled

### 5.2 ProfileManager (`Profiles/ProfileManager.cs`)
Loads/saves profiles from `DefaultProfiles/` and user `AppData/RagnaController/Profiles/`. Debounced saves (500ms). Safe file write via `.tmp` + `File.Move`.

### 5.3 ProfileTemplate (`Profiles/ProfileTemplate.cs`)
Template for First-Run Wizard. Contains PreRenewal / Renewal engine settings, ButtonMappings (`Dictionary<string, VirtualKey>`), Sensitivity.

### 5.4 ProfileTemplateManager (`Profiles/ProfileTemplateManager.cs`)
Loads templates from `DefaultProfiles/*.json`, converts `Profile` → `ProfileTemplate`. Creates `Profile` from template with given name.

---

## 6. PROJECT FILESYSTEM (STRUCTURE-001)

```
PROJECT_ROOT/
├── .workspace_temp/     (temp_builds/, scratchpad/, benchmarks/, logs/)
├── scripts/             (Python/PowerShell helper scripts)
├── src/RagnaController/
│   ├── Views/           (Windows/, Dialogs/, Overlays/, Editors/, Controls/)
│   ├── Controller/      (SDL2, XInput, Raw HID)
│   ├── Core/            (Engines/, Input/, Windows/, Services/, Interfaces/, Utilities/)
│   ├── Models/          (ParsedInput, VirtualKey, Settings, ...)
│   ├── Profiles/        (Profile, ProfileManager, ProfileTemplate, ...)
│   ├── Locales/         (en.json, de.json, +38 more — i18n JSON files)
│   ├── Assets/          (Classes/, Emotes/, icon.ico, startup_voice.mp3)
│   ├── DefaultProfiles/ (58 RO class templates)
│   └── RagnaController.csproj
├── tests/RagnaController.Tests/
├── release_final/       (only end products — binaries, ZIP, manual, license)
├── SOUL.md | WORKFLOW.md | ROADMAP.md | KANBAN.md | SESSION_STATE.md
├── ARCHITECTURE.md | README.md | CHANGELOG.md
└── .hermes/             (Hermes Agent configuration)
```

**Release isolation (RULE-001):** `release_final/` contains ONLY end products. No `.obj`, `.pdb`, `.tmp`, `.log`, `.cache`, `.debug`, source code, or test files.

---

## 7. VERSIONING & RELEASES

- **SemVer:** `MAJOR.MINOR.PATCH`
- **Build:** `dotnet build` — 0 errors, 0 warnings required
- **Tests:** `dotnet test` — 100% passing required
- **QA Gate:** QA Engineer (ROLE-004) has RELEASE BLOCK AUTHORITY
- **CHANGELOG:** Entries grouped by `Added`, `Changed`, `Fixed`, `Security`

---

## 8. TEAM ROLES (SOUL.md)

| Role | Agent | Authority |
|------|-------|-----------|
| ROLE-001 | Architect / PO | Team lead, architecture, roadmap, task-splitting. Approves architecture changes. |
| ROLE-002 | Frontend UI/UX | UI, layout, UX, animations, thread-safe UI. |
| ROLE-003 | Backend Core | Data models, algorithms, memory management, core logic. |
| ROLE-004 | QA Engineer | **RELEASE BLOCK AUTHORITY.** Can stop releases at any time. |
| ROLE-005 | DevOps Release | Build, packaging, deployment, workspace cleanup. |

---

## 9. KEY FILES

| File | Purpose |
|------|---------|
| `SOUL.md` | Core governance rules (L2 in rule hierarchy) |
| `WORKFLOW.md` | Phase model, handover format, task format |
| `ROADMAP.md` | Epics, tasks, status tracking |
| `KANBAN.md` | Kanban board with task matrix |
| `SESSION_STATE.md` | Session continuity state |
| `ARCHITECTURE.md` | This document |
| `RagnaController.csproj` | Project file with explicit `<Compile Include>` (EnableDefaultCompileItems=false) |
| `AppJsonContext.cs` | System.Text.Json source generation context |

---

## 10. DEPENDENCIES

| Package | Purpose |
|---------|---------|
| `Hexa.NET.SDL2` | Native gamepad support (Xbox, PlayStation) |
| `System.Text.Json` | JSON serialization with source generation |
| `Stryker.NET` | Mutation testing |
| `xUnit` | Unit testing framework |

---

*This document is maintained by the Architect Agent (ROLE-001). Architecture changes require a documented request in `docs/DECISIONS.md` and approval by ROLE-001.*