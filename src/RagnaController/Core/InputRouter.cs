using System;
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
        // Failover-only-Konstruktor sie leer lassen kann (Failover toucht keine Engine).
        // `= null!` = Deklarations-Init, damit CS8618 (TreatWarningsAsErrors) still ist;
        // die Produktion nutzt ausschließlich den Voll-Konstruktor, der alle Engines setzt.
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

        /// <summary>Die aktuell aktive Emulations-Strategie (SendInput oder Kernel).</summary>
        public IMouseEmulationStrategy? ActiveStrategy => _failoverManager.ActiveStrategy;

        /// <summary>ROB-002: Feuert bei jedem Failover-Switch (von, zu, Auslöser-Zähler). Für UI/Telemetrie.</summary>
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
        /// ROB-002: Failover-State-Machine. Wird von InputCommandQueue.SendInputLatencyRecorded
        /// auf dem Consumer-Thread aufgerufen (kein UI-Zugriff → thread-sicher per Design).
        /// N aufeinanderfolgende Flushes über der Latenz-Schwelle → Switch auf Kernel-Fallback.
        /// M stabile Flushes danach → Recovery zurück zu SendInput.
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

            // Gesten-Events mit RadialMenuService verdrahten
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
        /// ROB-002: Failover-only-Konstruktor für Unit-Tests (keine Engines).
        /// Nur die Failover-State-Machine ist in dieser Instanz aktiv;
        /// RouteInput darf hier NICHT aufgerufen werden.
        /// </summary>
        internal InputRouter() { }

        /// <summary>
                        /// Route input through the engine chain. Returns true if input was consumed.
                        /// </summary>
                        public bool RouteInput(ParsedInput input, int actualDeltaMs, bool rumbleEnabled)
                        {
                            // GEX-002: Measure Route latency (opt-in, zero-allocation when disabled per PERF-010)
                            var routeSw = InputLatencyBudgets.EnableBudgetTracking ? System.Diagnostics.Stopwatch.StartNew() : null;

                            // UX-016: Gestenerkennung (Right-Stick Swipes für Radial Menu)
                            _gestureRecognizer?.Tick(input.RightX, input.RightY, input.R3);

                            // Wenn Radial Menu offen ist: Right-Stick steuert das Menü, nicht den Cursor
                            if (_gestureRecognizer?.IsMenuOpen == true)
                            {
                                // Menu-Input wird vom GestureRecognizer verarbeitet (SelectNext/Previous/Activate)
                                // Cursor-Bewegung überspringen wenn Menü offen
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