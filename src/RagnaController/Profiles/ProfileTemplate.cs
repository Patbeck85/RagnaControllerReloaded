using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using RagnaController.Models;
using RagnaController.Views.Windows;

namespace RagnaController.Profiles
{
    /// <summary>
    /// TASK-1911: ProfileTemplate — definiert eine Vorlage für ein RO-Charakter-Profil.
    /// Enthält Klasse, Engine-Voreinstellungen, Button-Mappings und Skill-Empfehlungen.
    /// Wird im First-Run Wizard (Epic 9) verwendet, damit neue Nutzer schnell
    /// ein passendes Profil für ihre Klasse erstellen können.
    /// </summary>
    public sealed class ProfileTemplate
    {
        /// <summary>Eindeutige Kennung der Vorlage (z.B. "novice", "swordsman", "mage").</summary>
        [JsonInclude]
        public string Id { get; init; } = "";

        /// <summary>Anzeigename der Vorlage (z.B. "Novice", "Swordsman", "Mage").</summary>
        [JsonInclude]
        public string DisplayName { get; init; } = "";

        /// <summary>Beschreibung für den Wizard-Schritt.</summary>
        [JsonInclude]
        public string Description { get; init; } = "";

        /// <summary>RO-Klasse: Melee, Mage, Ranged oder Support.</summary>
        [JsonInclude]
        public string Class { get; init; } = "Melee";

        /// <summary>RO-Modus: Pre-Renewal oder Renewal.</summary>
        [JsonInclude]
        public string Mode { get; init; } = "Pre-Renewal";

        /// <summary>Empfohlene Skills für diese Klasse (vom Wizard angezeigt).</summary>
        [JsonInclude]
        public List<string> SkillRecommendations { get; init; } = new();

        /// <summary>Klassenspezifischer Tipp, der im Wizard angezeigt wird.</summary>
        [JsonInclude]
        public string ClassTips { get; init; } = "";

        /// <summary>Standard-Engine-Einstellungen der Vorlage.</summary>
        [JsonInclude]
        public TemplateEngineSettings Engines { get; init; } = new();

        /// <summary>Standard-Button-Mappings für A/B/X/Y (Key-String → Action-Key). Key-String kann "A", "L1+A", "R1+B" etc. sein.</summary>
        [JsonInclude]
        public Dictionary<string, VirtualKey> ButtonMappings { get; init; } = new();

        /// <summary>Standard-Sensitivitätswerte der Vorlage.</summary>
        [JsonInclude]
        public TemplateSensitivity Sensitivity { get; init; } = new();
    }

    /// <summary>
    /// Engine-Einstellungen einer Profil-Vorlage — getrennt für Pre-Renewal und Renewal Modus.
    /// Jedes Profil hat für beide Modi eigene Einstellungen, die im MainWindow per
    /// Renewal/Pre-Renewal Checkbox umgeschaltet werden können.
    /// </summary>
    public sealed class TemplateEngineSettings
    {
        /// <summary>Einstellungen für Pre-Renewal Modus.</summary>
        [JsonInclude]
        public TemplateEngineModeSettings PreRenewal { get; init; } = new();

        /// <summary>Einstellungen für Renewal Modus.</summary>
        [JsonInclude]
        public TemplateEngineModeSettings Renewal { get; init; } = new();
    }

    /// <summary>
    /// Engine-Einstellungen für einen einzelnen Modus (Pre-Renewal oder Renewal).
    /// </summary>
    public sealed class TemplateEngineModeSettings
    {
        [JsonInclude] public bool AutoAttackEnabled   { get; init; } = false;
        [JsonInclude] public bool AutoRetargetEnabled { get; init; } = false;
        [JsonInclude] public bool KiteEnabled         { get; init; } = false;
        [JsonInclude] public bool MageEnabled         { get; init; } = false;
        [JsonInclude] public bool SupportEnabled      { get; init; } = false;
        [JsonInclude] public bool ComboEnabled        { get; init; } = false;
        [JsonInclude] public bool SmartSkillEnabled   { get; init; } = true;
        [JsonInclude] public int  AutoAttackKeyVK     { get; init; } = 0x5A; // Z
        [JsonInclude] public int  KiteAttackKeyVK     { get; init; } = 0x5A; // Z
        [JsonInclude] public int  SupportHealKeyVK    { get; init; } = 0x5A; // Z
        [JsonInclude] public int  MageBoltKeyVK       { get; init; } = 0x58; // X
        [JsonInclude] public int  TabCycleMs          { get; init; } = 80;
        [JsonInclude] public int  AttackIntervalMs    { get; init; } = 100;
        [JsonInclude] public int  SkillInterruptMs    { get; init; } = 800;
    }

    /// <summary>
    /// Sensitivitätswerte einer Profil-Vorlage.
    /// </summary>
    public sealed class TemplateSensitivity
    {
        [JsonInclude] public float MouseSensitivity { get; init; } = 1.2f;
        [JsonInclude] public float Deadzone         { get; init; } = 0.12f;
        [JsonInclude] public float MovementCurve    { get; init; } = 1.5f;
        [JsonInclude] public float AimSensitivity   { get; init; } = 22f;
        [JsonInclude] public float AimDeadzone      { get; init; } = 0.20f;
        [JsonInclude] public float StickDeadzone    { get; init; } = 0.2f;
    }
}
