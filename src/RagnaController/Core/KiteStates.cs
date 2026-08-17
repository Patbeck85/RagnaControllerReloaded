using System;
using RagnaController.Models;
using RagnaController.Core;

namespace RagnaController.Core
{
    // ── Shared base ───────────────────────────────────────────────────────
    public abstract class KiteStateBase : ICombatState
    {
        public abstract string Label { get; }
        public virtual void Enter(CombatContext ctx) { }
        public virtual void Exit(CombatContext ctx)  { }
        public abstract ICombatState Update(ParsedInput input, int deltaMs, CombatContext ctx);

        protected static void UpdateAim(ParsedInput input, CombatContext ctx)
        {
            float sq = input.RightX * input.RightX + input.RightY * input.RightY;
            if (sq <= ctx.AimDeadzone * ctx.AimDeadzone) return;
            float mag  = MathF.Sqrt(sq);
            float norm = (mag - ctx.AimDeadzone) / (1f - ctx.AimDeadzone);
            ctx.LastAimX     = input.RightX / mag * norm;
            ctx.LastAimY     = input.RightY / mag * norm;
            ctx.AimValidated = true;
        }

        protected static void MoveCursor(CombatContext ctx, float speed)
        {
            int mx = (int)(ctx.LastAimX *  speed);
            int my = (int)(ctx.LastAimY * -speed);
            if (mx != 0 || my != 0) ctx.MouseMove?.Invoke(mx, my);
        }

        protected ICombatState Transition(ICombatState next, CombatContext ctx)
        {
            Exit(ctx); next.Enter(ctx); ctx.StateChanged?.Invoke(next); return next;
        }
    }

    // ── Pre-allocated State-Pool (verhindert heap alloc bei Transitionen) ─
    internal static class KiteStatePool
    {
        public static readonly KiteIdleState       Idle       = new();
        public static readonly KiteLockingState    Locking    = new();
        public static readonly KiteAttackingState  Attacking  = new();
        public static readonly KiteRetreatingState Retreating = new();
        public static readonly KitePivotingState   Pivoting   = new();
        public static readonly KiteRelockingState  Relocking  = new();
    }

    // ── Idle ──────────────────────────────────────────────────────────────
    public sealed class KiteIdleState : KiteStateBase
    {
        public static readonly KiteIdleState Instance = new();
        public override string Label => "IDLE";
        public override ICombatState Update(ParsedInput input, int deltaMs, CombatContext ctx) => this;
    }

    // ── Locking ───────────────────────────────────────────────────────────
    public sealed class KiteLockingState : KiteStateBase
    {
        private int _t = 200;
        public override string Label => "LOCKING TARGET";
        public override void Enter(CombatContext ctx) { _t = 200; ctx.AttacksThisCycle = 0; }
        public override ICombatState Update(ParsedInput input, int deltaMs, CombatContext ctx)
        {
            UpdateAim(input, ctx);
            MoveCursor(ctx, ctx.AimSensitivity * 1.5f);
            if ((_t -= deltaMs) > 0) return this;
            ctx.RightClick?.Invoke();
            return Transition(KiteStatePool.Attacking, ctx);
        }
    }

    // ── Attacking ─────────────────────────────────────────────────────────
    public sealed class KiteAttackingState : KiteStateBase
    {
        private int _cd;
        public override string Label => "ATTACKING";
        public override void Enter(CombatContext ctx) { ctx.AttacksThisCycle = 0; _cd = 0; }
        public override ICombatState Update(ParsedInput input, int deltaMs, CombatContext ctx)
        {
            UpdateAim(input, ctx);
            MoveCursor(ctx, ctx.AimSensitivity * 0.6f);
            if (input.R3) ctx.RightClick?.Invoke();
            if ((_cd = Math.Max(0, _cd - deltaMs)) == 0)
            {
                ctx.TapKey?.Invoke((VirtualKey)ctx.AttackKeyVK);
                _cd = ctx.AttackIntervalMs;
                ctx.AttacksThisCycle++;
                ctx.AttacksFired++;
            }
            if (ctx.AttacksThisCycle >= ctx.AttacksPerCycle && !input.L2)
                return Transition(KiteStatePool.Retreating, ctx);
            return this;
        }
    }

    // ── Retreating ────────────────────────────────────────────────────────
    public sealed class KiteRetreatingState : KiteStateBase
    {
        private int   _t, _clickT;
        private float _ax, _ay, _dx, _dy;
        public override string Label => "RETREATING";
        public override void Enter(CombatContext ctx)
        {
            _t = ctx.RetreatDurationMs; _clickT = 0;
            _dx = ctx.AimValidated ? -ctx.LastAimX : -0.707f;
            _dy = ctx.AimValidated ? -ctx.LastAimY : -0.707f;
            _ax = _ay = 0f;
        }
        public override ICombatState Update(ParsedInput input, int deltaMs, CombatContext ctx)
        {
            if (input.L2) return Transition(KiteStatePool.Pivoting, ctx);
            
            // FIX: Division by Zero Schutz — RetreatDurationMs kann im Profil auf 0 gesetzt werden
            // um den Rückzug zu deaktivieren. Wenn Duration <= 0, springen wir direkt zum Pivoting-Status.
            if (ctx.RetreatDurationMs <= 0) return Transition(KiteStatePool.Pivoting, ctx);
            
            float step = ctx.RetreatCursorDist / (ctx.RetreatDurationMs / (float)deltaMs);
            _ax += _dx * step; _ay += _dy * step;
            int mx = (int)_ax; int my = (int)_ay; _ax -= mx; _ay -= my;
            if ((_clickT -= deltaMs) <= 0) { ctx.MouseClick?.Invoke(0, 0); _clickT = 180; }
            if (mx != 0 || my != 0) ctx.MouseMove?.Invoke(mx, my);
            if ((_t -= deltaMs) <= 0) return Transition(KiteStatePool.Pivoting, ctx);
            return this;
        }
    }

    // ── Pivoting ──────────────────────────────────────────────────────────
    public sealed class KitePivotingState : KiteStateBase
    {
        private int _t;
        public override string Label => "PIVOTING";
        public override void Enter(CombatContext ctx) => _t = ctx.PivotDurationMs;
        public override ICombatState Update(ParsedInput input, int deltaMs, CombatContext ctx)
        {
            UpdateAim(input, ctx); MoveCursor(ctx, ctx.AimSensitivity * 2.5f);
            return (_t -= deltaMs) <= 0 ? Transition(KiteStatePool.Relocking, ctx) : this;
        }
    }

    // ── Relocking ─────────────────────────────────────────────────────────
    public sealed class KiteRelockingState : KiteStateBase
    {
        private int _t;
        public override string Label => "RELOCKING";
        public override void Enter(CombatContext ctx) => _t = ctx.RelockDelayMs;
        public override ICombatState Update(ParsedInput input, int deltaMs, CombatContext ctx)
        {
            if ((_t -= deltaMs) > 0) return this;
            ctx.RightClick?.Invoke();
            return Transition(KiteStatePool.Attacking, ctx);
        }
    }
}
