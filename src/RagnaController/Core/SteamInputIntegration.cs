using System;
using System.Collections.Generic;

namespace RagnaController.Core
{
    /// <summary>
    /// TASK-2406: SteamInputIntegration — Abstraktion über die Steam Input API.
    ///
    /// Ermöglicht die Nutzung von Steam Input für Controller-Eingaben, falls
    /// das Spiel über Steam läuft. Bietet einen Fallback-Modus ohne Steam,
    /// sodass die Anwendung auch ohne Steam-Client funktioniert.
    ///
    /// Hinweis: Die echte Steamworks.NET Integration erfordert das
    /// Steamworks.NET NuGet-Paket und einen gültigen Steam App-ID. Diese
    /// Implementierung bietet die Schnittstelle und einen Stub ohne Steam.
    /// </summary>
    public sealed class SteamInputIntegration : IDisposable
    {
        /// <summary>Ob Steam Input verfügbar ist (Steam läuft und API initialisiert).</summary>
        public bool IsSteamAvailable { get; private set; }

        /// <summary>Ob ein Controller über Steam Input erkannt wurde.</summary>
        public bool IsControllerConnected { get; private set; }

        /// <summary>Event bei Controller-Verbindung.</summary>
        public event EventHandler? ControllerConnected;

        /// <summary>Event bei Controller-Trennung.</summary>
        public event EventHandler? ControllerDisconnected;

        private bool _disposed;

        /// <summary>Initialisiert die Steam Input Integration.</summary>
        public SteamInputIntegration()
        {
            TryInitializeSteam();
        }

        /// <summary>Versucht, die Steam Input API zu initialisieren.</summary>
        private void TryInitializeSteam()
        {
            try
            {
                // Hinweis: Echte Steamworks.NET Integration würde hier erfolgen:
                // SteamAPI.Init();
                // SteamInput.Init();
                //
                // Da Steamworks.NET nicht als Dependency hinzugefügt wurde,
                // bleibt IsSteamAvailable = false (Fallback-Modus).

                IsSteamAvailable = false;
                IsControllerConnected = false;
            }
            catch
            {
                IsSteamAvailable = false;
                IsControllerConnected = false;
            }
        }

        /// <summary>
        /// Ruft den aktuellen Controller-State ab.
        /// Im Fallback-Modus werden alle Werte auf 0/false gesetzt.
        /// </summary>
        public ControllerState GetControllerState(uint controllerIndex = 0)
        {
            if (!IsSteamAvailable)
                return new ControllerState();

            // Echte Implementierung würde hier SteamInput.GetDigitalActionData etc. aufrufen
            return new ControllerState();
        }

        /// <summary>
        /// Wechselt das aktive Action-Set für einen Controller.
        /// </summary>
        public bool SetActionSet(uint controllerIndex, string actionSetName)
        {
            if (!IsSteamAvailable)
                return false;

            // Echte Implementierung: SteamInput.SetActionSet()
            return false;
        }

        /// <summary>
        /// Löst eine Vibration auf dem Controller aus.
        /// </summary>
        public bool TriggerVibration(uint controllerIndex, float leftSpeed, float rightSpeed, ushort durationMs)
        {
            if (!IsSteamAvailable)
                return false;

            // Echte Implementierung: SteamInput.TriggerVibration()
            return false;
        }

        /// <summary>
        /// Prüft, ob eine digitale Aktion gedrückt ist.
        /// </summary>
        public bool GetDigitalAction(uint controllerIndex, string actionName)
        {
            if (!IsSteamAvailable)
                return false;

            // Echte Implementierung: SteamInput.GetDigitalActionData()
            return false;
        }

        /// <summary>
        /// Ruft den Wert einer analogen Aktion ab (0.0 bis 1.0).
        /// </summary>
        public float GetAnalogAction(uint controllerIndex, string actionName)
        {
            if (!IsSteamAvailable)
                return 0f;

            // Echte Implementierung: SteamInput.GetAnalogActionData()
            return 0f;
        }

        /// <summary>Aktualisiert den Steam Input State (sollte pro Frame aufgerufen werden).</summary>
        public void RunFrame()
        {
            if (!IsSteamAvailable)
                return;

            // Echte Implementierung: SteamInput.RunFrame()
            // + Controller-Verbindungs-Events verarbeiten
        }

        /// <summary>Gibt alle Ressourcen frei.</summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                IsSteamAvailable = false;
                IsControllerConnected = false;
            }
        }
    }

    /// <summary>
    /// Repräsentiert den aktuellen Zustand eines Controllers.
    /// </summary>
    public struct ControllerState
    {
        /// <summary>Linker Stick X (-1.0 bis 1.0).</summary>
        public float LeftStickX;

        /// <summary>Linker Stick Y (-1.0 bis 1.0).</summary>
        public float LeftStickY;

        /// <summary>Rechter Stick X (-1.0 bis 1.0).</summary>
        public float RightStickX;

        /// <summary>Rechter Stick Y (-1.0 bis 1.0).</summary>
        public float RightStickY;

        /// <summary>Linker Trigger (0.0 bis 1.0).</summary>
        public float LeftTrigger;

        /// <summary>Rechter Trigger (0.0 bis 1.0).</summary>
        public float RightTrigger;

        /// <summary>D-Pad Oben.</summary>
        public bool DPadUp;

        /// <summary>D-Pad Unten.</summary>
        public bool DPadDown;

        /// <summary>D-Pad Links.</summary>
        public bool DPadLeft;

        /// <summary>D-Pad Rechts.</summary>
        public bool DPadRight;

        /// <summary>Alle Buttons als Bitmaske für einfache Prüfung.</summary>
        public ulong ButtonMask;

        /// <summary>Überprüft, ob ein bestimmter Button gedrückt ist.</summary>
        public bool IsButtonPressed(ulong buttonMask) => (ButtonMask & buttonMask) != 0;
    }
}
