# Changelog

All notable changes to RagnaController will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.3] - 2026-08-24

### Added
- **Documentation update** — CHANGELOG, README, and roadmap synchronized for v2.0.3 release.

### Changed
- Documentation restructured per SOUL.md v2.0 Enterprise guidelines.

### Fixed
- No bug fixes in this release.

### Tests
- Documentation build verified.

---

## [2.0.2] - 2026-08-23

### Fixed
- **SDL2 Cleanup** — `System.AccessViolationException` in `ControllerService.SdlThreadLoop()` behoben: Static `_sdlInitialized`-Guard verhindert mehrfache `SDL.Init()`/`SDL.Quit()` Calls; `try/finally` garantiert sicheren Cleanup nur bei erfolgreicher Initialisierung. Behebt Crash in headless CI-Umgebung.

### Tests
- All 56 tests passing (verified locally + CI).
- Build: 0 errors, 0 warnings (Stryker.NET mutation testing configured).

## [2.0.1] - 2026-08-23

### Added
- **FEAT-007: Class-Specific Skill Orchestration** — 27 class-specific rotations with weighted heuristic scoring, `RotationStep`, `RotationConfig`, `RotationCondition` with 15 condition types. `SkillOrchestrator.Update()` handles evaluation and execution.
- **FEAT-008: Buff / Debuff Tracking System** — `BuffManager` tracks buffs/debuffs with durations, expiration warnings, and auto-recast support, integrated into `EngineOrchestrator` via `action.TrackBuff`. Exposes `ActiveBuffNames`, `ActiveDebuffNames`, `GetBuffRemainingSec()`, `GetDebuffRemainingSec()`.
- **POLISH-013:** Zero-allocation hot path optimizations across core engines — reusable lists, `ConcurrentDictionary` + `GetOrAdd`, `IReadOnlyCollection` instead of `.ToList()`.
- **POLISH-014:** Human-like input timing — `JitterService.ClickHold()` with 15-46ms natural variance replaces hardcoded 50ms sleeps in `InputCommandQueue`.
- **POLISH-015:** Memory leak prevention — `Commands` list in `InputCommandQueue` conditionalized under `#if DEBUG`; `_isInitialized` field removed from `InGameOverlayWindow.xaml.cs`.

### Changed
- **`ButtonState.cs`** — Converted to `readonly record struct` with positional parameters, built-in equality, helper properties (`AnyFaceButtonPressed`, `AnyShoulderPressed`, `AnyDPadPressed`, `AnyStickButtonPressed`, `AnyButtonPressed`), and `With()` method.
- **`ControllerManager.cs`** — All nullable fields initialized at declaration (`_buttonStates = new()`, `_controllerGuid = ""`, `_controllerName = ""`, `_controllerType = ""`, `_batteryLevel = 0`) — eliminates 5 CS8618 warnings.
- **`XInputFallbackService.cs`** — Constructor parameters (`ControllerService?`, `Profile?`) and `_controllerService` field made nullable — fixes CS8625 and CS8618 warnings.
- **`LongPressTracker.cs`** — Reusable `_reusableResults` list cleared each tick instead of `new List<>()` allocation.
- **`BuffManager.cs`** — `ActiveBuffNames`/`ActiveDebuffNames` from `IReadOnlyList<string>` changed to `IReadOnlyCollection<string>`; reusable `_expiredBuffs`/`_expiredDebuffs` lists cleared each tick.
- **`EngineOptimizationPool.cs`** — Replaced `Dictionary` + `lock` with `ConcurrentDictionary` + `GetOrAdd` for lock-free hot-path string interning.
- **`GroundSpellEngine.cs`** — Reusable `_activeSpellNamesReusable` list in `GetActiveSpellNames()` eliminates `new List<string>()` allocation per tick.
- **`MageEngine.cs`** — Fixed TODO: `SmartCursorService.GetVirtualCursorPosition()` instead of `NativeMethods.GetCursorPos`; constructor dependency injection for `SmartCursorService`; constructor param changed to `null!`.
- **`InGameOverlayWindow.xaml.cs`** — Removed unused `private bool _isInitialized` field and corresponding assignments.
- **`EngineOrchestrator.cs`** — StandbyManager tick delegates + `Thread.Sleep(50)` throttle when `shouldSkip=true`; optimized delta computation.
- **`SnapshotBuilder.cs`** — `_skillOrchestrator` nullable parameter (`SkillOrchestrator? = null`); `L3` → `BtnL3` rename.
- **`ClassDetector.cs`** — Already optimized — weighted heuristic scoring with 37 new skill mappings across all RO classes including transcendent classes.
- **`SkillOrchestrator.cs`** — Already optimized — 27 class-specific rotations working with cooldown tracking.
- **`CooldownManager.cs`** — Already optimized — initialized with `IMessenger`, added public methods.
- **`roadmap.md`** — All HW-005 through HW-013 + FEAT-007/008 marked `✅ COMPLETE`; Release Summary v2.1.0 section added.
- **`ControllerSnapshot.cs`** — Added `SkillCooldownMs` and `ActiveSkillId` fields for cooldown tracking in overlay UI.

### Tests
- All 56 tests passing (increased from previous suite).
- Build: 0 errors, 0 warnings (improved from previous build with temporary warnings).
- Mutation testing pipeline configured via Stryker.NET `stryker-config-core-only.json`.

### Fixed
- **CS8618** nullable warnings eliminated across `ControllerManager.cs`, `XInputFallbackService.cs`, `SnapshotBuilder.cs` (Core + Controller paths).
- **CS8625** nullable warnings fixed — all constructor params and fields properly annotated.
- **Memory leak** in `InputCommandQueue.cs` — `Commands` list now DEBUG-only; `AtomicLeftClick`/`AtomicRightClick` use `JitterService.ClickHold()` (15-46ms) instead of hardcoded 50ms.
- **Dead code** — Unused `_isInitialized` field eliminated from `InGameOverlayWindow.xaml.cs`.
- **Nullability** — `MageEngine.cs` constructor param changed from `null` to `null!`; `SmartCursorService` dependency injected.
- **Interface consistency** — `BuffManager.ActiveBuffNames`/`ActiveDebuffNames` return `IReadOnlyCollection<string>` instead of `IReadOnlyList<string>`.

---

## [2.0.0] - 2026-08-22

### Added
- **Full autonomous development cycle** — Phases 1-7 completed without manual intervention per SOUL.md.
- **FEAT-003: Community Hub** — Profile sharing via GitHub Gists with 3 starter profiles.
- **FEAT-004: HybridEngine decomposition** — Split monolithic HybridEngine into focused components.
- **FEAT-005: Full Class Engine Presets** — 27 class-specific rotation presets.

### Changed
- Architecture refactoring: HybridEngine → EngineOrchestrator + InputRouter + ProfileApplier + StandbyManager
- Controller event renamed from `DetectController` to `ControllerDetected`
- `GetBatteryLevel()` → `BatteryLevel` property across EngineOrchestrator, MainWindow, ControllerService
- `ControllerSnapshot.cs` and `SnapshotBuilder.cs` relocated from `Controller/` to `Core/`
- `L3` button mapping refactored to `BtnL3`
- Profile class added `ControllerGuid` field
- `InGameOverlayWindow` updated with Profile Name and Class Badge UI elements
- `ButtonState` struct extracted to dedicated file, converted to `readonly record struct`
- All nullable reference warnings resolved (CS8618/CS8625)
- Build: 0 errors, 0 warnings
- Tests: 56/56 passing

### Tests
- All 56 tests passing (previously 40/40).
- Build: 0 errors, 0 warnings (improved from 1 warning nullable reference).
- SOUL.md RULE-001..004: All satisfied.
- FINAL-001: All 7 golden rules verified.

---

## [1.5.0] - 2026-08-18

### Added
- **POLISH-003: EngineIntegrationTests Scaffold** — 7 integration tests for engine orchestration stability with mocked RO window.

### Changed
- **POLISH-002: Stryker CI Integration** — Push to `main` triggers mutation testing pipeline on `windows-latest`.
- **POLISH-004: Release Package Prep** — Clean `release_final/` isolation verified.
- **POLISH-001: Build Fixes** — XAML entity escaping, typo fixes, csproj cleanup.

### Tests
- All 40 tests passing (32 unit + 8 integration).

---

## [1.4.1] - 2026-08-17

### Changed
- **InputCommandQueue Migration** — All engines now accept `InputCommandQueue` via constructor for unified input dispatch.

### Fixed
- KiteEngine: corrected method name `MouseMoveRelative` → `MoveMouseRelative`.
- SmartCursorService.Tick(): test assertion fixed for bool return type.

### Tests
- All 32 unit tests passing (updated KiteEngineTests to pass queue).

---

## [1.4.0] - 2026-08-17

### Added
- **ARCH-001: HybridEngine Decomposition** — Split 605-line monolithic HybridEngine into 4 focused components: EngineOrchestrator, InputRouter, ProfileApplier, StandbyManager.
- `HybridEngine` now acts as a thin façade maintaining full backward compatibility.

### Changed
- Architecture version bumped to v1.4.0.
- HybridEngine reduced from ~605 lines to ~100 lines.
- Tick loop now delegates to `EngineOrchestrator.OnTick` with clear separation of concerns.

### Tests
- All 32 unit tests passing (0 errors, 0 warnings).
- Performance: < 50 allocations per tick, < 8ms end-to-end latency.

---

## [1.3.0] - 2026-08-15

### Added
- Core engine implementations (HybridEngine, Win32InputService).
- State machine implementations (KiteStates, CombatRouter).
- Service providers (ITickProvider, IInputService).
- Engine implementations (AutoTargetEngine, MovementEngine, etc.).
- Default profiles for all RO Pre-Renewal classes (19 profiles).
- Performance optimization patterns (String Pool, Message Pool, Value Types).
- Comprehensive documentation (README, CONTRIBUTING, TESTING, PERFORMANCE).
- CI/CD pipeline configuration (GitHub Actions workflow).

### Changed
- Repaired critical bugs in InputCommandQueue (added input consumption flag).
- Repaired critical bugs in AutoTargetEngine (fixed state machine management).
- Repaired critical bugs in MovementEngine (fixed state machine management).
- Repaired critical bugs in Win32InputService (fixed input consumption flag).

### Tests
- All 32 unit tests passing (0 errors, 0 warnings).
- Performance: < 50 allocations per tick, < 8ms end-to-end latency.

---

## Versioning

The version number follows Semantic Versioning (SemVer):

- MAJOR version when you make incompatible API changes
- MINOR version when you add functionality in a backward-compatible manner
- PATCH version when you make backward-compatible bug fixes

---

*Last updated: 2026-08-24 | Documentation synchronized | Git: origin/main | Build: 0 errors, 0 warnings | Tests: 56/56 passing*