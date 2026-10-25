# RagnaController - Architecture & Folder Structure Analysis

**Date:** 2026-10-24  
**Version:** 2.6.0  
**Total Source Files:** ~323 `.cs` files  
**Test Files:** ~25 test files (1076 tests passing)

---

## 📊 Current Structure Overview

```
src/
├── RagnaController/                    # Main WPF Application
│   ├── App.xaml / App.xaml.cs
│   ├── check_sdl.cs                    # ⚠️ Root level - misplaced
│   ├── ControllerPreview.cs            # ⚠️ Root level - misplaced
│   ├── Converters.cs                   # ⚠️ Root level - misplaced
│   ├── LocStrings.Designer.cs          # ⚠️ Root level - misplaced
│   │
│   ├── Controller/                     # Hardware Abstraction (6 files)
│   │   ├── ButtonState.cs
│   │   ├── ControllerSnapshot.cs
│   │   ├── DualSenseHardwareService.cs
│   │   ├── SnapshotBuilder.cs
│   │   ├── XInputFallbackService.cs
│   │   └── README.md
│   │
│   ├── ControllerTest/                 # Test/Diagnostic Tools (3 files)
│   │
│   ├── Controls/                       # Reusable WPF Controls (2 files)
│   │
│   ├── Core/                           # ⚠️ MAIN ISSUE: 40+ files flat + subfolders
│   │   ├── *.cs (40+ files directly in Core/)
│   │   ├── Benchmarks/
│   │   ├── Engines/ (12 engine implementations)
│   │   ├── Input/ (14 input-related files)
│   │   ├── Interfaces/ (12 interfaces)
│   │   ├── Services/ (70+ services - TOO MANY)
│   │   ├── Utilities/ (20+ utilities)
│   │   │   └── Coordinators/ (5 coordinators)
│   │   └── Windows/ (6 window tracking)
│   │
│   ├── Models/                         # Domain Models (13 files)
│   │
│   ├── Profiles/                       # Profile System (12 files)
│   │
│   ├── Properties/                     # AssemblyInfo etc.
│   │
│   ├── Resources/                      # Embedded resources
│   │
│   ├── Services/                       # ⚠️ DUPLICATE: Only 3 files (ProfileService, ToastService)
│   │
│   └── Views/                          # WPF Views (well organized)
│       ├── Controls/
│       ├── Dialogs/
│       ├── Editors/
│       ├── Overlays/
│       └── Windows/
│
├── RagnaController.Benchmarks/         # Benchmark project
├── RagnaController.Tests/              # Test project
└── DefaultProfiles/                    # JSON profile templates
```

---

## 🚨 Critical Issues

### 1. **Duplicate Services Folders** (HIGH PRIORITY)
| Folder | Files | Issue |
|--------|-------|-------|
| `Core/Services/` | 70+ | Main services location |
| `Services/` | 3 | Duplicate, confusing, incomplete |

**Impact:** Developers don't know where to look/put services. `ProfileService.cs` in root `Services/` vs `ProfileManager.cs` in `Profiles/`.

### 2. **Core/ Root Folder Overflow** (HIGH PRIORITY)
**40+ files directly in `Core/`** - violates single responsibility, hard to navigate:
- Domain services: `AdvancedInputService`, `AutoBuildOptimizer`, `ChatMacroService`, `ShopNavigationService`, etc.
- Database services: `ItemDatabaseService`, `MapDatabaseService`, `MonsterDatabaseService` (also in Services!)
- Hardware: `MotionAimAssist`, `SteamInputIntegration`, `StickDriftCompensation`
- Models mixed in: `PartyMember`, `ProfileRating`, `RumblePattern`

### 3. **Core/Services/ God Folder** (HIGH PRIORITY)
**70+ services in one folder** - no categorization:
- Hardware: `GyroService`, `DualSenseLightbarService`, `HapticFeedbackService`
- Database: `MonsterDatabaseService`, `MonsterDataLoader`
- UI: `GamepadNavigationService`, `GamepadUiNavigator`, `HighContrastService`
- System: `AutoStartManager`, `AutoUpdateService`, `CICDService`, `LinuxSupportService`
- Gameplay: `BuffManager`, `CooldownManager`, `PartyManager`, `RadialMenuService`
- Overlay: `OverlayRouter`, `RoUiMenuService`
- Streaming: `ObsWebSocketService`, `StreamDeckService`, `ViewerInteractionService`
- Audio: `TtsAnnouncerService`, `VoiceChatService`
- Monitoring: `FrameBudgetMonitor`, `MemoryAllocationTracker`, `PowerMonitor`, `SystemMonitor`, `TelemetryService`

### 4. **Mixed Layer Responsibilities**
| File | Current Location | Should Be |
|------|-----------------|-----------|
| `MainViewModel.cs` | `Core/Utilities/` | `Presentation/ViewModels/` |
| `ProfileManager.cs` | `Profiles/` | `Application/Profiles/` |
| `MonsterDatabaseService.cs` | `Core/Services/` | `Infrastructure/Data/` |
| `GyroService.cs` | `Core/Services/` | `Infrastructure/Hardware/` |
| `CombatEngine.cs` | `Core/Engines/` | `Domain/Engines/` |

### 5. **God Classes (Large Files > 20KB)**
| File | Size | Lines | Issue |
|------|------|-------|-------|
| `EngineOrchestrator.cs` | 65 KB | ~2000+ | Central god class, too many responsibilities |
| `ProfileManager.cs` | 31 KB | ~900+ | Profile CRUD, migration, validation, collections, cloud sync |
| `Settings.cs` | 23 KB | ~700+ | 100+ properties, all settings in one class |
| `NativeMethodsImpl.cs` | 24 KB | ~700+ | All P/Invoke in one file |
| `MobDatabase.cs` | 57 KB | ~1800+ | Hardcoded monster data + logic |
| `ControllerSnapshot.cs` | 4 KB | ~120 | OK |
| `SnapshotBuilder.cs` | 9 KB | ~280 | OK |

### 6. **Inconsistent Folder Naming**
- `Controller/` (singular) vs `Engines/` (plural) vs `Services/` (plural)
- `Core/Windows/` vs `Views/Windows/`
- `Core/Input/` vs `Core/Services/` (input services)

### 7. **Root-Level Misplaced Files**
- `check_sdl.cs`, `ControllerPreview.cs`, `Converters.cs`, `LocStrings.Designer.cs` - should be in appropriate subfolders

---

## 🎯 Proposed Target Architecture (Clean Architecture / Vertical Slice)

```
src/
├── RagnaController/                          # WPF Application Entry Point
│   ├── App.xaml / App.xaml.cs
│   ├── CompositionRoot.cs                    # Dependency Injection Setup
│   └── Program.cs                            # Entry point (if .NET 6+ style)
│
├── RagnaController.Domain/                   # 🏛️ DOMAIN LAYER (Pure C#, no WPF)
│   ├── Entities/
│   │   ├── Profile.cs
│   │   ├── ProfileCollection.cs
│   │   ├── CharacterProfile.cs
│   │   ├── PartyMember.cs
│   │   ├── GuildMember.cs
│   │   └── ProfileRating.cs
│   │
│   ├── ValueObjects/
│   │   ├── ButtonKey.cs
│   │   ├── ButtonAction.cs
│   │   ├── ChordConfig.cs
│   │   ├── TriggerDeadzoneCurve.cs
│   │   ├── StickDriftCompensation.cs
│   │   └── RumblePattern.cs
│   │
│   ├── Engines/                              # Gameplay Logic (Pure)
│   │   ├── IEngine.cs
│   │   ├── CombatEngine.cs
│   │   ├── MovementEngine.cs
│   │   ├── KiteEngine.cs
│   │   ├── MageEngine.cs
│   │   ├── GroundSpellEngine.cs
│   │   ├── MobSweepEngine.cs
│   │   ├── ItemManagerEngine.cs
│   │   ├── SkillOrchestrator.cs
│   │   ├── AutoTargetEngine.cs
│   │   ├── ComboEngine.cs
│   │   ├── CursorEngine.cs
│   │   └── SmartCursorEngine.cs
│   │
│   ├── Calculators/                          # Game Calculations
│   │   ├── StatCalculator.cs
│   │   ├── ExperienceCalculator.cs
│   │   └── SkillSimulator.cs
│   │
│   ├── Recommendations/
│   │   ├── SkillRecommendationEngine.cs
│   │   └── AutoBuildOptimizer.cs
│   │
│   ├── Navigation/
│   │   ├── ShopNavigationService.cs
│   │   ├── StorageOrganizationService.cs
│   │   ├── SkillTreeNavigationService.cs
│   │   └── ChatMacroService.cs
│   │
│   ├── Input/
│   │   ├── AdvancedInputService.cs
│   │   ├── InputChain.cs
│   │   └── GestureRecognizer.cs
│   │
│   ├── Events/                               # Domain Events
│   └── Exceptions/
│
├── RagnaController.Application/              # 📦 APPLICATION LAYER
│   ├── Profiles/
│   │   ├── ProfileManager.cs
│   │   ├── ProfileService.cs                 # Merged from root Services/
│   │   ├── ProfileApplier.cs
│   │   ├── ProfileValidator.cs
│   │   ├── ProfileMigration.cs
│   │   ├── ProfileTemplateManager.cs
│   │   ├── ProfilePresetManager.cs
│   │   └── ProfileShareService.cs
│   │
│   ├── Database/
│   │   ├── IMonsterRepository.cs
│   │   ├── IItemRepository.cs
│   │   ├── ISkillRepository.cs
│   │   ├── IMapRepository.cs
│   │   ├── MonsterDatabaseService.cs
│   │   ├── MonsterDataLoader.cs
│   │   ├── ItemDatabaseService.cs
│   │   ├── SkillDatabaseService.cs
│   │   └── MapDatabaseService.cs
│   │
│   ├── Community/
│   │   ├── PartyManager.cs
│   │   ├── PartyMember.cs (moved from Domain)
│   │   ├── GuildManager.cs
│   │   └── CommunityEntry.cs
│   │
│   ├── Cloud/
│   │   ├── CloudSyncService.cs
│   │   └── ProfileShareService.cs
│   │
│   ├── Streaming/
│   │   ├── StreamDeckService.cs
│   │   ├── ObsWebSocketService.cs
│   │   ├── ViewerInteractionService.cs
│   │   ├── DonationAlertsService.cs
│   │   └── StreamerModeService.cs
│   │
│   ├── Developer/
│   │   ├── AutomatedTestRecorder.cs
│   │   ├── LuaScriptingService.cs
│   │   ├── InputLatencyVisualizer.cs
│   │   ├── ReplaySystem.cs
│   │   └── DebugOverlayService.cs
│   │
│   └── Settings/
│       ├── SettingsService.cs                # Extracted from Settings.cs
│       ├── Settings.cs (DTO only)
│       └── SettingsValidator.cs
│
├── RagnaController.Infrastructure/           # 🔧 INFRASTRUCTURE LAYER
│   ├── Hardware/
│   │   ├── IControllerProvider.cs
│   │   ├── ControllerSnapshot.cs
│   │   ├── SnapshotBuilder.cs
│   │   ├── ButtonState.cs
│   │   ├── GyroService.cs
│   │   ├── DualSenseHardwareService.cs
│   │   ├── XInputFallbackService.cs
│   │   ├── SteamInputIntegration.cs
│   │   ├── HidDeviceUtils.cs
│   │   └── MotionAimAssist.cs
│   │
│   ├── Input/
│   │   ├── IInputHandler.cs
│   │   ├── IKeyboardEmulationStrategy.cs
│   │   ├── IMouseEmulationStrategy.cs
│   │   ├── InputCommandQueue.cs
│   │   ├── InputFailoverManager.cs
│   │   ├── InputLatencyTracker.cs
│   │   ├── InputReader.cs
│   │   ├── InputRouter.cs
│   │   ├── InterceptionKeyboardStrategy.cs
│   │   ├── JitterService.cs
│   │   ├── KernelInputService.cs
│   │   ├── SendInputKeyboardStrategy.cs
│   │   ├── SendInputMouseStrategy.cs
│   │   ├── BezierMouseService.cs
│   │   └── MultiClientInputRouter.cs
│   │
│   ├── Persistence/
│   │   ├── IFileSystem.cs
│   │   ├── JsonProfileRepository.cs
│   │   ├── JsonSettingsRepository.cs
│   │   └── ProfileCollectionRepository.cs
│   │
│   ├── Platform/
│   │   ├── NativeMethods.cs
│   │   ├── NativeMethodsImpl.cs
│   │   ├── NativeStructs.cs
│   │   ├── ProcessNameResolver.cs
│   │   ├── DeviceNotificationWindow.cs
│   │   ├── WindowTracker.cs
│   │   ├── MultiClientWindowTracker.cs
│   │   └── WindowSwitcher.cs
│   │
│   ├── System/
│   │   ├── AutoStartManager.cs
│   │   ├── AutoUpdateService.cs
│   │   ├── PortableModeService.cs
│   │   ├── CICDService.cs
│   │   ├── LinuxSupportService.cs
│   │   ├── SteamDeckService.cs
│   │   ├── PowerModeService.cs
│   │   ├── SleepModeService.cs
│   │   ├── StandbyManager.cs
│   │   ├── BackgroundTickProvider.cs
│   │   ├── WpfTickProvider.cs
│   │   └── ITickProvider.cs
│   │
│   ├── Monitoring/
│   │   ├── FrameBudgetMonitor.cs
│   │   ├── PerformanceProfiler.cs
│   │   ├── MemoryAllocationTracker.cs
│   │   ├── EngineWatchdog.cs
│   │   ├── SystemMonitor.cs
│   │   ├── TelemetryService.cs
│   │   ├── ETWProvider.cs
│   │   ├── RagnaControllerETW.cs
│   │   └── GpuOverlayProfiler.cs
│   │
│   ├── Memory/
│   │   ├── ObjectPools.cs
│   │   ├── Pools.cs
│   │   ├── EngineOptimizationPool.cs
│   │   └── ControllerSnapshotPool.cs
│   │
│   ├── Network/
│   │   ├── DiscordRpcService.cs
│   │   ├── ObsWebSocketService.cs
│   │   └── UpdateChecker.cs
│   │
│   ├── Audio/
│   │   ├── HapticFeedbackService.cs
│   │   ├── TtsAnnouncerService.cs
│   │   └── VoiceChatService.cs
│   │
│   ├── OCR/
│   │   └── OcrService.cs
│   │
│   └── Localization/
│       └── LocalizationManager.cs
│
├── RagnaController.Presentation/             # 🎨 PRESENTATION LAYER (WPF)
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   ├── SettingsViewModel.cs
│   │   ├── ProfileViewModel.cs
│   │   ├── EngineViewModel.cs
│   │   └── OverlayViewModel.cs
│   │
│   ├── Views/
│   │   ├── Windows/
│   │   │   ├── MainWindow.xaml
│   │   │   ├── SettingsWindow.xaml
│   │   │   ├── SplashWindow.xaml
│   │   │   ├── UpdateWindow.xaml
│   │   │   ├── MonsterImportWindow.xaml
│   │   │   └── ControllerTestWindow.xaml
│   │   │
│   │   ├── Dialogs/
│   │   │   ├── ProfileWizardWindow.xaml
│   │   │   ├── ProfileLibraryWindow.xaml
│   │   │   ├── ProfileValidationWindow.xaml
│   │   │   ├── ProfileDiffWindow.xaml
│   │   │   ├── ProfileCollectionsWindow.xaml
│   │   │   ├── SelectProfilesDialog.xaml
│   │   │   ├── GyroCalibrationWindow.xaml
│   │   │   ├── WizardWindow.xaml
│   │   │   ├── TutorialWindow.xaml
│   │   │   └── RumbleEditorWindow.xaml
│   │   │
│   │   ├── Editors/
│   │   │   ├── RadialMenuWindow.xaml
│   │   │   └── RadialSetupWindow.xaml
│   │   │
│   │   ├── Overlays/
│   │   │   └── InGameOverlayWindow.xaml
│   │   │
│   │   └── Controls/
│   │       ├── ControllerVisualization.xaml
│   │       └── TelemetryPanel.xaml
│   │
│   ├── Converters/
│   │   └── Converters.cs                     # Moved from root
│   │
│   ├── Services/
│   │   ├── ToastService.cs                   # Moved from root Services/
│   │   ├── FeedbackRumbleProvider.cs
│   │   ├── GamepadNavigationService.cs
│   │   ├── GamepadUiNavigator.cs
│   │   ├── HighContrastService.cs
│   │   ├── ColorBlindService.cs
│   │   ├── ReducedMotionService.cs
│   │   ├── AccessibilityService.cs
│   │   ├── OverlayRouter.cs
│   │   └── RoUiMenuService.cs
│   │
│   └── Resources/
│       ├── Styles/
│       ├── Themes/
│       └── Icons/
│
├── RagnaController.CrossCutting/             # ✂️ CROSS-CUTTING CONCERNS
│   ├── Logging/
│   │   └── AdvancedLogger.cs
│   │
│   ├── Configuration/
│   │   ├── ConfigWatcher.cs
│   │   └── AppVersion.cs
│   │
│   ├── Validation/
│   │   └── ShareCodeValidator.cs
│   │
│   └── Extensions/
│       └── StringExtensions.cs (if any)
│
├── RagnaController.Tests/                    # Unit & Integration Tests
│
└── RagnaController.Benchmarks/               # Performance Benchmarks
```

---

## 📋 Migration Plan (Phased Approach)

### Phase 1: Quick Wins (No Breaking Changes)
1. **Merge duplicate `Services/` folders** - Move `ProfileService.cs`, `ToastService.cs` to appropriate locations
2. **Move root-level files** to proper subfolders (`check_sdl.cs` → `Infrastructure/Platform/`, `Converters.cs` → `Presentation/Converters/`)
3. **Extract `Settings.cs`** into `SettingsService.cs` + `Settings.cs` (DTO)

### Phase 2: Folder Reorganization (Low Risk)
1. **Create `Domain/`, `Application/`, `Infrastructure/`, `Presentation/` folders** alongside `Core/`
2. **Move files incrementally** - one category at a time, update namespaces
3. **Keep `Core/` as alias** during transition (using `extern alias` or gradual namespace migration)

### Phase 3: Service Decomposition (Medium Risk)
1. **Split `EngineOrchestrator`** into:
   - `EngineLifecycleManager` (start/stop/dispose)
   - `EngineCoordinator` (tick orchestration)
   - `EngineStateAggregator` (combat state, snapshots)
2. **Split `ProfileManager`** into:
   - `ProfileRepository` (persistence)
   - `ProfileCollectionService` (collections)
   - `ProfileMigrationService` (versioning)
   - `ProfileValidationService` (validation)
   - `ProfileCloudSyncService` (cloud)
3. **Split `Settings.cs`** into feature-specific settings classes

### Phase 4: Interface Segregation & DI (Medium Risk)
1. **Extract interfaces** for all services
2. **Setup proper DI container** (Microsoft.Extensions.DependencyInjection)
3. **Remove static/service locator patterns**

### Phase 5: Cleanup (Low Risk)
1. **Remove old `Core/` folder** after full migration
2. **Update csproj** with new folder structure
3. **Update documentation**

---

## 📦 Immediate Action Items (Can Do Today)

### 1. Fix Duplicate Services
```bash
# Move ProfileService.cs → Application/Profiles/
# Move ToastService.cs → Presentation/Services/
# Delete empty Services/ folder
```

### 2. Organize Core/ Root Files
```bash
# Create subfolders in Core/ and move:
Core/Database/          → ItemDatabaseService, MapDatabaseService, MonsterDatabaseService
Core/Hardware/          → MotionAimAssist, SteamInputIntegration, StickDriftCompensation
Core/Navigation/        → ShopNavigationService, StorageOrganizationService, SkillTreeNavigationService, ChatMacroService
Core/Community/         → PartyMember, ProfileRating, GuildMember (or move to Application)
Core/Developer/         → AutomatedTestRecorder, LuaScriptingService, InputLatencyVisualizer, ReplaySystem, DebugOverlayService
Core/Input/             → AdvancedInputService, TriggerDeadzoneCurve, RumblePattern
```

### 3. Organize Core/Services/ (70+ → Categorized)
```bash
Core/Services/Hardware/         → GyroService, DualSenseLightbarService, HapticFeedbackService
Core/Services/Database/         → MonsterDatabaseService, MonsterDataLoader
Core/Services/UI/               → GamepadNavigationService, GamepadUiNavigator, HighContrastService, ColorBlindService, ReducedMotionService, AccessibilityService
Core/Services/System/           → AutoStartManager, AutoUpdateService, CICDService, LinuxSupportService, SteamDeckService, PortableModeService
Core/Services/Gameplay/         → BuffManager, CooldownManager, PartyManager, RadialMenuService, SmartCursorService
Core/Services/Overlay/          → OverlayRouter, RoUiMenuService
Core/Services/Streaming/        → ObsWebSocketService, StreamDeckService, ViewerInteractionService
Core/Services/Audio/            → TtsAnnouncerService, VoiceChatService
Core/Services/Monitoring/       → FrameBudgetMonitor, MemoryAllocationTracker, PowerMonitor, SystemMonitor, TelemetryService, ETWProvider, RagnaControllerETW, GpuOverlayProfiler
Core/Services/Input/            → FeedbackRumbleProvider, ChordInputService, LongPressTracker
Core/Services/Engine/           → EngineInstance, MultiClientEngineOrchestrator
Core/Services/Profile/          → ProfileApplier, ProfileShareService, ProfileValidator, DefaultProfileLoader
Core/Services/Tick/             → BackgroundTickProvider, WpfTickProvider
Core/Services/Battery/          → BatteryOptimizationService
Core/Services/Config/           → ConfigWatcher
Core/Services/FirstRun/         → FirstRunService
Core/Services/Handheld/         → HandheldDetector, HandheldModeManager
Core/Services/Monitor/          → MonitorHelper
Core/Services/Power/            → PowerModeService, SleepModeService, StandbyManager
Core/Services/Update/           → UpdateChecker, UpdateService
```

---

## 🔍 Code Quality Issues to Address

### 1. **Static Dependencies** (Hard to Test)
- `NativeMethods` static class used everywhere
- `Settings.Instance` singleton pattern
- `ProfileManager.Instance` singleton pattern
- `EngineOrchestrator.Instance` singleton pattern

**Fix:** Inject `INativeMethods`, `ISettings`, `IProfileManager`, `IEngineOrchestrator`

### 2. **Missing Nullable Annotations**
- Many files lack `#nullable enable` or proper `?` annotations
- `TreatWarningsAsErrors=true` but nullable warnings may be suppressed

### 3. **Large Methods** (> 50 lines)
- `EngineOrchestrator.Tick()` - likely 200+ lines
- `ProfileManager.LoadProfiles()` - 100+ lines
- `SnapshotBuilder.Build()` - 100+ lines

### 4. **Primitive Obsession**
- `float leftX, leftY, rightX, rightY` passed everywhere instead of `Vector2` or `StickState`
- `string` for profile names, class names instead of strong types

### 5. **Missing Validation**
- Public methods without input validation
- No `ArgumentNullException` throws in constructors

---

## 📈 Metrics After Refactoring (Target)

| Metric | Current | Target |
|--------|---------|--------|
| Files per folder (max) | 70 | 15 |
| Folder depth | 1-2 | 3-4 |
| Largest file | 65 KB | 15 KB |
| Classes per file | 1 (mostly) | 1 |
| Cyclomatic complexity (avg) | Unknown | < 10 |
| Coupling (afferent/efferent) | High | Low |
| Test coverage | 1076 tests | Maintain 100% |

---

## ⚠️ Risks & Mitigations

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Build breaks during move | High | High | Phase 2: Move one category at a time, commit after each |
| Namespace conflicts | Medium | High | Use `extern alias` during transition, update all references |
| DI container setup | Medium | Medium | Start with manual composition root, migrate to DI gradually |
| Test failures | Medium | High | Run tests after each phase, fix immediately |
| Git history loss | Low | Medium | Use `git mv` for renames, preserve history |

---

## ✅ Recommended First Steps (This Session)

1. **Create analysis document** (this file)
2. **Fix duplicate Services folder** - move 3 files, delete folder
3. **Move root-level misplaced files** to proper locations
4. **Create subfolders in Core/** and organize 40+ files
5. **Run build & tests** after each step
6. **Commit incremental changes**

---

## 📝 Notes

- **SOUL.md Compliance:** All changes must maintain 0 build errors, 1076 tests passing
- **White-Hat Principle:** No automation of gameplay, only assistance/UI
- **Encoding:** EUC-KR/ANSI for RO sprites (never UTF-8)
- **File Naming Immunity:** Never rename RO sprite files
- **German Language:** All UI output and code comments in German

This analysis provides a roadmap for transforming the current flat/overlapping structure into a clean, maintainable architecture following Clean Architecture principles with clear layer separation.