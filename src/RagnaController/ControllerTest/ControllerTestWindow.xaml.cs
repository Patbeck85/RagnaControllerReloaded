using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            // Timer only runs when recording or diagnostic is active (TECH-065)
            // _updateTimer.Start();  ← REMOVED

            // Subscribe to connection changes
            if (_controllerProvider is ControllerService cs)
            {
                cs.ControllerDetected += OnControllerDetected;
                cs.ControllerDisconnected += OnControllerDisconnected;
            }

            // Initial update
            UpdateControllerState(this, EventArgs.Empty);
            _isInitialized = true;

            EnsureTimerState();
        }

        /// <summary>
        /// TECH-065: Starts the update timer only when recording or diagnostic is active.
        /// Stops it when neither is running — 0 CPU usage when idle.
        /// </summary>
        private void EnsureTimerState()
        {
            bool shouldRun = _recorder.IsRecording || _diagnostic.IsRunning;
            if (shouldRun && !_updateTimer!.IsEnabled)
                _updateTimer!.Start();
            else if (!shouldRun && _updateTimer!.IsEnabled)
                _updateTimer!.Stop();
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

            // TEST-016/017: Build a sample every tick (50ms) — independent of Button-Change-Guard,
            // because sticks/triggers can change without button change. One sample per tick serves
            // both recording and guided input self-test (same raw values).
            var sample = BuildCurrentSample();

            if (_recorder.IsRecording) _recorder.RecordFrame(sample);
            if (_diagnostic.IsRunning) FeedDiagnostic(sample);

            // Recording status (TEST-016): Show frame counter live.
            if (_recorder.IsRecording)
            {
                RecordingStatus.Text = $"recording… ({_recorder.FrameCount} frames)";
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
            catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($@"[ControllerTestWindow] Snapshot read error: {ex.Message}");
                        }
        }

        // ------------------------------------------------------------------ TEST-016: Recording

        private void BtnRecordStart_Click(object sender, RoutedEventArgs e)
        {
            _recorder.Start();
            RecordingStatus.Text = "recording… (0 frames)";
            RecordingStatus.Foreground = System.Windows.Media.Brushes.Orange;
            EnsureTimerState();
        }

        private void BtnRecordStopSave_Click(object sender, RoutedEventArgs e)
        {
            if (!_recorder.IsRecording)
            {
                RecordingStatus.Text = "nothing active";
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
                RecordingStatus.Text = $"saved: {_recorder.FrameCount} frames";
                RecordingStatus.Foreground = System.Windows.Media.Brushes.LimeGreen;
            }
            catch (Exception ex)
            {
                // ERROR-001: no silent error — message in status.
                RecordingStatus.Text = $"Error: {ex.Message}";
                RecordingStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
            EnsureTimerState();
        }

        /// <summary>
        /// Baut aus dem aktuellen Controller-Zustand ein <see cref="RecordingSample"/>: Button-Maske
        /// aus dem vereinheitlichten ButtonState + ROH-Achsen/Trigger (no Deadzone, keine
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
            catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($@"[ControllerTestWindow] Build sample error: {ex.Message}");
                        }

            var bs = _controllerProvider.ButtonStates;
            return RecordingSample.FromButtonState(bs, lx, ly, rx, ry, lt, rt);
        }

        // ------------------------------------------------------------------ TEST-017: Eingabe-Selbsttest

        /// <summary>
        /// Plays a sample into the guided self-test (UI-Thread — Timer running on the
        /// Dispatcher). The active control is displayed per prompt, on completion the
        /// Pass/Fail-Ergebnis with Latenz p50/p95.
        /// </summary>
        private void FeedDiagnostic(RecordingSample sample)
        {
            _diagnostic.FeedSample(sample);

            // Prompt only updated on switch of active control, so that the
            // passing feedback line (✓/✗ + Latenz) not immediately overwritten is.
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
            DiagStatus.Text = "running…";
            DiagStatus.Foreground = System.Windows.Media.Brushes.Orange;
            EnsureTimerState();
        }

        private void OnDiagnosticControlCompleted(DiagnosticControlResult result)
        {
            // Dispatcher-safe: Timer already running on UI-Thread, update directly here.
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
                DiagPrompt.Text = $"✓ All {_diagnostic.TotalControls} inputs passed.";
                DiagStatus.Text = $"PASS — {_diagnostic.PassedCount}/{_diagnostic.TotalControls} passed";
                DiagStatus.Foreground = System.Windows.Media.Brushes.LimeGreen;
            }
            else
            {
                DiagPrompt.Text = "✗ Self-test completed — some inputs not detected.";
                DiagStatus.Text = $"FAIL — {_diagnostic.PassedCount}/{_diagnostic.TotalControls} passed";
                DiagStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
            EnsureTimerState();
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
                DiagStatus.Text = $"Report exported: {Path.GetFileName(path)}";
                DiagStatus.Foreground = System.Windows.Media.Brushes.LimeGreen;
            }
            catch (Exception ex)
            {
                // ERROR-001: no silent error — message in status.
                DiagStatus.Text = $"Export error: {ex.Message}";
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

            // TECH-025: Runner freigeben (Subscriber-Referenzen + inert).
            _diagnostic?.Dispose();

            base.OnClosed(e);
        }

        // TASK-2018: Keyboard shortcuts
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.R && !_recorder.IsRecording)
            {
                BtnRecordStart_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.S && _recorder.IsRecording)
            {
                BtnRecordStopSave_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.D && !_diagnostic.IsRunning)
            {
                BtnDiagStart_Click(sender, e);
                e.Handled = true;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove();
        }
    }
}