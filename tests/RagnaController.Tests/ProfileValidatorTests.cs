using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Tests
{
    /// <summary>
    /// GEX-012: ProfileValidator tests — validates all 58 default profiles.
    /// </summary>
    public class ProfileValidatorTests
    {
        private static string DefaultProfilesDir
        {
            get
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                // Test runs from bin/Release/net8.0-windows/ → go up to find src/RagnaController/DefaultProfiles
                var candidate = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "src", "RagnaController", "DefaultProfiles"));
                if (Directory.Exists(candidate)) return candidate;
                // Fallback: bin/.../DefaultProfiles (copied during build)
                candidate = Path.GetFullPath(Path.Combine(baseDir, "DefaultProfiles"));
                if (Directory.Exists(candidate)) return candidate;
                throw new DirectoryNotFoundException($"DefaultProfiles directory not found. Searched: {baseDir}");
            }
        }

        [Fact]
        public void ValidateAll_DefaultProfiles_NoErrors()
        {
            var results = ProfileValidator.ValidateAll(DefaultProfilesDir);

            // Should have 58 profiles
            Assert.Equal(58, results.Count);

            // No errors allowed
            var errors = results.SelectMany(r => r.Errors).ToList();
            Assert.Empty(errors);
        }

        [Fact]
        public void ValidateAll_AllProfilesHaveManagedItems()
        {
            var results = ProfileValidator.ValidateAll(DefaultProfilesDir);

            foreach (var r in results)
            {
                Assert.DoesNotContain(r.Warnings, w => w.Contains("ManagedItems array is empty"));
                Assert.DoesNotContain(r.Warnings, w => w.Contains("Missing 'ManagedItems'"));
            }
        }

        [Fact]
        public void ValidateAll_AllProfilesHaveValidItemIds()
        {
            var results = ProfileValidator.ValidateAll(DefaultProfilesDir);

            foreach (var r in results)
            {
                var itemIdErrors = r.Errors.Where(e => e.Contains("Invalid ItemId")).ToList();
                Assert.Empty(itemIdErrors);
            }
        }

        [Fact]
        public void ValidateAll_AllProfilesHaveItemManagerEnabled()
        {
            var results = ProfileValidator.ValidateAll(DefaultProfilesDir);

            foreach (var r in results)
            {
                Assert.DoesNotContain(r.Warnings, w => w.Contains("ItemManagerEnabled is false"));
            }
        }

        [Fact]
        public void ValidateAll_AllProfilesHaveValidStructure()
        {
            var results = ProfileValidator.ValidateAll(DefaultProfilesDir);

            foreach (var r in results)
            {
                // Each profile should have Name and Class
                Assert.False(string.IsNullOrEmpty(r.ProfileName), $"Profile {r.FileName} missing Name");
                
                // No critical errors
                Assert.Empty(r.Errors);
            }
        }

        [Fact]
        public void ValidateSingle_KnownGoodProfile_Passes()
        {
            var knightPath = Path.Combine(DefaultProfilesDir, "knight.json");
            var result = ProfileValidator.ValidateSingle(knightPath);

            Assert.True(result.IsValid, $"Knight profile has errors: {string.Join(", ", result.Errors)}");
            Assert.Equal("Knight", result.ProfileName);
        }

        [Fact]
        public void ValidateSingle_DetectsInvalidItemId()
        {
            // Create a temp profile with invalid ItemId
            var tempDir = Path.Combine(Path.GetTempPath(), "ragna-validator-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var badProfile = new Profile
                {
                    Name = "TestBad",
                    Class = "Novice",
                    ItemManagerEnabled = true,
                    ManagedItems = new List<ItemConfig>
                    {
                        new ItemConfig { ItemId = 9999, Name = "Invalid", Key = VirtualKey.F1, HpThresholdPercent = 50, CooldownMs = 3000, CheckIntervalMs = 1000, Enabled = true }
                    }
                };

                var filePath = Path.Combine(tempDir, "TestBad.json");
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(filePath, JsonSerializer.Serialize(badProfile, opts));

                var result = ProfileValidator.ValidateSingle(filePath);

                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.Contains("Invalid ItemId 9999"));
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [Fact]
        public void ValidateSingle_DetectsMissingRequiredFields()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "ragna-validator-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                // JSON with missing required fields in ManagedItems
                var manualJson = """
                {
                  "Name": "TestMissing",
                  "Class": "Novice",
                  "ItemManagerEnabled": true,
                  "ManagedItems": [
                    { "ItemId": 501, "Name": "Red Potion", "Key": 120 }
                  ]
                }
                """;
                var filePath = Path.Combine(tempDir, "TestMissing.json");
                File.WriteAllText(filePath, manualJson);

                var result = ProfileValidator.ValidateSingle(filePath);

                Assert.False(result.IsValid);
                Assert.Contains(result.Errors, e => e.Contains("Missing HpThresholdPercent"));
                Assert.Contains(result.Errors, e => e.Contains("Missing CooldownMs"));
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [Fact]
        public void ValidateSingle_ValidatesNumericRanges()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "ragna-validator-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var manualJson = """
                {
                  "Name": "TestRange",
                  "Class": "Novice",
                  "PreRenewalAttackIntervalMs": 5,
                  "RenewalAttackIntervalMs": 600,
                  "TabCycleMs": 5,
                  "ItemManagerEnabled": true,
                  "ManagedItems": [
                    { "ItemId": 501, "Name": "Red Potion", "Key": 120, "HpThresholdPercent": 150, "CooldownMs": 3000, "CheckIntervalMs": 1000, "Enabled": true, "MinSpRequired": 0 }
                  ]
                }
                """;
                var filePath = Path.Combine(tempDir, "TestRange.json");
                File.WriteAllText(filePath, manualJson);

                var result = ProfileValidator.ValidateSingle(filePath);

                // Should have warnings for out-of-range numeric fields
                Assert.Contains(result.Warnings, w => w.Contains("PreRenewalAttackIntervalMs"));
                Assert.Contains(result.Warnings, w => w.Contains("RenewalAttackIntervalMs"));
                Assert.Contains(result.Warnings, w => w.Contains("TabCycleMs"));
                // HpThresholdPercent 150 should be error (0-100)
                Assert.Contains(result.Errors, e => e.Contains("HpThresholdPercent 150 out of range"));
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}