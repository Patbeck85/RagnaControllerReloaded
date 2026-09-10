using System;

namespace RagnaController.Models
{
    /// <summary>
    /// FEAT-011: Konfiguration eines verwalteten Items (Potion/Trank).
    /// Wird pro Profil in der Profile-Klasse gespeichert und von der
    /// ItemManagerEngine im Tick-Pfad ausgewertet.
    /// </summary>
    public class ItemConfig
    {
        /// <summary>Anzeigename des Items (z. B. "Potion", "Elixir").</summary>
        public string Name { get; set; } = "";

        /// <summary>Hotkey, der das Item im Spiel einsetzt.</summary>
        public VirtualKey Key { get; set; } = VirtualKey.None;

        /// <summary>Mindest-HP-Prozent, bei dem das Item NUR NICHT mehr gefeuert wird.
        /// Feuert wenn HP &lt;= Schwelle (d. h. erst ab Unterschreiten).</summary>
        public int HpThresholdPercent { get; set; } = 70;

        /// <summary>Mindest-SP-Wert, der für den Cast erforderlich ist (0 = keine SP-Bedingung).</summary>
        public int MinSpRequired { get; set; } = 0;

        /// <summary>Cooldown in ms nach dem Einsatz, bevor das Item erneut gefeuert werden darf.</summary>
        public int CooldownMs { get; set; } = 3000;

        /// <summary>Globales Update-Intervall in ms (wie oft die Engine prüft, ob dieses Item feuern soll).</summary>
        public int CheckIntervalMs { get; set; } = 1000;

        /// <summary>Ob das Item aktiv verwaltet wird. Default: true.</summary>
        public bool Enabled { get; set; } = true;
    }
}
