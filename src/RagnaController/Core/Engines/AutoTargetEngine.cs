using System;
using System.Threading;
using System.Threading.Tasks;
using RagnaController.Models;
using static RagnaController.Core.NativeMethods;

namespace RagnaController.Core
{
    /// <summary>
    /// Targeting strategy enum: Sticky holds current target until lost; Nearest cycles through all targets.
    /// </summary>
    public enum TargetingMode
    {
        /// <summary>Ziel bleibt fixiert, bis Tod/Reichweitenverlust erkannt is.</summary>
        Sticky = 0,
        /// <summary>Cycles through all targets (Tab-cycling) independent of lock status.</summary>
        Nearest = 1
    }

    /// <summary>
    /// AutoTargetEngine — Sticky and nearest targeting strategy for auto-attack.
    /// Holds target lock until lost (dead/out of range) or cycles through targets via Tab.
    /// Supports party targeting exclusion to prevent accidental party member selection.
    /// </summary>
    public class AutoTargetEngine : IInputHandler, IDisposable
        {
            public bool AutoAttackEnabled { get; set; } = true;
            public bool AutoRetargetEnabled { get; set; } = true;
            public bool AutoRetaliateEnabled { get; set; } = false;

            /// <summary>
            /// FEAT-012: If true, disabled der Engine den Tab-Cycling-Loop, um
            /// versehentliches Targeting from Party-Mitgliedern to verhindern (RO-Konvention:
            /// plain Tab cycles through ALL targets including party). The bot then relies
            /// exclusively on the explicit target lock (R3/right click).
            /// </summary>
            public bool PartyTargetingEnabled { get; set; } = false;
            public bool SmartSkillEnabled { get; set; } = true;
        public int AttackKey_VK { get; set; } = 90;
        public int TabCycleMs { get; set; } = 80;
        public int AttackIntervalMs { get; set; } = 60;
        public float AimSensitivity { get; set; } = 22f;
        public float AimDeadzone { get; set; } = 0.20f;
        public int SkillInterruptMs { get; set; } = 750;

        // FEAT-013: Targeting-Strategie & Reichweiten-Schwelle (Meter).
        public TargetingMode TargetingMode { get; set; } = TargetingMode.Sticky;
        public float MaxTargetDistance { get; set; } = 15f;

        // NEW: Smart Grid UI properties
        public string TargetName { get; set; } = "";
        public string TargetType { get; set; } = "";
        public float TargetDistance { get; set; }
        public bool IsAutoAttacking { get; private set; }

        // FEAT-013: Ziel-Daten only then for Tod-/Reichweiten-Erkennung verwenden, if eine
        // Datenquelle (OCR/Reader) active liefert. Default false → keine falschen Positives,
        // solange no Feed existiert (KISS: Feature is testbar, is beim ersten Feed active).
        public bool TargetDataValid { get; set; }

        public CombatState State { get; private set; } = CombatState.Idle;
                private readonly InputCommandQueue _queue;
                public bool IsTargetLocked { get; private set; }
       
                // FEAT-013: Zielverlust-Ereignis (Tod / Reichweitenverlust). Reason: "dead" | "out_of_range".
                public event Action<string>? LostTarget;

                // FEAT-007: Properties for SkillOrchestrator condition evaluation
                public object? CurrentTarget { get; private set; }
                public float CurrentTargetDistance { get; private set; }
                public bool IsFacingTarget { get; private set; }
                public int NearbyEnemyCount { get; private set; }
                // FIX: Dynamischer Getter - SuppressMovementClicks is true, sobald State != Idle!
                public bool SuppressMovementClicks => State != CombatState.Idle;

        public AutoTargetEngine(InputCommandQueue queue) { _queue = queue; }

        private volatile int _skillPause;
        private int _tc, _ac, _rc, _wac;
        private bool _prevR3;
        private POINT _lockPos;
        private bool _lockPosValid = false;
        private readonly SemaphoreSlim _skillSem = new(1, 1);

        public SkillOrchestrator? SkillOrch { get; set; }

        /// <summary>
        /// ToggleCombatMode method.
        /// </summary>
        public void ToggleCombatMode() { if (State == CombatState.Idle) EnterSeek(); else EnterIdle(); }

        public int Priority => 10;

        public bool Handle(ParsedInput input, int deltaMs)
        {
            if (State == CombatState.Idle) return false;
            Update(input.RightX, input.RightY, input.R3, _prevR3, input.R2, deltaMs, input.LeftX, input.LeftY);
            _prevR3 = input.R3;
            return true;
        }

        public void ResumeAutoAttack()
        {
            _skillPause = 0;
            _ac = 0;
            if (IsTargetLocked) _queue.RightClick();
        }

        public void NotifySkillFired()
        {
            _skillPause = SkillInterruptMs;
            _ac = SkillInterruptMs;
        }

        public void OnTargetLocked()
        {
            IsTargetLocked = true;
            if (NativeMethods.GetCursorPos(out _lockPos)) _lockPosValid = true;
            SetState(CombatState.Engaged);
        }

        public void FireSmartSkill(VirtualKey skillKey)
        {
            if (!SmartSkillEnabled || !IsTargetLocked || !_lockPosValid)
            {
                _queue.TapKey(skillKey);
                _queue.LeftClick();
                return;
            }

            if (!_skillSem.Wait(TimeSpan.FromMilliseconds(10))) return; 
            
                        GetCursorPos(out POINT saved);
            
                        _queue.MouseMoveAbsolute(_lockPos.X, _lockPos.Y);
            _queue.Wait(10);
            
            _queue.MouseMoveAbsolute(saved.X, saved.Y);
            _queue.KeyDown(skillKey);
            _queue.Wait(15);
            _queue.KeyUp(skillKey);
            _queue.Wait(15);
            _queue.LeftClick();
            _queue.Wait(10);
            NotifySkillFired();
            _skillSem.Release();
        }

        public void Update(float rx, float ry, bool r3, bool r3prev, bool rb, int ms, float lx, float ly)
        {
            _tc = Math.Max(0, _tc - ms); _ac = Math.Max(0, _ac - ms); _rc = Math.Max(0, _rc - ms);
            _wac = Math.Max(0, _wac - ms); _skillPause = Math.Max(0, _skillPause - ms);

            // FEAT-013: check target validity (death / range loss) → auto-retarget.
            CheckTargetValidity();

            // Stick-Zielen aktualisiert Lock-Position
            float rsq = rx * rx + ry * ry;
            if (rsq > AimDeadzone * AimDeadzone)
            {
                if (NativeMethods.GetCursorPos(out _lockPos)) _lockPosValid = true;
            }

            if (r3 && !r3prev) 
            { 
                _queue.LeftUp();    // FIX: Lauf-Befehl unterbrechen, bevor RightClick!
                _queue.RightClick();
                OnTargetLocked(); 
            }

            switch (State)
            {
                case CombatState.Seeking:
                    // FEAT-012: party targeting → no tab-cycling (would go through ALL targets
                    // inkl. Party-Mitglieder zyklieren). Bot wartet on expliziten Lock (R3).
                    if (!PartyTargetingEnabled && _rc <= 0 && _tc <= 0 && AutoRetargetEnabled)
                    {
                        _queue.TapKey(VirtualKey.Tab);
                        _tc = TabCycleMs;
                    }
                    break;
                case CombatState.Engaged:
                    if (AutoAttackEnabled) SetState(CombatState.Attacking);
                    break;
                case CombatState.Attacking:
                    // FEAT-013: Nearest-Modus zykliert auch im Angriff durch Ziele (Tab),
                    // to always hit nearest target. Sticky stays fixed.
                    // FEAT-012: PartyTargeting → Cycling disabled (Party-Schutz).
                    if (!PartyTargetingEnabled && TargetingMode == TargetingMode.Nearest && _tc <= 0 && AutoRetargetEnabled)
                    {
                        _queue.TapKey(VirtualKey.Tab);
                        _tc = TabCycleMs;
                    }

                    if (_skillPause <= 0 && _ac <= 0)
                    {
                        _queue.TapKey((VirtualKey)AttackKey_VK);
                        _ac = AttackIntervalMs;
                    }
                    break;
            }
        }

        private void EnterSeek() { IsTargetLocked = false; _lockPosValid = false; SetState(CombatState.Seeking); }
        private void EnterIdle() { IsTargetLocked = false; SetState(CombatState.Idle); }
        private void SetState(CombatState s) { if (State == s) return; State = s; }

        // FEAT-013: check target validity. A locked target is considered lost if it is dead
        // (empty target name) or outside range. Only active if a data source
        // delivers (TargetDataValid) — otherwise no false positives. Sticky otherwise holds the
        // target; Nearest additionally triggers cyclic tab-cycling to jump to next target.
        private void CheckTargetValidity()
        {
            if (!IsTargetLocked || State == CombatState.Idle) return;
            if (!TargetDataValid) return;

            bool isDead = string.IsNullOrEmpty(TargetName);
            bool outOfRange = TargetDistance > MaxTargetDistance;

            if (isDead || outOfRange)
            {
                string reason = isDead ? "dead" : "out_of_range";
                LostTarget?.Invoke(reason);
                IsTargetLocked = false;
                _lockPosValid = false;
                SetState(CombatState.Seeking);
            }
        }

        /// <summary>
        /// FEAT-013: Ziel-Daten aus einer Datenquelle (OCR/Reader) liefern.
        /// Sets TargetDataValid=true and triggers target validity check.
        /// </summary>
        public void SetTarget(string name, float distance)
        {
            TargetName = name ?? "";
            TargetDistance = distance;
            TargetDataValid = true;
        }

        /// <summary>FEAT-013: Datenquelle liefert gerade keine Ziel-Daten (z. B. OCR ohne Treffer).</summary>
        public void ClearTarget()
        {
            TargetName = "";
            TargetType = "";
            TargetDistance = 0f;
            TargetDataValid = false;
        }

        // ── Multi-Mob Targeting (GEX-006a) ──────────────────────────────
        /// <summary>All known targets in radius (name, distance, LoS, aggressiveness).</summary>
        private readonly List<(string Name, float Distance, bool InLineOfSight, bool IsAggressive, int HpPercent)> _knownTargets = new();

        /// <summary>
        /// Aktualisiert die bekannte Ziel-Liste (is vom OCR/Reader/Feeder aufgerufen).
        /// Ziel with leerem Name entfernt das Ziel aus der Liste.
        /// </summary>
        public void UpdateTargets(List<(string Name, float Distance, bool InLineOfSight, bool IsAggressive, int HpPercent)> targets)
        {
            _knownTargets.Clear();
            foreach (var t in targets)
            {
                if (string.IsNullOrWhiteSpace(t.Name)) continue;
                _knownTargets.Add(t);
            }
        }

        /// <summary>
        /// Returns the best target by priority:
        /// 1. In Line of Sight
        /// 2. Niedrigste Distanz
        /// 3. Niedrigster HP-Prozentsatz
        /// 4. Aggressiv (IsAggressive)
        /// </summary>
        public string? GetBestTarget()
        {
            if (_knownTargets.Count == 0) return null;

            var best = _knownTargets
                .Where(t => t.InLineOfSight)
                .OrderBy(t => t.Distance)
                .ThenBy(t => t.HpPercent)
                .ThenByDescending(t => t.IsAggressive)
                .FirstOrDefault();

            return best.Name;
        }

        /// <summary>
        /// Checks if a known target is in line of sight.
        /// </summary>
        public bool IsInLineOfSight(string targetName)
        {
            return _knownTargets.Any(t => t.Name == targetName && t.InLineOfSight);
        }

        /// <summary>
        /// Returns the number of known targets.
        /// </summary>
        public int KnownTargetCount => _knownTargets.Count;

        public string StateLabel => State.ToString().ToUpper();

        // Reset: All Felder on Initialwerte setzen (important bei Profil-Reload!)
        public void Reset()
        {
            AutoAttackEnabled = true;
            AutoRetargetEnabled = true;
            SmartSkillEnabled = true;
            AttackKey_VK = 90;
            TabCycleMs = 80;
            AttackIntervalMs = 60;
            AimSensitivity = 22f;
            AimDeadzone = 0.20f;
            SkillInterruptMs = 750;
            // FEAT-013: targeting strategy & range hard reset.
            TargetingMode = TargetingMode.Sticky;
            MaxTargetDistance = 15f;
            ClearTarget();
            State = CombatState.Idle;
            IsTargetLocked = false;
            IsAutoAttacking = false;
            CurrentTarget = null;
            CurrentTargetDistance = 0f;
            TargetName = "";
            TargetType = "";
            TargetDistance = 0f;
            TargetDataValid = false;
            _skillPause = 0;
            _tc = 0;
            _ac = 0;
            _rc = 0;
            _wac = 0;
            _prevR3 = false;
            _lockPosValid = false;
        }

        public void Dispose()
        {
            LostTarget = null;
            _skillSem?.Dispose();
        }
    }
}