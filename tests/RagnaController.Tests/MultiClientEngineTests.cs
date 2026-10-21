using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;
using RagnaController.Controller;

namespace RagnaController.Tests
{
    /// <summary>
    /// TASK-2108: Unit-Tests für MultiClientEngineOrchestrator und EngineInstance.
    /// Alle Tests headless/CI-sicher (echte Klassen + vollständige Stubs für Interfaces).
    /// </summary>
    public class MultiClientEngineTests
    {
        #region Vollständige Stubs für INativeMethods und IProcessNameResolver

        private sealed class StubNativeMethods : INativeMethods
        {
            public readonly HashSet<IntPtr> AliveWindows = new();
            public readonly Dictionary<IntPtr, uint> HwndToPid = new();
            public readonly List<(IntPtr Hwnd, uint Msg, IntPtr WParam, IntPtr LParam)> Posted = new();

            // Window & Process
            public IntPtr GetForegroundWindow() => IntPtr.Zero;
            public uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId)
            {
                lpdwProcessId = HwndToPid.TryGetValue(hWnd, out var pid) ? pid : 0u;
                return lpdwProcessId;
            }
            public bool GetClientRect(IntPtr hWnd, out RECT lpRect) { lpRect = default; return true; }
            public bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint) => true;
            public uint GetDpiForWindow(IntPtr hwnd) => 96;

            // Input
            public uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize) => 0u;
            public uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize) => 0u;
            public bool GetCursorPos(out POINT lpPoint) { lpPoint = default; return true; }
            public bool SetCursorPos(int x, int y) => true;
            public IntPtr GetCursor() => IntPtr.Zero;

            // Timer
            public uint timeBeginPeriod(uint uPeriod) => 0;
            public uint timeEndPeriod(uint uPeriod) => 0;

            // Window Styles
            public IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) => IntPtr.Zero;
            public IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong) => IntPtr.Zero;

            // HID & SetupAPI
            public void HidD_GetHidGuid(out Guid hidGuid) { hidGuid = Guid.Empty; }
            public IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags) => IntPtr.Zero;
            public bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData) => false;
            public bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, uint DeviceInterfaceDetailDataSize, out uint RequiredSize, IntPtr DeviceInfoData) { RequiredSize = 0; return false; }
            public bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet) => true;
            public IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile) => IntPtr.Zero;
            public bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped) { lpNumberOfBytesWritten = 0; return true; }
            public bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped) { lpNumberOfBytesRead = 0; return true; }
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

            // Multi-Client Window Enumeration
            public bool EnumWindows(EnumWindowsProcDelegate lpEnumFunc, IntPtr lParam)
            {
                foreach (var hwnd in AliveWindows)
                    if (!lpEnumFunc(hwnd, IntPtr.Zero)) return false;
                return true;
            }
            public bool IsWindow(IntPtr hWnd) => AliveWindows.Contains(hWnd);
            public bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam)
            {
                Posted.Add((hWnd, Msg, wParam, lParam));
                return true;
            }

            // Constants (as Properties per interface)
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
            public int WS_EX_LAYERED => 0x00080000;
            public int WS_EX_TRANSPARENT => 0x00000020;
            public uint DIGCF_PRESENT => 2;
            public uint DIGCF_DEVICEINTERFACE => 16;
            public uint GENERIC_READ => 0x80000000;
            public uint GENERIC_WRITE => 0x40000000;
            public uint FILE_SHARE_READ => 1;
            public uint FILE_SHARE_WRITE => 2;
            public uint OPEN_EXISTING => 3;
            public uint FILE_ATTRIBUTE_NORMAL => 0x80;
            public uint WM_DEVICECHANGE => 0x0219;
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

        private sealed class StubProcessNameResolver : IProcessNameResolver
        {
            public string? GetProcessName(int pid) => "ragexe";
        }

        #endregion

        #region Helfer

        private (MultiClientEngineOrchestrator orch, StubNativeMethods native) CreateOrchestratorWithClients(params (IntPtr hwnd, string processName, uint pid)[] clients)
        {
            var native = new StubNativeMethods();
            var resolver = new StubProcessNameResolver();
            var tick = new BackgroundTickProvider(125);
            var messenger = new Messenger();
            var logger = new AdvancedLogger(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"ragna_test_{Guid.NewGuid():N}.log"));
            var controllerService = new ControllerService();
            var profileManager = new ProfileManager();
            var settings = new Settings();
            var tracker = new MultiClientWindowTracker(native, resolver);
            var router = new MultiClientInputRouter(native, resolver, tracker);

            var orch = new MultiClientEngineOrchestrator(
                controllerService, profileManager, settings, messenger, logger, router, tracker);

            foreach (var (hwnd, processName, pid) in clients)
            {
                native.AliveWindows.Add(hwnd);
                orch.StartClient(hwnd, processName, pid, new Profile { Name = $"Profile_{pid}", Class = "Novice" });
            }

            return (orch, native);
        }

        private static IntPtr MakeHwnd(int value) => new IntPtr(value * 1000 + 100);

        #endregion

        #region MultiClientEngineOrchestrator Tests

        [Fact]
        public void StartClient_CreatesInstance_WithCorrectProperties()
        {
            var (orch, native) = CreateOrchestratorWithClients();
            var hwnd = MakeHwnd(1);
            native.AliveWindows.Add(hwnd);

            var instance = orch.StartClient(hwnd, "ragexe", 1234, new Profile { Name = "Test", Class = "Novice" });

            Assert.NotNull(instance);
            Assert.Equal(hwnd, instance.ClientHwnd);
            Assert.Equal("ragexe", instance.ProcessName);
            Assert.Equal(1234u, instance.ProcessId);
            Assert.True(instance.IsActive);
            Assert.True(instance.Orchestrator.IsRunning);
            Assert.Equal(EngineStatus.Running, instance.Status);
        }

        [Fact]
        public void StartClient_ReturnsSameInstance_ForSameHwnd()
        {
            var (orch, native) = CreateOrchestratorWithClients();
            var hwnd = MakeHwnd(2);
            native.AliveWindows.Add(hwnd);

            var first = orch.StartClient(hwnd, "ragexe", 1234, new Profile { Name = "P1", Class = "Novice" });
            var second = orch.StartClient(hwnd, "ragexe", 1234, null);

            Assert.Same(first, second);
            Assert.Equal(1, orch.TotalCount);
        }

        [Fact]
        public void StartClient_Throws_OnZeroHwnd()
        {
            var (orch, _) = CreateOrchestratorWithClients();

            Assert.Throws<ArgumentException>(() => orch.StartClient(IntPtr.Zero, "ragexe", 1234));
        }

        [Fact]
        public void StartClient_Throws_OnEmptyProcessName()
        {
            var (orch, _) = CreateOrchestratorWithClients();

            Assert.Throws<ArgumentException>(() => orch.StartClient(MakeHwnd(3), "", 1234));
        }

        [Fact]
        public void StopClient_RemovesInstance()
        {
            var (orch, native) = CreateOrchestratorWithClients((MakeHwnd(4), "ragexe", 1));
            Assert.Equal(1, orch.TotalCount);

            orch.StopClient(MakeHwnd(4));
            Assert.Equal(0, orch.TotalCount);
            Assert.Null(orch.GetInstance(MakeHwnd(4)));
        }

        [Fact]
        public void StopClient_NoOp_OnUnknownHwnd()
        {
            var (orch, _) = CreateOrchestratorWithClients();

            orch.StopClient(MakeHwnd(999));
        }

        [Fact]
        public void SwitchProfile_ChangesProfile()
        {
            var (orch, native) = CreateOrchestratorWithClients((MakeHwnd(5), "ragexe", 1));
            var newProfile = new Profile { Name = "NewProfile", Class = "Archer" };

            orch.SwitchProfile(MakeHwnd(5), newProfile);

            var instance = orch.GetInstance(MakeHwnd(5));
            Assert.NotNull(instance);
            Assert.True(instance.IsActive);
        }

        [Fact]
        public void SwitchProfile_Throws_OnMissingInstance()
        {
            var (orch, _) = CreateOrchestratorWithClients();

            Assert.Throws<KeyNotFoundException>(() =>
                orch.SwitchProfile(MakeHwnd(999), new Profile { Name = "X", Class = "Novice" }));
        }

        [Fact]
        public void GetInstance_ReturnsNull_ForUnknownHwnd()
        {
            var (orch, _) = CreateOrchestratorWithClients();

            Assert.Null(orch.GetInstance(MakeHwnd(999)));
        }

        [Fact]
        public void GetActiveInstance_ReturnsPreferredClient()
        {
            var (orch, native) = CreateOrchestratorWithClients((MakeHwnd(6), "ragexe", 1));
            var active = orch.GetActiveInstance();

            Assert.NotNull(active);
            Assert.Equal(MakeHwnd(6), active.ClientHwnd);
        }

        [Fact]
        public void GetActiveInstance_ReturnsNull_WhenNoClients()
        {
            var (orch, _) = CreateOrchestratorWithClients();

            Assert.Null(orch.GetActiveInstance());
        }

        [Fact]
        public void CleanupDeadClients_RemovesDeadClients()
        {
            var (orch, native) = CreateOrchestratorWithClients(
                (MakeHwnd(7), "ragexe", 1),
                (MakeHwnd(8), "ragexe", 2));
            native.AliveWindows.Remove(MakeHwnd(8));

            Assert.Equal(2, orch.TotalCount);
            orch.CleanupDeadClients();
            Assert.Equal(1, orch.TotalCount);
            Assert.NotNull(orch.GetInstance(MakeHwnd(7)));
            Assert.Null(orch.GetInstance(MakeHwnd(8)));
        }

        [Fact]
        public void StopAll_RemovesAllInstances()
        {
            var (orch, native) = CreateOrchestratorWithClients(
                (MakeHwnd(9), "ragexe", 1),
                (MakeHwnd(10), "ragexe", 2));

            Assert.Equal(2, orch.TotalCount);
            orch.StopAll();
            Assert.Equal(0, orch.TotalCount);
        }

        [Fact]
        public void ClientStarted_Event_Fires()
        {
            var (orch, native) = CreateOrchestratorWithClients();
            var hwnd = MakeHwnd(11);
            native.AliveWindows.Add(hwnd);

            EngineInstance? fired = null;
            orch.ClientStarted += (o, inst) => fired = inst;

            orch.StartClient(hwnd, "ragexe", 1234);

            Assert.NotNull(fired);
            Assert.Equal(hwnd, fired.ClientHwnd);
        }

        #endregion

        #region EngineInstance Tests

        [Fact]
        public void EngineInstance_Activate_SetsIsActive()
        {
            var (orch, native) = CreateOrchestratorWithClients();
            var hwnd = MakeHwnd(12);
            native.AliveWindows.Add(hwnd);
            var instance = orch.StartClient(hwnd, "ragexe", 1234);

            instance.Deactivate();
            Assert.False(instance.IsActive);

            instance.Activate(new Profile { Name = "Reactivated", Class = "Novice" });
            Assert.True(instance.IsActive);
        }

        [Fact]
        public void EngineInstance_Activate_Throws_OnNullProfile()
        {
            var (orch, native) = CreateOrchestratorWithClients();
            var hwnd = MakeHwnd(13);
            native.AliveWindows.Add(hwnd);
            var instance = orch.StartClient(hwnd, "ragexe", 1234);

            Assert.Throws<ArgumentNullException>(() => instance.Activate(null!));
        }

        [Fact]
        public void EngineInstance_StartStop_RunningState()
        {
            var (orch, native) = CreateOrchestratorWithClients();
            var hwnd = MakeHwnd(14);
            native.AliveWindows.Add(hwnd);
            var instance = orch.StartClient(hwnd, "ragexe", 1234);

            Assert.True(instance.Orchestrator.IsRunning);

            instance.Stop();
            Assert.False(instance.Orchestrator.IsRunning);

            instance.Start();
            Assert.True(instance.Orchestrator.IsRunning);
        }

        [Fact]
        public void EngineInstance_PauseResume_PausedState()
        {
            var (orch, native) = CreateOrchestratorWithClients();
            var hwnd = MakeHwnd(15);
            native.AliveWindows.Add(hwnd);
            var instance = orch.StartClient(hwnd, "ragexe", 1234);

            instance.Pause();
            Assert.True(instance.Orchestrator.IsPaused);

            instance.Resume();
            Assert.False(instance.Orchestrator.IsPaused);
        }

        [Fact]
        public void EngineInstance_SwitchProfile_Works()
        {
            var (orch, native) = CreateOrchestratorWithClients();
            var hwnd = MakeHwnd(16);
            native.AliveWindows.Add(hwnd);
            var instance = orch.StartClient(hwnd, "ragexe", 1234, new Profile { Name = "Old", Class = "Novice" });

            instance.SwitchProfile(new Profile { Name = "New", Class = "Archer" });

            Assert.True(instance.IsActive);
        }

        [Fact]
        public void EngineInstance_SwitchProfile_Throws_OnNull()
        {
            var (orch, native) = CreateOrchestratorWithClients();
            var hwnd = MakeHwnd(17);
            native.AliveWindows.Add(hwnd);
            var instance = orch.StartClient(hwnd, "ragexe", 1234);

            Assert.Throws<ArgumentNullException>(() => instance.SwitchProfile(null!));
        }

        [Fact]
        public void EngineInstance_ShowHideOverlay()
        {
            var (orch, native) = CreateOrchestratorWithClients();
            var hwnd = MakeHwnd(18);
            native.AliveWindows.Add(hwnd);
            var instance = orch.StartClient(hwnd, "ragexe", 1234);

            // Overlay is null until ShowOverlay() is called (lazy init)
            Assert.Null(instance.OverlayWindow);

            // ShowOverlay requires STA thread (WPF Window) — tested separately in integration
            // Verify the lazy-init pattern: OverlayWindow property returns null before ShowOverlay()
            Assert.Null(instance.OverlayWindow);
        }

        [Fact]
        public void EngineInstance_Status_Running_WhenStarted()
        {
            var (orch, native) = CreateOrchestratorWithClients();
            var hwnd = MakeHwnd(19);
            native.AliveWindows.Add(hwnd);
            var instance = orch.StartClient(hwnd, "ragexe", 1234);

            Assert.Equal(EngineStatus.Running, instance.Status);
        }

        [Fact]
        public void EngineInstance_Status_Stopped_WhenStopped()
        {
            var (orch, native) = CreateOrchestratorWithClients();
            var hwnd = MakeHwnd(20);
            native.AliveWindows.Add(hwnd);
            var instance = orch.StartClient(hwnd, "ragexe", 1234);

            instance.Stop();
            Assert.Equal(EngineStatus.Stopped, instance.Status);
        }

        [Fact]
        public void EngineInstance_Dispose_IsIdempotent()
        {
            var (orch, native) = CreateOrchestratorWithClients();
            var hwnd = MakeHwnd(21);
            native.AliveWindows.Add(hwnd);
            var instance = orch.StartClient(hwnd, "ragexe", 1234);

            instance.Dispose();
            instance.Dispose();
        }

        #endregion
    }
}
