using System;

namespace RagnaController.Core
{
    /// <summary>
    /// MovementEngine — Click-to-move character movement using left stick.
    /// Right stick moves mouse cursor, left stick sets click direction.
    /// Supports DPI-aware scaling, deadzone, curve sensitivity, and action RPG mode.
    /// </summary>
    /// <remarks>
    /// Thread safety: Not thread-safe. Must be called from the engine tick thread only.
    /// </remarks>
    public class MovementEngine
    {
        private readonly InputCommandQueue _queue;
        private readonly WindowTracker _tracker;

        // Fenster-Mittelpunkt (vom WindowTracker gesetzt)
        private int   _cx, _cy;
        private float _radiusScale = 1.0f;   // DPI × resolution scaling combined

        private const float BASE_RES_H = 1080f;  // reference resolution (1080p)

        // Zustand
                private bool  _isWalking;
                public  bool  IsWalking => _isWalking;
        
                // FEAT-007: Property for SkillOrchestrator condition evaluation
                public bool IsMoving => _isWalking;
        private float _lastX, _lastY;
        private int   _lastTx = -1; // last sent X coordinate (for deduplication)
        private int   _lastTy = -1; // last sent Y coordinate (for deduplication)

        // Konfiguration
        public float Deadzone      { get; set; } = 0.20f;
        public float Curve         { get; set; } = 1.5f;
        public int   LeashRadius   { get; set; } = 120;   // px vom Zentrum (logisch)
        public float Sensitivity   { get; set; } = 1.0f;
        public bool  ActionRpgMode { get; set; } = true;
        public int   CoastFrames   { get; set; } = 3;
        public int   CurveMode     { get; set; } = 1;

        private int  _coastCounter;

        public MovementEngine(InputCommandQueue queue, WindowTracker tracker)
        {
            _queue = queue;
            _tracker = tracker;
        }

        public void SetCenter(int x, int y, int clientH, float dpiScale = 1.0f)
        {
            _cx = x;
            _cy = y;
            // resolution relative to 1080p × DPI → a single multiplier
            float resScale = clientH > 0 ? clientH / BASE_RES_H : 1.0f;
            _radiusScale   = resScale * (dpiScale > 0f ? dpiScale : 1.0f);
        }

        // ── Haupt-Update (every Tick) ─────────────────────────────────────
        public void Update(float x, float y)
        {
            if (!ActionRpgMode) { ForceStop(); return; }
            float sqMag = x * x + y * y;

            if (sqMag <= Deadzone * Deadzone)
            {
                if (_isWalking)
                {
                    // CoastFrames = Anzahl der Deadzone-Frame, nach denen gestoppt is.
                    // FIX off-by-one: previously stopped at frame N+1 (counter only increased to N).
                    _coastCounter++;
                    if (_coastCounter >= CoastFrames)
                    {
                        _queue.LeftUp();
                        _isWalking = false;
                        _coastCounter = 0;
                        // cursor back to window center — prevents misclicks
                        // on next stick impulse
                        _queue.MouseMoveAbsolute(_cx, _cy);
                    }
                }
                return;
            }
            _coastCounter = 0;

            float mag    = MathF.Sqrt(sqMag);
            float norm   = (mag - Deadzone) / (1.0f - Deadzone);
            float curved = MathF.Pow(norm, Curve);

            float nx = x / mag;
            float ny = -y / mag;   // XInput Y+ = oben, Screen Y+ = unten

            // Radius: LeashRadius is in 1080p-Pixeln definiert → skalieren
            int radius = (int)(LeashRadius * _radiusScale);

            // FIX: limit click to actual inner area of window
            int limitX = _tracker.ClientW / 2;
            int limitY = _tracker.ClientH / 2;
            
            // prevent clicks outside game client
            int tx = _cx + (int)(nx * radius * curved);
            int ty = _cy + (int)(ny * radius * curved);
            tx = Math.Clamp(tx, _cx - Math.Min(radius, limitX), _cx + Math.Min(radius, limitX));
            ty = Math.Clamp(ty, _cy - Math.Min(radius, limitY), _cy + Math.Min(radius, limitY));

            if (!_isWalking)
            {
                _queue.MouseMoveAbsolute(tx, ty);
                _queue.LeftDown();
                _isWalking = true;
                _lastTx = tx;
                _lastTy = ty;
            }
            else
            {
                // FIX: only send if coordinates actually changed
                // Vermeidet 125x pro Sekunde identische WM_MOUSEMOVE-Nachrichten
                if (tx != _lastTx || ty != _lastTy)
                {
                    _queue.MouseMoveAbsolute(tx, ty);
                    _lastTx = tx;
                    _lastTy = ty;
                }
            }

            _lastX = nx;
            _lastY = ny;
        }

        /// <summary>
        /// Reset method. Sets all movement state to idle and clears input queue.
        /// </summary>
        public void Reset()
        {
            ForceStop();
            _lastTx = -1;
            _lastTy = -1;
            _lastX = 0f;
            _lastY = 0f;
            _coastCounter = 0;
        }

        // ── Loot-Vacuum (kreist um Mittelpunkt) ───────────────────────────
        public void PerformLootVacuum(int ms)
        {
            double angle = (DateTime.Now.Ticks / 10_000.0) * 0.05;
            int lx = _cx + (int)(Math.Cos(angle) * 60);
            int ly = _cy + (int)(Math.Sin(angle) * 60);
            _queue.MouseMoveAbsolute(lx, ly);
            _queue.LeftClick();
            // FIX: Nach LeftClick() muss _isWalking = false gesetzt are, damit die Engine
            // next tick knows they need to send LeftDown() click again
            _isWalking = false;
        }

        // ── Sofort stoppen (z.B. bei FocusLock) ──────────────────────────
        public void ForceStop()
        {
            if (_isWalking)
            {
                _queue.LeftUp();
                _isWalking = false;
            }
        }
    }
}
