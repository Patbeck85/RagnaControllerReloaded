using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RagnaController.Profiles
{
    /// <summary>
    /// TASK-1611: ProfilePresetManager — verwaltet gespeicherte Profil-Presets für
    /// schnellen Namens-basierten Profil-Wechsel.
    /// Presets werden in <dir>/presets.json persistiert und über Neustarts erhalten.
    /// </summary>
    public sealed class ProfilePresetManager : IDisposable
    {
        private readonly string _presetsPath;
        private readonly Dictionary<string, ProfilePreset> _presets = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Optional logger for diagnostics.</summary>
        public Action<string>? Logger { get; set; }

        public ProfilePresetManager(string? presetsDir = null)
        {
            var dir = presetsDir ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RagnaController", "Profiles");
            Directory.CreateDirectory(dir);
            _presetsPath = Path.Combine(dir, "presets.json");
            Load();
        }

        /// <summary>Alle gespeicherten Presets (Name → Preset).</summary>
        public IReadOnlyDictionary<string, ProfilePreset> Presets => _presets;

        /// <summary>Anzahl der gespeicherten Presets.</summary>
        public int Count => _presets.Count;

        /// <summary>
        /// Speichert ein Preset (überschreibt bei bestehendem Namen).
        /// </summary>
        public void SavePreset(ProfilePreset preset)
        {
            if (string.IsNullOrWhiteSpace(preset.PresetName))
                throw new ArgumentException("PresetName darf nicht leer sein.", nameof(preset.PresetName));

            _presets[preset.PresetName] = preset;
            Save();
            Logger?.Invoke($"[ProfilePresetManager] Preset gespeichert: '{preset.PresetName}' → '{preset.TargetProfileName}'");
        }

        /// <summary>
        /// Löscht ein Preset. Gibt true zurück, wenn gelöscht wurde.
        /// </summary>
        public bool DeletePreset(string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName)) return false;
            var removed = _presets.Remove(presetName);
            if (removed) Save();
            return removed;
        }

        /// <summary>
        /// Gibt ein Preset zurück oder null, wenn nicht gefunden.
        /// </summary>
        public ProfilePreset? GetPreset(string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName)) return null;
            return _presets.TryGetValue(presetName, out var preset) ? preset : null;
        }

        /// <summary>
        /// Listet alle Preset-Namen auf.
        /// </summary>
        public IEnumerable<string> ListPresetNames() => _presets.Keys;

        /// <summary>
        /// Markiert ein Preset als verwendet (LastUsedAt + UseCount inkrementiert).
        /// </summary>
        public void MarkUsed(string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName)) return;
            if (_presets.TryGetValue(presetName, out var preset))
            {
                preset.LastUsedAt = DateTime.UtcNow;
                preset.UseCount++;
                Save();
            }
        }

        private void Save()
        {
            try
            {
                var list = new List<ProfilePreset>(_presets.Values);
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(_presetsPath, JsonSerializer.Serialize(list, opts));
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[ProfilePresetManager] Speichern fehlgeschlagen: {ex.Message}");
            }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_presetsPath)) return;
                var json = File.ReadAllText(_presetsPath);
                var list = JsonSerializer.Deserialize<List<ProfilePreset>>(json) ?? new List<ProfilePreset>();
                _presets.Clear();
                foreach (var p in list)
                {
                    if (!string.IsNullOrWhiteSpace(p.PresetName))
                        _presets[p.PresetName] = p;
                }
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[ProfilePresetManager] Laden fehlgeschlagen: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Save();
        }
    }
}
