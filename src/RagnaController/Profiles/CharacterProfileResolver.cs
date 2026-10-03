using System;
using System.Collections.Generic;

namespace RagnaController.Profiles
{
    /// <summary>
    /// FEAT-024: Reiner Resolver, der einen in-game Charakter-Namen auf einen Profil-Namen
    /// abbildet. Trennt die Zuordnungslogik (unit-testbar) von der Persistenz/Aktivierung
    /// im <see cref="ProfileManager"/>.
    ///
    /// Normalisierung: RO-Client-Charakter-Namen können führende/nachgestellte Leerzeichen
    /// und gemischte Groß-/Kleinschreibung enthalten (OCR-/Fenster-Lese-Rauschen). Der Resolver
    /// normalisiert zu "getrimmt + interne Leerzeichen auf ein zusammengefasst" und vergleicht
    /// case-insensitiv, damit dieselbe Zuordnung auch bei Lese-Varianz stabil bleibt.
    /// </summary>
    public sealed class CharacterProfileResolver
    {
        // key = normalisierter Char-Name, value = Profil-Name.
        // OrdinalIgnoreCase: OCR-/Fenster-Lese-Rauschen ändert häufig nur die Großschreibung.
        private readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Anzahl der gespeicherten Zuordnungen.</summary>
        public int Count => _map.Count;

        /// <summary>
        /// Normalisiert einen Charakter-Namen: trims Außenleerzeichen und fasst interne
        /// Leerzeichen-Läufe auf ein einzelnes Leerzeichen zusammen. Leere/Null → leere Zeichenkette.
        /// </summary>
        public static string Normalize(string? characterName)
        {
            if (string.IsNullOrEmpty(characterName)) return string.Empty;

            var trimmed = characterName.Trim();
            if (trimmed.Length == 0) return string.Empty;

            // Interne Leerzeichen-Läufe auf ein einzelnes Leerzeichen reduzieren.
            var sb = new System.Text.StringBuilder(trimmed.Length);
            bool inRun = false;
            foreach (var ch in trimmed)
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (!inRun) { sb.Append(' '); inRun = true; }
                }
                else
                {
                    sb.Append(ch);
                    inRun = false;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Registriert eine Zuordnung Charakter-Name → Profil-Name. Leere Eingaben werden ignoriert.
        /// </summary>
        public void RegisterMapping(string characterName, string profileName)
        {
            if (string.IsNullOrWhiteSpace(characterName) || string.IsNullOrWhiteSpace(profileName))
                return;

            var key = Normalize(characterName);
            if (key.Length == 0) return;
            _map[key] = profileName.Trim();
        }

        /// <summary>Entfernt eine Zuordnung. Gibt zurück, ob etwas entfernt wurde.</summary>
        public bool Unregister(string characterName)
        {
            var key = Normalize(characterName);
            if (key.Length == 0) return false;
            return _map.Remove(key);
        }

        /// <summary>Entfernt alle Zuordnungen.</summary>
        public void Clear() => _map.Clear();

        /// <summary>
        /// Liefert den Profil-Namen für einen Charakter-Namen, oder <c>null</c>, wenn keine
        /// Zuordnung existiert. Unbekannte/leere Namen → <c>null</c>.
        /// </summary>
        public string? Resolve(string? characterName)
        {
            var key = Normalize(characterName);
            if (key.Length == 0) return null;
            return _map.TryGetValue(key, out var profileName) ? profileName : null;
        }

        /// <summary>Prüft, ob für einen Charakter-Namen eine Zuordnung existiert.</summary>
        public bool IsMapped(string? characterName) => Resolve(characterName) != null;

        /// <summary>Schnappschuss aller Zuordnungen (normalisierte Schlüssel) — für Persistenz/Tests.</summary>
        public IReadOnlyDictionary<string, string> Snapshot()
        {
            var copy = new Dictionary<string, string>(_map);
            return copy;
        }

        /// <summary>Lädt ein vorher gespeichertes Mapping (z. B. nach Neustart).</summary>
        public void LoadFrom(IReadOnlyDictionary<string, string> mappings)
        {
            _map.Clear();
            if (mappings == null) return;
            foreach (var kv in mappings)
            {
                var key = Normalize(kv.Key);
                if (key.Length > 0 && !string.IsNullOrWhiteSpace(kv.Value))
                    _map[key] = kv.Value.Trim();
            }
        }
    }
}
