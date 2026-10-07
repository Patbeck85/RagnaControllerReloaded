using RagnaController.Core;
using Xunit;

namespace RagnaController.Tests
{
    // ────────────────────────────────────────────────────────────────────────
    // TECH-027: KiteRetreatingState — Overshoot-Schutz (S3)
    //
    // Der ursprüngliche KANBAN-Eintrag nannte "Division by Zero". Das war bereits
    // behoben (if (ctx.RetreatDurationMs <= 0) → Pivoting). Der ECHTE verbleibende
    // Edge-Case in derselben Formel: step = RetreatCursorDist / (RetreatDurationMs / deltaMs)
    // ist algebraisch = RetreatCursorDist * deltaMs / RetreatDurationMs. Bei einem Frame mit
    // deltaMs >= RetreatDurationMs wird step > RetreatCursorDist → ein EINZELNER Frame bewegt
    // mehr als die gesamte Rückzugsdistanz (Overshoot). Fix: _rem-Clamp summiert exakt auf
    // RetreatCursorDist. Diese Tests sichern genau das.
    // ────────────────────────────────────────────────────────────────────────

    public class KiteRetreatOvershootTests
    {
        // Factory: Context mit gerichtetem Rückzug entlang -X (AimValidated, LastAimX=1).
        // Verwendet ref-local für Capture, gibt totalX per out zurück.
        private static CombatContext NewCtx(int dist, int durationMs, out int totalX)
        {
            int acc = 0;
            var ctx = new CombatContext
            {
                RetreatDurationMs = durationMs,
                RetreatCursorDist = dist,
                AimValidated      = true,
                LastAimX          = 1f,
                LastAimY          = 0f,
                MouseMove         = (dx, dy) => System.Threading.Interlocked.Add(ref acc, dx),
            };
            totalX = acc;
            return ctx;
        }

        // Helfer: Context ausführen und totalX zurücklesen (acc wird per Closure gemutet).
        private static int RunCtx(CombatContext ctx, ICombatState state, int deltaMs)
        {
            int acc = 0;
            ctx.MouseMove = (dx, dy) => System.Threading.Interlocked.Add(ref acc, dx);
            state.Update(default, deltaMs, ctx);
            return acc;
        }

        [Fact]
        public void GrosserDeltaMs_KeinOvershoot()
        {
            // Ein einzelner Frame mit deltaMs >= RetreatDurationMs darf nicht mehr bewegen
            // als die gesamte Rückzugsdistanz. Ohne _rem-Clamp wäre step = 90*700/600 = 105 (>90).
            var ctx = NewCtx(dist: 90, durationMs: 600, out int _);
            var state = new KiteRetreatingState();
            state.Enter(ctx);

            int totalX = RunCtx(ctx, state, deltaMs: 700); // > RetreatDurationMs → Overshoot-Risiko

            Assert.True(totalX < 0, "Rückzug muss Bewegung erzeugen");
            Assert.True(totalX >= -90, $"Overshoot erkannt: {totalX} (max erlaubt -90)");
        }

        [Fact]
        public void NormalerBetrieb_GesamtdistanzUngeschaeumt()
        {
            // Normalbetrieb (kleine Frames): die Gesamtdistanz summiert sich weiterhin auf
            // RetreatCursorDist (±2 wegen Integer-Truncation der Subpixel-Reste).
            var ctx = NewCtx(dist: 90, durationMs: 600, out int _);
            ICombatState cur = new KiteRetreatingState();
            cur.Enter(ctx);

            int totalX = 0;
            for (int i = 0; i < 60 && cur is KiteRetreatingState; i++)
            {
                int acc = 0;
                ctx.MouseMove = (dx, dy) => System.Threading.Interlocked.Add(ref acc, dx);
                cur = cur.Update(default, deltaMs: 10, ctx);
                totalX += acc;
            }

            Assert.InRange(totalX, -92, -88);
        }

        [Fact]
        public void Null_Duration_SofortPivoting()
        {
            // Bereits bestehender Schutz: Duration <= 0 → sofort Pivoting (kein Retreat).
            var ctx = NewCtx(dist: 90, durationMs: 0, out int _);
            var state = new KiteRetreatingState();
            state.Enter(ctx);

            var next = state.Update(default, deltaMs: 16, ctx);

            Assert.NotSame(state, next);                 // hat den State verlassen
            Assert.Equal(0, RunCtx(ctx, state, deltaMs: 16)); // keine Retreat-Bewegung
        }
    }
}
