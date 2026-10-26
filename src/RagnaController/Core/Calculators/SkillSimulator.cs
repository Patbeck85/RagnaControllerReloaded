using System;
using System.Collections.Generic;
using System.Linq;

namespace RagnaController.Core.Calculators {
    /// <summary>
    /// TASK-3004: SkillSimulator — Skill-Punkte verteilen → Build-Code für Forums/Guides.
    ///
    /// Ermöglicht das virtuelle Verteilen von Skill-Punkten und generiert
    /// Build-Code (Text) für Foren und Guides.
    /// White-Hat: Reine Planung, keine automatische In-Game-Verteilung.
    /// </summary>
    public sealed class SkillSimulator
    {
        private readonly List<SimSkill> _skills = new();
        private int _availablePoints;

        public SkillSimulator(int baseLevel, int jobLevel)
        {
            BaseLevel = baseLevel;
            JobLevel = jobLevel;
            _availablePoints = CalculateTotalPoints(baseLevel);
        }

        public int BaseLevel { get; }
        public int JobLevel { get; }
        public int AvailablePoints => _availablePoints;
        public int TotalPointsSpent => _skills.Sum(s => s.Level);
        public int PointsRemaining => _availablePoints - TotalPointsSpent;

        public IReadOnlyList<SimSkill> Skills => _skills;

        public void AddSkill(int id, string name, int maxLevel, int minBaseLevel, int minJobLevel)
        {
            _skills.Add(new SimSkill { Id = id, Name = name, MaxLevel = maxLevel, MinBaseLevel = minBaseLevel, MinJobLevel = minJobLevel });
        }

        public bool CanUpgrade(SimSkill skill) =>
            skill.Level < skill.MaxLevel &&
            PointsRemaining > 0 &&
            BaseLevel >= skill.MinBaseLevel &&
            JobLevel >= skill.MinJobLevel;

        public bool UpgradeSkill(SimSkill skill)
        {
            if (!CanUpgrade(skill)) return false;
            skill.Level++;
            return true;
        }

        public bool DowngradeSkill(SimSkill skill)
        {
            if (skill.Level <= 0) return false;
            skill.Level--;
            return true;
        }

        public string GenerateBuildCode()
        {
            var lines = new List<string>();
            lines.Add($"// RagnaController Skill Build");
            lines.Add($"// Base Level: {BaseLevel}, Job Level: {JobLevel}");
            lines.Add($"// Total Points: {_availablePoints}, Spent: {TotalPointsSpent}, Remaining: {PointsRemaining}");
            lines.Add("");

            foreach (var skill in _skills.Where(s => s.Level > 0))
            {
                lines.Add($"{skill.Name}: {skill.Level}/{skill.MaxLevel}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        public void Reset()
        {
            foreach (var skill in _skills) skill.Level = 0;
        }

        /// <summary>Lädt vordefinierte RO-Skills für eine Base-Klasse.</summary>
        public static void LoadClassSkills(SkillSimulator sim, string className)
        {
            var skills = className.ToLowerInvariant() switch
            {
                "swordman" => new (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[]
                {
                    (1, "Sword Mastery", 10, 1, 1),
                    (2, "Spear Mastery", 10, 1, 1),
                    (3, "Two-Handed Sword Mastery", 10, 1, 1),
                    (4, "Two-Handed Spear Mastery", 10, 1, 1),
                    (5, "Pole Arm Mastery", 10, 1, 1),
                    (6, "Bash", 10, 1, 1),
                    (7, "Magnum Break", 10, 1, 1),
                    (8, "Provoke", 10, 1, 1),
                    (9, "Weapon Research", 10, 1, 1),
                    (10, "First Aid", 5, 1, 1),
                },
                "mage" => new (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[]
                {
                    (101, "Fire Bolt", 10, 1, 1),
                    (102, "Fire Ball", 10, 1, 1),
                    (103, "Fire Wall", 10, 1, 1),
                    (104, "Meteor Storm", 10, 1, 1),
                    (105, "Lightning Bolt", 10, 1, 1),
                    (106, "Thunder Storm", 10, 1, 1),
                    (107, "Frost Diver", 10, 1, 1),
                    (108, "Ice Wall", 10, 1, 1),
                    (109, "Energy Coat", 1, 1, 1),
                    (110, "Increase SP Recovery", 1, 1, 1),
                    (111, "Heal", 10, 1, 1),
                    (112, "Blessing", 10, 1, 1),
                    (113, "Increase AGI", 10, 1, 1),
                },
                "archer" => new (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[]
                {
                    (201, "Double Strafe", 10, 1, 1),
                    (202, "Arrow Storm", 10, 1, 1),
                    (203, "Blaze", 10, 1, 1),
                    (204, "Concentration", 1, 1, 1),
                    (205, "Critical Explosion", 1, 1, 1),
                },
                "acolyte" => new (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[]
                {
                    (301, "Heal", 10, 1, 1),
                    (302, "Blessing", 10, 1, 1),
                    (303, "Increase AGI", 10, 1, 1),
                    (304, "Resurrection", 10, 1, 1),
                    (305, "Glory", 1, 1, 1),
                },
                "merchant" => new (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[]
                {
                    (401, "Discount", 10, 1, 1),
                    (402, "Overcharge", 10, 1, 1),
                    (403, "Mammonite", 10, 1, 1),
                    (404, "Coin", 10, 1, 1),
                },
                "thief" => new (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[]
                {
                    (501, "Steal", 10, 1, 1),
                    (502, "Hide", 1, 1, 1),
                    (503, "Double Attack", 10, 1, 1),
                    (504, "Critical Strike", 1, 1, 1),
                },
                "novice" or _ => new (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[]
                {
                    (901, "Punch", 1, 1, 1),
                    (902, "Stones", 1, 1, 1),
                },
            };

            foreach (var (id, name, maxLevel, minBase, minJob) in skills)
            {
                sim.AddSkill(id, name, maxLevel, minBase, minJob);
            }
        }

        private static int CalculateTotalPoints(int baseLevel)
        {
            // RO: 1 Stat-Punkt pro Level ab Level 1 (Base Level)
            return Math.Max(0, baseLevel - 1);
        }
    }

    public class SimSkill
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int MaxLevel { get; set; } = 10;
        public int MinBaseLevel { get; set; } = 1;
        public int MinJobLevel { get; set; } = 1;
        public int Level { get; set; } = 0;
    }
}
