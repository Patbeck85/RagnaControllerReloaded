# Known Limitations — RagnaController

Documented limitations and design decisions. These are not bugs — they are architectural
trade-offs or missing features planned for future releases.

---

## Input & Emulation

### BezierMouseService blocks consumer thread (TECH-038 — WONT_FIX)

**Limitation:** `queue.Wait(stepDelay)` calls `Thread.Sleep` inside the consumer thread,
blocking the entire input pipeline for the duration of the Bezier move.

**Impact:** Input latency spikes during Bezier moves; combat engine sees delayed inputs.

**Workaround:** Use short Bezier durations. The blocking behavior is intentional for
curve smoothness; refactoring would require significant architecture changes.

---

### Interception absolute moves fallback (TECH-041 — documented)

**Limitation:** Interception only supports relative mouse moves. `MoveAbsolute` falls back
to Win32 `SetCursorPos`.

**Impact:** Absolute moves are detectable by anti-cheat; not truly kernel-mode.

**Workaround:** Use SendInput backend for absolute moves if anti-cheat detection is a concern.

---

### Right stick policy cached 10s (TECH-042 — fixed)

**Limitation:** RightStickPolicy refreshes every ~10s (1250 ticks). Profile changes may
take up to 10s to take effect.

**Fix:** `InvalidateSettingsCache()` added to `InputRouter`, called by `ProfileApplier`
on profile load.

---

### SmartCursor 10x8 grid assumption (TECH-043 — WONT_FIX)

**Limitation:** `CalculateSlotIndex` assumes fixed 10x8 inventory grid.

**Impact:** Multi-selection slot indices wrong on non-standard grids (custom RO servers).

**Workaround:** Standard RO inventory is 10x8. Customization can be added later if needed.

---

## Performance

### Tick budget < 2ms (PERF-003)

**Limitation:** Tick loop must complete within 2ms to maintain 125Hz.

**Impact:** Exceeding budget causes input latency spikes.

**Workaround:** Monitor `FrameBudgetMonitor`. Avoid expensive operations in tick path.

---

### Input latency P99 ≤ 8ms (GEX-002)

**Limitation:** End-to-end latency budget: Parse ≤1ms, Route ≤2ms, Queue ≤1ms,
Emulation ≤4ms, Total ≤8ms.

**Impact:** Latency spikes cause missed casts or failed clicks in competitive RO play.

**Workaround:** Budget tracking is opt-in (`EnableBudgetTracking`) to preserve zero-allocation
hot path. Enable in tests/CI.

---

## UI & Accessibility

### High contrast mode not fully tested (GEX-020 — planned)

**Limitation:** Windows High Contrast + 150% scaling not verified across all 21 windows.

**Impact:** Clipped text or invisible focus rings possible in high contrast mode.

**Workaround:** Standard 100% scaling works correctly.

---

### RTL languages not verified (GEX-020 — planned)

**Limitation:** Arabic/Hebrew right-to-left UI flow not tested.

**Impact:** UI may not flow correctly in RTL locales.

**Workaround:** All 41 locale files contain translation keys; RTL layout not yet validated.

---

### Screen reader (NVDA) not fully verified (GEX-020 — planned)

**Limitation:** NVDA walkthrough of all 21 windows not completed.

**Impact:** Screen reader users may encounter missing labels or illogical tab order.

**Workaround:** 22 icon-only buttons have `AutomationProperties.Name` (UX-001).

---

## Release & Packaging

### Code signing not configured (GEX-019 — planned)

**Limitation:** Release binaries not code-signed (no certificate available).

**Impact:** Windows SmartScreen may flag unsigned binaries.

**Workaround:** Users must allow execution through SmartScreen warning.

---

### Auto-update not end-to-end verified (GEX-019 — planned)

**Limitation:** `UpdateChecker` fetches from GitHub Releases but end-to-end auto-update
not tested against staging release.

**Impact:** Auto-update may fail in production.

**Workaround:** Manually download latest release from GitHub Releases.

---

## Testing & CI

### Real RO client soak not available (GEX-016 — infrastructure required)

**Limitation:** Soak tests use mocked RO client. Real rAthena test server + RO client
not set up in CI.

**Impact:** Mock soak misses window focus issues, DPI scaling, anti-cheat, network latency.

**Workaround:** Mock soak (10k ticks) runs in CI and validates memory/handles.

---

### Property-based testing limited to input queue (GEX-015 — partial)

**Limitation:** FsCheck properties cover `InputCommandQueue` only. Full pipeline
(Reader → ParsedInput → Router → Queue → Strategy) not yet property-tested.

**Impact:** Rare input combinations in full pipeline not property-tested.

**Workaround:** Unit and integration tests cover pipeline behavior deterministically.

---

## Architecture

### EngineOrchestrator is a God Class (TECH-021 — WONT_FIX)

**Limitation:** `EngineOrchestrator` (686 lines, 20+ engine fields) instantiates, wires,
and manages all engines.

**Impact:** High coupling, harder to test, SRP violation.

**Workaround:** Existing code is stable (Build 0 Warnings, tests green). Refactoring risk >
benefit for hobby project. `IEngine` + `EngineRegistry` pattern prototyped but not integrated.

---

### ProfileManager constructor does File I/O (TECH-023 — WONT_FIX)

**Limitation:** Constructor performs file I/O and registers static events.

**Impact:** Startup latency, hard to unit-test, static state leaks between tests.

**Workaround:** User directive — S1 data loss risk on refactoring. Existing architecture stable.

---

### Multi-engine model not implemented (FEAT-015 — YAGNI)

**Limitation:** No parallel engine model, no UI/Settings, no client selector for multi-client.

**Impact:** Only routing override point (`PreferredClientHwnd`) available for Alt-Char/Farming.

**Workaround:** API is prepared; later FEAT can bind `PreferredClientHwnd` to client selector.

---

## Localization

### 500+ hardcoded strings (TECH-011 — known)

**Limitation:** German locale hardcoded in .cs files. No .resx or JSON localization for UI text.

**Impact:** No multi-language support possible without refactoring.

**Workaround:** User prefers German. Log messages can remain English.

---

## Compliance

### SOUL.md ERROR-001 compliance (TECH-049/TECH-050 — partially done)

**Limitation:** Some `catch { }` blocks and `async void` methods may remain in less-traveled
code paths.

**Impact:** Silent error swallowing or unhandled exceptions in edge cases.

**Workaround:** All identified instances in main code paths fixed. Remaining items in
`App.xaml.cs` Task.Run, `DualSenseHardwareService`, `AdvancedLogger`, `BackgroundTickProvider`,
`CombatEngine`, `EngineWatchdog`, `TelemetryService`, `TtsAnnouncerService`, `VoiceChatService`
are existing patterns with `IsBackground = true` and app lifetime — low risk.
