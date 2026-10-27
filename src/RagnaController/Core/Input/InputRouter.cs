using System;
using System.Collections.Generic;
using System.Diagnostics;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;

using RagnaController.Core.Services.Input;

namespace RagnaController.Core
{
    /// <summary>
    /// ARCH-001: Extracted from HybridEngine - Input routing & modifier parsing.
    /// Responsible for: Layer updates, overlay routing, smart cursor, engine chain routing.
    /// </summary>
    /// <remarks>
    /// Thread safety: Safe to call from background engine thread.
    /// Not safe for concurrent RouteInput calls from multiple threads.
    /// </remarks>
    /// <seealso cref="TECH-064"/>
    public class InputRouter
    {
        // PERF-006: Per-engine tick profiler (opt-in, zero-allocation when disabled)
        private readonly PerformanceProfiler? _perfProfiler;

        // FIX (ROB-002): Engine-Felder bewusst nicht mehr `readonly`, damit der
        // Failover-only-Konstruktor sie leer lassen kann (Failover toucht keine Engine).
        // `= null!` = Deklarations-Init, damit CS8618 (TreatWarningsAsErrors) still is;
        // production uses only full constructor, which sets all engines.
        private CombatEngine _combat = null!;
        private MovementEngine _movement = null!;
        private AutoTargetEngine _autoTarget = null!;
        private MageEngine _mage = null!;
        private ComboEngine _combo = null!;
        private CursorEngine _cursor = null!;
        private SmartCursorService _smartCursor = null!;
        private KiteEngine _kite = null!;
        private SupportEngine _support = null!;
        private OverlayRouter _overlayRouter = null!;
        private MobSweepEngine _mobSweep = null!;
        private HandheldModeManager _handheld = null!;
        private IFeedbackProvider _feedback = null!;
        private CooldownManager _cooldownManager = null!;

        // ── Radial Menu + Gesten (UX-016) ─────────────────────────────────
        private RadialMenuService? _radialMenu;
        private GestureRecognizer? _gestureRecognizer;

        // ── TASK-3601: Chorded Input ────────────────────────────────────
        private ChordInputService? _chordInputService;

        // ── ROB-002: Input-Emulation-Failover (delegated to InputFailoverManager) ────
        private readonly InputFailoverManager _failoverManager = new();

        // Cached settings for hot path (avoid Settings.Load() allocation every tick)
        private RightStickPolicy _cachedRightStickPolicy = RightStickPolicy.Hybrid;
        private long _settingsCacheTick = 0;
        private const long SETTINGS_CACHE_TTL_TICKS = 1250; // ~10s bei 125Hz

        /// <summary>The aktuell aktive Emulations-Strategie (SendInput or Kernel).</summary>
        public IMouseEmulationStrategy? ActiveStrategy => _failoverManager.ActiveStrategy;

        /// <summary>ROB-002: fires on every failover switch (from, to, trigger counter). For UI/telemetry.</summary>
        public event Action<string, string, int>? FailoverSwitched
        {
            add => _failoverManager.FailoverSwitched += value;
            remove => _failoverManager.FailoverSwitched -= value;
        }

        /// <summary>
        /// ROB-002: Initialisiert den Input-Emulation-Failover.
        /// primary = SendInput (default), fallback = kernel strategy (interception).
        /// </summary>
        public void InitializeFailover(IMouseEmulationStrategy primary, IMouseEmulationStrategy? fallback, Models.Settings settings)
        {
            _failoverManager.Initialize(primary, fallback, settings);
        }

        /// <summary>
        /// ROB-002: Failover-State-Machine. Is from InputCommandQueue.SendInputLatencyRecorded
        /// on dem Consumer-Thread aufgerufen (no UI-Zugriff → thread-sicher per Design).
        /// N aufeinanderfolgende Flushes about der Latenz-Schwelle → Switch on Kernel-Fallback.
        /// M stable flushes after → recovery back to SendInput.
        /// </summary>
        public void RecordSendInputLatency(double latencyMs)
        {
            _failoverManager.RecordLatency(latencyMs);
        }

        public InputRouter(
            CombatEngine combat,
            MovementEngine movement,
            AutoTargetEngine autoTarget,
            MageEngine mage,
            ComboEngine combo,
            CursorEngine cursor,
            SmartCursorService smartCursor,
            KiteEngine kite,
            SupportEngine support,
            OverlayRouter overlayRouter,
            MobSweepEngine mobSweep,
            HandheldModeManager handheld,
            IFeedbackProvider feedback,
            CooldownManager cooldownManager,
            RadialMenuService radialMenu,
            GestureRecognizer gestureRecognizer,
            PerformanceProfiler? perfProfiler = null)
        {
            _perfProfiler = perfProfiler;
            _combat = combat;
            _movement = movement;
            _autoTarget = autoTarget;
            _mage = mage;
            _combo = combo;
            _cursor = cursor;
            _smartCursor = smartCursor;
            _kite = kite;
            _support = support;
            _overlayRouter = overlayRouter;
            _mobSweep = mobSweep;
            _handheld = handheld;
            _feedback = feedback;
            _cooldownManager = cooldownManager;
            _radialMenu = radialMenu;
            _gestureRecognizer = gestureRecognizer;

            // TASK-3601: Chorded Input Service initialisieren
            _chordInputService = new ChordInputService();

            // Gesten-Events with RadialMenuService verdrahten
            _gestureRecognizer.MenuStateChanged += (s, isOpen) =>
            {
                if (isOpen) _radialMenu?.Open();
                else _radialMenu?.Close();
            };
            _gestureRecognizer.ItemNext += (s, e) => _radialMenu?.SelectNext();
            _gestureRecognizer.ItemPrevious += (s, e) => _radialMenu?.SelectPrevious();
            _gestureRecognizer.ItemActivate += (s, e) => _radialMenu?.ActivateSelection();
        }

        /// <summary>
        /// ROB-002: Failover-only-Konstruktor for Unit-Tests (keine Engines).
        /// Only die Failover-State-Machine is in this Instanz active;
        /// RouteInput darf hier NICHT aufgerufen are.
        /// </summary>
        internal InputRouter() { }

        /// <summary>
        /// Route input through the engine chain. Returns true if input was consumed.
        /// </summary>
        /// <param name="actualDeltaMs">Milliseconds elapsed since the last tick.</param>
        /// <param name="rumbleEnabled">Whether haptic feedback/rumble is enabled.</param>
        /// <returns>True if the input was consumed by an engine; false if it should pass through.</returns>
        public bool RouteInput(ParsedInput input, int actualDeltaMs, bool rumbleEnabled)
        {
#pragma warning disable CS8602 // Engine-Felder sind mit null! initialisiert und werden oben auf Null geprüft; nach der Prüfung sind sie nicht null
            // TECH-064: Validate engines are initialized — prevents NullReferenceException
            // when RouteInput is accidentally called on a failover-only InputRouter instance.
            if (_combat == null || _movement == null || _autoTarget == null || _mage == null ||
                _combo == null || _cursor == null || _smartCursor == null || _kite == null ||
                _support == null || _overlayRouter == null || _mobSweep == null || _handheld == null ||
                _feedback == null || _cooldownManager == null)
            {
                var missing = new List<string>();
                if (_combat == null) missing.Add(nameof(_combat));
                if (_movement == null) missing.Add(nameof(_movement));
                if (_autoTarget == null) missing.Add(nameof(_autoTarget));
                if (_mage == null) missing.Add(nameof(_mage));
                if (_combo == null) missing.Add(nameof(_combo));
                if (_cursor == null) missing.Add(nameof(_cursor));
                if (_smartCursor == null) missing.Add(nameof(_smartCursor));
                if (_kite == null) missing.Add(nameof(_kite));
                if (_support == null) missing.Add(nameof(_support));
                if (_overlayRouter == null) missing.Add(nameof(_overlayRouter));
                if (_mobSweep == null) missing.Add(nameof(_mobSweep));
                if (_handheld == null) missing.Add(nameof(_handheld));
                if (_feedback == null) missing.Add(nameof(_feedback));
                if (_cooldownManager == null) missing.Add(nameof(_cooldownManager));

                throw new InvalidOperationException(
                    $"[InputRouter] RouteInput called but engines not initialized: {string.Join(", ", missing)}. " +
                    "Use the full constructor for production; the failover-only constructor is for unit tests only.");
            }

            // GEX-002: Measure Route latency (opt-in, zero-allocation when disabled per PERF-010)
            var routeSw = InputLatencyBudgets.EnableBudgetTracking ? System.Diagnostics.Stopwatch.StartNew() : null;

            // TASK-3601: Chorded Input — wird vor der Engine-Kette geprüft
            var chordSw = _perfProfiler != null && PerformanceProfiler.EnableProfiling ? Stopwatch.StartNew() : null;
            if (_chordInputService != null && _chordInputService.Tick(input, actualDeltaMs, out var chordAction))
            {
                if (chordAction != null)
                {
                    OnActionFired(chordAction, rumbleEnabled);
                    if (chordSw != null) { chordSw!.Stop(); _perfProfiler.RecordEngineTick(nameof(ChordInputService), chordSw!.Elapsed.TotalMilliseconds); }
                    if (routeSw != null) RecordRouteBudget(routeSw);
                    return true;
                }
                if (chordSw != null) { chordSw!.Stop(); _perfProfiler.RecordEngineTick(nameof(ChordInputService), chordSw!.Elapsed.TotalMilliseconds); }
            }

            // UX-016: Gestenerkennung (Right-Stick Swipes for Radial Menu)
            var gestureSw = _perfProfiler != null && PerformanceProfiler.EnableProfiling ? Stopwatch.StartNew() : null;
            _gestureRecognizer?.Tick(input.RightX, input.RightY, input.R3);
            if (gestureSw != null) { gestureSw.Stop(); _perfProfiler.RecordEngineTick(nameof(GestureRecognizer), gestureSw.Elapsed.TotalMilliseconds); }

            // if radial menu open: right stick controls menu, not cursor
            if (_gestureRecognizer?.IsMenuOpen == true)
            {
                // Menu-Input is vom GestureRecognizer verarbeitet (SelectNext/Previous/Activate)
                // skip cursor movement if menu open
                if (routeSw != null) RecordRouteBudget(routeSw);
                return false;
            }

            // Update combat layers (L1, R1, L2, R2)
            var combatLayersSw = _perfProfiler != null && PerformanceProfiler.EnableProfiling ? Stopwatch.StartNew() : null;
            _combat.UpdateLayers(input.L1, input.R1, input.L2, input.R2);
            if (combatLayersSw != null) { combatLayersSw.Stop(); _perfProfiler.RecordEngineTick(nameof(CombatEngine) + ".UpdateLayers", combatLayersSw.Elapsed.TotalMilliseconds); }

            // Update movement (left stick)
            var movementSw = _perfProfiler != null && PerformanceProfiler.EnableProfiling ? Stopwatch.StartNew() : null;
            _movement.Update(input.LeftX, input.LeftY);
            if (movementSw != null) { movementSw.Stop(); _perfProfiler.RecordEngineTick(nameof(MovementEngine), movementSw.Elapsed.TotalMilliseconds); }

            // OverlayRouter has priority (Start+DPad shortcuts, Mini-Button)
            var overlaySw = _perfProfiler != null && PerformanceProfiler.EnableProfiling ? Stopwatch.StartNew() : null;
            if (_overlayRouter.TryHandleInput(input))
            {
                if (overlaySw != null) { overlaySw.Stop(); _perfProfiler.RecordEngineTick(nameof(OverlayRouter), overlaySw.Elapsed.TotalMilliseconds); }
                if (routeSw != null) RecordRouteBudget(routeSw);
                return true;
            }
            if (overlaySw != null) { overlaySw.Stop(); _perfProfiler.RecordEngineTick(nameof(OverlayRouter), overlaySw.Elapsed.TotalMilliseconds); }

            // SmartCursor: D-Pad Grid-Hopping + Precision-Aiming in menus
            var smartCursorSw = _perfProfiler != null && PerformanceProfiler.EnableProfiling ? Stopwatch.StartNew() : null;
            if (_smartCursor.Tick(input))
            {
                if (smartCursorSw != null) { smartCursorSw.Stop(); _perfProfiler.RecordEngineTick(nameof(SmartCursorService), smartCursorSw.Elapsed.TotalMilliseconds); }
                if (routeSw != null) RecordRouteBudget(routeSw);
                return true; // Menu input consumed → skip combat
            }
            if (smartCursorSw != null) { smartCursorSw.Stop(); _perfProfiler.RecordEngineTick(nameof(SmartCursorService), smartCursorSw.Elapsed.TotalMilliseconds); }

            // Cursor (right stick) — controlled by RightStickPolicy setting
            // Camera: always use right stick for camera
            // Aim: only use right stick when target locked
            // Hybrid: target locked → aim, otherwise camera
            // Use cached settings (refresh every ~10s to avoid allocation every tick)
            long currentTick = Environment.TickCount64;
            if ((currentTick - _settingsCacheTick) >= SETTINGS_CACHE_TTL_TICKS)
            {
                var fresh = Models.Settings.Load();
                _cachedRightStickPolicy = fresh.RightStickPolicy;
                _settingsCacheTick = currentTick;
            }

            bool useRightStickForCursor = _cachedRightStickPolicy switch
            {
                RightStickPolicy.Camera => true,
                RightStickPolicy.Aim => _autoTarget.IsTargetLocked,
                RightStickPolicy.Hybrid => _autoTarget.IsTargetLocked || !_movement.IsWalking, // Lock=Aim, else Camera
                _ => !_autoTarget.IsTargetLocked && !_movement.IsWalking // fallback = old behavior
            };

            if (useRightStickForCursor)
            {
                var cursorSw = _perfProfiler != null && PerformanceProfiler.EnableProfiling ? Stopwatch.StartNew() : null;
                _cursor.Handle(input, actualDeltaMs);
                if (cursorSw != null) { cursorSw.Stop(); _perfProfiler.RecordEngineTick(nameof(CursorEngine), cursorSw.Elapsed.TotalMilliseconds); }
            }

            // Engine chain: first engine returning true consumes the input
            var kiteSw = _perfProfiler != null && PerformanceProfiler.EnableProfiling ? Stopwatch.StartNew() : null;
            if (!_kite.Handle(input, actualDeltaMs))
            {
                if (kiteSw != null) { kiteSw.Stop(); _perfProfiler.RecordEngineTick(nameof(KiteEngine), kiteSw.Elapsed.TotalMilliseconds); }
                var autoTargetSw = _perfProfiler != null && PerformanceProfiler.EnableProfiling ? Stopwatch.StartNew() : null;
                if (!_autoTarget.Handle(input, actualDeltaMs))
                {
                    if (autoTargetSw != null) { autoTargetSw.Stop(); _perfProfiler.RecordEngineTick(nameof(AutoTargetEngine), autoTargetSw.Elapsed.TotalMilliseconds); }
                    var mageSw = _perfProfiler != null && PerformanceProfiler.EnableProfiling ? Stopwatch.StartNew() : null;
                    if (!_mage.Handle(input, actualDeltaMs))
                    {
                        if (mageSw != null) { mageSw.Stop(); _perfProfiler.RecordEngineTick(nameof(MageEngine), mageSw.Elapsed.TotalMilliseconds); }
                        var supportSw = _perfProfiler != null && PerformanceProfiler.EnableProfiling ? Stopwatch.StartNew() : null;
                        _support.Handle(input, actualDeltaMs);
                        if (supportSw != null) { supportSw.Stop(); _perfProfiler.RecordEngineTick(nameof(SupportEngine), supportSw.Elapsed.TotalMilliseconds); }
                    }
                }
            }
            else
            {
                if (kiteSw != null) { kiteSw.Stop(); _perfProfiler.RecordEngineTick(nameof(KiteEngine), kiteSw.Elapsed.TotalMilliseconds); }
            }

            // Combo engine (Y button hold)
            var comboSw = _perfProfiler != null && PerformanceProfiler.EnableProfiling ? Stopwatch.StartNew() : null;
            _combo.Update(input.BtnY, actualDeltaMs);
            if (comboSw != null) { comboSw.Stop(); _perfProfiler.RecordEngineTick(nameof(ComboEngine), comboSw.Elapsed.TotalMilliseconds); }

            // Cooldown tracking
            var cooldownSw = _perfProfiler != null && PerformanceProfiler.EnableProfiling ? Stopwatch.StartNew() : null;
            _cooldownManager.Tick();
            if (cooldownSw != null) { cooldownSw.Stop(); _perfProfiler.RecordEngineTick(nameof(CooldownManager), cooldownSw.Elapsed.TotalMilliseconds); }

            // Mob sweep
            var mobSweepSw = _perfProfiler != null && PerformanceProfiler.EnableProfiling ? Stopwatch.StartNew() : null;
            _mobSweep.Update(actualDeltaMs);
            if (mobSweepSw != null) { mobSweepSw.Stop(); _perfProfiler.RecordEngineTick(nameof(MobSweepEngine), mobSweepSw.Elapsed.TotalMilliseconds); }

            // Handheld mode
            var handheldSw = _perfProfiler != null && PerformanceProfiler.EnableProfiling ? Stopwatch.StartNew() : null;
            _handheld.Tick(input, actualDeltaMs);
            if (handheldSw != null) { handheldSw.Stop(); _perfProfiler.RecordEngineTick(nameof(HandheldModeManager), handheldSw.Elapsed.TotalMilliseconds); }

            // Feedback tick (rumble stop via timestamp)
            var feedbackSw = _perfProfiler != null && PerformanceProfiler.EnableProfiling ? Stopwatch.StartNew() : null;
            _feedback.Tick();
            if (feedbackSw != null) { feedbackSw.Stop(); _perfProfiler.RecordEngineTick(nameof(IFeedbackProvider), feedbackSw.Elapsed.TotalMilliseconds); }

            if (routeSw != null) RecordRouteBudget(routeSw);
#pragma warning restore CS8602
            return false; // Input not consumed by router (processed by engines)
        }

        /// <summary>
        /// GEX-002: Record Route latency budget.
        /// </summary>
        private void RecordRouteBudget(Stopwatch sw)
        {
            sw.Stop();
            InputLatencyBudgets.RecordRoute(sw.Elapsed.TotalMilliseconds);
        }

        /// <summary>
        /// Handle combat action fired event - triggers feedback and cooldown tracking
        /// </summary>
        public void OnActionFired(ButtonAction action, bool rumbleEnabled)
        {
            if (rumbleEnabled) _feedback.TriggerSkillFired();
            _cooldownManager.RegisterAction(action);
        }

        public void Dispose()
        {
            _gestureRecognizer?.Dispose();
            _radialMenu?.Dispose();
        }
    }
}