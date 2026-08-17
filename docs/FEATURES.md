# RagnaController — Feature Reference v1.3.0

**Tick rate:** 125 Hz (8 ms, ±0.5 ms jitter) · **Profiles:** 39 · **Controller brands:** 8

---

## Startup & UI Shell

### Obsidian & Gold UI
- Fully custom WPF theme: Glassmorphism panels, gold NeonGlow on active elements.
- Micro-animations on buttons (150 ms fade-in, 250 ms fade-out via `ColorAnimation` Storyboards).
- All toolbar icons are WPF `Path` vector graphics — crisp at any DPI, tinted by button `Foreground`.
- Tab bar reflects active state via `TabButtonActive` style (gold bottom-border).

### Focus Lock
- `GetForegroundWindow()` polled every ~500 ms alongside WindowTracker.
- If the foreground window is not the configured RO process, all `SendInput` calls are suppressed.
- Status bar shows `⛔ FOCUS LOCK — switch to RO` in orange while blocked; clears automatically.
- Process name set in **Settings → Game client** (Browse button opens file picker).

### Visual Deadzone Ring
- Red semi-transparent ellipse behind each stick dot — diameter = `deadzone × 50 px`.
- Updates live on slider drag, profile load, and double-click reset.

### Streamer Overlay (v1.3.0)
See [ADVANCED_FEATURES.md — Streamer Overlay](#12-streamer-overlay) for full details.

### Mini Mode
- 260 × 110 px always-on-top overlay showing profile and engine state.
- Right-click toggles click-through (border turns blue).
- `Start + Back` on controller restores the main window from any state.

---

## Window Tracking (`WindowTracker`)

Finds the RO client window and computes its exact centre in physical pixels.

**Priority order on each ~500 ms refresh:**
1. Cached HWND + still foreground → `UpdateGeometry()` only (fast path, no allocation)
2. Different foreground window → PID check — track if it's an RO process
3. Cached HWND still valid → reuse geometry (RO backgrounded)
4. Full `Process.GetProcesses()` scan (only when handle lost)

**Multi-client:** always prefers the current foreground window. `ForceRefreshOnNextTick` (now `_focusCheckCounter = 63`) triggers immediate re-centre 200 ms after a window switch.

**Status display:** tick-latency field shows `2.3ms | RO 1.50x DPI` or `2.3ms | RO: not found`.

---

## Movement Engine

Left stick → click-to-move via `SendInput`. Centre from `WindowTracker` (DPI-corrected), fallback to screen centre.

| Setting | Range | Effect |
|---|---|---|
| Deadzone | 0.0–0.5 | Dead zone radius (visualised by red ring) |
| Curve | 1.0–4.0 | Non-linear sensitivity |
| Action Speed | 1.0–10.0 | Leash radius for Action RPG mode |
| Max Cursor Speed | px/s | Cursor top speed (right stick) |

**Loot Vacuum** (`LB + RB`): spiral + click every 50 ms.

---

## 5-Layer Input System

| Layer | Modifier |
|---|---|
| Base | — |
| L1 | Hold LB |
| R1 | Hold RB |
| L2 | Hold LT |
| R2 | Hold RT |

**Fixed shortcuts:** `X hold`=Alt, `R3`=double-click, `Start+DPad`=profile switch, `Start+Back`=restore window, `Back+L1`=Voice, `Back+R1`=Daisy Wheel, `LT+RT`=Radial menu, `L3+R3`=Panic heal, `LB+RB`=Loot vacuum.

---

## Combat Engines

### AutoTargetEngine — Melee
**Smart Skill Auto-Aim:** when target is locked, skill press → save cursor → snap to `_lockPos` → fire + click → restore cursor. ~12 ms total, guarded by `SemaphoreSlim(1)` with 50 ms timeout.

### KiteEngine — Ranged
FSM: **Lock → Attack → Retreat → Pivot → Relock**. Retreat direction correctly inverted (`-_lastAimX`, `-_lastAimY`). Hold R2 to hold ground, L2 to force retreat.

### MageEngine — Mage / Wizard / Sage
Ground-target (right stick aims, R3 places) or Bolt mode (hold R2, lock and auto-fire).

### SupportEngine — Priest / High Priest
Right stick aims at ally, R3 snaps + heals. Phase labels (HEALING, SELF-HEAL, SANCTUARY) display for 500 ms.

---

## Voice-to-Chat
`Back + L1` — Windows Speech Recognition, serialised via `_isChatting` flag, 8-second timeout.

## Daisy Wheel Keyboard
`Back + R1` — circular on-screen keyboard. Y-axis correctly inverted. `SyncHeldState()` prevents false combo fire when wheel closes.

## Radial Emote Menu
Hold `LT + RT`. Window reused via `Visibility` — no WPF transparency re-init on rapid presses.

---

## Streamer Overlay (v1.3.0)

Opened via **Stream** button in toolbar. `StreamerOverlayWindow` subscribes to the same `SnapshotUpdated` event as the main window.

| Element | Detail |
|---|---|
| **Profile / Layer / State** | Top row — profile name, layer badge (colour-coded), engine state badge |
| **Button grid** | A=green, B=red/orange, X=blue, Y=yellow, LB/RB/L3/R3=gold, Back/Start=grey |
| **Trigger bars** | 22 px gold fill bar proportional to LT/RT analogue depth |
| **Stick radars** | 36×36 px radar with crosshair, deadzone ring, and 7 px dot |
| **Click-through** | Right-click — border turns blue, mouse events pass to game |
| **Size presets** | Double-click: Compact 220×120 · Normal 320×160 · Large 420×210 |
| **Opacity** | Dims to 40% when engine stopped or focus-locked |
| **OBS** | Window Capture — transparent background, no chroma key |

---

## Macro System
Record key+click sequences, edit delays, loop. Storage: `%AppData%\RagnaController\Macros\*.json`.

## Profile System
39 built-in profiles. `AddAndSave` prevents duplicates. `.bak.json` fallback on corrupt save. Profile names sanitized before use as filenames.

## Feedback System
Rumble patterns check `_rumbleEnabled` after every `await Task.Delay`. `KitePhase.Retreating` → `KiteCycle` pattern; `SupportPhase.Healing` → `HealCast` rumble.

## InputSimulator
- `INPUT` struct: `LayoutKind.Explicit`, `FieldOffset(8)` for 64-bit alignment.
- `MoveMouseRelative`: `MOUSEEVENTF_MOVE | MOUSEEVENTF_MOVE_NOCOALESCE` (0x2001) — no coalescing.
- `SendChatString`: serialised with `_isChatting` flag; `_shutdownRequested` abort on app exit.

## Performance

| Item | Detail |
|---|---|
| Timer resolution | `timeBeginPeriod(1)` — 1 ms Windows scheduler precision |
| Tick jitter | ±0.5 ms (was ±5 ms) |
| Perf log threshold | 25 ms |
| Vacuum click rate | 1 click / 50 ms |
| WindowTracker refresh | Fast-path: no allocation when HWND cached; full scan only when handle lost |

## Settings (`Models/Settings.cs`)

| Setting | Default | Description |
|---|---|---|
| FocusLockEnabled | true | Pause engine when RO loses focus |
| FocusLockProcess | `ragexe` | Process name (Browse button) |
| AutoStart | false | Start engine on launch |
| StartInMiniMode | false | Launch to Mini-Mode overlay |
| SoundEnabled | true | Audio feedback |
| RumbleEnabled | true | Haptic rumble |
| LogLevel | Info | Debug / Info / Warning / Error |
