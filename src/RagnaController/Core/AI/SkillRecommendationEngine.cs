using System;
using System.Collections.Generic;
using System.Linq;

namespace RagnaController.Core.AI {
    /// <summary>
    /// TASK-2602: SkillRecommendationEngine — Vorschlägt Skill-Builds basierend auf
    /// Klasse, Level, Ausrüstung und Spielstil (Pre-Renewal & Renewal).
    ///
    /// Die Engine nutzt eine regelbasierte Empfehlung, die durch Templates
    /// konfigurierbar ist. Keine KI im Sinne von ML, sondern deterministische
    /// "White-Hat"-Empfehlungen (kein Cheating, kein automatisches Spielen).
    /// </summary>
    public sealed class SkillRecommendationEngine
    {
        private readonly List<SkillBuildTemplate> _templates = new();

        public SkillRecommendationEngine()
        {
            InitializeDefaultTemplates();
        }

        /// <summary>Gibt alle registrierten Skill-Build-Templates zurück.</summary>
        public IReadOnlyList<SkillBuildTemplate> Templates => _templates;

        /// <summary>Fügt ein benutzerdefiniertes Template hinzu.</summary>
        public void AddTemplate(SkillBuildTemplate template)
        {
            _templates.Add(template);
        }

        /// <summary>
        /// Empfielt Skill-Builds für die gegebene Klasse und das Level.
        /// Gibt eine sortierte Liste (beste Übereinstimmung zuerst) zurück.
        /// </summary>
        /// <param name="baseClass">Base-Klasse (z.B. "Swordman", "Magician", "Archer", "Acolyte", "Merchant", "Thief").</param>
        /// <param name="jobClass">Job-Klasse (z.B. "Knight", "Wizard", "Hunter", "Priest", "Blacksmith", "Assassin", oder null für Base-Klasse).</param>
        /// <param name="baseLevel">Aktuelles Base-Level.</param>
        /// <param name="jobLevel">Aktuelles Job-Level.</param>
        /// <param name="playstyle">Bevorzugter Spielstil (z.B. "PvM", "PvP", "Support", "Hybrid").</param>
        /// <param name="maxResults">Maximale Anzahl an Empfehlungen.</param>
        public List<SkillRecommendation> Recommend(
            string baseClass,
            string? jobClass = null,
            int baseLevel = 1,
            int jobLevel = 1,
            string? playstyle = null,
            int maxResults = 5)
        {
            var matches = _templates
                .Where(t => t.BaseClass.Equals(baseClass, StringComparison.OrdinalIgnoreCase))
                .Where(t => jobClass == null || t.JobClass == null || t.JobClass.Equals(jobClass, StringComparison.OrdinalIgnoreCase))
                .Where(t => baseLevel >= t.MinBaseLevel && jobLevel >= t.MinJobLevel)
                .Where(t => playstyle == null || t.Playstyle.Equals(playstyle, StringComparison.OrdinalIgnoreCase) || t.Playstyle == "Hybrid")
                .Select(t => new SkillRecommendation
                {
                    Template = t,
                    MatchScore = CalculateMatchScore(t, baseLevel, jobLevel, playstyle),
                    RecommendedSkills = t.RecommendedSkills
                        .Where(s => s.MinBaseLevel <= baseLevel && s.MinJobLevel <= jobLevel)
                        .OrderBy(s => s.Priority)
                        .ToList(),
                })
                .Where(r => r.RecommendedSkills.Count > 0)
                .OrderByDescending(r => r.MatchScore)
                .Take(maxResults)
                .ToList();

            return matches;
        }

        private static float CalculateMatchScore(SkillBuildTemplate template, int baseLevel, int jobLevel, string? playstyle)
        {
            float score = 0f;

            // Basis-Übereinstimmung: Level-Anforderungen erfüllt
            score += 10f;

            // Je näher am empfohlenen Level, desto höher der Score
            float levelDiff = Math.Abs(baseLevel - template.RecommendedBaseLevel);
            score += Math.Max(0f, 10f - levelDiff * 0.5f);

            float jobLevelDiff = Math.Abs(jobLevel - template.RecommendedJobLevel);
            score += Math.Max(0f, 10f - jobLevelDiff * 0.5f);

            // Playstyle-Übereinstimmung
            if (playstyle != null && template.Playstyle.Equals(playstyle, StringComparison.OrdinalIgnoreCase))
                score += 20f;
            else if (template.Playstyle == "Hybrid")
                score += 5f;

            return score;
        }

        private void InitializeDefaultTemplates()
        {
            // ── Swordman → Knight (Pre-Renewal) ─────────────────────────────
            AddTemplate(new SkillBuildTemplate
            {
                Id = "knight_pvm",
                BaseClass = "Swordman",
                JobClass = "Knight",
                Playstyle = "PvM",
                MinBaseLevel = 1,
                MinJobLevel = 1,
                RecommendedBaseLevel = 99,
                RecommendedJobLevel = 50,
                Description = "Klassischer PvM-Knight mit Zwei-Hand-Schwert",
                RecommendedSkills = new List<SkillRecommendationEntry>
                {
                    new() { Id = 41, Name = "Spear Booster", MinBaseLevel = 1, MinJobLevel = 1, Priority = 1, MaxLevel = 10 },
                    new() { Id = 42, Name = "Spear Quick", MinBaseLevel = 1, MinJobLevel = 1, Priority = 2, MaxLevel = 10 },
                    new() { Id = 43, Name = "Brandish Spear", MinBaseLevel = 1, MinJobLevel = 1, Priority = 3, MaxLevel = 10 },
                    new() { Id = 44, Name = "Spiral Pierce", MinBaseLevel = 10, MinJobLevel = 10, Priority = 4, MaxLevel = 10 },
                    new() { Id = 45, Name = "Pierce", MinBaseLevel = 1, MinJobLevel = 1, Priority = 5, MaxLevel = 10 },
                },
            });

            // ── Swordman → Knight (Pre-Renewal, PvP) ────────────────────────
            AddTemplate(new SkillBuildTemplate
            {
                Id = "knight_pvp",
                BaseClass = "Swordman",
                JobClass = "Knight",
                Playstyle = "PvP",
                MinBaseLevel = 1,
                MinJobLevel = 1,
                RecommendedBaseLevel = 99,
                RecommendedJobLevel = 50,
                Description = "PvP-fokussierter Knight",
                RecommendedSkills = new List<SkillRecommendationEntry>
                {
                    new() { Id = 51, Name = "Bash", MinBaseLevel = 1, MinJobLevel = 1, Priority = 1, MaxLevel = 10 },
                    new() { Id = 52, Name = "Provoke", MinBaseLevel = 1, MinJobLevel = 1, Priority = 2, MaxLevel = 10 },
                    new() { Id = 53, Name = "Spear Booster", MinBaseLevel = 1, MinJobLevel = 1, Priority = 3, MaxLevel = 10 },
                    new() { Id = 54, Name = "Brandish Spear", MinBaseLevel = 1, MinJobLevel = 1, Priority = 4, MaxLevel = 10 },
                },
            });

            // ── Magician → Wizard (Pre-Renewal, PvM) ────────────────────────
            AddTemplate(new SkillBuildTemplate
            {
                Id = "wizard_pvm",
                BaseClass = "Magician",
                JobClass = "Wizard",
                Playstyle = "PvM",
                MinBaseLevel = 1,
                MinJobLevel = 1,
                RecommendedBaseLevel = 99,
                RecommendedJobLevel = 50,
                Description = "Feuer/Blut-Zauberer für PvM",
                RecommendedSkills = new List<SkillRecommendationEntry>
                {
                    new() { Id = 111, Name = "Fire Bolt", MinBaseLevel = 1, MinJobLevel = 1, Priority = 1, MaxLevel = 10 },
                    new() { Id = 112, Name = "Fire Ball", MinBaseLevel = 10, MinJobLevel = 10, Priority = 2, MaxLevel = 10 },
                    new() { Id = 113, Name = "Meteor Storm", MinBaseLevel = 50, MinJobLevel = 40, Priority = 3, MaxLevel = 10 },
                    new() { Id = 114, Name = "Lord of Vermilion", MinBaseLevel = 70, MinJobLevel = 50, Priority = 4, MaxLevel = 10 },
                },
            });

            // ── Archer → Hunter (Pre-Renewal, PvM) ──────────────────────────
            AddTemplate(new SkillBuildTemplate
            {
                Id = "hunter_pvm",
                BaseClass = "Archer",
                JobClass = "Hunter",
                Playstyle = "PvM",
                MinBaseLevel = 1,
                MinJobLevel = 1,
                RecommendedBaseLevel = 99,
                RecommendedJobLevel = 50,
                Description = "Bow-Jäger für einzelnes Ziel-PvM",
                RecommendedSkills = new List<SkillRecommendationEntry>
                {
                    new() { Id = 201, Name = "Double Strafe", MinBaseLevel = 1, MinJobLevel = 1, Priority = 1, MaxLevel = 10 },
                    new() { Id = 202, Name = "Arrow Storm", MinBaseLevel = 50, MinJobLevel = 40, Priority = 2, MaxLevel = 10 },
                    new() { Id = 203, Name = "Blazing Arrow", MinBaseLevel = 1, MinJobLevel = 1, Priority = 3, MaxLevel = 10 },
                },
            });

            // ── Acolyte → Priest (Pre-Renewal, Support) ─────────────────────
            AddTemplate(new SkillBuildTemplate
            {
                Id = "priest_support",
                BaseClass = "Acolyte",
                JobClass = "Priest",
                Playstyle = "Support",
                MinBaseLevel = 1,
                MinJobLevel = 1,
                RecommendedBaseLevel = 99,
                RecommendedJobLevel = 50,
                Description = "Support-Priest für Party und PvM",
                RecommendedSkills = new List<SkillRecommendationEntry>
                {
                    new() { Id = 301, Name = "Heal", MinBaseLevel = 1, MinJobLevel = 1, Priority = 1, MaxLevel = 10 },
                    new() { Id = 302, Name = "Blessing", MinBaseLevel = 1, MinJobLevel = 1, Priority = 2, MaxLevel = 10 },
                    new() { Id = 303, Name = "Increase AGI", MinBaseLevel = 1, MinJobLevel = 1, Priority = 3, MaxLevel = 10 },
                    new() { Id = 304, Name = "Restore", MinBaseLevel = 1, MinJobLevel = 1, Priority = 4, MaxLevel = 10 },
                    new() { Id = 305, Name = "Resurrection", MinBaseLevel = 50, MinJobLevel = 40, Priority = 5, MaxLevel = 10 },
                },
            });

            // ── Base-Klasse ohne Job-Transition ─────────────────────────────
            AddTemplate(new SkillBuildTemplate
            {
                Id = "base_generic",
                BaseClass = "Novice",
                JobClass = null,
                Playstyle = "Hybrid",
                MinBaseLevel = 1,
                MinJobLevel = 1,
                RecommendedBaseLevel = 1,
                RecommendedJobLevel = 1,
                Description = "Generischer Novice-Build",
                RecommendedSkills = new List<SkillRecommendationEntry>
                {
                    new() { Id = 1, Name = "Punch", MinBaseLevel = 1, MinJobLevel = 1, Priority = 1, MaxLevel = 1 },
                },
            });
        }
    }

    /// <summary>
    /// Ein Skill-Build-Template für eine bestimmte Klasse und Spielstil.
    /// </summary>
    public class SkillBuildTemplate
    {
        public string Id { get; set; } = "";
        public string BaseClass { get; set; } = "";
        public string? JobClass { get; set; }
        public string Playstyle { get; set; } = "Hybrid";
        public int MinBaseLevel { get; set; } = 1;
        public int MinJobLevel { get; set; } = 1;
        public int RecommendedBaseLevel { get; set; } = 99;
        public int RecommendedJobLevel { get; set; } = 50;
        public string Description { get; set; } = "";
        public List<SkillRecommendationEntry> RecommendedSkills { get; set; } = new();
    }

    /// <summary>
    /// Einzelne Skill-Empfehlung innerhalb eines Builds.
    /// </summary>
    public class SkillRecommendationEntry
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int MinBaseLevel { get; set; } = 1;
        public int MinJobLevel { get; set; } = 1;
        public int Priority { get; set; } = 1; // 1 = zuerst lernen
        public int MaxLevel { get; set; } = 10;
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Ergebnis einer Skill-Build-Empfehlung.
    /// </summary>
    public class SkillRecommendation
    {
        public SkillBuildTemplate Template { get; set; } = new();
        public float MatchScore { get; set; }
        public List<SkillRecommendationEntry> RecommendedSkills { get; set; } = new();
    }
}
