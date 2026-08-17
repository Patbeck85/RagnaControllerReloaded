using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RagnaController.Core
{
    /// <summary>
    /// Verfolgt das RO-Client-Fenster via WinEvent-Hook.
    /// Liefert CenterX/Y, ClientW/H und DpiScale für Maus-Koordinaten.
    /// </summary>
    public class WindowTracker : IDisposable
    {
        // ── Win32 WinEvent ────────────────────────────────────────────────
        private delegate void WinEventDelegate(
            IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")] private static extern IntPtr SetWinEventHook(
            uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess,
            uint idThread, uint dwFlags);

        [DllImport("user32.dll")] private static extern bool UnhookWinEvent(IntPtr hWinEventHook);
        [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);

        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint WINEVENT_OUTOFCONTEXT   = 0x0000;

        // ── State ─────────────────────────────────────────────────────────
        private IntPtr  _hook = IntPtr.Zero;
        private IntPtr  _hwnd = IntPtr.Zero;
        private string  _procName = "ragexe";
        private WinEventDelegate? _delegate; // GC-Anker — darf nicht gesammelt werden
        private readonly object _lock = new object();  // FIX: Thread-Safety für Refresh()/UpdateGeometry()

        public int   CenterX   { get; private set; }
        public int   CenterY   { get; private set; }
        public int   ClientW   { get; private set; }
        public int   ClientH   { get; private set; }
        public float DpiScale  { get; private set; } = 1.0f;
        public bool  IsTracking { get; private set; }
        public bool  WindowTracked { get; private set; }

        public event Action? ForegroundChanged;

        public void SetProcessName(string name)
        {
            _procName = name.ToLowerInvariant().Replace(".exe", "");
        }

        // ── Hook starten ──────────────────────────────────────────────────
        public void StartHook()
        {
            if (_hook != IntPtr.Zero) return;
            _delegate = OnWinEvent;  // Referenz halten damit GC sie nicht sammelt
            _hook = SetWinEventHook(
                EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, _delegate, 0, 0, WINEVENT_OUTOFCONTEXT);

            // Sofort einmal prüfen ob RO schon vorne ist
            Refresh();
        }

        public void StopHook()
        {
            if (_hook == IntPtr.Zero) return;
            UnhookWinEvent(_hook);
            _hook     = IntPtr.Zero;
            _delegate = null;
            IsTracking = false;
        }

        // ── WinEvent Callback ─────────────────────────────────────────────
        private void OnWinEvent(IntPtr hHook, uint eventType, IntPtr hwnd,
                                int idObject, int idChild, uint thread, uint time)
        {
            if (eventType != EVENT_SYSTEM_FOREGROUND) return;
            lock (_lock)
            {
                CheckWindow(hwnd);
            }
        }

        // ── Manuell auffrischen (z.B. beim Controller-Connect) ────────────
        public void Refresh()
        {
            lock (_lock)
            {
                IntPtr fg = NativeMethods.GetForegroundWindow();
                if (fg != IntPtr.Zero) CheckWindow(fg);
            }
        }

        private void CheckWindow(IntPtr hwnd)
        {
            NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) { IsTracking = false; return; }

            try
            {
                using var proc = Process.GetProcessById((int)pid);
                bool match = proc.ProcessName.ToLowerInvariant().Contains(_procName);
                if (!match) { IsTracking = false; return; }
            }
            catch { IsTracking = false; return; }

            _hwnd = hwnd;
            UpdateGeometry();
        }

        private void UpdateGeometry()
        {
            if (!NativeMethods.GetClientRect(_hwnd, out NativeMethods.RECT rect)) return;

            NativeMethods.POINT pt = new NativeMethods.POINT { X = 0, Y = 0 };
            NativeMethods.ClientToScreen(_hwnd, ref pt);

            uint dpi = GetDpiForWindow(_hwnd);
            float newScale = dpi > 0 ? dpi / 96.0f : 1.0f;

            int newW = rect.Right  - rect.Left;
            int newH = rect.Bottom - rect.Top;
            int newCx = pt.X + newW / 2;
            int newCy = pt.Y + newH / 2;
            bool nowTracking = newW > 0 && newH > 0;

            // Nur feuern wenn sich etwas tatsächlich geändert hat —
            // verhindert Endlosschleife wenn ForegroundChanged → Refresh() → UpdateGeometry()
            bool changed = nowTracking != IsTracking
                        || newCx != CenterX || newCy != CenterY
                        || newW  != ClientW  || newH  != ClientH
                        || newScale != DpiScale;

            DpiScale   = newScale;
            ClientW    = newW;
            ClientH    = newH;
            CenterX    = newCx;
            CenterY    = newCy;
            IsTracking = nowTracking;

            if (changed)
                ForegroundChanged?.Invoke();
        }

        public void Dispose() => StopHook();
    }
}
