using System;
using System.Runtime.InteropServices;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// Standard-Maus-Emulation über die Win32 SendInput API.
    /// </summary>
    public sealed class SendInputMouseStrategy : IMouseEmulationStrategy
    {
        // Die Größe der INPUT-Struktur für die Win32 API
        private static readonly int InputSize = Marshal.SizeOf<NativeMethods.INPUT>();
        
        // Konstante für MOUSEEVENTF_MOVE_NOCOALESCE (0x2000)
        // Prevents Windows from coalescing multiple small mouse movements.
        // Wichtig für flüssige Kreise/Bezier-Kurven.
        private const uint MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000;

        public bool IsAvailable => true;
        public string DisplayName => "SendInput (Standard)";

        /// <summary>
        /// Bewegt die Maus relativ zur aktuellen Position.
        /// </summary>
        public void MoveRelative(int dx, int dy)
        {
            if (dx == 0 && dy == 0) return;

            NativeMethods.INPUT input = new NativeMethods.INPUT();
            input.type = NativeMethods.INPUT_MOUSE;
            input.Data.mi.dx = dx;
            input.Data.mi.dy = dy;
            input.Data.mi.dwFlags = NativeMethods.MOUSEEVENTF_MOVE | MOUSEEVENTF_MOVE_NOCOALESCE;
            
            // Behebt CS1503: Wir übergeben ein Array mit einem Element
            NativeMethods.SendInput(1, new NativeMethods.INPUT[] { input }, InputSize);
        }

        /// <summary>
        /// Setzt die Maus auf eine absolute Bildschirmkoordinate.
        /// </summary>
        public void MoveAbsolute(int x, int y)
        {
            NativeMethods.SetCursorPos(x, y);
        }

        public void LeftDown()  => SendMouse(NativeMethods.MOUSEEVENTF_LEFTDOWN);
        public void LeftUp()    => SendMouse(NativeMethods.MOUSEEVENTF_LEFTUP);
        public void RightDown() => SendMouse(NativeMethods.MOUSEEVENTF_RIGHTDOWN);
        public void RightUp()   => SendMouse(NativeMethods.MOUSEEVENTF_RIGHTUP);

        /// <summary>
        /// Hilfsmethode zum Senden von Maustasten-Events.
        /// </summary>
        private void SendMouse(uint flags)
        {
            NativeMethods.INPUT input = new NativeMethods.INPUT();
            input.type = NativeMethods.INPUT_MOUSE;
            input.Data.mi.dwFlags = flags;
            
            // Behebt CS1503
            NativeMethods.SendInput(1, new NativeMethods.INPUT[] { input }, InputSize);
        }
    }

    /// <summary>
    /// Kernel-Mode Maus-Emulation über den Interception-Treiber.
    /// Bypasst LLMHF_INJECTED Flag für Anti-Cheat-Kompatibilität.
    /// </summary>
    public sealed class InterceptionMouseStrategy : IMouseEmulationStrategy
    {
        private readonly InterceptionContext _context;
        private int _mouseDeviceId = -1;
        private bool _disposed;

        public InterceptionMouseStrategy()
        {
            _context = new InterceptionContext();
            _mouseDeviceId = FindMouseDevice();
        }

        private int FindMouseDevice()
        {
            if (!_context.IsInitialized) return -1;

            for (int i = 0; i < 20; i++)
            {
                int device = _context.GetDeviceId(i);
                if (device != 0)
                {
                    // Check if it's a mouse device (INTERCEPTION_MOUSE flag = 0x10)
                    if ((device & 0x10) != 0)
                    {
                        return device;
                    }
                }
            }
            return -1;
        }

        public bool IsAvailable => _context.IsInitialized && _mouseDeviceId >= 0;

        public string DisplayName => "Interception (Kernel-Mode)";

        public void MoveRelative(int dx, int dy)
        {
            if (dx == 0 && dy == 0) return;
            if (!IsAvailable) return;

            var stroke = new InterceptionStroke
            {
                type = (uint)InterceptionStrokeType.MouseMove,
                device = _mouseDeviceId,
                x = dx,
                y = dy
            };
            _context.Send(ref stroke);
        }

        public void MoveAbsolute(int x, int y)
        {
            if (!IsAvailable) return;

            // For absolute movement, we need to calculate relative from current position
            // This is a limitation of Interception - it only supports relative mouse moves
            // Fallback to Win32 for absolute positioning
            NativeMethods.SetCursorPos(x, y);
        }

        public void LeftDown()
        {
            if (!IsAvailable) return;
            var stroke = new InterceptionStroke
            {
                type = (uint)InterceptionStrokeType.MouseButton,
                device = _mouseDeviceId,
                buttons = 0x0001 // Left button down
            };
            _context.Send(ref stroke);
        }

        public void LeftUp()
        {
            if (!IsAvailable) return;
            var stroke = new InterceptionStroke
            {
                type = (uint)InterceptionStrokeType.MouseButton,
                device = _mouseDeviceId,
                buttons = 0x0002 // Left button up
            };
            _context.Send(ref stroke);
        }

        public void RightDown()
        {
            if (!IsAvailable) return;
            var stroke = new InterceptionStroke
            {
                type = (uint)InterceptionStrokeType.MouseButton,
                device = _mouseDeviceId,
                buttons = 0x0004 // Right button down
            };
            _context.Send(ref stroke);
        }

        public void RightUp()
        {
            if (!IsAvailable) return;
            var stroke = new InterceptionStroke
            {
                type = (uint)InterceptionStrokeType.MouseButton,
                device = _mouseDeviceId,
                buttons = 0x0008 // Right button up
            };
            _context.Send(ref stroke);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _context?.Dispose();
        }
    }

    /// <summary>
    /// Interception context wrapper with proper P/Invoke signatures
    /// </summary>
    internal sealed class InterceptionContext : IDisposable
    {
        private IntPtr _context = IntPtr.Zero;
        private readonly object _lock = new();
        private bool _disposed;

        public bool IsInitialized => _context != IntPtr.Zero;

        public InterceptionContext()
        {
            Initialize();
        }

        private void Initialize()
        {
            lock (_lock)
            {
                if (_context != IntPtr.Zero) return;

                try
                {
                    _context = InterceptionNative.interception_create_context();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[InterceptionContext] Failed to initialize: {ex.Message}");
                    _context = IntPtr.Zero;
                }
            }
        }

        public void Send(ref InterceptionStroke stroke)
        {
            if (_context == IntPtr.Zero) return;

            try
            {
                InterceptionNative.interception_send(_context, stroke.device, ref stroke, 1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InterceptionContext] Send error: {ex.Message}");
            }
        }

        public int GetDeviceId(int deviceIndex)
        {
            if (_context == IntPtr.Zero) return -1;
            try
            {
                return InterceptionNative.interception_get_device(_context, deviceIndex);
            }
            catch
            {
                return -1;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (_lock)
            {
                if (_context != IntPtr.Zero)
                {
                    InterceptionNative.interception_destroy_context(_context);
                    _context = IntPtr.Zero;
                }
            }
        }
    }

    /// <summary>
    /// Native P/Invoke signatures for Interception driver
    /// </summary>
    internal static class InterceptionNative
    {
        private const string DllName = "AntiCheat\\interception.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "interception_create_context")]
        internal static extern IntPtr interception_create_context();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "interception_send")]
        internal static extern void interception_send(IntPtr context, int device, ref InterceptionStroke stroke, uint nstroke);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "interception_get_device")]
        internal static extern int interception_get_device(IntPtr context, int deviceIndex);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "interception_destroy_context")]
        internal static extern void interception_destroy_context(IntPtr context);
    }

    /// <summary>
    /// Interception stroke types matching interception.h
    /// </summary>
    internal enum InterceptionStrokeType : uint
    {
        KeyDown = 1,
        KeyUp = 2,
        MouseMove = 3,
        MouseButton = 4
    }

    /// <summary>
    /// InterceptionStroke structure matching the C struct from interception.h
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct InterceptionStroke
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