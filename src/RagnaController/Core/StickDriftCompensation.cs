using System;

namespace RagnaController.Core
{
    /// <summary>
    /// TASK-2402: StickDriftCompensation — Automatische Erkennung und Kompensation von Analog-Stick-Drift.
    ///
    /// Wenn ein Analog-Stick in Ruhe nicht exakt bei (0,0) liegt (Herstellungsfehler, Abnutzung),
    /// verursacht das unerwünschte Bewegung im Spiel. Diese Klasse misst den Stillstand-Drift
    /// über mehrere Samples und wendet einen Offset-Abzug an, sodass der Stick im Ruhezustand
    /// exakt (0,0) liefert.
    ///
    /// Verwendung:
    ///   1. Kalibrierung: Calibrate() mit N Samples im Stillstand aufrufen
    ///   2. Kompensation: Compensate() auf jeden Stick-Wert anwenden bevor er verwendet wird
    ///   3. Speicherung: DriftOffsetX/Y nach dem Speichern im Profil persistieren
    /// </summary>
    public sealed class StickDriftCompensation
    {
        /// <summary>Erkannter Drift-Offset für den linken Stick (X-Achse, -1.0 bis 1.0).</summary>
        public float LeftStickDriftX { get; private set; }

        /// <summary>Erkannter Drift-Offset für den linken Stick (Y-Achse, -1.0 bis 1.0).</summary>
        public float LeftStickDriftY { get; private set; }

        /// <summary>Erkannter Drift-Offset für den rechten Stick (X-Achse, -1.0 bis 1.0).</summary>
        public float RightStickDriftX { get; private set; }

        /// <summary>Erkannter Drift-Offset für den rechten Stick (Y-Achse, -1.0 bis 1.0).</summary>
        public float RightStickDriftY { get; private set; }

        /// <summary>Ob eine Kalibrierung durchgeführt wurde.</summary>
        public bool IsCalibrated { get; private set; }

        /// <summary>Maximaler Abweichungswert während der Kalibrierung (für Deadzone-Empfehlung).</summary>
        public float MaxDeviation { get; private set; }

        /// <summary>
        /// Kalibriert den Drift-Offset aus einer Reihe von Stillstand-Samples.
        /// Der Mittelwert aller Samples wird als Drift-Offset gespeichert.
        /// </summary>
        /// <param name="samplesLeftX">Samples des linken Sticks X-Achse im Stillstand.</param>
        /// <param name="samplesLeftY">Samples des linken Sticks Y-Achse im Stillstand.</param>
        /// <param name="samplesRightX">Samples des rechten Sticks X-Achse im Stillstand.</param>
        /// <param name="samplesRightY">Samples des rechten Sticks Y-Achse im Stillstand.</param>
        public void Calibrate(float[] samplesLeftX, float[] samplesLeftY,
                              float[] samplesRightX, float[] samplesRightY)
        {
            if (samplesLeftX == null || samplesLeftY == null ||
                samplesRightX == null || samplesRightY == null)
                throw new ArgumentNullException(nameof(samplesLeftX));

            if (samplesLeftX.Length == 0 || samplesLeftY.Length == 0 ||
                samplesRightX.Length == 0 || samplesRightY.Length == 0)
                throw new ArgumentException("Sample arrays must not be empty", nameof(samplesLeftX));

            LeftStickDriftX = Mean(samplesLeftX);
            LeftStickDriftY = Mean(samplesLeftY);
            RightStickDriftX = Mean(samplesRightX);
            RightStickDriftY = Mean(samplesRightY);

            MaxDeviation = Math.Max(Math.Max(Math.Abs(LeftStickDriftX), Math.Abs(LeftStickDriftY)),
                                    Math.Max(Math.Abs(RightStickDriftX), Math.Abs(RightStickDriftY)));

            IsCalibrated = true;
        }

        /// <summary>
        /// Wendet die Drift-Kompensation auf einen Stick-Wert an.
        /// Der Offset wird abgezogen und das Ergebnis auf [-1.0, 1.0] geklemmt.
        /// </summary>
        /// <param name="rawValue">Rohwert des Sticks (-1.0 bis 1.0).</param>
        /// <param name="offset">Erkannter Drift-Offset (-1.0 bis 1.0).</param>
        /// <returns>Kompensierter Wert (-1.0 bis 1.0).</returns>
        public static float Compensate(float rawValue, float offset)
        {
            // Offset abziehen
            float compensated = rawValue - offset;

            // Auf gültigen Bereich klemmen
            return Math.Clamp(compensated, -1.0f, 1.0f);
        }

        /// <summary>
        /// Setzt die Drift-Offsets manuell (z.B. beim Laden aus dem Profil).
        /// </summary>
        public void SetOffsets(float leftX, float leftY, float rightX, float rightY)
        {
            LeftStickDriftX = leftX;
            LeftStickDriftY = leftY;
            RightStickDriftX = rightX;
            RightStickDriftY = rightY;
            IsCalibrated = true;
        }

        /// <summary>Setzt alle Offsets auf 0 und markiert als nicht kalibriert.</summary>
        public void Reset()
        {
            LeftStickDriftX = 0f;
            LeftStickDriftY = 0f;
            RightStickDriftX = 0f;
            RightStickDriftY = 0f;
            MaxDeviation = 0f;
            IsCalibrated = false;
        }

        private static float Mean(float[] samples)
        {
            double sum = 0d;
            foreach (var s in samples) sum += s;
            return (float)(sum / samples.Length);
        }
    }
}
