using System;

namespace RagnaController.Core.Hardware {
    /// <summary>
    /// TASK-2403: TriggerDeadzoneCurve — Nicht-lineare Kurven für Analog-Trigger.
    ///
    /// Ermöglicht die Konfiguration von Trigger-Reaktionen für verschiedene Spielszenarien:
    ///   • Linear: 1:1 Mapping (Standard)
    ///   • Quadratic: Sanfter Beginn, steileres Ende (gut für Bogen-Laden)
    ///   • Exponential: Sehr sanfter Beginn, sehr steiles Ende (gut für Zauber-Laden)
    ///   • SCurve: Sanfter Beginn und Ende, steiler Mitte (ausgewogen)
    ///
    /// Zusätzlich gibt es einen Deadzone-Bereich (unterhalb dessen der Wert 0 ist)
    /// und einen Sättigungs-Bereich (oberhalb dessen der Wert 1.0 ist).
    /// </summary>
    public sealed class TriggerDeadzoneCurve
    {
        /// <summary>Kurven-Typ für die nicht-lineare Abbildung.</summary>
        public enum CurveType
        {
            /// <summary>Linear: Wert wird 1:1 übergeben (nach Deadzone).</summary>
            Linear,
            /// <summary>Quadratic: x² — sanfter Anfang, steileres Ende.</summary>
            Quadratic,
            /// <summary>Exponential: x³ — sehr sanfter Anfang, sehr steiles Ende.</summary>
            Exponential,
            /// <summary>SCurve: Sinus-basiert — sanfter Beginn und Ende.</summary>
            SCurve,
        }

        /// <summary>Welche Kurve verwendet werden soll.</summary>
        public CurveType Type { get; set; } = CurveType.Linear;

        /// <summary>Deadzone in Prozent (0.0 bis 1.0). Unterhalb dieses Werts ist der Output 0.</summary>
        public float Deadzone { get; set; } = 0.10f;

        /// <summary>Sättigungspunkt in Prozent (0.0 bis 1.0). Oberhalb ist der Output 1.0.</summary>
        public float Saturation { get; set; } = 1.00f;

        /// <summary>Ob die Kurve invertiert werden soll (1.0 wird zu 0.0 und umgekehrt).</summary>
        public bool Inverted { get; set; } = false;

        /// <summary>
        /// Wendet die Kurve auf einen rohen Trigger-Wert (0.0 bis 1.0) an.
        /// </summary>
        /// <param name="raw">Rohwert des Triggers (0.0 bis 1.0).</param>
        /// <returns>Kompensierter Wert (0.0 bis 1.0).</returns>
        public float Apply(float raw)
        {
            // 1. Invertieren falls gewünscht
            float value = Inverted ? 1.0f - raw : raw;

            // 2. Deadzone anwenden
            if (value <= Deadzone)
                return 0.0f;

            // 3. Im Bereich [Deadzone, Saturation] normalisieren
            float range = Saturation - Deadzone;
            if (range <= 0.001f)
                return 1.0f;

            float normalized = (value - Deadzone) / range;
            normalized = Math.Clamp(normalized, 0.0f, 1.0f);

            // 4. Kurve anwenden
            float curved = ApplyCurve(normalized);

            // 5. Zurück in [0.0, 1.0] klemmen
            return Math.Clamp(curved, 0.0f, 1.0f);
        }

        /// <summary>
        /// Wendet die gewählte Kurvenfunktion auf einen normalisierten Wert [0,1] an.
        /// </summary>
        private float ApplyCurve(float x)
        {
            return Type switch
            {
                CurveType.Linear => x,
                CurveType.Quadratic => x * x,
                CurveType.Exponential => x * x * x,
                CurveType.SCurve => 0.5f - 0.5f * MathF.Cos(x * MathF.PI),
                _ => x,
            };
        }

        /// <summary>
        /// Erstellt eine Kurve für Bogen-Laden (sanfter Anfang, präzise Kontrolle bei voller Spannung).
        /// </summary>
        public static TriggerDeadzoneCurve CreateBowCurve()
        {
            return new TriggerDeadzoneCurve
            {
                Type = CurveType.Quadratic,
                Deadzone = 0.05f,
                Saturation = 0.95f,
                Inverted = false,
            };
        }

        /// <summary>
        /// Erstellt eine Kurve für Zauber-Laden (sehr sanfter Anfang, schnelles Ende).
        /// </summary>
        public static TriggerDeadzoneCurve CreateSpellChargeCurve()
        {
            return new TriggerDeadzoneCurve
            {
                Type = CurveType.Exponential,
                Deadzone = 0.08f,
                Saturation = 0.90f,
                Inverted = false,
            };
        }

        /// <summary>
        /// Erstellt eine Standard-Lineare Kurve.
        /// </summary>
        public static TriggerDeadzoneCurve CreateLinearCurve()
        {
            return new TriggerDeadzoneCurve
            {
                Type = CurveType.Linear,
                Deadzone = 0.05f,
                Saturation = 1.00f,
                Inverted = false,
            };
        }

        /// <summary>
        /// Erstellt eine SCurve für ausgewogene Reaktion.
        /// </summary>
        public static TriggerDeadzoneCurve CreateSCurve()
        {
            return new TriggerDeadzoneCurve
            {
                Type = CurveType.SCurve,
                Deadzone = 0.05f,
                Saturation = 0.95f,
                Inverted = false,
            };
        }
    }
}
