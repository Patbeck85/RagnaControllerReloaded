using System;
using System.Diagnostics;
using Microsoft.Win32;
using RagnaController.Core;

namespace RagnaController.Core
{
    /// <summary>
    /// Steam Deck / Handheld Sleep-Mode Handling.
    /// Erkennt Windows-Power-Mode-Änderungen (Suspend/Resume) and pausiert/
    /// setzt die Engine-Orchestrierung cleanly fort — damit no Reconnect-
    /// Issue nach dem Aufwecken aus dem Sleep-Modus entsteht.
    ///
    /// Architektur:
    ///   - SystemEvents.PowerModeChanged überwacht Suspend/Resume
    ///   - Bei Suspend: OnSuspend-Callback (EngineOrchestrator.Pause)
    ///   - Bei Resume:  OnResume-Callback  (EngineOrchestrator.Resume)
    ///   - Beide Callbacks are injizierbar → unit-testbar ohne echtes Windows-Event
    /// </summary>
    /// <summary>
    /// Background service for game functionality.
    /// </summary>
    public sealed class SleepModeService : IDisposable
    {
        private bool _disposed;
        private bool _isSuspended;

        /// <summary>Callback beim Suspend (Sleep). Is vom EngineOrchestrator gesetzt.</summary>
        public Action? OnSuspend { get; set; }

        /// <summary>Callback beim Resume (Aufwecken). Is vom EngineOrchestrator gesetzt.</summary>
        public Action? OnResume { get; set; }

        /// <summary>True if das System im Suspend/Sleep-Zustand is.</summary>
        public bool IsSuspended => _isSuspended;

        public SleepModeService()
        {
            // Windows Power-Mode-Events abonnieren
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        private void OnPowerModeChanged(object s, PowerModeChangedEventArgs e)
        {
            if (_disposed) return;

            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    _isSuspended = true;
                    Debug.WriteLine("[SleepModeService] System suspending → OnSuspend");
                    OnSuspend?.Invoke();
                    break;

                case PowerModes.Resume:
                    _isSuspended = false;
                    Debug.WriteLine("[SleepModeService] System resuming → OnResume");
                    OnResume?.Invoke();
                    break;

                case PowerModes.StatusChange:
                    // Status-Änderung (z. B. Batterie-Status) — ignorieren
                    break;
            }
        }

        /// <summary>
        /// Simuliert einen Suspend (for Unit-Tests ohne echtes Windows-Event).
        /// Löst OnSuspend aus and setzt _isSuspended = true.
        /// </summary>
        public void SimulateSuspend()
        {
            if (_disposed) return;
            _isSuspended = true;
            OnSuspend?.Invoke();
            Debug.WriteLine("[SleepModeService] Simulated suspend");
        }

        /// <summary>
        /// Simuliert einen Resume (for Unit-Tests ohne echtes Windows-Event).
        /// Löst OnResume aus and setzt _isSuspended = false.
        /// </summary>
        public void SimulateResume()
        {
            if (_disposed) return;
            _isSuspended = false;
            OnResume?.Invoke();
            Debug.WriteLine("[SleepModeService] Simulated resume");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            }
            catch
            {
                // SystemEvents kann bereits disposed sein — ignorieren
            }
        }
    }
}
