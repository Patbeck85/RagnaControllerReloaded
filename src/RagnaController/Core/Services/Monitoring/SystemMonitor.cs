using System;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Views.Windows;

namespace RagnaController.Core
{
    /// <summary>
    /// Background service for game functionality.
    /// </summary>
    public sealed class SystemMonitor
    {
        private readonly WindowTracker _tracker;
        private readonly MovementEngine _movement;
        private readonly INativeMethods _native;
        private IntPtr _lastHwnd;

        public bool IsFocusLocked { get; private set; }
        public bool IsTracking => _tracker.IsTracking;
        public float DpiScale => _tracker.DpiScale;

        // Focus-Lock Einstellungen (from HybridEngine gesetzt, from MainWindow aus Settings geladen)
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

        public SystemMonitor(WindowTracker tracker, MovementEngine movement, INativeMethods? native = null)
        {
            _tracker  = tracker;
            _movement = movement;
            _native   = native ?? new NativeMethodsImpl();

            // Sofort Center setzen if Fenster kommt — nicht on nächsten Poll-Zyklus warten
            _tracker.ForegroundChanged += () =>
            {
                if (_tracker.IsTracking)
                    _movement.SetCenter(_tracker.CenterX, _tracker.CenterY, _tracker.ClientH, _tracker.DpiScale);
            };
        }

        public void Update()
        {
            _tracker.Refresh();
            _lastHwnd = _native.GetForegroundWindow();

            if (_tracker.IsTracking)
            {
                _movement.SetCenter(_tracker.CenterX, _tracker.CenterY, _tracker.ClientH, _tracker.DpiScale);
                IsFocusLocked = false;
            }
            else
            {
                // Only sperren if FocusLock active is UND das Fenster nicht RO is
                // verhindert, dass der Cursor im RO-Fenster gesperrt is
                IsFocusLocked = FocusLockEnabled && !_tracker.IsTracking;
            }
        }
    }
}