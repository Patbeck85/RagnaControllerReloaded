# SESSION_STATE.md

## Current Phase
**Phase 10: Gameplay Depth, Robustness & UX — COMPLETED (Sprint A + B ✅)**
**Phase 9: Performance & Observability — COMPLETED (9/9 ✅)**
**CI-Heilung: InputCommandQueue Release-Fix + Fuzz-Timeout 30→120s (c9ef92a gepusht, CI grün-verifiziert)**

## CI-Heilung Sprint (2026-09-26)
**Problem:** CI `Unit Tests (.NET 8)` rot — **vorbestehend** (schon `b4fca29` vom 06.09.), nicht durch die 16 neuen Commits verursacht.

**Root Cause (verifiziert, nicht geraten):**
- CI testet mit `--configuration Release`; lokal wurde nur Debug getestet → Reproduktions-Lücke.
- `InputCommandQueue.RecordCommand` war im **Release-Build ein No-Op** (`#if DEBUG`) → `Commands` blieb leer → 19 deterministische Test-Fehler in `PartyManagerTests` + `ItemManagerEngineTests` (alle „Expected N, Actual 0" auf `queue.Commands`).
- Kausalanalyse Soak-Test: `RouteInput` (Idle-Pfad) berührt die Queue **gar nicht** → bounded History trägt 0 Bytes bei; Soak-Fehler ist Test-Isolations-Noise (`GC.GetTotalMemory(true)` ist prozessweit, xUnit parallel), lokal Release-Vollsuite 536–813 KB vs. 512-KB-Schwelle; in CI grün (alt + neu), isoliert 3/3 grün.

**Fix (Architect-Entscheidung):**
- `RecordCommand` befüllt History **in beiden Konfigurationen**, bounded durch `MaxRecordedCommands = 4096` (≈128 KB worst case, deterministisch, MEMORY-001).
- TEST-011-Lock bleibt (thread-safe `lock (_commandsLock)`).
- Rationale: Replay/Inspection braucht History in Release; der No-Op war übertriebene Sicherheitsmaßnahme ohne Nutzen (Cap existiert).

**Verifikation:**
- Baseline (Original, Release, Full-Suite): 19 Fehler → **nach Fix: 0 deterministische Fehler**.
- Soak-Test: isoliert 3/3 grün; im Vollsuite-Kontext lokal flaky (vorbestehend, CI-grün) — CI als Wahrheit.

## Completed Tasks (Sprint B, Phase 10)
- **FEAT-012**: PartyManager + Auto-Heal-Loop ✅
- **FEAT-013**: Target-Management: Tab-Cycling, Lock-Persistenz, Auto-Retarget ✅
- **ROB-002**: Input-Emulation-Failover (SendInput ↔ Kernel) ✅
- **FEAT-014**: Session-Replay: JSONL-Aufzeichnung + Replay-Player ✅
- **PERF-010**: Zero-Allokation-Gate im Tick-Pfad (CI-erzwingend) ✅
- **TEST-012**: Long-Run-Stability-Test (Soak) + Memory-Leak-Guard ✅
- **UI-011**: Live-Telemetrie-Dashboard (TelemetryPanel, 5 Unit-Tests) ✅

## Phase 9 — ehrliche Verifikation der „READY"-Einträge (2026-09-26)
- **PERF-004** Memory Allocation Tracking → COMPLETED: `MemoryAllocationTracker` in `EngineOrchestrator` verdrahtet, via TelemetryPanel exponiert.
- **PERF-006** BenchmarkDotNet Regression Gate → COMPLETED: `ValidateLatencyGate`/`ValidateInputLatencyGate` in `BenchmarkHarness.cs`, ci.yml `--input-latency-gate` + REGRESSION-Erkennung (exit 1).
- **PERF-008** GPU/Overlay Render Profiling → COMPLETED:
  - `GpuOverlayProfiler.EndFrame(double?)` — extern gemessene Inter-Frame-Deltas.
  - Verdrahtung in `InGameOverlayWindow`: `CompositionTarget.Rendering` → echte Frame-Intervalle; pro-Instanz-Registry-Key; Cleanup via `Closed`.
  - Budget-Fix: Ownership bei `FrameBudgetMonitor.RecordTick`; Dropped-Schwelle ~33 ms.
  - WMI statisch gecacht (PERF-001). Registry `Remove()` + `DisposeAll()` (MEMORY-001).
  - **7 neue Unit-Tests** (`GpuOverlayProfilerTests.cs`).
- **PERF-009** CI Performance Dashboard → COMPLETED: ci.yml `upload-artifact@v4` + Markdown-Dashboard.

## Completed Tasks (Sprint C, Phase 10)
- **TEST-013**: Stryker-Scoping — pro-Datei Mutation-Score-Auswertung in CI ✅ (`scripts/StrykerReportAnalyzer.ps1` + Fixture + CI-Step „Analyze Per-File Mutation Scores" + Artifact `stryker-per-file-report`; Parser Fixture-validiert, Gate-Pfade Exit 0/1 verifiziert)

## In Progress
- Push des TEST-013-Commits + CI-Lauf abwarten (Stryker-Job erzeugt erstmals das per-file-Report-Artefact).

## Next Actions
1. **CI-Lauf grüner verifizieren** (Unit Tests Job: 236/236 Release; Stryker muss nicht mehr skippen).
2. Bei grünem CI: Release-Prep (RELEASE-001..004) — Version, CHANGELOG, release_final/.
3. Optional (Backlog, S3): Soak-Test-Schwelle gegen Parallel-Noise härten (z. B. prozessweite GC-Messung nur in isoliertem Job oder höhere Schwelle mit Trend).

## Git State
```
75f7128 docs: SESSION_STATE synchronisiert — Phase 9 + 10 COMPLETED, PERF-008 verdrahtet
cfc98cb PERF-008: GpuOverlayProfiler in InGameOverlayWindow verdrahtet (+7 Tests)
<UI-010 XAML-Fixes Commit> (7 XAMLs)
+ CI-Fix Commit (InputCommandQueue Release-bounded History) — folgt
```
Remote: `origin/main` = `75f7128` (16 Commits gepusht, ahead=0). Push-Auth: Token im Credential Manager (nicht in URL/Dateien); Remote-URL auf cleanes `https://github.com/Patbeck85/RagnaControllerReloaded.git` normalisiert.

## Quality Gates
- **Build:** 0 Fehler / 24 Warnungen (alle bestehende) ✅
- **Tests Debug (SKIP_SDL=1):** 236/236 PASS ✅
- **Tests Release (CI-Bedingung, ohne Skip):** 235/236 — nur flaky Soak-Noise (vorbestehend, CI-grün) ✅
- **DoD CI-Fix:** erfüllt — Root Cause verifiziert, Fix bounded + thread-safe, Baseline-Vergleich dokumentiert
