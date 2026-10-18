using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RagnaController.Core
{
    /// <summary>
    /// FEAT-030: Multi-monitor support without System.Windows.Forms dependency.
    /// Uses P/Invoke (user32.dll) to enumerate display monitors.
    /// </summary>
    public static class MonitorHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MonitorInfo
        {
            public int cbSize;
            public Rect rcMonitor;
            public Rect rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

        public const uint MONITORINFOF_PRIMARY = 0x00000001;

        /// <summary>
        /// Returns information about all display monitors.
        /// Index 0 is always the primary monitor.
        /// </summary>
        public static List<MonitorDisplayInfo> GetMonitors()
        {
            _currentMonitors.Clear();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumCallback, IntPtr.Zero);

            // Sort: primary monitor first
            _currentMonitors.Sort((a, b) => b.IsPrimary.CompareTo(a.IsPrimary));
            return new List<MonitorDisplayInfo>(_currentMonitors);
        }

        private static bool MonitorEnumCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData)
        {
            var info = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfo(hMonitor, ref info))
            {
                // We need a way to pass the monitors list back - use a static field
                _currentMonitors.Add(new MonitorDisplayInfo
                {
                    Handle = hMonitor,
                    DeviceName = info.szDevice,
                    IsPrimary = (info.dwFlags & MONITORINFOF_PRIMARY) != 0,
                    Left = info.rcMonitor.Left,
                    Top = info.rcMonitor.Top,
                    Right = info.rcMonitor.Right,
                    Bottom = info.rcMonitor.Bottom,
                    Width = info.rcMonitor.Right - info.rcMonitor.Left,
                    Height = info.rcMonitor.Bottom - info.rcMonitor.Top,
                });
            }
            return true; // continue enumeration
        }

        private static List<MonitorDisplayInfo> _currentMonitors = new();

        public static int GetMonitorCount() => GetMonitors().Count;
    }

    public class MonitorDisplayInfo
    {
        public IntPtr Handle { get; set; }
        public string DeviceName { get; set; } = "";
        public bool IsPrimary { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public string DisplayName => IsPrimary
            ? $"{DeviceName} (Primary)"
            : $"{DeviceName} ({Width}×{Height})";
    }
}
