using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace RagnaController.Core
{
    /// <summary>
    /// Hidden window that receives WM_DEVICECHANGE messages for instant controller hot-plug detection.
    /// Runs on the SDL thread to avoid cross-thread issues.
    /// </summary>
    internal sealed class DeviceNotificationWindow : IDisposable
    {
        private IntPtr _hwnd;
        private IntPtr _deviceNotificationHandle;
        private readonly Action _onDeviceChange;
        private readonly Thread _ownerThread;
        private bool _disposed;
        private NativeMethods.WndProcDelegate? _wndProcDelegate;
        private IntPtr _wndProcPtr = IntPtr.Zero;

        public DeviceNotificationWindow(Action onDeviceChange)
        {
            _onDeviceChange = onDeviceChange ?? throw new ArgumentNullException(nameof(onDeviceChange));
            _ownerThread = Thread.CurrentThread;

            // Create delegate and keep it alive
            _wndProcDelegate = WndProc;
            _wndProcPtr = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);

            // Create hidden window
            var wc = new NativeMethods.WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<NativeMethods.WNDCLASSEX>(),
                style = NativeMethods.CS_HREDRAW | NativeMethods.CS_VREDRAW,
                lpfnWndProc = _wndProcPtr,
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = NativeMethods.GetModuleHandle(null),
                hIcon = IntPtr.Zero,
                hCursor = IntPtr.Zero,
                hbrBackground = IntPtr.Zero,
                lpszMenuName = null!,
                lpszClassName = "RagnaController_DeviceNotify",
                hIconSm = IntPtr.Zero
            };

            ushort classAtom = NativeMethods.RegisterClassEx(ref wc);
            if (classAtom == 0)
            {
                int error = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(error, "Failed to register device notification window class");
            }

            _hwnd = NativeMethods.CreateWindowEx(
                0,
                "RagnaController_DeviceNotify",
                "RagnaController Device Notification",
                NativeMethods.WS_OVERLAPPED,
                NativeMethods.CW_USEDEFAULT, NativeMethods.CW_USEDEFAULT,
                0, 0,
                IntPtr.Zero, IntPtr.Zero,
                NativeMethods.GetModuleHandle(null),
                IntPtr.Zero);

            if (_hwnd == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(error, "Failed to create device notification window");
            }

            // Register for device interface change notifications (HID devices)
            var filter = new NativeMethods.DEV_BROADCAST_DEVICEINTERFACE
            {
                dbcc_size = Marshal.SizeOf<NativeMethods.DEV_BROADCAST_DEVICEINTERFACE>(),
                dbcc_devicetype = (int)NativeMethods.DBT_DEVTYP_DEVICEINTERFACE,
                dbcc_reserved = 0,
                dbcc_classguid = NativeMethods.GUID_DEVINTERFACE_HID,
                dbcc_name = ""
            };

            IntPtr filterPtr = Marshal.AllocHGlobal(Marshal.SizeOf(filter));
            try
            {
                Marshal.StructureToPtr(filter, filterPtr, false);
                _deviceNotificationHandle = NativeMethods.RegisterDeviceNotification(
                    _hwnd,
                    filterPtr,
                    NativeMethods.DEVICE_NOTIFY_WINDOW_HANDLE | NativeMethods.DEVICE_NOTIFY_ALL_INTERFACE_CLASSES);
            }
            finally
            {
                Marshal.FreeHGlobal(filterPtr);
            }

            if (_deviceNotificationHandle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"[DeviceNotificationWindow] RegisterDeviceNotification failed: {error}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DeviceNotificationWindow] Device notification registered successfully");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == NativeMethods.WM_DEVICECHANGE)
            {
                uint eventType = (uint)wParam;
                if (eventType == NativeMethods.DBT_DEVICEARRIVAL || eventType == NativeMethods.DBT_DEVICEREMOVECOMPLETE)
                {
                    // Verify it's a device interface change (HID)
                    if (lParam != IntPtr.Zero)
                    {
                        var hdr = Marshal.PtrToStructure<NativeMethods.DEV_BROADCAST_DEVICEINTERFACE>(lParam);
                        if (hdr.dbcc_devicetype == (int)NativeMethods.DBT_DEVTYP_DEVICEINTERFACE)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DeviceNotificationWindow] Device change: {eventType:X}");
                            _onDeviceChange?.Invoke();
                        }
                    }
                }
            }

            return NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_deviceNotificationHandle != IntPtr.Zero)
            {
                NativeMethods.UnregisterDeviceNotification(_deviceNotificationHandle);
                _deviceNotificationHandle = IntPtr.Zero;
            }

            if (_hwnd != IntPtr.Zero)
            {
                NativeMethods.DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }

            System.Diagnostics.Debug.WriteLine("[DeviceNotificationWindow] Disposed");
        }
    }
}