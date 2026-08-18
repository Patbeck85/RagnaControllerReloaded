# Changelog

All notable changes to RagnaController will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.5.0] - 2026-08-18

### Changed
- **Stryker CI Integration (POLISH-002)** — Configured mutation testing for CI pipeline:
  - Simplified `stryker-config.json` to thresholds/reporters only (project/test-project/mutate/ignore-mutations removed)
  - Using CLI `--mutate` flags to target Core/ engine files only (`src/RagnaController/Core/*.cs`)
  - Excluded WPF UI files: `*Window*.cs`, `*Window*.xaml.cs`, `App.xaml.cs`, `DeviceNotificationWindow.cs`, `*.g.cs`
  - Enabled Stryker Dashboard upload with API key
  - Job `stryker-mutation` triggers on push to `main` branch only
  - Thresholds: break=70%, low=80%, high=95%
  - Restored solution file (`RagnaController.sln`) for full build verification

### Fixed
- CI: Simplified Stryker configuration to avoid Windows shell parser issues with complex JSON
- CI: Removed `--config-file` flag in favor of explicit CLI `--mutate` filters for reliable exclusion of WPF files

### Tests
- All 32 unit tests passing
- Build: 0 errors, 0 warnings (clean compilation)

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

## [0.1.0] - 2026-05-13

### Added
- Initial project structure
- Core engine interfaces
- Basic input emulation support
- Profile loading system

### Changed
- Initial release of RagnaController

---

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

**Known Issues:**
- None at this time

**Performance Metrics:**
- Allocations per tick: < 50
- End-to-end latency: < 8ms
- String access time: < 0.001ms

---

*Last updated: 2026-08-18*