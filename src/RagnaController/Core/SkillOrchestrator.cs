using System;
using System.Collections.Generic;
using System.Linq;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// FEAT-007: Represents a single step in a skill rotation.
    /// </summary>
    public class RotationStep
    {
        /// <summary>The skill/label to execute (must match ButtonAction.Label in profile)</summary>
        public string SkillLabel { get; set; } = "";

        /// <summary>Minimum delay in ms before this step can execute after previous step</summary>
        public int DelayMs { get; set; } = 100;

        /// <summary>Conditions that must be met for this step to execute</summary>
        public List<RotationCondition> Conditions { get; set; } = new();

        /// <summary>Whether this step is a combo finisher (resets rotation)</summary>
        public bool IsFinisher { get; set; } = false;

        /// <summary>Priority for this step (higher = more important)</summary>
        public int Priority { get; set; } = 0;

        /// <summary>Maximum range to use this skill (0 = no range limit)</summary>
        public float MaxRange { get; set; } = 0f;
    }

    /// <summary>
    /// FEAT-007: Condition for a rotation step.
    /// </summary>
    public class RotationCondition
    {
        public enum ConditionType
        {
            HasTarget,
            TargetInRange,
            NotMoving,
            SPAbove,
            HPAbove,
            FacingTarget,
            EnemyCount,
            MissingBuff,
            HasBuff,
            GroundSpellActive,
            IsMoving,
            PartyMemberHasDebuff
        }

        public ConditionType Type { get; set; }
        public float FloatValue { get; set; } = 0f;
        public int IntValue { get; set; } = 0;
        public string StringValue { get; set; } = "";

        public static RotationCondition HasTarget => new() { Type = ConditionType.HasTarget };
        
        public static RotationCondition TargetInRange(float range) => new() 
        { 
            Type = ConditionType.TargetInRange, 
            FloatValue = range 
        };
        
        public static RotationCondition NotMoving => new() { Type = ConditionType.NotMoving };
        
        public static RotationCondition SPAbove(int sp) => new() 
        { 
            Type = ConditionType.SPAbove, 
            IntValue = sp 
        };
        
        public static RotationCondition HPAbove(int hpPercent) => new() 
        { 
            Type = ConditionType.HPAbove, 
            IntValue = hpPercent 
        };
        
        public static RotationCondition FacingTarget => new() { Type = ConditionType.FacingTarget };
        
        public static RotationCondition EnemyCount(int count, float radius) => new() 
        { 
            Type = ConditionType.EnemyCount, 
            IntValue = count, 
            FloatValue = radius 
        };
        
        public static RotationCondition MissingBuff(string buffName) => new() 
        { 
            Type = ConditionType.MissingBuff, 
            StringValue = buffName 
        };
        
        public static RotationCondition HasBuff(string buffName) => new() 
        { 
            Type = ConditionType.HasBuff, 
            StringValue = buffName 
        };
        
        public static RotationCondition GroundSpellActive(string spellName) => new() 
        { 
            Type = ConditionType.GroundSpellActive, 
            StringValue = spellName 
        };
        
        public static RotationCondition IsMoving => new() { Type = ConditionType.IsMoving };
        
        public static RotationCondition PartyMemberHasDebuff(string debuff) => new() 
        { 
            Type = ConditionType.PartyMemberHasDebuff, 
            StringValue = debuff 
        };
    }

    /// <summary>
    /// FEAT-007: Complete rotation configuration for a class.
    /// </summary>
    public class RotationConfig
    {
        public string ClassName { get; set; } = "";
        public EnginePreset PresetType { get; set; } = EnginePreset.Melee;
        public RotationSettings Settings { get; set; } = new();
        public List<RotationStep> Steps { get; set; } = new();
    }

    /// <summary>
    /// FEAT-007: Settings for rotation behavior.
    /// </summary>
    public class RotationSettings
    {
        public bool LoopRotation { get; set; } = true;
        public int GlobalCooldownMs { get; set; } = 200;
        public bool UsePrioritySelection { get; set; } = false;
        public int MaxStepsPerCycle { get; set; } = 0; // 0 = unlimited
    }

    /// <summary>
    /// FEAT-007: Interface for providing rotation configurations.
    /// </summary>
    public interface IRotationProvider
    {
        /// <summary>Get rotation configuration for a specific class name</summary>
        RotationConfig GetRotation(string className);

        /// <summary>Get rotation configuration for an engine preset</summary>
        RotationConfig GetRotation(EnginePreset preset);

        /// <summary>Get all available class rotations</summary>
        IReadOnlyDictionary<string, RotationConfig> GetAllRotations();
    }

    /// <summary>
    /// FEAT-007: Default implementation providing built-in rotations for all RO classes.
    /// </summary>
    public class DefaultRotationProvider : IRotationProvider
    {
        private readonly Dictionary<string, RotationConfig> _rotations = new(StringComparer.OrdinalIgnoreCase);

        public DefaultRotationProvider()
        {
            InitializeDefaultRotations();
        }

        public RotationConfig GetRotation(string className)
        {
            if (_rotations.TryGetValue(className, out var config))
                return config;

            // Fallback to preset-based
            var preset = ClassDetector.GetPresetForClass(className);
            return GetRotation(preset);
        }

        public RotationConfig GetRotation(EnginePreset preset)
        {
            string key = preset.ToString();
            if (_rotations.TryGetValue(key, out var config))
                return config;

            return new RotationConfig { ClassName = key, PresetType = preset };
        }

        public IReadOnlyDictionary<string, RotationConfig> GetAllRotations()
        {
            return _rotations;
        }

        private void InitializeDefaultRotations()
        {
            // ===== MELEE CLASSES =====
            
            // Swordsman / Knight / Crusader - Basic melee rotation
            _rotations["Melee"] = new RotationConfig
            {
                ClassName = "Melee",
                PresetType = EnginePreset.Melee,
                Settings = new RotationSettings { LoopRotation = true, GlobalCooldownMs = 200 },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "Bash", DelayMs = 200, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2) } },
                    new() { SkillLabel = "Magnum Break", DelayMs = 500, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2), RotationCondition.SPAbove(20) }, Priority = 10 },
                    new() { SkillLabel = "Bowling Bash", DelayMs = 800, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2), RotationCondition.SPAbove(30) }, Priority = 15 },
                    new() { SkillLabel = "Brandish Spear", DelayMs = 1000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(3), RotationCondition.SPAbove(25) }, Priority = 12 },
                    new() { SkillLabel = "Holy Cross", DelayMs = 1500, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2), RotationCondition.SPAbove(40) }, Priority = 18 },
                }
            };

            // ===== RANGED CLASSES =====
            
            // Archer / Hunter / Bard / Dancer
            _rotations["Ranged"] = new RotationConfig
            {
                ClassName = "Ranged",
                PresetType = EnginePreset.Ranged,
                Settings = new RotationSettings { LoopRotation = true, GlobalCooldownMs = 150 },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "Double Strafe", DelayMs = 300, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9) } },
                    new() { SkillLabel = "Arrow Shower", DelayMs = 1000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.EnemyCount(3, 5f), RotationCondition.SPAbove(20) }, Priority = 15 },
                    new() { SkillLabel = "Arrow Vulcan", DelayMs = 2000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.SPAbove(30) }, Priority = 18 },
                    new() { SkillLabel = "Blitz Beat", DelayMs = 1500, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.SPAbove(40) }, Priority = 12 },
                }
            };

            // ===== CASTER CLASSES =====
            
            // Mage / Wizard / Sage / Professor / Alchemist
            _rotations["Caster"] = new RotationConfig
            {
                ClassName = "Caster",
                PresetType = EnginePreset.Caster,
                Settings = new RotationSettings { LoopRotation = true, GlobalCooldownMs = 300, UsePrioritySelection = true },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "Fire Bolt", DelayMs = 500, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.NotMoving }, Priority = 5 },
                    new() { SkillLabel = "Cold Bolt", DelayMs = 500, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.NotMoving }, Priority = 5 },
                    new() { SkillLabel = "Lightning Bolt", DelayMs = 500, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.NotMoving }, Priority = 5 },
                    new() { SkillLabel = "Fire Wall", DelayMs = 2000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.NotMoving, RotationCondition.SPAbove(30), RotationCondition.GroundSpellActive("Fire Wall") }, Priority = 15, IsFinisher = false },
                    new() { SkillLabel = "Storm Gust", DelayMs = 3000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.NotMoving, RotationCondition.SPAbove(50), RotationCondition.EnemyCount(2, 7f) }, Priority = 25, IsFinisher = true },
                    new() { SkillLabel = "Magic Rod", DelayMs = 1000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.SPAbove(20) }, Priority = 10 },
                }
            };

            // ===== HYBRID CLASSES =====
            
            // Thief / Assassin / Rogue / Stalker / Monk / Taekwon / Ninja
            _rotations["Hybrid"] = new RotationConfig
            {
                ClassName = "Hybrid",
                PresetType = EnginePreset.Hybrid,
                Settings = new RotationSettings { LoopRotation = true, GlobalCooldownMs = 150, UsePrioritySelection = true },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "Double Attack", DelayMs = 200, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2) }, Priority = 5 },
                    new() { SkillLabel = "Sonic Blow", DelayMs = 800, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2), RotationCondition.SPAbove(30) }, Priority = 15 },
                    new() { SkillLabel = "Back Stab", DelayMs = 1000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2), RotationCondition.FacingTarget, RotationCondition.SPAbove(20) }, Priority = 18 },
                    new() { SkillLabel = "Asura Strike", DelayMs = 2000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2), RotationCondition.SPAbove(60), RotationCondition.HPAbove(30) }, Priority = 25, IsFinisher = true },
                    new() { SkillLabel = "Throw Shuriken", DelayMs = 300, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(7) }, Priority = 8 },
                    new() { SkillLabel = "Kunai Splash", DelayMs = 1500, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(5), RotationCondition.EnemyCount(2, 5f), RotationCondition.SPAbove(25) }, Priority = 20 },
                }
            };

            // ===== SUPPORT CLASSES =====
            
            // Acolyte / Priest / Soul Linker
            _rotations["Support"] = new RotationConfig
            {
                ClassName = "Support",
                PresetType = EnginePreset.Support,
                Settings = new RotationSettings { LoopRotation = true, GlobalCooldownMs = 500, UsePrioritySelection = true },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "Heal", DelayMs = 800, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.SPAbove(15) }, Priority = 20 },
                    new() { SkillLabel = "Blessing", DelayMs = 5000, Conditions = new() { RotationCondition.MissingBuff("Blessing"), RotationCondition.SPAbove(20) }, Priority = 15 },
                    new() { SkillLabel = "Increase Agi", DelayMs = 5000, Conditions = new() { RotationCondition.MissingBuff("Increase Agi"), RotationCondition.SPAbove(20) }, Priority = 15 },
                    new() { SkillLabel = "Kyrie Eleison", DelayMs = 10000, Conditions = new() { RotationCondition.MissingBuff("Kyrie Eleison"), RotationCondition.SPAbove(30) }, Priority = 18 },
                    new() { SkillLabel = "Magnificat", DelayMs = 15000, Conditions = new() { RotationCondition.MissingBuff("Magnificat"), RotationCondition.SPAbove(25) }, Priority = 12 },
                    new() { SkillLabel = "Sanctuary", DelayMs = 30000, Conditions = new() { RotationCondition.GroundSpellActive("Sanctuary"), RotationCondition.SPAbove(40), RotationCondition.NotMoving }, Priority = 10 },
                }
            };

            // Class-specific overrides
            
            // Knight
            _rotations["Knight"] = new RotationConfig
            {
                ClassName = "Knight",
                PresetType = EnginePreset.Melee,
                Settings = new RotationSettings { LoopRotation = true, GlobalCooldownMs = 200 },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "Bash", DelayMs = 200, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2) } },
                    new() { SkillLabel = "Magnum Break", DelayMs = 500, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2), RotationCondition.SPAbove(20) }, Priority = 10 },
                    new() { SkillLabel = "Bowling Bash", DelayMs = 800, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2), RotationCondition.SPAbove(30) }, Priority = 15, IsFinisher = false },
                    new() { SkillLabel = "Brandish Spear", DelayMs = 1000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(3), RotationCondition.SPAbove(25) }, Priority = 12 },
                    new() { SkillLabel = "Auto Counter", DelayMs = 3000, Conditions = new() { RotationCondition.MissingBuff("Auto Counter"), RotationCondition.SPAbove(30) }, Priority = 8 },
                }
            };

            // Crusader
            _rotations["Crusader"] = new RotationConfig
            {
                ClassName = "Crusader",
                PresetType = EnginePreset.Melee,
                Settings = new RotationSettings { LoopRotation = true, GlobalCooldownMs = 200 },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "Bash", DelayMs = 200, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2) } },
                    new() { SkillLabel = "Magnum Break", DelayMs = 500, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2), RotationCondition.SPAbove(20) }, Priority = 10 },
                    new() { SkillLabel = "Holy Cross", DelayMs = 1500, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2), RotationCondition.SPAbove(40) }, Priority = 18 },
                    new() { SkillLabel = "Grand Cross", DelayMs = 3000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2), RotationCondition.EnemyCount(3, 5f), RotationCondition.SPAbove(50), RotationCondition.HPAbove(30) }, Priority = 25, IsFinisher = true },
                    new() { SkillLabel = "Shield Boomerang", DelayMs = 800, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(5), RotationCondition.SPAbove(15) }, Priority = 8 },
                }
            };

            // Wizard
            _rotations["Wizard"] = new RotationConfig
            {
                ClassName = "Wizard",
                PresetType = EnginePreset.Caster,
                Settings = new RotationSettings { LoopRotation = true, GlobalCooldownMs = 300, UsePrioritySelection = true },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "Fire Bolt", DelayMs = 500, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.NotMoving }, Priority = 5 },
                    new() { SkillLabel = "Fire Wall", DelayMs = 2000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.NotMoving, RotationCondition.SPAbove(30) }, Priority = 15 },
                    new() { SkillLabel = "Storm Gust", DelayMs = 3000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.NotMoving, RotationCondition.SPAbove(50), RotationCondition.EnemyCount(2, 7f) }, Priority = 25, IsFinisher = true },
                    new() { SkillLabel = "Meteor Storm", DelayMs = 3000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.NotMoving, RotationCondition.SPAbove(60), RotationCondition.EnemyCount(3, 7f) }, Priority = 28, IsFinisher = true },
                    new() { SkillLabel = "Lord of Vermilion", DelayMs = 4000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.NotMoving, RotationCondition.SPAbove(60), RotationCondition.EnemyCount(3, 7f) }, Priority = 28, IsFinisher = true },
                }
            };

            // Sage
            _rotations["Sage"] = new RotationConfig
            {
                ClassName = "Sage",
                PresetType = EnginePreset.Caster,
                Settings = new RotationSettings { LoopRotation = true, GlobalCooldownMs = 300, UsePrioritySelection = true },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "Fire Bolt", DelayMs = 500, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.NotMoving }, Priority = 5 },
                    new() { SkillLabel = "Fire Wall", DelayMs = 2000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.NotMoving, RotationCondition.SPAbove(30) }, Priority = 15 },
                    new() { SkillLabel = "Storm Gust", DelayMs = 3000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.NotMoving, RotationCondition.SPAbove(50), RotationCondition.EnemyCount(2, 7f) }, Priority = 25, IsFinisher = true },
                    new() { SkillLabel = "Magic Rod", DelayMs = 1000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.SPAbove(20) }, Priority = 10 },
                    new() { SkillLabel = "Abracadabra", DelayMs = 5000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.SPAbove(30) }, Priority = 12 },
                }
            };

            // Hunter / Ranger
            _rotations["Hunter"] = new RotationConfig
            {
                ClassName = "Hunter",
                PresetType = EnginePreset.Ranged,
                Settings = new RotationSettings { LoopRotation = true, GlobalCooldownMs = 150 },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "Double Strafe", DelayMs = 300, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9) } },
                    new() { SkillLabel = "Arrow Shower", DelayMs = 1000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.EnemyCount(3, 5f), RotationCondition.SPAbove(20) }, Priority = 15 },
                    new() { SkillLabel = "Arrow Vulcan", DelayMs = 2000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.SPAbove(30) }, Priority = 18 },
                    new() { SkillLabel = "Blitz Beat", DelayMs = 1500, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.SPAbove(40) }, Priority = 12 },
                }
            };

            // Bard
            _rotations["Bard"] = new RotationConfig
            {
                ClassName = "Bard",
                PresetType = EnginePreset.Ranged,
                Settings = new RotationSettings { LoopRotation = true, GlobalCooldownMs = 300, UsePrioritySelection = true },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "Double Strafe", DelayMs = 300, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9) } },
                    new() { SkillLabel = "Arrow Vulcan", DelayMs = 2000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.SPAbove(30) }, Priority = 18 },
                    new() { SkillLabel = "Severe Rainstorm", DelayMs = 3000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.EnemyCount(3, 7f), RotationCondition.SPAbove(40), RotationCondition.HasBuff("Song") }, Priority = 22, IsFinisher = true },
                }
            };

            // Assassin
            _rotations["Assassin"] = new RotationConfig
            {
                ClassName = "Assassin",
                PresetType = EnginePreset.Hybrid,
                Settings = new RotationSettings { LoopRotation = true, GlobalCooldownMs = 150, UsePrioritySelection = true },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "Double Attack", DelayMs = 200, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2) }, Priority = 5 },
                    new() { SkillLabel = "Sonic Blow", DelayMs = 800, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2), RotationCondition.SPAbove(30) }, Priority = 15, IsFinisher = false },
                    new() { SkillLabel = "Grimtooth", DelayMs = 1500, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(3), RotationCondition.EnemyCount(2, 5f), RotationCondition.SPAbove(25) }, Priority = 18 },
                    new() { SkillLabel = "Venom Dust", DelayMs = 3000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2), RotationCondition.SPAbove(20) }, Priority = 10 },
                    new() { SkillLabel = "Enchant Poison", DelayMs = 5000, Conditions = new() { RotationCondition.MissingBuff("Enchant Poison"), RotationCondition.SPAbove(20) }, Priority = 8 },
                }
            };

            // Rogue
            _rotations["Rogue"] = new RotationConfig
            {
                ClassName = "Rogue",
                PresetType = EnginePreset.Hybrid,
                Settings = new RotationSettings { LoopRotation = true, GlobalCooldownMs = 150 },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "Double Attack", DelayMs = 200, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2) } },
                    new() { SkillLabel = "Back Stab", DelayMs = 1000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2), RotationCondition.FacingTarget, RotationCondition.SPAbove(20) }, Priority = 18 },
                    new() { SkillLabel = "Plagiarism", DelayMs = 2000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(3), RotationCondition.SPAbove(30) }, Priority = 15 },
                    new() { SkillLabel = "Divest Armor", DelayMs = 3000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(3), RotationCondition.SPAbove(25) }, Priority = 12 },
                }
            };

            // Monk
            _rotations["Monk"] = new RotationConfig
            {
                ClassName = "Monk",
                PresetType = EnginePreset.Hybrid,
                Settings = new RotationSettings { LoopRotation = true, GlobalCooldownMs = 200, UsePrioritySelection = true },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "Combo Attack", DelayMs = 200, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2) }, Priority = 5 },
                    new() { SkillLabel = "Tiger Knuckle Fist", DelayMs = 500, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2), RotationCondition.HasBuff("Combo:5") }, Priority = 15 },
                    new() { SkillLabel = "Asura Strike", DelayMs = 2000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2), RotationCondition.SPAbove(60), RotationCondition.HPAbove(30) }, Priority = 25, IsFinisher = true },
                    new() { SkillLabel = "Snap", DelayMs = 1000, Conditions = new() { RotationCondition.IsMoving, RotationCondition.SPAbove(15) }, Priority = 10 },
                    new() { SkillLabel = "Spirit Sphere", DelayMs = 3000, Conditions = new() { RotationCondition.MissingBuff("Spirit Sphere:5"), RotationCondition.SPAbove(20) }, Priority = 12 },
                    new() { SkillLabel = "Fury", DelayMs = 5000, Conditions = new() { RotationCondition.MissingBuff("Fury"), RotationCondition.SPAbove(30) }, Priority = 15 },
                }
            };

            // Priest
            _rotations["Priest"] = new RotationConfig
            {
                ClassName = "Priest",
                PresetType = EnginePreset.Support,
                Settings = new RotationSettings { LoopRotation = true, GlobalCooldownMs = 500, UsePrioritySelection = true },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "Heal", DelayMs = 800, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.SPAbove(15) }, Priority = 25 },
                    new() { SkillLabel = "Blessing", DelayMs = 5000, Conditions = new() { RotationCondition.MissingBuff("Blessing"), RotationCondition.SPAbove(20) }, Priority = 15 },
                    new() { SkillLabel = "Increase Agi", DelayMs = 5000, Conditions = new() { RotationCondition.MissingBuff("Increase Agi"), RotationCondition.SPAbove(20) }, Priority = 15 },
                    new() { SkillLabel = "Kyrie Eleison", DelayMs = 10000, Conditions = new() { RotationCondition.MissingBuff("Kyrie Eleison"), RotationCondition.SPAbove(30) }, Priority = 18 },
                    new() { SkillLabel = "Magnificat", DelayMs = 15000, Conditions = new() { RotationCondition.MissingBuff("Magnificat"), RotationCondition.SPAbove(25) }, Priority = 12 },
                    new() { SkillLabel = "Sanctuary", DelayMs = 30000, Conditions = new() { RotationCondition.GroundSpellActive("Sanctuary"), RotationCondition.SPAbove(40), RotationCondition.NotMoving }, Priority = 10 },
                    new() { SkillLabel = "Resurrection", DelayMs = 5000, Conditions = new() { RotationCondition.PartyMemberHasDebuff("Dead"), RotationCondition.SPAbove(50) }, Priority = 30 },
                }
            };

            // Alchemist
            _rotations["Alchemist"] = new RotationConfig
            {
                ClassName = "Alchemist",
                PresetType = EnginePreset.Caster,
                Settings = new RotationSettings { LoopRotation = true, GlobalCooldownMs = 300 },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "Acid Terror", DelayMs = 800, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.NotMoving, RotationCondition.SPAbove(20) }, Priority = 15 },
                    new() { SkillLabel = "Acid Demonstration", DelayMs = 2000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9), RotationCondition.NotMoving, RotationCondition.EnemyCount(3, 7f), RotationCondition.SPAbove(40) }, Priority = 20, IsFinisher = true },
                    new() { SkillLabel = "Homunculus AI", DelayMs = 1000, Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(9) }, Priority = 10 },
                }
            };
        }
    }

    /// <summary>
    /// FEAT-007: Skill Orchestrator - executes class-specific skill rotations.
    /// Integrates with CombatEngine, AutoTargetEngine, and GroundSpellEngine.
    /// </summary>
    public class SkillOrchestrator
    {
        private readonly InputCommandQueue _queue;
        private readonly IRotationProvider _rotationProvider;
        private RotationConfig? _currentRotation;
        private int _currentStepIndex = 0;
        private int _globalCooldownMs = 0;
        private int _lastStepTimeMs = 0;
        private bool _enabled = false;
        private string _activeClass = "";
        
        // Dependencies for condition evaluation
        private AutoTargetEngine? _autoTarget;
        private MageEngine? _mage;
        private SupportEngine? _support;
        private ComboEngine? _combo;
        private CooldownManager? _cooldownManager;
        private GroundSpellEngine? _groundSpell;
        
        // Events
        public event Action<string>? RotationStepExecuted;
        public event Action? RotationCompleted;
        public event Action<string>? RotationChanged;

        public SkillOrchestrator(InputCommandQueue queue, IRotationProvider? rotationProvider = null)
        {
            _queue = queue;
            _rotationProvider = rotationProvider ?? new DefaultRotationProvider();
        }
        
        /// <summary>Set dependencies for condition evaluation</summary>
        public void SetDependencies(
            AutoTargetEngine? autoTarget = null,
            MageEngine? mage = null,
            SupportEngine? support = null,
            ComboEngine? combo = null,
            CooldownManager? cooldownManager = null,
            GroundSpellEngine? groundSpell = null)
        {
            _autoTarget = autoTarget;
            _mage = mage;
            _support = support;
            _combo = combo;
            _cooldownManager = cooldownManager;
            _groundSpell = groundSpell;
        }
        
        /// <summary>Load a rotation config directly</summary>
        public void LoadRotation(RotationConfig config)
        {
            _currentRotation = config;
            _activeClass = config.ClassName;
            _currentStepIndex = 0;
            _globalCooldownMs = 0;
            _lastStepTimeMs = 0;
            RotationChanged?.Invoke(_activeClass);
        }
        
        /// <summary>Enable/disable the skill orchestrator</summary>
        public void SetEnabled(bool enabled)
        {
            if (_enabled != enabled)
            {
                _enabled = enabled;
                if (!_enabled) Reset();
            }
        }

        /// <summary>Currently active class name</summary>
        public string ActiveClass => _activeClass;

        /// <summary>Currently active rotation config</summary>
        public RotationConfig? CurrentRotation => _currentRotation;

        /// <summary>Current step index in rotation</summary>
        public int CurrentStepIndex => _currentStepIndex;

        /// <summary>Set the active class and load its rotation</summary>
        public void SetClass(string className)
        {
            if (string.IsNullOrWhiteSpace(className)) return;
            
            if (_activeClass != className)
            {
                _activeClass = className;
                _currentRotation = _rotationProvider.GetRotation(className);
                _currentStepIndex = 0;
                _globalCooldownMs = 0;
                _lastStepTimeMs = 0;
                RotationChanged?.Invoke(className);
            }
        }

        /// <summary>Set rotation by engine preset</summary>
        public void SetPreset(EnginePreset preset)
        {
            var config = _rotationProvider.GetRotation(preset);
            if (config != null)
            {
                _currentRotation = config;
                _activeClass = preset.ToString();
                _currentStepIndex = 0;
                _globalCooldownMs = 0;
                _lastStepTimeMs = 0;
                RotationChanged?.Invoke(_activeClass);
            }
        }

        /// <summary>Update the orchestrator - call each tick from EngineOrchestrator</summary>
        public void Update(ParsedInput input, int deltaMs, bool hasTarget, float targetDistance, int currentSP, int currentHPPercent, bool isMoving, bool facingTarget, List<string> activeBuffs, List<string> activeDebuffs, int enemyCount, List<string> groundSpellsActive)
        {
            if (!_enabled || _currentRotation == null) return;

            _globalCooldownMs = Math.Max(0, _globalCooldownMs - deltaMs);

            // Check if we should advance to next step
            if (_globalCooldownMs <= 0)
            {
                ExecuteNextStep(input, hasTarget, targetDistance, currentSP, currentHPPercent, isMoving, facingTarget, activeBuffs, activeDebuffs, enemyCount, groundSpellsActive);
            }
        }

        private void ExecuteNextStep(ParsedInput input, bool hasTarget, float targetDistance, int currentSP, int currentHPPercent, bool isMoving, bool facingTarget, List<string> activeBuffs, List<string> activeDebuffs, int enemyCount, List<string> groundSpellsActive)
        {
            if (_currentRotation == null || _currentRotation.Steps.Count == 0) return;

            var settings = _currentRotation.Settings;
            var steps = _currentRotation.Steps;

            // Find the next valid step
            var startIndex = _currentStepIndex;
            bool stepExecuted = false;

            for (int i = 0; i < steps.Count; i++)
            {
                int index = (_currentStepIndex + i) % steps.Count;
                var step = steps[index];

                if (CanExecuteStep(step, hasTarget, targetDistance, currentSP, currentHPPercent, isMoving, facingTarget, activeBuffs, activeDebuffs, enemyCount, groundSpellsActive))
                {
                    ExecuteStep(step, input);
                    _currentStepIndex = (index + 1) % steps.Count;
                    _globalCooldownMs = settings.GlobalCooldownMs;
                    _lastStepTimeMs = Environment.TickCount;
                    stepExecuted = true;

                    if (step.IsFinisher)
                    {
                        // Finisher resets rotation to start
                        _currentStepIndex = 0;
                        RotationCompleted?.Invoke();
                    }
                    break;
                }

                // If we've checked all steps and none can execute, check if we should loop
                if (i == steps.Count - 1 && !stepExecuted)
                {
                    if (!settings.LoopRotation)
                    {
                        // Rotation complete, wait for next cycle
                        _currentStepIndex = 0;
                    }
                }
            }
        }

        private bool CanExecuteStep(RotationStep step, bool hasTarget, float targetDistance, int currentSP, int currentHPPercent, bool isMoving, bool facingTarget, List<string> activeBuffs, List<string> activeDebuffs, int enemyCount, List<string> groundSpellsActive)
        {
            foreach (var condition in step.Conditions)
            {
                if (!CheckCondition(condition, hasTarget, targetDistance, currentSP, currentHPPercent, isMoving, facingTarget, activeBuffs, activeDebuffs, enemyCount, groundSpellsActive))
                    return false;
            }
            return true;
        }

        private bool CheckCondition(RotationCondition condition, bool hasTarget, float targetDistance, int currentSP, int currentHPPercent, bool isMoving, bool facingTarget, List<string> activeBuffs, List<string> activeDebuffs, int enemyCount, List<string> groundSpellsActive)
        {
            return condition.Type switch
            {
                RotationCondition.ConditionType.HasTarget => hasTarget,
                RotationCondition.ConditionType.TargetInRange => hasTarget && targetDistance <= condition.FloatValue,
                RotationCondition.ConditionType.NotMoving => !isMoving,
                RotationCondition.ConditionType.SPAbove => currentSP >= condition.IntValue,
                RotationCondition.ConditionType.HPAbove => currentHPPercent >= condition.IntValue,
                RotationCondition.ConditionType.FacingTarget => facingTarget,
                RotationCondition.ConditionType.EnemyCount => enemyCount >= condition.IntValue,
                RotationCondition.ConditionType.MissingBuff => !activeBuffs.Contains(condition.StringValue, StringComparer.OrdinalIgnoreCase),
                RotationCondition.ConditionType.HasBuff => activeBuffs.Contains(condition.StringValue, StringComparer.OrdinalIgnoreCase),
                RotationCondition.ConditionType.GroundSpellActive => groundSpellsActive.Contains(condition.StringValue, StringComparer.OrdinalIgnoreCase),
                RotationCondition.ConditionType.IsMoving => isMoving,
                RotationCondition.ConditionType.PartyMemberHasDebuff => activeDebuffs.Contains(condition.StringValue, StringComparer.OrdinalIgnoreCase),
                _ => true
            };
        }

        private void ExecuteStep(RotationStep step, ParsedInput input)
        {
            // Tap the skill key
            if (!string.IsNullOrEmpty(step.SkillLabel))
            {
                // Find the key for this skill label in current profile
                var key = FindKeyForSkillLabel(step.SkillLabel);
                if (key.HasValue)
                {
                    _queue.TapKey(key.Value);
                    RotationStepExecuted?.Invoke(step.SkillLabel);
                }
            }
        }

        private VirtualKey? FindKeyForSkillLabel(string skillLabel)
        {
            // This would need to be connected to the current profile's ButtonActions
            // For now, return null - the ProfileApplier should connect this
            return null;
        }

        /// <summary>Register a skill key mapping for a skill label</summary>
        public void RegisterSkillKey(string skillLabel, VirtualKey key)
        {
            // This can be called from ProfileApplier when loading a profile
        }

        /// <summary>Reset the orchestrator to initial state</summary>
        public void Reset()
        {
            _currentStepIndex = 0;
            _globalCooldownMs = 0;
            _lastStepTimeMs = 0;
        }

        /// <summary>Force execute a specific skill by label</summary>
        public void ForceExecuteSkill(string skillLabel)
        {
            var key = FindKeyForSkillLabel(skillLabel);
            if (key.HasValue)
            {
                _queue.TapKey(key.Value);
                RotationStepExecuted?.Invoke(skillLabel);
                _globalCooldownMs = _currentRotation?.Settings.GlobalCooldownMs ?? 200;
            }
        }
    }
}