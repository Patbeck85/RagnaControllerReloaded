using System;
using System.Runtime.InteropServices;
using System.Threading;
using Hexa.NET.SDL2;

namespace RagnaController.Controller
{
    /// <summary>
    /// SDL2 GameController wrapper.
    /// All SDL calls run on a single dedicated background thread (_sdlThread)
    /// because SDL2 is NOT thread-safe for event pumping — PumpEvents() must
    /// be called from the same OS thread that called SDL.Init().
    /// 
    /// Also hosts a hidden window for WM_DEVICECHANGE to enable instant controller hot-plug detection.
    /// </summary>
    public unsafe class ControllerService : IDisposable
    {
        // ── Shared state (volatile for cross-thread visibility) ──────────
        private volatile bool    _isConnected;
        private volatile bool    _disposed;
        private SDLGameController* _controller = null;

        // ── Thread-safe snapshot (cached per frame, read-only) ───────────
        // SDL thread updates _controller and _controllerSnapshot; main thread
        // reads the snapshot without locking — single-writer / single-reader pattern.
        private volatile SDLGameController* _controllerSnapshot = null;

        // ── Thread synchronization ─────────────────────────────────────────
                private readonly Thread _sdlThread;
                private readonly ManualResetEventSlim _scanNow = new ManualResetEventSlim(false);
                private Core.DeviceNotificationWindow? _deviceNotificationWindow;

        public bool   IsConnected   => _isConnected;
        public string ControllerName  { get; private set; } = "No Controller";
        public string ControllerType  { get; private set; } = "Unknown";

        public ControllerService()
        {
            _sdlThread = new Thread(SdlThreadLoop)
            {
                Name         = "SDL2_ControllerThread",
                IsBackground = true
            };
            _sdlThread.Start();
        }

        // ── Public API ──────────────────────────────────────────────────────

        /// <summary>
        /// Request an immediate controller scan (signal the SDL thread).
        /// Safe to call from any thread.
        /// </summary>
        public void DetectController() => _scanNow.Set();

        public SDLGameController* GetRawController() => _controller;

        /// <summary>
        /// Get a read-only snapshot of the current controller state.
        /// The snapshot is updated by the SDL thread each frame and can be safely
        /// read from any thread without racing — zero-allocation, lock-free pattern.
        /// </summary>
        public SDLGameController* GetControllerSnapshot() => _controllerSnapshot;

        public string GetBatteryLevel()
        {
            if (!_isConnected || _controller == null) return "Unknown";
            // NOTE: must only be called from the SDL thread — callers must use Dispatcher
            SDLJoystick* joy = SDL.GameControllerGetJoystick(_controller);
            if (joy == null) return "Unknown";
            SDLJoystickPowerLevel level = SDL.JoystickCurrentPowerLevel(joy);
            return level switch
            {
                SDLJoystickPowerLevel.Empty  => "Empty",
                SDLJoystickPowerLevel.Low    => "Low",
                SDLJoystickPowerLevel.Medium => "Mid",
                SDLJoystickPowerLevel.Full   => "Full",
                SDLJoystickPowerLevel.Wired  => "Wired",
                _                            => "Unknown"
            };
        }

        public void SetRumble(float left, float right)
        {
            if (!_isConnected || _controller == null) return;
            ushort l = (ushort)(Math.Clamp(left,  0f, 1f) * 65535);
            ushort r = (ushort)(Math.Clamp(right, 0f, 1f) * 65535);
            SDL.GameControllerRumble(_controller, l, r, 0xFFFFFFFF);
        }

        public void SetLED(byte r, byte g, byte b)
        {
            if (!_isConnected || _controller == null) return;
            if (SDL.GameControllerHasLED(_controller) == SDLBool.True)
                SDL.GameControllerSetLED(_controller, r, g, b);
        }

        /// <summary>
        /// Get current button states from the controller.
        /// Must be called from the SDL thread or via Dispatcher.
        /// </summary>
        public struct ButtonState
        {
            public bool APressed { get; set; }
            public bool BPressed { get; set; }
            public bool XPressed { get; set; }
            public bool YPressed { get; set; }
            public bool L1Pressed { get; set; }
            public bool R1Pressed { get; set; }
            public bool L2Pressed { get; set; }
            public bool R2Pressed { get; set; }
            public bool StartPressed { get; set; }
            public bool BackPressed { get; set; }
            public bool DPadUp { get; set; }
            public bool DPadDown { get; set; }
            public bool DPadLeft { get; set; }
            public bool DPadRight { get; set; }
            public bool L3Pressed { get; set; }
            public bool R3Pressed { get; set; }
        }

        /// <summary>
        /// Get current button states from the controller.
        /// Must be called from the SDL thread or via Dispatcher.
        /// </summary>
        public ButtonState GetButtonStates()
        {
            if (!_isConnected || _controller == null) return default;
            SDLJoystick* joy = SDL.GameControllerGetJoystick(_controller);
            if (joy == null) return default;

            // Pump events first to ensure button states are updated
            SDL.PumpEvents();

            // Read each button state via SDL_GameControllerGetButton
            // Triggers are read as axes (0-32767), threshold at > 0
            float lt = SDL.GameControllerGetAxis(_controller, SDLGameControllerAxis.Triggerleft) / 32767f;
            float rt = SDL.GameControllerGetAxis(_controller, SDLGameControllerAxis.Triggerright) / 32767f;

            return new ButtonState
            {
                APressed = SDL.GameControllerGetButton(_controller, SDLGameControllerButton.A) == 1,
                BPressed = SDL.GameControllerGetButton(_controller, SDLGameControllerButton.B) == 1,
                XPressed = SDL.GameControllerGetButton(_controller, SDLGameControllerButton.X) == 1,
                YPressed = SDL.GameControllerGetButton(_controller, SDLGameControllerButton.Y) == 1,
                L1Pressed = SDL.GameControllerGetButton(_controller, SDLGameControllerButton.Leftshoulder) == 1,
                R1Pressed = SDL.GameControllerGetButton(_controller, SDLGameControllerButton.Rightshoulder) == 1,
                L2Pressed = lt > 0.15f,
                R2Pressed = rt > 0.15f,
                StartPressed = SDL.GameControllerGetButton(_controller, SDLGameControllerButton.Start) == 1,
                BackPressed = SDL.GameControllerGetButton(_controller, SDLGameControllerButton.Back) == 1,
                DPadUp = SDL.GameControllerGetButton(_controller, SDLGameControllerButton.DpadUp) == 1,
                DPadDown = SDL.GameControllerGetButton(_controller, SDLGameControllerButton.DpadDown) == 1,
                DPadLeft = SDL.GameControllerGetButton(_controller, SDLGameControllerButton.DpadLeft) == 1,
                DPadRight = SDL.GameControllerGetButton(_controller, SDLGameControllerButton.DpadRight) == 1,
                L3Pressed = SDL.GameControllerGetButton(_controller, SDLGameControllerButton.Leftstick) == 1,
                R3Pressed = SDL.GameControllerGetButton(_controller, SDLGameControllerButton.Rightstick) == 1
            };
        }

        public void Dispose()
        {
            _disposed = true;
            _scanNow.Set(); // unblock the SDL thread so it can exit
            _deviceNotificationWindow?.Dispose();
            _deviceNotificationWindow = null;
        }

        // ── SDL thread body (ALL SDL calls live here) ──────────────────────

        private void SdlThreadLoop()
        {
            // ── Init SDL on this thread ──────────────────────────────────
            int result = SDL.Init(SDL.SDL_INIT_GAMECONTROLLER |
                                  SDL.SDL_INIT_JOYSTICK       |
                                  SDL.SDL_INIT_HAPTIC         |
                                  SDL.SDL_INIT_EVENTS);
            if (result < 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[ControllerService] SDL.Init failed: " + SDL.GetErrorS());
                return;
            }
            System.Diagnostics.Debug.WriteLine("[ControllerService] SDL2 initialized ✓"); // checkmark

            // ── Create Device Notification Window for instant hot-plug detection ────────────
            try
            {
                _deviceNotificationWindow = new Core.DeviceNotificationWindow(() =>
                {
                    System.Diagnostics.Debug.WriteLine("[ControllerService] WM_DEVICECHANGE received — requesting immediate scan");
                    _scanNow.Set();
                });
                System.Diagnostics.Debug.WriteLine("[ControllerService] Device notification window created");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ControllerService] Failed to create device notification window: {ex.Message}");
            }

            // ── Initial scan ────────────────────────────────────────────
            ScanForController();

            // ── Main loop: scan on request or every 3 s ─────────────────
            while (!_disposed)
            {
                // Wait up to 3000 ms for a scan request or disposal
                bool signalled = _scanNow.Wait(3000);
                if (_disposed) break;
                _scanNow.Reset();

                // Pump SDL events (safe — same thread as SDL.Init)
                SDL.PumpEvents();

                if (!_isConnected || _controller == null)
                    ScanForController();
                else
                    VerifyStillConnected();
            }

            // ── Cleanup on this thread ───────────────────────────────────
            if (_controller != null)
            {
                SDL.GameControllerRumble(_controller, 0, 0, 0);
                SDL.GameControllerClose(_controller);
                _controller = null;
            }
            _isConnected = false;
            SDL.Quit();
            System.Diagnostics.Debug.WriteLine("[ControllerService] SDL2 shut down");
        }

        private void ScanForController()
        {
            // Pump first so newly-plugged devices are visible
            SDL.PumpEvents();

            int numJoysticks = SDL.NumJoysticks();
            System.Diagnostics.Debug.WriteLine(
                $"[ControllerService] Scanning — {numJoysticks} joystick(s) found");

            // NOTE: Windows Device Change Event Handler (WM_DEVICECHANGE) for instant hot-plug detection
            // would require a hidden window and RegisterDeviceNotification. 
            // Current implementation uses polling every 3 seconds via _scanNow.
            // Future improvement: Implement proper WM_DEVICECHANGE handling in HW-001.

            for (int i = 0; i < numJoysticks; i++)
            {
                if (SDL.IsGameController(i) != SDLBool.True) continue;

                SDLGameController* ctrl = SDL.GameControllerOpen(i);
                if (ctrl == null) continue;

                string name = SDL.GameControllerNameS(ctrl) ?? "Unknown Gamepad";
                _controller   = ctrl;
                ControllerName = name;
                DetermineControllerType(name);
                _isConnected   = true;

                // Update the read-only snapshot for main-thread access (lock-free, single-writer)
                _controllerSnapshot = ctrl;

                System.Diagnostics.Debug.WriteLine(
                    $"[ControllerService] Connected: {name}");
                return;
            }

            // Nothing found — clear controller and snapshot
            _isConnected   = false;
            ControllerName = "No Controller";
            ControllerType = "Unknown";
            _controllerSnapshot = null;
        }

        private void VerifyStillConnected()
        {
            if (_controller == null) { _isConnected = false; return; }

            // GameControllerGetAttached returns SDL_FALSE when the device is gone
            if (SDL.GameControllerGetAttached(_controller) != SDLBool.True)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[ControllerService] {ControllerName} disconnected");
                SDL.GameControllerClose(_controller);
                _controller    = null;
                _isConnected   = false;
                ControllerName = "No Controller";
                ControllerType = "Unknown";

                // Update snapshot for main-thread access
                _controllerSnapshot = null;
            }
        }

        private void DetermineControllerType(string name)
        {
            string lower = name.ToLowerInvariant();
            if      (lower.Contains("dualsense") || lower.Contains("ps5"))       ControllerType = "PS5";
            else if (lower.Contains("dualshock") || lower.Contains("ps4"))       ControllerType = "PS4";
            else if (lower.Contains("nintendo")  || lower.Contains("switch") ||  lower.Contains("pro controller")) ControllerType = "Switch";
            else                                                                  ControllerType = "Xbox";
        }
    }
}