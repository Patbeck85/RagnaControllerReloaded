# RagnaController — Release Testing Checklist v1.3.0

Run through every section before tagging a release.

---

## 1. Build & Startup

- [ ] `START.bat` completes without errors (option 1 — Framework-dependent)
- [ ] Splash screen plays; main window opens at v1.3.0
- [ ] Admin warning banner appears if launched without admin rights
- [ ] No unhandled exception on cold start (fresh AppData)

---

## 2. Streamer Overlay (v1.3.0)

- [ ] **Stream button** in toolbar opens the overlay window
- [ ] Overlay appears in bottom-right corner of the primary screen
- [ ] **Profile name** updates when a different profile is selected
- [ ] **A button** lights green when pressed on controller
- [ ] **B button** lights red/orange
- [ ] **X button** lights blue (`#4A9EF5`)
- [ ] **Y button** lights yellow
- [ ] **LB / RB** light gold
- [ ] **L3 / R3** light gold
- [ ] **LT trigger bar** fills proportionally when trigger is pressed
- [ ] **RT trigger bar** fills proportionally
- [ ] **Left stick dot** tracks stick position on radar
- [ ] **Right stick dot** tracks stick position on radar
- [ ] **Layer badge** shows L1+ in gold when LB is held
- [ ] **State badge** shows ATTACKING / VACUUM / COMBO correctly
- [ ] **Right-click** → border turns blue, click-through active (clicks pass to game)
- [ ] **Double-click** → cycles through Compact / Normal / Large sizes
- [ ] **Drag** → overlay repositions on screen
- [ ] **Overlay dims to 40%** when engine is stopped
- [ ] Clicking **Stream button again** closes the overlay
- [ ] OBS "Window Capture" shows transparent background (no chroma key needed)

---

## 3. Focus Lock

- [ ] Settings → Focus Lock checkbox checked by default
- [ ] Browse button selects `.exe`; field updates to filename without extension
- [ ] Alt-tab to Notepad → status bar shows `⛔ FOCUS LOCK — switch to RO` in orange
- [ ] No controller input passes through while locked
- [ ] Tab back to RO → indicator clears, dot turns green within 500 ms

---

## 4. Visual Deadzone Ring

- [ ] Red ring visible on both stick visualisers at rest
- [ ] Deadzone slider → ring resizes live
- [ ] Double-click label → slider + ring reset to profile default
- [ ] Profile switch → ring updates to new profile's deadzone

---

## 5. Button Remapping Window

- [ ] **A button** shows green
- [ ] **B button** shows red
- [ ] **X button** shows blue
- [ ] **Y button** shows yellow/gold
- [ ] Clicking a button opens the binding panel for that button
- [ ] Binding saved correctly on close

---

## 6. Window Tracking & DPI

- [ ] Tick-latency shows `X.Xms | RO 1.00x DPI` when RO is open
- [ ] Move RO to different monitor → DPI label updates within 500 ms
- [ ] Cursor movement lands on character centre, not offset

---

## 7. Analog Movement & Combat

- [ ] Stick dots centre at rest (no drift visible at Deadzone 0.12)
- [ ] Left stick moves character smoothly
- [ ] L3 toggles Melee engine; L3 again deactivates
- [ ] Smart Skill snaps cursor to target and back
- [ ] LB+RB → Loot vacuum spirals and clicks
- [ ] L3+R3 → Panic heal fires F4 multiple times

---

## 8. Mini-Mode & Click-Through Trap

- [ ] Mini button → compact overlay appears
- [ ] Right-click overlay → click-through active, border blue
- [ ] `Start + Back` on controller → main window restores

---

## 9. Voice-to-Chat & Daisy Wheel

- [ ] `Back + L1` → microphone activates; auto-cancels after ~8 seconds if silent
- [ ] `Back + R1` → Daisy Wheel opens; stick UP selects top sector (not bottom)
- [ ] Start submits text to RO chat; B cancels

---

## 10. Profile System

- [ ] Import same-name profile → replaces, no duplicate
- [ ] Corrupt `.json` (delete half) → app loads `.bak.json` silently
- [ ] Wizard: empty name → warning MessageBox, no crash
- [ ] Wizard: name with `/` or `:` → sanitized, saved correctly

---

## 11. Performance

- [ ] Tick latency stays below 5 ms average over 5 minutes
- [ ] No perf warnings in log during normal play (threshold 25 ms)
- [ ] CPU below 2% while engine running
