using System;

namespace RagnaController.Core
{
    /// <summary>
    /// Bewegt den Charakter durch Click-to-Move (linke Maustaste halten).
    /// Rechter Stick → Mauszeiger, linker Stick → Klick-Richtung.
    /// </summary>
    public class MovementEngine
    {
        private readonly InputCommandQueue _queue;
        private readonly WindowTracker _tracker;

        // Fenster-Mittelpunkt (vom WindowTracker gesetzt)
        private int   _cx, _cy;
        private float _radiusScale = 1.0f;   // DPI × Auflösungs-Skalierung kombiniert

        private const float BASE_RES_H = 1080f;  // Referenz-Auflösung (1080p)

        // Zustand
                private bool  _isWalking;
                public  bool  IsWalking => _isWalking;
        
                // FEAT-007: Property for SkillOrchestrator condition evaluation
                public bool IsMoving => _isWalking;
        private float _lastX, _lastY;
        private int   _lastTx = -1; // letzte gesendete X-Koordinate (für Deduplizierung)
        private int   _lastTy = -1; // letzte gesendete Y-Koordinate (für Deduplizierung)

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
            // Auflösung relativ zu 1080p × DPI → ein einziger Multiplikator
            float resScale = clientH > 0 ? clientH / BASE_RES_H : 1.0f;
            _radiusScale   = resScale * (dpiScale > 0f ? dpiScale : 1.0f);
        }

        // ── Haupt-Update (jeder Tick) ─────────────────────────────────────
        public void Update(float x, float y)
        {
            if (!ActionRpgMode) { ForceStop(); return; }
            float sqMag = x * x + y * y;

            if (sqMag <= Deadzone * Deadzone)
            {
                if (_isWalking)
                {
                    if (_coastCounter < CoastFrames) { _coastCounter++; }
                    else
                    {
                        _queue.LeftUp();
                        _isWalking = false;
                        _coastCounter = 0;
                        // Cursor zurück zur Fenstermitte — verhindert Fehlklicks
                        // beim nächsten Stick-Impuls
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

            // Radius: LeashRadius ist in 1080p-Pixeln definiert → skalieren
            int radius = (int)(LeashRadius * _radiusScale);

            // FIX: Begrenze den Klick auf den tatsächlichen Innenraum des Fensters
            int limitX = _tracker.ClientW / 2;
            int limitY = _tracker.ClientH / 2;
            
            // Verhindere Klicks außerhalb des Game-Clients
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
                // FIX: Nur senden, wenn sich die Koordinaten tatsächlich geändert haben
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

        public void Reset() => ForceStop();

        // ── Loot-Vacuum (kreist um Mittelpunkt) ───────────────────────────
        public void PerformLootVacuum(int ms)
        {
            double angle = (DateTime.Now.Ticks / 10_000.0) * 0.05;
            int lx = _cx + (int)(Math.Cos(angle) * 60);
            int ly = _cy + (int)(Math.Sin(angle) * 60);
            _queue.MouseMoveAbsolute(lx, ly);
            _queue.LeftClick();
            // FIX: Nach LeftClick() muss _isWalking = false gesetzt werden, damit die Engine
            // im nächsten Tick weiß, dass sie wieder einen LeftDown() Klick senden muss
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
