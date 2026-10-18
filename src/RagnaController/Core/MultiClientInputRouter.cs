using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// TASK-1603: Multi-Client Input Router.
    ///
    /// Sendet Input gezielt an ein bestimmtes RO-Client-Fenster (HWND),
    /// anstatt globale SendInput-Aufrufe zu verwenden die immer nur das
    /// Vordergrund-Fenster treffen.
    ///
    /// Architektur:
    /// - SendKeyToClient(hwnd, key, keyDown): PostMessage WM_KEYDOWN/WM_KEYUP
    ///   → funktioniert auch wenn RO nicht im Vordergrund ist
    ///   → keine Fokus-Diebstahl, kein AttachThreadInput nötig
    /// - SendMouseClickToClient(hwnd, x, y): PostMessage WM_LBUTTONDOWN/UP
    /// - GetClientHwnd(processName): über MultiClientWindowTracker (TASK-1602)
    /// - RoExePath aus Settings: Prozessname aus Dateipfad extrahieren (myRO.exe → myro)
    /// - PreferredClientHwnd (WindowSwitcher.FEAT-015): manuelle Client-Auswahl
    ///
    /// Fallback: SendInput + AttachThreadInput für Spiele die PostMessage blockieren.
    ///
    /// Alle Methoden sind headless-testbar durch INativeMethods-Injektion.
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
        /// Setzt den bevorzugten Client-HWND für einen Prozessnamen.
        /// Wenn gesetzt, wird bei Send*ToClient dieses HWND bevorzugt (sofern alive).
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
        /// oder extrahiert aus Settings.RoExePath).
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
        /// Wird verwendet um Settings.RoExePath in einen Fenster-Filter zu verwandeln.
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

        #region Client-HWND Auflösung

        /// <summary>
        /// Löst den besten Client-HWND für einen Prozessnamen auf.
        /// Reihenfolge:
        /// 1. Bevorzugtes HWND (manuelle Auswahl durch User, SetPreferredClientHwnd)
        /// 2. Erstes lebendes HWND aus MultiClientWindowTracker
        /// 3. IntPtr.Zero (kein Client gefunden)
        /// </summary>
        public IntPtr ResolveClientHwnd(string processName)
        {
            var key = processName.ToLowerInvariant();

            lock (_lock)
            {
                // 1. Bevorzugtes HWND (manuell gesetzt)
                if (_preferredHwndByProcess.TryGetValue(key, out var preferred) && preferred != IntPtr.Zero)
                {
                    if (_windowTracker.IsAlive(preferred))
                        return preferred;

                    // Bevorzugtes HWND ist tot → aus Cache entfernen
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
        /// Löst den Client-HWND für den aktuell aktiven Prozessnamen (aus Settings).
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

        #region Tastatur-Input (PostMessage primär)

        /// <summary>
        /// Sendet Tastendruck an einen bestimmten Client-HWND via PostMessage.
        ///
        /// PostMessage funktioniert auch wenn das Zielfenster nicht im Vordergrund ist,
        /// und stiehlt keinen Fokus. Dies ist der bevorzugte Pfad für Multi-Client-Betrieb.
        ///
        /// Fallback: Wenn PostMessage fehlschlägt (z.B. Spiel blockiert gesendete Nachrichten),
        /// wird SendInput + AttachThreadInput verwendet (erfordert Vordergrund-Fenster kurzzeitig).
        /// </summary>
        public bool SendKeyToClient(IntPtr hwnd, VirtualKey key, bool keyDown)
        {
            if (hwnd == IntPtr.Zero || key == VirtualKey.None)
                return false;

            var vk = (ushort)key;
            var msg = keyDown ? _native.WM_KEYDOWN : _native.WM_KEYUP;
            var lParam = MakeKeyLParam(keyDown ? 0 : 1);

            // Primär: PostMessage (fokus-unabhängig)
            if (_native.PostMessage(hwnd, msg, new IntPtr(vk), new IntPtr(lParam)))
                return true;

            // ERROR-001: PostMessage fehlgeschlagen → Logging
            System.Diagnostics.Debug.WriteLine(
                $"[MultiClientInputRouter] PostMessage WM_KEY{(keyDown ? "DOWN" : "UP")} VK={vk:x} an HWND={hwnd} fehlgeschlagen.");

            // Fallback: SendInput (erfordert Vordergrund-Fenster)
            return SendKeyViaSendInput(key, keyDown);
        }

        /// <summary>
        /// Sendet Tastendruck an den aktiv ausgewählten Client (aus Settings).
        /// </summary>
        public bool SendKeyToActiveClient(VirtualKey key, bool keyDown)
        {
            var hwnd = ResolveActiveClientHwnd();
            if (hwnd == IntPtr.Zero)
                return false;
            return SendKeyToClient(hwnd, key, keyDown);
        }

        #endregion

        #region Maus-Input (PostMessage primär)

        /// <summary>
        /// Sendet Mausklick an einen bestimmten Client-HWND via PostMessage.
        ///
        /// x, y sind Client-Koordinaten (relativ zum Client-Bereich des Fensters).
        /// </summary>
        public bool SendMouseClickToClient(IntPtr hwnd, int x, int y, bool leftButton = true)
        {
            if (hwnd == IntPtr.Zero)
                return false;

            var lParam = MakeLParam(x, y);
            var msgDown = leftButton ? _native.WM_LBUTTONDOWN : _native.WM_RBUTTONDOWN;
            var msgUp = leftButton ? _native.WM_LBUTTONUP : _native.WM_RBUTTONUP;

            // Primär: PostMessage
            if (_native.PostMessage(hwnd, msgDown, new IntPtr(leftButton ? 0x0001 : 0x0002), new IntPtr(lParam)) &&
                _native.PostMessage(hwnd, msgUp, new IntPtr(leftButton ? 0x0000 : 0x0000), new IntPtr(lParam | 0x0002)))
            {
                return true;
            }

            System.Diagnostics.Debug.WriteLine(
                $"[MultiClientInputRouter] PostMessage WM_LBUTTON{(leftButton ? "" : "R")} an HWND={hwnd} fehlgeschlagen.");

            // Fallback nicht verfügbar für Maus (würden Cursor bewegen) → false
            return false;
        }

        /// <summary>
        /// Sendet Mausklick an den aktiv ausgewählten Client.
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
        /// Baut einen lParam Wert für PostMessage Maus-Nachrichten.
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
        /// Baut einen lParam Wert für PostMessage Tastatur-Nachrichten.
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
        /// Fallback: SendInput für Tastatur (erfordert Vordergrund-Fenster / AttachThreadInput).
        /// Hinweis: Der öffentliche INPUT-Typ wird vom INativeMethods-Interface verwendet.
        /// Diese Implementierung nutzt den PostMessage-Pfad als primären Weg;
        /// der SendInput-Fallback ist derzeit deaktiviert (gibt false zurück).
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
