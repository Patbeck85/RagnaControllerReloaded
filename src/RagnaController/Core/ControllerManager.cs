using System;
using RagnaController.Controller;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// Unified controller manager that automatically tries SDL2 first, with fallback to XInput.
    /// Implements IControllerProvider for use throughout the application.
    /// </summary>
    public class ControllerManager : IControllerProvider
    {
        private readonly ControllerService _sdlProvider;
        private readonly XInputFallbackService _xInputProvider;
        private IControllerProvider _activeProvider;
        private readonly object _lock = new();

        // State tracking
        private bool _isConnected;
        private ButtonState _buttonStates = new();
        private string _controllerGuid = "";
        private string _controllerName = "";
        private string _controllerType = "";
        private string _batteryLevel = "Unknown";

        public ControllerManager()
        {
            // Initialize with SDL2 provider first
            _sdlProvider = new ControllerService();
            _xInputProvider = new XInputFallbackService();

            // Set active provider to SDL2 initially
            _activeProvider = _sdlProvider;

            // Subscribe to SDL provider changes
            _sdlProvider.ControllerDetected += OnSdlControllerDetected;
        }

        // ── IControllerProvider Implementation ──────────────────────

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                    IsConnectedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public ButtonState ButtonStates
        {
            get => _buttonStates;
            private set
            {
                _buttonStates = value;
                OnPropertyChanged();
            }
        }

        public string ControllerGuid
        {
            get => _controllerGuid;
            private set
            {
                _controllerGuid = value;
                OnPropertyChanged();
            }
        }

        public string ControllerName
        {
            get => _controllerName;
            private set
            {
                _controllerName = value;
                OnPropertyChanged();
            }
        }

        public string ControllerType
        {
            get => _controllerType;
            private set
            {
                _controllerType = value;
                OnPropertyChanged();
            }
        }

        public string BatteryLevel
        {
            get => _batteryLevel;
            private set
            {
                _batteryLevel = value;
                OnPropertyChanged();
            }
        }

        // Events
        public event EventHandler? ControllerConnected;
        public event EventHandler? ControllerDisconnected;
        public event EventHandler? ProviderChanged;
        public event EventHandler? IsConnectedChanged;

        public void DetectController()
        {
            _sdlProvider.DetectController();
            // Also trigger XInput fallback check
            TryXInputFallback();
        }

        public void SetRumble(float left, float right)
        {
            lock (_lock)
            {
                _activeProvider?.SetRumble(left, right);
            }
        }

        public void SetLED(byte r, byte g, byte b)
        {
            lock (_lock)
            {
                _activeProvider?.SetLED(r, g, b);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _sdlProvider.ControllerDetected -= OnSdlControllerDetected;
                _sdlProvider?.Dispose();
                _xInputProvider?.Dispose();
            }
        }

        // ── Private Helpers ─────────────────────────────────────────

        private void OnSdlControllerDetected(object? sender, EventArgs e)
        {
            // SDL2 found a controller - update state from SDL provider
            UpdateFromProvider(_sdlProvider);

            // Switch active provider to SDL2 if not already
            if (_activeProvider != _sdlProvider)
            {
                _activeProvider = _sdlProvider;
                ProviderChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void TryXInputFallback()
        {
            // Try XInput as fallback - check up to 4 XInput devices
            for (int i = 0; i < 4; i++)
            {
                if (_xInputProvider.TryReadState(i, out var parsedInput))
                {
                    if (parsedInput.IsConnected)
                    {
                        // XInput found a controller
                        IsConnected = true;
                        ControllerName = parsedInput.ControllerName ?? $"XInput Device {i}";
                        ControllerType = parsedInput.ControllerType ?? "xinput";
                        ControllerGuid = parsedInput.ControllerGuid ?? "";
                        ButtonStates = ConvertParsedInputToButtonState(parsedInput);
                        BatteryLevel = _xInputProvider.GetBatteryLevel() ?? "Unknown";

                        // Switch active provider to XInput if SDL isn't connected
                        if (!_sdlProvider.IsConnected && _activeProvider != _xInputProvider)
                        {
                            _activeProvider = _xInputProvider;
                            ProviderChanged?.Invoke(this, EventArgs.Empty);
                        }
                        return;
                    }
                }
            }

            // No XInput controller found
            if (!_sdlProvider.IsConnected)
            {
                IsConnected = false;
                ControllerName = "No Controller";
                ControllerType = "Unknown";
                ControllerGuid = "";
                ButtonStates = default;
                BatteryLevel = "Unknown";
            }
        }

        private void UpdateFromProvider(ControllerService provider)
        {
            IsConnected = provider.IsConnected;
            ControllerName = provider.ControllerName;
            ControllerType = provider.ControllerType;
            ControllerGuid = provider.ControllerGuid;
            ButtonStates = provider.GetButtonStates();
            BatteryLevel = provider.BatteryLevel ?? "Unknown";

            if (IsConnected)
            {
                ControllerConnected?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ControllerDisconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        private ButtonState ConvertParsedInputToButtonState(ParsedInput input)
        {
            return new ButtonState
            {
                APressed = input.BtnA,
                BPressed = input.BtnB,
                XPressed = input.BtnX,
                YPressed = input.BtnY,
                L1Pressed = input.L1,
                R1Pressed = input.R1,
                L2Pressed = input.L2,
                R2Pressed = input.R2,
                StartPressed = input.Start,
                BackPressed = input.Back,
                DPadUp = input.DPadUp,
                DPadDown = input.DPadDown,
                DPadLeft = input.DPadLeft,
                DPadRight = input.DPadRight,
                L3Pressed = input.L3,
                R3Pressed = input.R3
            };
        }

        // PropertyChanged helper (simplified - could be INotifyPropertyChanged)
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) { }

        // Event invocation helpers
        private void OnControllerConnected(object? sender, EventArgs e) => ControllerConnected?.Invoke(sender, e);
        private void OnControllerDisconnected(object? sender, EventArgs e) => ControllerDisconnected?.Invoke(sender, e);
        private void OnProviderChanged(object? sender, EventArgs e) => ProviderChanged?.Invoke(sender, e);
        private void OnIsConnectedChanged(object? sender, EventArgs e) => IsConnectedChanged?.Invoke(sender, e);
    }
}