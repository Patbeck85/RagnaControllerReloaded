using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RagnaController.Core
{
    /// <summary>
    /// Fokussiert ein Fenster per Prozessname.
    /// Gecachte Prozess-HWND — kein GetProcessesByName() bei jedem Aufruf.
    /// </summary>
    public static class WindowSwitcher
    {
        private const int SW_RESTORE = 9;

        // HWND-Cache: ProcessName → (hwnd, lastChecked)
        private static readonly Dictionary<string, (IntPtr hwnd, long tick)> _cache = new();
        private const long CACHE_TTL_MS = 10_000; // alle 10s verifizieren

        public static async Task ToggleAsync(string processName)
        {
            // FIX: Atomare Window-Switching mit Retry-Logik gegen Race Conditions
            const int maxRetries = 3;
            for (int retry = 0; retry < maxRetries; retry++)
            {
                IntPtr hwnd = GetCachedHwnd(processName);
                if (hwnd == IntPtr.Zero) return;

                // FIX: Always restore window first to ensure consistent state
                if (IsIconic(hwnd)) ShowWindow(hwnd, SW_RESTORE);

                // FIX: Capture thread IDs in local variables (volatile not needed for locals)
                uint foreThread = 0;
                uint targetThread = 0;

                IntPtr foregroundHwnd = GetForegroundWindow();
                foreThread = (uint)GetWindowThreadProcessId(foregroundHwnd, IntPtr.Zero);
                targetThread = (uint)GetWindowThreadProcessId(hwnd, IntPtr.Zero);

                // FIX: Only attach if threads are different AND window is not already focused
                if (foreThread != targetThread && GetForegroundWindow() != hwnd)
                {
                    AttachThreadInput(foreThread, targetThread, true);
                    SetForegroundWindow(hwnd);
                    SetFocus(hwnd);
                    AttachThreadInput(foreThread, targetThread, false);
                }
                else if (GetForegroundWindow() != hwnd)
                {
                    // Window is already in foreground thread, just bring to front
                    SetForegroundWindow(hwnd);
                }

                // FIX: Small delay to allow Windows to process the window switch
                await Task.Delay(10);
                
                // FIX: Verify the window is actually focused after the operation
                IntPtr currentForeground = GetForegroundWindow();
                if (currentForeground == hwnd) return; // Success
                
                // If we get here, the window wasn't switched - retry or give up
                if (retry < maxRetries - 1) continue;
            }
        }

        private static IntPtr GetCachedHwnd(string name)
        {
            long now = Environment.TickCount64;
            if (_cache.TryGetValue(name, out var entry) && (now - entry.tick) < CACHE_TTL_MS)
                return entry.hwnd;

            // Teuer — aber nur alle 10s
            IntPtr found = FindWindow(null, null);
            // FindWindowByProcName via Enumeration (vermeidet GetProcessesByName)
            IntPtr hwnd = FindWindowByProcessName(name);
            _cache[name] = (hwnd, now);
            return hwnd;
        }

        private static IntPtr FindWindowByProcessName(string procName)
        {
            IntPtr result = IntPtr.Zero;
            EnumWindows((hwnd, _) =>
            {
                GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid == 0) return true;
                try
                {
                    using var p = System.Diagnostics.Process.GetProcessById((int)pid);
                    if (string.Equals(p.ProcessName, procName, StringComparison.OrdinalIgnoreCase))
                    { result = hwnd; return false; }
                }
                catch { }
                return true;
            }, IntPtr.Zero);
            return result;
        }

        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        [DllImport("user32.dll")] private static extern IntPtr SetFocus(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
    }
}
