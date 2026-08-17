# 🎮 Controls Guide - RagnaController

## The Ultimate Action-RPG Controller Middleware for Ragnarok Online

---

## 📋 Table of Contents

1. [The 5-Layer System](#the-5-layer-system)
2. [System Shortcuts](#system-shortcuts)
3. [Smart Grid Mode](#smart-grid-mode)
4. [Release-to-Cast](#release-to-cast)
5. [Smart Aim Assist](#smart-aim-assist)
6. [Combo System](#combo-system)
7. [Movement Controls](#movement-controls)

---

## 🎯 The 5-Layer System

RagnaController uses a **context-sensitive layer system** where holding modifier buttons changes the function of face buttons (A, B, X, Y). This allows you to map 20+ skills without complex key combinations.

### Layer Mapping Table

| Layer | Modifier | Face Button | Default Action | Example Skills |
|-------|----------|-------------|----------------|----------------|
| **Layer 1** | None | A | Equip/Use | Basic attacks, potions |
| **Layer 2** | L1 | A | Melee Skill | Sword skills, combo attacks |
| **Layer 3** | R1 | A | Magic Skill | Fireball, lightning bolt |
| **Layer 4** | L2 | A | Support Skill | Heal, buff party members |
| **Layer 5** | R2 | A | Utility Skill | Dash, teleport, special abilities |

### Layer-Specific Face Button Actions

#### A Button (Xbox) / Cross (PS)
| Layer | Action | Example |
|-------|--------|---------|
| None | Equip/Use Item | Potion, weapon |
| L1 | Melee Skill | Sword Slash, combo attack |
| R1 | Magic Skill | Fireball, lightning bolt |
| L2 | Support Skill | Heal, shield, buff |
| R2 | Utility Skill | Dash, teleport, special |

#### B Button (Xbox) / Circle (PS)
| Layer | Action | Example |
|-------|--------|---------|
| None | Cancel/Back | Cancel current action |
| L1 | Heavy Attack | Ultimate melee attack |
| R1 | Magic Burst | Enhanced spell damage |
| L2 | Party Heal | AoE heal for party |
| R2 | Emergency Action | Emergency teleport, escape |

#### X Button (Xbox) / Square (PS)
| Layer | Action | Example |
|-------|--------|---------|
| None | Right-Click | Item info, sell |
| L1 | Throw Weapon | Throw weapon at enemy |
| R1 | Magic Projectile | Fireball projectile |
| L2 | Party Buff | Buff entire party |
| R2 | Special Ability | Class-specific ability |

#### Y Button (Xbox) / Triangle (PS)
| Layer | Action | Example |
|-------|--------|---------|
| None | Jump/Attack | Basic jump or attack |
| L1 | Dash Attack | Forward dash with attack |
| R1 | Magic Channel | Channel spell for burst |
| L2 | Party Call | Call party member |
| R2 | Ultimate Skill | Class ultimate ability |

---

## ⌨️ System Shortcuts

### Hardcoded Shortcut Reference

| Input | Action | Description |
|-------|--------|-------------|
| **Left Stick** | Analog click-to-move | Click and drag to move character |
| **Right Stick** | Aiming / Camera | Move camera, aim spells |
| **L3 + Start** | Toggle Smart Grid | Enable/disable inventory navigation mode |
| **LT + RT** (Hold) | Open Radial Emote Menu | Hold both triggers to open emote wheel |
| **Back + R1** | Daisy Wheel On-Screen Keyboard | Activate circular keyboard |
| **Back + L1** | Voice-to-Chat Microphone | Enable voice chat input |
| **L3 + R3** | Panic Heal | Spams F4 instantly for emergency healing |
| **LB + RB** | Loot Vacuum | Spiral clicking to collect items |
| **Start + D-Pad Up/Down** | Quick Profile Switch | Cycle through character profiles |

### Shortcut Details

#### Movement Controls

| Input | Action | Notes |
|-------|--------|-------|
| Left Stick (Analog) | Move Character | Standard movement |
| Left Stick (Click) | Click-to-Move | Tap to move in direction |
| Right Stick (Analog) | Camera/Aim | Smooth camera control |
| Right Stick (Click) | Quick Aim | Instant aim adjustment |

#### Combat Controls

| Input | Action | Notes |
|-------|--------|-------|
| L3 + R3 | Panic Heal | Emergency heal spam |
| LB + RB | Loot Vacuum | Auto-collect items in radius |
| LT + RT (Hold) | Radial Menu | Open emote/skill wheel |

---

## 🧲 Smart Grid Mode

### Activating Smart Grid

Press **L3 + Start** to toggle Smart Grid mode. When active, the D-Pad transforms into a precise navigation tool.

### Smart Grid Behavior

| Action | Result |
|--------|--------|
| **D-Pad Press** | Moves cursor exactly 32 pixels (one inventory slot) |
| **A Button** | Double-clicks to equip/use item |
| **B Button** | Closes window (Escape equivalent) |
| **X Button** | Right-click action (item info, sell, etc.) |

### Smart Grid Use Cases

#### Inventory Navigation
```
1. Press L3 + Start to enable Smart Grid
2. Use D-Pad to navigate inventory slots (32px per press)
3. Press A to equip selected item
4. Press B to close inventory
```

#### Equipment Management
```
1. Open equipment window
2. Navigate to desired slot with D-Pad
3. Press A to double-click and equip
4. Press X for item details (price, stats)
```

#### Window Management
```
1. Press B to close any open window
2. Use Start + D-Pad to switch profiles quickly
```

### Smart Grid Visual Feedback

When Smart Grid is active:
- The cursor snaps to grid points (32px intervals)
- A visual indicator shows Smart Grid is enabled
- D-Pad movements are magnified for precision

---

## ⚡ Release-to-Cast

### How It Works

Release-to-Cast enables MOBA-style ability placement for ground spells. This allows you to aim abilities before firing them.

### Step-by-Step Guide

#### Casting Ground Spells (e.g., Storm Gust)

1. **Hold L1** - Activates casting mode
2. **Tap Spell Button** - Select the desired spell from your combo
3. **Aim with Right Stick** - Position cursor at target location
4. **Release L1** - Fires the spell at the aimed location

#### Example: Storm Gust Placement

```
1. Hold L1 (activates casting mode)
2. Tap A button (selects Storm Gust from combo)
3. Move Right Stick to desired location
4. Release L1 → Storm Gust appears at cursor position
```

### Advanced Release-to-Cast Techniques

#### Multi-Target Casting
```
1. Hold L1 + R1 simultaneously
2. Tap spell button
3. Aim at multiple targets with Right Stick
4. Release L1 to cast on all targets
```

#### Area-of-Effect (AoE) Placement
```
1. Hold LT + RT (both triggers)
2. Tap spell button
3. Draw circle with Right Stick for AoE radius
4. Release triggers to cast
```

### Supported Spells

| Spell Type | Examples | Release-to-Cast Compatible |
|------------|----------|---------------------------|
| Ground Spells | Storm Gust, Fire Wall | ✅ Yes |
| AoE Spells | Meteor, Explosion | ✅ Yes |
| Buff Spells | Shield, Blessing | ⚠️ Limited |
| Instant Spells | Lightning Bolt | ❌ No (instant cast) |

---

## 🎯 Smart Aim Assist

### How It Works

Smart Aim Assist ensures you never miss your target by performing micro-spiral click patterns when needed.

### Activation Method

1. **Slightly tilt the Right Stick** - Small movement (5-10 degrees)
2. **Hold R3 (Right Stick Click)** - Maintain click pressure
3. **System performs micro-spiral** - Automatic click pattern
4. **Target is guaranteed hit** - Even small hitboxes

### Smart Aim Assist Patterns

#### Micro-Spiral Pattern
```
The system executes a spiral click pattern:
1. Initial click at target center
2. Small circular motion (radius: 2-5 pixels)
3. Multiple clicks if target is small
4. Final confirmation click
```

#### Distance-Based Adjustment
```
- Close range (< 50px): Single precise click
- Medium range (50-200px): Standard spiral pattern
- Long range (> 200px): Extended spiral with more clicks
```

### Smart Aim Assist Use Cases

#### Melee Combat
```
Scenario: Enemy has small hitbox
Action: Hold R3 + slight Right Stick tilt
Result: Guaranteed hit on enemy
```

#### Precision Aiming
```
Scenario: Need to hit specific body part
Action: Fine-tune with Right Stick tilt
Result: Precise targeting enabled
```

#### Fast Combat
```
Scenario: Multiple enemies in close range
Action: Smart Aim Assist auto-selects nearest target
Result: No missed attacks
```

### Configuring Smart Aim Assist

| Setting | Value | Effect |
|---------|-------|--------|
| **Sensitivity** | Low/Medium/High | How much tilt triggers assist |
| **Pattern Size** | Small/Medium/Large | Spiral click radius |
| **Click Count** | 1-5 | Number of clicks in pattern |
| **Auto-Target** | On/Off | Auto-select nearest target |

---

## 🔄 Combo System

### Creating Combos

Combos allow you to chain multiple skills together with precise timing.

#### Visual Timeline Editor

1. Open **Macro Editor** from main menu
2. Click **"New Combo"** button
3. Drag skills from skill list onto timeline
4. Set delays between skills using slider
5. Save combo with descriptive name

#### Example: Warrior Combo

```
Timeline:
[0ms]   Sword Slash
[150ms] Heavy Attack  
[300ms] Dash Attack
[450ms] Ultimate Skill
```

### Combo Execution

| Method | How to Execute |
|--------|----------------|
| **Auto-Play** | Hold L2, combo plays automatically |
| **Manual Trigger** | Tap A at each skill slot |
| **Quick Reset** | Press B to cancel and restart |

---

## 🏃 Movement Controls

### Basic Movement

| Input | Action | Notes |
|-------|--------|-------|
| Left Stick (Analog) | Move character | Standard movement |
| Left Stick (Click) | Click-to-move | Tap to move in direction |
| Right Stick (Analog) | Camera control | Smooth camera adjustment |

### Advanced Movement

| Input | Action | Notes |
|-------|--------|-------|
| **L2 + Left Stick** | Dash movement | Quick repositioning |
| **R2 + Left Stick** | Sprint | Increased movement speed |
| **A (while moving)** | Attack while running | Maintain momentum |

### Kiting (Hit-and-Run)

| Input | Action | Notes |
|-------|--------|-------|
| **L1 + Right Stick** | Kite mode | Auto-retreat while attacking |
| **R1 + Left Stick** | Kite attack | Attack then retreat automatically |

---

## 🎨 Customization

### Remapping Controls

1. Open **Settings → Controls**
2. Select control to remap
3. Press desired button combination
4. Click **"Apply"** to save

### Creating Custom Layers

1. Go to **Settings → Layer Configuration**
2. Create new layer profile
3. Assign skills to each face button
4. Save and activate custom layer

### Preset Profiles

| Profile | Best For | Characteristics |
|---------|----------|-----------------|
| **Warrior** | Melee combat | High damage, combo-focused |
| **Mage** | Magic casting | Precision aiming, AoE spells |
| **Archer** | Ranged combat | Kiting, precision shots |
| **Support** | Party healing | Multi-target heals, buffs |
| **Hybrid** | Versatile play | Balanced for all situations |

---

## 📝 Tips & Tricks

### Pro Tips

1. **Use Smart Grid for inventory management** - Much faster than manual navigation
2. **Release-to-Cast for ground spells** - Perfect placement every time
3. **Smart Aim Assist for melee combat** - Never miss your target
4. **L3 + R3 panic heal** - Emergency healing in tough fights
5. **LT + RT radial menu** - Quick access to all emotes

### Performance Tips

- Keep controller connected via USB for lowest latency
- Use DualSense for gyro aiming (more precise)
- Disable Smart Grid when not needed (saves battery)
- Update controller firmware for best performance

---

## 🆘 Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| Controls not responding | Check controller connection, restart app |
| Anti-cheat blocking | Install Interception driver in Settings |
| Gyro not working | Enable in Settings → Advanced |
| Smart Grid not snapping | Toggle L3 + Start again |

### Getting Help

- **Discord:** [Join our Discord](https://discord.gg/ragnac)
- **GitHub Issues:** [Report a bug](https://github.com/RagnaController/RagnaController/issues)
- **Documentation:** Check [README.md](../README.md) for more info

---

## 📜 License

RagnaController is released under the [MIT License](../LICENSE).

---

Made with ❤️ by the RagnaController Team
