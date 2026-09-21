# SESSION_STATE.md

## Current Phase
**Phase 10: Gameplay Depth, Robustness & UX — SPRINT B (IN PROGRESS)**
Sprint-B-Reihenfolge: FEAT-012 ✅ → FEAT-013 ✅ → ROB-002 ✅ → PERF-010 ✅ → **TEST-012 ✅** → UI-011 (nächstes) → FEAT-014

## Completed Tasks (Sprint B, Phase 10)
- **FEAT-012**: PartyManager + Auto-Heal-Loop ✅
- **FEAT-013**: Target-Management: Tab-Cycling, Lock-Persistenz, Auto-Retarget ✅
- **ROB-002**: Input-Emulation-Failover (SendInput ↔ Kernel) ✅ — State-Machine in `InputRouter` (`InitializeFailover` + `RecordSendInputLatency`), Orchestrator-Wiring mit graceful Driver-Degradation, ETW Event 82, 6 Unit-Tests
- **FEAT-014**: Session-Replay: JSONL-Aufzeichnung + Replay-Player ✅ (CLOSED 2026-09-18)
- **PERF-010**: Zero-Allokation-Gate im Tick-Pfad (CI-erzwingend) ✅
  - `tests/RagnaController.Tests/TickAllocationGateTest.cs` (neu): treibt den dominanten per-Tick-Pfad `InputRouter.RouteInput(...)` — exakt die Methode, die `EngineOrchestrator.OnTick` jeden Frame aufruft (Zeile 482) — headless mit identischer Produktions-Wiring.
  - Messung: `GC.GetAllocatedBytesForCurrentThread()` vor/nach 512-Tick-Steady-State-Fenster (128 Warmup), Budget = **0 Bytes/Tick** (true zero-alloc). Ergebnis: RouteInput allokiert **0 Bytes/Tick**. Verifikation: injiziertes `new byte[64]`/Tick korrekt als 88 B/Tick-Verletzung gemeldet.
- **TEST-012**: Long-Run-Stability-Test (Soak) + Memory-Leak-Guard ✅
  - `tests/RagnaController.Tests/LongRunStabilityTests.cs` (neu): 10k Ticks durch `InputRouter.RouteInput` (komplette Engine-Kette) im Idle-Steady-State, seitenwirkungsfrei (kein SendInput). Drei Leak-Guards: `GC.GetTotalMemory(true)` + Gen2-`CollectionCount(2)` + `Process.HandleCount`, Triple-Full-GC vor/nach.
  - **Kritische Korrektur:** Gen2 wird VOR dem Cleanup-Full-GC gemessen — sonst zählt der eigene `ForceFullGc()` (3× GC.Collect()) als Gen2-GCs und meldet einen Phantom-Leak (Design-Bug, gefixt).
  - Ergebnis: Gen2-Delta ≤1 (stärker als DoD ≤2), Mem-Delta <512KB (stärker als DoD <5MB), Handle-Delta <32 (stärker als DoD <100).
  - `Soak_LeakGuard_DetectsInjectedHeapGrowth`: injiziert 20k×1KB in eine lokale Liste → beweist, dass der Guard echtes Heap-Wachstum fängt (kein Blind-Pass).

## In Progress
(nichts — TEST-012 abgeschlossen)

## Next Actions
1. Commit TEST-012 (LongRunStabilityTests.cs + ROADMAP.md + KANBAN.md + SESSION_STATE.md)
2. Danach **UI-011**: Live-Telemetrie-Dashboard (Phase-9-Metriken in UI sichtbar machen) — benötigt Phase 9 Tracker, UI-010

## Git State
```
aef9183 feat: PERF-010 Zero-Allokation-Gate im Tick-Pfad (CI-erzwingend)
93a20df feat: FEAT-011 ItemManagerEngine — Auto-Potion & Item-Verwaltung (Sprint A)
b0469dd feat: ROB-001 Watchdog-Härtung — Hang-Erkennung & Auto-Restart (Sprint A)
```
**Uncommitted (TEST-012):** `tests/RagnaController.Tests/LongRunStabilityTests.cs` (neu), ROADMAP.md, KANBAN.md, SESSION_STATE.md

## Quality Gates
- **Build:** 0 Errors / 0 Warnings ✅
- **Tests:** 224/224 PASS mit `RAGNACONTROLLER_SKIP_SDL=1` (reproduziert CI-Verhalten) ✅
- **DoD TEST-012:** erfüllt — Soak headless ohne Exception; Gen2-Delta ≤1, Memory-Delta <512KB, HandleCount-Delta <32 (alle strenger als DoD-Schwelle); Guard-Verifikation (injected heap growth detected)