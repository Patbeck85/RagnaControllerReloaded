# Skill Mechanics Reference — rAthena / Ragnarok Online

## Overview
This document defines the verified skill mechanics from rAthena server behavior that our engines must match.

---

## 1. Skill Execution Phases (rAthena Pipeline)

### Phase 1: Initiation
- Skill name appears above character head
- SP/HP cost deducted
- Requirements checked (state, items, target, range, direction)

### Phase 2: Cast Time (Fixed + Variable)
**Formula (Renewal):**
```
FixedCastTime = SkillDB_FixedCast * (1 - FixedCastReduction%)
VariableCastTime = SkillDB_VariableCast * (1 - VariableCastReduction%)
TotalCastTime = FixedCastTime + VariableCastTime
```

**Key Mechanics:**
- Fixed cast cannot be reduced by DEX/INT (only by specific items/skills like Sacrament, Magical Strings)
- Variable cast reduced by: DEX, INT, Cast Speed items (Sacrament, Magical Strings, Suffragium)
- Min cast time = FixedCastTime (if VariableCastReduction = 100%)
- Some skills have ONLY fixed cast (instant after fixed) — e.g., many traps, AoE ground spells
- Interruptible by: Damage, Stun, Stone Curse, Sleep, Knockback, Move command

### Phase 3: Execution
- Skill effect triggered
- Animation played
- Projectile/aoe/skill-specific logic executed

### Phase 4: After-Cast Delay (ACD)
**Also called:** Cast Delay, Delay After Cast, Global Cooldown (GCD)

**Formula:**
```
ACD = SkillDB_AfterCastDelay * (1 - AfterCastDelayReduction%)
```

**Key Mechanics:**
- ACD_base defined per-skill in skill database
- Client-side ACD usually ranges 140-180ms (configurable in skill.conf)
- Prevents: Next skill cast, Normal attack, Movement (sometimes), Item use
- Can be reduced by: VCT reduction items (some servers), specific skills
- rAthena config: `skill_delay_attack` (default 140-180ms range)

---

## 2. Skill Types & Special Mechanics

### 2.1 Bolt Spells (Wizard/Professor/Sage/High Wizard/Warlock/Sorcerer)
- **Examples:** Fire Bolt, Cold Bolt, Lightning Bolt, Earth Spike
- **Mechanic:** Can be "bolt spammed" — cast time only on first cast, subsequent casts have NO cast time (only ACD)
- **Condition:** Must hold hotkey / rapid press within ACD window
- **Our MageEngine:** Uses `MageBoltCastDelayMs` (default 1200ms) as ACD timer for bolt spam

### 2.2 Ground Spells / AoE (Wizard/Warlock/Sage/Professor)
- **Examples:** Fire Wall, Frost Driver, Storm Gust, Meteor Storm, Lord of Vermilion, Safety Wall, Pneuma, Heal Circle
- **Mechanic:** 
  1. Press hotkey → targeting circle appears
  2. Move mouse to position
  3. Left-click → cast at position
  4. Effect persists for duration, ticks at interval
- **Our GroundSpellEngine:** Tracks position, duration, tick intervals, auto-cleanup

### 2.3 Self-Cast / Instant Skills
- **Examples:** Buffs (Increase AGI, Blessing, Kyrie Eleison), Heal, Magnificat, Gloria
- **Mechanic:** No targeting, instant or short cast, affects self/party
- **Our CombatEngine:** `ExecuteSelfCast` — snap to center, fire, snap back

### 2.4 Trap Skills (Hunter/Sniper/Ranger)
- **Examples:** Ankle Snare, Blast Mine, Claymore Trap, Land Mine
- **Mechanic:** Instant cast, placed at character position or targeted tile
- **ACD only** (no variable cast)

### 2.5 Melee Skills (Knight/Crusader/Paladin/Monk/Champion)
- **Examples:** Bash, Magnum Break, Bowling Bash, Brandish Spear, Asura Strike, Snap
- **Mechanic:** Usually instant or very short cast, requires target in melee range
- **Combo chains:** Some have specific combo windows (Double Attack → Triple Attack)

---

## 3. Global Cooldown (GCD) / After-Cast Delay Details

### rAthena Configuration (skill.conf):
```
skill_delay_attack: yes/no (whether normal attacks share ACD)
skill_delay_skill: yes/no (whether skills share ACD with each other)
min_skill_delay: 140  // minimum ACD in ms
max_skill_delay: 180  // maximum ACD in ms
```

### Our Implementation:
- **SkillOrchestrator.RotationSettings.GlobalCooldownMs** = 200ms (conservative)
- **CombatEngine:** `_bufferedAction` with 100ms buffer window
- **MageEngine:** `_castCooldown` with JitterService ±50ms

---

## 4. Interrupt Mechanics

### What Interrupts Cast:
1. **Damage taken** (any amount)
2. **Status effects:** Stun, Stone Curse, Sleep, Frozen, Knockback
3. **Movement:** Walk/Run command issued
4. **Skill cancel:** Pressing ESC or another skill hotkey (client-side)

### What Does NOT Interrupt:
- Damage = 0 (miss, blocked)
- Status: Poison, Curse, Silence (unless skill specifically requires voice)
- HP/SP drain

### Our Engines:
- CombatEngine: No explicit interrupt handling (reliant on game client)
- MageEngine: Bolt spam stops on R2 release
- GroundSpellEngine: No interrupt handling for persistent effects

---

## 5. Skill-Specific Known Values (Classic/Pre-Renewal)

| Skill | Cast Time | After-Cast | Type |
|-------|-----------|------------|------|
| Fire Bolt | 0.5s variable | ~200ms | Bolt |
| Cold Bolt | 0.5s variable | ~200ms | Bolt |
| Lightning Bolt | 0.5s variable | ~200ms | Bolt |
| Fire Ball | 1.0s variable | ~400ms | AoE |
| Fire Wall | 1.0s fixed | ~500ms | Ground |
| Frost Driver | 1.5s variable | ~600ms | Ground |
| Storm Gust | 2.0s variable | ~800ms | Ground |
| Meteor Storm | 3.0s variable | ~1000ms | Ground |
| Safety Wall | 0.5s fixed | ~300ms | Ground |
| Heal | 0.8s variable | ~200ms | Self/Target |
| Blessing | 1.0s variable | ~300ms | Self/Party |
| Increase AGI | 0.5s variable | ~200ms | Self/Target |
| Magnum Break | 0s instant | ~200ms | Melee |
| Bowling Bash | 0s instant | ~300ms | Melee |
| Asura Strike | 1.5s variable | ~500ms | Melee |
| Double Attack | 0s instant | ~150ms | Melee |

---

## 6. Server-Side vs Client-Side

### Server-Side (Authoritative):
- Cast time validation
- SP/HP cost
- Target validation
- Damage calculation
- ACD enforcement (skill_delay_attack/skill)

### Client-Side (Predictive):
- Cast bar animation
- Targeting circle display
- Skill queue buffering
- ACD visual feedback

### Our Architecture:
- **CombatEngine/MageEngine/GroundSpellEngine:** Client-side prediction (what we send)
- **CooldownManager:** Tracks server-side cooldowns via timers
- **AutoTargetEngine:** Target validation (range, LoS, death)

---

## 7. Testing Requirements

### Unit Tests Needed:
1. **Cast Time Verification:** First cast vs subsequent (bolt spam)
2. **ACD Enforcement:** Cannot cast next skill until ACD expires
3. **Interrupt Simulation:** Damage → cast cancelled
4. **Ground Spell Lifecycle:** Register → Tick → Expire → Cleanup
5. **Combo Chains:** Double Attack → Triple Attack window
6. **Self-Cast:** Snap → Fire → Snap back timing
7. **Bolt Spam:** Hold R2 → rapid taps at ACD interval ± jitter
8. **Cooldown Tracking:** Buff warning at (duration - warning)

### Integration Tests:
1. Full rotation: Skill A → (wait cast+ACD) → Skill B → ...
2. Failover: SendInput ↔ Interception parity during rotation
3. Latency: P99 ≤ 8ms from button press → game receives
4. Soak: 100k ticks, no desync, no memory leak

---

## 8. Configuration Mapping (Profile → Engine)

| Profile Field | Engine | Default | Description |
|--------------|--------|---------|-------------|
| `MageBoltCastDelayMs` | MageEngine | 1200 | ACD for bolt spam |
| `GlobalCooldownMs` | SkillOrchestrator | 200 | Minimum between rotation steps |
| `GroundSpellDurationSec` | GroundSpellEngine | per-skill | Persistent effect duration |
| `GroundSpellTickIntervalMs` | GroundSpellEngine | per-skill | Damage/heal tick rate |
| `BuffDurationSec` | CooldownManager | per-buff | Buff tracking |
| `BuffWarningSec` | CooldownManager | per-buff | Warning threshold |

---

## 9. References
- rAthena skill.conf: `conf/battle/skill.conf`
- rAthena skill execution: `src/map/skill.c`
- iRO Wiki: https://irowiki.org/wiki/Skills
- DeepWiki rAthena: https://deepwiki.com/rathena/rathena
- NexusRO Cast Formulas: https://playnexusro.com/wiki/formulas/skill-timing/