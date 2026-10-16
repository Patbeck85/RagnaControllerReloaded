# Troubleshooting Guide — RagnaController

Common issues, symptoms, root causes, and fixes. If your issue is not listed here,
see also [Known Limitations](KnownLimitations.md) and the GitHub Issues page.

---

## 1. Controller not detected

**Symptoms:** Controller status shows "No Controller", inputs do not reach the game.

| Check | Action |
|-------|--------|
| Controller connected? | Plug in the controller (USB or Bluetooth). Windows should show a device sound. |
| Correct XInput/DualSense mode? | DualSense controllers may need to be switched to XInput mode (hold PS + Share buttons). |
| SDL2 initialized? | Check `ControllerService` logs. Look for `SDL_Init` success in `DeveloperConsoleWindow`. |
| Focus Lock active? | If Focus Lock is enabled and RO is not foreground, input is suppressed. Disable Focus Lock or click into RO window. |

**Root cause:** SDL2 subsystem not initialized, or controller in unsupported mode.

**Fix:** Restart RagnaController with the controller plugged in. Check `ControllerTestWindow` → "Input Check" to verify button events arrive.

---

## 2. Focus Lock not working

**Symptoms:** Input leaks to desktop when RO is not foreground, or RO inputs are blocked when RO IS foreground.

| Check | Action |
|-------|--------|
| Focus Lock enabled? | Check `SettingsWindow` → "Enable Focus Lock". |
| Correct process name? | Check `FocusLockProcess` in settings. Should match RO client exe (e.g., `ragexe`, `ragexere`, `myRO`). |
| Multiple RO clients? | Use `PreferredClientHwnd` to address a specific client (FEAT-015). |

**Root cause:** Wrong process name or Focus Lock state desynced.

**Fix:** In SettingsWindow, click "Browse" next to RO ExePath to auto-detect the process name. Toggle Focus Lock off and on again.

---

## 3. Skills not casting

**Symptoms:** Skill hotkeys (F-keys, number keys) do not trigger in-game.

| Check | Action |
|-------|--------|
| MageEngine active? | Toggle Mage mode (R2 trigger). Check `MageEngine` phase in overlay. |
| Trigger threshold? | Analog trigger must be > 0.5 (strict). Digital R2 also works. |
| Cast delay? | `MageBoltCastDelayMs` may prevent rapid recasts. Check cooldown. |
| Target locked? | AutoTarget requires a locked target for some skills. Use Tab to cycle. |

**Root cause:** Trigger below threshold, cast delay cooldown, or no valid target.

**Fix:** Increase trigger pressure or use digital R2. Check `MageEngineTests` for reference values.

---

## 4. Cursor desync / jumping

**Symptoms:** Mouse cursor jumps to wrong position after profile switch or physical mouse movement.

| Check | Action |
|-------|--------|
| Virtual cursor reset? | Call `CursorEngine.Reset()` on profile switch (TECH-034). |
| DPI scaling? | Multi-monitor DPI differences cause wrong clamping (TECH-035). |
| Physical mouse moved? | Virtual cursor resyncs every ~500ms (TECH-032). |

**Root cause:** Virtual cursor desynchronized from real Windows cursor position.

**Fix:** Switch profiles/clients triggers `Reset()`. Cursor auto-resyncs within 500ms.

---

## 5. Input lag / latency spikes

**Symptoms:** Input feels delayed, especially during Bezier mouse moves or combat.

| Check | Action |
|-------|--------|
| Bezier moves blocking? | `BezierMouseService` blocks consumer thread (TECH-038 — WONT_FIX, documented). |
| Tick budget exceeded? | Tick should be < 2ms (PERF-003). Check `FrameBudgetMonitor`. |
| SendInput vs Interception? | Failover (ROB-002) switches backend if latency exceeds threshold. |

**Root cause:** Blocking operations in consumer thread or tick budget overrun.

**Fix:** Avoid long Bezier durations. Monitor `TelemetryPanel` for P95/P99 latency.

---

## 6. Profile does not load / settings lost

**Symptoms:** Profile settings revert after restart, or profile JSON is corrupted.

| Check | Action |
|-------|--------|
| profiles.json writable? | Check permissions on `<dir>/profiles.json`. |
| Migration from v1.0? | Profile migration v1.0 → v2.1.0 is tested (GEX-012). |
| Character mapping? | Character→profile mapping persists in `character_mappings.json` (FEAT-024). |

**Root cause:** File I/O error, migration issue, or missing character mapping.

**Fix:** Check `ProfileManager` logs. Delete `character_mappings.json` and remap characters if needed.

---

## 7. Storage drop puts valuables in stash

**Symptoms:** Quest items or equipment are moved to storage instead of consumables.

| Check | Action |
|-------|--------|
| Item classification? | `StorageDropPlanner` classifies Consumable→Stash, Buff→Drop, Quest/Equipment→Keep (FEAT-023). |
| Item DB correct? | Check item IDs in default profiles (GEX-010). |

**Root cause:** Misclassified item in `StorageDropPlanner`.

**Fix:** Verify item rarity/type in profile JSON. Quest items and equipment should never be auto-dropped.

---

## 8. Party heal targets wrong member

**Symptoms:** Party heal targets a member with full HP instead of the weakest.

| Check | Action |
|-------|--------|
| PartyManager active? | `PartyManager.Start()` must be called (FEAT-012). |
| HP threshold set? | `HealThresholdPercent` default 70. Members above threshold are not healed. |
| Member count correct? | `MemberCount` clamped to 1..12 (MaxMembers). |

**Root cause:** Threshold too high, or PartyManager not started.

**Fix:** Lower `HealThresholdPercent` or verify `PartyManager.IsRunning`.

---

## 9. Combo breaks / wrong skill fires

**Symptoms:** Combo chain does not execute correctly, wrong skill in sequence.

| Check | Action |
|-------|--------|
| Buffer window? | Buffer refills every 64ms on hold. Release needs 2 frames (drain + reset). |
| Chain cooldown? | `ChainCooldownMs` must elapse before auto-loop restarts. |
| Skill interrupt? | Overlay close or skill interrupt can break chain (`SyncHeldState`). |

**Root cause:** Buffer window missed, cooldown active, or interrupt false-fire.

**Fix:** Press next skill within 64ms buffer window. Wait for cooldown before re-chaining.

---

## 10. Build fails / CI errors

**Symptoms:** `dotnet build` or GitHub Actions CI fails.

| Check | Action |
|-------|--------|
| .NET 8 SDK? | Install .NET 8 SDK. |
| SDL2 headers? | Set `RAGNACONTROLLER_SKIP_SDL=1` for headless/CI builds. |
| Stryker mutation score? | Global ≥70%, per-engine floors (Combat ≥80%, Movement ≥80%, InputRouter ≥85%). |
| Generated files? | `*.g.cs` and `*.g.i.cs` must NOT be in git (TECH-057). |

**Root cause:** Missing SDK, SDL2 in headless env, or mutation score below floor.

**Fix:** Install .NET 8, set env var, address mutation survivors per hardening report.

---

## 11. Out of memory / memory leak

**Symptoms:** Memory grows over time, especially with many profile switches or test runs.

| Check | Action |
|-------|--------|
| Static registries? | `FrameBudgetRegistry`, `GpuOverlayProfilerRegistry`, `InputLatencyRegistry`, `MemoryAllocationRegistry` — call `DisposeAll()` on shutdown (TECH-051). |
| Event subscribers? | Classes with events implement `IDisposable` and null events on dispose (TECH-052). |
| HttpClient? | `ProfileShareService` static HttpClient disposed on shutdown (TECH-055). |

**Root cause:** Static collections growing unbounded, event subscription leaks.

**Fix:** Call `EngineOrchestrator.Shutdown()` which triggers all cleanups.

---

## 12. Empty catch blocks / silent errors

**Symptoms:** Exceptions swallowed, debugging impossible.

| Check | Action |
|-------|--------|
| `catch { }` blocks? | SOUL.md ERROR-001 forbids empty catch. All must log via `AdvancedLogger.Error()` or `Debug.WriteLine()`. |
| `async void` methods? | Can crash app without stacktrace. Use `async Task` + try/catch (TECH-050). |

**Root cause:** Silent error swallowing violates SOUL.md.

**Fix:** Check `MainWindow.xaml.cs`, `UpdateChecker.cs`, `TelemetryService.cs`, `TtsAnnouncerService.cs`, `VoiceChatService.cs`.

---

## 13. Release artifacts contain debug files

**Symptoms:** `release_final/` contains `.pdb`, `.obj`, `.log`, or source code.

| Check | Action |
|-------|--------|
| RULE-001/003? | `release_final/` must contain ONLY binaries, installer, ZIP, manual, license. |
| CI build? | Use Release configuration, clean workspace before packaging. |

**Root cause:** Debug artifacts accidentally included in release package.

**Fix:** Clean `release_final/`, rebuild in Release, verify package contents.

---

## Still stuck?

1. Check `DeveloperConsoleWindow` for real-time logs.
2. Enable `Settings_EnableTelemetry` for anonymous diagnostics.
3. Open a GitHub Issue with: OS version, controller type, RO client version, steps to reproduce, and log excerpt.
