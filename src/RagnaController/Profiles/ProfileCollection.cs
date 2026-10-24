using System;
using System.Collections.Generic;

namespace RagnaController.Profiles
{
    /// <summary>
    /// TASK-2307: ProfileCollection — Gruppierung von Profilen in Sammlungen.
    /// Ermöglicht z.B. "Meine WoE-Profile", "Meine Leveling-Profile" als benannte Sammlungen
    /// für schnelleres Auffinden und Umschalten zwischen verwandten Profilen.
    /// </summary>
    public class ProfileCollection
    {
        /// <summary>Anzeigename der Sammlung (z.B. "Meine WoE-Profile").</summary>
        public string Name { get; set; } = "Neue Sammlung";

        /// <summary>Optionale Beschreibung der Sammlung.</summary>
        public string Description { get; set; } = "";

        /// <summary>Namen der enthaltenen Profile (Referenzen, keine Duplikate).</summary>
        public List<string> ProfileNames { get; set; } = new();

        /// <summary>Erstellungsdatum der Sammlung.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Datum der letzten Änderung.</summary>
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Gibt zurück, ob die Sammlung ein bestimmtes Profil enthält.</summary>
        public bool ContainsProfile(string profileName)
        {
            return ProfileNames.Contains(profileName);
        }

        /// <summary>Fügt ein Profil zur Sammlung hinzu (falls noch nicht enthalten).</summary>
        public void AddProfile(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) return;
            if (!ProfileNames.Contains(profileName))
            {
                ProfileNames.Add(profileName);
                LastModifiedAt = DateTime.UtcNow;
            }
        }

        /// <summary>Entfernt ein Profil aus der Sammlung.</summary>
        public void RemoveProfile(string profileName)
        {
            if (ProfileNames.Remove(profileName))
                LastModifiedAt = DateTime.UtcNow;
        }

        /// <summary>Gibt die Anzahl der enthaltenen Profile zurück.</summary>
        public int ProfileCount => ProfileNames.Count;
    }
}
