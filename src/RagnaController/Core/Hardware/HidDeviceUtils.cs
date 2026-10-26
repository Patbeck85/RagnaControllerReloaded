using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using RagnaController.Core;
using RagnaController.Controller;

namespace RagnaController.Core.Hardware {
    /// <summary>
    /// TASK-TECH: Shared HID Device Utilities.
    /// 
    /// Zentrale Utilities für HID-Geräte-Erkennung und -Enumeration.
    /// Eliminiert Code-Duplikate zwischen GyroService und DualSenseHardwareService.
    /// </summary>
    public static class HidDeviceUtils
    {
        // DualSense USB Vendor/Product IDs
        public const string DualSenseVid = "VID_054C";
        public const string DualSensePid1 = "PID_0CE6";  // DualSense
        public const string DualSensePid2 = "PID_0DF2";  // DualSense Edge

        /// <summary>
        /// Prüft ob ein Gerätepfad zu einem DualSense-Controller gehört.
        /// </summary>
        public static bool IsDualSenseDevice(string devicePath)
        {
            if (string.IsNullOrWhiteSpace(devicePath))
                return false;

            return devicePath.IndexOf(DualSenseVid, StringComparison.OrdinalIgnoreCase) >= 0 &&
                   (devicePath.IndexOf(DualSensePid1, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    devicePath.IndexOf(DualSensePid2, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Enumeriert alle HID-Geräte und gibt Pfade von DualSense-Controllern zurück.
        /// </summary>
        public static System.Collections.Generic.List<string> EnumerateDualSenseDevices()
        {
            var devices = new System.Collections.Generic.List<string>();
            
            try
            {
                var hidGuid = new Guid();
                HidD_GetHidGuid(out hidGuid);

                IntPtr devInfo = SetupDiGetClassDevs(
                    ref hidGuid, IntPtr.Zero, IntPtr.Zero,
                    DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

                if (devInfo == IntPtr.Zero || devInfo == new IntPtr(-1))
                    return devices;

                try
                {
                    var ifaceData = new SP_DEVICE_INTERFACE_DATA { cbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>() };
                    for (uint index = 0; SetupDiEnumDeviceInterfaces(devInfo, IntPtr.Zero, ref hidGuid, index, ref ifaceData); index++)
                    {
                        uint needed = 0;
                        SetupDiGetDeviceInterfaceDetail(devInfo, ref ifaceData, IntPtr.Zero, 0, out needed, IntPtr.Zero);
                        
                        var detailBuf = Marshal.AllocHGlobal((int)needed);
                        try
                        {
                            var detail = new SP_DEVICE_INTERFACE_DETAIL_DATA
                            {
                                cbSize = IntPtr.Size == 8 ? 8 : 4 + Marshal.SystemDefaultCharSize
                            };
                            Marshal.StructureToPtr(detail, detailBuf, false);

                            if (SetupDiGetDeviceInterfaceDetail(devInfo, ref ifaceData, detailBuf, needed, out _, IntPtr.Zero))
                            {
                                var path = Marshal.PtrToStringAuto(
                                    IntPtr.Add(detailBuf, IntPtr.Size == 8 ? 8 : 6));
                                
                                if (IsDualSenseDevice(path!))
                                    devices.Add(path);
                            }
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(detailBuf);
                        }
                    }
                }
                finally
                {
                    SetupDiDestroyDeviceInfoList(devInfo);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HidDeviceUtils] Device enumeration failed: {ex.Message}");
            }

            return devices;
        }

        #region P/Invoke

        private const int DIGCF_PRESENT = 0x02;
        private const int DIGCF_DEVICEINTERFACE = 0x10;

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            public char DevicePath;
        }

        [DllImport("hid.dll", SetLastError = true)]
        private static extern void HidD_GetHidGuid(out Guid hidGuid);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SetupDiGetClassDevs(
            ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, int flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr deviceInfoSet, IntPtr deviceInfoData, ref Guid interfaceClassGuid,
            uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            IntPtr deviceInterfaceDetailData, uint deviceInterfaceDetailDataSize,
            out uint requiredSize, IntPtr deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        #endregion
    }
}