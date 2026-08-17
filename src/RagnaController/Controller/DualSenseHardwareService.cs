using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Controller
{
    public sealed class DualSenseHardwareService : IDisposable
    {
        private const string DualSenseVid  = "VID_054C";
        private const string DualSensePid1 = "PID_0CE6";
        private const string DualSensePid2 = "PID_0DF2";

        private const int  REPORT_SIZE   = 64;
        private const byte REPORT_ID     = 0x02;
        
        // Flags: 0x40 = Lightbar, 0x04 = RightTrigger, 0x08 = LeftTrigger
        private const byte VALID_FLAGS   = 0x40 | 0x04 | 0x08; 
        
        private const int OFFSET_R       = 45;
        private const int OFFSET_G       = 46;
        private const int OFFSET_B       = 47;
        
        private const int OFFSET_R2_MODE = 11;
        private const int OFFSET_L2_MODE = 22;

        private IntPtr _handle = IntPtr.Zero;
        private bool   _disposed;
        private int    _isWriting = 0;

        private readonly byte[] _report = new byte[REPORT_SIZE];

        public bool IsAvailable => _handle != IntPtr.Zero;

        public DualSenseHardwareService()
        {
            _report[0] = REPORT_ID;
            _report[1] = VALID_FLAGS;
            _handle    = OpenDualSenseHandle();

            if (IsAvailable)
            {
                SetHardwareState(0, 90, 210, AdaptiveTriggerMode.Off, AdaptiveTriggerMode.Off);
            }
        }

        // Applies dynamic colors based on engine state
        public void ApplySnapshot(Core.ControllerSnapshot snap, AdaptiveTriggerMode l2Mode, AdaptiveTriggerMode r2Mode)
        {
            if (!IsAvailable) return;

            byte r = 0, g = 90, b = 210; // Default Blue

            if (snap.PanicActive)       { r = 255; g = 80; b = 0; }
            else if (snap.VacuumActive) { r = 0; g = 220; b = 80; }
            else if (snap.ComboActive)  { r = 140; g = 0; b = 255; }
            else
            {
                switch (snap.CombatState)
                {
                    case "ENGAGED": r = 220; g = 20; b = 20; break;
                    case "SEEKING": r = 200; g = 150; b = 0; break;
                    default:
                        if (snap.L1)      { r = 0; g = 180; b = 220; }
                        else if (snap.R1) { r = 220; g = 80; b = 0; }
                        else if (snap.L2) { r = 0; g = 220; b = 120; }
                        else if (snap.R2) { r = 180; g = 0; b = 220; }
                        break;
                }
            }

            SetHardwareState(r, g, b, l2Mode, r2Mode);
        }

        private void SetHardwareState(byte r, byte g, byte b, AdaptiveTriggerMode l2, AdaptiveTriggerMode r2)
        {
            if (!IsAvailable) return;

            byte[] reportCopy = new byte[REPORT_SIZE];
            Buffer.BlockCopy(_report, 0, reportCopy, 0, REPORT_SIZE);

            // Set RGB
            reportCopy[OFFSET_R] = r;
            reportCopy[OFFSET_G] = g;
            reportCopy[OFFSET_B] = b;

            // Set Right Trigger (R2)
            reportCopy[OFFSET_R2_MODE] = (byte)r2;
            ApplyTriggerParameters(reportCopy, OFFSET_R2_MODE, r2);

            // Set Left Trigger (L2)
            reportCopy[OFFSET_L2_MODE] = (byte)l2;
            ApplyTriggerParameters(reportCopy, OFFSET_L2_MODE, l2);

            // Async USB write with anti-flooding mechanism
            if (Interlocked.CompareExchange(ref _isWriting, 1, 0) == 0)
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        NativeMethods.WriteFile(_handle, reportCopy, (uint)reportCopy.Length, out _, IntPtr.Zero);
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _isWriting, 0);
                    }
                });
            }
        }

        private static void ApplyTriggerParameters(byte[] buffer, int offset, AdaptiveTriggerMode mode)
        {
            // Parameters for different mechanical feelings
            switch (mode)
            {
                case AdaptiveTriggerMode.BowTension: // Continuous Resistance
                    buffer[offset + 1] = 0x02; // Start position
                    buffer[offset + 2] = 0x06; // Force
                    break;
                case AdaptiveTriggerMode.WeaponRecoil: // Clicky
                    buffer[offset + 1] = 0x02; // Start position
                    buffer[offset + 2] = 0x08; // End position
                    buffer[offset + 3] = 0x08; // Force
                    break;
                case AdaptiveTriggerMode.MagicPulse:
                    buffer[offset + 1] = 0x0F; // Pulse speed
                    buffer[offset + 2] = 0x04; // Pulse force
                    break;
                case AdaptiveTriggerMode.HardBlock:
                    buffer[offset + 1] = 0x00; // Start immediately
                    buffer[offset + 2] = 0xFF; // Max force
                    break;
            }
        }

        public void TurnOff() => SetHardwareState(0, 0, 0, AdaptiveTriggerMode.Off, AdaptiveTriggerMode.Off);

        /// <summary>
        /// Sets adaptive trigger modes from profile configuration.
        /// Called when profile is loaded to apply L2/R2 trigger modes.
        /// </summary>
        public void SetAdaptiveTriggerModes(AdaptiveTriggerMode l2Mode, AdaptiveTriggerMode r2Mode)
        {
            if (!IsAvailable) return;
            
            // Apply current snapshot with new trigger modes
            // We need a dummy snapshot to preserve RGB behavior
            var dummySnapshot = new Core.ControllerSnapshot();
            ApplySnapshot(dummySnapshot, l2Mode, r2Mode);
        }

        /// <summary>
        /// Sets the lightbar color without trigger parameters (for simple color control).
        /// Use this when you only need to change RGB without modifying adaptive triggers.
        /// </summary>
        public void SetColor(byte r, byte g, byte b)
        {
            if (!IsAvailable) return;
            
            byte[] reportCopy = new byte[REPORT_SIZE];
            Buffer.BlockCopy(_report, 0, reportCopy, 0, REPORT_SIZE);
            
            // Set RGB only (triggers remain at default)
            reportCopy[OFFSET_R] = r;
            reportCopy[OFFSET_G] = g;
            reportCopy[OFFSET_B] = b;
            
            // Async USB write with anti-flooding mechanism
            if (Interlocked.CompareExchange(ref _isWriting, 1, 0) == 0)
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        NativeMethods.WriteFile(_handle, reportCopy, (uint)reportCopy.Length, out _, IntPtr.Zero);
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _isWriting, 0);
                    }
                });
            }
        }

        private static IntPtr OpenDualSenseHandle()
        {
            // 1. Get the HID device GUID from the system
            NativeMethods.HidD_GetHidGuid(out Guid hidGuid);

            // 2. Enumerate all present HID devices
            IntPtr devInfo = NativeMethods.SetupDiGetClassDevs(
                ref hidGuid, IntPtr.Zero, IntPtr.Zero,
                NativeMethods.DIGCF_PRESENT | NativeMethods.DIGCF_DEVICEINTERFACE);

            if (devInfo == IntPtr.Zero || devInfo == new IntPtr(-1))
                return IntPtr.Zero;

            try
            {
                var ifaceData = new NativeMethods.SP_DEVICE_INTERFACE_DATA();
                ifaceData.cbSize = (uint)Marshal.SizeOf<NativeMethods.SP_DEVICE_INTERFACE_DATA>();

                for (uint i = 0; ; i++)
                {
                    // Enumerate each HID interface
                    if (!NativeMethods.SetupDiEnumDeviceInterfaces(
                            devInfo, IntPtr.Zero, ref hidGuid, i, ref ifaceData))
                        break; // ERROR_NO_MORE_ITEMS

                    // Get required buffer size for the detail struct
                    NativeMethods.SetupDiGetDeviceInterfaceDetail(
                        devInfo, ref ifaceData, IntPtr.Zero, 0, out uint needed, IntPtr.Zero);

                    if (needed == 0) continue;

                    // Allocate unmanaged buffer and read device path
                    IntPtr detailBuf = Marshal.AllocHGlobal((int)needed);
                    try
                    {
                        // First field of SP_DEVICE_INTERFACE_DETAIL_DATA is cbSize (DWORD)
                        Marshal.WriteInt32(detailBuf, IntPtr.Size == 8 ? 8 : 6);

                        if (!NativeMethods.SetupDiGetDeviceInterfaceDetail(
                                devInfo, ref ifaceData, detailBuf, needed, out _, IntPtr.Zero))
                            continue;

                        // Device path starts at offset 4 (after cbSize DWORD)
                        string path = Marshal.PtrToStringAuto(detailBuf + 4) ?? "";

                        // Check for DualSense VID/PID in path
                        bool isDualSense =
                            path.IndexOf(DualSenseVid,  StringComparison.OrdinalIgnoreCase) >= 0 &&
                           (path.IndexOf(DualSensePid1, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            path.IndexOf(DualSensePid2, StringComparison.OrdinalIgnoreCase) >= 0);

                        if (!isDualSense) continue;

                        Debug.WriteLine($"[DualSense] Found device: {path}");

                        IntPtr handle = NativeMethods.CreateFile(
                            path,
                            NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE,
                            NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                            IntPtr.Zero,
                            NativeMethods.OPEN_EXISTING,
                            NativeMethods.FILE_ATTRIBUTE_NORMAL,
                            IntPtr.Zero);

                        if (handle != IntPtr.Zero && handle != new IntPtr(-1))
                            return handle; // success — caller owns handle
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(detailBuf);
                    }
                }
            }
            finally
            {
                NativeMethods.SetupDiDestroyDeviceInfoList(devInfo);
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_handle != IntPtr.Zero)
            {
                TurnOff();
                NativeMethods.CloseHandle(_handle);
                _handle = IntPtr.Zero;
            }
        }
    }
}
