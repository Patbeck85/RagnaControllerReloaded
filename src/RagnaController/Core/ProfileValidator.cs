using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Core
{
    /// <summary>
    /// GEX-012: ProfileValidator — scans all profiles and reports validation errors.
    /// Checks: Class/Name match, valid ItemIds, complete skill mappings, required fields.
    /// </summary>
    public static class ProfileValidator
    {
        private static readonly HashSet<int> ValidPotionItemIds = new()
        {
            501, 502, 503, 504, 505,  // Red, Blue, White, Yellow, Green Potion
            545, 546, 547, 548        // Red Slim, Blue Slim, White Slim, Yellow Slim
        };

        private static readonly HashSet<string> ValidClasses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Novice", "Swordsman", "Mage", "Archer", "Acolyte", "Merchant", "Thief",
            "Knight", "Wizard", "Hunter", "Priest", "Blacksmith", "Assassin",
            "Crusader", "Monk", "Sage", "Rogue", "Alchemist", "Bard", "Dancer",
            "Lord_Knight", "High_Wizard", "Sniper", "High_Priest", "Whitesmith",
            "Guillotine_Cross", "Champion", "Professor", "Stalker", "Creator",
            "Clown", "Gypsy", "Paladin", "Ranger", "Archbishop", "Mechanic",
            "Rune_Knight", "Warlock", "Royal_Guard", "Sorcerer", "Minstrel", "Wanderer",
            "Genetic", "Guillotine_Cross", "Shadow_Chaser", "Sura", "Kagerou_Oboro",
            "Shinkiro_Shiranui", "Rebellion", "Star_Emperor", "Star_Gladiator",
            "Super_Novice", "Taekwon", "Soul_Linker", "Soul_Reaper", "Kagerou_Oboro"
        };

        public sealed class ValidationResult
        {
            public string ProfileName { get; set; } = "";
            public string FileName { get; set; } = "";
            public List<string> Errors { get; } = new();
            public List<string> Warnings { get; } = new();
            public bool IsValid => Errors.Count == 0;
        }

        public static List<ValidationResult> ValidateAll(string profilesDirectory)
        {
            var results = new List<ValidationResult>();
            if (!Directory.Exists(profilesDirectory))
            {
                results.Add(new ValidationResult { FileName = profilesDirectory, Errors = { "Directory not found" } });
                return results;
            }

            var files = Directory.GetFiles(profilesDirectory, "*.json");
            foreach (var file in files)
            {
                if (file.EndsWith(".bak.json", StringComparison.OrdinalIgnoreCase)) continue;
                var result = ValidateSingle(file);
                results.Add(result);
            }

            return results;
        }

        public static ValidationResult ValidateSingle(string filePath)
        {
            var result = new ValidationResult { FileName = Path.GetFileName(filePath) };
            try
            {
                var json = File.ReadAllText(filePath);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Name
                if (root.TryGetProperty("Name", out var nameProp))
                    result.ProfileName = nameProp.GetString() ?? "";
                else
                    result.Errors.Add("Missing 'Name' field");

                // Class
                string? className = null;
                if (root.TryGetProperty("Class", out var classProp))
                    className = classProp.GetString();
                else
                    result.Errors.Add("Missing 'Class' field");

                // Name/Class consistency
                if (!string.IsNullOrEmpty(result.ProfileName) && !string.IsNullOrEmpty(className))
                {
                    if (!string.Equals(result.ProfileName, className, StringComparison.OrdinalIgnoreCase))
                    {
                        // Some profiles use different naming (e.g., "Merchant" class = "Blacksmith")
                        var knownMismatches = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "Merchant", "Blacksmith" }
                        };
                        if (!knownMismatches.TryGetValue(result.ProfileName, out var expectedClass) ||
                            !string.Equals(expectedClass, className, StringComparison.OrdinalIgnoreCase))
                        {
                            result.Warnings.Add($"Profile Name '{result.ProfileName}' differs from Class '{className}'");
                        }
                    }
                }

                // Valid Class
                if (!string.IsNullOrEmpty(className) && !ValidClasses.Contains(className))
                {
                    result.Warnings.Add($"Class '{className}' not in known RO classes list");
                }

                // ManagedItems validation
                if (root.TryGetProperty("ManagedItems", out var itemsProp))
                {
                    var items = itemsProp.EnumerateArray().ToList();
                    if (items.Count == 0)
                    {
                        result.Warnings.Add("ManagedItems array is empty");
                    }
                    else
                    {
                        for (int i = 0; i < items.Count; i++)
                        {
                            var item = items[i];
                            ValidateItem(item, i, result);
                        }
                    }
                }
                else
                {
                    result.Warnings.Add("Missing 'ManagedItems' array");
                }

                // ItemManagerEnabled
                if (root.TryGetProperty("ItemManagerEnabled", out var imeProp))
                {
                    if (!imeProp.GetBoolean())
                        result.Warnings.Add("ItemManagerEnabled is false (auto-pot disabled)");
                }
                else
                {
                    result.Warnings.Add("Missing 'ItemManagerEnabled' field");
                }

                // ButtonMappings validation
                if (root.TryGetProperty("ButtonMappings", out var btnProp))
                {
                    foreach (var btn in btnProp.EnumerateObject())
                    {
                        if (btn.Value.TryGetProperty("Type", out var typeProp))
                        {
                            var type = typeProp.GetString();
                            if (type == "Key" && btn.Value.TryGetProperty("Key", out var keyProp))
                            {
                                // Key can be string (F1) or int (120)
                                if (keyProp.ValueKind == JsonValueKind.String)
                                {
                                    var keyStr = keyProp.GetString();
                                    if (string.IsNullOrEmpty(keyStr) || !IsValidVirtualKeyString(keyStr))
                                        result.Warnings.Add($"Button '{btn.Name}': Unknown key '{keyStr}'");
                                }
                            }
                        }
                    }
                }

                // SkillRecommendations
                if (root.TryGetProperty("SkillRecommendations", out var skillsProp))
                {
                    if (skillsProp.GetArrayLength() == 0)
                        result.Warnings.Add("SkillRecommendations array is empty");
                }
                else
                {
                    result.Warnings.Add("Missing 'SkillRecommendations'");
                }

                // ClassTips
                if (!root.TryGetProperty("ClassTips", out _))
                    result.Warnings.Add("Missing 'ClassTips'");

                // Key numeric fields
                ValidateNumericField(root, "PreRenewalAttackIntervalMs", 20, 500, result);
                ValidateNumericField(root, "RenewalAttackIntervalMs", 20, 500, result);
                ValidateNumericField(root, "TabCycleMs", 10, 500, result);
                ValidateNumericField(root, "PreRenewalSkillInterruptMs", 100, 2000, result);
                ValidateNumericField(root, "RenewalSkillInterruptMs", 100, 2000, result);
            }
            catch (JsonException ex)
            {
                result.Errors.Add($"JSON parse error: {ex.Message}");
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Validation error: {ex.Message}");
            }

            return result;
        }

        private static void ValidateItem(JsonElement item, int index, ValidationResult result)
        {
            string prefix = $"ManagedItems[{index}]";

            if (!item.TryGetProperty("ItemId", out var idProp) || idProp.ValueKind != JsonValueKind.Number)
            {
                result.Errors.Add($"{prefix}: Missing or invalid ItemId");
            }
            else
            {
                int itemId = idProp.GetInt32();
                if (!ValidPotionItemIds.Contains(itemId))
                {
                    result.Errors.Add($"{prefix}: Invalid ItemId {itemId} (not a known potion ID)");
                }
            }

            if (!item.TryGetProperty("Name", out var nameProp) || string.IsNullOrEmpty(nameProp.GetString()))
                result.Errors.Add($"{prefix}: Missing Name");

            if (!item.TryGetProperty("Key", out var keyProp) || keyProp.ValueKind != JsonValueKind.Number)
                result.Errors.Add($"{prefix}: Missing or invalid Key (VirtualKey int)");

            if (!item.TryGetProperty("HpThresholdPercent", out var hpProp) || hpProp.ValueKind != JsonValueKind.Number)
                result.Errors.Add($"{prefix}: Missing HpThresholdPercent");
            else
            {
                int hp = hpProp.GetInt32();
                if (hp < 0 || hp > 100)
                    result.Errors.Add($"{prefix}: HpThresholdPercent {hp} out of range (0-100)");
            }

            if (!item.TryGetProperty("CooldownMs", out var cdProp) || cdProp.ValueKind != JsonValueKind.Number)
                result.Errors.Add($"{prefix}: Missing CooldownMs");
            else if (cdProp.GetInt32() < 1)
                result.Errors.Add($"{prefix}: CooldownMs must be >= 1");

            if (!item.TryGetProperty("CheckIntervalMs", out var ciProp) || ciProp.ValueKind != JsonValueKind.Number)
                result.Errors.Add($"{prefix}: Missing CheckIntervalMs");
            else if (ciProp.GetInt32() < 1)
                result.Errors.Add($"{prefix}: CheckIntervalMs must be >= 1");

            if (!item.TryGetProperty("Enabled", out var enProp) || enProp.ValueKind != JsonValueKind.True)
                result.Warnings.Add($"{prefix}: Enabled is not true");

            if (!item.TryGetProperty("MinSpRequired", out var spProp) || spProp.ValueKind != JsonValueKind.Number)
                result.Errors.Add($"{prefix}: Missing MinSpRequired");
        }

        private static void ValidateNumericField(JsonElement root, string field, int min, int max, ValidationResult result)
        {
            if (root.TryGetProperty(field, out var prop) && prop.ValueKind == JsonValueKind.Number)
            {
                int val = prop.GetInt32();
                if (val < min || val > max)
                    result.Warnings.Add($"{field} = {val} outside typical range ({min}-{max})");
            }
            else
            {
                result.Warnings.Add($"Missing or invalid '{field}'");
            }
        }

        private static bool IsValidVirtualKeyString(string keyStr)
        {
            var validKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
                "A","B","C","D","E","F","G","H","I","J","K","L","M",
                "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
                "0","1","2","3","4","5","6","7","8","9",
                "Space","Enter","Tab","Shift","Ctrl","Alt",
                "Left","Right","Up","Down",
                "LButton","RButton","MButton",
                "LShift","RShift","LControl","RControl","LAlt","RAlt"
            };
            return validKeys.Contains(keyStr);
        }

        public static void PrintReport(List<ValidationResult> results, Action<string>? logger = null)
        {
            var log = logger ?? (s => Console.WriteLine(s));
            int totalErrors = results.Sum(r => r.Errors.Count);
            int totalWarnings = results.Sum(r => r.Warnings.Count);
            int validCount = results.Count(r => r.IsValid);

            log($"=== ProfileValidator Report ===");
            log($"Profiles checked: {results.Count}");
            log($"Valid: {validCount} | With Errors: {results.Count - validCount}");
            log($"Total Errors: {totalErrors} | Total Warnings: {totalWarnings}");
            log($"");

            foreach (var r in results.Where(x => !x.IsValid))
            {
                log($"❌ {r.ProfileName} ({r.FileName}) - {r.Errors.Count} ERRORS:");
                foreach (var e in r.Errors) log($"   - {e}");
            }

            foreach (var r in results.Where(x => x.IsValid && x.Warnings.Count > 0))
            {
                log($"⚠️ {r.ProfileName} ({r.FileName}) - {r.Warnings.Count} WARNINGS:");
                foreach (var w in r.Warnings) log($"   - {w}");
            }

            foreach (var r in results.Where(x => x.IsValid && x.Warnings.Count == 0))
            {
                log($"✅ {r.ProfileName} ({r.FileName}) - OK");
            }
        }
    }
}