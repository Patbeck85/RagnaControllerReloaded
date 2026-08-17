using System;
using Microsoft.Win32;
using RagnaController.Controller;

namespace RagnaController.Core
{
    /// <summary>
    /// Handles Windows power-mode transitions (Sleep / Resume / Hibernate).
    ///
    /// Problem:
    ///   When a laptop lid closes, Windows suspends the process. On resume:
    ///   (a) USB HID device handles held by ControllerService become stale
    ///   (b) timeBeginPeriod/timeEndPeriod lose their calibration
    ///   (c) The multimedia timer in BackgroundTickProvider may fire erratically
    ///   (d) WindowTracker's cached HWND is invalidated if RO reconnected
    ///
    /// Fix:
    ///   Subscribe to SystemEvents.PowerModeChanged (fires on the UI thread).
    ///   On Suspend → stop the engine cleanly.
    ///   On Resume  → schedule a re-init after a short delay (hardware needs ~2s to settle).
    /// </summary>
    public sealed class PowerModeService : IDisposable
    {
        private readonly HybridEngine    _engine;
        private readonly ControllerService _ctrl;
        private readonly WindowTracker   _tracker;

        /// <summary>Fired on resume — MainWindow can show a "Reconnecting…" banner.</summary>
        public event Action<bool>? SleepStateChanged; // true = sleeping, false = awake

        private bool _disposed;
        private System.Threading.CancellationTokenSource _resumeCts = new();

        public PowerModeService(HybridEngine engine, ControllerService ctrl, WindowTracker tracker)
        {
            _engine  = engine;
            _ctrl    = ctrl;
            _tracker = tracker;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        private async void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    // ── Going to sleep ─────────────────────────────────────
                    SleepStateChanged?.Invoke(true);
                    // Stop engine cleanly: drains queue, releases Alt, stops rumble
                    if (_engine.IsRunning || _engine.IsPaused)
                        _resumeCts.Cancel();
                    _resumeCts = new System.Threading.CancellationTokenSource();
                    _engine.Pause();
                    System.Diagnostics.Debug.WriteLine("[PowerMode] Suspended — engine paused.");
                    break;

                case PowerModes.Resume:
                    // ── Waking up — hardware needs time to re-enumerate ────
                    // 2500ms: empirically the minimum for USB HID re-init on most BIOSes
                    try { await System.Threading.Tasks.Task.Delay(2500, _resumeCts.Token); }
                    catch (System.Threading.Tasks.TaskCanceledException) { break; }

                    // Force WindowTracker to re-detect the RO window
                    _tracker.StopHook();
                    _tracker.StartHook();

                    // Re-detect controller (USB may have re-enumerated with a new handle)
                    _ctrl.DetectController();

                    SleepStateChanged?.Invoke(false);
                    System.Diagnostics.Debug.WriteLine("[PowerMode] Resumed — controller re-detected.");
                    break;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        }
    }
}
