using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;
using RagnaController.Core;

namespace RagnaController.Tests
{
    /// <summary>
    /// TASK-1602: Unit-Tests für MultiClientWindowTracker.
    /// Alle Tests headless/CI-sicher (keine echten Win32-Aufrufe, Mocks verwendet).
    /// </summary>
    public class MultiClientWindowTrackerTests
    {
        private sealed class MockNativeMethods : INativeMethods
        {
            public readonly HashSet<IntPtr> AliveWindows = new();
            public readonly Dictionary<IntPtr, uint> HwndToPid = new();
            public readonly Dictionary<uint, string> PidToProcessName = new();
            public readonly List<IntPtr> EnumWindowsCalls = new();

            public bool EnumWindows(EnumWindowsProcDelegate lpEnumFunc, IntPtr lParam)
            {
                EnumWindowsCalls.Add(IntPtr.Zero);
                foreach (var hwnd in AliveWindows)
                {
                    if (!lpEnumFunc(hwnd, IntPtr.Zero))
                        return false;
                }
                return true;
            }

            public bool IsWindow(IntPtr hWnd) => AliveWindows.Contains(hWnd);

            public bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam) => true;

            // INativeMethods — nicht benötigte Member (werden nicht von MultiClientWindowTracker genutzt)
            public IntPtr GetForegroundWindow() => IntPtr.Zero;
            public uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId)
            {
                lpdwProcessId = HwndToPid.TryGetValue(hWnd, out var pid) ? pid : 0u;
                return lpdwProcessId;
            }
            public bool GetClientRect(IntPtr hWnd, out RECT lpRect) { lpRect = default; return true; }
            public bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint) => true;
            public uint GetDpiForWindow(IntPtr hwnd) => 96;
            public uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize) => 0;
            public uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize) => 0;
            public bool GetCursorPos(out POINT lpPoint) { lpPoint = default; return true; }
            public bool SetCursorPos(int x, int y) => true;
            public IntPtr GetCursor() => IntPtr.Zero;
            public uint timeBeginPeriod(uint uPeriod) => 1;
            public uint timeEndPeriod(uint uPeriod) => 1;
            public IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) => IntPtr.Zero;
            public IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) => IntPtr.Zero;
            public void HidD_GetHidGuid(out Guid hidGuid) { hidGuid = Guid.Empty; }
            public IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags) => IntPtr.Zero;
            public bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData) => false;
            public bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, uint DeviceInterfaceDetailDataSize, out uint RequiredSize, IntPtr DeviceInfoData) { RequiredSize = 0; return false; }
            public bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet) => true;
            public IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile) => IntPtr.Zero;
            public bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped) { lpNumberOfBytesWritten = 0; return true; }
            public bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped) { lpNumberOfBytesRead = 0; return true; }
            public bool CloseHandle(IntPtr hObject) => true;
            public IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr notificationFilter, uint flags) => IntPtr.Zero;
            public bool UnregisterDeviceNotification(IntPtr handle) => true;
            public IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam) => IntPtr.Zero;
            public bool DestroyWindow(IntPtr hWnd) => true;
            public IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam) => IntPtr.Zero;
            public ushort RegisterClassEx(ref WNDCLASSEX lpwcx) => 0;
            public IntPtr GetModuleHandle(string? lpModuleName) => IntPtr.Zero;
            public int InputSize => 0;
            public uint INPUT_MOUSE => 0;
            public uint INPUT_KEYBOARD => 1;
            public uint MOUSEEVENTF_MOVE => 1;
            public uint MOUSEEVENTF_LEFTDOWN => 2;
            public uint MOUSEEVENTF_LEFTUP => 4;
            public uint MOUSEEVENTF_RIGHTDOWN => 8;
            public uint MOUSEEVENTF_RIGHTUP => 16;
            public uint MOUSEEVENTF_MOVE_NOCOALESCE => 0x2000;
            public uint MOUSEEVENTF_ABSOLUTE => 0x8000;
            public uint MOUSEEVENTF_WHEEL => 0x0800;
            public uint KEYEVENTF_KEYDOWN => 0;
            public uint KEYEVENTF_KEYUP => 2;
            public int GWL_EXSTYLE => -20;
            public int WS_EX_LAYERED => unchecked((int)0x80000);
            public int WS_EX_TRANSPARENT => 0x20;
            public uint DIGCF_PRESENT => 2;
            public uint DIGCF_DEVICEINTERFACE => 16;
            public uint GENERIC_READ => 0x80000000;
            public uint GENERIC_WRITE => 0x40000000;
            public uint FILE_SHARE_READ => 1;
            public uint FILE_SHARE_WRITE => 2;
            public uint OPEN_EXISTING => 3;
            public uint FILE_ATTRIBUTE_NORMAL => 0x80;
            public uint WM_DEVICECHANGE => 0x219;
            public uint DBT_DEVICEARRIVAL => 0x8000;
            public uint DBT_DEVICEREMOVECOMPLETE => 0x8004;
            public uint DBT_DEVTYP_DEVICEINTERFACE => 5;
            public uint DEVICE_NOTIFY_WINDOW_HANDLE => 0;
            public uint DEVICE_NOTIFY_ALL_INTERFACE_CLASSES => 4;
            public uint CS_HREDRAW => 2;
            public uint CS_VREDRAW => 1;
            public uint WS_OVERLAPPED => 0;
            public int CW_USEDEFAULT => unchecked((int)0x80000000);
            public Guid GUID_DEVINTERFACE_HID => Guid.Empty;
            public uint WM_KEYDOWN => 0x0100;
            public uint WM_KEYUP => 0x0101;
            public uint WM_CHAR => 0x0102;
            public uint WM_LBUTTONDOWN => 0x0201;
            public uint WM_LBUTTONUP => 0x0202;
            public uint WM_RBUTTONDOWN => 0x0204;
            public uint WM_RBUTTONUP => 0x0205;
            public uint WM_MOUSEWHEEL => 0x020A;
        }

        private sealed class MockProcessNameResolver : IProcessNameResolver
        {
            public readonly Dictionary<int, string> PidToName = new();
            public string? GetProcessName(int pid) => PidToName.TryGetValue(pid, out var name) ? name : null;
        }

        private static readonly IntPtr Hwnd1 = new(0x1001);
        private static readonly IntPtr Hwnd2 = new(0x1002);
        private static readonly IntPtr Hwnd3 = new(0x1003);

        [Fact]
        public void MultiClientWindowTracker_Refresh_FindsAllClients()
        {
            // Arrange
            var native = new MockNativeMethods();
            native.AliveWindows.Add(Hwnd1);
            native.AliveWindows.Add(Hwnd2);
            native.AliveWindows.Add(Hwnd3);
            native.HwndToPid[Hwnd1] = 1001;
            native.HwndToPid[Hwnd2] = 1002;
            native.HwndToPid[Hwnd3] = 1003;
            native.PidToProcessName[1001] = "ragexe";
            native.PidToProcessName[1002] = "ragexe";
            native.PidToProcessName[1003] = "other_process";

            var resolver = new MockProcessNameResolver();
            resolver.PidToName[1001] = "ragexe";
            resolver.PidToName[1002] = "ragexe";
            resolver.PidToName[1003] = "other_process";

            var tracker = new MultiClientWindowTracker(native, resolver);

            // Act
            tracker.Refresh("ragexe");

            // Assert
            var clients = tracker.GetClients("ragexe");
            Assert.Equal(2, clients.Count);
            Assert.Contains(clients, c => c.Hwnd == Hwnd1 && c.Pid == 1001 && c.IsAlive);
            Assert.Contains(clients, c => c.Hwnd == Hwnd2 && c.Pid == 1002 && c.IsAlive);
            Assert.DoesNotContain(clients, c => c.Hwnd == Hwnd3); // anderer Prozessname
        }

        [Fact]
        public void MultiClientWindowTracker_Refresh_NoClients_ReturnsEmpty()
        {
            // Arrange
            var native = new MockNativeMethods();
            var resolver = new MockProcessNameResolver();
            var tracker = new MultiClientWindowTracker(native, resolver);

            // Act
            tracker.Refresh("nonexistent");

            // Assert
            Assert.Empty(tracker.GetClients("nonexistent"));
        }

        [Fact]
        public void MultiClientWindowTracker_IsAlive_ReturnsCorrectState()
        {
            // Arrange
            var native = new MockNativeMethods();
            native.AliveWindows.Add(Hwnd1);
            native.HwndToPid[Hwnd1] = 1001;
            native.PidToProcessName[1001] = "ragexe";

            var resolver = new MockProcessNameResolver();
            resolver.PidToName[1001] = "ragexe";

            var tracker = new MultiClientWindowTracker(native, resolver);

            // Act & Assert
            Assert.True(tracker.IsAlive(Hwnd1));
            Assert.False(tracker.IsAlive(Hwnd2)); // nicht registriert
            Assert.False(tracker.IsAlive(IntPtr.Zero));
        }

        [Fact]
        public void MultiClientWindowTracker_CleanupDead_RemovesDeadClients()
        {
            // Arrange
            var native = new MockNativeMethods();
            native.AliveWindows.Add(Hwnd1); // lebt
            native.AliveWindows.Add(Hwnd2); // lebt zunächst
            native.HwndToPid[Hwnd1] = 1001;
            native.HwndToPid[Hwnd2] = 1002;
            native.PidToProcessName[1001] = "ragexe";
            native.PidToProcessName[1002] = "ragexe";

            var resolver = new MockProcessNameResolver();
            resolver.PidToName[1001] = "ragexe";
            resolver.PidToName[1002] = "ragexe";

            var tracker = new MultiClientWindowTracker(native, resolver);
            tracker.Refresh("ragexe");
            Assert.Equal(2, tracker.GetClients("ragexe").Count);

            // Act: Hwnd2 "tötet" (aus AliveWindows entfernen)
            native.AliveWindows.Remove(Hwnd2);
            tracker.CleanupDead();

            // Assert
            var clients = tracker.GetClients("ragexe");
            Assert.Single(clients);
            Assert.Equal(Hwnd1, clients[0].Hwnd);
        }

        [Fact]
        public void MultiClientWindowTracker_Clear_EmptiesCache()
        {
            // Arrange
            var native = new MockNativeMethods();
            native.AliveWindows.Add(Hwnd1);
            native.HwndToPid[Hwnd1] = 1001;
            native.PidToProcessName[1001] = "ragexe";

            var resolver = new MockProcessNameResolver();
            resolver.PidToName[1001] = "ragexe";

            var tracker = new MultiClientWindowTracker(native, resolver);
            tracker.Refresh("ragexe");
            Assert.NotEmpty(tracker.GetClients("ragexe"));

            // Act
            tracker.Clear();

            // Assert
            Assert.Empty(tracker.GetClients("ragexe"));
        }

        [Fact]
        public void MultiClientWindowTracker_DoesNotDuplicateOnRepeatedRefresh()
        {
            // Arrange
            var native = new MockNativeMethods();
            native.AliveWindows.Add(Hwnd1);
            native.HwndToPid[Hwnd1] = 1001;
            native.PidToProcessName[1001] = "ragexe";

            var resolver = new MockProcessNameResolver();
            resolver.PidToName[1001] = "ragexe";

            var tracker = new MultiClientWindowTracker(native, resolver);

            // Act
            tracker.Refresh("ragexe");
            tracker.Refresh("ragexe");
            tracker.Refresh("ragexe");

            // Assert
            var clients = tracker.GetClients("ragexe");
            Assert.Single(clients);
        }
    }
}
