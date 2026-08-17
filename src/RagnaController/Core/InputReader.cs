using System;
using Hexa.NET.SDL2;
using RagnaController.Controller;
using RagnaController.Models;

namespace RagnaController.Core
{
    public unsafe sealed class InputReader
    {
        private readonly ControllerService _ctrl;
        private GamepadButtonFlags _prevRawButtons = GamepadButtonFlags.None; // Previous frame buttons for edge detection

        public InputReader(ControllerService ctrl) => _ctrl = ctrl;

        public ParsedInput Read()
        {
            try
            {
                SDLGameController* pad = _ctrl.GetControllerSnapshot();
                if (pad == null)
                {
                    return ParsedInput.Disconnected;
                }

                // Note: SDL event pumping is handled by ControllerService._sdlThread.
                // SDL_GameControllerUpdate is not called here to avoid cross-thread SDL issues.

                // Read Axes (SDL uses -32768 to 32767)
                float lx = SDL.GameControllerGetAxis(pad, SDLGameControllerAxis.Leftx) / 32768f;
                float ly = SDL.GameControllerGetAxis(pad, SDLGameControllerAxis.Lefty) / -32768f; // Invert Y so Up is positive
                float rx = SDL.GameControllerGetAxis(pad, SDLGameControllerAxis.Rightx) / 32768f;
                float ry = SDL.GameControllerGetAxis(pad, SDLGameControllerAxis.Righty) / -32768f;

                // Read Triggers (SDL uses 0 to 32767)
                float lt = SDL.GameControllerGetAxis(pad, SDLGameControllerAxis.Triggerleft) / 32767f;
                float rt = SDL.GameControllerGetAxis(pad, SDLGameControllerAxis.Triggerright) / 32767f;

                // Read Buttons
                bool btnA = SDL.GameControllerGetButton(pad, SDLGameControllerButton.A) == 1;
                bool btnB = SDL.GameControllerGetButton(pad, SDLGameControllerButton.B) == 1;
                bool btnX = SDL.GameControllerGetButton(pad, SDLGameControllerButton.X) == 1;
                bool btnY = SDL.GameControllerGetButton(pad, SDLGameControllerButton.Y) == 1;
                
                bool l1 = SDL.GameControllerGetButton(pad, SDLGameControllerButton.Leftshoulder) == 1;
                bool r1 = SDL.GameControllerGetButton(pad, SDLGameControllerButton.Rightshoulder) == 1;
                bool l3 = SDL.GameControllerGetButton(pad, SDLGameControllerButton.Leftstick) == 1;
                bool r3 = SDL.GameControllerGetButton(pad, SDLGameControllerButton.Rightstick) == 1;

                bool start = SDL.GameControllerGetButton(pad, SDLGameControllerButton.Start) == 1;
                bool back = SDL.GameControllerGetButton(pad, SDLGameControllerButton.Back) == 1;

                bool dpadUp = SDL.GameControllerGetButton(pad, SDLGameControllerButton.DpadUp) == 1;
                bool dpadDown = SDL.GameControllerGetButton(pad, SDLGameControllerButton.DpadDown) == 1;
                bool dpadLeft = SDL.GameControllerGetButton(pad, SDLGameControllerButton.DpadLeft) == 1;
                bool dpadRight = SDL.GameControllerGetButton(pad, SDLGameControllerButton.DpadRight) == 1;

                // Create GamepadButtonFlags bitmask for JustPressed/JustReleased logic
                GamepadButtonFlags currentButtons = GamepadButtonFlags.None;
                if (btnA) currentButtons |= GamepadButtonFlags.BtnA;
                if (btnB) currentButtons |= GamepadButtonFlags.BtnB;
                if (btnX) currentButtons |= GamepadButtonFlags.BtnX;
                if (btnY) currentButtons |= GamepadButtonFlags.BtnY;
                if (l1) currentButtons |= GamepadButtonFlags.L1;
                if (r1) currentButtons |= GamepadButtonFlags.R1;
                if (l3) currentButtons |= GamepadButtonFlags.L3;
                if (r3) currentButtons |= GamepadButtonFlags.R3;
                if (start) currentButtons |= GamepadButtonFlags.Start;
                if (back) currentButtons |= GamepadButtonFlags.Back;
                if (dpadUp) currentButtons |= GamepadButtonFlags.DPadUp;
                if (dpadDown) currentButtons |= GamepadButtonFlags.DPadDown;
                if (dpadLeft) currentButtons |= GamepadButtonFlags.DPadLeft;
                if (dpadRight) currentButtons |= GamepadButtonFlags.DPadRight;

                // Return new readonly record struct directly - zero heap allocation!
                var input = new ParsedInput
                {
                    IsConnected = true,
                    LeftX = lx,
                    LeftY = ly,
                    RightX = rx,
                    RightY = ry,
                    L2 = lt > 0.15f,
                    R2 = rt > 0.15f,
                    TriggerLeft = lt,
                    TriggerRight = rt,
                    L1 = l1,
                    R1 = r1,
                    L3 = l3,
                    R3 = r3,
                    BtnA = btnA,
                    BtnB = btnB,
                    BtnX = btnX,
                    BtnY = btnY,
                    Start = start,
                    Back = back,
                    DPadUp = dpadUp,
                    DPadDown = dpadDown,
                    DPadLeft = dpadLeft,
                    DPadRight = dpadRight,
                    RawButtons = currentButtons,
                    PrevRawButtons = _prevRawButtons
                };

                _prevRawButtons = currentButtons;
                return input;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InputReader] Read error: {ex.Message}");
                return ParsedInput.Disconnected;
            }
        }
    }
}
