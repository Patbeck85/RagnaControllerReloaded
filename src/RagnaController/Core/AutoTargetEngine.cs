using System;
using System.Threading;
using System.Threading.Tasks;
using RagnaController.Models;

namespace RagnaController.Core
{
    public class AutoTargetEngine : IInputHandler
    {
        public bool AutoAttackEnabled { get; set; } = true;
        public bool AutoRetargetEnabled { get; set; } = true;
        public bool SmartSkillEnabled { get; set; } = true;
        public int AttackKey_VK { get; set; } = 90;
        public int TabCycleMs { get; set; } = 80;
        public int AttackIntervalMs { get; set; } = 60;
        public float AimSensitivity { get; set; } = 22f;
        public float AimDeadzone { get; set; } = 0.20f;
        public int SkillInterruptMs { get; set; } = 750;

        // NEW: Smart Grid UI properties
        public string TargetName { get; set; } = "";
        public string TargetType { get; set; } = "";
        public float TargetDistance { get; set; }
        public bool IsAutoAttacking { get; private set; }

        public CombatState State { get; private set; } = CombatState.Idle;
        private readonly InputCommandQueue _queue;
        public bool IsTargetLocked { get; private set; }
        // FIX: Dynamischer Getter - SuppressMovementClicks ist true, sobald State != Idle!
        public bool SuppressMovementClicks => State != CombatState.Idle;

        public AutoTargetEngine(InputCommandQueue queue) { _queue = queue; }
        
        private volatile int _skillPause;
        private int _tc, _ac, _rc, _wac;
        private bool _prevR3;
        private NativeMethods.POINT _lockPos;
        private bool _lockPosValid = false;
        private readonly SemaphoreSlim _skillSem = new(1, 1);

        public SkillOrchestrator? SkillOrch { get; set; }

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
            
            NativeMethods.GetCursorPos(out NativeMethods.POINT saved);
            
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
                    if (_rc <= 0 && _tc <= 0 && AutoRetargetEnabled)
                    {
                        _queue.TapKey(VirtualKey.Tab);
                        _tc = TabCycleMs;
                    }
                    break;
                case CombatState.Engaged:
                    if (AutoAttackEnabled) SetState(CombatState.Attacking);
                    break;
                case CombatState.Attacking:
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
        public string StateLabel => State.ToString().ToUpper();

        // Reset: Alle Felder auf Initialwerte setzen (wichtig bei Profil-Reload!)
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
            State = CombatState.Idle;
            IsTargetLocked = false;
            _skillPause = 0;
            _ac = 0;
            _rc = 0;
            _tc = 0;
            _wac = 0;
            _prevR3 = false;
            _lockPosValid = false;
        }
    }
}