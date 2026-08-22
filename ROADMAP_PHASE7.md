# Phase 7: Action RPG Completeness & Class Support

## FEAT-005: Full Class Engine Presets
- **Goal:** Define complete EnginePreset configurations for every RO class (Swordsman, Knight, Crusader, Blacksmith, Archer, Hunter, Bard, Dancer, Gunslinger, Rebellion, Mage, Wizard, Sage, Professor, Alchemist, Thief, Assassin, Rogue, Stalker, Acolyte, Priest) with appropriate engine priority, targeting, and skill routing.
- **Files:** `ClassDetector.cs` → expand `EnginePreset` enum with per-class fields; create `ClassPresetLoader` to load preset configs.
- **Definition of Done:** Every class has a preset → auto-class detection sets correct engine configs on profile load.

## FEAT-006: Ground Spell / AoE Skill System
- **Goal:** Support heal/slow/ground-control spells that place persistent AOEs on the ground (e.g., Stone Curse radius, Healing Circle, Frozen Ground). Add `IsGroundSpell` handling + timer + auto-cleanup.
- **Files:** `ButtonAction.cs` → added ground spell properties; `GroundSpellEngine.cs` → created with ActiveGroundSpell tracking; `EngineOrchestrator.cs` → integrated ground spell lifecycle.
- **Definition of Done:** Profile can define a ground spell key → engine tracks duration, auto-removes after expires, UI shows remaining time. ✅ **COMPLETE**
- **Implementation:**
  - ✅ `ButtonAction.cs` — Added `IsGroundSpell`, `GroundSpellDurationSec`, `GroundSpellTickIntervalMs`, `GroundSpellRadius`, `GroundSpellFollowsTarget`, `GroundSpellIsHealing`, `IsSelfCast`
  - ✅ `GroundSpellEngine.cs` — `ActiveGroundSpell` with position, duration, tick interval, radius, healing/damage type, follow-target flag; `RegisterGroundSpell`, `Handle`, `GetActiveSpells`, `ClearAll`, `GroundSpellTick`/`GroundSpellExpired` events
  - ✅ `EngineOrchestrator.cs` — GroundSpellEngine integrated into tick loop, CombatEngine.ActionFired registers ground spells using WindowTracker center position
  - ✅ Unit tests: 3 tests in EngineIntegrationTests.cs (RegisterAndUpdate, TickEvent, ClearAll)
  - ✅ Build: 0 errors, 1 warning | Tests: 56/56 passing

## FEAT-007: Class-Specific Skill Orchestration
- **Goal:** Replace hardcoded skill logic with data-driven, per-class rotations. Create `IRotationProvider` interface, `RotationConfig`/`RotationStep` models, and a `SkillOrchestrator` engine that evaluates conditions (HP, SP, distance, buffs, ground spells, enemy count) and fires the appropriate skill key. Ship built-in rotations for all RO classes (Knight, Wizard, Priest, Assassin, Monk, Bard, Hunter, Rogue, Crusader, Sage, Alchemist) + preset fallbacks.
- **Status:** COMPLETE ✅
- **Implementation:**
  - `IRotationProvider` interface with `GetRotation(string className)` and `GetRotation(EnginePreset preset)`
  - `DefaultRotationProvider` with 12 built-in class rotations (Melee, Ranged, Caster, Hybrid, Support + Knight, Crusader, Wizard, Sage, Hunter, Bard, Assassin, Rogue, Monk, Priest, Alchemist)
  - `RotationConfig` / `RotationStep` / `RotationCondition` / `RotationSettings` models
  - `SkillOrchestrator` engine with condition evaluation (HasTarget, TargetInRange, NotMoving, SPAbove, HPAbove, FacingTarget, EnemyCount, MissingBuff, HasBuff, GroundSpellActive, IsMoving, PartyMemberHasDebuff)
  - Priority-based step selection with finishers and looping
  - Integrated into `EngineOrchestrator` tick loop with full state from AutoTargetEngine, CombatEngine, MovementEngine, SupportEngine, GroundSpellEngine
  - `ClassDetector.GetRotationConfig()` provides rotation configs per class/preset
  - `ClassDetector.ApplyClassPreset()` loads rotation and enables orchestrator
  - `SetDependencies()` connects all required engines for condition evaluation
- **Files:**
  - `SkillOrchestrator.cs` - Complete implementation with all models
  - `EngineOrchestrator.cs` - Integrated SkillOrchestrator into tick loop
  - `ClassDetector.cs` - Added GetRotationConfig and ApplyClassPreset loads rotation
  - `AutoTargetEngine.cs` - Added CurrentTarget, CurrentTargetDistance, IsFacingTarget, NearbyEnemyCount
  - `CombatEngine.cs` - Added CurrentSP, CurrentHPPercent
  - `MovementEngine.cs` - Added IsMoving property
  - `SupportEngine.cs` - Added ActiveBuffs, ActiveDebuffs
  - `GroundSpellEngine.cs` - Added GetActiveSpellNames()

## FEAT-008: Buff / Debuff Tracking System
- **Goal:** Add comprehensive buff/debuff tracking for both self and party members. Support duration warnings, cooldown tracking, and automatic re-casting when expired. Extend `ButtonAction.TrackBuff` + add `BuffManager` service.
- **Files:** `Models/ButtonAction.cs` → add `TrackBuff`, `BuffDurationSec`, `BuffWarningSec` (already exist — extend usage); create `BuffManager` class; `ProfileApplier` → wire buff config.
- **Definition of Done:** Profile can enable buff tracking for any skill → system shows warning 10s before expiry, auto-recasts if configured. ✅ **COMPLETE**
- **Implementation:**
  - ✅ `BuffManager.cs` — Created with active buff/debuff tracking (`ActiveBuff`, `ActiveDebuff` entries with duration, warning threshold, auto-recast, recast key); `RegisterBuff`, `RegisterDebuff`, `Update`, `ClearAll`, `BuffExpiringWarning`, `BuffExpired` events
  - ✅ `EngineOrchestrator.cs` — BuffManager integrated into tick loop (Update called each tick), CombatEngine.ActionFired registers tracked buffs from ButtonAction (TrackBuff, BuffDurationSec, BuffWarningSec, Key)
  - ✅ `Models/ButtonAction.cs` — Already had TrackBuff, BuffDurationSec, BuffWarningSec properties
  - ✅ Build: 0 errors | Tests: 56/56 passing

## FEAT-009: Auto-Class Detection from Keybinds (FEAT-004 completion)
- **Goal:** Improve `ClassDetector.DetectClass()` to analyze `ButtonMappings` more thoroughly — currently relies on few skill keys. Add detection for: attack type (melee/ranged), casting time, buff presence, ground spell indicators.
- **Files:** `ClassDetector.cs` → expand `SkillToClassMap` with more VirtualKey entries; add heuristic scoring (melee vs ranged vs magic vs support); add `DetectRole()` (tank/dps/support).
- **Definition of Done:** Profiling 5+ skills reliably detects class; fallback to profile.Class only when insufficient data.

---

## 📋 Improvement List — What Still Needs Work (per Class Type)

### Melee Classes (Swordsman, Knight, Crusader, Blacksmith)
- [ ] **Rotation priority:** Auto-attack → skill 1 → skill 2 → combo → auto-attack
- [ ] **Leash/range limit:** Melee classes should disengage when target > 12m (needs range check)
- [ ] **Auto-retaliate:** When hit, auto-cast defensive skill (e.g., Shield Boomerang)
- [ ] **Buff cycle:** Auto-cast Blessing/Increase ATK at start of session

### Ranged Classes (Archer, Hunter, Bard, Dancer, Gunslinger, Rebellion)
- [ ] **Lead target:** Aim ahead of moving target based on speed estimation
- [ ] **Ammo management:** Auto-refer arrow/bolt skills when "ammo" depleted (simulate)
- [ ] **Snare kite:** Auto-retreat + shoot when enemy closes
- [ ] **Pet support:** Bard/Dancer auto-buff pet/summon

### Mage / Caster Classes (Mage, Wizard, Sage, Professor, Alchemist)
- [ ] **Ground spell support:** Stone Curse, Frozen Ground, Healing Circle AOEs
- [ ] **Cast bar protection:** Don't move/interrupt when casting > 1.5s unless stunned
- [ ] **Mana management:** Auto-potion when < 30% (configurable threshold)
- [ ] **Skill queue:** Cast next skill when GCD available, not just on keypress

### Support / Healer Classes (Acolyte, Priest)
- [ ] **Party member targeting:** Auto-detect nearest party member HP < 70%
- [ ] **Heal priority:** Single target → party → self, with cooldown per target
- [ ] **Ressurection:** Auto-cast Revive/Resurrection when party member downed
- [ ] **Debuff clear:** Auto-dispel Stone Curse/Posion on party

### Hybrid Classes (Thief, Assassin, Rogue, Stalker)
- [ ] **Dual-weapon mode:** Dagger + Shortsword switching based on situation
- [ ] **Stealth mode:** Auto-toggle when out of combat, auto-untarget when engaging
- [ ] **Backstab priority:** Back-attacks do 2x damage, auto-aim when behind target
- [ ] **Escape art:** Auto-retreat + heal when HP < 40%

### All Classes — General
- [ ] **Profile import/export:** JSON profiles work, but class presets not persisted separately
- [ ] **Live profile switching:** While running → smooth transition without engine reset lag
- [ ] **Telemetry:** Per-class skill firing stats (count, last used, success rate)
- [ ] **Hotkey re-binding:** Real-time remap without restart (currently requires profile reload)

---