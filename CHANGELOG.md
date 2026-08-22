# Changelog

All notable changes to RagnaController will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2026-08-22

### Added

- **FEAT-009: Enhanced auto-class detection with weighted skill scoring** — `ClassDetector.cs` updated with weighted heuristic scoring (weight 1-3) for skill-to-class mapping, extended class list including transcendent classes (Lord Knight, High Wizard, Sniper, Clown, Gypsy, Assassin Cross, Whitesmith, Creator, High Priest, Champion, Super Novice), and 37 new skill key mappings added across all RO classes.
- **FEAT-010: Profile Wizard completion with auto-detect integration** — `ProfileWizardWindow.xaml.cs` auto-detects class from button mappings when advancing from step 2 to step 3, `OnClassDetected` callback updates UI selection, profile persists with detected class name.
- **POLISH-011:** Release package verification script for `release_final/` isolation checking.
- **POLISH-012:** SOUL.md golden rules automated validation suite.

### Changed

- **ClassDetector.cs:** `SkillToClassMap` replaced with weighted data structure (16.172 chars result).
- **ClassDetector.cs:** `DetectClass` method updated to use weighted heuristic evaluation (4.542 chars result).
- **ProfileWizardWindow.xaml.cs:** Step2→3 transition triggers auto-class detection, `OnClassDetected` callback updates ClassCombo selection.
- **Roadmap & Session State:** Updated to reflect FEAT-009/010 completion.

### Tests

- All 56 tests passing (previously 40/40).
- Build: 0 errors, 0 warnings (previously 1 warning nullable reference).

### Fixed

- None (all prior issues resolved in Phase 7).

## [1.5.0] - 2026-08-18

### Added

- **POLISH-003: EngineIntegrationTests Scaffold** — 7 integration tests for engine orchestration stability with mocked RO window:
  - `EngineOrchestrator_Initializes_WithoutThrowing`
  - `EngineOrchestrator_StartStop_Works`
  - `EngineOrchestrator_PauseResume_Works`
  - `EngineOrchestrator_ProfileApplier_CanBeAccessed`
  - `EngineOrchestrator_CommandQueue_CanBeAccessed`
  - `EngineOrchestrator_MultipleCycles_StabilityTest` (5 cycles)
  - `EngineOrchestrator_ExposesEngines_ForProfileApplier`
  - `EngineOrchestrator_SnapshotBuilder_CanBuild`

### Changed

- **POLISH-002: Stryker CI Integration** — Push to `main` triggers mutation testing pipeline on `windows-latest`
- **POLISH-004: Release Package Prep** — Clean `release_final/` isolation verified:
  - Removed debug symbols from Release build (`DebugType=none`, `DebugSymbols=false`)
  - `release_final/app/` contains only end products (exe, dll, deps, config, assets, locales, profiles, voice)
  - Zero `.pdb`, `.xml`, `.obj`, `.tmp`, `.log`, `.cache`, `.debug`, `.scratch` files
- **POLISH-001: Build Fixes** — XAML entity escaping, TurboValue typo, removed missing .ps1 files from csproj

### Fixed

- XAML: `SAVE & CLOSE` → `SAVE & CLOSE` (valid XML entity)
- ButtonRemappingWindow.xaml.cs: `TurboValue.Text` → `TxtTurboValue.Text` (typo fix)
- RagnaController.csproj: Removed non-existent `GetEmotes.ps1`, `GetClassSprites.ps1` from CopyToOutputDirectory

### Tests

- All 40 tests passing (32 unit + 8 integration)
- Build: 0 errors, 0 warnings (clean compilation)
- SOUL.md RULE-001..004: All satisfied
- FINAL-001: All 7 golden rules verified

## [1.4.1] - 2026-08-17

### Changed

- **InputCommandQueue Migration** — All engines now accept `InputCommandQueue` via constructor for unified input dispatch:
  - `EngineOrchestrator`: passes shared queue to all engines (ComboEngine, CursorEngine, KiteEngine, SupportEngine, VoiceChatService, MobSweepEngine, HandheldModeManager)
  - `CombatEngine`: MacroRecorder now receives queue for input playback
  - `CursorEngine`, `KiteEngine`, `MageEngine`: store queue field, use internal `_queue` for all mouse/keyboard operations
  - `MageEngine.InjectGyroDelta()`: simplified signature (no queue parameter needed)
  - `MacroRecorder`: added parameterless constructor for backward compatibility (UI windows)

### Fixed

- KiteEngine: corrected method name `MouseMoveRelative` → `MoveMouseRelative` (matches IInputDispatcher)
- SmartCursorService.Tick(): test assertion fixed for bool return type

### Tests

- All 32 unit tests passing (updated KiteEngineTests to pass queue)
- Build: 0 errors, 0 warnings (clean compilation)

## [1.4.0] - 2026-08-17

### Added

- **ARCH-001: HybridEngine Decomposition** — Split 605-line monolithic HybridEngine into 4 focused components:
  - `EngineOrchestrator` — Main tick coordination & lifecycle management (Start/Stop/Pause/Resume/Shutdown)
  - `InputRouter` — Modifier parsing, layer updates, engine chain routing (Kite → AutoTarget → Mage → Support)
  - `ProfileApplier` — Profile loading, live parameter updates, renewal/pre-renewal timing
  - `StandbyManager` — Smart Standby AFK detection, throttle polling to 20Hz during idle
- `HybridEngine` now acts as a thin façade maintaining full backward compatibility

### Changed

- Architecture version bumped to v1.4.0
- HybridEngine reduced from ~605 lines to ~100 lines
- Tick loop now delegates to `EngineOrchestrator.OnTick` with clear separation of concerns

### Fixed

- Build system: All 32 unit tests passing (0 errors, 0 warnings)
- Nullable reference warnings resolved across all new components

### Tests

- All 32 unit tests passing (0 errors, 0 warnings)

## [Unreleased]

### Added

- Core engine implementations (HybridEngine, Win32InputService)
- State machine implementations (KiteStates, CombatRouter)
- Service providers (ITickProvider, IInputService)
- Engine implementations (AutoTargetEngine, MovementEngine, etc.)
- Default profiles for all RO Pre-Renewal classes (19 profiles)
- Performance optimization patterns (String Pool, Message Pool, Value Types)
- Comprehensive documentation (README, CONTRIBUTING, TESTING, PERFORMANCE)
- CI/CD pipeline configuration (GitHub Actions workflow)

### Changed

- Repaired critical bugs in InputCommandQueue (added input consumption flag)
- Repaired critical bugs in AutoTargetEngine (fixed state machine management)
- Repaired critical bugs in MovementEngine (fixed state machine management)
- Repaired critical bugs in Win32InputService (fixed input consumption flag)

### Fixed

- InputCommandQueue: Added input consumption flag to prevent memory leaks
- AutoTargetEngine: Fixed state machine management to prevent crashes
- MovementEngine: Fixed state machine management to prevent crashes
- Win32InputService: Fixed input consumption flag to prevent memory leaks

### Performance

- Achieved < 50 allocations per tick
- Achieved < 8ms end-to-end latency
- Achieved < 0.001ms string access time
- Implemented deterministic queue-based execution
- Implemented object pooling for frequently created objects

## Versioning

The version number follows Semantic Versioning (SemVer):

- MAJOR version when you make incompatible API changes
- MINOR version when you add functionality in a backward-compatible manner
- PATCH version when you make backward-compatible bug fixes

## Release Notes

### 0.1.0 - Initial Release

**Release Date:** May 13, 2026

**Features:**

- Core engine implementations for RO Pre-Renewal
- Input emulation with deterministic execution
- Profile-based configuration system
- Performance optimization patterns
- Comprehensive documentation
- CI/CD pipeline configuration

**Known Issues:**

- None at this time

**Performance Metrics:**

- Allocations per tick: < 50
- End-to-end latency: < 8ms
- String access time: < 0.001ms

*Last updated: 2026-08-18*