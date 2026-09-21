# SESSION_STATE.md

## Current Phase
**Phase 10: Gameplay Depth, Robustness & UX — SPRINT B (IN PROGRESS)**
Sprint-B-Reihenfolge: FEAT-012 ✅ → FEAT-013 ✅ → ROB-002 ✅ → **PERF-010 ✅** → TEST-012 (nächstes) → UI-011 → FEAT-014

## Completed Tasks (Sprint B, Phase 10)
- **FEAT-012**: PartyManager + Auto-Heal-Loop ✅
- **FEAT-013**: Target-Management: Tab-Cycling, Lock-Persistenz, Auto-Retarget ✅
- **ROB-002**: Input-Emulation-Failover (SendInput ↔ Kernel) ✅ — State-Machine in `InputRouter` (`InitializeFailover` + `RecordSendInputLatency`), Orchestrator-Wiring mit graceful Driver-Degradation, ETW Event 82, 6 Unit-Tests
- **FEAT-014**: Session-Replay: JSONL-Aufzeichnung + Replay-Player ✅ (CLOSED 2026-09-18)
- **PERF-010**: Zero-Allokation-Gate im Tick-Pfad (CI-erzwingend) ✅
  - `tests/RagnaController.Tests/TickAllocationGateTest.cs` (neu): treibt den dominanten per-Tick-Pfad `InputRouter.RouteInput(...)` — exakt die Methode, die `EngineOrchestrator.OnTick` jeden Frame aufruft (Zeile 482) — headless mit identischer Produktions-Wiring (MockTickProvider + injiziertes InputCommandQueue).
  - Messung: `GC.GetAllocatedBytesForCurrentThread()` vor/nach 512-Tick-Steady-State-Fenster (128 Warmup), Budget = **0 Bytes/Tick** (true zero-alloc, strikter als ROADMAP „≤2 Allokationen/Tick"). Idle-Steady-State: kein Button, kein Ziel gelockt, keine aktiven Buffs.
  - Ergebnis: 1/1 PASS — RouteInput allokiert **0 Bytes/Tick** im Idle-Steady-State. Verifikation: injiziertes `new byte[64]`/Tick wurde korrekt als 88 B/Tick-Verletzung gemeldet → Gate fängt Allokationen, winkt nichts blind durch.
  - CI-erzwingend: läuft in der Standard-Suite (GitHub Actions windows-latest). `RAGNACONTROLLER_SKIP_SDL=1` reproduziert exakt das CI-Verhalten (`IsHeadlessEnvironment=true` → SDL übersprungen, kein AccessViolation im Testhost).

## In Progress
(nichts — PERF-010 abgeschlossen, TEST-012 als nächstes)

## Next Actions
1. Commit PERF-010 (TickAllocationGateTest.cs + ROADMAP.md + KANBAN.md + SESSION_STATE.md)
2. Danach **TEST-012**: Long-Run-Stability-Test (Soak): 10k Ticks + Memory-Leak-Guard — baut auf dem neuen Gate auf (gleiche `GC.GetAllocatedBytesForCurrentThread`-Technik, längeres Fenster + Leak-Delta)
3. Danach **UI-011**: Live-Telemetrie-Dashboard

## Git State
```
93a20df feat: FEAT-011 ItemManagerEngine — Auto-Potion & Item-Verwaltung (Sprint A)
b0469dd feat: ROB-001 Watchdog-Härtung — Hang-Erkennung & Auto-Restart (Sprint A)
cff40d4 refactor: UI2026 Design System als Single Source of Truth + InputCommandQueue Race-Fix
```
**Uncommitted (PERF-010):** `tests/RagnaController.Tests/TickAllocationGateTest.cs` (neu), ROADMAP.md, KANBAN.md, SESSION_STATE.md

## Quality Gates
- **Build:** 0 Errors / 0 Warnings ✅
- **Tests:** 222/222 PASS mit `RAGNACONTROLLER_SKIP_SDL=1` (reproduziert CI-Verhalten) ✅
- **DoD PERF-010:** erfüllt — Gate implementiert, 0 Allokationen/Tick gemessen, Messung verifiziert (injected alloc detected), Suite grün
