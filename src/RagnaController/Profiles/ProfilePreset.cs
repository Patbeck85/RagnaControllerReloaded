using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RagnaController.Profiles;

namespace RagnaController.Profiles
{
    /// <summary>
    /// TASK-1611: Profile preset — a named stored profile clipboard for
    /// schnellen Namens-basierten Wechsel zwischen Profilen.
    /// </summary>
    public sealed class ProfilePreset
    {
        /// <summary>Anzeigename des Presets (z.B. "Wizard PvP", "Knight Farming").</summary>
        public string PresetName { get; set; } = "";

        /// <summary>Name des Ziel-Profils, das beim Anwenden geladen wird.</summary>
        public string TargetProfileName { get; set; } = "";

        /// <summary>Optionale Beschreibung (z.B. "Mage-Fokus, Auto-Attack an").</summary>
        public string Description { get; set; } = "";

        /// <summary>Erstellungsdatum.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Letztes Nutzungsdatum.</summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>Anzahl der Male, das Preset angewendet wurde.</summary>
        public int UseCount { get; set; }
    }
}
