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
                private static readonly Dictionary<VirtualKey, List<(string Class, int Weight, string Category)>> SkillToClassMap = new()
                {
                    // Swordsman / Knight / Crusader
                    [VirtualKey.F1] = new() { ("Swordsman", 2, "offensive"), ("Knight", 2, "offensive"), ("Crusader", 1, "offensive") },        // Bash
                    [VirtualKey.F2] = new() { ("Swordsman", 2, "offensive"), ("Knight", 2, "offensive"), ("Crusader", 1, "offensive") },        // Magnum Break
                    [VirtualKey.F3] = new() { ("Knight", 3, "signature"), ("Crusader", 2, "offensive") },                     // Bowling Bash (Knight signature)
                    [VirtualKey.F4] = new() { ("Knight", 3, "signature") },                                 // Brandish Spear (Knight signature)
                    [VirtualKey.F5] = new() { ("Crusader", 3, "signature") },                               // Holy Cross (Crusader signature)
                    [VirtualKey.F6] = new() { ("Crusader", 3, "signature") },                               // Grand Cross (Crusader signature)

                    // Lord Knight / Paladin (Transcendent)
                    [VirtualKey.F7] = new() { ("Lord Knight", 3, "signature"), ("Paladin", 2, "offensive") }, // Spiral Pierce / Shield Boomerang
                    [VirtualKey.F8] = new() { ("Paladin", 3, "signature") },                                // Martyr's Reckoning / Shield Chain

                    // Mage / Wizard / Sage / Professor
                    [VirtualKey.F7] = new() { ("Mage", 2, "offensive"), ("Wizard", 2, "offensive"), ("Sage", 1, "offensive"), ("Professor", 1, "offensive") },    // Fire Bolt
                    [VirtualKey.F8] = new() { ("Mage", 2, "offensive"), ("Wizard", 2, "offensive"), ("Sage", 1, "offensive"), ("Professor", 1, "offensive") },    // Cold Bolt
                    [VirtualKey.F9] = new() { ("Mage", 2, "offensive"), ("Wizard", 2, "offensive"), ("Sage", 1, "offensive"), ("Professor", 1, "offensive") },    // Lightning Bolt
                    [VirtualKey.F10] = new() { ("Wizard", 3, "signature"), ("Professor", 2, "offensive") },                   // Fire Wall / Meteor Storm
                    [VirtualKey.F11] = new() { ("Wizard", 3, "signature") },                                // Storm Gust (Wizard signature)
                    [VirtualKey.F12] = new() { ("Sage", 3, "signature"), ("Professor", 2, "support") },                     // Magic Rod / Abracadabra

                    // High Wizard / Professor (Transcendent)
                    [VirtualKey.F1] = new() { ("High Wizard", 3, "signature") },                             // Meteor Storm
                    [VirtualKey.F2] = new() { ("Professor", 3, "signature") },                               // Double Bolt

                    // Archer / Hunter / Bard / Dancer
                    [VirtualKey.D1] = new() { ("Archer", 2, "offensive"), ("Hunter", 2, "offensive"), ("Bard", 1, "offensive"), ("Dancer", 1, "offensive") },     // Double Strafe
                    [VirtualKey.D2] = new() { ("Hunter", 3, "signature"), ("Bard", 2, "offensive") },                         // Arrow Shower (Hunter signature)
                    [VirtualKey.D3] = new() { ("Bard", 3, "signature"), ("Dancer", 2, "offensive") },                         // Arrow Vulcan (Bard signature)
                    [VirtualKey.D4] = new() { ("Hunter", 3, "signature") },                                 // Blitz Beat (Hunter signature)

                    // Sniper / Clown / Gypsy (Transcendent)
                    [VirtualKey.D5] = new() { ("Sniper", 3, "signature"), ("Clown", 2, "offensive") },      // Sharp Shooting / Arrow Vulcan
                    [VirtualKey.D6] = new() { ("Sniper", 2, "offensive"), ("Gypsy", 2, "offensive") },      // Focused Arrow Strike

                    // Thief / Assassin / Rogue / Stalker
                    [VirtualKey.D5] = new() { ("Thief", 2, "offensive"), ("Assassin", 2, "offensive"), ("Rogue", 2, "offensive"), ("Stalker", 1, "offensive") },  // Double Attack
                    [VirtualKey.D6] = new() { ("Assassin", 3, "signature"), ("Stalker", 2, "offensive") },                    // Sonic Blow (Assassin signature)
                    [VirtualKey.D7] = new() { ("Rogue", 3, "signature"), ("Stalker", 2, "offensive") },                       // Back Stab (Rogue signature)
                    [VirtualKey.D8] = new() { ("Stalker", 3, "signature") },                                // Chase Walk (Stalker signature)

                    // Assassin Cross / Stalker (Transcendent)
                    [VirtualKey.D9] = new() { ("Assassin Cross", 3, "signature") },                          // Meteor Assault
                    [VirtualKey.D0] = new() { ("Stalker", 3, "signature") },                                 // Shadow Spell

                    // Merchant / Blacksmith / Alchemist
                    [VirtualKey.D9] = new() { ("Merchant", 2, "offensive"), ("Blacksmith", 2, "offensive"), ("Alchemist", 1, "offensive") },    // Mammonite
                    [VirtualKey.D0] = new() { ("Blacksmith", 3, "signature") },                             // Cart Revolution (Blacksmith signature)
                    [VirtualKey.Q] = new() { ("Alchemist", 3, "signature") },                               // Acid Terror (Alchemist signature)
                    [VirtualKey.W] = new() { ("Alchemist", 2, "support") },                                 // Homunculus skills

                    // Whitesmith / Creator (Transcendent)
                    [VirtualKey.E] = new() { ("Whitesmith", 3, "signature") },                              // Cart Boost
                    [VirtualKey.R] = new() { ("Creator", 3, "signature") },                                 // Homunculus Call

                    // Acolyte / Priest / Monk
                    [VirtualKey.W] = new() { ("Acolyte", 2, "support"), ("Priest", 2, "support"), ("Monk", 1, "support") },               // Heal
                    [VirtualKey.E] = new() { ("Priest", 3, "signature"), ("Monk", 2, "support") },                          // Blessing (Priest signature)
                    [VirtualKey.R] = new() { ("Monk", 3, "signature") },                                    // Asura Strike (Monk signature)
                    [VirtualKey.T] = new() { ("Monk", 2, "offensive") },                                    // Snap
                    [VirtualKey.Y] = new() { ("Priest", 3, "signature") },                                  // Resurrection / Sanctuary

                    // High Priest / Champion (Transcendent)
                    [VirtualKey.U] = new() { ("High Priest", 3, "signature") },                             // Magnus Exorcismus
                    [VirtualKey.I] = new() { ("Champion", 3, "signature") },                                // Asura Strike (Champion)

                    // Taekwon / Soul Linker / Star Gladiator
                    [VirtualKey.Y] = new() { ("Taekwon", 3, "signature"), ("Star Gladiator", 2, "offensive") },               // Flying Kick
                    [VirtualKey.U] = new() { ("Star Gladiator", 3, "signature") },                          // Demon of the Sun
                    [VirtualKey.I] = new() { ("Soul Linker", 3, "signature") },                             // Soul Link skills

                    // Gunslinger / Rebellion
                    [VirtualKey.I] = new() { ("Gunslinger", 3, "signature"), ("Rebellion", 2, "offensive") },                 // Desperado
                    [VirtualKey.O] = new() { ("Rebellion", 3, "signature") },                               // Eternal Chain
                    [VirtualKey.P] = new() { ("Gunslinger", 2, "offensive"), ("Rebellion", 2, "offensive") }, // Triple Action

                    // Ninja / Kagerou / Oboro
                    [VirtualKey.P] = new() { ("Ninja", 3, "signature"), ("Kagerou", 2, "offensive"), ("Oboro", 2, "offensive") },               // Throw Shuriken
                    [VirtualKey.A] = new() { ("Kagerou", 3, "signature") },                                 // Kunai Splash
                    [VirtualKey.S] = new() { ("Oboro", 3, "signature") },                                   // Shadow Slash

                    // Super Novice
                    [VirtualKey.H] = new() { ("Super Novice", 3, "signature") },                            // Heal (Super Novice) - unique
                    [VirtualKey.J] = new() { ("Super Novice", 2, "support") },                              // Various mimic skills

                    // Additional common skills mapped to number keys / letters
                    [VirtualKey.D1] = new() { ("Archer", 2, "offensive"), ("Hunter", 2, "offensive"), ("Bard", 1, "offensive"), ("Dancer", 1, "offensive") },     // Double Strafe (duplicate for num keys)
                    [VirtualKey.D2] = new() { ("Hunter", 3, "signature"), ("Bard", 2, "offensive") },
                    [VirtualKey.D3] = new() { ("Bard", 3, "signature"), ("Dancer", 2, "offensive") },
                    [VirtualKey.D4] = new() { ("Hunter", 3, "signature") },
                    [VirtualKey.D5] = new() { ("Thief", 2, "offensive"), ("Assassin", 2, "offensive"), ("Rogue", 2, "offensive"), ("Stalker", 1, "offensive") },
                    [VirtualKey.D6] = new() { ("Assassin", 3, "signature"), ("Stalker", 2, "offensive") },
                    [VirtualKey.D7] = new() { ("Rogue", 3, "signature"), ("Stalker", 2, "offensive") },
                    [VirtualKey.D8] = new() { ("Stalker", 3, "signature") },
                    [VirtualKey.D9] = new() { ("Merchant", 2, "offensive"), ("Blacksmith", 2, "offensive"), ("Alchemist", 1, "offensive") },
                    [VirtualKey.D0] = new() { ("Blacksmith", 3, "signature") },
                };

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

                    // Return class with highest weighted score
                    var detected = classScores.OrderByDescending(kvp => kvp.Value).First().Key;
                    return detected;
                }

        /// <summary>
        /// Determines if a ButtonAction represents a class-specific skill.
        /// </summary>
        private static bool IsSkillAction(ButtonAction action)
        {
            // Movement, basic attack, potion, etc. are not class-specific
            if (action.Type == ActionType.Key ||
                action.Type == ActionType.LeftClick ||
                action.Type == ActionType.RightClick ||
                action.Type == ActionType.Scroll ||
                action.Type == ActionType.Combo ||
                action.Type == ActionType.SwitchWindow ||
                action.Type == ActionType.RoFeature)
                return false;

            return action.Type == ActionType.Key; // Skills are mapped as Key type
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