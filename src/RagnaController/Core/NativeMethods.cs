using System;
using System.Runtime.InteropServices;

namespace RagnaController.Core
{
    internal static partial class NativeMethods
    {
        // --- Win32 Input API ---
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetCursorPos(out POINT lpPoint);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetCursor();

        [LibraryImport("user32.dll")]
        internal static partial IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [LibraryImport("user32.dll")]
        internal static partial int GetSystemMetrics(int nIndex);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        internal static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        internal static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        internal static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        // --- HID & SetupAPI (DualSense) ---
        [DllImport("hid.dll", SetLastError = true)]
        internal static extern void HidD_GetHidGuid(out Guid hidGuid);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, uint DeviceInterfaceDetailDataSize, out uint RequiredSize, IntPtr DeviceInfoData);

        [DllImport("setupapi.dll")]
        internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        // --- Multimedia Timer ---
        [DllImport("winmm.dll")]
        internal static extern uint timeBeginPeriod(uint uPeriod);
        [DllImport("winmm.dll")]
        internal static extern uint timeEndPeriod(uint uPeriod);

        // --- WM_DEVICECHANGE / Device Notification API ---
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr notificationFilter, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnregisterDeviceNotification(IntPtr handle);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpwcx);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetModuleHandle(string? lpModuleName);

        // WndProc delegate for window procedure
        internal delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        // --- Constants ---
        internal const uint INPUT_MOUSE = 0;
        internal const uint INPUT_KEYBOARD = 1;
        internal const uint MOUSEEVENTF_MOVE = 0x0001;
        internal const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        internal const uint MOUSEEVENTF_LEFTUP = 0x0004;
        internal const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        internal const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        internal const uint MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000;
        internal const uint KEYEVENTF_KEYDOWN = 0x0000;
        internal const uint KEYEVENTF_KEYUP = 0x0002;

        internal static readonly int InputSize = System.Runtime.InteropServices.Marshal.SizeOf<INPUT>();

        internal const int GWL_EXSTYLE = -20;
        internal const int WS_EX_LAYERED = 0x00080000;
        internal const int WS_EX_TRANSPARENT = 0x00000020;

        internal const uint DIGCF_PRESENT = 0x02;
        internal const uint DIGCF_DEVICEINTERFACE = 0x10;
        internal const uint GENERIC_READ = 0x80000000;
        internal const uint GENERIC_WRITE = 0x40000000;
        internal const uint FILE_SHARE_READ = 0x00000001;
        internal const uint FILE_SHARE_WRITE = 0x00000002;
        internal const uint OPEN_EXISTING = 3;
        internal const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        // WM_DEVICECHANGE constants
        internal const uint WM_DEVICECHANGE = 0x0219;
        internal const uint DBT_DEVICEARRIVAL = 0x8000;
        internal const uint DBT_DEVICEREMOVECOMPLETE = 0x8004;
        internal const uint DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;
        internal const uint DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
        internal const uint DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 0x00000004;

        // Window class/style constants
        internal const uint CS_HREDRAW = 0x0002;
        internal const uint CS_VREDRAW = 0x0001;
        internal const uint WS_OVERLAPPED = 0x00000000;
        internal const int CW_USEDEFAULT = unchecked((int)0x80000000);

        // GUID for HID devices
        internal static readonly Guid GUID_DEVINTERFACE_HID = new Guid(0x4D1E55B2, 0xF16F, 0x11CF, 0x88, 0xCB, 0x00, 0x11, 0x11, 0x00, 0x00, 0x30);

        // --- Structures ---
        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_DEVICE_INTERFACE_DATA { 
            public uint cbSize; 
            public Guid InterfaceClassGuid; 
            public uint Flags; 
            public IntPtr Reserved; 
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct DEV_BROADCAST_DEVICEINTERFACE
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string dbcc_name;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

        [StructLayout(LayoutKind.Explicit)]
        internal struct INPUTUNION { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }

        // Explizites Layout für 64-Bit-Alignment:
        // Windows erwartet Data-Feld an Offset 8 (4 Bytes uint + 4 Bytes Padding)
        [StructLayout(LayoutKind.Explicit)]
        internal struct INPUT
        {
            [FieldOffset(0)] public uint type;
            [FieldOffset(8)] public INPUTUNION Data; // ← 64-Bit Alignment!
        }

        // Bewegt den Cursor relativ zur aktuellen Position via SendInput (MOUSEEVENTF_MOVE)
        internal static void MoveCursorRelative(int dx, int dy)
        {
            var inp = new INPUT
            {
                type = 0, // INPUT_MOUSE
                Data = new INPUTUNION
                {
                    mi = new MOUSEINPUT { dx = dx, dy = dy, dwFlags = 0x0001 } // MOUSEEVENTF_MOVE
                }
            };
            SendInput(1, ref inp, System.Runtime.InteropServices.Marshal.SizeOf<INPUT>());
        }
    }
}