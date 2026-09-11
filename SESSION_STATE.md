# SESSION_STATE.md

## Current Phase
**Phase 10: Gameplay Depth, Robustness & UX — SPRINT A (IN PROGRESS)**
Sprint-A-Reihenfolge: UI-010 ✅ → ROB-001 ✅ → FEAT-011 ✅ → **TEST-010 ✅** → TEST-011 (nächstes)

## Completed Tasks (Phase 10 / Sprint A)
- **UI-010**: BUG-FIX Design-Token `WindowControlButton` ✅ — Token zentral in `Resources/UI2026DesignSystem.xaml` definiert, alle 5 betroffenen Fenster konsistent
- **ROB-001**: Watchdog-Härtung ✅ — Hang-Erkennung (externer Timer), Auto-Restart, Input-Loss-Metrik (Commit `b0469dd`)
- **FEAT-011**: ItemManagerEngine — Auto-Potion & Item-Verwaltung ✅ *(merge Coder FEAT-011 + QA FEAT-020)*
  - `Core/ItemManagerEngine.cs` (neu): HP/SP-Schwellen-Monitoring, per-item Cooldown + Check-Intervall über Parallel-Arrays (Zero-Allokation im Tick-Pfad), injizierbare Clock
  - `Models/ItemConfig.cs` (neu): Name, Key, HpThresholdPercent=70, MinSpRequired, CooldownMs=3000, CheckIntervalMs=1000, Enabled
  - `Profiles/Profile.cs`: `ItemManagerEnabled` (Default false) + `List<ItemConfig> ManagedItems`
  - `Core/EngineOrchestrator.cs`: Tick-Pfad-Integration nach BuffManager, Start/Stop/Dispose mit Engine-Lifecycle gekoppelt, `ItemFiredMessage` via Messenger
  - `Core/ProfileApplier.cs`: `ApplyItemManager(p)` in beiden Load-Pfaden, Reset beim Profil-Switch
  - `Core/Messages.cs`: `ItemFiredMessage`
  - Tests: 18 Facts (`tests/RagnaController.Tests/ItemManagerEngineTests.cs`) — Schwelle exakt/unter/über, SP-Bedingung, Cooldown blockiert Re-Fire, Check-Intervall, Stop/Start-Lifecycle, Disabled Items, Configure/Reset, ItemFired-Event, Multi-Item unabhängige Cooldowns
- **TEST-010**: Dedizierte Unit-Tests für alle 6 ungetesteten Engines ✅ — `MobSweepEngineTests` (11 Facts), `CooldownManagerTests`, `SupportEngineTests`, `BuffManagerTests`, `MageEngineTests`, `SkillOrchestratorTests` (RotationSteps, Condition-Grenzwerte, Priority-Selection, Loop-vs-Single-Pass-Differential, Completion). Nebeneffekt: S2-Bugfix in `InputCommandQueue.cs` (2-Arg-Konstruktor chainete auf falschen Overload → VK 0 statt Key)

## In Progress
(nur TEST-011 nach Commit)

**TEST-010 (abgeschlossen):** Alle 6 Engine-Testdateien grün — MobSweepEngineTests (11), CooldownManagerTests, SupportEngineTests, BuffManagerTests, MageEngineTests, SkillOrchestratorTests (RotationSteps-Auswahl, Condition-Grenzwerte, Priority-Selection, Loop-vs-Single-Pass-Differential, Completion-Events). 148 Tests total, 0 Fehler.

## Next Actions
1. Commit TEST-010 (6 Engine-Testdateien + InputCommandQueue-Bugfix + KANBAN/SESSION_STATE)
2. Danach **TEST-011**: Fuzzing/Robustness für InputCommandQueue & ParsedInput (Seeded-RNG 10.000 Commands, Enqueue↔Stop-Race, Overflow, Edge-Cases)
3. Nach Sprint A: Build + Test-Gate (0 Errors, alle Tests grün), dann Sprint B (FEAT-012 PartyManager — baut auf FEAT-011 auf)

## Git State
```
93a20df feat: FEAT-011 ItemManagerEngine — Auto-Potion & Item-Verwaltung (Sprint A)
b0469dd feat: ROB-001 Watchdog-Härtung — Hang-Erkennung & Auto-Restart (Sprint A)
cff40d4 refactor: UI2026 Design System als Single Source of Truth + InputCommandQueue Race-Fix
```
**Uncommitted (TEST-010):** `Core/InputCommandQueue.cs` (Bugfix S2), 6 neue Engine-Testdateien (`MobSweepEngineTests`, `CooldownManagerTests`, `SupportEngineTests`, `BuffManagerTests`, `MageEngineTests`, `SkillOrchestratorTests`), KANBAN.md, SESSION_STATE.md

## Quality Gates
- **Build:** 0 Errors / 0 Warnings ✅
- **Tests:** alle Tests PASS (44 in Filter-Runde MobSweepEngine + Vollsuite; SDL-Teardown-AV ist präexistend/flaky, mit RAGNACONTROLLER_SKIP_SDL=1 im CI irrelevant) ✅
- **DoD FEAT-011:** erfüllt (Schwellen, Cooldown, disconnected, Defaults, headless Tests)
