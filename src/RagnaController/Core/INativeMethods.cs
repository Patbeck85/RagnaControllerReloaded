using System;
using System.Runtime.InteropServices;

namespace RagnaController.Core
{
    /// <summary>
    /// Interface für Win32-API-Aufrufe - ermöglicht Mocking in Unit-Tests.
    /// </summary>
    public interface INativeMethods
    {
        // Window & Process
        IntPtr GetForegroundWindow();
        uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
        uint GetDpiForWindow(IntPtr hwnd);

        // Input
        uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);
        bool GetCursorPos(out POINT lpPoint);
        bool SetCursorPos(int x, int y);
        IntPtr GetCursor();

        // Timer
        uint timeBeginPeriod(uint uPeriod);
        uint timeEndPeriod(uint uPeriod);

        // Window Styles
        IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
        IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        // HID & SetupAPI
        void HidD_GetHidGuid(out Guid hidGuid);
        IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags);
        bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);
        bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, uint DeviceInterfaceDetailDataSize, out uint RequiredSize, IntPtr DeviceInfoData);
        bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);
        IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);
        bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);
        bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);
        bool CloseHandle(IntPtr hObject);

        // Device Notification
        IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr notificationFilter, uint flags);
        bool UnregisterDeviceNotification(IntPtr handle);

        // Window Class
        IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);
        bool DestroyWindow(IntPtr hWnd);
        IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
        ushort RegisterClassEx(ref WNDCLASSEX lpwcx);
        IntPtr GetModuleHandle(string? lpModuleName);

        // Constants
        int InputSize { get; }
        uint INPUT_MOUSE { get; }
        uint INPUT_KEYBOARD { get; }
        uint MOUSEEVENTF_MOVE { get; }
        uint MOUSEEVENTF_LEFTDOWN { get; }
        uint MOUSEEVENTF_LEFTUP { get; }
        uint MOUSEEVENTF_RIGHTDOWN { get; }
        uint MOUSEEVENTF_RIGHTUP { get; }
        uint MOUSEEVENTF_MOVE_NOCOALESCE { get; }
        uint KEYEVENTF_KEYDOWN { get; }
        uint KEYEVENTF_KEYUP { get; }
        int GWL_EXSTYLE { get; }
        int WS_EX_LAYERED { get; }
        int WS_EX_TRANSPARENT { get; }
        uint DIGCF_PRESENT { get; }
        uint DIGCF_DEVICEINTERFACE { get; }
        uint GENERIC_READ { get; }
        uint GENERIC_WRITE { get; }
        uint FILE_SHARE_READ { get; }
        uint FILE_SHARE_WRITE { get; }
        uint OPEN_EXISTING { get; }
        uint FILE_ATTRIBUTE_NORMAL { get; }
        uint WM_DEVICECHANGE { get; }
        uint DBT_DEVICEARRIVAL { get; }
        uint DBT_DEVICEREMOVECOMPLETE { get; }
        uint DBT_DEVTYP_DEVICEINTERFACE { get; }
        uint DEVICE_NOTIFY_WINDOW_HANDLE { get; }
        uint DEVICE_NOTIFY_ALL_INTERFACE_CLASSES { get; }
        uint CS_HREDRAW { get; }
        uint CS_VREDRAW { get; }
        uint WS_OVERLAPPED { get; }
        int CW_USEDEFAULT { get; }
        Guid GUID_DEVINTERFACE_HID { get; }
    }
}