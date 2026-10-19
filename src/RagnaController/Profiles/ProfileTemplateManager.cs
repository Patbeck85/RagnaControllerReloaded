using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RagnaController.Models;

namespace RagnaController.Profiles
{
    /// <summary>
    /// TASK-1911: ProfileTemplateManager — verwaltet alle eingebauten Profil-Vorlagen
    /// für RO-Klassen (Novice, Swordsman, Mage, Archer, Acolyte, Merchant, Thief).
    /// Jede Vorlage definiert Klasse, Engine-Einstellungen, Button-Mappings und
    /// Sensitivität, die im First-Run Wizard ausgewählt werden können.
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
            LoadBuiltInTemplates();
        }

        /// <summary>
        /// Lädt alle eingebauten Klassen-Vorlagen.
        /// </summary>
        private void LoadBuiltInTemplates()
        {
            _templates.Clear();

            // ── NOVICE ── Startklasse, keine Spezialisierung
            RegisterTemplate(new ProfileTemplate
            {
                Id = "novice",
                DisplayName = "Novice",
                Description = "Startklasse – ausgewogene Einstellungen für alle Situationen.",
                Class = "Melee",
                SkillRecommendations = new List<string> { "Schlag", "Steigerung" },
                ClassTips = "Die Grundklasse. Gute Basiswerte, keine Spezialisierung.",
                Engines = new TemplateEngineSettings
                {
                    AutoAttackEnabled = true,
                    SmartSkillEnabled = true,
                    AutoAttackKeyVK = 0x5A, // Z
                    TabCycleMs = 80
                },
                ButtonMappings = new Dictionary<VirtualKey, VirtualKey>
                {
                    { VirtualKey.A, VirtualKey.Z }, // Angriff
                    { VirtualKey.B, VirtualKey.X }, // Fähigkeit
                    { VirtualKey.X, VirtualKey.C }, // Item
                    { VirtualKey.Y, VirtualKey.V }  // Buff
                },
                Sensitivity = new TemplateSensitivity
                {
                    MouseSensitivity = 1.2f,
                    Deadzone = 0.12f,
                    AimSensitivity = 22f,
                    AimDeadzone = 0.20f,
                    StickDeadzone = 0.2f
                }
            });

            // ── SWORDSMAN ── Nahkampf, Auto-Attack, Kite
            RegisterTemplate(new ProfileTemplate
            {
                Id = "swordsman",
                DisplayName = "Swordsman",
                Description = "Nahkampf-Klasse mit Auto-Attack und Kite-Unterstützung.",
                Class = "Melee",
                SkillRecommendations = new List<string> { "Schlag", "Steigerung", "Kriegerschrei" },
                ClassTips = "Nahkampf-Spezialist. Automatischer Angriff und Kite-Unterstützung für distanzierte Feinde.",
                Engines = new TemplateEngineSettings
                {
                    AutoAttackEnabled = true,
                    AutoRetargetEnabled = true,
                    KiteEnabled = true,
                    SmartSkillEnabled = true,
                    AutoAttackKeyVK = 0x5A, // Z
                    KiteAttackKeyVK = 0x5A, // Z
                    TabCycleMs = 80
                },
                ButtonMappings = new Dictionary<VirtualKey, VirtualKey>
                {
                    { VirtualKey.A, VirtualKey.Z }, // Auto-Attack
                    { VirtualKey.B, VirtualKey.X }, // Fähigkeit
                    { VirtualKey.X, VirtualKey.C }, // Item
                    { VirtualKey.Y, VirtualKey.V }  // Buff
                },
                Sensitivity = new TemplateSensitivity
                {
                    MouseSensitivity = 1.1f,
                    Deadzone = 0.10f,
                    AimSensitivity = 20f,
                    AimDeadzone = 0.18f,
                    StickDeadzone = 0.18f
                }
            });

            // ── MAGE ── Magie-Klasse, Mage Engine, Bodenzauber
            RegisterTemplate(new ProfileTemplate
            {
                Id = "mage",
                DisplayName = "Mage",
                Description = "Magie-Klasse mit Mage Engine für automatisches Zaubern.",
                Class = "Mage",
                SkillRecommendations = new List<string> { "Fire Bolt", "Thunder Storm", "Storm Gust", "Meteor Storm" },
                ClassTips = "Magier. Automatisches Zaubern mit Mage Engine. Bodenzauber mit Right-Stick zielen.",
                Engines = new TemplateEngineSettings
                {
                    MageEnabled = true,
                    SmartSkillEnabled = true,
                    MageBoltKeyVK = 0x58, // X
                    AutoAttackKeyVK = 0x5A, // Z
                    TabCycleMs = 100
                },
                ButtonMappings = new Dictionary<VirtualKey, VirtualKey>
                {
                    { VirtualKey.A, VirtualKey.X }, // Fire Bolt
                    { VirtualKey.B, VirtualKey.Z }, // Auto-Attack / Wand
                    { VirtualKey.X, VirtualKey.C }, // Item
                    { VirtualKey.Y, VirtualKey.V }  // Buff
                },
                Sensitivity = new TemplateSensitivity
                {
                    MouseSensitivity = 1.3f,
                    Deadzone = 0.15f,
                    AimSensitivity = 25f,
                    AimDeadzone = 0.22f,
                    StickDeadzone = 0.22f
                }
            });

            // ── ARCHER ── Fernkampf, Kite, Auto-Attack
            RegisterTemplate(new ProfileTemplate
            {
                Id = "archer",
                DisplayName = "Archer",
                Description = "Fernkampf-Klasse mit Kite-Unterstützung und schnellem Auto-Attack.",
                Class = "Ranged",
                SkillRecommendations = new List<string> { "Double Strafe", "Arrow Storm", "Strafe" },
                ClassTips = "Bogenschütze. Fernkampf mit Kite – automatisch Abstand zum Gegner halten.",
                Engines = new TemplateEngineSettings
                {
                    AutoAttackEnabled = true,
                    AutoRetargetEnabled = true,
                    KiteEnabled = true,
                    SmartSkillEnabled = true,
                    AutoAttackKeyVK = 0x5A, // Z
                    KiteAttackKeyVK = 0x5A, // Z
                    TabCycleMs = 60
                },
                ButtonMappings = new Dictionary<VirtualKey, VirtualKey>
                {
                    { VirtualKey.A, VirtualKey.Z }, // Auto-Attack
                    { VirtualKey.B, VirtualKey.X }, // Fähigkeit
                    { VirtualKey.X, VirtualKey.C }, // Item
                    { VirtualKey.Y, VirtualKey.V }  // Buff
                },
                Sensitivity = new TemplateSensitivity
                {
                    MouseSensitivity = 1.4f,
                    Deadzone = 0.10f,
                    AimSensitivity = 28f,
                    AimDeadzone = 0.15f,
                    StickDeadzone = 0.15f
                }
            });

            // ── ACOLYTE ── Support/Heilung, Support Engine
            RegisterTemplate(new ProfileTemplate
            {
                Id = "acolyte",
                DisplayName = "Acolyte",
                Description = "Support-Klasse mit automatischer Heilung und Party-Unterstützung.",
                Class = "Support",
                SkillRecommendations = new List<string> { "Heal", "Increase AGI", "Gloria", "Resurrection" },
                ClassTips = "Heiler. Automatische Party-Heilung mit Support Engine. Gruppenbuffs.",
                Engines = new TemplateEngineSettings
                {
                    SupportEnabled = true,
                    SmartSkillEnabled = true,
                    SupportHealKeyVK = 0x5A, // Z
                    AutoAttackKeyVK = 0x5A, // Z
                    TabCycleMs = 100
                },
                ButtonMappings = new Dictionary<VirtualKey, VirtualKey>
                {
                    { VirtualKey.A, VirtualKey.Z }, // Heal
                    { VirtualKey.B, VirtualKey.X }, // Buff
                    { VirtualKey.X, VirtualKey.C }, // Item
                    { VirtualKey.Y, VirtualKey.V }  // Rez / Party
                },
                Sensitivity = new TemplateSensitivity
                {
                    MouseSensitivity = 1.2f,
                    Deadzone = 0.12f,
                    AimSensitivity = 22f,
                    AimDeadzone = 0.20f,
                    StickDeadzone = 0.2f
                }
            });

            // ── MERCHANT ── Händler, ItemManager, Auto-Attack
            RegisterTemplate(new ProfileTemplate
            {
                Id = "merchant",
                DisplayName = "Merchant",
                Description = "Händler-Klasse mit ItemManager für automatisches Item-Management.",
                Class = "Melee",
                SkillRecommendations = new List<string> { "Mammonite", "Overcharge", "Cart Revolution" },
                ClassTips = "Händler. ItemManager für automatische Potion-Verwaltung und Cart-Fähigkeiten.",
                Engines = new TemplateEngineSettings
                {
                    AutoAttackEnabled = true,
                    AutoRetargetEnabled = true,
                    SmartSkillEnabled = true,
                    AutoAttackKeyVK = 0x5A, // Z
                    TabCycleMs = 80
                },
                ButtonMappings = new Dictionary<VirtualKey, VirtualKey>
                {
                    { VirtualKey.A, VirtualKey.Z }, // Auto-Attack
                    { VirtualKey.B, VirtualKey.X }, // Fähigkeit
                    { VirtualKey.X, VirtualKey.C }, // Item
                    { VirtualKey.Y, VirtualKey.V }  // Buff
                },
                Sensitivity = new TemplateSensitivity
                {
                    MouseSensitivity = 1.2f,
                    Deadzone = 0.12f,
                    AimSensitivity = 22f,
                    AimDeadzone = 0.20f,
                    StickDeadzone = 0.2f
                }
            });

            // ── THIEF ── Dieb, Kite, schnelle Angriffe
            RegisterTemplate(new ProfileTemplate
            {
                Id = "thief",
                DisplayName = "Thief",
                Description = "Dieb-Klasse mit Kite-Unterstützung und schnellen Angriffen.",
                Class = "Melee",
                SkillRecommendations = new List<string> { "Double Attack", "Sonic Blow", "Bash", "Spear Quick Throw" },
                ClassTips = "Dieb. Schnelle Nahkampfangriffe mit Kite-Unterstützung für hohe Mobilität.",
                Engines = new TemplateEngineSettings
                {
                    AutoAttackEnabled = true,
                    AutoRetargetEnabled = true,
                    KiteEnabled = true,
                    SmartSkillEnabled = true,
                    AutoAttackKeyVK = 0x5A, // Z
                    KiteAttackKeyVK = 0x5A, // Z
                    TabCycleMs = 60
                },
                ButtonMappings = new Dictionary<VirtualKey, VirtualKey>
                {
                    { VirtualKey.A, VirtualKey.Z }, // Auto-Attack
                    { VirtualKey.B, VirtualKey.X }, // Fähigkeit
                    { VirtualKey.X, VirtualKey.C }, // Item
                    { VirtualKey.Y, VirtualKey.V }  // Buff
                },
                Sensitivity = new TemplateSensitivity
                {
                    MouseSensitivity = 1.3f,
                    Deadzone = 0.10f,
                    AimSensitivity = 26f,
                    AimDeadzone = 0.15f,
                    StickDeadzone = 0.15f
                }
            });

            Logger?.Invoke($"[ProfileTemplateManager] {_templates.Count} eingebaute Vorlagen geladen.");
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
