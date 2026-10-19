using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Tests
{
    /// <summary>
    /// TASK-1916: Unit-Tests für ProfileTemplateManager und Button-Mapping.
    /// Erstellt temporäre Test-Profile als JSON-Strings statt auf DefaultProfiles zuzugreifen.
    /// </summary>
    public class ProfileTemplateManagerTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly ProfileTemplateManager _manager;

        public ProfileTemplateManagerTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"ragna-template-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);

            // Schreibe Test-Profile als hartecodierte JSON-Strings (vermeidet ButtonKey-Serialisierungs-Problem)
            WriteTestProfile(_tempDir, "novice.json", @"{
  ""Name"": ""Novice"",
  ""Class"": ""Melee"",
  ""AutoAttackEnabled"": true,
  ""AutoRetargetEnabled"": false,
  ""KiteEnabled"": false,
  ""MageEnabled"": false,
  ""SupportEnabled"": false,
  ""ComboEnabled"": false,
  ""SmartSkillEnabled"": true,
  ""AutoAttackKeyVK"": 90,
  ""KiteAttackKeyVK"": 90,
  ""SupportHealKeyVK"": 90,
  ""MageBoltKeyVK"": 88,
  ""TabCycleMs"": 80,
  ""PreRenewalAttackIntervalMs"": 100,
  ""PreRenewalSkillInterruptMs"": 800,
  ""RenewalAttackIntervalMs"": 60,
  ""RenewalSkillInterruptMs"": 400,
  ""MouseSensitivity"": 1.2,
  ""Deadzone"": 0.12,
  ""MovementCurve"": 1.5,
  ""AimSensitivity"": 22.0,
  ""AimDeadzone"": 0.20,
  ""StickDeadzone"": 0.2,
  ""SkillRecommendations"": [""Schlag"", ""Steigerung""],
  ""ClassTips"": ""Die Grundklasse."",
  ""ButtonMappings"": {}
}");

            WriteTestProfile(_tempDir, "mage.json", @"{
  ""Name"": ""Mage"",
  ""Class"": ""Mage"",
  ""AutoAttackEnabled"": false,
  ""AutoRetargetEnabled"": false,
  ""KiteEnabled"": false,
  ""MageEnabled"": true,
  ""SupportEnabled"": false,
  ""ComboEnabled"": false,
  ""SmartSkillEnabled"": true,
  ""AutoAttackKeyVK"": 90,
  ""KiteAttackKeyVK"": 90,
  ""SupportHealKeyVK"": 90,
  ""MageBoltKeyVK"": 88,
  ""TabCycleMs"": 100,
  ""PreRenewalAttackIntervalMs"": 1200,
  ""PreRenewalSkillInterruptMs"": 800,
  ""RenewalAttackIntervalMs"": 800,
  ""RenewalSkillInterruptMs"": 400,
  ""MouseSensitivity"": 1.3,
  ""Deadzone"": 0.15,
  ""MovementCurve"": 1.5,
  ""AimSensitivity"": 25.0,
  ""AimDeadzone"": 0.22,
  ""StickDeadzone"": 0.22,
  ""SkillRecommendations"": [""Fire Bolt"", ""Storm Gust""],
  ""ClassTips"": ""Magier-Klasse."",
  ""ButtonMappings"": {}
}");

            _manager = new ProfileTemplateManager { Logger = s => System.Diagnostics.Debug.WriteLine(s) };

            // Registriere Test-Templates manuell aus den temporären Dateien
            var ctx = AppJsonContext.Default;
            foreach (var file in Directory.GetFiles(_tempDir, "*.json"))
            {
                var json = File.ReadAllText(file);
                var profile = JsonSerializer.Deserialize<Profile>(json, ctx.Profile);
                if (profile != null)
                {
                    var template = ProfileToTemplateForTest(profile, Path.GetFileNameWithoutExtension(file));
                    if (template != null)
                        _manager.RegisterTemplate(template);
                }
            }
        }

        public void Dispose()
        {
            _manager.Dispose();
            try { Directory.Delete(_tempDir, true); } catch { }
        }

        private static void WriteTestProfile(string dir, string fileName, string jsonContent)
        {
            var path = Path.Combine(dir, fileName);
            File.WriteAllText(path, jsonContent);
        }

        private static ProfileTemplate? ProfileToTemplateForTest(Profile profile, string fileNameWithoutExtension)
        {
            if (profile == null) return null;

            var id = fileNameWithoutExtension.ToLowerInvariant();
            var mode = "Pre-Renewal";
            var lowerName = fileNameWithoutExtension.ToLowerInvariant();
            if (lowerName.EndsWith("_renewal")) mode = "Renewal";
            else if (lowerName.EndsWith("_preremewal")) mode = "Pre-Renewal";

            return new ProfileTemplate
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
                    PreRenewal = new TemplateEngineModeSettings
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
                        TabCycleMs = profile.TabCycleMs,
                        AttackIntervalMs = profile.PreRenewalAttackIntervalMs,
                        SkillInterruptMs = profile.PreRenewalSkillInterruptMs
                    },
                    Renewal = new TemplateEngineModeSettings
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
                        TabCycleMs = profile.TabCycleMs,
                        AttackIntervalMs = profile.RenewalAttackIntervalMs,
                        SkillInterruptMs = profile.RenewalSkillInterruptMs
                    }
                },
                ButtonMappings = new Dictionary<string, VirtualKey>(),
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
        }

        // ── Laden ───────────────────────────────────────────────────────

        [Fact]
        public void Constructor_LoadsTestTemplates()
        {
            Assert.Equal(2, _manager.Count);
            Assert.Equal(2, _manager.ListTemplates().Count);
        }

        [Fact]
        public void ListTemplates_ReturnsNonNullIdsAndNames()
        {
            var templates = _manager.ListTemplates();
            Assert.All(templates, t => Assert.NotNull(t.Id));
            Assert.All(templates, t => Assert.NotNull(t.DisplayName));
            Assert.All(templates, t => Assert.NotNull(t.Class));
        }

        // ── GetTemplate ─────────────────────────────────────────────────

        [Fact]
        public void GetTemplate_ExistingId_ReturnsTemplate()
        {
            var first = _manager.ListTemplates().First();
            var result = _manager.GetTemplate(first.Id);
            Assert.NotNull(result);
            Assert.Equal(first.Id, result.Id);
            Assert.Equal(first.DisplayName, result.DisplayName);
        }

        [Fact]
        public void GetTemplate_NonExistingId_ReturnsNull()
            => Assert.Null(_manager.GetTemplate("nonexistent_id"));

        [Fact]
        public void GetTemplate_EmptyId_ReturnsNull()
            => Assert.Null(_manager.GetTemplate(""));

        [Fact]
        public void GetTemplate_NullId_ReturnsNull()
            => Assert.Null(_manager.GetTemplate(null!));

        [Fact]
        public void GetTemplate_CaseInsensitive()
        {
            var first = _manager.ListTemplates().First();
            var result = _manager.GetTemplate(first.Id.ToUpperInvariant());
            Assert.NotNull(result);
            Assert.Equal(first.Id, result.Id);
        }

        // ── Pre-Renewal / Renewal Trennung ──────────────────────────────

        [Fact]
        public void Template_HasSeparatePreRenewalAndRenewalSettings()
        {
            var templates = _manager.ListTemplates();
            Assert.All(templates, t =>
            {
                Assert.NotNull(t.Engines.PreRenewal);
                Assert.NotNull(t.Engines.Renewal);
                Assert.True(t.Engines.PreRenewal.AttackIntervalMs >= 0);
                Assert.True(t.Engines.Renewal.AttackIntervalMs >= 0);
            });
        }

        [Fact]
        public void Template_ModeIsPreRenewalOrRenewal()
        {
            var templates = _manager.ListTemplates();
            Assert.All(templates, t =>
            {
                Assert.Matches("^(Pre-Renewal|Renewal)$", t.Mode);
            });
        }

        [Fact]
        public void Template_PreRenewalAndRenewalHaveDifferentValues()
        {
            var novice = _manager.GetTemplate("novice");
            Assert.NotNull(novice);
            Assert.Equal(100, novice.Engines.PreRenewal.AttackIntervalMs);
            Assert.Equal(60, novice.Engines.Renewal.AttackIntervalMs);
        }

        // ── CreateProfileFromTemplate ───────────────────────────────────

        [Fact]
        public void CreateProfileFromTemplate_ValidTemplate_CreatesProfileWithCorrectValues()
        {
            var template = _manager.ListTemplates().First();
            var profile = _manager.CreateProfileFromTemplate(template.Id, "TestProfile");

            Assert.Equal("TestProfile", profile.Name);
            Assert.Equal(template.Class, profile.Class);
            Assert.True(profile.IsEnabled);
            Assert.False(profile.IsBuiltIn);

            Assert.Equal(template.Engines.PreRenewal.AutoAttackEnabled, profile.AutoAttackEnabled);
            Assert.Equal(template.Engines.PreRenewal.KiteEnabled, profile.KiteEnabled);
            Assert.Equal(template.Engines.PreRenewal.MageEnabled, profile.MageEnabled);
            Assert.Equal(template.Engines.PreRenewal.SupportEnabled, profile.SupportEnabled);

            Assert.Equal(template.Engines.PreRenewal.AttackIntervalMs, profile.PreRenewalAttackIntervalMs);
            Assert.Equal(template.Engines.PreRenewal.SkillInterruptMs, profile.PreRenewalSkillInterruptMs);
            Assert.Equal(template.Engines.Renewal.AttackIntervalMs, profile.RenewalAttackIntervalMs);
            Assert.Equal(template.Engines.Renewal.SkillInterruptMs, profile.RenewalSkillInterruptMs);
        }

        [Fact]
        public void CreateProfileFromTemplate_EmptyName_ThrowsArgumentException()
        {
            var template = _manager.ListTemplates().First();
            Assert.Throws<ArgumentException>(() => _manager.CreateProfileFromTemplate(template.Id, ""));
            Assert.Throws<ArgumentException>(() => _manager.CreateProfileFromTemplate(template.Id, "   "));
        }

        [Fact]
        public void CreateProfileFromTemplate_NonExistingTemplate_ThrowsKeyNotFoundException()
        {
            Assert.Throws<KeyNotFoundException>(() => _manager.CreateProfileFromTemplate("nonexistent", "Test"));
        }

        [Fact]
        public void CreateProfileFromTemplate_InvalidCharacters_Stripped()
        {
            var template = _manager.ListTemplates().First();
            var profile = _manager.CreateProfileFromTemplate(template.Id, "Test</>&*Profile");
            Assert.DoesNotContain("<", profile.Name);
            Assert.DoesNotContain(">", profile.Name);
            Assert.DoesNotContain("*", profile.Name);
        }

        // ── RegisterTemplate ────────────────────────────────────────────

        [Fact]
        public void RegisterTemplate_ValidTemplate_AddsToList()
        {
            var initialCount = _manager.Count;
            var newTemplate = new ProfileTemplate
            {
                Id = "test_template_unique",
                DisplayName = "Test Template",
                Class = "Melee",
                Mode = "Pre-Renewal",
                SkillRecommendations = new List<string> { "Test Skill" },
                Engines = new TemplateEngineSettings
                {
                    PreRenewal = new TemplateEngineModeSettings { AutoAttackEnabled = true },
                    Renewal = new TemplateEngineModeSettings { AutoAttackEnabled = false }
                },
                Sensitivity = new TemplateSensitivity()
            };

            _manager.RegisterTemplate(newTemplate);
            Assert.Equal(initialCount + 1, _manager.Count);
            var retrieved = _manager.GetTemplate("test_template_unique");
            Assert.NotNull(retrieved);
            Assert.Equal("Test Template", retrieved.DisplayName);
        }

        [Fact]
        public void RegisterTemplate_EmptyId_ThrowsArgumentException()
        {
            var template = new ProfileTemplate { Id = "", DisplayName = "Test" };
            Assert.Throws<ArgumentException>(() => _manager.RegisterTemplate(template));
        }

        [Fact]
        public void RegisterTemplate_WhitespaceId_ThrowsArgumentException()
        {
            var template = new ProfileTemplate { Id = "   ", DisplayName = "Test" };
            Assert.Throws<ArgumentException>(() => _manager.RegisterTemplate(template));
        }

        [Fact]
        public void RegisterTemplate_DuplicateId_Overwrites()
        {
            var existing = _manager.ListTemplates().First();
            var updated = new ProfileTemplate
            {
                Id = existing.Id,
                DisplayName = "Updated Display Name",
                Class = existing.Class,
                Mode = existing.Mode,
                Engines = existing.Engines,
                Sensitivity = existing.Sensitivity
            };

            _manager.RegisterTemplate(updated);
            var retrieved = _manager.GetTemplate(existing.Id);
            Assert.NotNull(retrieved);
            Assert.Equal("Updated Display Name", retrieved.DisplayName);
            Assert.Equal(_manager.Count, _manager.ListTemplates().Count);
        }

        // ── Sensitivität ────────────────────────────────────────────────

        [Fact]
        public void Template_HasValidSensitivityValues()
        {
            var templates = _manager.ListTemplates();
            Assert.All(templates, t =>
            {
                Assert.True(t.Sensitivity.MouseSensitivity > 0);
                Assert.True(t.Sensitivity.Deadzone >= 0);
                Assert.True(t.Sensitivity.AimSensitivity > 0);
                Assert.True(t.Sensitivity.StickDeadzone >= 0);
            });
        }

        // ── Skill-Empfehlungen ──────────────────────────────────────────

        [Fact]
        public void Template_HasSkillRecommendations()
        {
            var templates = _manager.ListTemplates();
            Assert.All(templates, t =>
            {
                Assert.NotNull(t.SkillRecommendations);
                if (t.SkillRecommendations.Count > 0)
                {
                    Assert.All(t.SkillRecommendations, s => Assert.NotNull(s));
                }
            });
        }
    }
}
