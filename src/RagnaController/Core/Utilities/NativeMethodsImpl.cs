using System;
using System.Runtime.InteropServices;
using RagnaController.Core;

namespace RagnaController.Core
{
    /// <summary>
    /// Standard-Implementierung from INativeMethods - ruft die echten Win32-APIs on.
    /// </summary>
    public class NativeMethodsImpl : INativeMethods
    {
        /// <summary>
        /// GetForegroundWindow method.
        /// <returns>Result of GetForegroundWindow.</returns>
        /// </summary>
        public IntPtr GetForegroundWindow() => NativeMethods.GetForegroundWindow();
        /// <summary>
        /// GetWindowThreadProcessId method.
        /// <param name="hWnd">hWnd parameter.</param>
        /// <param name="lpdwProcessId">lpdwProcessId parameter.</param>
        /// <returns>Result of GetWindowThreadProcessId.</returns>
        /// </summary>
        public uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId) => NativeMethods.GetWindowThreadProcessId(hWnd, out lpdwProcessId);
        /// <summary>
        /// GetClientRect method.
        /// <param name="hWnd">hWnd parameter.</param>
        /// <param name="lpRect">lpRect parameter.</param>
        /// <returns>Result of GetClientRect.</returns>
        /// </summary>
        public bool GetClientRect(IntPtr hWnd, out RECT lpRect) => NativeMethods.GetClientRect(hWnd, out lpRect);
        /// <summary>
        /// ClientToScreen method.
        /// <param name="hWnd">hWnd parameter.</param>
        /// <param name="lpPoint">lpPoint parameter.</param>
        /// <returns>Result of ClientToScreen.</returns>
        /// </summary>
        public bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint) => NativeMethods.ClientToScreen(hWnd, ref lpPoint);
        /// <summary>
        /// GetDpiForWindow method.
        /// <param name="hwnd">hwnd parameter.</param>
        /// <returns>Result of GetDpiForWindow.</returns>
        /// </summary>
        public uint GetDpiForWindow(IntPtr hwnd) => NativeMethods.GetDpiForWindow(hwnd);

        /// <summary>
        /// SendInput method.
        /// <param name="nInputs">nInputs parameter.</param>
        /// <param name="pInputs">pInputs parameter.</param>
        /// <param name="cbSize">cbSize parameter.</param>
        /// <returns>Result of SendInput.</returns>
        /// </summary>
        public uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize) => NativeMethods.SendInput(nInputs, pInputs, cbSize);
        /// <summary>
        /// SendInput method.
        /// <param name="nInputs">nInputs parameter.</param>
        /// <param name="pInputs">pInputs parameter.</param>
        /// <param name="cbSize">cbSize parameter.</param>
        /// <returns>Result of SendInput.</returns>
        /// </summary>
        public uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize) => NativeMethods.SendInput(nInputs, ref pInputs, cbSize);
        /// <summary>
        /// GetCursorPos method.
        /// <param name="lpPoint">lpPoint parameter.</param>
        /// <returns>Result of GetCursorPos.</returns>
        /// </summary>
        public bool GetCursorPos(out POINT lpPoint) => NativeMethods.GetCursorPos(out lpPoint);
        /// <summary>
        /// SetCursorPos method.
        /// <param name="x">x parameter.</param>
        /// <param name="y">y parameter.</param>
        /// <returns>Result of SetCursorPos.</returns>
        /// </summary>
        public bool SetCursorPos(int x, int y) => NativeMethods.SetCursorPos(x, y);
        /// <summary>
        /// GetCursor method.
        /// <returns>Result of GetCursor.</returns>
        /// </summary>
        public IntPtr GetCursor() => NativeMethods.GetCursor();

        /// <summary>
        /// timeBeginPeriod method.
        /// <param name="uPeriod">uPeriod parameter.</param>
        /// <returns>Result of timeBeginPeriod.</returns>
        /// </summary>
        public uint timeBeginPeriod(uint uPeriod) => NativeMethods.timeBeginPeriod(uPeriod);
        /// <summary>
        /// timeEndPeriod method.
        /// <param name="uPeriod">uPeriod parameter.</param>
        /// <returns>Result of timeEndPeriod.</returns>
        /// </summary>
        public uint timeEndPeriod(uint uPeriod) => NativeMethods.timeEndPeriod(uPeriod);

        /// <summary>
        /// GetWindowLongPtr method.
        /// <param name="hWnd">hWnd parameter.</param>
        /// <param name="nIndex">nIndex parameter.</param>
        /// <returns>Result of GetWindowLongPtr.</returns>
        /// </summary>
        public IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) => NativeMethods.GetWindowLongPtr(hWnd, nIndex);
        /// <summary>
        /// SetWindowLongPtr method.
        /// <param name="hWnd">hWnd parameter.</param>
        /// <param name="nIndex">nIndex parameter.</param>
        /// <param name="dwNewLong">dwNewLong parameter.</param>
        /// <returns>Result of SetWindowLongPtr.</returns>
        /// </summary>
        public IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) => NativeMethods.SetWindowLongPtr(hWnd, nIndex, dwNewLong);

        /// <summary>
        /// HidD_GetHidGuid method.
        /// <param name="hidGuid">hidGuid parameter.</param>
        /// </summary>
        public void HidD_GetHidGuid(out Guid hidGuid) => NativeMethods.HidD_GetHidGuid(out hidGuid);
        /// <summary>
        /// SetupDiGetClassDevs method.
        /// <param name="ClassGuid">ClassGuid parameter.</param>
        /// <param name="Enumerator">Enumerator parameter.</param>
        /// <param name="hwndParent">hwndParent parameter.</param>
        /// <param name="Flags">Flags parameter.</param>
        /// <returns>Result of SetupDiGetClassDevs.</returns>
        /// </summary>
        public IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags) => NativeMethods.SetupDiGetClassDevs(ref ClassGuid, Enumerator, hwndParent, Flags);
        /// <summary>
        /// SetupDiEnumDeviceInterfaces method.
        /// <param name="DeviceInfoSet">DeviceInfoSet parameter.</param>
        /// <param name="DeviceInfoData">DeviceInfoData parameter.</param>
        /// <param name="InterfaceClassGuid">InterfaceClassGuid parameter.</param>
        /// <param name="MemberIndex">MemberIndex parameter.</param>
        /// <param name="DeviceInterfaceData">DeviceInterfaceData parameter.</param>
        /// <returns>Result of SetupDiEnumDeviceInterfaces.</returns>
        /// </summary>
        /// <summary>
        /// SetupDiEnumDeviceInterfaces method.
        /// <param name="DeviceInfoSet">DeviceInfoSet parameter.</param>
        /// <param name="DeviceInfoData">DeviceInfoData parameter.</param>
        /// <param name="InterfaceClassGuid">InterfaceClassGuid parameter.</param>
        /// <param name="MemberIndex">MemberIndex parameter.</param>
        /// <param name="DeviceInterfaceData">DeviceInterfaceData parameter.</param>
        /// <returns>Result of SetupDiEnumDeviceInterfaces.</returns>
        /// </summary>
        public bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData) => NativeMethods.SetupDiEnumDeviceInterfaces(DeviceInfoSet, DeviceInfoData, ref InterfaceClassGuid, MemberIndex, ref DeviceInterfaceData);
        /// <summary>
        /// SetupDiGetDeviceInterfaceDetail method.
        /// <param name="DeviceInfoSet">DeviceInfoSet parameter.</param>
        /// <param name="DeviceInterfaceData">DeviceInterfaceData parameter.</param>
        /// <param name="DeviceInterfaceDetailData">DeviceInterfaceDetailData parameter.</param>
        /// <param name="DeviceInterfaceDetailDataSize">DeviceInterfaceDetailDataSize parameter.</param>
        /// <param name="RequiredSize">RequiredSize parameter.</param>
        /// <param name="DeviceInfoData">DeviceInfoData parameter.</param>
        /// <returns>Result of SetupDiGetDeviceInterfaceDetail.</returns>
        /// </summary>
        /// <summary>
        /// SetupDiGetDeviceInterfaceDetail method.
        /// <param name="DeviceInfoSet">DeviceInfoSet parameter.</param>
        /// <param name="DeviceInterfaceData">DeviceInterfaceData parameter.</param>
        /// <param name="DeviceInterfaceDetailData">DeviceInterfaceDetailData parameter.</param>
        /// <param name="DeviceInterfaceDetailDataSize">DeviceInterfaceDetailDataSize parameter.</param>
        /// <param name="RequiredSize">RequiredSize parameter.</param>
        /// <param name="DeviceInfoData">DeviceInfoData parameter.</param>
        /// <returns>Result of SetupDiGetDeviceInterfaceDetail.</returns>
        /// </summary>
        public bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, uint DeviceInterfaceDetailDataSize, out uint RequiredSize, IntPtr DeviceInfoData) => NativeMethods.SetupDiGetDeviceInterfaceDetail(DeviceInfoSet, ref DeviceInterfaceData, DeviceInterfaceDetailData, DeviceInterfaceDetailDataSize, out RequiredSize, DeviceInfoData);
        /// <summary>
        /// SetupDiDestroyDeviceInfoList method.
        /// <param name="DeviceInfoSet">DeviceInfoSet parameter.</param>
        /// <returns>Result of SetupDiDestroyDeviceInfoList.</returns>
        /// </summary>
        public bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet) => NativeMethods.SetupDiDestroyDeviceInfoList(DeviceInfoSet);
        /// <summary>
        /// CreateFile method.
        /// <param name="lpFileName">lpFileName parameter.</param>
        /// <param name="dwDesiredAccess">dwDesiredAccess parameter.</param>
        /// <param name="dwShareMode">dwShareMode parameter.</param>
        /// <param name="lpSecurityAttributes">lpSecurityAttributes parameter.</param>
        /// <param name="dwCreationDisposition">dwCreationDisposition parameter.</param>
        /// <param name="dwFlagsAndAttributes">dwFlagsAndAttributes parameter.</param>
        /// <param name="hTemplateFile">hTemplateFile parameter.</param>
        /// <returns>Result of CreateFile.</returns>
        /// </summary>
        /// <summary>
        /// CreateFile method.
        /// <param name="lpFileName">lpFileName parameter.</param>
        /// <param name="dwDesiredAccess">dwDesiredAccess parameter.</param>
        /// <param name="dwShareMode">dwShareMode parameter.</param>
        /// <param name="lpSecurityAttributes">lpSecurityAttributes parameter.</param>
        /// <param name="dwCreationDisposition">dwCreationDisposition parameter.</param>
        /// <param name="dwFlagsAndAttributes">dwFlagsAndAttributes parameter.</param>
        /// <param name="hTemplateFile">hTemplateFile parameter.</param>
        /// <returns>Result of CreateFile.</returns>
        /// </summary>
        /// <summary>
        /// CreateFile method.
        /// <param name="lpFileName">lpFileName parameter.</param>
        /// <param name="dwDesiredAccess">dwDesiredAccess parameter.</param>
        /// <param name="dwShareMode">dwShareMode parameter.</param>
        /// <param name="lpSecurityAttributes">lpSecurityAttributes parameter.</param>
        /// </summary>
        public IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile) => NativeMethods.CreateFile(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
        /// <summary>
        /// WriteFile method.
        /// <param name="hFile">hFile parameter.</param>
        /// <param name="lpBuffer">lpBuffer parameter.</param>
        /// <param name="nNumberOfBytesToWrite">nNumberOfBytesToWrite parameter.</param>
        /// <param name="lpNumberOfBytesWritten">lpNumberOfBytesWritten parameter.</param>
        /// <param name="lpOverlapped">lpOverlapped parameter.</param>
        /// <returns>Result of WriteFile.</returns>
    /// <param name="lpFileName">lpFileName parameter.</param>
    /// <param name="dwDesiredAccess">dwDesiredAccess parameter.</param>
        /// </summary>
        /// <summary>
        /// WriteFile method.
        /// <param name="hFile">hFile parameter.</param>
        /// <param name="lpBuffer">lpBuffer parameter.</param>
        /// <param name="nNumberOfBytesToWrite">nNumberOfBytesToWrite parameter.</param>
        /// <param name="lpNumberOfBytesWritten">lpNumberOfBytesWritten parameter.</param>
        /// <param name="lpOverlapped">lpOverlapped parameter.</param>
        /// <returns>Result of WriteFile.</returns>
        /// </summary>
        public bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped) => NativeMethods.WriteFile(hFile, lpBuffer, nNumberOfBytesToWrite, out lpNumberOfBytesWritten, lpOverlapped);
        /// <summary>
        /// ReadFile method.
        /// <param name="hFile">hFile parameter.</param>
        /// <param name="lpBuffer">lpBuffer parameter.</param>
        /// <param name="nNumberOfBytesToRead">nNumberOfBytesToRead parameter.</param>
        /// <param name="lpNumberOfBytesRead">lpNumberOfBytesRead parameter.</param>
        /// <param name="lpOverlapped">lpOverlapped parameter.</param>
        /// <returns>Result of ReadFile.</returns>
        /// </summary>
        /// <summary>
        /// ReadFile method.
        /// <param name="hFile">hFile parameter.</param>
        /// <param name="lpBuffer">lpBuffer parameter.</param>
        /// <param name="nNumberOfBytesToRead">nNumberOfBytesToRead parameter.</param>
        /// <param name="lpNumberOfBytesRead">lpNumberOfBytesRead parameter.</param>
        /// <param name="lpOverlapped">lpOverlapped parameter.</param>
        /// <returns>Result of ReadFile.</returns>
        /// </summary>
        public bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped) => NativeMethods.ReadFile(hFile, lpBuffer, nNumberOfBytesToRead, out lpNumberOfBytesRead, lpOverlapped);
        /// <summary>
        /// CloseHandle method.
        /// <param name="hObject">hObject parameter.</param>
        /// <returns>Result of CloseHandle.</returns>
        /// </summary>
        public bool CloseHandle(IntPtr hObject) => NativeMethods.CloseHandle(hObject);

        /// <summary>
        /// RegisterDeviceNotification method.
        /// <param name="hRecipient">hRecipient parameter.</param>
        /// <param name="notificationFilter">notificationFilter parameter.</param>
        /// <param name="flags">flags parameter.</param>
        /// <returns>Result of RegisterDeviceNotification.</returns>
        /// </summary>
        public IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr notificationFilter, uint flags) => NativeMethods.RegisterDeviceNotification(hRecipient, notificationFilter, flags);
        /// <summary>
        /// UnregisterDeviceNotification method.
        /// <param name="handle">handle parameter.</param>
        /// <returns>Result of UnregisterDeviceNotification.</returns>
        /// </summary>
        public bool UnregisterDeviceNotification(IntPtr handle) => NativeMethods.UnregisterDeviceNotification(handle);

        /// <summary>
        /// CreateWindowEx method.
        /// <param name="dwExStyle">dwExStyle parameter.</param>
        /// <param name="lpClassName">lpClassName parameter.</param>
        /// <param name="lpWindowName">lpWindowName parameter.</param>
        /// <param name="dwStyle">dwStyle parameter.</param>
        /// <param name="x">x parameter.</param>
        /// <param name="y">y parameter.</param>
        /// <param name="nWidth">nWidth parameter.</param>
        /// <param name="nHeight">nHeight parameter.</param>
        /// <param name="hWndParent">hWndParent parameter.</param>
        /// <param name="hMenu">hMenu parameter.</param>
        /// <param name="hInstance">hInstance parameter.</param>
        /// <param name="lpParam">lpParam parameter.</param>
        /// <returns>Result of CreateWindowEx.</returns>
        /// </summary>
        /// <summary>
        /// CreateWindowEx method.
        /// <param name="dwExStyle">dwExStyle parameter.</param>
        /// <param name="lpClassName">lpClassName parameter.</param>
        /// <param name="lpWindowName">lpWindowName parameter.</param>
        /// <param name="dwStyle">dwStyle parameter.</param>
        /// <param name="x">x parameter.</param>
        /// <param name="y">y parameter.</param>
        /// <param name="nWidth">nWidth parameter.</param>
        /// <param name="nHeight">nHeight parameter.</param>
        /// <param name="hWndParent">hWndParent parameter.</param>
        /// <param name="hMenu">hMenu parameter.</param>
        /// <param name="hInstance">hInstance parameter.</param>
        /// <param name="lpParam">lpParam parameter.</param>
        /// <returns>Result of CreateWindowEx.</returns>
        /// </summary>
        /// <summary>
        /// CreateWindowEx method.
        /// <param name="dwExStyle">dwExStyle parameter.</param>
        /// <param name="lpClassName">lpClassName parameter.</param>
        /// <param name="lpWindowName">lpWindowName parameter.</param>
        /// <param name="dwStyle">dwStyle parameter.</param>
        /// <param name="x">x parameter.</param>
        /// <param name="y">y parameter.</param>
        /// <param name="nWidth">nWidth parameter.</param>
        /// <param name="nHeight">nHeight parameter.</param>
        /// <param name="hWndParent">hWndParent parameter.</param>
        /// <param name="hMenu">hMenu parameter.</param>
        /// <param name="hInstance">hInstance parameter.</param>
        /// <param name="lpParam">lpParam parameter.</param>
        /// <returns>Result of CreateWindowEx.</returns>
        /// </summary>
        public IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam) => NativeMethods.CreateWindowEx(dwExStyle, lpClassName, lpWindowName, dwStyle, x, y, nWidth, nHeight, hWndParent, hMenu, hInstance, lpParam);
        /// <summary>
        /// DestroyWindow method.
        /// <param name="hWnd">hWnd parameter.</param>
        /// <returns>Result of DestroyWindow.</returns>
        /// </summary>
        /// </summary>
        public bool DestroyWindow(IntPtr hWnd) => NativeMethods.DestroyWindow(hWnd);
        /// <summary>
        /// DefWindowProc method.
        /// <param name="hWnd">hWnd parameter.</param>
        /// <param name="uMsg">uMsg parameter.</param>
        /// <param name="wParam">wParam parameter.</param>
        /// <param name="lParam">lParam parameter.</param>
        /// <returns>Result of DefWindowProc.</returns>
        /// </summary>
        public IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam) => NativeMethods.DefWindowProc(hWnd, uMsg, wParam, lParam);
        /// <summary>
        /// RegisterClassEx method.
        /// <param name="lpwcx">lpwcx parameter.</param>
        /// <returns>Result of RegisterClassEx.</returns>
        /// </summary>
        public ushort RegisterClassEx(ref WNDCLASSEX lpwcx) => NativeMethods.RegisterClassEx(ref lpwcx);
        /// <summary>
        /// GetModuleHandle method.
        /// <param name="lpModuleName">lpModuleName parameter.</param>
        /// <returns>Result of GetModuleHandle.</returns>
        /// </summary>
        public IntPtr GetModuleHandle(string? lpModuleName) => NativeMethods.GetModuleHandle(lpModuleName);

        public int InputSize => NativeMethods.InputSize;

        // Multi-Client Window Enumeration (TASK-1602)
        /// <summary>
        /// EnumWindows method.
        /// <param name="lpEnumFunc">lpEnumFunc parameter.</param>
        /// <param name="lParam">lParam parameter.</param>
        /// <returns>Result of EnumWindows.</returns>
        /// </summary>
        public bool EnumWindows(EnumWindowsProcDelegate lpEnumFunc, IntPtr lParam) => NativeMethods.EnumWindows(lpEnumFunc, lParam);
        /// <summary>
        /// IsWindow method.
        /// <param name="hWnd">hWnd parameter.</param>
        /// <returns>Result of IsWindow.</returns>
        /// </summary>
        public bool IsWindow(IntPtr hWnd) => NativeMethods.IsWindow(hWnd);
        /// <summary>
        /// PostMessage method.
        /// <param name="hWnd">hWnd parameter.</param>
        /// <param name="Msg">Msg parameter.</param>
        /// <param name="wParam">wParam parameter.</param>
        /// <param name="lParam">lParam parameter.</param>
        /// <returns>Result of PostMessage.</returns>
        /// </summary>
        public bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam) => NativeMethods.PostMessage(hWnd, Msg, wParam, lParam);
        public uint INPUT_MOUSE => NativeMethods.INPUT_MOUSE;
        public uint INPUT_KEYBOARD => NativeMethods.INPUT_KEYBOARD;
        public uint MOUSEEVENTF_MOVE => NativeMethods.MOUSEEVENTF_MOVE;
        public uint MOUSEEVENTF_LEFTDOWN => NativeMethods.MOUSEEVENTF_LEFTDOWN;
        public uint MOUSEEVENTF_LEFTUP => NativeMethods.MOUSEEVENTF_LEFTUP;
        public uint MOUSEEVENTF_RIGHTDOWN => NativeMethods.MOUSEEVENTF_RIGHTDOWN;
        public uint MOUSEEVENTF_RIGHTUP => NativeMethods.MOUSEEVENTF_RIGHTUP;
        public uint MOUSEEVENTF_MOVE_NOCOALESCE => NativeMethods.MOUSEEVENTF_MOVE_NOCOALESCE;
        public uint MOUSEEVENTF_ABSOLUTE => NativeMethods.MOUSEEVENTF_ABSOLUTE;
        public uint MOUSEEVENTF_WHEEL => NativeMethods.MOUSEEVENTF_WHEEL;
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

        // PostMessage constants (TASK-1602)
        public uint WM_KEYDOWN => NativeMethods.WM_KEYDOWN;
        public uint WM_KEYUP => NativeMethods.WM_KEYUP;
        public uint WM_CHAR => NativeMethods.WM_CHAR;
        public uint WM_LBUTTONDOWN => NativeMethods.WM_LBUTTONDOWN;
        public uint WM_LBUTTONUP => NativeMethods.WM_LBUTTONUP;
        public uint WM_RBUTTONDOWN => NativeMethods.WM_RBUTTONDOWN;
        public uint WM_RBUTTONUP => NativeMethods.WM_RBUTTONUP;
        public uint WM_MOUSEWHEEL => NativeMethods.WM_MOUSEWHEEL;
    }
}