using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RagnaController.Core;

namespace RagnaController.Tests
{
    /// <summary>
    /// Deterministischer Test-Double für INativeMethods.
    /// Ersetzt Moq für out-Parameter und statische Abhängigkeiten.
    /// </summary>
    public sealed class TestNativeMethods : RagnaController.Core.INativeMethods
    {
        // Window & Process
        public IntPtr GetForegroundWindow() => _foregroundWindow ?? IntPtr.Zero;
        public uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId)
        {
            lpdwProcessId = _windowProcessIdMap.TryGetValue(hWnd, out var pid) ? pid : 0u;
            return lpdwProcessId != 0 ? 1u : 0u; // thread ID != 0 means success
        }
        public bool GetClientRect(IntPtr hWnd, out RECT lpRect)
        {
            if (_clientRectMap.TryGetValue(hWnd, out var rect))
            {
                lpRect = rect;
                return true;
            }
            lpRect = default;
            return false;
        }
        public bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint)
        {
            if (_clientToScreenOffsetMap.TryGetValue(hWnd, out var offset))
            {
                lpPoint.X += offset.X;
                lpPoint.Y += offset.Y;
                return true;
            }
            return false;
        }
        public uint GetDpiForWindow(IntPtr hwnd) => _dpiMap.TryGetValue(hwnd, out var dpi) ? dpi : 96u;

        // Input
        public uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize) => nInputs;
        public uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize) => nInputs;
        public bool GetCursorPos(out POINT lpPoint)
        {
            lpPoint = _cursorPos;
            return true;
        }
        public bool SetCursorPos(int x, int y)
        {
            _cursorPos = new POINT { X = x, Y = y };
            return true;
        }
        public IntPtr GetCursor() => IntPtr.Zero;

        // Timer
        public uint timeBeginPeriod(uint uPeriod) => 0;
        public uint timeEndPeriod(uint uPeriod) => 0;

        // Window Styles
        public IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) => IntPtr.Zero;
        public IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) => IntPtr.Zero;

        // HID & SetupAPI
        public void HidD_GetHidGuid(out Guid hidGuid) => hidGuid = Guid.Empty;
        public IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags) => IntPtr.Zero;
        public bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData) => false;
        public bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, uint DeviceInterfaceDetailDataSize, out uint RequiredSize, IntPtr DeviceInfoData)
        {
            RequiredSize = 0;
            return false;
        }
        public bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet) => true;
        public IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile) => IntPtr.Zero;
        public bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped)
        {
            lpNumberOfBytesWritten = 0;
            return false;
        }
        public bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped)
        {
            lpNumberOfBytesRead = 0;
            return false;
        }
        public bool CloseHandle(IntPtr hObject) => true;

        // Device Notification
        public IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr notificationFilter, uint flags) => IntPtr.Zero;
        public bool UnregisterDeviceNotification(IntPtr handle) => true;

        // Window Class
        public IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam) => IntPtr.Zero;
        public bool DestroyWindow(IntPtr hWnd) => true;
        public IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam) => IntPtr.Zero;
        public ushort RegisterClassEx(ref WNDCLASSEX lpwcx) => 0;
        public IntPtr GetModuleHandle(string? lpModuleName) => IntPtr.Zero;

        // Constants
        public int InputSize => System.Runtime.InteropServices.Marshal.SizeOf<INPUT>();
        public uint INPUT_MOUSE => 0;
        public uint INPUT_KEYBOARD => 1;
        public uint MOUSEEVENTF_MOVE => 0x0001;
        public uint MOUSEEVENTF_LEFTDOWN => 0x0002;
        public uint MOUSEEVENTF_LEFTUP => 0x0004;
        public uint MOUSEEVENTF_RIGHTDOWN => 0x0008;
        public uint MOUSEEVENTF_RIGHTUP => 0x0010;
        public uint MOUSEEVENTF_MOVE_NOCOALESCE => 0x2000;
        public uint MOUSEEVENTF_ABSOLUTE => 0x8000;
        public uint MOUSEEVENTF_WHEEL => 0x0800;
        public uint KEYEVENTF_KEYDOWN => 0x0000;
        public uint KEYEVENTF_KEYUP => 0x0002;
        public int GWL_EXSTYLE => -20;
        public int WS_EX_LAYERED => 0x00080000;
        public int WS_EX_TRANSPARENT => 0x00000020;
        public uint DIGCF_PRESENT => 0x00000002;
        public uint DIGCF_DEVICEINTERFACE => 0x00000010;
        public uint GENERIC_READ => 0x80000000;
        public uint GENERIC_WRITE => 0x40000000;
        public uint FILE_SHARE_READ => 0x00000001;
        public uint FILE_SHARE_WRITE => 0x00000002;
        public uint OPEN_EXISTING => 3;
        public uint FILE_ATTRIBUTE_NORMAL => 0x00000080;
        public uint WM_DEVICECHANGE => 0x0219;
        public uint DBT_DEVICEARRIVAL => 0x8000;
        public uint DBT_DEVICEREMOVECOMPLETE => 0x8004;
        public uint DBT_DEVTYP_DEVICEINTERFACE => 0x00000005;
        public uint DEVICE_NOTIFY_WINDOW_HANDLE => 0x00000000;
        public uint DEVICE_NOTIFY_ALL_INTERFACE_CLASSES => 0x00000004;
        public uint CS_HREDRAW => 0x0002;
        public uint CS_VREDRAW => 0x0001;
        public uint WS_OVERLAPPED => 0x00000000;
        public int CW_USEDEFAULT => unchecked((int)0x80000000);
        public Guid GUID_DEVINTERFACE_HID => new Guid(0x4D1E55B2, 0xF16F, 0x11CF, 0x88, 0xCB, 0x00, 0x11, 0x11, 0x00, 0x00, 0x30);

        // Multi-Client Window Enumeration (TASK-1602)
        public bool EnumWindows(EnumWindowsProcDelegate lpEnumFunc, IntPtr lParam)
        {
            foreach (var hwnd in _aliveWindows)
            {
                if (!lpEnumFunc(hwnd, IntPtr.Zero))
                    return false;
            }
            return true;
        }
        public bool IsWindow(IntPtr hWnd) => _aliveWindows.Contains(hWnd);
        public bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam) => true;

        // PostMessage constants (TASK-1602)
        public uint WM_KEYDOWN => 0x0100;
        public uint WM_KEYUP => 0x0101;
        public uint WM_CHAR => 0x0102;
        public uint WM_LBUTTONDOWN => 0x0201;
        public uint WM_LBUTTONUP => 0x0202;
        public uint WM_RBUTTONDOWN => 0x0204;
        public uint WM_RBUTTONUP => 0x0205;
        public uint WM_MOUSEWHEEL => 0x020A;

        // Test control
        private IntPtr? _foregroundWindow;
        private readonly Dictionary<IntPtr, uint> _windowProcessIdMap = new();
        private readonly Dictionary<IntPtr, RECT> _clientRectMap = new();
        private readonly Dictionary<IntPtr, POINT> _clientToScreenOffsetMap = new();
        private readonly Dictionary<IntPtr, uint> _dpiMap = new();
        private readonly HashSet<IntPtr> _aliveWindows = new();
        private POINT _cursorPos = new POINT { X = 0, Y = 0 };

        public void SetForegroundWindow(IntPtr hwnd) => _foregroundWindow = hwnd;
        public void SetWindowProcessId(IntPtr hwnd, uint pid) => _windowProcessIdMap[hwnd] = pid;
        public void SetClientRect(IntPtr hwnd, RECT rect) => _clientRectMap[hwnd] = rect;
        public void SetClientToScreenOffset(IntPtr hwnd, POINT offset) => _clientToScreenOffsetMap[hwnd] = offset;
        public void SetDpiForWindow(IntPtr hwnd, uint dpi) => _dpiMap[hwnd] = dpi;
        public void SetAliveWindow(IntPtr hwnd, bool alive = true)
        {
            if (alive) _aliveWindows.Add(hwnd); else _aliveWindows.Remove(hwnd);
        }
        public void Clear() { _foregroundWindow = null; _windowProcessIdMap.Clear(); _clientRectMap.Clear(); _clientToScreenOffsetMap.Clear(); _dpiMap.Clear(); _aliveWindows.Clear(); }
    }
}