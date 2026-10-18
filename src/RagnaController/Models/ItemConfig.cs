using System;

namespace RagnaController.Models
{
    /// <summary>
        /// FEAT-011: Konfiguration eines verwalteten Items (Potion/Trank).
        /// Is pro Profil in der Profile-Klasse gespeichert and from der
        /// ItemManagerEngine im Tick-Pfad ausgewertet.
        /// </summary>
        public class ItemConfig
        {
            /// <summary>RO Item-ID (z. B. 501 = Red Potion, 502 = Blue Potion, 545 = Red Slim Potion).</summary>
            public int ItemId { get; set; } = 0;

            /// <summary>Anzeigename des Items (z. B. "Potion", "Elixir").</summary>
            public string Name { get; set; } = "";

            /// <summary>Hotkey, der das Item im Spiel einsetzt.</summary>
            public VirtualKey Key { get; set; } = VirtualKey.None;

            /// <summary>Mindest-HP-percent, bei dem das Item NUR NICHT mehr gefeuert is.
            /// Feuert if HP <= Schwelle (d. h. erst ab Unterschreiten).</summary>
            public int HpThresholdPercent { get; set; } = 70;

            /// <summary>Mindest-SP-Wert, der for den Cast required is (0 = keine SP-Bedingung).</summary>
            public int MinSpRequired { get; set; } = 0;

            /// <summary>Cooldown in ms nach dem Einsatz, bevor das Item erneut gefeuert are darf.</summary>
            public int CooldownMs { get; set; } = 3000;

            /// <summary>Global update interval in ms (how often engine checks if this item should fire).</summary>
            public int CheckIntervalMs { get; set; } = 1000;

            /// <summary>Ob das Item active verwaltet is. Default: true.</summary>
            public bool Enabled { get; set; } = true;
        }
    }
