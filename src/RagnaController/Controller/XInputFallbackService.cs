using System;
using System.Runtime.InteropServices;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Controller
{
    /// <summary>
    /// XInput Fallback Service - provides Xbox controller support as a fallback
    /// when SDL2 gamecontroller detection fails or is unavailable.
    /// Maps XInput device indices to RagnaController ControllerService integration.
    /// </summary>
    public sealed class XInputFallbackService : IDisposable, RagnaController.Core.IControllerProvider
    {
        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState", CallingConvention = CallingConvention.StdCall)]
        private static extern uint Native_XInputGetState(uint dwUserIndex, ref XINPUT_STATE pState);

        [DllImport("xinput1_4.dll", EntryPoint = "XInputSetState", CallingConvention = CallingConvention.StdCall)]
        private static extern uint Native_XInputSetState(uint dwUserIndex, ref XINPUT_VIBRATION pVibration);

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetCapabilities", CallingConvention = CallingConvention.StdCall)]
        private static extern uint Native_XInputGetCapabilities(uint dwUserIndex, uint dwFlags, ref XINPUT_CAPABILITIES pCapabilities);

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_STATE
        {
            public uint dwPacketNumber;
            public XINPUT_GAMEPAD Gamepad;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_GAMEPAD
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_VIBRATION
        {
            public short wLeftMotorSpeed;
            public short wRightMotorSpeed;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_CAPABILITIES
        {
            public byte Type;
            public byte SubType;
            public byte Flags;
            public XINPUT_GAMEPAD Gamepad;
            public XINPUT_VIBRATION Vibration;
        }

        private readonly RagnaController.Core.ControllerService? _controllerService;
        private readonly Profile _profile;
        private bool _disposed;
        private int _lastReportedIndex = 0xFFFF;
        private XINPUT_STATE _tempState;

        // Battery level tracking
        private byte _batteryLevel = 0xFF; // 0xFF = unknown

        // State properties
        public bool IsConnected { get; private set; }
        public ButtonState ButtonStates { get; private set; }
        public string ControllerGuid { get; private set; }
        public string ControllerName { get; private set; }
        public string ControllerType { get; private set; }
        public string BatteryLevel { get; private set; }

        public XInputFallbackService(RagnaController.Core.ControllerService? controllerService = null, Profile? profile = null)
        {
            _controllerService = controllerService;
            _profile = profile ?? new Profile();
            _disposed = false;
            _lastReportedIndex = 0xFFFF;
            
            IsConnected = false;
            ControllerName = "No Controller";
            ControllerType = "Unknown";
            ControllerGuid = "";
            BatteryLevel = "Unknown";
        }

        /// <summary>
        /// Attempts to read state from XInput device at the given index.
        /// Returns true if a controller was found and state was read successfully.
        /// </summary>
        /// <param name="deviceIndex">The XInput device index (0-based).</param>
        /// <param name="parsedInput">Output parsed input from the controller.</param>
        /// <returns>true if XInput device was found and state read; false otherwise.</returns>
        public bool TryReadState(int deviceIndex, out ParsedInput parsedInput)
        {
            parsedInput = ParsedInput.Disconnected;

            // Only check each device once per reporting cycle to avoid redundant calls
            if (deviceIndex == _lastReportedIndex) return false;
            _lastReportedIndex = deviceIndex;

            uint result = Native_XInputGetState((uint)deviceIndex, ref _tempState);
            if (result != 0 && result != 116) return false; // 116 = ERROR_DEVICE_NOT_CONNECTED

            // Map XInput buttons to ParsedInput
            bool btnA = (_tempState.Gamepad.wButtons & 0x1000) != 0; // A button
            bool btnB = (_tempState.Gamepad.wButtons & 0x2000) != 0; // B button
            bool btnX = (_tempState.Gamepad.wButtons & 0x4000) != 0; // X button
            bool btnY = (_tempState.Gamepad.wButtons & 0x8000) != 0; // Y button
            bool btnLb = (_tempState.Gamepad.wButtons & 0x0001) != 0; // Left bumper
            bool btnRb = (_tempState.Gamepad.wButtons & 0x0002) != 0; // Right bumper
            bool btnBack = (_tempState.Gamepad.wButtons & 0x0004) != 0; // Back
            bool btnStart = (_tempState.Gamepad.wButtons & 0x0008) != 0; // Start
            bool btnLs = (_tempState.Gamepad.wButtons & 0x0010) != 0; // Left stick button
            bool btnRs = (_tempState.Gamepad.wButtons & 0x0020) != 0; // Right stick button

            // Map analog sticks (XInput uses -32768 to 32767, same as SDL)
            float lx = _tempState.Gamepad.sThumbLX / 32768f;
            float ly = _tempState.Gamepad.sThumbLY / 32768f;
            float rx = _tempState.Gamepad.sThumbRX / 32768f;
            float ry = _tempState.Gamepad.sThumbRY / 32768f;

            // Read triggers (0 to 255 in XInput, normalize to 0-1)
            float lt = _tempState.Gamepad.bLeftTrigger / 255f;
            float rt = _tempState.Gamepad.bRightTrigger / 255f;

            // Read battery level if available (XInput capability query)
            ReadBatteryLevel(deviceIndex);

            // Build GamepadButtonFlags from individual button states
            var rawButtons = GamepadButtonFlags.None;
            if (btnA) rawButtons |= GamepadButtonFlags.BtnA;
            if (btnB) rawButtons |= GamepadButtonFlags.BtnB;
            if (btnX) rawButtons |= GamepadButtonFlags.BtnX;
            if (btnY) rawButtons |= GamepadButtonFlags.BtnY;
            if (btnLb) rawButtons |= GamepadButtonFlags.L1;
            if (btnRb) rawButtons |= GamepadButtonFlags.R1;
            if (lt > 0.15f) rawButtons |= GamepadButtonFlags.L2;
            if (rt > 0.15f) rawButtons |= GamepadButtonFlags.R2;
            if (btnLs) rawButtons |= GamepadButtonFlags.L3;
            if (btnRs) rawButtons |= GamepadButtonFlags.R3;
            if (btnStart) rawButtons |= GamepadButtonFlags.Start;
            if (btnBack) rawButtons |= GamepadButtonFlags.Back;

            parsedInput = new ParsedInput
            {
                IsConnected = true,
                LeftX = lx,
                LeftY = ly,
                RightX = rx,
                RightY = ry,
                TriggerLeft = lt,
                TriggerRight = rt,
                L1 = btnLb,
                R1 = btnRb,
                L2 = lt > 0.15f,
                R2 = rt > 0.15f,
                L3 = btnLs,
                R3 = btnRs,
                BtnA = btnA,
                BtnB = btnB,
                BtnX = btnX,
                BtnY = btnY,
                Start = btnStart,
                Back = btnBack,
                RawButtons = rawButtons,
                PrevRawButtons = GamepadButtonFlags.None
            };

            // Update state properties
            IsConnected = true;
            ControllerName = $"XInput Device {deviceIndex}";
            ControllerType = "xinput";
            ControllerGuid = _profile.ControllerGuid;
            ButtonStates = new ButtonState
            {
                APressed = btnA,
                BPressed = btnB,
                XPressed = btnX,
                YPressed = btnY,
                L1Pressed = btnLb,
                R1Pressed = btnRb,
                L2Pressed = lt > 0.15f,
                R2Pressed = rt > 0.15f,
                StartPressed = btnStart,
                BackPressed = btnBack,
                DPadUp = false,
                DPadDown = false,
                DPadLeft = false,
                DPadRight = false,
                L3Pressed = btnLs,
                R3Pressed = btnRs
            };
            BatteryLevel = GetBatteryLevel();

            return true;
        }

        private XINPUT_CAPABILITIES _tempCapabilities;

        /// <summary>
        /// Reads the battery level from XInput for the specified device.
        /// XInput returns battery level as a percentage (0-100) or device power state.
        /// </summary>
        /// <param name="deviceIndex">The XInput device index (0-based).</param>
        private void ReadBatteryLevel(int deviceIndex)
        {
            try
            {
                if (Native_XInputGetCapabilities((uint)deviceIndex, 0, ref _tempCapabilities) == 0)
                {
                    // Check for battery info in capabilities
                    if ((_tempCapabilities.Flags & 0x04) != 0) // GAMEPAD_CAPS_BATTERY
                    {
                        _batteryLevel = _tempCapabilities.Flags;
                    }
                    else
                    {
                        _batteryLevel = 0xFF; // Unknown
                    }
                }
                else
                {
                    _batteryLevel = 0xFF; // Unknown
                }
            }
            catch
            {
                _batteryLevel = 0xFF; // Unknown on error
            }
        }

        /// <summary>
        /// Gets the current battery level as a human-readable string.
        /// </summary>
        public string GetBatteryLevel()
        {
            if (_batteryLevel == 0xFF) return "Unknown";

            int percent = _batteryLevel;
            if (percent >= 80) return "Full";
            if (percent >= 60) return "High";
            if (percent >= 40) return "Mid";
            if (percent >= 20) return "Low";
            return "Empty";
        }

        /// <summary>
        /// Sets vibration/motor feedback on the XInput controller.
        /// </summary>
        /// <param name="leftMotorSpeed">Left motor speed (0-65535).</param>
        /// <param name="rightMotorSpeed">Right motor speed (0-65535).</param>
        public void SetVibration(uint leftMotorSpeed, uint rightMotorSpeed)
        {
            if (_controllerService == null) return;

            var vibration = new XINPUT_VIBRATION
            {
                wLeftMotorSpeed = (short)leftMotorSpeed,
                wRightMotorSpeed = (short)rightMotorSpeed
            };

            uint result = Native_XInputSetState(0, ref vibration);
            if (result != 0) System.Diagnostics.Debug.WriteLine($"[XInputFallback] SetVibration failed: error code {result}");
        }

        // IControllerProvider implementation
        public void DetectController()
        {
            // Try all 4 XInput devices
            for (int i = 0; i < 4; i++)
            {
                if (TryReadState(i, out _))
                {
                    return; // Found one
                }
            }
            // None found
            IsConnected = false;
            ControllerName = "No Controller";
            ControllerType = "Unknown";
            ControllerGuid = "";
            ButtonStates = default;
            BatteryLevel = "Unknown";
        }

        public void SetRumble(float left, float right)
        {
            uint leftMotor = (uint)(Math.Clamp(left, 0f, 1f) * 65535);
            uint rightMotor = (uint)(Math.Clamp(right, 0f, 1f) * 65535);
            SetVibration(leftMotor, rightMotor);
        }

        public void SetLED(byte r, byte g, byte b)
        {
            // XInput doesn't support LED
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}