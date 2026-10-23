using System;

namespace RagnaController.Models
{
    /// <summary>
    /// Definiert eine Chorded-Input-Kombination (mehrere Buttons gleichzeitig).
    /// Beispiel: L1+R1+A ≠ L1+A + R1+A (eigene Aktion für die Kombination).
    /// </summary>
    public class ChordConfig
    {
        /// <summary>Eindeutiger Name des Chords (z.B. "L1+R1+A_Skill").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Buttons die gleichzeitig gedrückt sein müssen.</summary>
        public GamepadButtonFlags Buttons { get; set; } = GamepadButtonFlags.None;

        /// <summary>Aktion die ausgeführt wird wenn der Chord erkannt wird.</summary>
        public ButtonAction? Action { get; set; }

        /// <summary>Maximales Zeitfenster in ms innerhalb dessen alle Buttons gedrückt sein müssen.</summary>
        public int MaxIntervalMs { get; set; } = 150;

        /// <summary>Ob der Chord nur einmal feuern soll (bis alle Buttons losgelassen).</summary>
        public bool FireOnce { get; set; } = true;

        /// <summary>Ob der Chord aktiviert ist.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Beschreibung für die UI.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Prüft ob alle erforderlichen Buttons im aktuellen Input gedrückt sind.</summary>
        public bool Matches(GamepadButtonFlags currentButtons)
            => (currentButtons & Buttons) == Buttons && currentButtons != GamepadButtonFlags.None;

        /// <summary>Prüft ob der Chord exakt übereinstimmt (keine zusätzlichen Buttons).</summary>
        public bool MatchesExact(GamepadButtonFlags currentButtons)
            => currentButtons == Buttons && currentButtons != GamepadButtonFlags.None;
    }

    /// <summary>
    /// Definiert eine sequentielle Eingabekombination (z.B. A→B→X innerhalb von 500ms).
    /// </summary>
    public class SequenceConfig
    {
        /// <summary>Eindeutiger Name der Sequenz.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Die Buttons in der richtigen Reihenfolge.</summary>
        public GamepadButtonFlags[] Sequence { get; set; } = System.Array.Empty<GamepadButtonFlags>();

        /// <summary>Aktion die ausgeführt wird wenn die Sequenz erkannt wird.</summary>
        public ButtonAction? Action { get; set; }

        /// <summary>Maximales Zeitfenster in ms für die gesamte Sequenz.</summary>
        public int MaxIntervalMs { get; set; } = 500;

        /// <summary>Ob die Sequenz nur einmal feuern soll (bis alle Buttons losgelassen).</summary>
        public bool FireOnce { get; set; } = true;

        /// <summary>Ob die Sequenz aktiviert ist.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Beschreibung für die UI.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Aktuelle Position in der Sequenz (intern).</summary>
        public int CurrentIndex { get; set; } = 0;

        /// <summary>Zeitpunkt des ersten gedrückten Buttons der Sequenz (intern).</summary>
        public long StartTick { get; set; } = 0;
    }

    /// <summary>
    /// Definiert eine Hold+Tap-Kombination (kurz drücken = Aktion A, halten = Aktion B).
    /// </summary>
    public class HoldTapConfig
    {
        /// <summary>Der Button für Hold+Tap.</summary>
        public GamepadButtonFlags Button { get; set; } = GamepadButtonFlags.None;

        /// <summary>Aktion bei kurzem Drücken (Tap).</summary>
        public ButtonAction? TapAction { get; set; }

        /// <summary>Aktion beim Halten (Hold).</summary>
        public ButtonAction? HoldAction { get; set; }

        /// <summary>Schwelle in ms ab wann als "Halten" gewertet wird.</summary>
        public int HoldThresholdMs { get; set; } = 300;

        /// <summary>Ob Hold+Tap aktiviert ist.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Beschreibung für die UI.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Zeitpunkt wann der Button gedrückt wurde (intern).</summary>
        public long PressTick { get; set; } = 0;

        /// <summary>Ob der Hold bereits ausgelöst wurde (intern).</summary>
        public bool HoldFired { get; set; } = false;
    }
}
