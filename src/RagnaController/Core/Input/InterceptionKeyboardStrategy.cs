using System;
using System.Runtime.InteropServices;
using RagnaController.Core;

namespace RagnaController.Core
{
    /// <summary>
    /// GEX-003: Interception Keyboard Strategy
    /// Kernel-mode keyboard emulation via Interception driver.
    /// Must match SendInputKeyboardStrategy behavior EXACTLY for seamless failover.
    /// Uses shared InterceptionNative from SendInputMouseStrategy.cs
    /// </summary>
    /// <summary>
    /// Input processing and routing component.
    /// </summary>
    public sealed class InterceptionKeyboardStrategy : IKeyboardEmulationStrategy
    {
        private readonly IntPtr _context;
        private int _keyboardDeviceId = -1;
        private bool _disposed;

        public InterceptionKeyboardStrategy()
        {
            _context = InterceptionNative.interception_create_context();
            _keyboardDeviceId = FindKeyboardDevice();
        }

        private int FindKeyboardDevice()
        {
            if (_context == IntPtr.Zero) return -1;

            for (int i = 0; i < 20; i++)
            {
                int device = InterceptionNative.interception_get_device(_context, i);
                if (device != 0)
                {
                    // Check if it's a keyboard device (INTERCEPTION_KEYBOARD flag = 0x01)
                    if ((device & 0x01) != 0)
                    {
                        return device;
                    }
                }
            }
            return -1;
        }

        public bool IsAvailable => _context != IntPtr.Zero && _keyboardDeviceId >= 0;
        public string DisplayName => "Interception (Kernel-Mode)";

        public void KeyDown(ushort keyCode)
        {
            if (!IsAvailable) return;

            var stroke = new InterceptionStroke
            {
                type = (uint)InterceptionStrokeType.KeyDown,
                device = _keyboardDeviceId,
                key_code = keyCode,
                scan_code = 0,
                modifiers = 0
            };
            InterceptionNative.interception_send(_context, _keyboardDeviceId, ref stroke, 1);
        }

        public void KeyUp(ushort keyCode)
        {
            if (!IsAvailable) return;

            var stroke = new InterceptionStroke
            {
                type = (uint)InterceptionStrokeType.KeyUp,
                device = _keyboardDeviceId,
                key_code = keyCode,
                scan_code = 0,
                modifiers = 0
            };
            InterceptionNative.interception_send(_context, _keyboardDeviceId, ref stroke, 1);
        }

        public void TapKey(ushort keyCode, int holdMs = 0)
        {
            if (!IsAvailable) return;

            KeyDown(keyCode);
            if (holdMs > 0) System.Threading.Thread.Sleep(holdMs);
            else System.Threading.Thread.Sleep(JitterService.ClickHold() / 3);
            KeyUp(keyCode);
        }

        public void TapKeyWithModifier(ushort modifier, ushort keyCode, int holdMs = 0)
        {
            if (!IsAvailable) return;

            KeyDown(modifier);
            System.Threading.Thread.Sleep(10);
            KeyDown(keyCode);
            System.Threading.Thread.Sleep(holdMs > 0 ? holdMs : JitterService.ClickHold() / 3);
            KeyUp(keyCode);
            System.Threading.Thread.Sleep(10);
            KeyUp(modifier);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_context != IntPtr.Zero)
            {
                InterceptionNative.interception_destroy_context(_context);
            }
        }
    }
}