using System;
using System.Runtime.InteropServices;
using RagnaController.Core;
using static RagnaController.Core.NativeMethods;

namespace RagnaController.Core
{
    /// <summary>
    /// GEX-003: SendInput Keyboard Strategy
    /// Standard keyboard emulation via Win32 SendInput API.
    /// Reference implementation — InterceptionKeyboardStrategy must match behavior exactly.
    /// </summary>
    public sealed class SendInputKeyboardStrategy : IKeyboardEmulationStrategy
    {
        private static readonly int InputSize = Marshal.SizeOf<INPUT>();

        public bool IsAvailable => true;
        public string DisplayName => "SendInput (Standard)";

        public void Dispose() { }

        public void KeyDown(ushort keyCode)
        {
            INPUT input = new INPUT();
            input.type = INPUT_KEYBOARD;
            input.Data.ki.wVk = keyCode;
            input.Data.ki.dwFlags = 0; // KEYEVENTF_KEYDOWN

            SendInput(1, new INPUT[] { input }, InputSize);
        }

        public void KeyUp(ushort keyCode)
        {
            INPUT input = new INPUT();
            input.type = INPUT_KEYBOARD;
            input.Data.ki.wVk = keyCode;
            input.Data.ki.dwFlags = KEYEVENTF_KEYUP;

            SendInput(1, new INPUT[] { input }, InputSize);
        }

        public void TapKey(ushort keyCode, int holdMs = 0)
        {
            KeyDown(keyCode);
            if (holdMs > 0) System.Threading.Thread.Sleep(holdMs);
            else System.Threading.Thread.Sleep(JitterService.ClickHold() / 3);
            KeyUp(keyCode);
        }

        public void TapKeyWithModifier(ushort modifier, ushort keyCode, int holdMs = 0)
        {
            KeyDown(modifier);
            System.Threading.Thread.Sleep(10);
            KeyDown(keyCode);
            System.Threading.Thread.Sleep(holdMs > 0 ? holdMs : JitterService.ClickHold() / 3);
            KeyUp(keyCode);
            System.Threading.Thread.Sleep(10);
            KeyUp(modifier);
        }
    }
}