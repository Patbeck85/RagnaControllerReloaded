using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using RagnaController.Controller;
using RagnaController.Core;
using Hexa.NET.SDL2;

namespace RagnaController.ControllerTest
{
    /// <summary>
    /// Controller test window that appears when a controller is connected.
    /// Shows all button states and thumbstick positions for testing.
    /// </summary>
    public unsafe partial class ControllerTestWindow : Window
    {
        private readonly IControllerProvider _controllerProvider;
        private readonly ControllerSessionRecorder _recorder = new();
        private readonly ControllerDiagnosticRunner _diagnostic = new();
        private int _diagShownIndex;
        private DispatcherTimer? _updateTimer;
        private ButtonState _lastButtonState;
        private bool _isInitialized;

        public ControllerTestWindow(IControllerProvider controllerProvider)
        {
            InitializeComponent();
            _controllerProvider = controllerProvider;
            _lastButtonState = new ButtonState();
            _isInitialized = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(50); // 20 FPS update
            _updateTimer.Tick += UpdateControllerState;
            _updateTimer.Start();

            // Subscribe to connection changes
            if (_controllerProvider is ControllerService cs)
            {
                cs.ControllerDetected += OnControllerDetected;
                cs.ControllerDisconnected += OnControllerDisconnected;
            }

            // Initial update
            UpdateControllerState(this, EventArgs.Empty);
            _isInitialized = true;
        }

        private void OnControllerDetected(object? sender, EventArgs e)
        {
            // Controller connected - window is already shown
            _updateTimer?.Start();
        }

        private void OnControllerDisconnected(object? sender, EventArgs e)
        {
            // Controller disconnected - stop updates and close
            _updateTimer?.Stop();
            this.Close();
        }

        private void UpdateControllerState(object? sender, EventArgs e)
        {
            if (!_isInitialized || _controllerProvider == null) return;

            // TEST-016/017: jeden Tick (50ms) ein Sample bauen — unabhängig vom Button-Change-Guard,
            // da Sticks/Trigger sich ändern können ohne Button-Wechsel. Ein Sample pro Tick dient
            // sowohl der Aufnahme als auch dem geführten Eingabe-Selbsttest (gleiche Rohwerte).
            var sample = BuildCurrentSample();

            if (_recorder.IsRecording) _recorder.RecordFrame(sample);
            if (_diagnostic.IsRunning) FeedDiagnostic(sample);

            // Aufnahme-Status (TEST-016): Frame-Zähler live anzeigen.
            if (_recorder.IsRecording)
            {
                RecordingStatus.Text = $"läuft… ({_recorder.FrameCount} Frames)";
            }

            // Get current button state
            var currentState = _controllerProvider.ButtonStates;
            if (currentState == _lastButtonState && _updateTimer != null) return;
            _lastButtonState = currentState;

            // Update UI - Dispatcher-safe
            Dispatcher.Invoke(() =>
            {
                UpdateButtonStates(currentState);
                UpdateThumbsticks(currentState);
            });
        }

        private void UpdateButtonStates(ButtonState state)
        {
            // Update all button indicators - match XAML control names
            BtnAPressed.IsChecked = state.APressed;
            BtnBPressed.IsChecked = state.BPressed;
            BtnXPressed.IsChecked = state.XPressed;
            BtnYPressed.IsChecked = state.YPressed;

            BtnL1.IsChecked = state.L1Pressed;
            BtnR1.IsChecked = state.R1Pressed;

            BtnStart.IsChecked = state.StartPressed;
            BtnBack.IsChecked = state.BackPressed;

            BtnL3.IsChecked = state.L3Pressed;
            BtnR3.IsChecked = state.R3Pressed;
        }

        private unsafe void UpdateThumbsticks(ButtonState state)
        {
            // Thumbstick data comes from the controller provider via SDL
            try
            {
                if (_controllerProvider is ControllerService cs)
                {
                    var pad = cs.GetControllerSnapshot();
                    if (pad != null)
                    {
                        // Read Axes using SDL functions (same pattern as InputReader.cs)
                        // SDL uses -32768 to 32767
                        float lx = SDL.GameControllerGetAxis(pad, SDLGameControllerAxis.Leftx) / 32768f;
                        float ly = SDL.GameControllerGetAxis(pad, SDLGameControllerAxis.Lefty) / -32768f; // Invert Y so Up is positive
                        float rx = SDL.GameControllerGetAxis(pad, SDLGameControllerAxis.Rightx) / 32768f;
                        float ry = SDL.GameControllerGetAxis(pad, SDLGameControllerAxis.Righty) / -32768f;

                        // Update visual indicators - using TranslateTransform from XAML
                        LeftStickPos.X = lx * 30; // Max radius 30
                        LeftStickPos.Y = ly * 30;
                        RightStickPos.X = rx * 30;
                        RightStickPos.Y = ry * 30;

                        // Update text labels (we need to add these to XAML if needed)
                    }
                }
            }
            catch
            {
                // Ignore errors reading raw snapshot
            }
        }

        // ------------------------------------------------------------------ TEST-016: Aufnahme

        private void BtnRecordStart_Click(object sender, RoutedEventArgs e)
        {
            _recorder.Start();
            RecordingStatus.Text = "läuft… (0 Frames)";
            RecordingStatus.Foreground = System.Windows.Media.Brushes.Orange;
        }

        private void BtnRecordStopSave_Click(object sender, RoutedEventArgs e)
        {
            if (!_recorder.IsRecording)
            {
                RecordingStatus.Text = "nichts aktiv";
                return;
            }
            _recorder.Stop();

            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RagnaController", "recordings");
            string path = Path.Combine(dir, $"session-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            try
            {
                _recorder.Save(path);
                RecordingStatus.Text = $"gespeichert: {_recorder.FrameCount} Frames";
                RecordingStatus.Foreground = System.Windows.Media.Brushes.LimeGreen;
            }
            catch (Exception ex)
            {
                // ERROR-001: kein stiller Fehler — Meldung im Status.
                RecordingStatus.Text = $"Fehler: {ex.Message}";
                RecordingStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        /// <summary>
        /// Baut aus dem aktuellen Controller-Zustand ein <see cref="RecordingSample"/>: Button-Maske
        /// aus dem vereinheitlichten ButtonState + ROH-Achsen/Trigger (kein Deadzone, keine
        /// Normalisierung — exakt das Format der CI-Replay-Fixtures). Dient sowohl TEST-016
        /// (Aufnahme) als auch TEST-017 (Eingabe-Selbsttest) als gemeinsame Datenbasis.
        /// </summary>
        private unsafe RecordingSample BuildCurrentSample()
        {
            bool connected = _controllerProvider.IsConnected;
            if (!connected) return RecordingSample.Disconnected;

            float lx = 0, ly = 0, rx = 0, ry = 0, lt = 0, rt = 0;
            try
            {
                if (_controllerProvider is ControllerService cs)
                {
                    var pad = cs.GetControllerSnapshot();
                    if (pad != null)
                    {
                        // Gleiche Rohwerte wie InputReader vor dem Deadzone: SDL -32768..32767 / 0..32767.
                        lx = SDL.GameControllerGetAxis(pad, SDLGameControllerAxis.Leftx) / 32768f;
                        ly = SDL.GameControllerGetAxis(pad, SDLGameControllerAxis.Lefty) / -32768f; // Y invertiert (Up positiv)
                        rx = SDL.GameControllerGetAxis(pad, SDLGameControllerAxis.Rightx) / 32768f;
                        ry = SDL.GameControllerGetAxis(pad, SDLGameControllerAxis.Righty) / -32768f;
                        lt = SDL.GameControllerGetAxis(pad, SDLGameControllerAxis.Triggerleft) / 32767f;
                        rt = SDL.GameControllerGetAxis(pad, SDLGameControllerAxis.Triggerright) / 32767f;
                    }
                }
            }
            catch
            {
                // Snapshot-Lesefehler darf die UI nicht stören (Rohwerte bleiben 0).
            }

            var bs = _controllerProvider.ButtonStates;
            return RecordingSample.FromButtonState(bs, lx, ly, rx, ry, lt, rt);
        }

        // ------------------------------------------------------------------ TEST-017: Eingabe-Selbsttest

        /// <summary>
        /// Spielt ein Sample in den geführten Selbsttest ein (UI-Thread — Timer läuft auf dem
        /// Dispatcher). Die aktive Kontrolle wird per Prompt angezeigt, bei Abschluss das
        /// Pass/Fail-Ergebnis mit Latenz p50/p95.
        /// </summary>
        private void FeedDiagnostic(RecordingSample sample)
        {
            _diagnostic.FeedSample(sample);

            // Prompt nur bei Wechsel der aktiven Kontrolle aktualisieren, damit die
            // Bestehens-Feedback-Zeile (✓/✗ + Latenz) nicht sofort überschrieben wird.
            var current = _diagnostic.CurrentControl;
            int idx = _diagnostic.CurrentIndex;
            if (current != null && idx >= 0 && idx != _diagShownIndex)
            {
                _diagShownIndex = idx;
                DiagPrompt.Text = $"{idx + 1}/{_diagnostic.TotalControls} — {current.Value.Prompt}";
            }
        }

        private void BtnDiagStart_Click(object sender, RoutedEventArgs e)
        {
            _diagnostic.ControlCompleted -= OnDiagnosticControlCompleted;
            _diagnostic.Completed -= OnDiagnosticCompleted;
            _diagnostic.ControlCompleted += OnDiagnosticControlCompleted;
            _diagnostic.Completed += OnDiagnosticCompleted;

            _diagnostic.Start();
            _diagShownIndex = -1;
            BtnDiagStart.IsEnabled = false;
            BtnDiagExport.IsEnabled = false;
            DiagPrompt.Text = $"1/{_diagnostic.TotalControls} — {_diagnostic.CurrentControl?.Prompt ?? ""}";
            DiagStatus.Text = "läuft…";
            DiagStatus.Foreground = System.Windows.Media.Brushes.Orange;
        }

        private void OnDiagnosticControlCompleted(DiagnosticControlResult result)
        {
            // Dispatcher-safe: Timer läuft bereits auf dem UI-Thread, hier direkt updaten.
            string mark = result.Passed ? "✓" : "✗";
            DiagPrompt.Text = $"{mark} {result.Name} — {result.Prompt}" +
                              $"  (p50 {result.P50Ms:0.#} ms / p95 {result.P95Ms:0.#} ms)";
        }

        private void OnDiagnosticCompleted()
        {
            BtnDiagStart.IsEnabled = true;
            BtnDiagExport.IsEnabled = true;

            if (_diagnostic.AllPassed)
            {
                DiagPrompt.Text = $"✓ Alle {_diagnostic.TotalControls} Eingaben einwandfrei.";
                DiagStatus.Text = $"PASS — {_diagnostic.PassedCount}/{_diagnostic.TotalControls} bestanden";
                DiagStatus.Foreground = System.Windows.Media.Brushes.LimeGreen;
            }
            else
            {
                DiagPrompt.Text = "✗ Selbsttest abgeschlossen — einige Eingaben nicht erkannt.";
                DiagStatus.Text = $"FAIL — {_diagnostic.PassedCount}/{_diagnostic.TotalControls} bestanden";
                DiagStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void BtnDiagExport_Click(object sender, RoutedEventArgs e)
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RagnaController", "diagnostics");
            string path = Path.Combine(dir, $"controller-diagnostic-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            try
            {
                _diagnostic.SaveJsonReport(path);
                DiagStatus.Text = $"Report exportiert: {Path.GetFileName(path)}";
                DiagStatus.Foreground = System.Windows.Media.Brushes.LimeGreen;
            }
            catch (Exception ex)
            {
                // ERROR-001: kein stiller Fehler — Meldung im Status.
                DiagStatus.Text = $"Export-Fehler: {ex.Message}";
                DiagStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        // Cleanup on close
        protected override void OnClosed(EventArgs e)
        {
            _updateTimer?.Stop();
            _updateTimer = null;
            
            if (_controllerProvider is ControllerService cs)
            {
                cs.ControllerDetected -= OnControllerDetected;
                cs.ControllerDisconnected -= OnControllerDisconnected;
            }

            base.OnClosed(e);
        }
    }
}