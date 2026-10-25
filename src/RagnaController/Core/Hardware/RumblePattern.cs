using System;
using System.Collections.Generic;

namespace RagnaController.Core.Hardware {
    /// <summary>
    /// TASK-2404: RumblePattern — Definition eines Rumble-Vibrationsmusters für den DualSense-Controller.
    ///
    /// Ein Rumble-Pattern besteht aus einer Wellform (Sinus, Rechteck, Sägezahn, Impuls),
    /// einer Frequenz (Hz), einer Amplitude (0.0 bis 1.0) und einer Dauer (ms).
    /// Mehrere Patterns können zu einer Sequenz verkettet werden für komplexe Effekte.
    /// </summary>
    public sealed class RumblePattern
    {
        /// <summary>Name des Patterns (z.B. "Schwertschlag", "Explosion", "Sanftes Vibrieren").</summary>
        public string Name { get; set; } = "Neues Pattern";

        /// <summary>Beschreibung des Patterns.</summary>
        public string Description { get; set; } = "";

        /// <summary>Wellenform-Typ für die Vibration.</summary>
        public WaveformType Waveform { get; set; } = WaveformType.Sine;

        /// <summary>Frequenz in Hertz (1.0 bis 100.0). Höher = schnelleres Vibrieren.</summary>
        public float Frequency { get; set; } = 10.0f;

        /// <summary>Amplitude in Prozent (0.0 bis 1.0). Höher = stärkeres Vibrieren.</summary>
        public float Amplitude { get; set; } = 0.5f;

        /// <summary>Dauer in Millisekunden (50 bis 5000).</summary>
        public int DurationMs { get; set; } = 200;

        /// <summary>Ob das Pattern im Loop abgespielt werden soll.</summary>
        public bool Loop { get; set; } = false;

        /// <summary>Einmalige Wiedergabe-Anzahl (1 = einmalig, 0 = unendlich wenn Loop=true).</summary>
        public int RepeatCount { get; set; } = 1;

        /// <summary>
        /// Gibt einen beschreibenden Namen für das Pattern zurück.
        /// </summary>
        public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name : "Unnamed Pattern";

        /// <summary>
        /// Erstellt ein Deep-Kopie dieses Patterns.
        /// </summary>
        public RumblePattern Clone()
        {
            return new RumblePattern
            {
                Name = Name,
                Description = Description,
                Waveform = Waveform,
                Frequency = Frequency,
                Amplitude = Amplitude,
                DurationMs = DurationMs,
                Loop = Loop,
                RepeatCount = RepeatCount,
            };
        }
    }

    /// <summary>
    /// Unterstützte Wellenform-Typen für Rumble-Patterns.
    /// </summary>
    public enum WaveformType
    {
        /// <summary>Sinuswelle — sanftes, wellenförmiges Vibrieren.</summary>
        Sine,
        /// <summary>Rechteckwelle — hartes, schnelles Ein/Aus-Vibrieren.</summary>
        Square,
        /// <summary>Sägezahnwelle — linearer Anstieg, schneller Abfall.</summary>
        Sawtooth,
        /// <summary>Impuls — kurzer starker Schlag mit langer Pause.</summary>
        Pulse,
        /// <summary>Doppel-Impuls — zwei schnelle Schläge hintereinander.</summary>
        DoublePulse,
    }
}
