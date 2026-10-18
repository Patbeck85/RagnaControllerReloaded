using System;
using System.Collections.Generic;
using System.Diagnostics;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Core
{
    /// <summary>
    /// ARCH-001: Extracted from HybridEngine - Input routing & modifier parsing.
    /// Responsible for: Layer updates, overlay routing, smart cursor, engine chain routing.
    /// </summary>
    public class InputRouter
    {
        // FIX (ROB-002): Engine-Felder bewusst nicht mehr `readonly`, damit der
        // Failover-only-Konstruktor they leer lassen kann (Failover toucht keine Engine).
        // `= null!` = Deklarations-Init, damit CS8618 (TreatWarningsAsErrors) still is;
        // die Produktion nutzt ausschließlich den Voll-Konstruktor, der all Engines setzt.
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

        // ── ROB-002: Input-Emulation-Failover (delegated to InputFailoverManager) ────
        private readonly InputFailoverManager _failoverManager = new();

        // Cached settings for hot path (avoid Settings.Load() allocation every tick)
        private RightStickPolicy _cachedRightStickPolicy = RightStickPolicy.Hybrid;
        private long _settingsCacheTick = 0;
        private const long SETTINGS_CACHE_TTL_TICKS = 1250; // ~10s bei 125Hz

        /// <summary>The aktuell aktive Emulations-Strategie (SendInput or Kernel).</summary>
        public IMouseEmulationStrategy? ActiveStrategy => _failoverManager.ActiveStrategy;

        /// <summary>ROB-002: Feuert bei jedem Failover-Switch (from, to, Auslöser-Zähler). For UI/Telemetrie.</summary>
        public event Action<string, string, int>? FailoverSwitched
        {
            add => _failoverManager.FailoverSwitched += value;
            remove => _failoverManager.FailoverSwitched -= value;
        }

                /// <summary>
                /// ROB-002: Initialisiert den Input-Emulation-Failover.
                /// Primär = SendInput (Standard), Fallback = Kernel-Strategie (Interception).
                /// </summary>
                public void InitializeFailover(IMouseEmulationStrategy primary, IMouseEmulationStrategy? fallback, Models.Settings settings)
                {
                    _failoverManager.Initialize(primary, fallback, settings);
                }

        /// <summary>
        /// ROB-002: Failover-State-Machine. Is from InputCommandQueue.SendInputLatencyRecorded
        /// on dem Consumer-Thread aufgerufen (no UI-Zugriff → thread-sicher per Design).
        /// N aufeinanderfolgende Flushes about der Latenz-Schwelle → Switch on Kernel-Fallback.
        /// M stabile Flushes danach → Recovery zurück to SendInput.
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
            GestureRecognizer gestureRecognizer)
        {
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
                        public bool RouteInput(ParsedInput input, int actualDeltaMs, bool rumbleEnabled)
                        {
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

                            // UX-016: Gestenerkennung (Right-Stick Swipes for Radial Menu)
                            _gestureRecognizer?.Tick(input.RightX, input.RightY, input.R3);

                            // If Radial Menu offen is: Right-Stick steuert das Menü, nicht den Cursor
                            if (_gestureRecognizer?.IsMenuOpen == true)
                            {
                                // Menu-Input is vom GestureRecognizer verarbeitet (SelectNext/Previous/Activate)
                                // Cursor-Bewegung überspringen if Menü offen
                                if (routeSw != null) RecordRouteBudget(routeSw);
                                return false;
                            }

                            // Update combat layers (L1, R1, L2, R2)
                            _combat.UpdateLayers(input.L1, input.R1, input.L2, input.R2);

                            // Update movement (left stick)
                            _movement.Update(input.LeftX, input.LeftY);

                            // OverlayRouter has priority (Start+DPad shortcuts, Mini-Button)
                            if (_overlayRouter.TryHandleInput(input))
                            {
                                if (routeSw != null) RecordRouteBudget(routeSw);
                                return true;
                            }

                            // SmartCursor: D-Pad Grid-Hopping + Precision-Aiming in menus
                            if (_smartCursor.Tick(input))
                            {
                                if (routeSw != null) RecordRouteBudget(routeSw);
                                return true; // Menu input consumed → skip combat
                            }

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
                                _cursor.Handle(input, actualDeltaMs);
                            }

                            // Engine chain: first engine returning true consumes the input
                            if (!_kite.Handle(input, actualDeltaMs))
                                if (!_autoTarget.Handle(input, actualDeltaMs))
                                    if (!_mage.Handle(input, actualDeltaMs))
                                        _support.Handle(input, actualDeltaMs);

                            // Combo engine (Y button hold)
                            _combo.Update(input.BtnY, actualDeltaMs);

                            // Cooldown tracking
                            _cooldownManager.Tick();

                            // Mob sweep
                            _mobSweep.Update(actualDeltaMs);

                            // Handheld mode
                            _handheld.Tick(input, actualDeltaMs);

                            // Feedback tick (rumble stop via timestamp)
                            _feedback.Tick();

                            if (routeSw != null) RecordRouteBudget(routeSw);
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