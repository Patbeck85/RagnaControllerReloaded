using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace RagnaController.Tests
{
    public class ProfileItemValidationTests
    {
        // RO Item IDs für Potions
        private static readonly Dictionary<int, string> ValidPotionIds = new()
        {
            { 501, "Red Potion" },
            { 502, "Blue Potion" },
            { 503, "White Potion" },
            { 504, "Yellow Potion" },
            { 505, "Green Potion" },
            { 545, "Red Slim Potion" },
            { 546, "Blue Slim Potion" },
            { 547, "White Slim Potion" },
            { 548, "Yellow Slim Potion" },
        };

        private static string ProfileDir
        {
            get
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                // Test läuft aus bin/Release/net8.0-windows/ → ../../../src/RagnaController/DefaultProfiles
                var candidate = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "src", "RagnaController", "DefaultProfiles"));
                if (Directory.Exists(candidate)) return candidate;
                // Fallback: bin/.../DefaultProfiles (copied during build)
                candidate = Path.GetFullPath(Path.Combine(baseDir, "DefaultProfiles"));
                if (Directory.Exists(candidate)) return candidate;
                throw new DirectoryNotFoundException($"Profile directory not found. Searched: {baseDir}");
            }
        }

        [Fact]
        public void AllDefaultProfiles_HaveManagedItems()
        {
            var files = Directory.GetFiles(ProfileDir, "*.json");
            Assert.Equal(58, files.Length); // 58 Profile erwartet

            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                Assert.True(root.TryGetProperty("ManagedItems", out var itemsProp),
                    $"Profile {Path.GetFileName(file)} fehlt ManagedItems");

                var items = itemsProp.EnumerateArray().ToList();
                Assert.True(items.Count > 0,
                    $"Profile {Path.GetFileName(file)} hat leere ManagedItems");
            }
        }

        [Fact]
        public void AllManagedItems_HaveValidItemIds()
        {
            var files = Directory.GetFiles(ProfileDir, "*.json");
            var errors = new List<string>();

            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("ManagedItems", out var itemsProp)) continue;

                foreach (var item in itemsProp.EnumerateArray())
                {
                    if (!item.TryGetProperty("ItemId", out var idProp))
                    {
                        errors.Add($"{Path.GetFileName(file)}: Item fehlt ItemId");
                        continue;
                    }

                    int itemId = idProp.GetInt32();
                    if (!ValidPotionIds.ContainsKey(itemId))
                    {
                        errors.Add($"{Path.GetFileName(file)}: Ungültige ItemId {itemId}");
                    }

                    // Prüfen dass Name konsistent mit ItemId ist
                    if (item.TryGetProperty("Name", out var nameProp))
                    {
                        string name = nameProp.GetString() ?? "";
                        if (ValidPotionIds.TryGetValue(itemId, out var expectedName))
                        {
                            if (name != expectedName)
                            {
                                errors.Add($"{Path.GetFileName(file)}: ItemId {itemId} hat Name '{name}', erwartet '{expectedName}'");
                            }
                        }
                    }
                }
            }

            Assert.Empty(errors);
        }

        [Fact]
        public void AllManagedItems_HaveRequiredFields()
        {
            var files = Directory.GetFiles(ProfileDir, "*.json");
            var errors = new List<string>();

            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("ManagedItems", out var itemsProp)) continue;

                foreach (var item in itemsProp.EnumerateArray())
                {
                    var missing = new List<string>();
                    if (!item.TryGetProperty("ItemId", out _)) missing.Add("ItemId");
                    if (!item.TryGetProperty("Name", out _)) missing.Add("Name");
                    if (!item.TryGetProperty("Key", out _)) missing.Add("Key");
                    if (!item.TryGetProperty("HpThresholdPercent", out _)) missing.Add("HpThresholdPercent");
                    if (!item.TryGetProperty("CooldownMs", out _)) missing.Add("CooldownMs");
                    if (!item.TryGetProperty("CheckIntervalMs", out _)) missing.Add("CheckIntervalMs");
                    if (!item.TryGetProperty("Enabled", out _)) missing.Add("Enabled");

                    if (missing.Count > 0)
                    {
                        errors.Add($"{Path.GetFileName(file)}: Item fehlt Felder: {string.Join(", ", missing)}");
                    }
                }
            }

            Assert.Empty(errors);
        }

        [Fact]
        public void AllProfiles_HaveItemManagerEnabledTrue()
        {
            var files = Directory.GetFiles(ProfileDir, "*.json");
            var errors = new List<string>();

            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("ItemManagerEnabled", out var prop) || !prop.GetBoolean())
                {
                    errors.Add($"{Path.GetFileName(file)}: ItemManagerEnabled ist nicht true");
                }
            }

            Assert.Empty(errors);
        }
    }
}
