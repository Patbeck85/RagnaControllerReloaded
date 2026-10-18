using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RagnaController.Models
{
    /// <summary>
    /// FEAT-008 / GEX-011: RO Buff/Debuff Registry.
    /// Central database of all RO Buffs and Debuffs with IDs, duration, stacks, icons, classes.
    /// Loaded at runtime from Resources/BuffRegistry.json and used by BuffManager
    /// for validation, defaults and auto-recast.
    /// </summary>
    public static class BuffRegistry
    {
        private static BuffRegistryData? _data;
        private static readonly object _initLock = new();
        private static readonly Dictionary<int, BuffEntry> _buffById = new();
        private static readonly Dictionary<string, BuffEntry> _buffByName = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<int, DebuffEntry> _debuffById = new();
        private static readonly Dictionary<string, DebuffEntry> _debuffByName = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Lädt die Registry aus der eingebetteten JSON-Ressource (einmalig, thread-safe).</summary>
        public static void Initialize()
        {
            if (_data != null) return;

            lock (_initLock)
            {
                if (_data != null) return;

                // Try embedded resource first (production)
                var assembly = typeof(BuffRegistry).Assembly;
                var resourceName = "RagnaController.Resources.BuffRegistry.json";
                using var stream = assembly.GetManifestResourceStream(resourceName);
                
                string json;
                if (stream != null)
                {
                    using var reader = new System.IO.StreamReader(stream);
                    json = reader.ReadToEnd();
                }
                else
                {
                    // Fallback: load from file (tests / development)
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var filePath = System.IO.Path.Combine(baseDir, "Resources", "BuffRegistry.json");
                    if (!System.IO.File.Exists(filePath))
                    {
                        // Try parent directories (test project structure)
                        filePath = System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "src", "RagnaController", "Resources", "BuffRegistry.json");
                        filePath = System.IO.Path.GetFullPath(filePath);
                    }
                    if (!System.IO.File.Exists(filePath))
                    {
                        // Try test project output
                        filePath = System.IO.Path.Combine(baseDir, "Resources", "BuffRegistry.json");
                        filePath = System.IO.Path.GetFullPath(filePath);
                    }
                    if (!System.IO.File.Exists(filePath))
                        throw new InvalidOperationException($"BuffRegistry.json not found. Searched: {baseDir}");

                    json = System.IO.File.ReadAllText(filePath);
                }

                var data = System.Text.Json.JsonSerializer.Deserialize<BuffRegistryData>(json)
                    ?? throw new InvalidOperationException("Failed to deserialize BuffRegistry.json");

                // Indizes bauen
                _buffById.Clear();
                _buffByName.Clear();
                _debuffById.Clear();
                _debuffByName.Clear();

                foreach (var b in data.Buffs)
                {
                    _buffById[b.Id] = b;
                    _buffByName[b.Name] = b;
                }
                foreach (var d in data.Debuffs)
                {
                    _debuffById[d.Id] = d;
                    _debuffByName[d.Name] = d;
                }

                _data = data;
            }
        }

        /// <summary>Buff nach RO Skill-ID (z. B. "PR_BLESSING") finden.</summary>
        public static BuffEntry? GetBuffBySkillId(string skillId)
        {
            Initialize();
            foreach (var b in _data!.Buffs)
                if (string.Equals(b.SkillId, skillId, StringComparison.OrdinalIgnoreCase))
                    return b;
            return null;
        }

        /// <summary>Buff nach numerischer ID finden.</summary>
        public static BuffEntry? GetBuffById(int id)
        {
            Initialize();
            _buffById.TryGetValue(id, out var b);
            return b;
        }

        /// <summary>Buff nach Name finden (case-insensitive).</summary>
        public static BuffEntry? GetBuffByName(string name)
        {
            Initialize();
            _buffByName.TryGetValue(name, out var b);
            return b;
        }

        /// <summary>Debuff nach ID finden.</summary>
        public static DebuffEntry? GetDebuffById(int id)
        {
            Initialize();
            _debuffById.TryGetValue(id, out var d);
            return d;
        }

        /// <summary>Debuff nach Name finden (case-insensitive).</summary>
        public static DebuffEntry? GetDebuffByName(string name)
        {
            Initialize();
            _debuffByName.TryGetValue(name, out var d);
            return d;
        }

        /// <summary>All Buffs (for Debug/UI).</summary>
        public static IReadOnlyList<BuffEntry> AllBuffs
        {
            get
            {
                Initialize();
                return _data!.Buffs;
            }
        }

        /// <summary>All Debuffs (for Debug/UI).</summary>
        public static IReadOnlyList<DebuffEntry> AllDebuffs
        {
            get
            {
                Initialize();
                return _data!.Debuffs;
            }
        }

        /// <summary>Validiert einen Buff-Namen gegen die Registry.</summary>
        public static bool IsValidBuff(string name) => GetBuffByName(name) != null;

    /// <param name="name">name parameter.</param>
    /// <param name="name">name parameter.</param>
        /// <summary>Validiert einen Debuff-Namen gegen die Registry.</summary>
        public static bool IsValidDebuff(string name) => GetDebuffByName(name) != null;

        /// <summary>Liefert Standard-Dauer for einen Buff (aus Registry or Fallback 60s).</summary>
        public static int GetDefaultDuration(string buffName)
        {
            var buff = GetBuffByName(buffName);
            return buff?.DurationSec ?? 60;
        }

        /// <summary>Liefert Standard-Warning-Schwelle for einen Buff (aus Registry or Fallback 10s).</summary>
        public static int GetDefaultWarningSec(string buffName)
        {
            var buff = GetBuffByName(buffName);
            return buff?.WarningSec ?? 10;
        }

        /// <summary>Liefert Auto-Recast-Default for einen Buff (Key + enabled).</summary>
        public static (bool AutoRecast, VirtualKey RecastKey) GetDefaultAutoRecast(string buffName)
        {
            var buff = GetBuffByName(buffName);
            if (buff == null) return (false, VirtualKey.None);
            // Autorecast only if SkillId bekannt and RecastKey gesetzt wäre — hier Default: false
            return (false, VirtualKey.None);
        }
    }

    internal class BuffRegistryData
    {
        [JsonPropertyName("buffs")]
        public List<BuffEntry> Buffs { get; set; } = new();

        [JsonPropertyName("debuffs")]
        public List<DebuffEntry> Debuffs { get; set; } = new();
    }

    /// <summary>Ein Buff-Eintrag in der Registry.</summary>
    public class BuffEntry
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("durationSec")]
        public int DurationSec { get; set; } = 60;

        [JsonPropertyName("maxStacks")]
        public int MaxStacks { get; set; } = 1;

        [JsonPropertyName("iconPath")]
        public string IconPath { get; set; } = "";

        [JsonPropertyName("class")]
        public string Class { get; set; } = "Universal";

        [JsonPropertyName("skillId")]
        public string SkillId { get; set; } = "";

        // Computed/Optional for Auto-Recast-Defaults
        [JsonPropertyName("warningSec")]
        public int WarningSec { get; set; } = 10;

        [JsonPropertyName("autoRecast")]
        public bool AutoRecast { get; set; } = false;

        [JsonPropertyName("recastKey")]
        public int RecastKey { get; set; } = 0;
    }

    /// <summary>Ein Debuff-Eintrag in der Registry.</summary>
    public class DebuffEntry
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("durationSec")]
        public int DurationSec { get; set; } = 30;

        [JsonPropertyName("iconPath")]
        public string IconPath { get; set; } = "";

        [JsonPropertyName("isDebuff")]
        public bool IsDebuff { get; set; } = true;
    }
}