using System;

namespace RagnaController.Core
{
    public static class BezierMouseService
    {
        public static void MoveSmooth(int x0, int y0, int x1, int y1, int durationMs, InputCommandQueue queue)
        {
            if (x0 == x1 && y0 == y1) return;

            int steps = Math.Max(5, durationMs / 8);
            int stepDelay = durationMs / steps;

            // Kontrollpunkte für eine natürliche Kurve
            float ctrlX = x0 + (x1 - x0) * 0.5f + Random.Shared.Next(-20, 20);
            float ctrlY = y0 + (y1 - y0) * 0.5f + Random.Shared.Next(-20, 20);

            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                // Quadratische Bezier-Formel: (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
                float u = 1 - t;
                int bx = (int)(u * u * x0 + 2 * u * t * ctrlX + t * t * x1);
                int by = (int)(u * u * y0 + 2 * u * t * ctrlY + t * t * y1);

                queue.MouseMoveAbsolute(bx, by);
                if (i < steps) queue.Wait(stepDelay);
            }
        }
    }
}