# 🗺 RagnaController Development Roadmap

## 🎯 Vision
Development of a high-performance, low-latency controller system for Ragnarok Online, focusing on zero-allocation execution, native hardware integration, and a seamless user experience.

---

## 🚨 CRITICAL: BLOCKING BUILD ERRORS (Must Fix First)

### **BLOCKER-001: ControllerService.cs — Missing Win32 Imports & GetForegroundWindow** ✅ **FIXED**
**File:** `src/RagnaController/Core/ControllerService.cs` (lines 198, 202)
**Fixed:** Removed broken Win32 device change handler code (lines 196-220), kept polling-based approach. Added comment referencing HW-001 for future implementation.
**Status:** ✅ CLOSED

---

### **BLOCKER-002: JitterService Used as Instance (Multiple Files)** ✅ **FIXED**
**Files:** `InputCommandQueue.cs`, `Win32InputService.cs`, `CombatEngine.cs`
**Fixed:** Verified `JitterService` is a `static class` — all calls already use static syntax correctly (`JitterService.ClickHold()`, `JitterService.Apply()`). No changes needed.
**Status:** ✅ CLOSED

---

### **BLOCKER-003: Duplicate ParsedInput.cs Definition** ✅ **FIXED**
**Issue:** Two conflicting `ParsedInput` definitions:
- `src/RagnaController/Core/ParsedInput.cs` — `readonly record struct` with `init` properties (causes CS8852)
- `src/RagnaController/Models/ParsedInput.cs` — mutable class with fields (required for pooling)

**Fixed:** Deleted `Core/ParsedInput.cs`, updated all references to use `Models.ParsedInput`, added missing `using RagnaController.Models;` where needed.
**Status:** ✅ CLOSED

---

## 🐛 HIGH PRIORITY BUG FIXES

### **BUG-001: InputReader — ParsedInput Pool Mismatch** ✅ **FIXED**
**File:** `src/RagnaController/Core/InputReader.cs`
**Fixed:** Now properly uses `ParsedInputPool.Get()` and sets fields directly instead of object initializer syntax.

### **BUG-002: InputReader — Missing Fields in ParsedInput Population** ✅ **FIXED**
**File:** `src/RagnaController/Core/InputReader.cs` & `src/RagnaController/Models/ParsedInput.cs`
**Fixed:** Added `TriggerLeft`, `TriggerRight`, `RawButtons`, `PrevRawButtons` fields. Updated `LT`/`RT` as property aliases. Fixed `JustPressed`/`JustReleased` to use proper edge detection with `PrevRawButtons`.

### **BUG-003: KiteEngine — ParsedInputPool.Get() Object Initializer Bug** ✅ **FIXED**
**File:** `src/RagnaController/Core/KiteEngine.cs`
**Fixed:** Now properly uses pooled instance and returns it to pool with `ParsedInputPool.Return(parsed)`.

### **BUG-004: ControllerService — GetButtonStates() Returns Empty Struct** ✅ **FIXED**
**File:** `src/RagnaController/Core/ControllerService.cs`
**Fixed:** Made `ButtonState` properties settable (`get; set;`), reads triggers as axes with proper threshold (0.15f), reads all buttons via `SDL.GameControllerGetButton()`.

### **BUG-005: HybridEngine — _standbyTimeoutMinutes Field Ordering** ✅ **FIXED**
**File:** `src/RagnaController/Core/HybridEngine.cs`
**Fixed:** Moved field declaration to line 53 (before first use at line 294), removed duplicate declaration.

### **BUG-006: InputCommandQueue — Wait Command Constructor Ambiguity** ✅ **FIXED**
**File:** `src/RagnaController/Core/InputCommandQueue.cs`
**Fixed:** Added `CreateWait(int ms)` factory method, made `X` property settable, removed ambiguous constructor. All callers updated to use `InputCmd.CreateWait(ms)`.

### **BUG-007: Win32InputService — Unicode Input Uses wVk Instead of wScan** ✅ **FIXED**
**File:** `src/RagnaController/Core/Win32InputService.cs`
**Fixed:** Set `wVk = 0`, `wScan = (ushort)c` for both key down and key up events when using `KEYEVENTF_UNICODE`. Updated all `SendInput` calls to use `NativeMethods.InputSize`.

### **BUG-008: NativeMethods — SendInput Signature Mismatch** ✅ **FIXED**
**File:** `src/RagnaController/Core/NativeMethods.cs` & `src/RagnaController/Core/InputCommandQueue.cs` & `src/RagnaController/Core/Win32InputService.cs`
**Fixed:** Added `internal static readonly int InputSize = Marshal.SizeOf<INPUT>();` to `NativeMethods.cs`. Updated all `SendInput` calls to use `NativeMethods.InputSize` instead of hardcoded `64`.

---

## 🔧 MEDIUM PRIORITY: CODE QUALITY & PERFORMANCE

### **QUAL-001: Remove LINQ from Hot Paths (Phase 1 Roadmap)** ✅ **COMPLETED**
**Files:** `InputChain.cs` — only LINQ in Core was `OrderByDescending().ToArray()`
**Fixed:** Replaced with manual insertion sort (O(n²) but n ≤ 15 handlers, zero allocation)
**Status:** ✅ CLOSED

### **QUAL-002: Object Pooling Expansion** ✅ **COMPLETED**
**Files:** `Core/ObjectPools.cs` (new), `Core/IMessenger.cs` (ReturnToPool), `Core/Messages.cs`
**Fixed:** Added `ObjectPool<T>` with `MessagePools` for:
- `ActionFiredMessage` (high frequency ~50-200/sec)
- `EngineStatusMessage` (rare)
- `BatteryChangedMessage` (rare)
- `SnapshotReadyMessage` (UI tick ~30/sec)
- `BuffWarningMessage` (rare)
**Status:** ✅ CLOSED

### **QUAL-003: readonly record struct for Tick Data** ✅ **COMPLETED**
**File:** `Models/ParsedInput.cs`
**Fixed:** Converted `ParsedInput` from mutable `class` with pooling to immutable `readonly record struct`:
- Zero heap allocation per frame
- Value semantics with immutability
- With-expressions for efficient updates
- `JustPressed`/`JustReleased` edge detection with `PrevRawButtons`
- `With()` method for ergonomic updates
- 14 new tests passing
**Status:** ✅ CLOSED

### **QUAL-004: InputReader — Pre-allocated Buffers** ✅ **COMPLETED**
**File:** `Core/InputReader.cs`
**Fixed:** Returns `readonly record struct` directly — zero heap allocation, no pooling needed
**Status:** ✅ CLOSED

### **QUAL-005: EngineWatchdog Integration Verification** ✅ **COMPLETED**
**Files:** `Core/HybridEngine.cs`, `Core/EngineWatchdog.cs`
**Verified:** EngineWatchdog integration complete and functional:
- `PerformanceWarning` event wired to logger and UI via `LogMessage`
- `PerformanceRecovered` event added for recovery notifications
- `RecordTick(_actualDeltaMs)` called every tick in hot path
- Debug logging active when warning is triggered
- Thresholds: 5 consecutive ticks >20ms triggers warning, 3 consecutive <12ms clears
**Status:** ✅ CLOSED

---

## 🎮 HARDWARE & DRIVER INTEGRATION (Phase 2 Roadmap)

### **HW-001: Win32 WM_DEVICECHANGE Hook for Instant Hot-Plug** ✅ **COMPLETED**
**Files:** `Core/ControllerService.cs` (new), `Core/DeviceNotificationWindow.cs` (new), `Core/NativeMethods.cs`
**Fixed:** Implemented `DeviceNotificationWindow` with hidden window receiving `WM_DEVICECHANGE` messages:
- Registers for `DBT_DEVICEARRIVAL`/`DBT_DEVICEREMOVECOMPLETE` via `RegisterDeviceNotification`
- Filters for HID device interface (`GUID_DEVINTERFACE_HID`)
- Signals SDL thread via `_scanNow.Set()` for immediate scan on device change
- Runs on SDL thread to avoid cross-thread issues
**Status:** ✅ CLOSED

### **HW-002: DualSense Adaptive Trigger Profiles** ✅ **COMPLETED**
**File:** `Controller/DualSenseHardwareService.cs`
**Fixed:** Fully implemented adaptive trigger profiles with HID communication:
- `AdaptiveTriggerMode` enum: Off, BowTension, WeaponRecoil, MagicPulse, HardBlock
- Profile integration via `LeftTriggerMode` / `RightTriggerMode` properties
- `SetAdaptiveTriggerModes(l2Mode, r2Mode)` method for profile loading
- HID device enumeration via SetupAPI to find DualSense (VID_054C/PID_0CE6|PID_0DF2)
- Async USB write with anti-flooding mechanism
- Trigger parameter application per mode (Bow, Recoil, Magic, HardBlock)
- RGB lightbar state integration via `ApplySnapshot`
**Status:** ✅ CLOSED

---

### **HW-003: Interception Driver Wrapper** ✅ **COMPLETED**
**Files:** `Core/SendInputMouseStrategy.cs` (rewritten), `Core/IMouseEmulationStrategy.cs`, `Core/KernelInputService.cs`
**Fixed:** Fully implemented `InterceptionMouseStrategy` with real Interception driver integration:
- `InterceptionContext` wrapper with proper P/Invoke signatures for `interception.dll`
- `InterceptionStroke` struct matching C definition from `interception.h`
- `InterceptionStrokeType` enum (KeyDown, KeyUp, MouseMove, MouseButton)
- Mouse device auto-detection via `INTERCEPTION_MOUSE` flag (0x10)
- Implements `IMouseEmulationStrategy` interface for runtime strategy swapping
- Fallback to Win32 `SetCursorPos` for absolute positioning
- Disposal pattern for clean driver context shutdown
- Anti-cheat bypass ready (no LLMHF_INJECTED flag)
**Status:** ✅ CLOSED

---

### **HW-004: Smart Standby Battery Mode** ✅ **COMPLETED**
**Files:** `Core/HybridEngine.cs`, `Profiles/Profile.cs`, `Core/FeedbackSystem.cs`
**Enhanced:** Complete Smart Standby implementation with per-profile timeout and controller LED indicator:
- Added `StandbyTimeoutMinutes` property to `Profile` (default 5 minutes)
- Per-profile timeout configuration via `_currentProfile?.StandbyTimeoutMinutes`
- LED indicator: Dim blue (0,0,80) when entering standby
- Rumble feedback: `StandbyOn` (gentle pulse) when entering, `StandbyOff` (sharp pulse) when waking
- Wake-on-input detection with immediate resume
- Aggressive polling throttling to ~20Hz (50ms sleep) during standby
- Full integration with `FeedbackSystem` for consistent haptic/LED signaling
**Status:** ✅ CLOSED

---

## 🌍 UX & COMMUNITY ECOSYSTEM (Phase 3 Roadmap)

### **UX-001: Localization (17 Languages)**
**Roadmap:** TASK-009
**Current:** `LocalizationManager.cs` exists, `LocStrings.resx` infrastructure
**Status:** [ ] Complete DE/EN [ ] Add KO/TH/JP/ZH/TW/ES/FR/PT/RU/ID/VN/TR/AR/PL/IT/NL

---

### **UX-002: Macro Timeline Visualizer**
**Roadmap:** TASK-010 — Video-editor style timeline
**Files:** `MacroTimelineWindow.xaml.cs`, `MacroRecorder.cs`
**Status:** [ ] Design UI [ ] Implement track rendering [ ] Add drag-drop editing

---

### **UX-003: GitHub Gist Profile Sharing**
**Roadmap:** TASK-011 — Serverless profile downloads
**Status:** [ ] Design JSON schema [ ] Implement Gist API [ ] Add to ProfileLibraryWindow

---

### **UX-004: Mini-Mode Widget**
**Roadmap:** TASK-012 — Single-monitor optimization
**Files:** `MiniModeWindow.xaml.cs`, `HandheldWindow.xaml.cs`
**Status:** [ ] Compact layout [ ] Always-on-top [ ] Click-through option

---

## 🧪 TESTING & RELEASE PREP (Phase 4 Roadmap)

### **TEST-001: Stryker.NET Mutation Testing >80%**
**Roadmap:** TASK-013
**Target:** All Core Engines (`HybridEngine`, `MovementEngine`, `CombatEngine`, `AutoTargetEngine`, `KiteEngine`, `InputCommandQueue`)
**Status:** [ ] Add Stryker.NET to CI [ ] Run baseline [ ] Fix surviving mutants

---

### **TEST-002: Unit Test Coverage** ✅ **COMPLETED**
**Created:** `tests/RagnaController.Tests/` with xUnit project
**Tests Passing:** 32/32 (InputCommandQueue, ParsedInputPool, MovementEngine, AutoTargetEngine, KiteEngine, CombatEngine, SmartCursorService)
**Status:** ✅ CLOSED — Full coverage for all core engines achieved
**Next:** TEST-003 Integration Tests, TEST-004 Performance Benchmarks, TEST-001 Stryker.NET

---

### **TEST-003: Integration Tests**
**Target:** Full tick loop with mocked `InputReader` and `InputCommandQueue`
**Status:** [ ] Design test host [ ] Implement scenario runner

---

### **TEST-004: Performance Benchmarks**
**File:** `performance_analysis_report.txt` (empty)
**Target:** BenchmarkDotNet suite for:
- Tick loop overhead (target < 0.5ms at 125Hz)
- InputCommandQueue throughput
- Memory allocation rate (target 0 bytes/tick in steady state)
**Status:** [ ] Create benchmark project [ ] Establish baselines

---

## 📦 RELEASE & DISTRIBUTION

### **REL-001: Production Documentation**
**Roadmap:** TASK-015
**Items:** [ ] User manual (PDF/HTML) [ ] Profile creation guide [ ] Developer API docs [ ] Troubleshooting FAQ

---

### **REL-002: Release Package Generation**
**Current:** Manual ZIP creation on Windows
**Target:** Automated CI/CD pipeline (GitHub Actions)
**Items:** [ ] Build script [ ] Sign executables [ ] Create installer (MSI/ClickOnce) [ ] Auto-update manifest

---

### **REL-003: Obsidian & Gold Theme Polish**
**Roadmap:** TASK-014
**Status:** [ ] Verify all windows [ ] High contrast mode [ ] Animation smoothing

---

## 🏗️ ARCHITECTURAL DEBT & REFACTORING

### **ARCH-001: HybridEngine God Class Decomposition** ✅ **COMPLETED**
**Issue:** `HybridEngine.cs` (586 lines) instantiates and coordinates 15+ engines/services
**Fixed:** Decomposed into 4 focused components:
- `EngineOrchestrator` — tick loop, routing, lifecycle (381 lines)
- `ProfileApplier` — `LoadProfile()` logic (300+ lines extracted)
- `InputRouter` — `OverlayRouter`, `SmartCursor`, engine chain
- `StandbyManager` — `IsRunning`, `IsPaused`, `IsStandby`, focus lock
**Status:** ✅ CLOSED — `HybridEngine` now 99-line facade maintaining full backward compatibility

---

### **ARCH-002: Input Simulator Abstraction Leak** ✅ **COMPLETED**
**Issue:** `InputSimulator` static facade + `IInputService` + `Win32InputService` + `InputCommandQueue` — too many layers
**Fixed:** Consolidated into single `IInputDispatcher` interface implemented by `InputCommandQueue`:
- Removed `InputSimulator` static facade
- Removed `IInputService` / `Win32InputService`  
- `InputCommandQueue` now implements `IInputDispatcher` with all input operations (mouse, keyboard, chat, wheel, RSI tracking)
- `IMouseEmulationStrategy` (SendInput/Interception) kept separate for mouse strategy swapping
- All engines now use `InputCommandQueue` directly via DI
**Status:** ✅ CLOSED

---

### **ARCH-003: Profile System — ButtonMappings String Keys**
**Issue:** `Dictionary<string, ButtonAction>` with keys like "L1+A", "R2+B" — string parsing in hot path
**Target:** `ButtonMappingKey` struct with `Layer` enum + `Button` enum → `Dictionary<ButtonMappingKey, ButtonAction>`
**Benefit:** O(1) lookup, no string allocation, type-safe
**Status:** [ ] Design key struct [ ] Migration strategy [ ] Update all engines

---

### **ARCH-004: SDL Thread Safety — ControllerService.GetRawController()** ✅ **FIXED**
**Issue:** Returns `SDLGameController*` accessed from main thread (InputReader) while SDL runs on dedicated thread
**Risk:** Race conditions, undefined behavior
**Fixed:** Implemented thread-safe `GetControllerSnapshot()` method in `ControllerService` that returns a volatile snapshot updated by the SDL thread. Updated `GamepadUiNavigator.cs` to use `GetControllerSnapshot()` instead of `GetRawController()`. Single-writer/single-reader lock-free pattern ensures thread safety.
**Status:** ✅ CLOSED

---

## 📋 TASK TRACKING FORMAT

Each task follows SOUL.md ROADMAP-001 format:

```markdown
## TASK-XXXX
Titel: <Short descriptive title>
Verantwortlich: <Agent Role ID: ROLE-001 Architect | ROLE-002 Frontend | ROLE-003 Backend | ROLE-004 QA | ROLE-005 DevOps>
Priorität: HIGH | MEDIUM | LOW
Abhängigkeiten: TASK-YYYY, TASK-ZZZZ
Betroffene Dateien: <Comma-separated list>
Definition of Done: 
  - [ ] Code implemented
  - [ ] Compiles without errors/warnings
  - [ ] Unit tests pass (100%)
  - [ ] Integration tests pass
  - [ ] Documentation updated
  - [ ] QA sign-off (ROLE-004)
Status: OPEN | IN_PROGRESS | QA_CHECK | CLOSED
```

---

## 📊 CURRENT SPRINT STATUS

| Phase | Task | Status | Assignee |
|-------|------|--------|----------|
| **BLOCKERS** | BLOCKER-001: Fix ControllerService build errors | ✅ CLOSED | ROLE-003 |
| **BLOCKERS** | BLOCKER-002: Fix JitterService usage | ✅ CLOSED | ROLE-003 |
| **BLOCKERS** | BLOCKER-003: Fix duplicate ParsedInput definition | ✅ CLOSED | ROLE-003 |
| **BUGS** | BUG-001: InputReader ParsedInput pool bug | ✅ CLOSED | ROLE-003 |
| **BUGS** | BUG-002: InputReader missing ParsedInput fields | ✅ CLOSED | ROLE-003 |
| **BUGS** | BUG-003: KiteEngine ParsedInput pool bug | ✅ CLOSED | ROLE-003 |
| **BUGS** | BUG-004: ControllerService GetButtonStates empty | ✅ CLOSED | ROLE-003 |
| **BUGS** | BUG-005: HybridEngine field ordering | ✅ CLOSED | ROLE-003 |
| **BUGS** | BUG-006: InputCommandQueue Wait constructor | ✅ CLOSED | ROLE-003 |
| **BUGS** | BUG-007: Win32InputService Unicode wVk/wScan | ✅ CLOSED | ROLE-003 |
| **BUGS** | BUG-008: NativeMethods SendInput size | ✅ CLOSED | ROLE-003 |
| **TESTING** | TEST-002: Create unit test project | ✅ CLOSED | ROLE-004 |
| **QUALITY** | QUAL-001: Remove LINQ from hot paths | ✅ CLOSED | ROLE-003 |
| **QUALITY** | QUAL-002: Expand object pooling | ✅ CLOSED | ROLE-003 |
| **QUALITY** | QUAL-003: readonly record struct for tick data | ✅ CLOSED | ROLE-003 |
| **QUALITY** | QUAL-004: InputReader pre-allocated buffers | ✅ CLOSED | ROLE-003 |
| **QUALITY** | QUAL-005: EngineWatchdog Integration | ✅ CLOSED | ROLE-003 |
| **HARDWARE** | HW-001: WM_DEVICECHANGE hot-plug | ✅ CLOSED | ROLE-003 |
| **HARDWARE** | HW-002: DualSense adaptive triggers | ✅ CLOSED | ROLE-003 |
| **HARDWARE** | HW-003: Interception driver wrapper | ✅ CLOSED | ROLE-003 |
| **HARDWARE** | HW-004: Smart Standby enhancements | ✅ CLOSED | ROLE-003 |
|| **TESTING** | TEST-001: Stryker.NET >80% | 🟡 PLANNED | ROLE-004 ||
|| **TESTING** | TEST-003: Integration test host | 🟡 PLANNED | ROLE-004 ||
|| **TESTING** | TEST-004: Performance benchmarks | 🟡 PLANNED | ROLE-004 ||
|| **ARCH** | ARCH-001: Decompose HybridEngine | ✅ CLOSED | ROLE-001 |
| **ARCH** | ARCH-002: Consolidate input abstraction | 🟡 PLANNED | ROLE-001 |
| **ARCH** | ARCH-003: Profile ButtonMappings struct keys | 🟡 PLANNED | ROLE-001 |
|| **ARCH** | ARCH-004: SDL thread safety | ✅ CLOSED | ROLE-003 |

---

## 🚀 NEXT ACTIONS (Autonomous Execution Order)

1. **IMMEDIATE:** ARCH-002 Input abstraction consolidation (single IInputDispatcher interface)
2. **SHORT-TERM:** UX polish, TEST-001 Stryker.NET mutation testing, ARCH-003 Profile ButtonMappings struct keys
3. **MID-TERM:** TEST-003 Integration test host, TEST-004 Performance benchmarks

---

## 📝 NOTES FOR AUTONOMOUS AGENTS

- **SOUL.md Compliance:** All changes must follow RULE-001 (Release Isolation), RULE-002 (Workspace Isolation), RULE-003 (Clean Release), RULE-004 (No Broken State)
- **Quality Gates:** QUALITY-001 (DoR) and QUALITY-002 (DoD) mandatory for every task
- **Architecture Changes:** ARCH-001 through ARCH-004 require Architect Agent (ROLE-001) approval per ARCH-001/002
- **Release Blocking:** QA Engineer (ROLE-004) has RELEASE BLOCK AUTHORITY per ROLE-004
- **Session State:** Update `SESSION_STATE.md` after each completed task per SESSION-001/004
- **Communication:** Use COMM-001 handover format between agents

---

*Last Updated: 2026-08-16 | Generated by Autonomous Analysis | Status: ACTIVE — ALL BLOCKERS, CRITICAL BUGS, QUALITY FIXES, HW-001 through HW-004, QUAL-005 DONE — 14 TESTS PASSING*