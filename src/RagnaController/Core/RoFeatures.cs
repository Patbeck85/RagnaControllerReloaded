using System;
using RagnaController.Models;

namespace RagnaController.Core
{
    public sealed class RoFeatures
    {
        private readonly InputCommandQueue _queue;
        private int _cx, _cy;

        public RoFeatures(InputCommandQueue queue) => _queue = queue;

        public void SetCenter(int x, int y) { _cx = x; _cy = y; }

        /// <summary>
        /// Ermöglicht Ausweichen (Backslide) in Stick-Richtung.
        /// </summary>
        public void DodgeRoll(float sx, float sy, VirtualKey backSlideKey)
        {
            if (sx == 0 && sy == 0) return;

            // 1. In Gegenrichtung schauen (für Backslide nötig)
            int lookX = _cx - (int)(sx * 100);
            int lookY = _cy + (int)(sy * 100);

            _queue.MouseMoveAbsolute(lookX, lookY);
            _queue.Action(() => _queue.RightClick()); // Nur drehen, nicht laufen
            _queue.Wait(15);

            // 2. Skill zünden
            _queue.Action(() => _queue.TapKey(backSlideKey));

            // 3. Look back to center
            _queue.Wait(10);
            _queue.MouseMoveAbsolute(_cx, _cy);
        }

        /// <summary>
        /// Schnell-Loot-Vakuum (LB+RB)
        /// </summary>
        public void PerformLootVacuum(int ms)
        {
            double angle = (DateTime.Now.Ticks / 10000.0) * 0.05;
            float radius = 60;

            int lx = _cx + (int)(Math.Cos(angle) * radius);
            int ly = _cy + (int)(Math.Sin(angle) * radius);

            _queue.MouseMoveAbsolute(lx, ly);
            if (JitterService.Chance(0.2)) _queue.LeftClick();
        }
    }
}
