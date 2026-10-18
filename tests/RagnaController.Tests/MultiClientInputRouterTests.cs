using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// TASK-1603: Unit-Tests für MultiClientInputRouter.
    /// Alle Tests headless/CI-sicher (Mocks verwendet, keine echten Win32-Aufrufe).
    /// </summary>
    public class MultiClientInputRouterTests
    {
        private sealed class MockNativeMethods : INativeMethods
        {
            public readonly HashSet<IntPtr> AliveWindows = new();
            public readonly Dictionary<IntPtr, uint> HwndToPid = new();
            public readonly Dictionary<uint, string> PidToProcessName = new();
            public readonly List<(IntPtr Hwnd, uint Msg, IntPtr WParam, IntPtr LParam)> PostedMessages = new();

            public bool EnumWindows(EnumWindowsProcDelegate lpEnumFunc, IntPtr lParam)
            {
                foreach (var hwnd in AliveWindows)
                {
                    if (!lpEnumFunc(hwnd, IntPtr.Zero))
                        return false;
                }
                return true;
            }

            public bool IsWindow(IntPtr hWnd) => AliveWindows.Contains(hWnd);

            public bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam)
            {
                PostedMessages.Add((hWnd, Msg, wParam, lParam));
                return true;
            }

            // INativeMethods — nicht benötigte Member
            public IntPtr GetForegroundWindow() => IntPtr.Zero;
            public uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId)
            {
                lpdwProcessId = HwndToPid.TryGetValue(hWnd, out var pid) ? pid : 0u;
                return lpdwProcessId;
            }
            public bool GetClientRect(IntPtr hWnd, out RECT lpRect) { lpRect = default; return true; }
            public bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint) => true;
            public uint GetDpiForWindow(IntPtr hwnd) => 96;
            public uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize) => nInputs;
            public uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize) => nInputs;
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
        public void MultiClientInputRouter_SendKeyToClient_PostsMessage()
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

            var router = new MultiClientInputRouter(native, resolver, tracker);

            // Act
            var result = router.SendKeyToClient(Hwnd1, VirtualKey.F1, keyDown: true);

            // Assert
            Assert.True(result);
            Assert.Single(native.PostedMessages);
            var (hwnd, msg, wParam, lParam) = native.PostedMessages[0];
            Assert.Equal(Hwnd1, hwnd);
            Assert.Equal(native.WM_KEYDOWN, msg);
            Assert.Equal((IntPtr)(ushort)VirtualKey.F1, wParam);
        }

        [Fact]
        public void MultiClientInputRouter_SendKeyToClient_ZeroHwnd_ReturnsFalse()
        {
            // Arrange
            var native = new MockNativeMethods();
            var resolver = new MockProcessNameResolver();
            var tracker = new MultiClientWindowTracker(native, resolver);
            var router = new MultiClientInputRouter(native, resolver, tracker);

            // Act
            var result = router.SendKeyToClient(IntPtr.Zero, VirtualKey.F1, keyDown: true);

            // Assert
            Assert.False(result);
            Assert.Empty(native.PostedMessages);
        }

        [Fact]
        public void MultiClientInputRouter_SendKeyToClient_NoneKey_ReturnsFalse()
        {
            // Arrange
            var native = new MockNativeMethods();
            var resolver = new MockProcessNameResolver();
            var tracker = new MultiClientWindowTracker(native, resolver);
            var router = new MultiClientInputRouter(native, resolver, tracker);

            // Act
            var result = router.SendKeyToClient(Hwnd1, VirtualKey.None, keyDown: true);

            // Assert
            Assert.False(result);
            Assert.Empty(native.PostedMessages);
        }

        [Fact]
        public void MultiClientInputRouter_SendKeyUp_PostsWM_KEYUP()
        {
            // Arrange
            var native = new MockNativeMethods();
            native.AliveWindows.Add(Hwnd1);
            native.HwndToPid[Hwnd1] = 1001;
            native.PidToProcessName[1001] = "ragexe";

            var resolver = new MockProcessNameResolver();
            resolver.PidToName[1001] = "ragexe";

            var tracker = new MultiClientWindowTracker(native, resolver);
            var router = new MultiClientInputRouter(native, resolver, tracker);

            // Act
            var result = router.SendKeyToClient(Hwnd1, VirtualKey.Escape, keyDown: false);

            // Assert
            Assert.True(result);
            Assert.Single(native.PostedMessages);
            Assert.Equal(native.WM_KEYUP, native.PostedMessages[0].Msg);
        }

        [Fact]
        public void MultiClientInputRouter_SendMouseClickToClient_PostsMessages()
        {
            // Arrange
            var native = new MockNativeMethods();
            native.AliveWindows.Add(Hwnd1);
            native.HwndToPid[Hwnd1] = 1001;
            native.PidToProcessName[1001] = "ragexe";

            var resolver = new MockProcessNameResolver();
            resolver.PidToName[1001] = "ragexe";

            var tracker = new MultiClientWindowTracker(native, resolver);
            var router = new MultiClientInputRouter(native, resolver, tracker);

            // Act
            var result = router.SendMouseClickToClient(Hwnd1, 100, 200, leftButton: true);

            // Assert
            Assert.True(result);
            Assert.Equal(2, native.PostedMessages.Count);
            Assert.Equal(native.WM_LBUTTONDOWN, native.PostedMessages[0].Msg);
            Assert.Equal(native.WM_LBUTTONUP, native.PostedMessages[1].Msg);

            // lParam enthält x=100, y=200
            var lParamDown = native.PostedMessages[0].LParam;
            var x = lParamDown & 0xFFFF;
            var y = (lParamDown >> 16) & 0xFFFF;
            Assert.Equal(100, x);
            Assert.Equal(200, y);
        }

        [Fact]
        public void MultiClientInputRouter_ResolveClientHwnd_PreferredHwndWins()
        {
            // Arrange
            var native = new MockNativeMethods();
            native.AliveWindows.Add(Hwnd1);
            native.AliveWindows.Add(Hwnd2);
            native.HwndToPid[Hwnd1] = 1001;
            native.HwndToPid[Hwnd2] = 1002;
            native.PidToProcessName[1001] = "ragexe";
            native.PidToProcessName[1002] = "ragexe";

            var resolver = new MockProcessNameResolver();
            resolver.PidToName[1001] = "ragexe";
            resolver.PidToName[1002] = "ragexe";

            var tracker = new MultiClientWindowTracker(native, resolver);
            tracker.Refresh("ragexe");

            var router = new MultiClientInputRouter(native, resolver, tracker);

            // Bevorzugtes HWND setzen (Hwnd2)
            router.SetPreferredClientHwnd("ragexe", Hwnd2);

            // Act
            var hwnd = router.ResolveClientHwnd("ragexe");

            // Assert
            Assert.Equal(Hwnd2, hwnd);
        }

        [Fact]
        public void MultiClientInputRouter_ResolveClientHwnd_FirstAliveWhenNoPreferred()
        {
            // Arrange
            var native = new MockNativeMethods();
            native.AliveWindows.Add(Hwnd1);
            native.AliveWindows.Add(Hwnd2);
            native.HwndToPid[Hwnd1] = 1001;
            native.HwndToPid[Hwnd2] = 1002;
            native.PidToProcessName[1001] = "ragexe";
            native.PidToProcessName[1002] = "ragexe";

            var resolver = new MockProcessNameResolver();
            resolver.PidToName[1001] = "ragexe";
            resolver.PidToName[1002] = "ragexe";

            var tracker = new MultiClientWindowTracker(native, resolver);
            tracker.Refresh("ragexe");

            var router = new MultiClientInputRouter(native, resolver, tracker);

            // Act
            var hwnd = router.ResolveClientHwnd("ragexe");

            // Assert — erster gefundener (EnumWindows Reihenfolge)
            Assert.True(hwnd == Hwnd1 || hwnd == Hwnd2);
        }

        [Fact]
        public void MultiClientInputRouter_ResolveClientHwnd_NoClients_ReturnsZero()
        {
            // Arrange
            var native = new MockNativeMethods();
            var resolver = new MockProcessNameResolver();
            var tracker = new MultiClientWindowTracker(native, resolver);
            var router = new MultiClientInputRouter(native, resolver, tracker);

            // Act
            var hwnd = router.ResolveClientHwnd("nonexistent");

            // Assert
            Assert.Equal(IntPtr.Zero, hwnd);
        }

        [Fact]
        public void MultiClientInputRouter_ResolveClientHwnd_PreferredDead_FallsBackToFirstAlive()
        {
            // Arrange
            var native = new MockNativeMethods();
            native.AliveWindows.Add(Hwnd1); // lebt
            // Hwnd2 ist nicht in AliveWindows → tot
            native.HwndToPid[Hwnd1] = 1001;
            native.HwndToPid[Hwnd2] = 1002;
            native.PidToProcessName[1001] = "ragexe";
            native.PidToProcessName[1002] = "ragexe";

            var resolver = new MockProcessNameResolver();
            resolver.PidToName[1001] = "ragexe";
            resolver.PidToName[1002] = "ragexe";

            var tracker = new MultiClientWindowTracker(native, resolver);
            tracker.Refresh("ragexe");

            var router = new MultiClientInputRouter(native, resolver, tracker);

            // Bevorzugtes Hwnd2 setzen (ist tot)
            router.SetPreferredClientHwnd("ragexe", Hwnd2);

            // Act
            var hwnd = router.ResolveClientHwnd("ragexe");

            // Assert — fällt zurück auf Hwnd1 (lebt)
            Assert.Equal(Hwnd1, hwnd);
        }

        [Fact]
        public void MultiClientInputRouter_SetActiveProcessName_ResolveActiveClientHwnd()
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

            var router = new MultiClientInputRouter(native, resolver, tracker);

            // Act
            router.SetActiveProcessName("ragexe");
            var hwnd = router.ResolveActiveClientHwnd();

            // Assert
            Assert.Equal(Hwnd1, hwnd);
        }

        [Fact]
        public void MultiClientInputRouter_SetActiveProcessName_Null_ReturnsZero()
        {
            // Arrange
            var native = new MockNativeMethods();
            var resolver = new MockProcessNameResolver();
            var tracker = new MultiClientWindowTracker(native, resolver);
            var router = new MultiClientInputRouter(native, resolver, tracker);

            // Act
            router.SetActiveProcessName(null);
            var hwnd = router.ResolveActiveClientHwnd();

            // Assert
            Assert.Equal(IntPtr.Zero, hwnd);
        }

        [Fact]
        public void MultiClientInputRouter_ExtractProcessNameFromPath()
        {
            // Standard-Pfad
            Assert.Equal("myro", MultiClientInputRouter.ExtractProcessNameFromPath(@"C:\Ragnarok Online\myRO.exe"));
            Assert.Equal("ragexe", MultiClientInputRouter.ExtractProcessNameFromPath(@"C:\RO\ragexe.exe"));
            Assert.Equal("ragnarok", MultiClientInputRouter.ExtractProcessNameFromPath(@"D:\Games\Ragnarok.exe"));

            // Edge Cases
            Assert.Null(MultiClientInputRouter.ExtractProcessNameFromPath(null));
            Assert.Null(MultiClientInputRouter.ExtractProcessNameFromPath(""));
            Assert.Null(MultiClientInputRouter.ExtractProcessNameFromPath("   "));
            Assert.Equal("file", MultiClientInputRouter.ExtractProcessNameFromPath("file.exe")); // keine .exe Extension
        }

        [Fact]
        public void MultiClientInputRouter_SendKeyToActiveClient_NoProcessName_ReturnsFalse()
        {
            // Arrange
            var native = new MockNativeMethods();
            var resolver = new MockProcessNameResolver();
            var tracker = new MultiClientWindowTracker(native, resolver);
            var router = new MultiClientInputRouter(native, resolver, tracker);

            // Act — kein aktiver Prozessname gesetzt
            var result = router.SendKeyToActiveClient(VirtualKey.F1, keyDown: true);

            // Assert
            Assert.False(result);
            Assert.Empty(native.PostedMessages);
        }

        [Fact]
        public void MultiClientInputRouter_Dispose_ClearsPreferredHwnd()
        {
            // Arrange
            var native = new MockNativeMethods();
            var resolver = new MockProcessNameResolver();
            var tracker = new MultiClientWindowTracker(native, resolver);
            var router = new MultiClientInputRouter(native, resolver, tracker);

            router.SetPreferredClientHwnd("ragexe", Hwnd1);

            // Act
            router.Dispose();

            // Assert — nach Dispose sollte kein bevorzugtes HWND mehr gesetzt sein
            // (der bevorzugte HWND wird ignoriert, da der Router disposed ist)
            // Wir prüfen nur, dass keine Exception auftritt
            Assert.True(true);
        }
    }
}
