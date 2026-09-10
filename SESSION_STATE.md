# SESSION_STATE.md

## Current Phase
**Phase 10: Gameplay Depth, Robustness & UX — SPRINT A (IN PROGRESS)**
Sprint-A-Reihenfolge: UI-010 ✅ → ROB-001 ✅ → **FEAT-011 ✅** → TEST-010 (nächstes) → TEST-011

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

## In Progress
- **TEST-010**: Dedizierte Unit-Tests für ungetestete Engines (SkillOrchestrator, BuffManager, CooldownManager, SupportEngine, MageEngine, MobSweepEngine) — als Nächstes in Sprint A

## Next Actions
1. **TEST-010** starten: min. 5 Facts pro Engine (Step-Auswahl, Condition-Grenzwerte, Loop vs. Single-Pass, Warnungs-Event exakt einmal, AutoRecast via Mock-Queue)
2. Danach **TEST-011**: Fuzzing/Robustness für InputCommandQueue & ParsedInput
3. Nach Sprint A: Build + Test-Gate (0 Errors, alle Tests grün), dann Sprint B (FEAT-012 PartyManager — baut auf FEAT-011 auf)

## Git State
```
b0469dd feat: ROB-001 Watchdog-Härtung — Hang-Erkennung & Auto-Restart (Sprint A)
cff40d4 refactor: UI2026 Design System als Single Source of Truth + InputCommandQueue Race-Fix
```
**Uncommitted (FEAT-011):** `Core/ItemManagerEngine.cs` (neu), `Models/ItemConfig.cs` (neu), `Core/EngineOrchestrator.cs`, `Core/Messages.cs`, `Core/ProfileApplier.cs`, `Profiles/Profile.cs`, `Profiles/AppJsonContext.cs`, `RagnaController.csproj`, `tests/RagnaController.Tests/ItemManagerEngineTests.cs` (neu), ROADMAP.md, KANBAN.md

## Quality Gates
- **Build:** 0 Errors / 0 Warnings ✅
- **Tests:** 87/87 passing (mit RAGNACONTROLLER_SKIP_SDL=1) ✅ — davon 18 neu für FEAT-011
- **DoD FEAT-011:** erfüllt (Schwellen, Cooldown, disconnected, Defaults, headless Tests)
