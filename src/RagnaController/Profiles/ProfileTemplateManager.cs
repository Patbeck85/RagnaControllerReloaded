using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RagnaController.Models;

namespace RagnaController.Profiles
{
    /// <summary>
    /// TASK-1911: ProfileTemplateManager — verwaltet alle verfügbaren Profil-Vorlagen
    /// für RO-Klassen. Lädt Vorlagen aus JSON-Dateien im <DefaultProfiles/>-Ordner
    /// (z.B. <novice_preremewal.json>, <mage_renewal.json>). Jede Vorlage definiert
    /// Klasse, Engine-Einstellungen, Button-Mappings und Sensitivität, die im
    /// First-Run Wizard (Epic 9) ausgewählt werden können.
    ///
    /// Hinweis: Statt hardcodierter Klassen werden Vorlagen aus JSON-Dateien geladen,
    /// sodass du deine eigenen Pre-Renewal / Renewal Vorlagen einfach als
    /// DefaultProfiles\*.json ablegen kannst. Später können Vorlagen auch über
    /// GitHub Gists geteilt und heruntergeladen werden.
    /// </summary>
    public sealed class ProfileTemplateManager : IDisposable
    {
        private readonly Dictionary<string, ProfileTemplate> _templates = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Alle verfügbaren Vorlagen (ID → Template).</summary>
        public IReadOnlyDictionary<string, ProfileTemplate> Templates => _templates;

        /// <summary>Anzahl der registrierten Vorlagen.</summary>
        public int Count => _templates.Count;

        /// <summary>Optional logger for diagnostics.</summary>
        public Action<string>? Logger { get; set; }

        public ProfileTemplateManager()
        {
            LoadFromDefaultProfilesFolder();
        }

        /// <summary>
        /// Lädt alle Profil-Vorlagen aus JSON-Dateien im DefaultProfiles-Ordner.
        /// Dateinamenschema: <klasse>_<modus>.json (z.B. novice_preremewal.json, mage_renewal.json)
        /// </summary>
        private void LoadFromDefaultProfilesFolder()
        {
            _templates.Clear();

            var templateDir = Path.Combine(AppContext.BaseDirectory, "ProfileTemplates");
            if (!Directory.Exists(templateDir))
            {
                Logger?.Invoke("[ProfileTemplateManager] ProfileTemplates-Ordner nicht gefunden — keine Vorlagen geladen.");
                return;
            }

            int loaded = 0;
            foreach (var file in Directory.GetFiles(templateDir, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var template = JsonSerializer.Deserialize<ProfileTemplate>(json, AppJsonContext.Default.ProfileTemplate);
                    if (template != null && !string.IsNullOrWhiteSpace(template.Id))
                    {
                        _templates[template.Id] = template;
                        loaded++;
                    }
                }
                catch (Exception ex)
                {
                    Logger?.Invoke($"[ProfileTemplateManager] Ungültige Vorlage '{Path.GetFileName(file)}': {ex.Message}");
                }
            }

            Logger?.Invoke($"[ProfileTemplateManager] {loaded} Vorlagen aus ProfileTemplates geladen.");
        }

        /// <summary>
        /// Registriert eine Vorlage (überschreibt bei doppelter ID).
        /// </summary>
        public void RegisterTemplate(ProfileTemplate template)
        {
            if (string.IsNullOrWhiteSpace(template.Id))
                throw new ArgumentException("Template Id darf nicht leer sein.", nameof(template.Id));

            _templates[template.Id] = template;
        }

        /// <summary>
        /// Liefert eine Vorlage nach ID oder null, falls nicht gefunden.
        /// </summary>
        public ProfileTemplate? GetTemplate(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return _templates.TryGetValue(id, out var t) ? t : null;
        }

        /// <summary>
        /// Liefert alle Vorlagen als Liste.
        /// </summary>
        public List<ProfileTemplate> ListTemplates() => new(_templates.Values);

        /// <summary>
        /// Erstellt ein neues Profile-Objekt aus einer Vorlage mit gegebenem Namen.
        /// Wendet alle Vorlagen-Werte (Klasse, Engines, Button-Mappings, Sensitivität) an.
        /// </summary>
        public Profile CreateProfileFromTemplate(string templateId, string profileName)
        {
            var template = GetTemplate(templateId)
                ?? throw new KeyNotFoundException($"Vorlage '{templateId}' nicht gefunden.");

            if (string.IsNullOrWhiteSpace(profileName))
                throw new ArgumentException("Profilname darf nicht leer sein.", nameof(profileName));

            var safeName = string.Concat(profileName.Split(Path.GetInvalidFileNameChars())).Trim();
            if (string.IsNullOrWhiteSpace(safeName))
                throw new ArgumentException("Profilname enthält nur ungültige Zeichen.", nameof(profileName));

            var profile = new Profile
            {
                Name = safeName,
                Class = template.Class,
                IsEnabled = true,

                // Engine-Einstellungen aus Vorlage
                AutoAttackEnabled = template.Engines.AutoAttackEnabled,
                AutoRetargetEnabled = template.Engines.AutoRetargetEnabled,
                KiteEnabled = template.Engines.KiteEnabled,
                MageEnabled = template.Engines.MageEnabled,
                SupportEnabled = template.Engines.SupportEnabled,
                ComboEnabled = template.Engines.ComboEnabled,
                SmartSkillEnabled = template.Engines.SmartSkillEnabled,
                AutoAttackKeyVK = template.Engines.AutoAttackKeyVK,
                KiteAttackKeyVK = template.Engines.KiteAttackKeyVK,
                SupportHealKeyVK = template.Engines.SupportHealKeyVK,
                MageBoltKeyVK = template.Engines.MageBoltKeyVK,
                TabCycleMs = template.Engines.TabCycleMs,

                // Sensitivität aus Vorlage
                MouseSensitivity = template.Sensitivity.MouseSensitivity,
                Deadzone = template.Sensitivity.Deadzone,
                MovementCurve = template.Sensitivity.MovementCurve,
                AimSensitivity = template.Sensitivity.AimSensitivity,
                AimDeadzone = template.Sensitivity.AimDeadzone,
                StickDeadzone = template.Sensitivity.StickDeadzone,

                // Skill-Empfehlungen und Klassentipp
                SkillRecommendations = new List<string>(template.SkillRecommendations),
                ClassTips = template.ClassTips,

                // Button-Mappings aus Vorlage (VirtualKey → ButtonAction)
                ButtonMappings = new Dictionary<ButtonKey, ButtonAction>()
            };

            // Konvertiere ButtonMappings (Dictionary<VirtualKey, VirtualKey>)
            foreach (var kvp in template.ButtonMappings)
            {
                profile.ButtonMappings[kvp.Key] = new ButtonAction
                {
                    Type = ActionType.Key,
                    Key = kvp.Value,
                    Label = $"Quick Key {kvp.Key}"
                };
            }

            return profile;
        }

        public void Dispose()
        {
            _templates.Clear();
        }
    }
}
