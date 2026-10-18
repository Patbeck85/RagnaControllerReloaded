using System;
using System.Collections.Generic;

namespace RagnaController.Profiles
{
    /// <summary>
    /// FEAT-024: Reiner Resolver, der einen in-game Charakter-Namen on einen Profil-Namen
    /// abbildet. Trennt die Zuordnungslogik (unit-testbar) from der Persistenz/Aktivierung
    /// im <see cref="ProfileManager"/>.
    ///
    /// Normalization: RO client character names can have leading/trailing spaces
    /// and mixed upper/lowercase included (OCR/window-read noise). The resolver
    /// normalisiert to "getrimmt + interne Leerzeichen on ein zusammengefasst" and vergleicht
    /// case-insensitiv, damit dieselbe Zuordnung auch bei Lese-Varianz stabil bleibt.
    /// </summary>
    /// <summary>
    /// Profile management class.
    /// </summary>
    public sealed class CharacterProfileResolver
    {
        // key = normalisierter Char-Name, value = Profil-Name.
        // OrdinalIgnoreCase: OCR/window-read noise often changes only capitalization.
        private readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Anzahl der gespeicherten Zuordnungen.</summary>
        public int Count => _map.Count;

        /// <summary>
        /// Normalizes a character name: trims outer spaces and combines internal
        /// space sequences into a single space. Empty/null → empty string.
        /// </summary>
        public static string Normalize(string? characterName)
        {
            if (string.IsNullOrEmpty(characterName)) return string.Empty;

            var trimmed = characterName.Trim();
            if (trimmed.Length == 0) return string.Empty;

            // Internal space sequences reduce to a single space.
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
        /// Registriert eine Zuordnung Charakter-Name → Profil-Name. Leere Eingaben are ignoriert.
        /// </summary>
        public void RegisterMapping(string characterName, string profileName)
        {
            if (string.IsNullOrWhiteSpace(characterName) || string.IsNullOrWhiteSpace(profileName))
                return;

            var key = Normalize(characterName);
            if (key.Length == 0) return;
            _map[key] = profileName.Trim();
        }

        /// <summary>Removes a mapping. Returns whether something was removed.</summary>
        public bool Unregister(string characterName)
        {
            var key = Normalize(characterName);
            if (key.Length == 0) return false;
            return _map.Remove(key);
        }

        /// <summary>Entfernt all Zuordnungen.</summary>
        public void Clear() => _map.Clear();

        /// <summary>
        /// Liefert den Profil-Namen for einen Charakter-Namen, or <c>null</c>, if keine
        /// Zuordnung existiert. Unbekannte/leere Namen → <c>null</c>.
    /// <param name="characterName">characterName parameter.</param>
        /// </summary>
        public string? Resolve(string? characterName)
        {
            var key = Normalize(characterName);
            if (key.Length == 0) return null;
            return _map.TryGetValue(key, out var profileName) ? profileName : null;
        }

        /// <summary>Checks whether a mapping exists for a character name.</summary>
        public bool IsMapped(string? characterName) => Resolve(characterName) != null;

    /// <param name="characterName">characterName parameter.</param>
    /// <param name="characterName">characterName parameter.</param>
        /// <summary>Snapshot of all mappings (normalized keys) — for persistence/tests.</summary>
        public IReadOnlyDictionary<string, string> Snapshot()
        {
            var copy = new Dictionary<string, string>(_map);
            return copy;
        }

        /// <summary>Loads a previously saved mapping (e.g. after restart).</summary>
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
