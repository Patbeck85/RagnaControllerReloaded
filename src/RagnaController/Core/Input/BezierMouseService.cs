using System;

namespace RagnaController.Core
{
    public static class BezierMouseService
    {
        /// <summary>
        /// Moves the mouse smoothly along a quadratic Bezier curve.
        /// Enqueues intermediate positions instead of blocking the consumer thread.
        /// </summary>
        /// <param name="y0">Start Y</param>
        /// <param name="x1">End X</param>
        /// <param name="y1">End Y</param>
        /// <param name="durationMs">Total duration in milliseconds</param>
        /// <param name="queue">Input command queue</param>
        /// <param name="rng">Optional seeded RNG for deterministic paths (useful for replay/testing)</param>
        public static void MoveSmooth(int x0, int y0, int x1, int y1, int durationMs, InputCommandQueue queue, Random? rng = null)
        {
            if (x0 == x1 && y0 == y1) return;

            // Guard: durationMs must be positive to avoid divide-by-zero and negative stepDelay
            if (durationMs <= 0) durationMs = 1;

            int steps = Math.Max(5, durationMs / 8);
            int stepDelay = Math.Max(1, durationMs / steps); // Clamp to min 1ms to avoid busy-wait

            // Use injected RNG for deterministic paths (replay/testing), fallback to Random.Shared
            var random = rng ?? Random.Shared;

            // Kontrollpunkte for eine natürliche Kurve
            // TECH-037: Use injected RNG for determinism
            float ctrlX = x0 + (x1 - x0) * 0.5f + random.Next(-20, 20);
            float ctrlY = y0 + (y1 - y0) * 0.5f + random.Next(-20, 20);

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

        /// <summary>
        /// Deterministic overload for replay/testing - uses hash of coordinates as seed.
        /// </summary>
        public static void MoveSmoothDeterministic(int x0, int y0, int x1, int y1, int durationMs, InputCommandQueue queue)
        {
            // Create deterministic seed from coordinates (unchecked to allow overflow)
            unchecked
            {
                int seed = (int)((x0 * 73856093) ^ (y0 * 19349663) ^ (x1 * 83492791) ^ (y1 * 2654435769) ^ durationMs);
                var rng = new Random(seed);
                MoveSmooth(x0, y0, x1, y1, durationMs, queue, rng);
            }
        }
    }
}