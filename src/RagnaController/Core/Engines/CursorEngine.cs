using System;
using RagnaController.Models;
using static RagnaController.Core.NativeMethods;

namespace RagnaController.Core
{
    /// <summary>
    /// CursorEngine — Free mouse movement using right stick input.
    /// Applies deadzone, curve sensitivity, and precision factor for smooth cursor control.
    /// Maintains virtual cursor position to prevent rubber-banding on async threads.
    /// </summary>
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

                // FIX: Virtueller Cursor for asynchrones Threading (verhindert Rubber-Banding)
                private float _virtualX, _virtualY;
                private bool _wasInDeadzone = true;

                // TECH-036: Expose virtual cursor position for UI/Telemetry
                public (int X, int Y) VirtualCursorPosition => ((int)_virtualX, (int)_virtualY);

                // TECH-034: Reset virtual cursor state on profile/game mode switch
                public void Reset()
                {
                    _wasInDeadzone = true;
                    _virtualX = 0f;
                    _virtualY = 0f;
                    _remainderX = 0f;
                    _remainderY = 0f;
                }

                // TECH-020 Fix: Periodischer Resync des virtuellen Cursors with echter Windows-Position
        // Verhindert Desync bei physischer Maus-Bewegung or Fensterwechsel
        private int _resyncCounter = 0;
        private const int RESYNC_INTERVAL_TICKS = 62; // ~500ms bei 125Hz (62 * 8ms ≈ 500ms)
        private const int MAX_DESYNC_PIXELS = 50; // Max erlaubte Abweichung vor Forced Resync

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

            // If der Stick losgelassen is (Deadzone)
            if (sqMag <= Deadzone * Deadzone)
            {
                _remainderX = _remainderY = 0;
                _wasInDeadzone = true; // Mark that we need to resync on next touch
                return;
            }

            // Vektor Mathematik (With Square-Gate Fix)
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
                                    if (GetCursorPos(out POINT pt))
                                    {
                                        _virtualX = pt.X;
                                        _virtualY = pt.Y;
                                    }
                                    _wasInDeadzone = false;
                                }

                // Wende die Bewegung völlig flüssig on unseren virtuellen Cursor an
                _virtualX += mx;
                _virtualY += my;

                int targetX = (int)_virtualX;
                                int targetY = (int)_virtualY;

                                // FIX: Sperre den Cursor zwingend im RO-Fenster ein (abzüglich 5px Rand)
                                // TECH-035: Apply DpiScale for per-monitor DPI awareness
                                if (_tracker != null && _tracker.IsTracking)
                                {
                                    float scale = _tracker.DpiScale;
                                    int limitX = (int)((_tracker.ClientW / 2f - 5f) * scale);
                                    int limitY = (int)((_tracker.ClientH / 2f - 5f) * scale);

                                    targetX = Math.Clamp(targetX, _tracker.CenterX - limitX, _tracker.CenterX + limitX);
                                    targetY = Math.Clamp(targetY, _tracker.CenterY - limitY, _tracker.CenterY + limitY);

                                    // Verhindert, dass der virtuelle Cursor den Bildschirmrand verlässt and sich aufstaut
                                    _virtualX = targetX;
                                    _virtualY = targetY;
                                }

                _queue.MoveMouseAbsolute(targetX, targetY);
            }

            // TECH-020: Periodischer Resync des virtuellen Cursors
                        // Verhindert Desync bei physischer Maus-Bewegung, Fensterwechsel or DPI-Änderungen
                        _resyncCounter++;
                        if (_resyncCounter >= RESYNC_INTERVAL_TICKS)
                        {
                            _resyncCounter = 0;
                            if (GetCursorPos(out POINT pt))
                            {
                                // TECH-035: Account for DpiScale in desync detection
                                float scale = _tracker?.DpiScale ?? 1.0f;
                                int dx = Math.Abs(pt.X - (int)_virtualX);
                                int dy = Math.Abs(pt.Y - (int)_virtualY);
                                if (dx > (int)(MAX_DESYNC_PIXELS * scale) || dy > (int)(MAX_DESYNC_PIXELS * scale))
                                {
                                    // Forced Resync bei to großer Abweichung
                                    _virtualX = pt.X;
                                    _virtualY = pt.Y;
                                }
                            }
                        }
        }
    }
}