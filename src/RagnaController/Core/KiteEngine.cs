using System;
using RagnaController.Models;

namespace RagnaController.Core
{
    public class KiteEngine : IInputHandler
    {
        public int  Priority        => 30;
        public bool KiteEnabled     { get; set; }
        public int  AttackKeyVK     { get; set; } = 90;
        public int  AttackIntervalMs { get; set; } = 60;
        public int  AttacksPerCycle  { get; set; } = 3;
        public int  RetreatDurationMs{ get; set; } = 400;
        public int  RetreatCursorDist{ get; set; } = 110;
        public int  PivotDurationMs  { get; set; } = 300;
        public int  RelockDelayMs    { get; set; } = 100;
        public float AimDeadzone    { get; set; } = 0.25f;
        public float AimSensitivity { get; set; } = 14f;

        public bool IsActive => _state is not KiteIdleState;
        public bool IsRetreating => _state is KiteRetreatingState;
        public string PhaseLabel => _state?.Label ?? "IDLE";

        /// <summary>Fired on every state transition.</summary>
        public event Action<ICombatState>? PhaseChanged;

        private ICombatState _state = KiteStatePool.Idle;
        private readonly CombatContext _ctx;
        private readonly InputCommandQueue _queue;

        public KiteEngine(InputCommandQueue queue)
        {
            _queue = queue;
            _ctx = new CombatContext();
                        _ctx.StateChanged    = s => { _state = s; PhaseChanged?.Invoke(s); };
                        _ctx.MouseMove       = (dx, dy) => _queue.MoveMouseRelative(dx, dy);
                        _ctx.MouseClick      = (x, y)   => _queue.LeftClick();
                        _ctx.RightClick      = ()        => _queue.RightClick();
                        _ctx.TapKey          = k         => _queue.TapKey(k);
        }

        public void ToggleKiteMode()
        {
            if (IsActive)
            {
                _state.Exit(_ctx);
                _state = KiteStatePool.Idle;
                PhaseChanged?.Invoke(_state);
            }
            else
            {
                _state = KiteStatePool.Locking;
                _state.Enter(_ctx);
                PhaseChanged?.Invoke(_state);
            }
        }

        public void ForceRetreat()
        {
            if (!IsActive) return;
            _state.Exit(_ctx);
            _state = KiteStatePool.Retreating;
            _state.Enter(_ctx);
            PhaseChanged?.Invoke(_state);
        }

        public bool Handle(ParsedInput input, int deltaMs)
        {
            if (!KiteEnabled || !IsActive) return false;

            // Sync engine config → context each tick
            _ctx.AttackKeyVK       = AttackKeyVK;
            _ctx.AttackIntervalMs  = AttackIntervalMs;
            _ctx.AttacksPerCycle   = AttacksPerCycle;
            _ctx.RetreatDurationMs = RetreatDurationMs;
            _ctx.RetreatCursorDist = RetreatCursorDist;
            _ctx.PivotDurationMs   = PivotDurationMs;
            _ctx.RelockDelayMs     = RelockDelayMs;
            _ctx.AimDeadzone       = AimDeadzone;
            _ctx.AimSensitivity    = AimSensitivity;

            // Create new immutable struct directly - zero allocation
            var parsed = input with
            {
                IsConnected = input.IsConnected,
                RightX = input.RightX,
                RightY = input.RightY,
                L2 = input.L2,
                R3 = input.R3
            };

            var next = _state.Update(parsed, deltaMs, _ctx);

            if (!ReferenceEquals(next, _state))
            {
                _state = next;
                PhaseChanged?.Invoke(_state);
            }
            return true;
        }

        // Reset: Alle Felder auf Initialwerte setzen (wichtig bei Profil-Reload!)
        public void Reset()
        {
            KiteEnabled = false;
            AttackKeyVK = 90;
            AttackIntervalMs = 60;
            AttacksPerCycle = 3;
            RetreatDurationMs = 400;
            RetreatCursorDist = 110;
            PivotDurationMs = 300;
            RelockDelayMs = 100;
            AimDeadzone = 0.25f;
            AimSensitivity = 14f;
            _state = KiteStatePool.Idle;
        }
    }
}
