using System;

namespace RagnaController.Core.Hardware {
    /// <summary>
    /// TASK-2405: MotionAimAssist — Gyro-gestützte Zielunterstützung (Motion Aim).
    ///
    /// Wandelt Gyro-Sensordaten (Pitch, Yaw, Roll) in einen Ziel-Versatz um,
    /// der für präzises Zielen mit dem Controller verwendet werden kann (z.B.
    /// Bogen-Zielen, Zauber-Zielen, Feinjustierung der Kreuzhairs).
    ///
    /// Algorithmus:
    ///   1. Gyro-Raten (Grad/s) werden über einen Zeitintervall integriert
    ///   2. Deadzone filtert kleine unbeabsichtigte Bewegungen
    ///   3. Sensitivity skaliert den Versatz
    ///   4. Smoothing (Exponential Moving Average) entfernt Ruckler
    ///   5. Output wird auf einen konfigurierbaren Max-Bereich geklemmt
    /// </summary>
    public sealed class MotionAimAssist
    {
        /// <summary>Aktuelle Gyro-Rate für Pitch (X-Achse, auf/ab) in Grad/s.</summary>
        public float GyroPitchRate { get; set; }

        /// <summary>Aktuelle Gyro-Rate für Yaw (Y-Achse, links/rechts) in Grad/s.</summary>
        public float GyroYawRate { get; set; }

        /// <summary>Aktuelle Gyro-Rate für Roll (Z-Achse, rotieren) in Grad/s.</summary>
        public float GyroRollRate { get; set; }

        /// <summary>Empfindlichkeit für die Gyro-Umsetzung (Standard: 1.0).</summary>
        public float Sensitivity { get; set; } = 1.0f;

        /// <summary>Deadzone in Grad/s. Bewegungen darunter werden ignoriert.</summary>
        public float Deadzone { get; set; } = 2.0f;

        /// <summary>Maximaler Output-Versatz (z.B. Bildschirm-Pixel oder Normalisiert).</summary>
        public float MaxOutput { get; set; } = 50.0f;

        /// <summary>Smoothing-Faktor (0.0 = kein Smoothing, 1.0 = starkes Smoothing).</summary>
        public float Smoothing { get; set; } = 0.3f;

        /// <summary>Aktueller berechneter X-Versatz (Output).</summary>
        public float OutputX { get; private set; }

        /// <summary>Aktueller berechneter Y-Versatz (Output).</summary>
        public float OutputY { get; private set; }

        /// <summary>Ob die Motion Aim aktiviert ist.</summary>
        public bool IsEnabled { get; set; } = true;

        private float _smoothedX = 0f;
        private float _smoothedY = 0f;
        private DateTime _lastUpdate = DateTime.MinValue;

        /// <summary>
        /// Aktualisiert die Motion Aim Berechnung mit neuen Gyro-Daten.
        /// Sollte pro Frame (oder bei jedem neuen Gyro-Sample) aufgerufen werden.
        /// </summary>
        /// <param name="pitchRate">Pitch-Rate in Grad/s.</param>
        /// <param name="yawRate">Yaw-Rate in Grad/s.</param>
        /// <param name="rollRate">Roll-Rate in Grad/s (wird ignoriert, aber gespeichert).</param>
        /// <param name="deltaTime">Zeit seit letztem Update in Sekunden.</param>
        public void Update(float pitchRate, float yawRate, float rollRate, float deltaTime)
        {
            GyroPitchRate = pitchRate;
            GyroYawRate = yawRate;
            GyroRollRate = rollRate;

            if (!IsEnabled || deltaTime <= 0f)
                return;

            // 1. Integration: Rate * Zeit = Winkel-Änderung
            float pitchDelta = pitchRate * deltaTime;
            float yawDelta = yawRate * deltaTime;

            // 2. Deadzone anwenden
            pitchDelta = ApplyDeadzone(pitchDelta, Deadzone * deltaTime);
            yawDelta = ApplyDeadzone(yawDelta, Deadzone * deltaTime);

            // 3. Sensitivity skalieren
            pitchDelta *= Sensitivity;
            yawDelta *= Sensitivity;

            // 4. Smoothing (Exponential Moving Average)
            _smoothedX = Lerp(_smoothedX, yawDelta, Smoothing);
            _smoothedY = Lerp(_smoothedY, pitchDelta, Smoothing);

            // 5. Output klemmen
            OutputX = Math.Clamp(_smoothedX, -MaxOutput, MaxOutput);
            OutputY = Math.Clamp(_smoothedY, -MaxOutput, MaxOutput);

            _lastUpdate = DateTime.Now;
        }

        /// <summary>Setzt den Output auf 0 zurück.</summary>
        public void Reset()
        {
            OutputX = 0f;
            OutputY = 0f;
            _smoothedX = 0f;
            _smoothedY = 0f;
        }

        private static float ApplyDeadzone(float value, float deadzone)
        {
            if (Math.Abs(value) < deadzone)
                return 0f;
            return value > 0 ? value - deadzone : value + deadzone;
        }

        private static float Lerp(float current, float target, float factor)
        {
            return current + (target - current) * factor;
        }
    }
}
