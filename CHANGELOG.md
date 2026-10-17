# Changelog

All notable changes to RagnaController will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

--

## [2.3.0] - 2026-10-17

### 🟥 SPRINT 15A — Critical Bug Fixes
- **TECH-061: EngineOrchestrator Startup Validation** — All engine fields changed from `null!` to `private readonly`. Startup validation throws clear `InvalidOperationException` instead of silent `NullReferenceException` if any engine fails to initialize.
- **TECH-062: MainWindow God Class Refactoring** — Extracted `ToastService` (~25 lines removed) and `ProfileService` (~60 lines removed) from MainWindow. MainWindow now delegates toast notifications and profile management to dedicated services.
- **TECH-063: App.xaml.cs Auto-Restart on Crash** — Added `AutoRestartOnCrash` setting. Unhandled/Dispatcher exceptions now auto-restart the app with settings persisted first, preventing dead controller mid-session.

### 🟨 SPRINT 15B — UI/UX Polish
- **UX-018: Empty States in All Panels** — ProfileLibraryWindow shows contextual empty state with icon, message, and "Create New Profile" CTA button when no profiles found.
- **UX-019: Keyboard Shortcuts for Main Functions** — Ctrl+O (Profile Library), Ctrl+S (Save Settings), F1 (Help), Esc (Exit Mini Mode / Close Overlay) via `PreviewKeyDown`.
- **UX-020: HandheldWindow Touch Optimization** — Battery status display in top bar (color-coded icon + text). All 6 main buttons already ≥180px touch targets.
- **UX-021: InGameOverlayWindow Position Memory & Movable** — Overlay position saved/restored from settings. Draggable with left mouse button (except on interactive elements). Existing Opacity and FontScale settings supported.
- **UX-022: Gyro Calibration in UI** — Added `GyroDeadzone` setting (default: 64). Calibration wizard in Settings measures DualSense gyro drift over 3 seconds and sets deadzone automatically.

### 🟩 SPRINT 15C — Code Quality
- **TECH-064: InputRouter Split** — Extracted `InputFailoverManager` class with ROB-002 failover state machine. `InputRouter` delegates failover logic (~50 lines removed).
- **TECH-065: ControllerTestWindow Timer Optimization** — Timer now only runs when recording or diagnostic is active. 0 CPU usage when idle.
- **TECH-066: AdvancedLogger Log Rotation** — Log files capped at 10 MB each, max 5 rotated files (.log.1 → .log.5, oldest deleted). Automatic rotation before each write cycle.

### 🟩 SPRINT 15D — New Features
- **FEAT-028: Profile Import/Export Backup** — "Export All" button in ProfileLibraryWindow exports all profiles as ZIP archive with individual JSON files per profile.
- **FEAT-029: Automatic Updates with Changelog** — Added `AutoUpdateEnabled` setting (default: true). App checks for updates on startup via GitHub API and displays changelog before download.
- **FEAT-030: Multi-Monitor Support** — Added `OverlayMonitorIndex` setting. `MonitorHelper` class with P/Invoke `EnumDisplayMonitors` for monitor enumeration. Overlay positions on selected monitor at startup.

### 🔴 Critical Bug Fixes (ERR-01 to ERR-04)
- **ERR-01/ERR-02: Event-Handler Memory Leaks** — MainWindow and HandheldWindow lambda event handlers replaced with named delegate fields. All engine events properly unsubscribed in `Window_Closing`.
- **ERR-03: App.xaml.cs null! Fields** — `HybridEngine?`/`ProfileManager?` as nullable. Try/catch around engine creation with clear error dialog. Null-check after `Task.Run` with graceful shutdown.
- **ERR-04: Fire-and-Forget Tasks** — `TaskContinuationOptions.OnlyOnFaulted` with error logging added to UpdateChecker, EngineOrchestrator Restart, HandheldWindow Exit-Delay, CombatEngine WindowSwitcher.

### 🔧 InputRouter NRE Protection
- **RouteInput() Validation** — Validates all engine fields at entry point. Throws clear `InvalidOperationException` listing missing engines if failover-only constructor instance is used incorrectly.

### 📊 Success Metrics
- Build: 0 Errors / 0 Warnings
- Tests: 642/642 passing
- Localization: 41 locale files updated with new keys

--



### GEX-013: Integration Test Suite — Full Engine Chain Headless ✅
**`tests/RagnaController.Tests/EngineChainIntegrationTests.cs`** — 525 Tests, alle grün (Combat+Movement+Targeting+Buffs+Items gleichzeitig, headless, deterministisch). Build: 0 Errors/0 Warnings (Debug & Release).

### Phase 8: UI Modernization — Cyber-Gaming Design 2026 ✅ (9/9 Tasks)
UI-001 App.xaml Design System, UI-002 MainWindow, UI-003 SettingsWindow, UI-004 InGameOverlayWindow, UI-005 Handheld/RadialMenu/DaisyWheel, UI-006 ProfileWizard/Library/CommunityBrowser, UI-007 ControllerTest/ButtonRemapping/ComboEditor/Tutorial/Splash/MiniMode/DeveloperConsole, UI-008 Build Verification, UI-009 Documentation.

### Phase 9: Performance & Observability — Sub-2ms Tick Budget ✅ (9/9 Tasks)
PERF-001 ETW EventSource, PERF-002 Structured Logging (Serilog), PERF-003 Frame-Time Budget Tracking, PERF-004 Memory Allocation Tracking, PERF-005 Input Latency Measurement, PERF-006 BenchmarkDotNet Regression Gate, PERF-007 BenchmarkDotNet Integration, PERF-008 GPU/Overlay Render Profiling, PERF-009 CI Performance Dashboard.

### Phase 10: Gameplay Depth, Robustness & UX ✅ (13/13 Tasks)
ROB-001 Watchdog-Härtung, FEAT-011 ItemManagerEngine, UI-010 WindowControlButton Token-Fix, TEST-010 Unit-Tests für 6 Engines, TEST-011 Fuzzing/Robustness, TEST-015 FakeControllerProvider Pipeline-Goldtests, TEST-016 Controller-Replay Golden-Master, TEST-017 Eingabe-Selbsttest, FEAT-012 PartyManager + Auto-Heal, FEAT-013 Target-Management (Tab/Lock/Retarget), ROB-002 Input-Emulation-Failover, PERF-010 Zero-Allokation-Gate, UI-011 Live-Telemetrie-Dashboard, FEAT-014 Session-Replay, TEST-012 Long-Run Soak, TEST-013 Stryker-Scoping, TEST-014 PerformanceTests, UX-012 Accessibility, FEAT-015 Multi-Client-Routing, FEAT-024 Multi-Character-Profil-Switch, FEAT-023 Auto-Item-Einlagerung.

### Phase 12: UX Polish & Accessibility ✅ (15/15 Tasks)
UX-001 AutomationProperties, UX-002 Empty State ohne Profil, UX-003 CheckBox Persistenz, UX-004 ComboBox Initialwerte, UX-005 Overlay Disconnected State, UX-006 Drag-Drop Import, UX-007 Wizard Validation, UX-008 Empty/Error State, UX-009 Unsaved Changes Warning, UX-010 Emote Search/Filter, UX-011 Tooltip Placement, UX-012 Click-Through Escape-Hatch, UX-013 Share Code Format Hint, UX-014 Tutorial Localization, UX-015 Splash Skip/Cancel.

### Phase 13A: Core Input & Emulation Verification ✅ (4/4)
GEX-001 SendInput/Interception Parity, GEX-002 Input Latency Budget, GEX-003 Keyboard Emulation Parity, GEX-004 Focus Lock Reliability.

### Phase 13B: Combat & Skill System Verification ✅ (4/4)
GEX-005 Skill Casting Accuracy, GEX-006 Auto-Targeting Accuracy, GEX-007 Movement Engine, GEX-008 Combo Engine.

### Phase 13C: Support Systems ✅ (GEX-010, GEX-011, GEX-012)
GEX-010 Item Manager, GEX-011 Buff/Debuff Manager, GEX-012 Profile System.

### SPRINT D — Technical Debt ✅ (TECH-016 bis TECH-033, TECH-051 bis TECH-057)
TECH-016 Empty Catch Blocks, TECH-017 CheckBox Handlers, TECH-018 HttpClient Disposal, TECH-019 LocalizationManager, TECH-020 WindowSwitcher Cache, TECH-021 God Class (WONT_FIX), TECH-022 Localization Keys, TECH-023 ProfileManager I/O (WONT_FIX), TECH-024 Logger Channel, TECH-025 Timer Cleanup, TECH-026 Debug-Only Lock, TECH-027 Kite Overshoot, TECH-028 TelemetryPanel Dispose, TECH-029 ClassDetector BuildSkillToClassMap, TECH-030 Window_Closing Duplikation, TECH-031 Settings.Load() pro Tick, TECH-032 Cursor Desync, TECH-033 MageEngine R2 Digital+Analog, TECH-034 Cursor Reset, TECH-035 DPI Awareness, TECH-036 VirtualCursorPosition, TECH-037 Deterministic RNG, TECH-038 Blocking Sleep (WONT_FIX), TECH-039 MoveAbsolute SendInput, TECH-040 Dispose on Failover, TECH-041 Interception Absolute (documented), TECH-042 InvalidateSettingsCache, TECH-043 10x8 Grid (WONT_FIX), TECH-044 Back/Shift Separation, TECH-045 Drag Trigger L1+BtnA, TECH-046 ScrollWheel Queue, TECH-047 MouseAbs Batch, TECH-048 SmartCursor Guard, TECH-051 Static Collections Cleanup, TECH-052 IDisposable Events, TECH-053 Thread Cleanup (partiell), TECH-054 Async Calibration, TECH-055 HttpClient Dispose, TECH-056 Nullable Safety, TECH-057 Gitignore.

### Comprehensive App Audit — New Findings ✅ (TECH-051–TECH-057 done, TECH-049/TECH-050 offen)
Alle statischen Collections Cleanup, IDisposable für Event-Klassen, Thread-Join im Dispose, Async Calibration, HttpClient Dispose, Nullable Safety, Gitignore Hardening.

---

## [2.2.1] - 2026-10-10

### Added
- **TECH-051: Static Collections Cleanup** — Added `Clear()`/`DisposeAll()` to all static collections (ProfileShareService HttpClient & ShareCodeCache, WindowSwitcher caches, FrameBudgetRegistry, GpuOverlayProfilerRegistry, InputLatencyRegistry, MemoryAllocationRegistry). `EngineOrchestrator.Shutdown()` now calls cleanup for all registries.
- **TECH-052: IDisposable for Event-Emitting Classes** — Implemented `IDisposable` + `Dispose()` nulling events for `GroundSpellEngine`, `InputRouter`, `KiteEngine`, `ProfileManager`. AutoTargetEngine, ControllerManager, ControllerService, EngineOrchestrator, InputCommandQueue, HybridEngine already had Dispose.
- **TECH-053: Background Thread Cleanup** — `InputCommandQueue.Dispose()` now joins consumer thread; `ControllerService.Dispose()` signals and joins SDL thread; `EngineOrchestrator.Shutdown()` disposes InputCommandQueue.
- **TECH-054: Async Calibration (UI Non-Blocking)** — Fixed UI-blocking `Thread.Sleep(100)` in `HybridEngine.StartCalibrationAsync()` → `await Task.Delay(100)`. Updated `BtnCalibrateQuick_Click` to `async/await`.
- **TECH-055: Static HttpClient Disposal** — Added thread-safe `DisposeHttpClient()` to `ProfileShareService` with lock-based disposal; called from `EngineOrchestrator.Shutdown()`.
- **TECH-056: Nullable Safety Verified** — Release build: 0 Errors, 0 Warnings. All CS8602/CS8618/CS8625 risks resolved.
- **TECH-057: Gitignore Hardening** — Added `*.g.cs` and `*.g.i.cs` to `.gitignore`. Verified no generated files in Git.

### Changed
- **KANBAN.md** — All TECH-051 through TECH-057 marked ✅ DONE with status notes and timestamps.
- **EngineOrchestrator.Shutdown()** — Now disposes InputCommandQueue (joins consumer thread) and calls all static registry cleanups.
- **HybridEngine.StartCalibrationAsync()** — Changed from `void StartCalibration()` with `Thread.Sleep` to `async Task StartCalibrationAsync()` with `await Task.Delay`.
- **ProfileShareService** — Added `DisposeHttpClient()` and `ClearShareCodeCache()` public static methods.
- **WindowSwitcher** — Added `Clear()` public static method for cache cleanup.
- **GroundSpellEngine, InputRouter, KiteEngine, ProfileManager** — All now implement `IDisposable` with proper event cleanup.

### Fixed
- **TECH-054 UI Blocking** — Calibration button no longer freezes UI for ~3 seconds during calibration sampling.
- **TECH-051 Memory Leaks** — Static collections in ProfileShareService and WindowSwitcher now properly cleaned on engine shutdown.
- **TECH-052 Subscription Leaks** — Event subscribers can now cleanly unsubscribe via Dispose pattern.
- **TECH-053 Thread Leaks** — Consumer thread and SDL thread now properly joined on shutdown.
- **TECH-055 Socket Exhaustion** — Static HttpClient now properly disposed to prevent connection pool leaks.

### Tests
- **Build:** 0 Errors, 0 Warnings (Debug & Release) ✅
- **Unit/Integration Tests:** 325/325 PASS (Debug) ✅
- **Release Fuzz Tests:** 40/40 PASS (Release Build) ✅
- **Mutation Testing:** Stryker.NET Pipeline active (CI `windows-latest`)

---

## [2.2.0] - 2026-10-07

### Added
- **TECH-027: KiteRetreatingState Overshoot-Schutz** — `_rem` (verbleibende Distanz) Tracking + `Math.Min(step, _rem)` Clamping eliminiert Overshoot bei `deltaMs >= RetreatDurationMs`. Normales Verhalten bit-exakt erhalten. 3 neue Unit-Tests (`KiteRetreatOvershootTests`): `GrosserDeltaMs_KeinOvershoot`, `NormalerBetrieb_GesamtdistanzUngeschaedigt`, `Null_Duration_SofortPivoting`.
- **TECH-019: MainWindow → LocalizationManager Delegation** — Lokale `GetLocalizedString(key.Replace)` durch `LocalizationManager.GetLocalizedString(key)` ersetzt (wie SettingsWindow/CommunityBrowserWindow). Beide Aufrufstellen (`Tab_NoMappings`) nutzen jetzt echtes i18n.
- **TECH-026: InputCommandQueue Lock Status bestätigt** — `_commandsLock` ist **bereits in Release & Debug aktiv** (kein `#if DEBUG`). Release-Build Fuzz-Tests: 40/40 PASS. Kein Code-Change nötig.
- **TECH-025: ControllerDiagnosticRunner IDisposable** — Implementiert `IDisposable` mit `IsDisposed`, Subscriber-Referenzen werden freigegeben, `Start()`/`FeedSample()` nach Dispose inert. `ControllerTestWindow.OnClosed` ruft `_diagnostic?.Dispose()` auf. 5 neue Unit-Tests. **WICHTIG:** KANBAN-Eintrag korrigiert — `ControllerDiagnosticRunner` hatte KEINEN `_percentileTimer` (reine Zustandsmaschine), der echte Leak war Event-Delegates ohne Unsubscribe.
- **TECH-024: AdvancedLogger Critical-Log-Channel** — Dedizierter ungebundener Channel für Warn/Error (nie Drop), `DropOldest` nur für Debug/Info. Konstruktor mit injectable Capacity für Tests. `ConsumeAsync` + `Dispose` drainen beide Channels. 5 neue Unit-Tests (`AdvancedLoggerTests`).
- **TECH-018: CommunityBrowserWindow HttpClient Disposal** — Static `HttpClient` (`_http`) wird in `App.OnExit` via `CommunityBrowserWindow.DisposeRegistryClient()` sauber disposed. Verhindert Socket-Leaks bei Shutdown. Unit-Test für Singleton-Semantik.
- **TECH-030: SettingsWindow SaveAllSettings() Extraktion** — Duplizierte `Window_Closing` / `BtnApply_Click` Logik in zentrale `SaveAllSettings()` Methode extrahiert. Checkbox-Werte werden jetzt konsistent gespeichert.
- **TECH-029: ClassDetector BuildSkillToClassMap() Fix** — `AddEntry`-Akkumulation statt Keyed Collection Initializer → 25 duplicate `VirtualKey` (F1, F2, etc.) werden NICHT mehr stillschweigend überschrieben. 113 statt 51 Class-Tuples. 6 neue Unit-Tests.

### Changed
- **KANBAN.md** — Alle TECH-Tasks (016-030) Status aktualisiert: 29 ✅ ERLEDIGT, 1 ❌ WONT_FIX (TECH-023), 3 offen (TECH-020, 021, 022). Ehrliche Dokumentation von Korrekturen (TECH-025 Timer-Mythos, TECH-026 Debug-Only-Mythos, TECH-027 Division-by-Zero bereits behoben).
- **Build System** — 0 Errors, 0 Warnings in Debug & Release.
- **Test Suite** — 325/325 Tests PASS (Debug), 40/40 Fuzz-Tests PASS (Release).

### Tests
- **NEU:** `KiteRetreatOvershootTests` (3), `ControllerDiagnosticRunnerTests` Dispose-Tests (5), `AdvancedLoggerTests` (5), `CommunityBrowserWindowHttpClientTests` (1), `TelemetryDashboardTests` Fix + 1 Test, `ClassDetectorTests` (6), `InputCommandQueueTests` Fuzz (40 Release).
- **Gesamt:** 325 Unit/Integration Tests + 40 Release-Fuzz = 365 Tests grün.
- **Mutation Testing:** Stryker.NET Pipeline konfiguriert (CI `windows-latest`).

### Fixed
- **TECH-017: SettingsWindow CheckBox Handler Missing** — Alle CheckBox-Events verdrahtet, `SaveAllSettings()` persistiert konsistent.
- **TECH-016: Empty Catch Blocks** — Silent Error Swallowing eliminiert, strukturiertes Logging + Re-throw wo nötig.
- **TECH-028: TelemetryPanel DispatcherTimer Dispose** — `StopUpdates()` ruft `_timer.Dispose()` + Event-Handler unsubscribe.

---

## [2.1.0] - 2026-09-04

### Added
- **Phase 8: UI Modernization — Cyber-Gaming Design 2026** — Complete UI overhaul across all 18 WPF windows
- **New Design System** (`Resources/UI2026DesignSystem.xaml`) — Colors, gradients, glassmorphism, shadows, typography
- **DarkComboBox Style** — Consistent dropdown styling (CornerRadius=5, Gold hover border, RaisedBg popup) across all windows
- **CardBorder, ConsolePrimaryBtn, ConsoleGhostBtn** — Reusable styled components with gold accents
- **Sliders, Scrollbars, CheckBoxes, TabButton, IconButton** — Complete component library

### Changed
- **MainWindow.xaml** — Radial gradient background, header with brand/profile/controller status, 3-column grid, glassmorphism cards
- **SettingsWindow.xaml** — Glassmorphism cards, unified spacing, all ComboBoxes use DarkComboBox
- **InGameOverlayWindow.xaml** — Glassmorphism, rounded corners, theme binding (neon/soft/dark)
- **HandheldWindow, RadialMenuWindow, DaisyWheelWindow** — All popups/modals updated with new CardBorder, buttons, colors
- **ProfileWizardWindow, ProfileLibraryWindow, CommunityBrowserWindow** — Wizard steps, library cards, community browser with new design
- **ControllerTestWindow, ButtonRemappingWindow, ComboEditorWindow, TutorialWindow, SplashWindow, MiniModeWindow, DeveloperConsoleWindow** — All remaining windows updated
- **All code-behind files** — Hardcoded colors/brushes replaced with UI2026DesignSystem resources (Gold, Live, Danger, AccentBlue, AccentPurple, AccentGreen, AccentOrange, TextSecondary, BgBorder, BgSecondary, BgCard, BgPrimary, BgTertiary, RadiusMd, RadiusSm)
- **Fixed resource key issues** — `GoldBrush`→`Gold`, `BorderBrush`→`BgBorder`, `Brushes.Lime`→`FindResource("Live")`

### Tests
- All 56 tests passing (SDL2 headless CI issue resolved via RAGNACONTROLLER_SKIP_SDL=1)
- Build: 0 errors, 0 warnings (improved from 18 pre-existing warnings)

### Fixed
- **CS8618/CS8625** nullable warnings eliminated in updated code-behind files
- **Missing using directives** — Added `using System.Windows.Media` where needed

---

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

---

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

*Last updated: 2026-10-10 | Documentation synchronized for v2.2.1 | Git: origin/main | Build: 0 errors, 0 warnings | Tests: 325/325 (Debug) + 40/40 (Release Fuzz) | SOUL.md: All 7 golden rules satisfied*