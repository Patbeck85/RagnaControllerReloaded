using System;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// State Design Pattern for combat engines.
    /// Each phase is its own class — no more switch(Phase) blocks.
    /// Engine loop: <c>_state = _state.Update(input, deltaMs, ctx);</c>
    /// </summary>
    public interface ICombatState
    {
        string Label { get; }
        void Enter(CombatContext ctx) { }
        void Exit(CombatContext ctx)  { }
        ICombatState Update(ParsedInput input, int deltaMs, CombatContext ctx);
    }

    /// <summary>
    /// Shared mutable context injected into every state.
    /// States read config and write output via callbacks — zero direct dependencies.
    /// </summary>
    public sealed class CombatContext
    {
        // ── Config (written once by engine on profile load) ────────────
        public float AimSensitivity    { get; set; } = 20f;
        public float AimDeadzone       { get; set; } = 0.18f;
        public int   AttackKeyVK       { get; set; } = 0x5A;
        public int   AttackIntervalMs  { get; set; } = 55;
        public int   AttacksPerCycle   { get; set; } = 3;
        public int   RetreatDurationMs { get; set; } = 600;
        public int   PivotDurationMs   { get; set; } = 180;
        public int   RelockDelayMs     { get; set; } = 120;
        public float RetreatCursorDist { get; set; } = 90f;

        // ── Shared runtime state ───────────────────────────────────────
        /// <summary>Last recorded aim direction from the right stick.</summary>
        public float LastAimX       { get; set; } = 1f;
        public float LastAimY       { get; set; } = 0f;
        /// <summary>
        /// True once the right stick has been moved deliberately.
        /// Until validated, retreat direction falls back to facing-away from map center
        /// instead of -LastAimX/-LastAimY (which defaults to right, causing wrong retreat).
        /// </summary>
        public bool  AimValidated    { get; set; } = false;
        /// <summary>
        /// FIX: true once the player has moved the right stick at least once.
        /// Before validation, retreating defaults to the character's facing direction
        /// rather than a hardcoded "right" fallback.
        /// </summary>
        public bool  AimValid        { get; set; } = false;
        public int   AttacksFired   { get; set; }
        public int   AttacksThisCycle { get; set; }

        // ── Output callbacks (set by engine, invoked by states) ────────
        public Action?               RightClick   { get; set; }
        public Action<int, int>?     MouseMove    { get; set; }
        public Action<int, int>?     MouseClick   { get; set; }
        public Action<VirtualKey>?   TapKey       { get; set; }
        public Action<ICombatState>? StateChanged { get; set; }
    }
}
