using System;
using System.Diagnostics;

namespace RagnaController.Core
{
    /// <summary>
    /// DualSense Light Bar Status — PS5 LED Farben via Raw HID (Platzhalter für Hardware-Integration).
    /// Unterstützt Farbwechsel und Puls-Effekte für Spiel-Status-Feedback.
    /// </summary>
    public sealed class DualSenseLightbarService : IDisposable
    {
        private bool _disposed;
        private bool _isConnected;
        private byte _red;
        private byte _green;
        private byte _blue;

        public bool IsConnected => _isConnected;

        public byte Red => _red;
        public byte Green => _green;
        public byte Blue => _blue;

        public DualSenseLightbarService()
        {
            // Platzhalter: Keine echte Hardware-Verbindung in dieser Implementierung
            _isConnected = false;
            _red = 0;
            _green = 0;
            _blue = 0;
        }

        /// <summary>
        /// Setzt die Light Bar Farbe (RGB).
        /// Gibt true zurück wenn die Farbe erfolgreich gesetzt wurde (oder im Simulations-Modus).
        /// </summary>
        public bool SetColor(byte red, byte green, byte blue)
        {
            if (_disposed) return false;
            _red = red;
            _green = green;
            _blue = blue;
            Debug.WriteLine($"[DualSenseLightbar] Farbe gesetzt: RGB({red}, {green}, {blue})");
            return true;
        }

        /// <summary>
        /// Setzt ein vordefiniertes Farbprofil basierend auf Spiel-Status.
        /// </summary>
        public bool SetStatusColor(LightBarStatus status)
        {
            if (_disposed) return false;
            switch (status)
            {
                case LightBarStatus.Idle:
                    return SetColor(100, 100, 100);      // Grau
                case LightBarStatus.Active:
                    return SetColor(0, 255, 0);           // Grün
                case LightBarStatus.Combat:
                    return SetColor(255, 0, 0);           // Rot
                case LightBarStatus.Casting:
                    return SetColor(0, 0, 255);           // Blau
                case LightBarStatus.LowHealth:
                    return SetColor(255, 165, 0);         // Orange
                case LightBarStatus.Dead:
                    return SetColor(128, 0, 128);         // Lila
                default:
                    return SetColor(100, 100, 100);
            }
        }

        /// <summary>
        /// Startet einen Puls-Effekt (Platzhalter).
        /// </summary>
        public bool Pulse(byte red, byte green, byte blue, int durationMs)
        {
            if (_disposed) return false;
            Debug.WriteLine($"[DualSenseLightbar] Puls: RGB({red}, {green}, {blue}) für {durationMs}ms");
            return SetColor(red, green, blue);
        }

        /// <summary>
        /// Schaltet die Light Bar aus.
        /// </summary>
        public bool TurnOff()
        {
            return SetColor(0, 0, 0);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            TurnOff();
        }
    }

    /// <summary>
    /// Verfügbare Light Bar Status-Farben für DualSense Feedback.
    /// </summary>
    public enum LightBarStatus
    {
        Idle,
        Active,
        Combat,
        Casting,
        LowHealth,
        Dead
    }
}
