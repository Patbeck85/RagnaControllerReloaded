using System;
using RagnaController.Models;

namespace RagnaController.Core
{
    public class CursorEngine : IInputHandler
    {
        public int   Priority     => 0;
        public float MaxSpeed     { get; set; } = 1200f;
        public float Deadzone     { get; set; } = 0.12f;
        public float Curve        { get; set; } = 1.6f;
        public float Sensitivity  { get; set; } = 1.0f;
        public float PrecisionFactor { get; set; } = 0.40f;
        public bool  PrecisionMode   { get; set; }

        private readonly WindowTracker _tracker;
        private readonly InputCommandQueue _queue;
        private float _remainderX, _remainderY;

        // FIX: Virtueller Cursor für asynchrones Threading (verhindert Rubber-Banding)
        private float _virtualX, _virtualY;
        private bool _wasInDeadzone = true;

        public CursorEngine(WindowTracker tracker, InputCommandQueue queue)
        {
            _tracker = tracker;
            _queue = queue;
        }

        public bool Handle(ParsedInput input, int deltaMs)
        {
            Update(input.RightX, input.RightY, deltaMs);
            return false;
        }

        public void Update(float rx, float ry, int ms)
        {
            float sqMag = rx * rx + ry * ry;

            // Wenn der Stick losgelassen wird (Deadzone)
            if (sqMag <= Deadzone * Deadzone)
            {
                _remainderX = _remainderY = 0;
                _wasInDeadzone = true; // Mark that we need to resync on next touch
                return;
            }

            // Vektor Mathematik (Mit Square-Gate Fix)
            float trueMag = MathF.Sqrt(sqMag);
            // FIX: Magnitude clamping vor curve calculation verhindert diagonal speed explosion
            float clampedMag = Math.Min(1.0f, trueMag);
            float normMag = (clampedMag - Deadzone) / (1f - Deadzone);

            float nx = rx / trueMag;
            float ny = -ry / trueMag;

            float speed = MathF.Pow(normMag, Curve)
                        * (PrecisionMode ? MaxSpeed * PrecisionFactor : MaxSpeed)
                        * Sensitivity
                        * (ms / 1000f);

            float vx = nx * speed + _remainderX;
            float vy = ny * speed + _remainderY;

            int mx = (int)vx;
            int my = (int)vy;
            _remainderX = vx - mx;
            _remainderY = vy - my;

            if (mx != 0 || my != 0)
            {
                // FIX: Hole die echte Windows-Mausposition NUR beim allerersten Bewegen aus der Deadzone heraus
                if (_wasInDeadzone)
                {
                    if (NativeMethods.GetCursorPos(out NativeMethods.POINT pt))
                    {
                        _virtualX = pt.X;
                        _virtualY = pt.Y;
                    }
                    _wasInDeadzone = false;
                }

                // Wende die Bewegung völlig flüssig auf unseren virtuellen Cursor an
                _virtualX += mx;
                _virtualY += my;

                int targetX = (int)_virtualX;
                int targetY = (int)_virtualY;

                // FIX: Sperre den Cursor zwingend im RO-Fenster ein (abzüglich 5px Rand)
                if (_tracker != null && _tracker.IsTracking)
                {
                    int limitX = (_tracker.ClientW / 2) - 5;
                    int limitY = (_tracker.ClientH / 2) - 5;

                    targetX = Math.Clamp(targetX, _tracker.CenterX - limitX, _tracker.CenterX + limitX);
                    targetY = Math.Clamp(targetY, _tracker.CenterY - limitY, _tracker.CenterY + limitY);

                    // Verhindert, dass der virtuelle Cursor den Bildschirmrand verlässt und sich aufstaut
                    _virtualX = targetX;
                    _virtualY = targetY;
                }

                _queue.MoveMouseAbsolute(targetX, targetY);
            }
        }
    }
}