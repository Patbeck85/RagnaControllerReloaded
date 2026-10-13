# Movement Mechanics Reference — rAthena / Ragnarok Online

## Overview
This document defines the verified movement mechanics from rAthena server behavior that our engines must match.

---

## 1. Basic Movement System

### 1.1 Movement Speed
**Base Speed (No modifiers):**
- **0.15 seconds per cell** = 150ms per cell movement
- This is the "delay after moving to the next ground tile" — movement is instantaneous, then delay

### 1.2 Walk Delay (Client-Side)
**Default: 600ms (0.6 seconds)**
- Hardcoded in client, modifiable by modders
- Applied when: taking damage, certain skills, movement interruptions
- Prevents movement for duration

### 1.3 Movement Modifiers
| Modifier | Effect | Source |
|----------|--------|--------|
| Peco Peco Riding | +200% speed | Riding skill |
| Increase AGI | Variable | Priest skill |
| Wind Walk | Variable | Stalker skill |
| Cart Boost (Genetic) | Variable | Cart skill |
| Snap (Shadow Chaser) | Instant turn | Skill |
| Back Slide | Instant 5-cell slide | Thief skill |
| Front Slide | Instant 7-cell slide | Mechanic skill |

---

## 2. Movement States (Server-Side)

### 2.1 Walk vs Run
- **Walk**: Normal movement, 150ms/cell
- **Run**: Not explicitly separate in classic RO; movement speed is unified
- **Delay After Stop**: When stopping movement, there's a brief delay before next action

### 2.2 Position Interpolation
- **MIN_POS_INTERVAL = 20ms** — server recalculates exact positions for units in motion
- Client interpolates between server position updates

---

## 3. Special Movement Skills

### 3.1 Back Slide (Thief) — TF_BACKSLIDING
| Property | Value |
|----------|-------|
| Type | Active, Support, Physical |
| Target | Self |
| SP Cost | 7 (classic) / 10 (renewal) |
| Cast Time | **None (Instant)** |
| Cast Delay | **ASPD Based** (uses ASPD formula) |
| Range | — |
| Knockback | **5 cells backward** (opposite facing direction) |
| Prerequisites | Complete Thief Guild quest (Job Lv.35) |
| Special | Cannot use in WoE/GvG/BG; Endure effect 0.2s on cast |
| Commands | `/bingbing` `/bangbang` change facing without moving |

**Mechanic:**
- Instantly slides 5 cells backwards
- Direction = opposite of character facing
- Can be spammed (no cooldown, only ASPD delay)
- Used for: Disengaging, dodging, positioning

### 3.2 Front Slide (Mechanic) — Frontside Slide
| Property | Value |
|----------|-------|
| Type | Active, Madogear |
| Target | Self |
| Cost | 1 Magic Gear Fuel |
| Cast Time | None |
| Range | **7 cells forward** |
| Prerequisites | Mechanic class, Madogear equipped |

### 3.3 Slide Casting (General Mechanic)
**Concept:** Casting skills while moving without interrupting movement
- Some skills allow "slide cast" — cast animation plays while character continues moving
- Not all skills support this; depends on skill configuration
- Server validates: if movement packet received during cast, may cancel or allow based on skill

---

## 4. rAthena Movement Code (unit.cpp)

### 4.1 Key Structures
```cpp
// unit.hpp
struct unit_data {
    short walkpath[256];    // Path to follow
    int walkpath_len;       // Path length
    int walk_count;         // Current step in path
    int timer;              // Movement timer
    // ... position, direction, etc.
};
```

### 4.2 Walk Delay Application
```cpp
// When unit takes damage
bl->walk_delay = ...; // Applied delay preventing movement
```

### 4.3 Movement Timer
- Server processes movement at regular intervals
- Client sends movement packets, server validates
- Pathfinding recalculated if blocked

---

## 5. Client-Side Movement

### 5.1 Click-to-Move
- Right-click on ground → character pathfinds to cell
- Client sends `Move` packet with destination
- Server validates path, sends position updates

### 5.2 WASD / Controller Movement
- Continuous direction input → repeated move packets
- Server applies walk delay, validates collision

### 5.3 Movement Speed Display
- Status window shows "Move Speed" as percentage
- 100% = base 150ms/cell
- 200% = 75ms/cell (Peco)

---

## 6. Kiting Mechanics (Our Implementation)

### 6.1 State Machine
```
IDLE → LOCKING → ATTACKING → RETREATING → PIVOTING → RELOCKING → ATTACKING
                ↓                              ↑
                └──────────────────────────────┘
```

### 6.2 Timing Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `AttackIntervalMs` | 60ms | Time between attacks |
| `AttacksPerCycle` | 3 | Attacks before retreat |
| `RetreatDurationMs` | 400ms | Time moving backward |
| `RetreatCursorDist` | 110px | Distance to move cursor |
| `PivotDurationMs` | 300ms | Time aiming at target |
| `RelockDelayMs` | 100ms | Delay before re-locking |

### 6.3 Movement During Kiting
- **Locking (200ms)**: Move cursor toward target at 1.5x sensitivity, then RightClick
- **Attacking**: Move cursor at 0.6x sensitivity; attack every `AttackIntervalMs`
- **Retreating (400ms)**: Move cursor opposite aim direction at calculated step; LeftClick every 180ms
- **Pivoting (300ms)**: Move cursor at 2.5x sensitivity to re-aim
- **Relocking (100ms)**: Wait, then RightClick to re-acquire target

---

## 7. Testing Requirements

### 7.1 Unit Tests Needed
1. **Walk/Run Timing**: Verify 150ms/cell base, modifiers apply correctly
2. **Walk Delay**: Damage applies delay, movement blocked
3. **Back Slide**: Instant 5-cell slide, ASPD-based cooldown, SP cost
4. **Front Slide**: 7-cell forward, fuel consumption
5. **Slide Cast**: Movement continues during specific skill casts
5. **Kite States**: All 6 states transition correctly with timings
6. **Retreat Logic**: 5 cells backward (Back Slide) vs calculated retreat distance
7. **Pivot/Relock**: Aim sensitivity multipliers applied

### 7.2 Integration Tests
1. **Full Kite Cycle**: Lock → Attack ×3 → Retreat → Pivot → Relock → Attack
2. **Movement + Attack**: Character attacks while moving (slide cast)
3. **Back Slide Spam**: Multiple Back Slides respect ASPD delay
4. **Walk Delay Interrupt**: Damage stops movement for 600ms

---

## 8. Configuration Mapping

| Profile Field | Engine | Default | Description |
|--------------|--------|---------|-------------|
| `WalkDelay` | MovementEngine | 600 | Walk delay when hit |
| `MoveSpeedPercent` | MovementEngine | 100 | Base speed multiplier |
| `BackSlideEnabled` | KiteEngine | true | Use Back Slide for retreat |
| `FrontSlideEnabled` | KiteEngine | false | Use Front Slide (Mechanic) |
| `SlideCastEnabled` | CombatEngine | true | Allow cast while moving |

---

## 9. References
- rAthena unit.cpp: `src/map/unit.cpp`
- iRO Wiki Movement Speed: https://irowiki.org/wiki/Movement_Speed
- iRO Wiki Back Slide: https://irowiki.org/wiki/Back_Slide
- Ragnarok Research Lab Movement: https://ragnarokresearchlab.github.io/game-mechanics/movement/
- rAthena GitHub: https://github.com/rathena/rathena