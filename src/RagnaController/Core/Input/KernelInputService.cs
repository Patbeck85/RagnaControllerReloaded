using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace RagnaController.Core
{
    /// <summary>
    /// Kernel-Level Input Service using Interception Driver for Anti-Cheat Bypass
    /// Wraps the interception.dll with P/Invoke calls to intercept keyboard/mouse input at kernel level
    /// </summary>
    public static class KernelInputService
    {
        // Interception API constants
        private const int INTERCEPTION_CREATE_CONTEXT = 0;
        private const int INTERCEPTION_SEND = 1;
        private const int INTERCEPTION_GET_DEVICE = 2;
        private const int INTERCEPTION_DESTROY_CONTEXT = 3;

        // Device IDs for common input devices
        private const int DEVICE_KEYBOARD = 0x00000001;
        private const int DEVICE_MOUSE = 0x00000002;

        // Stroke types
        private const int STROKE_KEYDOWN = 1;
        private const int STROKE_KEYUP = 2;
        private const int STROKE_MOVEDOWN = 3;
        private const int STROKE_MOVEUP = 4;
        private const int STROKE_MOUSE_DOWN = 5;
        private const int STROKE_MOUSE_UP = 6;

        [DllImport(@"AntiCheat\interception.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "interception_create_context")]
        public static extern IntPtr interception_create_context();

        [DllImport(@"AntiCheat\interception.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "interception_send")]
        private static extern void interception_send(IntPtr context, int device, ref InterceptionStroke stroke, uint nstroke);

        [DllImport(@"AntiCheat\interception.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "interception_get_device")]
        private static extern int interception_get_device(IntPtr context, int deviceIndex);

        [DllImport(@"AntiCheat\interception.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "interception_destroy_context")]
        private static extern void interception_destroy_context(IntPtr context);

        private static IntPtr _context = IntPtr.Zero;
        private static readonly object _lock = new object();

        /// <summary>
        /// Initializes the interception context
        /// </summary>
        public static bool Initialize()
        {
            try
            {
                lock (_lock)
                {
                    if (_context != IntPtr.Zero)
                        return true;

                    _context = interception_create_context();
                    return _context != IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KernelInputService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Shuts down the interception context
        /// </summary>
        public static void Shutdown()
        {
            lock (_lock)
            {
                if (_context != IntPtr.Zero)
                {
                    interception_destroy_context(_context);
                    _context = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Intercepts and forwards a stroke to the application
        /// </summary>
        public static void InterceptStroke(ref InterceptionStroke stroke, uint nstroke)
        {
            if (_context == IntPtr.Zero)
                return;

            try
            {
                interception_send(_context, -1, ref stroke, nstroke);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KernelInputService] Intercept error: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the device ID for a specific device index
        /// </summary>
        public static int GetDeviceId(int deviceIndex)
        {
            if (_context == IntPtr.Zero)
                return -1;

            try
            {
                return interception_get_device(_context, deviceIndex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KernelInputService] GetDeviceId error: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// InterceptionStroke structure matching the C# struct from interception.h
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct InterceptionStroke
        {
            public uint type;
            public int device;
            public int key_code;
            public int scan_code;
            public int x;
            public int y;
            public int wheel;
            public int buttons;
            public int modifiers;
            public int timestamp;
        }
    }
}
