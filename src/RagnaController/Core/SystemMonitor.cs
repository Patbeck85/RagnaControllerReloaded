using System;

namespace RagnaController.Core
{
    public sealed class SystemMonitor
    {
        private readonly WindowTracker _tracker;
        private readonly MovementEngine _movement;
        private IntPtr _lastHwnd;
        private int _checkCounter;

        public bool IsFocusLocked { get; private set; }
        public bool IsTracking => _tracker.IsTracking;
        public float DpiScale => _tracker.DpiScale;

        // Focus-Lock Einstellungen (von HybridEngine gesetzt, von MainWindow aus Settings geladen)
        public bool FocusLockEnabled { get; set; } = true;

        private string _focusLockProcess = "ragexe";
        public string FocusLockProcess
        {
            get => _focusLockProcess;
            set
            {
                _focusLockProcess = value;
                _tracker.SetProcessName(value);   // WindowTracker immer synchron halten
                _tracker.Refresh();               // Sofort neu suchen mit neuem Namen
            }
        }

        public SystemMonitor(WindowTracker tracker, MovementEngine movement)
        {
            _tracker  = tracker;
            _movement = movement;

            // Sofort Center setzen wenn Fenster kommt — nicht auf nächsten Poll-Zyklus warten
            _tracker.ForegroundChanged += () =>
            {
                if (_tracker.IsTracking)
                    _movement.SetCenter(_tracker.CenterX, _tracker.CenterY, _tracker.ClientH, _tracker.DpiScale);
            };
        }

        private bool _firstUpdate = true;

        public void Update()
        {
            // Force an immediate check on the very first tick after startup —
            // otherwise IsTracking stays false (and all input is blocked) for
            // up to 500ms while we wait for the normal poll interval.
            if (_firstUpdate)
            {
                _firstUpdate = false;
                _checkCounter = 60;
            }

            // Nur alle 500ms (60 Ticks) teure Checks machen
            if (++_checkCounter < 60)
            {
                // Schneller Check: Ist das aktuelle Fenster noch RO?
                if (NativeMethods.GetForegroundWindow() != _lastHwnd)
                {
                    _checkCounter = 60; // FIX: Sofortiger Deep-Check im SELBEN Tick (nicht warten auf nächsten)
                }
                return;
            }
            _checkCounter = 0;

            _tracker.Refresh();
            _lastHwnd = NativeMethods.GetForegroundWindow();

            if (_tracker.IsTracking)
            {
                _movement.SetCenter(_tracker.CenterX, _tracker.CenterY, _tracker.ClientH, _tracker.DpiScale);
                IsFocusLocked = false;
            }
            else
            {
                // Nur sperren wenn FocusLock aktiv ist UND das Fenster nicht RO ist
                // verhindert, dass der Cursor im RO-Fenster gesperrt wird
                IsFocusLocked = FocusLockEnabled && !_tracker.IsTracking;
            }
        }
    }
}