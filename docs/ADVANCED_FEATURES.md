# Advanced Pro Features Guide (v1.3.0)

---

## 1. Streamer Overlay

**Trigger:** Click the **📹 Stream** button in the main toolbar.

A compact always-on-top window appears (default: bottom-right corner of the screen) showing your controller state in real time — perfect for a stream corner or capture card overlay.

**What it shows:**
- **Profile name** — active profile in the top-left
- **Layer badge** — BASE (grey) · L1+/R1+ (gold) · L2+/R2+ (orange)
- **State badge** — ATTACKING · VACUUM · COMBO · PANIC · RADIAL, each in its own colour
- **Button grid** — all 12 buttons light up in their Xbox colour when pressed:
  - A = green · B = red · X = blue · Y = yellow
  - LB/RB/L3/R3 = gold · Back/Start = grey
- **Trigger fill bars** — LT and RT show analogue depth as a gold bar (0–22 px)
- **Stick radars** — two 36 px radar dots for L-stick and R-stick with deadzone ring
- **Opacity** — dims to 40% when engine is stopped or Focus Lock is active

**Interaction:**
- **Drag** — left-click and drag to reposition
- **Right-click** — toggle click-through (`WS_EX_TRANSPARENT`), border turns blue
- **Double-click** — cycle size presets: Compact 220×120 → Normal 320×160 → Large 420×210

**OBS / XSplit setup:**
Add as "Window Capture" — the background is already transparent (WPF `AllowsTransparency`). No chroma key needed.

---

## 2. Smart Skill Auto-Aim (Cursor Juggling)

When `AutoTargetEngine` has a target locked and a skill button is pressed:
1. Saves current walking cursor position.
2. Snaps cursor to `_lockPos` (target screen coordinates).
3. Fires the skill key + left-click.
4. Restores cursor to walking position.

Total time: ~12 ms. Guarded by `SemaphoreSlim(1)` with 50 ms timeout — skills queue briefly instead of being silently dropped.

---

## 3. Focus Lock (Discord / Desktop Protection)

`GetForegroundWindow()` polled every ~500 ms. If the active window's process name doesn't match the configured game client, all `SendInput` calls are suppressed. Status bar shows `⛔ FOCUS LOCK — switch to RO`.

Configure the process name in **Settings → Game client** — Browse opens a file picker, only the filename without `.exe` is stored (e.g. `ragexe`, `custom2025`).

---

## 4. DPI-Aware Window Tracking

`WindowTracker` reads the RO window's exact physical bounds:
- `GetClientRect` → inner drawable area (no title bar)
- `ClientToScreen` → real screen origin
- `MonitorFromWindow` + `GetDpiForMonitor` → monitor's DPI

With `PerMonitorV2` in `app.manifest`, all coordinates are already physical pixels. `DpiScale` is retained for the status bar display only.

**Multi-client:** always prefers the foreground window. After a Window Switch, counter is reset so re-centering happens within ~8 ms.

---

## 5. Multi-Client Window Switcher

Map any button to `Action Type: Switch Window`. `WindowSwitcher.Toggle()` runs on `Task.Run` — `AttachThreadInput` never blocks the UI thread. After the switch, `WindowTracker` re-centres to the new window's monitor and DPI.

---

## 6. Voice-to-Chat

`Back + L1` — activates Windows Speech Recognition. `SendChatString` is serialised via `_isChatting` flag; auto-cancels after 8 seconds. `_shutdownRequested` ensures clean abort on app exit.

---

## 7. Daisy Wheel Keyboard

`Back + R1` — circular on-screen keyboard. Left stick selects sector, face buttons type. L3=Backspace, R3=Space, Start=submit. `SyncHeldState()` prevents the combo engine firing a false Step 1 when the wheel closes while a button is still held.

---

## 8. Radial Emote Menu

Hold `LT + RT`. Window is kept alive in memory (`Visibility.Hidden`/`Visible`) — no WPF transparency reinitialisation on rapid presses. Run `GetEmotes.ps1` to download all 60 RO emotes (4× nearest-neighbour upscaled).

---

## 9. Panic Emergency Heal

`L3 + R3` — bypasses all delays, triggers haptic warning rumble, fires `F4` × 10 in 100 ms. 125-tick cooldown prevents accidental re-trigger.

---

## 10. Loot Vacuum

`LB + RB` — spirals cursor around character centre, one click every 50 ms. Centre position is DPI-corrected from `WindowTracker`.

---

## 11. Combo Engine

Class-aware sequential skill chains with Per-Renewal / Renewal timing. `SyncHeldState(bool)` prevents false fire after overlays close. Configured in the **Combo** window.

---

## 12. Timer Precision

`timeBeginPeriod(1)` on engine startup locks the Windows scheduler to 1 ms resolution. DispatcherTimer jitter drops from ±5 ms to ±0.5 ms. `timeEndPeriod(1)` restores the OS default on shutdown.
