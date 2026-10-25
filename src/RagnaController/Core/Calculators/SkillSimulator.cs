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

        private static int CalculateTotalPoints(int baseLevel)
        {
            // Vereinfacht: 1 Punkt pro Level ab Level 1
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
