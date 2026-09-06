using System;
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