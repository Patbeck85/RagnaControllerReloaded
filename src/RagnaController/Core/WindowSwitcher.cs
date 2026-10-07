using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RagnaController.Core
{
    /// <summary>
    /// Fokussiert ein Fenster per Prozessname.
    /// Gecachte Prozess-HWND — kein GetProcessesByName() bei jedem Aufruf.
    /// TECH-020 Fix: PID→ProcessName Cache wird einmal pro TTL gebaut (keine Allokationen im EnumWindows-Callback).
    /// </summary>
    public static class WindowSwitcher
    {
        private const int SW_RESTORE = 9;

        // HWND-Cache: ProcessName → (hwnd, lastChecked)
        private static readonly Dictionary<string, (IntPtr hwnd, long tick)> _cache = new();
        private const long CACHE_TTL_MS = 10_000; // alle 10s verifizieren

        // TECH-020: PID → ProcessName Cache (einmal pro TTL, nicht pro EnumWindow-Callback)
        private static readonly Dictionary<int, string> _pidNameCache = new();
        private static long _pidNameCacheTick = 0;
        private const long PID_NAME_CACHE_TTL_MS = 10_000; // synchron mit HWND-Cache

        /// <summary>
        /// FEAT-015: Explizites Ziel-Fenster für Multi-Client-Betrieb (Alt-Char, Farming).
        /// Wenn gesetzt und das Fenster noch existiert, werden alle SwitchWindow-Aktionen an
        /// genau dieses HWND geroutet; sonst Fallback auf die Prozessname-Auflösung (Legacy).
        /// YAGNI: Override-Punkt für einen späteren Client-Selector — bewusst ohne UI/Settings.
        /// Thread-Hinweis: wird vom UI-Thread gesetzt, vom Switch-Task gelesen (benigne Race).
        /// </summary>
        public static IntPtr? PreferredClientHwnd { get; set; }

        public static async Task ToggleAsync(string processName)
            => await ToggleAsync(processName, PreferredClientHwnd ?? IntPtr.Zero);

        public static async Task ToggleAsync(string processName, IntPtr preferredHwnd)
        {
            // FIX: Atomare Window-Switching mit Retry-Logik gegen Race Conditions
            const int maxRetries = 3;
            for (int retry = 0; retry < maxRetries; retry++)
            {
                IntPtr hwnd = SelectTargetHwnd(processName, preferredHwnd, IsWindow);
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

        /// <summary>
        /// FEAT-015: Reine Ziel-Auswahl (testbar ohne Win32-Seitenwirkung).
        /// 1) PreferredHwnd existiert noch → wird verwendet (Multi-Client-Routing).
        /// 2) Sonst Legacy: gecachte Prozessname-Auflösung.
        /// </summary>
        internal static IntPtr SelectTargetHwnd(string processName, IntPtr preferredHwnd, Func<IntPtr, bool>? aliveCheck = null)
        {
            if (preferredHwnd != IntPtr.Zero && (aliveCheck == null || aliveCheck(preferredHwnd)))
                return preferredHwnd;

            // Legacy-Pfad: Prozessname-Auflösung (gewollt auch bei ungültigem PreferredHwnd)
            return GetCachedHwnd(processName);
        }

        private static IntPtr GetCachedHwnd(string name)
        {
            long now = Environment.TickCount64;
            if (_cache.TryGetValue(name, out var entry) && (now - entry.tick) < CACHE_TTL_MS)
                return entry.hwnd;

            // TECH-020: PID→Name Cache einmalig aufbauen (nicht pro Callback)
            RefreshPidNameCache(now);

            // Teuer — aber nur alle 10s
            IntPtr found = FindWindow(null, null);
            IntPtr hwnd = FindWindowByProcessName(name);
            _cache[name] = (hwnd, now);
            return hwnd;
        }

        /// <summary>
        /// TECH-020: Baut PID→ProcessName Mapping einmal pro TTL auf.
        /// Vermeidet Process.GetProcessById() Allokationen im EnumWindows-Callback.
        /// </summary>
        private static void RefreshPidNameCache(long now)
        {
            if ((now - _pidNameCacheTick) < PID_NAME_CACHE_TTL_MS && _pidNameCache.Count > 0)
                return;

            _pidNameCache.Clear();
            try
            {
                // Einmalig alle Prozesse laden — außerhalb des Hot Paths
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        _pidNameCache[p.Id] = p.ProcessName;
                    }
                    catch { /* Access denied — ignorieren */ }
                    finally
                    {
                        p.Dispose();
                    }
                }
                _pidNameCacheTick = now;
            }
            catch { /* System.Diagnostics nicht verfügbar — Fallback leer lassen */ }
        }

        private static IntPtr FindWindowByProcessName(string procName)
        {
            IntPtr result = IntPtr.Zero;
            EnumWindows((hwnd, _) =>
            {
                GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid == 0) return true;

                // TECH-020: Lookup im vorgefertigten Cache (keine Allokation!)
                if (_pidNameCache.TryGetValue((int)pid, out var cachedName) &&
                    string.Equals(cachedName, procName, StringComparison.OrdinalIgnoreCase))
                {
                    result = hwnd;
                    return false; // Enum stoppen
                }
                return true;
            }, IntPtr.Zero);
            return result;
        }

        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
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
