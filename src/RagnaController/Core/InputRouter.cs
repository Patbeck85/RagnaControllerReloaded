using System;
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

        // ── ROB-002: Input-Emulation-Failover (SendInput ↔ Kernel) ─────────────
                private IMouseEmulationStrategy? _failoverPrimary;
                private IMouseEmulationStrategy? _failoverFallback;
                private int _failoverThresholdMs;
                private int _failoverTriggerCount;
                private int _failoverRecoveryCount;
                private int _consecutiveSlow;
                private int _consecutiveStable;
                private bool _usingFallback;
                private readonly object _failoverLock = new();

                // Cached settings for hot path (avoid Settings.Load() allocation every tick)
                private RightStickPolicy _cachedRightStickPolicy = RightStickPolicy.Hybrid;
                private long _settingsCacheTick = 0;
                private const long SETTINGS_CACHE_TTL_TICKS = 1250; // ~10s bei 125Hz

                /// <summary>Die aktuell aktive Emulations-Strategie (SendInput oder Kernel).</summary>
                public IMouseEmulationStrategy? ActiveStrategy => _usingFallback ? _failoverFallback : _failoverPrimary;

                /// <summary>ROB-002: Feuert bei jedem Failover-Switch (von, zu, Auslöser-Zähler). Für UI/Telemetrie.</summary>
                public event Action<string, string, int>? FailoverSwitched;

                /// <summary>
                /// ROB-002: Initialisiert den Input-Emulation-Failover.
                /// Primär = SendInput (Standard), Fallback = Kernel-Strategie (Interception).
                /// </summary>
                public void InitializeFailover(IMouseEmulationStrategy primary, IMouseEmulationStrategy? fallback, Models.Settings settings)
                {
                    lock (_failoverLock)
                    {
                        _failoverPrimary = primary;
                        _failoverFallback = fallback;
                        _failoverThresholdMs = Math.Max(1, settings.FailoverLatencyThresholdMs);
                        _failoverTriggerCount = Math.Max(1, settings.FailoverTriggerCount);
                        _failoverRecoveryCount = Math.Max(1, settings.FailoverRecoveryCount);
                    }
                }

        /// <summary>
        /// ROB-002: Failover-State-Machine. Wird von InputCommandQueue.SendInputLatencyRecorded
        /// auf dem Consumer-Thread aufgerufen (kein UI-Zugriff → thread-sicher per Design).
        /// N aufeinanderfolgende Flushes über der Latenz-Schwelle → Switch auf Kernel-Fallback.
        /// M stabile Flushes danach → Recovery zurück zu SendInput.
        /// </summary>
        public void RecordSendInputLatency(double latencyMs)
        {
            IMouseEmulationStrategy? primary;
            lock (_failoverLock)
            {
                primary = _failoverPrimary;
                if (primary == null || _failoverFallback == null) return;

                bool slow = latencyMs > _failoverThresholdMs;

                if (!_usingFallback)
                {
                    // ── Normal-Betrieb: SendInput-Latenz beobachten ─────────────
                    if (slow)
                    {
                        _consecutiveSlow++;
                        _consecutiveStable = 0;
                        if (_consecutiveSlow >= _failoverTriggerCount)
                        {
                            _usingFallback = true;
                            _consecutiveSlow = 0;
                            SwitchTo(primary.DisplayName, _failoverFallback.DisplayName, _failoverTriggerCount);
                        }
                    }
                    else
                    {
                        _consecutiveSlow = 0;
                    }
                }
                else
                {
                    // ── Fallback aktiv: auf stabile SendInput-Latenz warten ───────
                    if (!slow)
                    {
                        _consecutiveStable++;
                        _consecutiveSlow = 0;
                        if (_consecutiveStable >= _failoverRecoveryCount)
                        {
                            _usingFallback = false;
                            _consecutiveStable = 0;
                            SwitchTo(_failoverFallback.DisplayName, primary.DisplayName, _failoverRecoveryCount);
                        }
                    }
                    else
                    {
                        _consecutiveStable = 0;
                    }
                }
            }
        }

        private void SwitchTo(string from, string to, int triggerCount)
        {
            RagnaControllerETW.Log.InputFailoverSwitched(from, to, triggerCount);
            FailoverSwitched?.Invoke(from, to, triggerCount);
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
            CooldownManager cooldownManager)
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
                    // Update combat layers (L1, R1, L2, R2)
                    _combat.UpdateLayers(input.L1, input.R1, input.L2, input.R2);

                    // Update movement (left stick)
                    _movement.Update(input.LeftX, input.LeftY);

                    // OverlayRouter has priority (Start+DPad shortcuts, Mini-Button)
                    if (_overlayRouter.TryHandleInput(input)) return true;

                    // SmartCursor: D-Pad Grid-Hopping + Precision-Aiming in menus
                    if (_smartCursor.Tick(input)) return true; // Menu input consumed → skip combat

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

                    return false; // Input not consumed by router (processed by engines)
                }

        /// <summary>
        /// Handle combat action fired event - triggers feedback and cooldown tracking
        /// </summary>
        public void OnActionFired(ButtonAction action, bool rumbleEnabled)
        {
            if (rumbleEnabled) _feedback.TriggerSkillFired();
            _cooldownManager.RegisterAction(action);
        }

    }
}