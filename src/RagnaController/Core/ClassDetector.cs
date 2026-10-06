using System;
using System.Collections.Generic;
using System.Linq;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Core
{
    /// <summary>
    /// FEAT-005: Extended engine preset data with per-class configuration.
    /// Holds class-specific engine engine flags beyond the basic category (Melee/Ranged/Caster/etc.).
    /// </summary>
    public struct ClassPresetData
    {
        /// <summary>Enable auto-attack for this class</summary>
        public bool AutoAttack;
        /// <summary>Enable kite/retreat behavior</summary>
        public bool Kite;
        /// <summary>Enable mage/casting engine</summary>
        public bool Mage;
        /// <summary>Enable support/buff engine</summary>
        public bool Support;
        /// <summary>Enable combo system</summary>
        public bool Combo;
        /// <summary>Enable mob sweep (AoE clearing)</summary>
        public bool MobSweep;
        /// <summary>Enable auto-retaliate when hit</summary>
        public bool AutoRetaliate;
        /// <summary>Enable party member targeting</summary>
        public bool PartyTargeting;

        /// <summary>Default Melee preset (Swordsman, Knight, Crusader, Blacksmith)</summary>
        public static ClassPresetData MeleeDefault => new() { AutoAttack = true, Kite = false, Mage = false, Support = false, Combo = false, MobSweep = false, AutoRetaliate = false, PartyTargeting = false };

        /// <summary>Default Ranged preset (Archer, Hunter, Bard, Dancer, Gunslinger, Rebellion)</summary>
        public static ClassPresetData RangedDefault => new() { AutoAttack = true, Kite = true, Mage = false, Support = false, Combo = false, MobSweep = false, AutoRetaliate = false, PartyTargeting = false };

        /// <summary>Default Caster preset (Mage, Wizard, Sage, Professor, Alchemist)</summary>
        public static ClassPresetData CasterDefault => new() { AutoAttack = false, Kite = false, Mage = true, Support = true, Combo = false, MobSweep = false, AutoRetaliate = false, PartyTargeting = false };

        /// <summary>Default Hybrid preset (Thief, Assassin, Rogue, Stalker, Monk, Taekwon, Ninja, Kagerou, Oboro)</summary>
        public static ClassPresetData HybridDefault => new() { AutoAttack = true, Kite = true, Mage = true, Support = false, Combo = true, MobSweep = false, AutoRetaliate = false, PartyTargeting = false };

        /// <summary>Default Support preset (Acolyte, Priest, Soul Linker)</summary>
        public static ClassPresetData SupportDefault => new() { AutoAttack = false, Kite = false, Mage = false, Support = true, Combo = false, MobSweep = false, AutoRetaliate = false, PartyTargeting = true };
    }

    /// <summary>
    /// FEAT-007: Class-specific rotation data for SkillOrchestrator.
    /// </summary>
    public class ClassRotationData
    {
        public string ClassName { get; set; } = "";
        public EnginePreset PresetType { get; set; } = EnginePreset.Melee;
        public RotationConfig RotationConfig { get; set; } = new();
    }

    /// <summary>
    /// FEAT-004: Auto-class detection from keybinds.
    /// Analyzes Profile.ButtonMappings for class-specific skills and assigns appropriate engine preset.
    /// FEAT-007: Extended to provide rotation configs.
    /// </summary>
    public static class ClassDetector
    {
        // Known RO skill key mappings per class (VirtualKey -> class hints with weights)
                // Format: VirtualKey -> List of (ClassName, Weight, SkillCategory)
                // Weight: 3 = signature skill (unique to class), 2 = class-specific, 1 = shared across category
                // SkillCategory: "weapon", "offensive", "defensive", "support", "utility", "signature"
                private static readonly Dictionary<VirtualKey, List<(string Class, int Weight, string Category)>> SkillToClassMap = BuildSkillToClassMap();

        /// <summary>
        /// Builds the skill-to-class map programmatically so that multiple classes sharing a single
        /// keybind (e.g. F1 = Swordsman Bash AND High Wizard) accumulate in one list instead of
        /// silently overwriting each other via duplicate keyed collection-initializer entries (TECH-030).
        /// </summary>
        private static Dictionary<VirtualKey, List<(string Class, int Weight, string Category)>> BuildSkillToClassMap()
        {
            var map = new Dictionary<VirtualKey, List<(string Class, int Weight, string Category)>>();

            void AddEntry(VirtualKey vk, string className, int weight, string category)
            {
                if (map.TryGetValue(vk, out var entries))
                    entries.Add((className, weight, category));
                else
                    map[vk] = new List<(string Class, int Weight, string Category)> { (className, weight, category) };
            }

            // F1
            AddEntry(VirtualKey.F1, "Swordsman", 2, "offensive");
            AddEntry(VirtualKey.F1, "Knight", 2, "offensive");
            AddEntry(VirtualKey.F1, "Crusader", 1, "offensive");
            // F2
            AddEntry(VirtualKey.F2, "Swordsman", 2, "offensive");
            AddEntry(VirtualKey.F2, "Knight", 2, "offensive");
            AddEntry(VirtualKey.F2, "Crusader", 1, "offensive");
            // F3
            AddEntry(VirtualKey.F3, "Knight", 3, "signature");
            AddEntry(VirtualKey.F3, "Crusader", 2, "offensive");
            // F4
            AddEntry(VirtualKey.F4, "Knight", 3, "signature");
            // F5
            AddEntry(VirtualKey.F5, "Crusader", 3, "signature");
            // F6
            AddEntry(VirtualKey.F6, "Crusader", 3, "signature");
            // F7
            AddEntry(VirtualKey.F7, "Lord Knight", 3, "signature");
            AddEntry(VirtualKey.F7, "Paladin", 2, "offensive");
            // F8
            AddEntry(VirtualKey.F8, "Paladin", 3, "signature");
            // F7
            AddEntry(VirtualKey.F7, "Mage", 2, "offensive");
            AddEntry(VirtualKey.F7, "Wizard", 2, "offensive");
            AddEntry(VirtualKey.F7, "Sage", 1, "offensive");
            AddEntry(VirtualKey.F7, "Professor", 1, "offensive");
            // F8
            AddEntry(VirtualKey.F8, "Mage", 2, "offensive");
            AddEntry(VirtualKey.F8, "Wizard", 2, "offensive");
            AddEntry(VirtualKey.F8, "Sage", 1, "offensive");
            AddEntry(VirtualKey.F8, "Professor", 1, "offensive");
            // F9
            AddEntry(VirtualKey.F9, "Mage", 2, "offensive");
            AddEntry(VirtualKey.F9, "Wizard", 2, "offensive");
            AddEntry(VirtualKey.F9, "Sage", 1, "offensive");
            AddEntry(VirtualKey.F9, "Professor", 1, "offensive");
            // F10
            AddEntry(VirtualKey.F10, "Wizard", 3, "signature");
            AddEntry(VirtualKey.F10, "Professor", 2, "offensive");
            // F11
            AddEntry(VirtualKey.F11, "Wizard", 3, "signature");
            // F12
            AddEntry(VirtualKey.F12, "Sage", 3, "signature");
            AddEntry(VirtualKey.F12, "Professor", 2, "support");
            // F1
            AddEntry(VirtualKey.F1, "High Wizard", 3, "signature");
            // F2
            AddEntry(VirtualKey.F2, "Professor", 3, "signature");
            // D1
            AddEntry(VirtualKey.D1, "Archer", 2, "offensive");
            AddEntry(VirtualKey.D1, "Hunter", 2, "offensive");
            AddEntry(VirtualKey.D1, "Bard", 1, "offensive");
            AddEntry(VirtualKey.D1, "Dancer", 1, "offensive");
            // D2
            AddEntry(VirtualKey.D2, "Hunter", 3, "signature");
            AddEntry(VirtualKey.D2, "Bard", 2, "offensive");
            // D3
            AddEntry(VirtualKey.D3, "Bard", 3, "signature");
            AddEntry(VirtualKey.D3, "Dancer", 2, "offensive");
            // D4
            AddEntry(VirtualKey.D4, "Hunter", 3, "signature");
            // D5
            AddEntry(VirtualKey.D5, "Sniper", 3, "signature");
            AddEntry(VirtualKey.D5, "Clown", 2, "offensive");
            // D6
            AddEntry(VirtualKey.D6, "Sniper", 2, "offensive");
            AddEntry(VirtualKey.D6, "Gypsy", 2, "offensive");
            // D5
            AddEntry(VirtualKey.D5, "Thief", 2, "offensive");
            AddEntry(VirtualKey.D5, "Assassin", 2, "offensive");
            AddEntry(VirtualKey.D5, "Rogue", 2, "offensive");
            AddEntry(VirtualKey.D5, "Stalker", 1, "offensive");
            // D6
            AddEntry(VirtualKey.D6, "Assassin", 3, "signature");
            AddEntry(VirtualKey.D6, "Stalker", 2, "offensive");
            // D7
            AddEntry(VirtualKey.D7, "Rogue", 3, "signature");
            AddEntry(VirtualKey.D7, "Stalker", 2, "offensive");
            // D8
            AddEntry(VirtualKey.D8, "Stalker", 3, "signature");
            // D9
            AddEntry(VirtualKey.D9, "Assassin Cross", 3, "signature");
            // D0
            AddEntry(VirtualKey.D0, "Stalker", 3, "signature");
            // D9
            AddEntry(VirtualKey.D9, "Merchant", 2, "offensive");
            AddEntry(VirtualKey.D9, "Blacksmith", 2, "offensive");
            AddEntry(VirtualKey.D9, "Alchemist", 1, "offensive");
            // D0
            AddEntry(VirtualKey.D0, "Blacksmith", 3, "signature");
            // Q
            AddEntry(VirtualKey.Q, "Alchemist", 3, "signature");
            // W
            AddEntry(VirtualKey.W, "Alchemist", 2, "support");
            // E
            AddEntry(VirtualKey.E, "Whitesmith", 3, "signature");
            // R
            AddEntry(VirtualKey.R, "Creator", 3, "signature");
            // W
            AddEntry(VirtualKey.W, "Acolyte", 2, "support");
            AddEntry(VirtualKey.W, "Priest", 2, "support");
            AddEntry(VirtualKey.W, "Monk", 1, "support");
            // E
            AddEntry(VirtualKey.E, "Priest", 3, "signature");
            AddEntry(VirtualKey.E, "Monk", 2, "support");
            // R
            AddEntry(VirtualKey.R, "Monk", 3, "signature");
            // T
            AddEntry(VirtualKey.T, "Monk", 2, "offensive");
            // Y
            AddEntry(VirtualKey.Y, "Priest", 3, "signature");
            // U
            AddEntry(VirtualKey.U, "High Priest", 3, "signature");
            // I
            AddEntry(VirtualKey.I, "Champion", 3, "signature");
            // Y
            AddEntry(VirtualKey.Y, "Taekwon", 3, "signature");
            AddEntry(VirtualKey.Y, "Star Gladiator", 2, "offensive");
            // U
            AddEntry(VirtualKey.U, "Star Gladiator", 3, "signature");
            // I
            AddEntry(VirtualKey.I, "Soul Linker", 3, "signature");
            AddEntry(VirtualKey.I, "Gunslinger", 3, "signature");
            AddEntry(VirtualKey.I, "Rebellion", 2, "offensive");
            // O
            AddEntry(VirtualKey.O, "Rebellion", 3, "signature");
            // P
            AddEntry(VirtualKey.P, "Gunslinger", 2, "offensive");
            AddEntry(VirtualKey.P, "Rebellion", 2, "offensive");
            AddEntry(VirtualKey.P, "Ninja", 3, "signature");
            AddEntry(VirtualKey.P, "Kagerou", 2, "offensive");
            AddEntry(VirtualKey.P, "Oboro", 2, "offensive");
            // A
            AddEntry(VirtualKey.A, "Kagerou", 3, "signature");
            // S
            AddEntry(VirtualKey.S, "Oboro", 3, "signature");
            // H
            AddEntry(VirtualKey.H, "Super Novice", 3, "signature");
            // J
            AddEntry(VirtualKey.J, "Super Novice", 2, "support");
            // D1
            AddEntry(VirtualKey.D1, "Archer", 2, "offensive");
            AddEntry(VirtualKey.D1, "Hunter", 2, "offensive");
            AddEntry(VirtualKey.D1, "Bard", 1, "offensive");
            AddEntry(VirtualKey.D1, "Dancer", 1, "offensive");
            // D2
            AddEntry(VirtualKey.D2, "Hunter", 3, "signature");
            AddEntry(VirtualKey.D2, "Bard", 2, "offensive");
            // D3
            AddEntry(VirtualKey.D3, "Bard", 3, "signature");
            AddEntry(VirtualKey.D3, "Dancer", 2, "offensive");
            // D4
            AddEntry(VirtualKey.D4, "Hunter", 3, "signature");
            // D5
            AddEntry(VirtualKey.D5, "Thief", 2, "offensive");
            AddEntry(VirtualKey.D5, "Assassin", 2, "offensive");
            AddEntry(VirtualKey.D5, "Rogue", 2, "offensive");
            AddEntry(VirtualKey.D5, "Stalker", 1, "offensive");
            // D6
            AddEntry(VirtualKey.D6, "Assassin", 3, "signature");
            AddEntry(VirtualKey.D6, "Stalker", 2, "offensive");
            // D7
            AddEntry(VirtualKey.D7, "Rogue", 3, "signature");
            AddEntry(VirtualKey.D7, "Stalker", 2, "offensive");
            // D8
            AddEntry(VirtualKey.D8, "Stalker", 3, "signature");
            // D9
            AddEntry(VirtualKey.D9, "Merchant", 2, "offensive");
            AddEntry(VirtualKey.D9, "Blacksmith", 2, "offensive");
            AddEntry(VirtualKey.D9, "Alchemist", 1, "offensive");
            // D0
            AddEntry(VirtualKey.D0, "Blacksmith", 3, "signature");

            return map;
        }


        // Class to engine preset mapping
        private static readonly Dictionary<string, EnginePreset> ClassToPreset = new(StringComparer.OrdinalIgnoreCase)
        {
            // Melee presets
            ["Swordsman"] = EnginePreset.Melee,
            ["Knight"] = EnginePreset.Melee,
            ["Crusader"] = EnginePreset.Melee,
            ["Blacksmith"] = EnginePreset.Melee,

            // Ranged presets
            ["Archer"] = EnginePreset.Ranged,
            ["Hunter"] = EnginePreset.Ranged,
            ["Bard"] = EnginePreset.Ranged,
            ["Dancer"] = EnginePreset.Ranged,
            ["Gunslinger"] = EnginePreset.Ranged,
            ["Rebellion"] = EnginePreset.Ranged,

            // Caster presets
            ["Mage"] = EnginePreset.Caster,
            ["Wizard"] = EnginePreset.Caster,
            ["Sage"] = EnginePreset.Caster,
            ["Professor"] = EnginePreset.Caster,
            ["Alchemist"] = EnginePreset.Caster,

            // Assassin/Thief presets (hybrid)
            ["Thief"] = EnginePreset.Hybrid,
            ["Assassin"] = EnginePreset.Hybrid,
            ["Rogue"] = EnginePreset.Hybrid,
            ["Stalker"] = EnginePreset.Hybrid,

            // Support presets
            ["Acolyte"] = EnginePreset.Support,
            ["Priest"] = EnginePreset.Support,
            ["Monk"] = EnginePreset.Hybrid, // Monk is hybrid melee/caster

            // Special
            ["Taekwon"] = EnginePreset.Hybrid,
            ["Soul Linker"] = EnginePreset.Support,
            ["Star Gladiator"] = EnginePreset.Hybrid,
            ["Ninja"] = EnginePreset.Hybrid,
            ["Kagerou"] = EnginePreset.Hybrid,
            ["Oboro"] = EnginePreset.Hybrid,
            ["Super Novice"] = EnginePreset.Melee,
        };

        /// <summary>
                /// Detects RO class from Profile.ButtonMappings using weighted heuristic scoring.
                /// Returns the most likely class based on mapped skills, with signature skills weighted higher.
                /// </summary>
                public static string DetectClass(Profile profile)
                {
                    if (profile?.ButtonMappings == null || profile.ButtonMappings.Count == 0)
                        return "Melee"; // Default fallback

                    var classScores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    foreach (var kvp in profile.ButtonMappings)
                    {
                        var buttonKey = kvp.Key;
                        var action = kvp.Value;

                        // Only count actual skill actions (not movement, basic attack, etc.)
                        if (!IsSkillAction(action))
                            continue;

                        // Use the VirtualKey directly from ButtonKey (ignoring modifier for class detection)
                        var vk = buttonKey.Key;
                        if (vk == VirtualKey.None)
                            continue;

                        if (SkillToClassMap.TryGetValue(vk, out var classEntries))
                        {
                            foreach (var entry in classEntries)
                            {
                                // Weighted scoring: signature skills (weight=3) count more
                                int scoreMultiplier = entry.Weight;
                                classScores[entry.Class] = classScores.GetValueOrDefault(entry.Class) + scoreMultiplier;
                            }
                        }
                    }

                    if (classScores.Count == 0)
                        return profile.Class; // Keep existing if no skills mapped

                    // Return class with highest weighted score - manual max tracking instead of LINQ (zero allocation)
                    string bestClass = "";
                    int bestScore = int.MinValue;
                    foreach (var kvp in classScores)
                    {
                        if (kvp.Value > bestScore)
                        {
                            bestScore = kvp.Value;
                            bestClass = kvp.Key;
                        }
                    }
                    return bestClass;
                }

        /// <summary>
                /// Determines if a ButtonAction represents a class-specific skill.
                /// </summary>
                private static bool IsSkillAction(ButtonAction action)
                {
                    // Only Key-type actions can be skills
                    if (action.Type != ActionType.Key)
                        return false;

                    // Movement, basic attack, potion, etc. are not class-specific skills.
                    // A skill is a Key action whose VirtualKey appears in SkillToClassMap.
                    // This ensures only mapped skill keys (F-keys, number keys, letter keys for skills)
                    // are counted, not movement keys (arrows, WASD) or basic attack.
                    return action.Key != VirtualKey.None && SkillToClassMap.ContainsKey(action.Key);
                }

        /// <summary>
        /// Gets the recommended EnginePreset for a detected class.
        /// </summary>
        public static EnginePreset GetPresetForClass(string className)
        {
            return ClassToPreset.TryGetValue(className, out var preset) ? preset : EnginePreset.Melee;
        }

        /// <summary>
        /// FEAT-007: Gets the rotation config for a class.
        /// </summary>
        public static RotationConfig GetRotationConfig(string className)
        {
            var preset = GetPresetForClass(className);
            var provider = new DefaultRotationProvider();
            return provider.GetRotation(className);
        }

        /// <summary>
        /// FEAT-007: Gets the rotation config for an EnginePreset.
        /// </summary>
        public static RotationConfig GetRotationConfig(EnginePreset preset)
        {
            var provider = new DefaultRotationProvider();
            return provider.GetRotation(preset);
        }

        /// <summary>
        /// Applies the detected class preset to the EngineOrchestrator.
        /// Uses ClassPresetData for class-specific engine configuration.
        /// </summary>
        public static void ApplyClassPreset(EngineOrchestrator orchestrator, Profile profile, EnginePreset preset)
        {
            if (orchestrator == null || profile == null)
                return;

            // Get the class-specific preset data
            var classData = preset switch
            {
                EnginePreset.Melee => ClassPresetData.MeleeDefault,
                EnginePreset.Ranged => ClassPresetData.RangedDefault,
                EnginePreset.Caster => ClassPresetData.CasterDefault,
                EnginePreset.Hybrid => ClassPresetData.HybridDefault,
                EnginePreset.Support => ClassPresetData.SupportDefault,
                _ => ClassPresetData.MeleeDefault
            };

            profile.Class = preset.ToString();

            // Apply class-specific engine configuration
            orchestrator.AutoTarget.AutoAttackEnabled = classData.AutoAttack;
            orchestrator.AutoTarget.AutoRetargetEnabled = classData.AutoAttack; // reuse same flag
            orchestrator.Kite.KiteEnabled = classData.Kite;
            orchestrator.Mage.MageEnabled = classData.Mage;
            orchestrator.Support.SupportEnabled = classData.Support;
            orchestrator.Combo.Enabled = classData.Combo;
            orchestrator.MobSweep.MobSweepEnabled = classData.MobSweep;

            // FEAT-005: Additional configuration flags
            orchestrator.AutoTarget.AutoRetaliateEnabled = classData.AutoRetaliate;
            orchestrator.AutoTarget.PartyTargetingEnabled = classData.PartyTargeting;

            // FEAT-007: Load rotation config for this class
            var rotationConfig = GetRotationConfig(preset);
            orchestrator.SkillOrchestrator.LoadRotation(rotationConfig);
            orchestrator.SkillOrchestrator.SetEnabled(true);

            orchestrator.SubscribeToLog($"[Engine] Auto-class applied: {preset} ({profile.Class})");
        }

        // Legacy Apply*Preset methods kept for backward compatibility
        private static void ApplyMeleePreset(EngineOrchestrator o, Profile p)
        {
            // Melee: Movement + AutoTarget, disable caster engines
            o.AutoTarget.AutoAttackEnabled = p.AutoAttackEnabled;
            o.AutoTarget.AutoRetargetEnabled = p.AutoRetargetEnabled;
            o.Kite.KiteEnabled = p.KiteEnabled;
            o.Mage.MageEnabled = false;
            o.Support.SupportEnabled = false;
            o.Combo.Enabled = p.ComboEnabled;
            o.MobSweep.MobSweepEnabled = p.MobSweepEnabled;
        }

        private static void ApplyRangedPreset(EngineOrchestrator o, Profile p)
        {
            // Ranged: Movement + AutoTarget + Kite, disable caster
            o.AutoTarget.AutoAttackEnabled = p.AutoAttackEnabled;
            o.AutoTarget.AutoRetargetEnabled = p.AutoRetargetEnabled;
            o.Kite.KiteEnabled = p.KiteEnabled;
            o.Mage.MageEnabled = false;
            o.Support.SupportEnabled = false;
            o.Combo.Enabled = p.ComboEnabled;
            o.MobSweep.MobSweepEnabled = false;
        }

        private static void ApplyCasterPreset(EngineOrchestrator o, Profile p)
        {
            // Caster: Mage + Support, disable melee engines
            o.AutoTarget.AutoAttackEnabled = false;
            o.AutoTarget.AutoRetargetEnabled = false;
            o.Kite.KiteEnabled = false;
            o.Mage.MageEnabled = p.MageEnabled;
            o.Support.SupportEnabled = p.SupportEnabled;
            o.Combo.Enabled = false;
            o.MobSweep.MobSweepEnabled = false;
        }

        private static void ApplyHybridPreset(EngineOrchestrator o, Profile p)
        {
            // Hybrid: Melee + some caster (Monk, Assassin, etc.)
            o.AutoTarget.AutoAttackEnabled = p.AutoAttackEnabled;
            o.AutoTarget.AutoRetargetEnabled = p.AutoRetargetEnabled;
            o.Kite.KiteEnabled = p.KiteEnabled;
            o.Mage.MageEnabled = p.MageEnabled;
            o.Support.SupportEnabled = p.SupportEnabled;
            o.Combo.Enabled = p.ComboEnabled;
            o.MobSweep.MobSweepEnabled = p.MobSweepEnabled;
        }

        private static void ApplySupportPreset(EngineOrchestrator o, Profile p)
        {
            // Support: Heal/buff focused, minimal offense
            o.AutoTarget.AutoAttackEnabled = false;
            o.AutoTarget.AutoRetargetEnabled = false;
            o.Kite.KiteEnabled = false;
            o.Mage.MageEnabled = false;
            o.Support.SupportEnabled = p.SupportEnabled;
            o.Combo.Enabled = false;
            o.MobSweep.MobSweepEnabled = false;
        }
    }

    /// <summary>
    /// Engine preset types for auto-class detection.
    /// </summary>
    public enum EnginePreset
    {
        Melee,      // Swordsman, Knight, Crusader, Blacksmith
        Ranged,     // Archer, Hunter, Bard, Dancer, Gunslinger
        Caster,     // Mage, Wizard, Sage, Professor, Alchemist
        Hybrid,     // Thief, Assassin, Rogue, Stalker, Monk, Taekwon, Ninja
        Support     // Acolyte, Priest, Soul Linker
    }
}