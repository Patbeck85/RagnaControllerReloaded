using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RagnaController.Models;

namespace RagnaController.Profiles
{
    /// <summary>
    /// TASK-1911: ProfileTemplateManager — verwaltet alle verfügbaren Profil-Vorlagen
    /// für RO-Klassen. Lädt Vorlagen aus JSON-Dateien im <DefaultProfiles/>-Ordner
    /// (z.B. <novice.json>, <mage.json>, <swordsman.json>). Jede Vorlage definiert
    /// Klasse, Engine-Einstellungen, Button-Mappings und Sensitivität, die im
    /// First-Run Wizard (Epic 9) ausgewählt werden können.
    ///
    /// Die DefaultProfiles-Dateien sind bereits vollständige Profile-Objekte.
    /// Dieser Manager konvertiert sie zu ProfileTemplate-Objekten für den Wizard.
    /// Später können Vorlagen auch über GitHub Gists geteilt werden.
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

            var defaultProfilesDir = Path.Combine(AppContext.BaseDirectory, "DefaultProfiles");
            if (!Directory.Exists(defaultProfilesDir))
            {
                Logger?.Invoke("[ProfileTemplateManager] DefaultProfiles-Ordner nicht gefunden — keine Vorlagen geladen.");
                return;
            }

            int loaded = 0;
            foreach (var file in Directory.GetFiles(defaultProfilesDir, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var profile = JsonSerializer.Deserialize<Profile>(json, AppJsonContext.Default.Profile);
                    if (profile == null) continue;

                    var template = ProfileToTemplate(profile, Path.GetFileNameWithoutExtension(file));
                    if (template != null)
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

            Logger?.Invoke($"[ProfileTemplateManager] {loaded} Vorlagen aus DefaultProfiles geladen.");
        }

        /// <summary>
        /// Konvertiert ein Profile-Objekt zu einem ProfileTemplate für den Wizard.
        /// </summary>
        private static ProfileTemplate? ProfileToTemplate(Profile profile, string fileNameWithoutExtension)
        {
            if (profile == null) return null;

            // Id aus Dateinamen ableiten (z.B. "mage_renewal" → "mage_renewal")
            var id = fileNameWithoutExtension.ToLowerInvariant();

            // Modus aus Dateinamen erkennen: *_preremewal oder *_renewal
            var mode = "Pre-Renewal";
            var lowerName = fileNameWithoutExtension.ToLowerInvariant();
            if (lowerName.EndsWith("_renewal")) mode = "Renewal";
            else if (lowerName.EndsWith("_preremewal")) mode = "Pre-Renewal";

            var template = new ProfileTemplate
            {
                Id = id,
                DisplayName = profile.Name,
                Description = profile.ClassTips ?? $"Profil für Klasse {profile.Class} ({mode})",
                Class = profile.Class,
                Mode = mode,
                SkillRecommendations = new List<string>(profile.SkillRecommendations ?? new List<string>()),
                ClassTips = profile.ClassTips ?? "",
                Engines = new TemplateEngineSettings
                {
                    AutoAttackEnabled = profile.AutoAttackEnabled,
                    AutoRetargetEnabled = profile.AutoRetargetEnabled,
                    KiteEnabled = profile.KiteEnabled,
                    MageEnabled = profile.MageEnabled,
                    SupportEnabled = profile.SupportEnabled,
                    ComboEnabled = profile.ComboEnabled,
                    SmartSkillEnabled = profile.SmartSkillEnabled,
                    AutoAttackKeyVK = profile.AutoAttackKeyVK,
                    KiteAttackKeyVK = profile.KiteAttackKeyVK,
                    SupportHealKeyVK = profile.SupportHealKeyVK,
                    MageBoltKeyVK = profile.MageBoltKeyVK,
                    TabCycleMs = profile.TabCycleMs
                },
                ButtonMappings = new Dictionary<VirtualKey, VirtualKey>(),
                Sensitivity = new TemplateSensitivity
                {
                    MouseSensitivity = profile.MouseSensitivity,
                    Deadzone = profile.Deadzone,
                    MovementCurve = profile.MovementCurve,
                    AimSensitivity = profile.AimSensitivity,
                    AimDeadzone = profile.AimDeadzone,
                    StickDeadzone = profile.StickDeadzone
                }
            };

            // Konvertiere ButtonMappings (Dictionary<ButtonKey, ButtonAction>) → Dictionary<VirtualKey, VirtualKey>
            if (profile.ButtonMappings != null)
            {
                foreach (var kvp in profile.ButtonMappings)
                {
                    if (kvp.Key.Key != VirtualKey.None)
                    {
                        template.ButtonMappings[kvp.Key.Key] = kvp.Value.Key;
                    }
                }
            }

            return template;
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
