using System;
using System.Runtime.InteropServices;

namespace RagnaController.Core
{
    /// <summary>
    /// Standard-Implementierung von INativeMethods - ruft die echten Win32-APIs auf.
    /// </summary>
    public class NativeMethodsImpl : INativeMethods
    {
        public IntPtr GetForegroundWindow() => NativeMethods.GetForegroundWindow();
        public uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId) => NativeMethods.GetWindowThreadProcessId(hWnd, out lpdwProcessId);
        public bool GetClientRect(IntPtr hWnd, out RECT lpRect) => NativeMethods.GetClientRect(hWnd, out lpRect);
        public bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint) => NativeMethods.ClientToScreen(hWnd, ref lpPoint);
        public uint GetDpiForWindow(IntPtr hwnd) => NativeMethods.GetDpiForWindow(hwnd);

        public uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize) => NativeMethods.SendInput(nInputs, pInputs, cbSize);
        public uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize) => NativeMethods.SendInput(nInputs, ref pInputs, cbSize);
        public bool GetCursorPos(out POINT lpPoint) => NativeMethods.GetCursorPos(out lpPoint);
        public bool SetCursorPos(int x, int y) => NativeMethods.SetCursorPos(x, y);
        public IntPtr GetCursor() => NativeMethods.GetCursor();

        public uint timeBeginPeriod(uint uPeriod) => NativeMethods.timeBeginPeriod(uPeriod);
        public uint timeEndPeriod(uint uPeriod) => NativeMethods.timeEndPeriod(uPeriod);

        public IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) => NativeMethods.GetWindowLongPtr(hWnd, nIndex);
        public IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) => NativeMethods.SetWindowLongPtr(hWnd, nIndex, dwNewLong);

        public void HidD_GetHidGuid(out Guid hidGuid) => NativeMethods.HidD_GetHidGuid(out hidGuid);
        public IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags) => NativeMethods.SetupDiGetClassDevs(ref ClassGuid, Enumerator, hwndParent, Flags);
        public bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData) => NativeMethods.SetupDiEnumDeviceInterfaces(DeviceInfoSet, DeviceInfoData, ref InterfaceClassGuid, MemberIndex, ref DeviceInterfaceData);
        public bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, uint DeviceInterfaceDetailDataSize, out uint RequiredSize, IntPtr DeviceInfoData) => NativeMethods.SetupDiGetDeviceInterfaceDetail(DeviceInfoSet, ref DeviceInterfaceData, DeviceInterfaceDetailData, DeviceInterfaceDetailDataSize, out RequiredSize, DeviceInfoData);
        public bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet) => NativeMethods.SetupDiDestroyDeviceInfoList(DeviceInfoSet);
        public IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile) => NativeMethods.CreateFile(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
        public bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped) => NativeMethods.WriteFile(hFile, lpBuffer, nNumberOfBytesToWrite, out lpNumberOfBytesWritten, lpOverlapped);
        public bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped) => NativeMethods.ReadFile(hFile, lpBuffer, nNumberOfBytesToRead, out lpNumberOfBytesRead, lpOverlapped);
        public bool CloseHandle(IntPtr hObject) => NativeMethods.CloseHandle(hObject);

        public IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr notificationFilter, uint flags) => NativeMethods.RegisterDeviceNotification(hRecipient, notificationFilter, flags);
        public bool UnregisterDeviceNotification(IntPtr handle) => NativeMethods.UnregisterDeviceNotification(handle);

        public IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam) => NativeMethods.CreateWindowEx(dwExStyle, lpClassName, lpWindowName, dwStyle, x, y, nWidth, nHeight, hWndParent, hMenu, hInstance, lpParam);
        public bool DestroyWindow(IntPtr hWnd) => NativeMethods.DestroyWindow(hWnd);
        public IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam) => NativeMethods.DefWindowProc(hWnd, uMsg, wParam, lParam);
        public ushort RegisterClassEx(ref WNDCLASSEX lpwcx) => NativeMethods.RegisterClassEx(ref lpwcx);
        public IntPtr GetModuleHandle(string? lpModuleName) => NativeMethods.GetModuleHandle(lpModuleName);

        public int InputSize => NativeMethods.InputSize;
        public uint INPUT_MOUSE => NativeMethods.INPUT_MOUSE;
        public uint INPUT_KEYBOARD => NativeMethods.INPUT_KEYBOARD;
        public uint MOUSEEVENTF_MOVE => NativeMethods.MOUSEEVENTF_MOVE;
        public uint MOUSEEVENTF_LEFTDOWN => NativeMethods.MOUSEEVENTF_LEFTDOWN;
        public uint MOUSEEVENTF_LEFTUP => NativeMethods.MOUSEEVENTF_LEFTUP;
        public uint MOUSEEVENTF_RIGHTDOWN => NativeMethods.MOUSEEVENTF_RIGHTDOWN;
        public uint MOUSEEVENTF_RIGHTUP => NativeMethods.MOUSEEVENTF_RIGHTUP;
        public uint MOUSEEVENTF_MOVE_NOCOALESCE => NativeMethods.MOUSEEVENTF_MOVE_NOCOALESCE;
        public uint KEYEVENTF_KEYDOWN => NativeMethods.KEYEVENTF_KEYDOWN;
        public uint KEYEVENTF_KEYUP => NativeMethods.KEYEVENTF_KEYUP;
        public int GWL_EXSTYLE => NativeMethods.GWL_EXSTYLE;
        public int WS_EX_LAYERED => NativeMethods.WS_EX_LAYERED;
        public int WS_EX_TRANSPARENT => NativeMethods.WS_EX_TRANSPARENT;
        public uint DIGCF_PRESENT => NativeMethods.DIGCF_PRESENT;
        public uint DIGCF_DEVICEINTERFACE => NativeMethods.DIGCF_DEVICEINTERFACE;
        public uint GENERIC_READ => NativeMethods.GENERIC_READ;
        public uint GENERIC_WRITE => NativeMethods.GENERIC_WRITE;
        public uint FILE_SHARE_READ => NativeMethods.FILE_SHARE_READ;
        public uint FILE_SHARE_WRITE => NativeMethods.FILE_SHARE_WRITE;
        public uint OPEN_EXISTING => NativeMethods.OPEN_EXISTING;
        public uint FILE_ATTRIBUTE_NORMAL => NativeMethods.FILE_ATTRIBUTE_NORMAL;
        public uint WM_DEVICECHANGE => NativeMethods.WM_DEVICECHANGE;
        public uint DBT_DEVICEARRIVAL => NativeMethods.DBT_DEVICEARRIVAL;
        public uint DBT_DEVICEREMOVECOMPLETE => NativeMethods.DBT_DEVICEREMOVECOMPLETE;
        public uint DBT_DEVTYP_DEVICEINTERFACE => NativeMethods.DBT_DEVTYP_DEVICEINTERFACE;
        public uint DEVICE_NOTIFY_WINDOW_HANDLE => NativeMethods.DEVICE_NOTIFY_WINDOW_HANDLE;
        public uint DEVICE_NOTIFY_ALL_INTERFACE_CLASSES => NativeMethods.DEVICE_NOTIFY_ALL_INTERFACE_CLASSES;
        public uint CS_HREDRAW => NativeMethods.CS_HREDRAW;
        public uint CS_VREDRAW => NativeMethods.CS_VREDRAW;
        public uint WS_OVERLAPPED => NativeMethods.WS_OVERLAPPED;
        public int CW_USEDEFAULT => NativeMethods.CW_USEDEFAULT;
        public Guid GUID_DEVINTERFACE_HID => NativeMethods.GUID_DEVINTERFACE_HID;
    }
}