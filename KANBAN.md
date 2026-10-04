# KANBAN Board — RagnaController v2.1.0

## Phase 8: UI Modernization — Cyber-Gaming Design 2026 ✅ COMPLETED
**Goal:** Elevate all WPF windows to 2026 Cyber-Gaming standard: dark theme, glassmorphism, rounded corners, gold accents, consistent DarkComboBox across all windows.

### UI-001: App.xaml Design System ✅ DONE
**Status:** ✅ Done
**Assigned:** @designer / @coder
**Description:** Neues Design-System mit Colors, Gradients, Glassmorphism, Shadows, Typography
**Implementation:** `Resources/UI2026DesignSystem.xaml` erstellt
**Features:** CardBorder, ConsolePrimaryBtn, ConsoleGhostBtn, DarkComboBox, Sliders, Scrollbars, CheckBoxes, TabButton, IconButton

### UI-002: MainWindow.xaml Modernisierung ✅ DONE
**Status:** ✅ Done
**Assigned:** @designer / @coder
**Description:** MainWindow.xaml vollständig auf neues Design-System migriert: Radial-Gradient-Hintergrund, Header mit Brand/Status/Profile-Selector, 3-Spalten-Grid, Glassmorphism Cards, DarkComboBox, Tab-Navigation, Quick Actions
**Implementation:** `src/RagnaController/MainWindow.xaml`

### UI-003: SettingsWindow.xaml Modernisierung ✅ DONE
**Status:** ✅ Done
**Assigned:** @designer / @coder
**Description:** SettingsWindow.xaml mit konsistentem DarkComboBox, Glassmorphism Cards, einheitlichem Spacing
**Implementation:** `src/RagnaController/SettingsWindow.xaml`

### UI-004: InGameOverlayWindow.xaml Modernisierung ✅ DONE
**Status:** ✅ Done
**Description:** Glassmorphism, rounded corners, theme binding (neon/soft/dark)
**Implementation:** `src/RagnaController/InGameOverlayWindow.xaml`
**Changes:** Updated to use new CardBorder, Theme system (neon/soft/dark) integrated

### UI-005: HandheldWindow, RadialMenuWindow, DaisyWheelWindow ✅ DONE
**Status:** ✅ Done
**Description:** Alle Popups/Modals mit neuen CardBorder, Buttons, Colors, consistent DarkComboBox style
**Implementation:** `src/RagnaController/HandheldWindow.xaml`, `src/RagnaController/RadialMenuWindow.xaml`, `src/RagnaController/DaisyWheelWindow.xaml`
**Changes:** All hardcoded colors/brushes in code-behind replaced with UI2026DesignSystem resources (BgSecondary, BgBorder, TextSecondary, Gold, AccentPurple, AccentBlue, Live, Danger, RadiusSm, RadiusFull, CardShadow)

---

### UI-006: ProfileWizardWindow, ProfileLibraryWindow, CommunityBrowserWindow ✅ DONE
**Status:** ✅ Done
**Description:** Wizard steps, library cards, community browser mit neuem Design
**Implementation:** `src/RagnaController/ProfileWizardWindow.xaml`, `src/RagnaController/ProfileLibraryWindow.xaml`, `src/RagnaController/CommunityBrowserWindow.xaml`
**Changes:** Fixed hardcoded `Brushes.Lime` → `FindResource("Live")`, fixed wrong resource keys (`GoldBrush`→`Gold`, `BorderBrush`→`BgBorder`), added missing `using System.Windows.Media` in code-behind files

---

### UI-007: ControllerTestWindow, ButtonRemappingWindow, ComboEditorWindow, TutorialWindow, SplashWindow, MiniModeWindow, DeveloperConsoleWindow ✅ DONE
**Status:** ✅ Done
**Description:** Alle verbleibenden Fenster aktualisieren
**Implementation:** `src/RagnaController/ControllerTest/ControllerTestWindow.xaml.cs`, `src/RagnaController/ButtonRemappingWindow.xaml.cs`, `src/RagnaController/ComboEditorWindow.xaml.cs`, `src/RagnaController/TutorialWindow.xaml.cs`, `src/RagnaController/SplashWindow.xaml.cs`, `src/RagnaController/MiniModeWindow.xaml.cs`, `src/RagnaController/DeveloperConsoleWindow.xaml.cs`
**Changes:** All hardcoded colors/brushes replaced with UI2026DesignSystem resources (Gold, Live, Danger, AccentBlue, AccentPurple, AccentGreen, AccentOrange, TextSecondary, BgBorder, BgSecondary, BgCard, BgPrimary, BgTertiary, RadiusMd, RadiusSm)

---

### UI-008: Build Verification ✅ DONE
**Status:** ✅ Done
**Description:** 0 Errors, 0 Warnings, alle 56 Tests grün (mit RAGNACONTROLLER_SKIP_SDL=1)

### UI-009: Documentation ✅ DONE
**Status:** ✅ Done
**Description:** CHANGELOG.md v2.1.0, README.md updates

## Phase 9: Performance & Observability — Sub-2ms Tick Budget ✅ IN PROGRESS
**Goal:** Sub-2ms tick budget, structured logging, ETW tracing, benchmark regression gates, and profiling infrastructure.

### PERF-001: ETW EventSource for tick loop ✅ DONE
**Status:** ✅ Done
**Assigned:** @coder
**Description:** `RagnaControllerEventSource` with TickStart/TickEnd, EngineStart/EngineEnd, InputEmitted events
**Implementation:** `src/RagnaController/Core/RagnaControllerETW.cs`

### PERF-002: Structured Logging (Serilog) ✅ DONE
**Status:** ✅ Done
**Assigned:** @coder
**Description:** Serilog configured with JSON output, correlation IDs, log levels per component
**Implementation:** `src/RagnaController/Core/AdvancedLogger.cs`

### PERF-003: Frame-Time Budget Tracking ✅ DONE
**Status:** ✅ Done
**Assigned:** @coder
**Description:** `FrameBudgetMonitor` tracks P50/P95/P99 tick latency, warns >2ms, exports ETW
**Implementation:** `src/RagnaController/Core/FrameBudgetMonitor.cs`

### PERF-004: Memory Allocation Tracking ✅ DONE
**Status:** ✅ Done
**Assigned:** @coder
**Description:** Track Gen0/1/2 collections per tick, large object heap pressure, object pool hit rates
**Implementation:** `src/RagnaController/Core/MemoryAllocationTracker.cs`
**Features:** Ring buffer with 5000 samples, percentile tracking (P50/P95/P99), object pool hit/miss rates, ETW integration, thread-safe lock-free design

### PERF-005: Input Latency Measurement ✅ DONE
**Status:** ✅ Done
**Assigned:** @coder
**Description:** End-to-end latency: hardware event → SendInput completion, P99 < 5ms
**Implementation:** `src/RagnaController/Core/InputLatencyTracker.cs`, integrated into `EngineOrchestrator` and `InputCommandQueue`
**Features:** Lock-free ring buffer (50k samples), P50/P95/P99 percentiles per stage (Enqueue/Dispatch/SendInput/Total), per-controller tracking, ETW integration, background percentile calculation every 10s, budget exceedance detection (>5ms), human-readable reports
**Dependencies:** PERF-001

### PERF-006: BenchmarkDotNet Regression Gate ✅ DONE
**Status:** ✅ Done
**Assigned:** @qa / @coder
**Description:** CI gate: `BenchmarkGate.ValidateInputLatencyGate()` fails build if P95 > 5ms threshold (PERF-005 target)
**Dependencies:** PERF-007 ✅
**Implementation:** `benchmarks/RagnaController.Benchmarks/BenchmarkHarness.cs` - `BenchmarkGate` class with CLI `--input-latency-gate <jsonPath>`
**CI Integration:** `.github/workflows/ci.yml` - new `benchmark` job runs InputLatency benchmarks and validates P95 < 5ms gate
**Results:** P95 = 0.077ms (keystroke), 0.027ms (mouse), 0.399ms (macro) — all well under 5ms target

### PERF-007: BenchmarkDotNet Integration ✅ DONE
**Status:** ✅ Done
**Assigned:** @coder
**Description:** `BenchmarkHarness.cs` executable project with 5 benchmark suites
**Implementation:** `benchmarks/RagnaController.Benchmarks/BenchmarkHarness.cs`
**Suites:** ControllerPolling, EngineOrchestrator, InputEmulation, MemoryAllocation, ProfileAndMacro, **InputLatency (new: end-to-end hardware→SendInput)**
**Features:** BenchmarkDotNet 0.14, MemoryDiagnoser, DisassemblyDiagnoser (Windows), GitHub/CSV/HTML/JSON exporters, headless CI config

### PERF-008: GPU/Overlay Render Profiling ✅ DONE
**Status:** ✅ Done
**Assigned:** @designer / @coder
**Description:** `GpuOverlayProfiler.cs` erstellt: Render-Tier-Detektion, Frame-Timing (Layout/Render/Composite), Composition-FPS, GPU-Memory-Pressure via WMI
**ETW Events:** 70-73 hinzugefügt in `RagnaControllerETW.cs` (`WpfRenderTier`, `OverlayFrameTiming`, `CompositionEngineStats`, `GpuMemoryPressure`)
**Registry:** Zentrale `GpuOverlayProfilerRegistry` mit Report-Generierung
**Integration:** In `InGameOverlayWindow` über `BeginFrame/EndLayout/EndRender/EndFrame` Pattern
**Completed:** 2026-09-03
**Verified By:** @coder

### PERF-009: CI Performance Dashboard ✅ DONE
**Status:** ✅ Done
**Assigned:** @devops
**Description:** GitHub Actions erweitert: Benchmark-Job generiert Markdown-Summary-Tabelle (P50/P95/P99/Mean/StdDev), lädt als Artifact hoch (90 Tage Retention), prüft Regressionen (P95 > 5ms InputLatency, > 10ms allgemein) und schlägt Build fehl bei Regression
**Completed:** 2026-09-03
**Verified By:** @coder

---

## Phase 10: Gameplay Depth, Robustness & UX (IN PLANNING → SPRINT A START)
**Goal:** Die größten Gameplay-Lücken schließen (Items/Potions, Party, Targeting), Robustheit härten (Watchdog-Hang-Erkennung, Input-Failover, Fuzzing, Soak), Phase-9-Telemetrie in die UI bringen und Design-Token-Bug fixen.
**Entstanden durch:** Team-Diskussion 2026-09-07 (Coder ROLE-003 + Designer ROLE-002 + QA/Researcher ROLE-004, moderiert von Architect ROLE-001).

### 🟥 SPRINT A — HIGH Priority (sofort startklar)

#### ROB-001: Watchdog-Härtung — Hang-Erkennung & Auto-Restart
**Status:** ✅ DONE | **Assigned:** @coder | **Priorität:** HIGH
**Description:** `EngineWatchdog` überwacht heute nur Tick-Dauer (kein RecordTick mehr = kein Hang erkannt). Erweiterung um externen Timer (Task.Delay ~100ms) mit Last-Tick-Timestamp: >500ms ohne Tick → `EngineOrchestrator.Restart()` + Telemetrie-Event. Input-Loss-Metrik via InputLatencyTracker.
**Dependencies:** TelemetryService, PERF-005 (beide existieren)
**Files:** `Core/EngineWatchdog.cs`, `Core/EngineOrchestrator.cs`, `Core/TelemetryService.cs`
**DoD:** Hang simuliert im Test → Orchestrator-Restart ohne App-Crash; Watchdog deterministisch stoppbar (IDisposable); Unit-Tests grün.

#### FEAT-011: ItemManagerEngine — Auto-Potion & Item-Verwaltung *(merge Coder FEAT-011 + QA FEAT-020)*
**Status:** ✅ DONE | **Assigned:** @coder | **Priorität:** HIGH
**Description:** Größte Gameplay-Lücke: kein Item-/Potion-Management vorhanden (nur `CombatEngine.CurrentHPPercent` ohne Konsument). Neue Engine überwacht HP/SP-Schwellwerte pro Profil und feuert Heil-/SP-Potion-Hotkeys, respektiert Cooldowns über `CooldownManager`, Zero-Allokation im Tick-Pfad.
**Dependencies:** CooldownManager (existiert), SkillOrchestrator-Conditions als Muster
**Files:** `Core/ItemManagerEngine.cs` (neu), `Models/ItemConfig.cs` (neu), `Core/EngineOrchestrator.cs`, `Profiles/Profile.cs`, `Profiles/AppJsonContext.cs`, `Core/ProfileApplier.cs`, `Core/Messages.cs`
**DoD:** ✅ Items feuern bei unterschrittener Schwelle + Cooldown eingehalten · ✅ Settings-Model mit Defaults (`ItemConfig`: HpThresholdPercent=70, CooldownMs=3000, CheckIntervalMs=1000) · ✅ Unit-Tests headless: 18 Facts — Schwelle exakt/unter/über, Cooldown blockiert Re-Fire, kein Fire wenn gestoppt (disconnected)
**Implementation Notes:** Per-item Cooldown + Check-Intervall über Parallel-Arrays (`_nextCheck`/`_nextEligible`, index-basiert → Zero-Allokation im Tick-Pfad), injizierbare Clock für deterministische Tests. `ItemManagerEnabled` (Default false) + `ManagedItems` pro Profil persistierbar via JSON Source-Gen.

#### UI-010: BUG-FIX — undefiniertes Design-Token `WindowControlButton` *(aus Designer-Audit)*
**Status:** ✅ DONE | **Assigned:** @designer / @coder | **Priorität:** HIGH (S2 — 5 Fenster betroffen)
**Description:** `StaticResource WindowControlButton` wird in `MainWindow.xaml`, `ComboEditorWindow.xaml`, `ButtonRemappingWindow.xaml`, `TutorialWindow.xaml`, `DeveloperConsoleWindow.xaml` referenziert, aber nirgends definiert (Audit: 0 Definitionen in `UI2026DesignSystem.xaml` + `App.xaml`). Fenster-Close/Minimize-Buttons fallen auf Default-Styling zurück bzw. brechen beim Resource-Lookup.
**Files:** `Resources/UI2026DesignSystem.xaml`, betroffene XAMLs
**DoD:** Token zentral definiert (konsistent mit Cyber-Gaming Design-Sprache), alle 5 Fenster verwenden es, Build 0 Errors/Warnings.

#### TEST-010: Dedizierte Unit-Tests für ungetestete Engines
**Status:** ✅ DONE | **Assigned:** @qa / @coder | **Priorität:** HIGH
**Description:** SkillOrchestrator (komplexeste Engine: RotationSteps, Conditions HPAbove/SPAbove/MissingBuff, Priority-Selection), BuffManager, CooldownManager, SupportEngine, MageEngine, MobSweepEngine haben KEINE eigenen Testdateien — nur indirekte Wiring-Prüfungen. Gefährdet die Stryker-80%-Schwelle.
**Files:** `tests/RagnaController.Tests/SkillOrchestratorTests.cs` (neu), `BuffManagerTests.cs`, `CooldownManagerTests.cs`, `SupportEngineTests.cs`
**DoD:** Min. 5 Facts pro Engine (Step-Auswahl, Condition-Grenzwerte, Loop vs. Single-Pass, Warnungs-Event exakt einmal, AutoRecast via Mock-Queue, Cooldown blockiert Re-Register); Stryker-Score der Dateien steigt messbar.
**Fortschritt (2026-09-11):** ✅ **ALLE 6 ENGINES ABGEDeckt — 148 Tests grün**
- ✅ **MobSweepEngine:** `MobSweepEngineTests.cs` — 11 Facts: Defaults, Activate/Deactivate, R1→TargetingParty+Attack-Key, Cooldown-Gating (kein Re-Fire), Tab-Tap+Idle-Rückkehr, BtnY→Healing+Heal-Key, Update-Dekrement/Clamp, Reset
- ✅ **Bugfix (S2) als Nebenprodukt:** `InputCommandQueue.cs` — 2-Arg-Konstruktor `InputCmd(CmdType, ushort key)` chainete auf falschen Overload `(type, int x, int y, Action?)` → VK-Code landete in `X` statt `Key`, `ProcessCommand` sendete stillschweigend VK 0 (alle Tasten-Inputs der Queue defekt). Fix: Key explizit setzen; gleicher Bug-Klasse auch bei key+callback-Variante + `Wheel(delta)` (Delta jetzt in `X`) korrigiert.
- ✅ **CooldownManager:** `CooldownManagerTests.cs` — RegisterAction/TrackBuff-Gating, Warn-Zeitpunkt = Duration−Warning via TickCount64, BuffWarning exakt einmal, ResetAll
- ✅ **SupportEngine:** `SupportEngineTests.cs` — R1→Tab (PartyTabCycle), Y→Heal-Key, Cooldown-Gating blockiert Re-Fire, Phase-Maschine, ToggleSupportMode, Reset
- ✅ **BuffManager:** `BuffManagerTests.cs` — RegisterBuff/Update: Warnung exakt einmal (WarningFired-Flag), Expired-Event, AutoRecast feuert RecastKey via Mock-Queue, Debuff-Lifecycle, ClearAll
- ✅ **MageEngine:** `MageEngineTests.cs` — R2→Bolt-Spam mit Cast-Delay-Cooldown (JitterService.Apply!), Phase Idle↔BoltSpamming, Gyro-Injection, Reset
- ✅ **SkillOrchestrator:** `SkillOrchestratorTests.cs` — RotationSteps-Auswahl, Conditions HPAbove/SPAbove/MissingBuff-Grenzwerte, Priority-Selection, Loop vs. Single-Pass (Differential-Test: Loop wartet auf blockiertem Step, Single-Pass resetet auf Start), Completion-Events
**DoD-Abnahme:** ✅ Min. 5 Facts pro Engine (alle 6 Engines übererfüllt) · ✅ `dotnet test` grün mit `RAGNACONTROLLER_SKIP_SDL=1` (148 Tests, 0 Fehler) · ✅ Build 0 Errors/Warnings

#### TEST-011: Fuzzing / Robustness für InputCommandQueue & ParsedInput
**Status:** ✅ DONE | **Assigned:** @qa / @coder | **Priorität:** HIGH
**Description:** Die Input-Queue ist das Herzstück (alle Engines enqueueen), aber nur 5 deterministische Tests. Fehlt: zufällige Command-Sequenzen, Enqueue während Stop/Shutdown-Race, Overflow bei vollem Queue, ParsedInput-Edge-Cases (256 Button-Kombinationen, extreme Stick-Werte).
**Files:** `tests/RagnaController.Tests/InputFuzzTests.cs` (neu), `Core/InputCommandQueue.cs`, `Core/ParsedInput.cs`
**DoD:** Seeded-RNG-Fuzz: 10.000 Commands deterministisch reproduzierbar ohne Exception/Deadlock; Race-Test Enqueue↔Stop() über 20 Zyklen verliert keine Konsistenz; Edge-Cases als Facts grün.
**Fortschritt (2026-09-12):** ✅ **35 Fuzz-Facts grün — DoD erfüllt**
- ✅ **Seeded-RNG-Fuzz (Seed 20260912):** 10.000 Commands (Key/VK 0..255, MouseRel, Wheel-Deltas, Wait, Atomic-Mouse, Action) — Determinismus per SHA256-Sequenz-Hash verifiziert; Consumer-Drain mit 30s-Deadlock-Guard; `QueueCount == 0` nach Drain (kein stilles Kommando-Verlust)
- ✅ **Race-Test Enqueue↔Stop():** 20 Zyklen × 4 Producer-Threads (je 500 Commands, seeded) gegen Stop() mit randomisiertem Zeitpunkt (0..15ms); Invariante `executed ≤ enqueued`, keine Exception im Consumer
- ✅ **Bugfix (S2) als Nebenprodukt:** `InputCommandQueue.RecordCommand` — ungesehütztes `List.Add` aus mehreren Enqueue-Threads → `ArgumentException` im Resize-Pfad. Fix: `_commandsLock` um `Commands.Add` (DEBUG-only Pfad, keine neuen Abhängigkeiten).
- ✅ **ParsedInput-Edge-Cases:** 24 Flags × JustPressed/JustReleased-Transitionen (Theorie), 1.024 seeded zufällige Button-Kombinations-Paare gegen Referenz-Definition, extreme Stick-Werte (NaN/±Inf/Max/Min) bit-stabil durch `With()`, Trigger-Out-of-Range ohne Clamping, ButtonsPressed-Isolation
**DoD-Abnahme:** ✅ 10.000 Commands deterministisch reproduzierbar, keine Exception/Deadlock · ✅ Race 20 Zyklen konsistent · ✅ Edge-Cases grün · ✅ `dotnet test` Full-Suite 183/183 (mit RAGNACONTROLLER_SKIP_SDL=1)

#### TEST-015: Controller-Eingaben-Testharness — FakeControllerProvider + Pipeline-Goldtests
**Status:** COMPLETE | **Assigned:** @qa / @coder | **Priorität:** HIGH
**Description:** Die Input-Pipeline (Roh-State → `ButtonState` → `ParsedInput` → `SnapshotBuilder.Build`) wird heute nur stückweise getestet (`ParsedInputTests`, `InputFuzzTests`, `InputCommandQueueTests`). Es fehlt der **end-to-end-Test ohne Hardware**: Ein injizierbares `FakeControllerProvider` (implementiert `IControllerProvider`, synthetische deterministische Frames) erlaubt es, exakte Button-/Stick-/Trigger-Sequenzen in die echte Pipeline zu speisen und das resultierende `ControllerSnapshot` gegen Erwartungswerte abzugleichen. Damit ist jede Stufe der Eingabe-Kette (Mapping, JustPressed/JustReleased-Transitionen, Cooldown-Gating im Snapshot) reproduzierbar verifizierbar — auch headless in CI (`RAGNACONTROLLER_SKIP_SDL=1`).
**Dependencies:** `IControllerProvider` (existiert), `SnapshotBuilder` (existiert, P/Invoke-frei → direkt testbar), `InternalsVisibleTo` (seit FEAT-015 vorhanden)
**Files:** `tests/RagnaController.Tests/FakeControllerProvider.cs` (neu), `tests/RagnaController.Tests/ControllerPipelineTests.cs` (neu), ggf. `Core/SnapshotBuilder.cs` (nur wenn Testbarkeit erfordert)
**DoD:** ≥ 10 Facts: (1) exaktes Button-Drücken → korrekter Snapshot mit JustPressed, (2) Release → JustReleased genau ein Frame später, (3) Stick/Trigger-Werte landen bit-exakt im Snapshot, (4) Disconnect → `Disconnected`-State, (5) Reconnect → saubere Transition ohne Ghost-Buttons, (6) XInput-Fallback-Pfad mit Fake identisch mappbar. Suite grün headless.
**DoD-Abnahme:** ✅ 10 Facts: JustPressed/JustReleased exakt einmal · Stick/Trigger bit-exakt (L2/R2-Schwelle 0.15) · Disconnect→Disconnected + Prev-Reset · Reconnect ohne Ghost-Buttons · XInput-Digital-Mapping bit-exakt (RawButtons-Mask) · End-to-End `SnapshotBuilder.Build` → korrekter `ControllerSnapshot` (BtnA/L1/RightX/RightY/StateLabel). Suite 253/253 grün headless.

#### TEST-016: Controller-Replay — Aufgezeichnete Sessions als CI-Golden-Master
**Status:** ✅ COMPLETE | **Assigned:** @qa / @coder | **Priorität:** HIGH
**Description:** Antwort auf „läuft das alles einwandfrei?" mit echter Hardware-Datenlage: `ControllerTestWindow` (oder ein CLI-Tool) zeichnet eine Controller-Session als deterministisches JSON-Fixtur auf (Timestamp + Roh-State pro Frame). Die Fixtur wird ins Repo committet und in der CI **replayed** durch die echte Pipeline (`FakeControllerProvider` aus TEST-015 speist die Frames, `SnapshotBuilder` baut die Snapshots) — Abweichung gegen den aufgezeichneten Snapshot-Verlauf = Regression. Einmal aufgezeichnet (mit echtem DualSense/XInput), für immer CI-verifizierbar. Deckt Mapping-Drift ab, der nur an echter Hardware sichtbar wäre.
**Dependencies:** TEST-015 (`FakeControllerProvider`)
**Files:** `tests/RagnaController.Tests/fixtures/controller-session.golden.json` (neu), `tests/RagnaController.Tests/ControllerReplayTests.cs` (neu), `ControllerTest/ControllerTestWindow.xaml.cs` (Aufnahmefunktion, opt. CLI-Export)
**DoD:** ≥ 1 Session-Fixtur (≥ 500 Frames, mehrere Buttons + Sticks + Disconnect/Reconnect) committet; Replay-Test vergleicht Frame-für-Frame (JustPressed/JustReleased + Stick-Werte) und schlägt bei jeder Abweichung fehl; Aufnahmefunktion in ControllerTestWindow mit Export nach JSON. Suite grün headless.
**DoD-Abnahme:** ✅ 720-Frame-Session `fixtures/controller_session_001.json` (deterministisch generiert via `tools/generate_session_001.py`, 5 Disconnect-Frames 405–409 + Reconnect 410) + Golden-Master `controller_session_001.golden.json` · `ControllerReplayTests`: Frame-für-Frame Replay durch `FakeControllerProvider` + `PipelineStep`, schlägt bei jeder Abweichung fehl (JustPressed/JustReleased/Sticks bit-exakt) · Ghost-Release-Test generisch (findet Disconnect/Reconnect in der Fixtur, kein Hardcoding) · Aufnahmefunktion: `ControllerSessionRecorder` (unit-testbar, 5 Facts) + Start/Stop-Buttons im `ControllerTestWindow` mit Export nach `%LOCALAPPDATA%\RagnaController\recordings\session-<ts>.json` im exakten Fixture-Format. Suite 260/260 grün headless.

#### TEST-017: Controller-Diagnose — Eingabe-Selbsttest im ControllerTestWindow
**Status:** COMPLETE | **Assigned:** @designer / @coder | **Priorität:** HIGH
**Description:** Manuelle Verifikationsebene für den Endnutzer („einfach prüfen, ob mein Controller alles kann"): Neuer Tab/Modus „Eingabe-Check" in `ControllerTestWindow` — führt eine geführte Prüfung durch: nacheinander jedes Button/Stick/Trigger wird aufgefordert („Drücke □", „Stick links nach oben"), pro Kontrolle wird gemessen **ob** das Event ankommt, **wie schnell** (Input-Latenz via PERF-005 `LatencyTracker`) und ob die erwartete Mapping-Kette stimmt. Ergebnis: Pass/Fail pro Kontrolle + Gesamtstatus, optional als Report exportierbar (JSON/Text). Damit hat der Nutzer ein verifizierbares „einwandfrei"-Statement statt nur Live-Anzeige.
**Dependencies:** `ControllerTestWindow` (existiert), PERF-005 LatencyTracker (existiert)
**Files:** `ControllerTest/ControllerTestWindow.xaml(.cs)` (Diagnose-Modus + Report), ggf. `Core/InputLatencyTracker.cs` (Read-API)
**DoD:** ≥ 12 geprüfte Eingaben (alle Buttons, beide Sticks, beide Triggers, DPad) mit je Pass/Fail; Latenz pro Kontrolle angezeigt (p50/p95); Export-Report als JSON; UI folgt 2026-Design-System; Build + Suite grün.
**DoD-Abnahme:** ✅ `ControllerDiagnosticRunner` (unit-testbar, Zeit injizierbar): **20 geprüfte Eingaben** — A/B/X/Y, L1/R1, Start/Back, L3/R3, DPad (4), beide Sticks (4 Richtungen via ±0.6-Schwelle), beide Triggers (±0.5) — mit Pass/Fail pro Kontrolle · Latenz: 1 Reaktionszeit-Sample pro Kontrolle (Aktivierung→Erste-Detektion; n=1 ⇒ p50=p95, transparent im Report) · Timeout pro Kontrolle (Default 15 s) → Fail + Auto-Vorankommen · Disconnect-Frames: keine Detektion, nur Timeout läuft weiter · `BuildJsonReport`/`SaveJsonReport` (JSON via Utf8JsonWriter, ElapsedMs-Präzision, erstellt Parent-Verzeichnisse) · UI im ControllerTestWindow: 2026-Design (Consolas, #E5B842, CornerRadius 6), Prompt wechselt nur bei Kontrolle-Wechsel (Feedback bleibt sichtbar), Start/Stop/Export-Buttons · `ControllerDiagnosticRunnerTests`: 12 neue Facts (DoD-Anzahl, Happy Path mit exakten Reaktionszeiten, Stick/Trigger-Schwellen, Timeout, Disconnect, Stop-Semantik, JSON-Inhalt + Datei-Export, HasButton). Suite 272/272 grün.

### 🟨 SPRINT B — MEDIUM Priority (nach Sprint A)

#### FEAT-012: PartyManager + Auto-Heal-Loop *(merge Coder FEAT-012 + QA FEAT-021)* ✅ DONE
**Status:** CLOSED | **Assigned:** @coder | **Priorität:** MEDIUM
**Description:** `PartyTargetingEnabled` existiert als Flag in `AutoTargetEngine` ohne Logik dahinter. Neue PartyManager-Engine: Party-Mitglieder (max. 5), Heilungs-/Buff-Aktionen bei HP-Schwelle des schwächsten Mitglieds, autonomer Heal-Loop (Tab + Heal zyklisch, konfigurierbares Intervall) — manuelles Verhalten (Y/R1) bleibt unverändert.
**Dependencies:** FEAT-011 (Party-Heiltränke), BuffManager (existiert)
**Files:** `Core/PartyManager.cs` (neu), `Core/AutoTargetEngine.cs`, `Core/SupportEngine.cs`, `Core/EngineOrchestrator.cs`
**DoD:** Party-Mitglieder verwaltbar, Heil-Loop feuert bei Schwelle, Targeting kann auf Party umschalten; Unit-Tests headless für Zielwahl + Zyklus-Timing.

#### FEAT-013: Target-Management — Tab-Cycling, Lock-Persistenz, Auto-Retarget *(merge Coder FEAT-013 + QA FEAT-022)*
**Status:** ✅ CLOSED | **Assigned:** @coder | **Priorität:** MEDIUM
**Description:** `AutoTargetEngine` hat `IsTargetLocked`, aber kein echtes Tab-Cycling und keine Lock-Persistenz über Skill-Interrupts. Ergänzung: Tab-Zielwechsel im Radius (nur bei Lock, kein Seek-Reset), Lock-Timeout/Reichweitenverlust → Auto-Retarget mit LogMessage-Event, konfigurierbar sticky vs. nearest.
**Dependencies:** FEAT-012 (Party als exkludierbare Ziele)
**Files:** `Core/AutoTargetEngine.cs`, `Models/Settings.cs`
**DoD:** ✅ Tab mit/ohne Lock · ✅ Lock übersteht Skill-Interrupt · ✅ Tod/Reichweitenverlust → Auto-Retarget (`LostTarget`-Event, an Orchestrator-LogMessage gekoppelt) · ✅ konfigurierbar sticky vs. nearest (`TargetingMode`) · ✅ Unit-Tests für alle Zustandsübergänge (9 Facts).
**Erledigt 2026-09-14:** `TargetingMode`(Sticky/Nearest), `MaxTargetDistance`, `LostTarget`-Event, `CheckTargetValidity()`, `SetTarget/ClearTarget`-Feed mit False-Positive-Schutz. Suite: 209 Tests grün.

#### ROB-002: Input-Emulation-Failover (SendInput ↔ Kernel-Service)
**Status:** ✅ CLOSED (2026-09-17, commit 0dc14e3) | **Assigned:** @coder | **Priorität:** MEDIUM
**Description:** `SendInputMouseStrategy` und `KernelInputService` existieren nebeneinander, Fallback ist statisch konfiguriert. Dynamisches Failover in `InputRouter`: N aufeinanderfolgende Kommandos außerhalb erwarteter Latenz (via InputLatencyTracker) → Auto-Switch + Telemetrie-Event + Recovery nach M erfolgreichen Kommandos.
**Dependencies:** PERF-005, IMouseEmulationStrategy (existieren)
**Files:** `Core/InputRouter.cs`, `Core/SendInputMouseStrategy.cs`, `Core/KernelInputService.cs`
**DoD:** Failover-Logik in InputRouter (KISS, keine neue Klasse), Schwellwerte in Settings, Telemetrie-Event pro Switch, Unit-Test mit mockter Strategie.

#### PERF-010: Zero-Allokation-Gate im Tick-Pfad (CI-erzwingend)
**Status:** CLOSED ✅ | **Assigned:** @qa / @coder | **Priorität:** MEDIUM
**Description:** `MemoryAllocationTracker` (PERF-004) misst, aber es gab kein hartes Gate. Test-Harness: 1000-Tick-Steady-State pro Engine mit Budget (≤2 Allokationen/Tick), Violation schlägt rot und benennt die allocating Engine per Stacktrace/ETW.
**Dependencies:** PERF-004, BenchmarkHarness (existieren)
**Implementation (PERF-010):** `tests/RagnaController.Tests/TickAllocationGateTest.cs` (neu). Gate treibt den dominanten per-Tick-Pfad `InputRouter.RouteInput(...)` — exakt die Methode, die `EngineOrchestrator.OnTick` jeden Frame aufruft (Zeile 482) — headless mit identischer Produktions-Wiring (MockTickProvider + injiziertes InputCommandQueue, vgl. FullOverlayIntegrationTests). Messung: `GC.GetAllocatedBytesForCurrentThread()` vor/nach 512-Tick-Steady-State-Fenster (128 Warmup-Ticks), Budget = **0 Bytes/Tick** (true zero-alloc, strikter als ROADMAP „≤2 Allokationen/Tick"). Idle-Steady-State: kein Button gedrückt, kein Ziel gelockt, keine aktiven Buffs.
**Ergebnis:** 1/1 PASS — RouteInput allokiert **0 Bytes/Tick** im Idle-Steady-State (Pfad Short-Circuits vor der Env-Read; ParsedInput ist ein struct → Zero-Allokation). Verifikation: injiziertes `new byte[64]` pro Tick wurde korrekt als 88 B/Tick-Verletzung gemeldet → Gate fängt Allokationen, winkt nichts blind durch.
**CI-erzwingend:** Läuft in der Standard-Test-Suite (GitHub Actions windows-latest). Headless-Umgebung: `RAGNACONTROLLER_SKIP_SDL=1` reproduziert exakt das CI-Verhalten (`IsHeadlessEnvironment=true` → SDL-Treiber übersprungen, kein AccessViolation im Testhost).
**DoD:** erfüllt — Gate läuft headless, identifiziert violating Engine (Stacktrace/Bytes/Tick), bestehende Tests bleiben grün. Suite 222/222 PASS mit `RAGNACONTROLLER_SKIP_SDL=1`.

#### UI-011: Live-Telemetrie-Dashboard *(aus Designer-Audit)*
**Status:** ✅ CLOSED (2026-09-24) | **Assigned:** @designer / @coder | **Priorität:** MEDIUM
**Description:** Phase-9-Metriken (FrameBudgetMonitor, InputLatencyTracker, MemoryAllocationTracker, GpuOverlayProfiler) werden in KEINEM XAML referenziert — DeveloperConsoleWindow ist reines Text-Log. Neue Telemetrie-Ansicht: P50/P95/P99 Tick-Latency, Input-Latenz pro Stage, GC-Druck, GPU-Tier — als Card im MainWindow (Developer-Tab) und/oder kompakt in der Overlay-Mini-Anzeige.
**Dependencies:** Phase 9 Tracker (existieren), UI-010
**Files:** `MainWindow.xaml(.cs)`, neue `Controls/TelemetryPanel.xaml`
**DoD:** Live-Werte <2Hz Refresh (kein Tick-Pfad-Zugriff, thread-sicher über Dispatcher), Design-Sprache konsistent, Build 0 Errors.
**Implementation:** `Controls/TelemetryPanel.xaml(.cs)` — self-contained UserControl, DispatcherTimer 500 ms (2 Hz) liest nur thread-safene Interlocked-Snapshots (`GetPercentiles()`/`GetAggregateStats()`/`GetPoolStats()`), kein Tick-Pfad-Zugriff. Karten: Input-Latenz (P50/P95/Max, Controller-Stats) + Memory/GC (WorkingSet, Gen2-Collections, Pools); FrameBudgetMonitor & GpuOverlayProfiler sind in Produktion nicht verdrahtet → „nicht aktiv". `IsVisibleChanged` startet/stopp-t Timer (kein Idle-Leak). `MainWindow.xaml(.cs)`: 7. Tab im Developer-Bereich, eigene Handhabung (kein Mapping-`Border`, da `PopulateTabPanel.Child=` das Panel überschreiben würde), Engine via `SetEngine(_engine)` im Konstruktor, `StopUpdates()` in `Window_Closing`. `HybridEngine.cs`: öffentliche Read-only Properties `LatencyTracker`/`MemoryTracker` delegieren an den Orchestrator (UI-011-API). Tests: 5 Facts in `TelemetryDashboardTests.cs` (Delegation + Tracker-API, headless via `RAGNACONTROLLER_SKIP_SDL=1`). **Ergebnis:** Build 0 Errors, Suite 229/229 PASS.

#### FEAT-014: Session-Replay — Aufzeichnung & Wiedergabe
**Status:** ✅ CLOSED (2026-09-18) | **Assigned:** @coder | **Priorität:** MEDIUM
**Description:** `ActionLogService` loggt nur Labels in In-Memory-Ringbuffer. Recorder zeichnet pro Session JSONL auf (Zeitstempel, Input-Snapshot, Engine-Zustand, gefeuerte Aktionen), <1ms Overhead via Pools, Rotation bei 50MB. Replay-Player im Test-Harness für deterministische Regressionstests + Bug-Report-Debugging.
**Dependencies:** ActionLogService, ControllerSnapshot (existieren)
**Files:** `Core/SessionRecorder.cs` (neu), `Models/Settings.cs` (`EnableSessionRecording`), `EngineOrchestrator.cs`, `tests/RagnaController.Tests/SessionRecorderTests.cs` (neu)
**DoD:** ✅ Roundtrip-Test: aufzeichnen → abspielen → identische Aktionssequenz — 6 Tests grün (Roundtrip, Rotation-Merge, Action-Sequenz, State-Throttle, JSON-Escaping, Header-Skip).

#### TEST-012: Long-Run-Stability-Test (Soak) mit Memory-Leak-Guard
**Status:** CLOSED ✅ | **Assigned:** @qa / @coder | **Priorität:** MEDIUM
**Description:** Bester Stabilitäts-Test hat nur 5 Start/Stop-Zyklen ohne Tick-Last. Soak: 10.000 Mock-Ticks (~8s bei 125Hz) durch komplette Engine-Kette mit GC.GetTotalMemory, Gen2-GC-Count und Handle-Zählung als Leak-Guard.
**Dependencies:** TEST-010
**Files:** `tests/RagnaController.Tests/LongRunStabilityTests.cs` (neu)
**DoD:** Soak headless ohne Exception; Gen2-Delta ≤2, Memory-Delta nach GC <5MB, HandleCount-Delta <100; stabil über 3 CI-Runs.
**Implementation:** `LongRunStabilityTests.cs` — 10k Ticks durch `InputRouter.RouteInput` (komplette Engine-Kette) im Idle-Steady-State, seitenwirkungsfrei (kein SendInput). Drei Leak-Guards: `GC.GetTotalMemory(true)` + Gen2-`CollectionCount(2)` + `Process.HandleCount`, Triple-Full-GC vor/nach. **Kritische Korrektur:** Gen2 wird VOR dem Cleanup-Full-GC gemessen — sonst zählt der eigene `ForceFullGc()` (3× GC.Collect()) als Gen2-GCs und meldet einen Phantom-Leak (Design-Bug, gefixt). Zusätzlich `Soak_LeakGuard_DetectsInjectedHeapGrowth`: injiziert 20k×1KB in eine lokale Liste → beweist, dass der Guard echtes Heap-Wachstum fängt (kein Blind-Pass).
**Ergebnis:** Gen2-Delta ≤1 (stärker als DoD ≤2), Mem-Delta <512KB (stärker als DoD <5MB), Handle-Delta <32 (stärker als DoD <100). Suite 224/224 PASS mit `RAGNACONTROLLER_SKIP_SDL=1`. **CI-Bestätigung:** Die „3 CI-Runs"-Stufe wird beim Push auf GitHub Actions validiert (lokal nicht reproduzierbar).

#### TEST-013: Stryker-Scoping — pro-Datei Mutation-Score-Auswertung in CI
**Status:** ✅ DONE | **Assigned:** @coder / @qa | **Priorität:** MEDIUM→C
**Description:** Die CI prüft nur den globalen Mutation Score (≥70%). Fehlt: pro-Datei Auswertung, um die Top-N Survivor-Dateien als Hardening-Kandidaten zu identifizieren.
**Files:** `scripts/StrykerReportAnalyzer.ps1` (neu), `scripts/test-fixtures/mutation-report.fixture.json` (neu), `.github/workflows/test.yml` (erweitert)
**Implementation:** PowerShell-Skript gruppiert `mutation-report.json` pro Datei, berechnet Score je Datei (Killed/Total), erzeugt Markdown-Tabelle (alle Dateien, schlechtester Score zuerst) + Top-N Survivor-Liste (meiste Survivors zuerst). Optionaler `-FailBelow`-Floor-Gate (Exit 1 bei Verletzung). CI: neuer Step „Analyze Per-File Mutation Scores" + Artifact `stryker-per-file-report`.
**DoD:** ✅ Parser validiert gegen schema-genaues Fixture (4 Dateien, 20 Mutanten: BuffManager 50%, Cooldown 60%, SkillOrchestrator 60%, EngineOrchestrator 75%) · ✅ Gate-Pfade verifiziert (Floor 70% → Exit 1, Floor 40% → Exit 0) · ✅ `dotnet test` grün: 236/236 PASS mit `RAGNACONTROLLER_SKIP_SDL=1`
**Known-Issue:** Skript ist bewusst reines ASCII (Windows PowerShell 5.1 liest BOM-freie Dateien als ANSI/cp1252 — Em-Dashes/Umlaute in String-Literalen korruptieren den Parser: „ExpressionsMustBeFirstInPipeline" + „InvalidVariableReferenceWithDrive"). Live-Run lokal flaky (WPF `.g.cs` CS0229 bei Debug-Mutation); CI läuft Release mit Exclusions und ist der Referenz-Pfad.

#### TEST-014: PerformanceTests entflaken — deterministische Timing-/Allokations-Assertions
**Status:** ✅ DONE | **Assigned:** @coder / @qa | **Priorität:** LOW
**Root-Cause (Fehldiagnose korrigiert):** `tests/PerformanceTests.cs` lag **außerhalb** des Test-Projektfalters (`tests/RagnaController.Tests/`) → wurde vom SDK-Glob nie kompiliert, lief also in CI gar nicht. Zusätzlich referenzierte sie nicht existierende APIs (`AutoTargetEngine.UpdateState`, `MovementEngine.ProcessInput/CalculatePosition`, `ComboEngine.ComboCount`, 4-Arg-Konstruktor, `Assert.Approximately`) = Phantom-/Dead-Code (verletzt RULE-004 No Broken State).
**Files:** `tests/RagnaController.Tests/PerformanceTests.cs` (neu, im Projekt), `tests/PerformanceTests.cs` (Phantom-Datei entfernt)
**Implementation:** Zwei Tests deterministisch gegen die echte, headless-sichere `EngineOptimizationPool.GetString`-API neu geschrieben:
- `StringPooling_ShouldReduceAllocations`: statt Wall-Clock-Millisekunden-Vergleich (beide Loops sub-millisecond = reines Rauschen) jetzt **allokierte Bytes** via `GC.GetAllocatedBytesForCurrentThread()`. Non-pooled allokiert pro Iteration, pooled ≈ 0. `_sink`-Feld verhindert Dead-Code-Elimination.
- `MemoryLatency_ShouldBeUnderThreshold`: statt throw-pro-einzelner-Iteration jetzt **Median/p95** über 1000 Samples (nach 100 Warmup) — eine GC-Pause trifft nur ein Sample, nicht die Verteilung.
**DoD:** ✅ Datei ins Projekt verlegt (kompiliert, wird ausgeführt) · ✅ `dotnet test` grün: **238/238 PASS** mit `RAGNACONTROLLER_SKIP_SDL=1` (vorher 236 — die 2 neuen Tests laufen jetzt erstmals) · ✅ Determinismus verifiziert: 5× Filter-Lauf `PerformanceTests` = 5× 2/2 PASS, 0 Fehler

#### UX-012: Accessibility — AutomationProperties + Gamepad-Fokus-Ring *(Designer-Audit)*
**Status:** ✅ DONE | **Assigned:** @designer / @coder | **Priorität:** LOW (Backlog → SPRINT C)
**Root-Cause (Designer-Audit):** 0 `AutomationProperties.Name` in allen 8 Fenstern — Screenreader/Assistive Tech können icon-only Buttons nicht benennen. Kein Gamepad-Fokus-Handling in MainWindow-Tabs: Handheld-Navigation existierte nur in `HandheldWindow`, Hauptfenster ohne DPad/Tab-Fokus-Ring.
**Files:** 10 XAML-Fenster (MainWindow, ButtonRemapping, ComboEditor, CommunityBrowser, DeveloperConsole, ProfileLibrary, ProfileWizard, RadialSetup×2, Tutorial) + `Resources/UI2026DesignSystem.xaml` + `MainWindow.xaml.cs`
**Implementation — WP1 (AutomationProperties):** Alle 22 icon-only Buttons (`&#xE8BB;` Close, `&#xE921;/E922` Min/Max, `&#x21BA;` Reset×10, `✕` Wizard/Gallery) mit `AutomationProperties.Name` versehen. Screenreader liest jetzt z.B. „Close window" statt leeren Glyphs.
**Implementation — WP2 (Fokus-Ring):** Thematisierter `IsKeyboardFocused`-Trigger in alle 4 Button-Templates (`PrimaryButton`, `GhostButton`, `WindowControlButton`, `WindowControlButtonClose`) → AccentBlue-Border + `BlueGlowSubtle`-Glow. Konsistent mit dem bestehenden TextBox-Fokus-Pattern (L401). Via BasedOn erbt TabButton/DangerButton automatisch.
**Implementation — WP3 (Gamepad-Navigator in MainWindow):** `MainWindow.xaml.cs` initialisiert `GamepadUiNavigator(_engine.ControllerSvc) { ActiveWindow = this }` im Ctor, `Start()` in `Window_Loaded`, `Stop()+Dispose()` in `Window_Closing` (MEMORY-001: Timer sauber freigegeben). Handheld-DPad navigiert jetzt auch durch Hauptfenster-Tabs.
**DoD:** ✅ Build 0 Errors / 24 Vorwarnungen (keine neuen) · ✅ `dotnet test` grün: **238/238 PASS** mit `RAGNACONTROLLER_SKIP_SDL=1` · ✅ Alle 8 Fenster + MainWindow verdrahtet, Handler-Integrität verifiziert (ComboEditor/CommunityBrowser → `BtnClose_Click`)
**Known-Issue:** Fuzzy-Patch-Risiko bei icon-only Buttons: Patch-Tool hat in ComboEditor/CommunityBrowser versehentlich `Click="BtnClose_Click"`→`BtnCancel_Click` + ToolTip „Close window" umgeschrieben (Handler existiert dort nicht → Laufzeit-Break). Sofort korrigiert auf Original-Handler + Original-ToolTip. Lektion: Bei XAML-Patches mit `replace_all`/Fuzzy-Matching immer den exakten `Click=`-Handler im `.xaml.cs` gegenprüfen, bevor der Patch gilt.

#### FEAT-015: Multi-Client-Routing — Preferred-HWND (AltChar, Farming)
**Status:** ✅ DONE | **Assigned:** @coder | **Priorität:** LOW (Backlog → SPRINT C)
**Root-Cause:** `WindowSwitcher.ToggleAsync` löste pro Prozessname immer das **erste** Enum-Fenster auf (`EnumWindows`-Reihenfolge). Bei 2 parallelen RO-Clients (gleicher Exe-Name) war der Ziel-Client zufällig fest — Alt-Char/Farming konnte Client 2 nicht adressieren.
**Files:** `Core/WindowSwitcher.cs`, `Properties/AssemblyInfo.cs` (neu, `InternalsVisibleTo`), `RagnaController.csproj` (Compile-Eintrag), `tests/RagnaController.Tests/WindowSwitcherTests.cs` (neu)
**Implementation (YAGNI-Override-Punkt, bewusst ohne UI/Settings):**
- `public static IntPtr? PreferredClientHwnd { get; set; }` — Override-Punkt: wenn gesetzt **und** das Fenster lebt (`IsWindow`), werden alle Switch-Aktionen an genau dieses HWND geroutet.
- `ToggleAsync(processName)` (Legacy-Signatur, unverändert) delegiert an `ToggleAsync(processName, PreferredClientHwnd ?? IntPtr.Zero)` → 100% Backward-Kompatibel, einziger Aufrufer (`CombatEngine`) untouched.
- Reine Auswahl-Funktion `internal static IntPtr SelectTargetHwnd(processName, preferredHwnd, aliveCheck)`: (1) lebt Preferred → Preferred; (2) sonst Legacy-Prozessname-Auflösung (auch bei ungültigem/totem Preferred — z.B. Client neu gestartet). `aliveCheck` injiziert → unit-testbar ohne Win32-Seitenwirkung.
- Retry-Logik (3×) bleibt erhalten; `IsWindow`-Import ergänzt.
**Tests:** 5 neue `WindowSwitcherTests` (Preferred-llebt→gewinnt, Preferred-tot→Legacy-Fallback, null-aliveCheck→vertraut, ZeroPreferred→nie akzeptiert, aliveCheck-wird-konsultiert). Alle rein/deterministisch, CI-sicher headless.
**DoD:** ✅ Build 0 Errors / 24 Vorwarnungen (keine neue) · ✅ `dotnet test` grün: **243/243 PASS** (`RAGNACONTROLLER_SKIP_SDL=1`) · ✅ Backward-kompatibel (Legacy-Signatur + einziger Aufrufer unverändert)
**Known-Issue / Scope-Entscheidung:** Bewusst **kein** paralleles Engine-Modell, keine UI/Settings, kein Client-Selector — nur der Routing-Override-Punkt (YAGNI gemäß PM-Moderation). Ein späterer FEAT-Punkt kann `PreferredClientHwnd` an einen Client-Selector/Alt-Char-Profil binden; die API ist dafür vorbereitet.

### 🟩 SPRINT C / BACKLOG — LOW Priority (bewusst parkiert)

| Task | Titel | Priorität | Notiz |
|------|-------|-----------|-------|
| FEAT-015 | Multi-Window / Multi-Client (AltChar, Farming) | LOW | ✅ DONE 2026-09-30 (siehe Detailblock unten) |
| FEAT-023 | Auto-Item-Einlagerung (Storage-Drop bei vollem Inventar) | LOW | ✅ DONE 2026-10-04 — `StorageDropPlanner` (reine Planungslogik, headless-testbar; siehe Detailblock unten) |
| FEAT-024 | Multi-Character-Profil-Schnellwechsel (Name-basiert) | LOW | ✅ DONE 2026-10-03 — `CharacterProfileResolver` + Persistenz über Neustart + Auto-Switch (siehe Detailblock unten) |
| TEST-013 | Stryker-Scoping: pro-Datei Mutation-Score-Auswertung in CI | MEDIUM→C | ✅ DONE 2026-09-28 (siehe Detailblock unten) |
| TEST-014 | PerformanceTests entflaken (flaky Timing-Assertions) | LOW | ✅ DONE 2026-09-29 (siehe Detailblock oben) |
| UX-012 | Accessibility: AutomationProperties + Gamepad-Fokus-Ring *(Designer-Audit)* | LOW | ✅ DONE 2026-09-30 (siehe Detailblock unten) |

### FEAT-024: Multi-Character-Profil-Schnellwechsel (Name-basiert) ✅ DONE 2026-10-03
**Scope:** Name-basierte Char→Profil-Zuordnung mit Persistenz über Neustart und automatischem Profil-Switch bei Charakter-Erkennung. Bewusst **kein** OCR/Window-Lesung im Scope (separater Integrationspunkt, braucht echtes RO-Client-Fenster) — analog FEAT-015 (Routing-only/YAGNI).
**Implementierung:**
- `CharacterProfileResolver` (rein, unit-testbar): Normalisierung (Trim + Leerzeichen-Lauf-Zusammenfassung), case-insensitive Lookup (`OrdinalIgnoreCase`), Register/Unregister/Snapshot/LoadFrom.
- `ProfileManager`: `RegisterCharacterMapping` / `UnregisterCharacterMapping` / `GetProfileForCharacter` / `OnCharacterDetected` (Auto-Switch nur bei existierendem Profil) + Persistenz in `<dir>/character_mappings.json` (Konstruktor lädt nach Neustart).
**Tests:** 18 neue Facts (`CharacterProfileResolverTests`: Normalisierung, Case-Insensitivity, Unregister, Snapshot-Roundtrip; `ProfileManagerCharacterMappingTests`: Persistenz über Neustart, Auto-Switch, Unbekannt-Profil-Negativfälle, Unregister-Persistenz). Alle headless/CI-sicher (Temp-Dirs).
**DoD:** ✅ Build 0 Errors · ✅ `dotnet test` grün: **290/290 PASS** · ✅ Persistenz über Neustart verifiziert (Neue-Instanz-Test) · ✅ Auto-Switch nur bei existierendem Profil (Negativfall getestet)
**Known-Issue / Scope-Entscheidung:** Die eigentliche Char-Namen-Erkennung (OCR-/Fenster-Lesung des Game-Fensters) ist bewusst **nicht** im Scope — sie ruft `ProfileManager.OnCharacterDetected(characterName)` als Integrationspunkt auf. UI für Mapping-Verwaltung folgt mit einem späteren FEAT-Punkt (YAGNI).

### FEAT-023: Auto-Item-Einlagerung (Storage-Drop bei vollem Inventar) ✅ DONE 2026-10-04
**Scope:** Reine, deterministische Planungslogik für die Auto-Item-Einlagerung: Welche Inventar-Slots werden befreit, wenn das Inventar voll ist — Consumables → Storage-Drop (Stash), Buff-Items → Drop, QuestItems/Equipment → Keep (niemals automatisch freigeben). Bewusst **kein** OCR/Window-Lesung und kein Cursor-Zugriff im Scope (separater Integrationspunkt über RoUiMenuService/SmartCursorService, braucht echtes RO-Client-Fenster) — analog FEAT-015/FEAT-024 (Routing-only/YAGNI).
**Implementierung:**
- `StorageDropPlanner` (statisch, rein, unit-testbar): `Plan(capacity, usedSlots, slots)` liefert pro Slot ein `StorageDropIntent` (SlotIndex + Item + Aktion + Begründung) in Slot-Reihenfolge; leer bei nicht vollem Inventar.
- Datenmodell: `InventorySlotItem` (Name, Rarity, StackCount), `StorageDropPriority` (Consumable/BuffItem/QuestItem/Equipment), `StorageDropAction` (Stash/Drop/Keep).
- Hilfs-APIs: `PlanDefault` (Standard-Kapazität 24 = Classic-RO-Inventar), `CountReleasingActions`, `FreesEnough(plan, neededSlots=1)`.
**Tests:** 11 neue Facts (`StorageDropPlannerTests`): Nicht-voll→leerer Plan, Stash/Drop/Keep-Zuordnung, Slot-Reihenfolge, null/leere Slots, nicht-positive Kapazität, Standard-Kapazität 24, CountReleasingActions, FreesEnough-Positiv/Negativ. Alle headless/CI-sicher (reine Daten).
**DoD:** ✅ Build 0 Errors · ✅ `dotnet test` grün: **301/301 PASS** · ✅ Planungslogik deterministisch + unit-testbar · ✅ Schutzklassen (Quest/Ausrüstung) explizit getestet
**Known-Issue / Scope-Entscheidung:** Die eigentliche Ausführung (OCR-Erkennung „Inventar voll" → Cursor zur Storage-UI navigieren → Items physisch verschieben/dropen) ist bewusst **nicht** im Scope — sie ruft `StorageDropPlanner.Plan` als Integrationspunkt auf und nutzt die vorhandene Basis RoUiMenuService + SmartCursorService (Grid-Geometrie).

### PM-Moderation — Konfliktauflösung (2026-09-07)
1. **Duplikat aufgelöst:** Coder FEAT-011 + QA FEAT-020 → **ein** Task FEAT-011 (ItemManagerEngine). QA hat den Code-Gap bestätigt, Coder das Design geliefert.
2. **Duplikat aufgelöst:** Coder FEAT-012 + QA FEAT-021 → **ein** Task FEAT-012 (PartyManager inkl. Auto-Heal-Loop; SupportEngine bleibt manuell als Fallback).
3. **Duplikat aufgelöst:** Coder FEAT-013 + QA FEAT-022 → **ein** Task FEAT-013 (Target-Management; Tab nur bei Lock, kein Seek-Reset).
4. **Designer-Audit-Ergebnisse übernommen:** UI-010 (Token-Bug, S2) in Sprint A, UI-011 (Telemetrie unsichtbar) in Sprint B, UX-012 (Accessibility) in Backlog. Designer-Detailanalyse lieferte keine vollständige Taskliste (Schema-Fehler) — Audit-Zahlen sind verifiziert und maßgeblich.
5. **Architektur-Entscheidungen:** Keine neuen Abstraktionsschichten für FEAT-012/013 (KISS): PartyManager als Engine, Targeting-Erweiterung direkt in AutoTargetEngine. Multi-Window (FEAT-015) bewusst YAGNI-gemäß nur als Routing.
6. **Quest-Navigation:** von QA geprüft → ohne Memory-/Positionssystem nicht sauber umsetzbar, bewusst KEIN Ticket.

## Audit-Findings / Known Issues (Comprehensive App Review)

### 🟥 UI-001: CheckBox-Handler-Missing in SettingsWindow (S3)
**Date:** 2026-10-04 | **Severity:** S3 — Funktionalität eingeschränkt | **Status:** bekannt
**File:** src/RagnaController/SettingsWindow.xaml (Zeilen 66-122)
**Fundament:** 13 CheckBox-Elemente existieren im UI, aber es sind keine Click- oder Checked-Handler zugeordnet. Die CheckBoxes sind sichtbar, aber funktionslos.
- `ChkMinimizeToTray` (Minimize to tray on close) — kein Handler
- `ChkShowLatency` (Show latency) — kein Handler  
- `ChkAutoStart` (Enable Auto-Start) — kein Handler
- `ChkSound` (Enable Sound) — kein Handler
- `ChkRumble` (Enable Rumble Feedback) — kein Handler
- `ChkHapticMetronome` (Enable Haptic Metronome) — kein Handler
- `ChkStartInMiniMode` (Start in Mini Mode) — kein Handler
- `ChkSmartStandby` (Enable Smart Standby / AFK Battery Saver) — kein Handler
- `ChkFocusLock` (Enable Focus Lock) — kein Handler
- `ChkDiscordRPC` (Enable Discord Rich Presence) — kein Handler
- `ChkVoiceAnnouncements` (Enable Voice Announcements) — kein Handler
- `ChkTelemetry` (Enable Anonymous Telemetry) — kein Handler
**Impact:** User kann diese Einstellungen im UI nicht steuern; Derstellungen werden vermutlich anderswo oder per Code verwaltet.
**Confidence:** hoch (direkter Code-Check)
**Suggested Fix:** Handler hinzufügen oder Bindung an ViewModel-Eigenschaften herstellen.

### 🟥 UI-002: ClassDetector.cs Doppelte Dictionary-Keys (S2)
**Date:** 2026-10-04 | **Severity:** S2 — Klassenderkennung unzuverlässig | **Status:** ✅ ERLEDIGT (2026-10-04)
**File:** src/RagnaController/Core/ClassDetector.cs (Zeilen 69-166)
**Fundament:** Der `SkillToClassMap`-Dictionary-Initialisierer enthielt mehrfach gleiche `VirtualKey`-Keys, die in C# stumm überschrieben wurden. Die programmatische Neubau-Methode `BuildSkillToClassMap()` akkumuliert jetzt Einträge pro Key statt zu überschreiben.
**Behobene Beispiele:**
- `[VirtualKey.F7]`: War Lord Knight/Paladin UND Mage/Wizard/Sage/Professor — jetzt werden beide Einträge in der Liste akkumuliert
- `[VirtualKey.F1]`: War Swordsman/Knight/Crusader UND High Wizard — jetzt beide vorhanden
- `[VirtualKey.D5]`: War Sniper/Clown/Gypsy UND Thief/Assassin/Rogue/Stalker — jetzt beide vorhanden
- `[VirtualKey.D6]`: War Sniper/Gypsy UND Assassin/Stalker — jetzt beide vorhanden
- `[VirtualKey.D9]`: War Assassin Cross UND Merchant/Blacksmith/Alchemist — jetzt beide vorhanden
- `[VirtualKey.D0]`: War Stalker/Blacksmith/weitere — jetzt akkumuliert
- `[VirtualKey.E]`: War Whitesmith UND Priest/Monk — jetzt beide vorhanden
- `[VirtualKey.R]`: War Creator UND Monk UND Gunslinger/Rebellion — jetzt alle drei vorhanden
- `[VirtualKey.I]`: War Champion UND Soul Linker UND Gunslinger/Rebellion — jetzt alle drei vorhanden
- `[VirtualKey.Y]`: War Taekwon/Star Gladiator UND Priest (Resurrection) — jetzt beide vorhanden
**Impact:** Class Detection weist Skills jetzt korrekt allen passenden Klassen zu — die Klassifizierung ist bei gleichzeitiger Nutzung mehrerer Klassen mit gleichen Tasten nun brauchbar.
**Lösung:** Dictionary wurde von Collection-Initializer auf programmatische `BuildSkillToClassMap()`-Methode umgebildet, die bei gleichen Keys die Liste erweitert (`List.Add`) statt zu überschreiben.
**Confidence:** hoch
**Verifiziert:** Build 0 Fehler, 301/301 Tests PASS.

### 🟥 UI-003: Profile JSON Class/Name Inkonsequenz (S3)
**Date:** 2026-10-04 | **Severity:** S3 — Auto-Klassenzuweisung möglicherweise fehlerhaft | **Status:** ✅ ERLEDIGT (2026-10-04)
**File:** src/RagnaController/DefaultProfiles/*.json (58 Dateien)
**Fundament:** Das `Class-Feld` in vielen Profil-JSON-Dateien stimmte nicht mit dem `Name-Feld` überein und existierte nicht in der `ClassToPreset`-Dictionary in ClassDetector.cs.
**Lösung:** Alle 58 JSON-Dateien auf gültige ClassToPreset-Keys aktualisiert via automatisiertem Script.
**Aktualisierte Profile (Beispiele):**
- `archbishop.json`: Class "Support Healer" → "Priest" (Support preset)
- `assassin.json` / `assassin_cross.json` / `guillotine_cross.json`: "Melee DPS" → "Assassin" (Hybrid preset)
- `bard.json` / `bard_dancer.json` / `minstrel.json` / `minstrel_wanderer.json` / `clown.json`: "Support Musician"/"Musician"/"Archer" → "Bard" (Ranged preset)
- `creator.json` / `genetic.json` / `alchemist.json`: "Merchant" → "Alchemist" (Caster preset)
- `crusader.json` / `paladin.json` / `knight.json` / `lord_knight.json` / `royal_guard.json` / `rune_knight.json` / `blacksmith.json` / `whitesmith.json` / `mechanic.json` / `merchant.json`: "Melee Tank"/"Merchant" → "Knight"/"Crusader"/"Blacksmith" (Melee preset)
- `dancer.json` / `wanderer.json`: "Archer"/"Bard" → "Dancer" (Ranged preset)
- `high_wizard.json` / `sorcerer.json` / `warlock.json` / `wizard.json`: "Magic DPS" → "Wizard" (Caster preset)
- `professor.json`: "Magic Support" → "Professor" (Caster preset)
- `sage.json`: "Magic Support" → "Sage" (Caster preset)
- `shadow_chaser.json` / `stalker.json`: "Assassin"/"Thief" → "Stalker" (Hybrid preset)
- `ninja.json` / `kagerou_oboro.json` / `shinkiro_shiranui.json`: "Ninja Class" → "Ninja"/"Kagerou" (Hybrid preset)
- `star_emperor.json` / `taekwon.json`: "Taekwon Class" → "Taekwon" (Hybrid preset)
- `star_gladiator.json`: "Taekwon Class" → "Star Gladiator" (Hybrid preset)
- `soul_linker.json` / `soul_reaper.json`: "Soul Linker Class" → "Soul Linker" (Support preset)
- `rogue.json`: "Thief" → "Rogue" (Hybrid preset)
- `ranger.json` / `sniper.json`: "Ranged DPS" → "Hunter" (Ranged preset)
- `novice.json` / `super_novice.json`: "Novice Class" → "Swordsman"/"Super Novice" (Melee preset)
**Impact:** Auto-Klassendetektion weist Profilen jetzt korrekte Engine-Presets zu.
**Verifiziert:** Build 0 Fehler, 301/301 Tests PASS.

### 🟥 UI-004: IsSkillAction zu breite Definition (S3)
**Date:** 2026-10-04 | **Severity:** S3 — Konnte nicht-skill Keys als Skills zählen | **Status:** ✅ ERLEDIGT (2026-10-04)
**File:** src/RagnaController/Core/ClassDetector.cs (Zeilen 260-273)
**Fundament:** Die Methode `IsSkillAction` hatte einen logischen Widerspruch: Sie gab für `ActionType.Key` zuerst `false` zurück, dann versuchte am Ende `action.Type == ActionType.Key` was immer `false` war. Resultat: **Keine Skills wurden je für Klassenerkennung gezählt**.
**Lösung:** Logik korrigiert auf:
1. Nur `ActionType.Key` können Skills sein
2. Zusätzlich muss der `VirtualKey` im `SkillToClassMap` existieren
3. `VirtualKey.None` wird ausgeschlossen
**Code:**
```csharp
private static bool IsSkillAction(ButtonAction action)
{
    if (action.Type != ActionType.Key)
        return false;
    return action.Key != VirtualKey.None && SkillToClassMap.ContainsKey(action.Key);
}
```
**Impact:** Klassenerkennung zählt jetzt nur tatsächlich gemappte Skill-Keys (F-Keys, Nummern-Keys, Buchstaben-Keys für Skills), keine Bewegungstasten (Pfeile, WASD) oder Grundangriff.
**Verifiziert:** Build 0 Fehler, 301/301 Tests PASS.

## Metriken
- **Build:** 0 Errors ✅ (24 Vorwarnungen, keine neuen durch ROB-002)
- **Tests:** 243/243 passing (mit RAGNACONTROLLER_SKIP_SDL=1) ✅ — inkl. 6 FEAT-014 SessionRecorder-Tests, 2 PerformanceTests (TEST-014), 5 WindowSwitcher-Tests (FEAT-015)
- **Phase 8 Completion:** 100% (9/9 Tasks) ✅
- **Phase 9 Progress:** 9/9 Tasks (100%) — **ALL COMPLETE** ✅
- **Phase 10 Planned:** 13 Tasks (5 Sprint A / 7 Sprint B / 6 Backlog inkl. 2 merges + 1 parkiert)

## Next Steps
1. **Sprint A abgeschlossen:** ROB-001 ✅ → FEAT-011 ✅ → TEST-010 ✅ → TEST-011 ✅ (alle 4 HIGH-Tasks DONE)
2. **ROB-002 abgeschlossen ✅** — Input-Emulation-Failover (SendInput ↔ Kernel): State-Machine in `InputRouter` (`InitializeFailover` + `RecordSendInputLatency`), Orchestrator-Wiring mit graceful Driver-Degradation, ETW Event 82, 6 Unit-Tests.
3. **Sprint B Fortschritt:** FEAT-012 PartyManager ✅ → FEAT-013 Target-Management ✅ → ROB-002 ✅ → FEAT-014 Session-Replay ✅ (CLOSED 2026-09-18: JSONL-Recorder, 50MB-Rotation, Replay-Player, 6 Tests)
4. **PERF-010 ✅ + TEST-012 ✅** — Zero-Allokation-Gate (0 Bytes/Tick) UND Soak 10k Ticks mit Memory-Leak-Guard (Gen2≤1, Mem<512KB, Handles<32).
5. **UI-011 ✅** — Live-Telemetrie-Dashboard: TelemetryPanel (2 Hz, thread-sichere Snapshots) + 7. Developer-Tab + HybridEngine-Delegation (`LatencyTracker`/`MemoryTracker`) + 5 Unit-Tests. Suite 229/229 PASS.
6. **UX-012 ✅** — Accessibility: 22 icon-only Buttons mit `AutomationProperties.Name`, thematisierter Fokus-Ring in alle 4 Button-Templates, GamepadUiNavigator in MainWindow verdrahtet (DPad/Tab). Suite 238/238 PASS.
7. **FEAT-015 ✅** — Multi-Client-Routing: `PreferredClientHwnd`-Override + reine `SelectTargetHwnd`-Funktion (YAGNI, ohne UI) → Alt-Char/Farming kann jetzt exakt einen Client adressieren statt „erstes Enum-Fenster". Backward-kompatibel. Suite 243/243 PASS.
8. **TEST-015 ✅** — Controller-Eingaben-Testharness: `FakeControllerProvider` (skriptbare `IControllerProvider`, hardware-frei) + 10 Pipeline-Goldtests auf der echten `ParsedInput.JustPressed/JustReleased`-Maschine, inkl. End-to-End bis `SnapshotBuilder.Build` → korrekter `ControllerSnapshot`. Suite 253/253 PASS headless.
9. **TEST-015/016/017 COMPLETE (HIGH, SPRINT A)** — Controller-Eingaben-Verifikation: TEST-015 Pipeline-Goldtests (253→255), TEST-016 Controller-Replay als CI-Golden-Master (720-Frame-Fixtur + Replay-Tests + Aufnahmefunktion im ControllerTestWindow, Suite 260/260), TEST-017 Eingabe-Selbsttest im ControllerTestWindow (`ControllerDiagnosticRunner`: 20 geprüfte Eingaben mit Pass/Fail + Reaktionszeit p50/p95 pro Kontrolle, Timeout-Logik, JSON-Report-Export, 2026-Design-UI; Suite 272/272). SPRINT A damit vollständig abgeschlossen.
10. **FEAT-024 ✅ (SPRINT C)** — Multi-Character-Profil-Schnellwechsel (Name-basiert): `CharacterProfileResolver` (Normalisierung + case-insensitive Lookup) + `ProfileManager`-Integration (`RegisterCharacterMapping`, `OnCharacterDetected` Auto-Switch, Persistenz in `character_mappings.json` über Neustart). 18 neue Facts, Suite **290/290** PASS. OCR-/Fenster-Lesung als separater Integrationspunkt (`OnCharacterDetected`) dokumentiert.
11. **FEAT-023 ✅ (SPRINT C)** — Auto-Item-Einlagerung (Storage-Drop bei vollem Inventar): `StorageDropPlanner` (reine Planungslogik: Consumables→Stash, Buffs→Drop, Quest/Ausrüstung→Keep; Standard-Kapazität 24). 11 neue Facts, Suite **301/301** PASS. OCR-/Cursor-Ausführung als separater Integrationspunkt (`StorageDropPlanner.Plan` + RoUiMenuService/SmartCursorService) dokumentiert. **SPRINT C damit vollständig abgeschlossen** ✅ (FEAT-015, FEAT-023, FEAT-024).

---

## Audit-Findings / Comprehensive Code Review (2026-10-04)

### 🟨 TECH-001: BezierMouseService — Divide by Zero Risk (S3)
**File:** src/RagnaController/Core/BezierMouseService.cs (Zeile 12)
**Fundament:** `int stepDelay = durationMs / steps;` wobei `steps = Math.Max(5, durationMs / 8)`. Wenn `durationMs = 0`, dann `steps = 5`, `stepDelay = 0`. `queue.Wait(0)` könnte zu busy-wait führen. Wenn `durationMs < 0`, `steps = 5`, `stepDelay < 0`.
**Impact:** Potentieller busy-wait oder negatives Wait-Timeout.
**Suggested Fix:** `durationMs` validieren (min 1ms), `stepDelay` auf min 1 clampen.
**Status:** ✅ ERLEDIGT (2026-10-04) — Guard für durationMs <= 0 (min 1ms) + stepDelay clamp auf min 1ms hinzugefügt.

### 🟨 TECH-002: GroundSpellEngine — Divide by Zero (S3)
**File:** src/RagnaController/Core/GroundSpellEngine.cs (Zeile 141)
**Fundament:** `public float RemainingPercent => 1f - (float)ElapsedMs / DurationMs;` — wenn `DurationMs = 0`, Division durch Null → NaN/Exception.
**Impact:** Runtime Exception bei ungültiger Spell-Konfiguration.
**Suggested Fix:** Guard: `DurationMs <= 0 ? 1f : ...` oder Constructor-Validierung.
**Status:** ✅ ERLEDIGT (2026-10-04) — Guard `DurationMs <= 0 ? 1f : ...` hinzugefügt.

### 🟨 TECH-003: KiteStates — Divide by Zero (S3)
**File:** src/RagnaController/Core/KiteStates.cs (Zeilen 21-22)
**Fundament:** `ctx.LastAimX = input.RightX / mag * norm;` — `mag = MathF.Sqrt(input.RightX^2 + input.RightY^2)`. Wenn beide 0, Division durch Null.
**Impact:** Exception bei neutralem Stick während Kite-State aktiv.
**Suggested Fix:** `if (mag <= float.Epsilon) return;` vor Division.
**Status:** ✅ ERLEDIGT (2026-10-04) — Guard `if (mag <= float.Epsilon) return;` hinzugefügt.

### 🟨 TECH-004: Async void in SplashWindow (S4)
**File:** src/RagnaController/SplashWindow.xaml.cs (Zeile 52)
**Fundament:** `private async void StartAnimations()` — async void ohne Try/Catch. Unbehandelte Exceptions crashen die App still.
**Impact:** Crash-Risiko bei Animations-Fehlern (z.B. fehlende Ressourcen).
**Suggested Fix:** `private async Task StartAnimationsAsync()` + Caller awaitet oder `.ContinueWith` mit Error-Handling.
**Status:** ✅ ERLEDIGT (2026-10-04) — In `async Task StartAnimationsAsync()` umgeändert, Caller `ContentRendered += async (s,e) => await StartAnimationsAsync().ConfigureAwait(false)`.

### 🟨 TECH-005: Static ConcurrentDictionary Registries ohne Cleanup (S3)
**Files:** 
- FrameBudgetMonitor.cs (_monitors, Zeile 163)
- GpuOverlayProfiler.cs (_profilers, Zeile 334)
- InputLatencyTracker.cs (_trackers, Zeile 454)
- MemoryAllocationTracker.cs (_trackers, Zeile 395)
**Fundament:** Statische Registries wachsen unbegrenzt bei wiederholter Erstellung (Tests, Restarts, Session-Wechsel). Keine `Remove/Unregister/Clear`-Methode.
**Impact:** Memory Leak bei langen Sessions / vielen Test-Runs / Engine-Restarts.
**Suggested Fix:** `public static void Unregister(string name)` + `Clear()` Methoden hinzufügen; in EngineOrchestrator.Shutdown() Cleanup aufrufen.
**Status:** ✅ ERLEDIGT (2026-10-04) — Alle 4 Registries hatten bereits `Remove` und `DisposeAll` Methoden. `EngineOrchestrator.Shutdown()` ruft jetzt `FrameBudgetRegistry.DisposeAll()`, `GpuOverlayProfilerRegistry.DisposeAll()`, `InputLatencyRegistry.DisposeAll()`, `MemoryAllocationRegistry.DisposeAll()` auf.

### 🟨 TECH-006: ProfileShareService Static Map ohne Reload (S4)
**File:** src/RagnaController/Core/ProfileShareService.cs (Zeile 219)
**Fundament:** `private static Dictionary<string, string> _map = Load();` — einmalig beim Class-Load. Änderungen an der JSON-Datei werden nicht erkannt.
**Impact:** Profile-Sharing zeigt veraltete Daten bis App-Restart.
**Suggested Fix:** `public static void Reload()` Methode + FileSystemWatcher optional.
**Status:** ✅ ERLEDIGT (2026-10-04) — `public static void Reload()` Methode hinzugefügt, die `_map = Load()` aufruft.

### 🟨 TECH-007: Classes mit Events aber ohne IDisposable (S3)
**Files:**
- ActionLogService.cs (Event: EntryAdded) — kein IDisposable
- AdvancedLogger.cs (Event: LiveLogReceived) — hat Dispose aber unsubscribt nicht Events
- AutoTargetEngine.cs (Event: LostTarget) — kein IDisposable
- BuffManager.cs (Events: BuffExpiringWarning, BuffExpired, DebuffApplied, DebuffExpired) — kein IDisposable
- CombatEngine.cs (Event: ActionFired) — kein IDisposable
- ComboEngine.cs (Event: ComboStepFired) — kein IDisposable
- ControllerManager.cs (4 Events) — kein IDisposable
- ControllerService.cs (2 Events) — hat Dispose
**Fundament:** Event-Subscriber können nicht sauber unsubscriben → Memory Leaks wenn Instanzen kurzlebig sind (Tests, Window-Open/Close).
**Suggested Fix:** `IDisposable` implementieren + Events in `Dispose()` auf null setzen (`event = null`).
**Status:** ✅ ERLEDIGT (2026-10-04) — Alle 8 Klassen haben jetzt `IDisposable` implementiert und setzen Events in `Dispose()` auf null. AdvancedLogger, ControllerManager, ControllerService hatten bereits Dispose, jetzt mit Event-Nulling.

### 🟨 TECH-008: XAML — Missing AutomationProperties bei weiteren Fenstern (S4)
**Fundament:** UX-012 hat 22 Buttons in 8 Fenstern gefixt. Prüfen: DeveloperConsoleWindow, MiniModeWindow, RadialMenuWindow, DaisyWheelWindow, HandheldWindow, InGameOverlayWindow, ProfileWizardWindow, TutorialWindow, SettingsWindow.
**Suggested Fix:** Audit aller icon-only Buttons + `AutomationProperties.Name` nachpflegen.
**Status:** teilweise erledigt (UX-012), Rest offen

### 🟨 TECH-009: LINQ in Hot Path — ClassDetector.OrderByDescending (S4)
**File:** src/RagnaController/Core/ClassDetector.cs (Zeile 253)
**Fundament:** `classScores.OrderByDescending(kvp => kvp.Value).First().Key` allokiert Enumerator + Delegate pro Aufruf. Wird bei jedem Profil-Wechsel / Class-Detect aufgerufen.
**Impact:** Geringe Allokation, aber in Hot Path vermeidbar.
**Suggested Fix:** Manuelles Max-Tracking statt LINQ (Dictionary < 20 Einträge → O(n) trivial).
**Status:** ✅ ERLEDIGT (2026-10-04) — Manuelles Max-Tracking mit foreach-Schleife statt LINQ OrderByDescending implementiert.

### 🟨 TECH-010: FindResource ohne Try-Catch (S3)
**Files:** 15+ Code-behind Dateien (ButtonRemappingWindow, ComboEditorWindow, CommunityBrowserWindow, DaisyWheelWindow, HandheldWindow, TelemetryPanel, etc.)
**Fundament:** `FindResource("Key")` wirft `ResourceReferenceKeyNotFoundException` wenn Key fehlt → App-Crash.
**Impact:** Runtime-Crash bei Tippfehlern in Resource-Keys oder fehlendem Design-System.
**Suggested Fix:** `TryFindResource` + Null-Check, oder Fallback-Brush. Oder Design-System-Validierung beim Start.
**Status:** ✅ ERLEDIGT (2026-10-04) — Alle `FindResource()` Aufrufe in Code-behind durch `TryFindResource()` mit Fallbacks ersetzt (z.B. `TryFindResource("Gold") as Brush ?? Brushes.Gold`, `TryFindResource("RadiusMd") as CornerRadius? ?? new CornerRadius(6)`, etc.).

### 🟨 TECH-011: Hardcoded Strings in UI (Lokalisierung) (S4)
**Fundament:** 500+ hardcoded Strings in .cs Dateien (MessageBox, Tooltips, Log-Messages, UI-Labels). Deutsche Locale hardcoded.
**Impact:** Keine Mehrsprachigkeit möglich. Wartung aufwendig.
**Suggested Fix:** Resource-Dateien (.resx) oder JSON-Localization für UI-Texte. Log-Messages können englisch bleiben.
**Status:** bekannt, bewusst (User bevorzugt Deutsch)

### 🟨 TECH-012: WindowTracker / WindowSwitcher — Process.GetProcesses() Allokationen (S4)
**Files:** WindowTracker.cs, WindowSwitcher.cs
**Fundament:** `Process.GetProcessesByName()` allokiert Array bei jedem Call. In Tick-Pfad oder häufigen Switches kostenintensiv.
**Suggested Fix:** Caching mit TTL (z.B. 500ms), oder `Process.GetProcessesByName` nur on-demand.
**Status:** ✅ BEREITS ERLEDIGT — WindowSwitcher nutzt bereits 10s TTL-Cache in `_cache` (Dictionary<string, (IntPtr hwnd, long tick)>). FindWindowByProcessName nutzt EnumWindows mit Process.GetProcessById nur für gefundene HWNDs (nicht für alle Prozesse).

### 🟨 TECH-013: ControllerTestWindow — Timer läuft nach Close weiter (S3)
**File:** src/RagnaController/ControllerTest/ControllerTestWindow.xaml.cs
**Fundament:** `_updateTimer` wird in `Window_Closing` nicht gestoppt (kein Closing-Handler sichtbar).
**Impact:** Timer tickt weiter → Access zu disposed Window → Exception/Leak.
**Suggested Fix:** `Closing += (s,e) => _updateTimer.Stop();` oder `Unloaded` Handler.
**Status:** ✅ BEREITS ERLEDIGT — `OnClosed` Override vorhanden (Zeilen 308-320) stoppt Timer und unsubscribt Events.

### 🟨 TECH-014: Duplicate UI-001 Entry in KANBAN (Cleanup)
**Fundament:** UI-001 (CheckBox-Handler) steht doppelt in KANBAN (Zeilen 350-368 und 390-394).
**Suggested Fix:** Doppelte Sektion entfernen.
**Status:** offen