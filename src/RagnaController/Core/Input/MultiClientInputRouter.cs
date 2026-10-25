using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using RagnaController.Models;
using RagnaController.Core;

namespace RagnaController.Core
{
    /// <summary>
    /// TASK-1603: Multi-Client Input Router.
    ///
    /// Sendet Input gezielt an ein bestimmtes RO-Client-Fenster (HWND),
    /// anstatt globale SendInput-Aufrufe to verwenden die immer only das
    /// Vordergrand-Fenster treffen.
    ///
    /// Architektur:
    /// - SendKeyToClient(hwnd, key, keyDown): PostMessage WM_KEYDOWN/WM_KEYUP
    ///   → funktioniert auch if RO nicht im Vordergrand is
    /// → no focus theft, no AttachThreadInput needed
    /// - SendMouseClickToClient(hwnd, x, y): PostMessage WM_LBUTTONDOWN/UP
    /// - GetClientHwnd(processName): about MultiClientWindowTracker (TASK-1602)
    /// - RoExePath aus Settings: Prozessname aus Dateipfad extrahieren (myRO.exe → myro)
    /// - PreferredClientHwnd (WindowSwitcher.FEAT-015): manuelle Client-Auswahl
    ///
    /// Fallback: SendInput + AttachThreadInput for Spiele die PostMessage blockieren.
    ///
    /// All Methoden are headless-testbar durch INativeMethods-Injektion.
    /// </summary>
    /// <summary>
    /// Input processing and routing component.
    /// </summary>
    public sealed class MultiClientInputRouter : IDisposable
    {
        private readonly INativeMethods _native;
        private readonly IProcessNameResolver _processNames;
        private readonly MultiClientWindowTracker _windowTracker;
        private readonly object _lock = new();

        // Prozessname → bevorzugtes HWND (manuelle Auswahl durch User)
        private readonly Dictionary<string, IntPtr> _preferredHwndByProcess = new(StringComparer.OrdinalIgnoreCase);

        // Aktiver Prozessname (aus Settings RoExePath / FocusLockProcess)
        private string? _activeProcessName;

        /// <summary>
        /// Setzt den bevorzugten Client-HWND for einen Prozessnamen.
        /// If gesetzt, is bei Send*ToClient this HWND bevorzugt (sofern alive).
        /// </summary>
        public void SetPreferredClientHwnd(string processName, IntPtr hwnd)
        {
            lock (_lock)
            {
                _preferredHwndByProcess[processName.ToLowerInvariant()] = hwnd;
            }
        }

        /// <summary>
        /// Setzt den aktiven Prozessname (z.B. "ragexe" aus Settings.FocusLockProcess
        /// or extrahiert aus Settings.RoExePath).
        /// </summary>
        public void SetActiveProcessName(string? processName)
        {
            lock (_lock)
            {
                _activeProcessName = string.IsNullOrEmpty(processName) ? null : processName.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Extrahiert den Prozessnamen aus einem Dateipfad (z.B. "C:\RO\myRO.exe" → "myro").
        /// Is verwendet um Settings.RoExePath in einen Fenster-Filter to verwandeln.
        /// </summary>
        public static string? ExtractProcessNameFromPath(string? exePath)
        {
            if (string.IsNullOrWhiteSpace(exePath))
                return null;

            try
            {
                var fileName = Path.GetFileName(exePath); // myRO.exe
                if (string.IsNullOrEmpty(fileName))
                    return null;

                // .exe Extension entfernen
                if (fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    fileName = fileName[0..^4];

                return fileName.ToLowerInvariant();
            }
            catch
            {
                return null;
            }
        }

        public MultiClientInputRouter(INativeMethods native, IProcessNameResolver processNames, MultiClientWindowTracker windowTracker)
        {
            _native = native ?? throw new ArgumentNullException(nameof(native));
            _processNames = processNames ?? throw new ArgumentNullException(nameof(processNames));
            _windowTracker = windowTracker ?? throw new ArgumentNullException(nameof(windowTracker));
        }

        #region Client HWND resolution

        /// <summary>
        /// Resolves best client HWND for a process name.
        /// Reihenfolge:
        /// 1. Bevorzugtes HWND (manuelle Auswahl durch User, SetPreferredClientHwnd)
        /// 2. Erstes lebendes HWND aus MultiClientWindowTracker
        /// 3. IntPtr.Zero (no Client gefunden)
        /// </summary>
        public IntPtr ResolveClientHwnd(string processName)
        {
            var key = processName.ToLowerInvariant();

            lock (_lock)
            {
                // 1. Bevorzugtes HWND (manual gesetzt)
                if (_preferredHwndByProcess.TryGetValue(key, out var preferred) && preferred != IntPtr.Zero)
                {
                    if (_windowTracker.IsAlive(preferred))
                        return preferred;

                    // Bevorzugtes HWND is tot → aus Cache entfernen
                    _preferredHwndByProcess.Remove(key);
                }
            }

            // 2. Erstes lebendes HWND aus Tracker
            var clients = _windowTracker.GetClients(processName);
            foreach (var client in clients)
            {
                if (client.IsAlive)
                    return client.Hwnd;
            }

            // 3. Nicht gefunden
            return IntPtr.Zero;
        }

        /// <summary>
        /// Resolves client HWND for currently active process name (from Settings).
        /// </summary>
        public IntPtr ResolveActiveClientHwnd()
        {
            lock (_lock)
            {
                if (_activeProcessName == null)
                    return IntPtr.Zero;
                return ResolveClientHwnd(_activeProcessName);
            }
        }

        #endregion

        #region Keyboard input (PostMessage primary)

        /// <summary>
        /// Sendet Tastendruck an einen bestimmten Client-HWND via PostMessage.
        ///
        /// PostMessage funktioniert auch if das Zielfenster nicht im Vordergrand is,
        /// and stiehlt keinen Fokus. Dies is der bevorzugte Pfad for Multi-Client-Betrieb.
        ///
        /// Fallback: if PostMessage fails (e.g. game blocks sent messages),
        /// is SendInput + AttachThreadInput verwendet (erfordert Vordergrand-Fenster kurzzeitig).
        /// </summary>
        public bool SendKeyToClient(IntPtr hwnd, VirtualKey key, bool keyDown)
        {
            if (hwnd == IntPtr.Zero || key == VirtualKey.None)
                return false;

            var vk = (ushort)key;
            var msg = keyDown ? _native.WM_KEYDOWN : _native.WM_KEYUP;
            var lParam = MakeKeyLParam(keyDown ? 0 : 1);

            // Primary: PostMessage (focus-independent)
            if (_native.PostMessage(hwnd, msg, new IntPtr(vk), new IntPtr(lParam)))
                return true;

            // ERROR-001: PostMessage fehlgeschlagen → Logging
            System.Diagnostics.Debug.WriteLine(
                $"[MultiClientInputRouter] PostMessage WM_KEY{(keyDown ? "DOWN" : "UP")} VK={vk:x} an HWND={hwnd} fehlgeschlagen.");

            // Fallback: SendInput (erfordert Vordergrand-Fenster)
            return SendKeyViaSendInput(key, keyDown);
        }

        /// <summary>
        /// Sends key press to active selected client (from Settings).
        /// </summary>
        public bool SendKeyToActiveClient(VirtualKey key, bool keyDown)
        {
            var hwnd = ResolveActiveClientHwnd();
            if (hwnd == IntPtr.Zero)
                return false;
            return SendKeyToClient(hwnd, key, keyDown);
        }

        #endregion

        #region Mouse input (PostMessage primary)

        /// <summary>
        /// Sendet Mausklick an einen bestimmten Client-HWND via PostMessage.
        ///
        /// x, y are Client-Koordinaten (relativ zum Client-Bereich des Fensters).
        /// </summary>
        public bool SendMouseClickToClient(IntPtr hwnd, int x, int y, bool leftButton = true)
        {
            if (hwnd == IntPtr.Zero)
                return false;

            var lParam = MakeLParam(x, y);
            var msgDown = leftButton ? _native.WM_LBUTTONDOWN : _native.WM_RBUTTONDOWN;
            var msgUp = leftButton ? _native.WM_LBUTTONUP : _native.WM_RBUTTONUP;

            // Primary: PostMessage
            if (_native.PostMessage(hwnd, msgDown, new IntPtr(leftButton ? 0x0001 : 0x0002), new IntPtr(lParam)) &&
                _native.PostMessage(hwnd, msgUp, new IntPtr(leftButton ? 0x0000 : 0x0000), new IntPtr(lParam | 0x0002)))
            {
                return true;
            }

            System.Diagnostics.Debug.WriteLine(
                $"[MultiClientInputRouter] PostMessage WM_LBUTTON{(leftButton ? "" : "R")} an HWND={hwnd} fehlgeschlagen.");

            // Fallback not available for mouse (would move cursor) → false
            return false;
        }

        /// <summary>
        /// Sendet Mausklick an den active ausgewählten Client.
        /// </summary>
        public bool SendMouseClickToActiveClient(int x, int y, bool leftButton = true)
        {
            var hwnd = ResolveActiveClientHwnd();
            if (hwnd == IntPtr.Zero)
                return false;
            return SendMouseClickToClient(hwnd, x, y, leftButton);
        }

        #endregion

        #region Helfer

        /// <summary>
        /// Baut einen lParam Wert for PostMessage Maus-Nachrichten.
        /// LOWORD = x (signed 16-bit), HIWORD = y (signed 16-bit).
        /// </summary>
        private static int MakeLParam(int x, int y)
        {
            unchecked
            {
                return (x & 0xFFFF) | ((y & 0xFFFF) << 16);
            }
        }

        /// <summary>
        /// Baut einen lParam Wert for PostMessage Tastatur-Nachrichten.
        /// Bit 30 = Übergangszustand (1 bei KeyUp, 0 bei KeyDown).
        /// </summary>
        private static int MakeKeyLParam(int transitionState)
        {
            unchecked
            {
                return (transitionState & 1) << 31;
            }
        }

        /// <summary>
        /// Fallback: SendInput for Tastatur (erfordert Vordergrand-Fenster / AttachThreadInput).
        /// Note: The öffentliche INPUT-Typ is vom INativeMethods-Interface verwendet.
        /// These Implementierung nutzt den PostMessage-Pfad als primären Weg;
        /// der SendInput-Fallback is derzeit disabled (gibt false zurück).
        /// </summary>
        private bool SendKeyViaSendInput(VirtualKey key, bool keyDown)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[MultiClientInputRouter] SendInput Fallback nicht verfügbar für VK={key}. PostMessage wird primär verwendet.");
            return false;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _preferredHwndByProcess.Clear();
            }
        }
        #endregion
    }
}
