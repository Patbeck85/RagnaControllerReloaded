using System;
using System.Collections.Generic;
using System.Linq;

namespace RagnaController.Core
{
    /// <summary>
    /// TASK-2603: AutoBuildOptimizer — Empfiehlt Stat-Allokation (Str/Agi/Vit/Int/Dex/Luk)
    /// für Builds (Pre-Renewal & Renewal).
    ///
    /// Der Optimizer nutzt regelbasierte Vorgaben pro Klasse und Spielstil.
    /// Keine automatische Stat-Zuteilung im Spiel (White-Hat), nur Empfehlungen
    /// für den Spieler zur manuellen Allokation.
    /// </summary>
    public sealed class AutoBuildOptimizer
    {
        private readonly List<StatBuildTemplate> _templates = new();

        public AutoBuildOptimizer()
        {
            InitializeDefaultTemplates();
        }

        /// <summary>Gibt alle registrierten Stat-Build-Templates zurück.</summary>
        public IReadOnlyList<StatBuildTemplate> Templates => _templates;

        /// <summary>Fügt ein benutzerdefiniertes Template hinzu.</summary>
        public void AddTemplate(StatBuildTemplate template)
        {
            _templates.Add(template);
        }

        /// <summary>
        /// Empfielt eine Stat-Allokation für die gegebene Klasse und Spielstil.
        /// </summary>
        /// <param name="baseClass">Base-Klasse (z.B. "Swordman", "Magician", "Archer", "Acolyte", "Merchant", "Thief").</param>
        /// <param name="playstyle">Bevorzugter Spielstil (z.B. "PvM", "PvP", "Support", "Hybrid", "Tank").</param>
        /// <param name>targetBaseLevel>Ziel-Base-Level für die Verteilung.</param>
        /// <param name="maxResults">Maximale Anzahl an Empfehlungen.</param>
        public List<StatBuildRecommendation> Recommend(
            string baseClass,
            string? playstyle = null,
            int targetBaseLevel = 99,
            int maxResults = 3)
        {
            var matches = _templates
                .Where(t => t.BaseClass.Equals(baseClass, StringComparison.OrdinalIgnoreCase))
                .Where(t => playstyle == null || t.Playstyle.Equals(playstyle, StringComparison.OrdinalIgnoreCase) || t.Playstyle == "Hybrid")
                .Select(t => new StatBuildRecommendation
                {
                    Template = t,
                    MatchScore = CalculateMatchScore(t, playstyle),
                    SuggestedStats = CalculateStats(t, targetBaseLevel),
                })
                .OrderByDescending(r => r.MatchScore)
                .Take(maxResults)
                .ToList();

            return matches;
        }

        /// <summary>Berechnet die Gesamt-Stat-Punkte basierend auf Level und Template-Gewichten.</summary>
        private StatAllocation CalculateStats(StatBuildTemplate template, int targetBaseLevel)
        {
            // RO hat bei Level 99 ca. 79 Stat-Punkte (ohne Boni)
            int totalPoints = targetBaseLevel - 1; // Vereinfacht: 1 Punkt pro Level über 1

            var weights = new Dictionary<string, float>
            {
                ["Str"] = template.StrWeight,
                ["Agi"] = template.AgiWeight,
                ["Vit"] = template.VitWeight,
                ["Int"] = template.IntWeight,
                ["Dex"] = template.DexWeight,
                ["Luk"] = template.LukWeight,
            };

            float totalWeight = weights.Values.Sum();
            if (totalWeight <= 0)
                totalWeight = 1f;

            var result = new StatAllocation();
            int remaining = totalPoints;

            // Proportionale Verteilung basierend auf Gewichten
            var ordered = weights.OrderByDescending(kv => kv.Value).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                var stat = ordered[i];
                int alloc;
                if (i == ordered.Count - 1)
                {
                    // Letzter Stat bekommt den Rest
                    alloc = remaining;
                }
                else
                {
                    alloc = (int)Math.Round(totalPoints * stat.Value / totalWeight);
                    alloc = Math.Clamp(alloc, 0, remaining);
                }

                switch (stat.Key)
                {
                    case "Str": result.Str = alloc; break;
                    case "Agi": result.Agi = alloc; break;
                    case "Vit": result.Vit = alloc; break;
                    case "Int": result.Int = alloc; break;
                    case "Dex": result.Dex = alloc; break;
                    case "Luk": result.Luk = alloc; break;
                }
                remaining -= alloc;
            }

            result.TotalPoints = totalPoints - remaining;
            return result;
        }

        private static float CalculateMatchScore(StatBuildTemplate template, string? playstyle)
        {
            float score = 10f;

            if (playstyle != null && template.Playstyle.Equals(playstyle, StringComparison.OrdinalIgnoreCase))
                score += 20f;
            else if (template.Playstyle == "Hybrid")
                score += 5f;

            return score;
        }

        private void InitializeDefaultTemplates()
        {
            // ── Swordman → Knight (PvM, Zwei-Hand-Schwert) ──────────────────
            AddTemplate(new StatBuildTemplate
            {
                Id = "knight_pvm",
                BaseClass = "Swordman",
                Playstyle = "PvM",
                Description = "Klassischer PvM-Knight: Hohe Str für Schaden, Vit für Überleben",
                StrWeight = 40f,
                Agi = 0f,
                Vit = 30f,
                Int = 0f,
                Dex = 20f,
                Luk = 10f,
            });

            // ── Swordman → Knight (PvP) ─────────────────────────────────────
            AddTemplate(new StatBuildTemplate
            {
                Id = "knight_pvp",
                BaseClass = "Swordman",
                Playstyle = "PvP",
                Description = "PvP-Knight: Str für Schaden, Agi für Flee, Dex für Treffer",
                StrWeight = 35f,
                Agi = 25f,
                Vit = 20f,
                Int = 0f,
                Dex = 15f,
                Luk = 5f,
            });

            // ── Magician → Wizard (PvM, Feuerzauber) ────────────────────────
            AddTemplate(new StatBuildTemplate
            {
                Id = "wizard_pvm",
                BaseClass = "Magician",
                Playstyle = "PvM",
                Description = "Zauberer: Int für Matk/SP, Vit für Überleben",
                StrWeight = 0f,
                Agi = 0f,
                Vit = 30f,
                Int = 50f,
                Dex = 10f,
                Luk = 10f,
            });

            // ── Archer → Hunter (PvM, Bogen) ────────────────────────────────
            AddTemplate(new StatBuildTemplate
            {
                Id = "hunter_pvm",
                BaseClass = "Archer",
                Playstyle = "PvM",
                Description = "Jäger: Dex für Treffer/Angriff, Agi für Flee, Str für Schaden",
                StrWeight = 25f,
                Agi = 25f,
                Vit = 15f,
                Int = 0f,
                Dex = 30f,
                Luk = 5f,
            });

            // ── Acolyte → Priest (Support) ──────────────────────────────────
            AddTemplate(new StatBuildTemplate
            {
                Id = "priest_support",
                BaseClass = "Acolyte",
                Playstyle = "Support",
                Description = "Priest: Int für SP/Heilung, Vit für Überleben",
                StrWeight = 0f,
                Agi = 0f,
                Vit = 35f,
                Int = 50f,
                Dex = 10f,
                Luk = 5f,
            });

            // ── Thief → Assassin (PvM/PvP) ──────────────────────────────────
            AddTemplate(new StatBuildTemplate
            {
                Id = "assassin_pvm",
                BaseClass = "Thief",
                Playstyle = "PvM",
                Description = "Assassin: Agi für Flee/ASP, Dex für Treffer, Luk für Krit",
                StrWeight = 15f,
                Agi = 30f,
                Vit = 15f,
                Int = 0f,
                Dex = 25f,
                Luk = 15f,
            });

            // ── Merchant → Blacksmith (PvM) ─────────────────────────────────
            AddTemplate(new StatBuildTemplate
            {
                Id = "blacksmith_pvm",
                BaseClass = "Merchant",
                Playstyle = "PvM",
                Description = "Schmied: Str für Schaden, Vit für Überleben",
                StrWeight = 40f,
                Agi = 0f,
                Vit = 35f,
                Int = 0f,
                Dex = 20f,
                Luk = 5f,
            });

            // ── Generisch (Novice oder unbekannte Klasse) ───────────────────
            AddTemplate(new StatBuildTemplate
            {
                Id = "generic_hybrid",
                BaseClass = "Novice",
                Playstyle = "Hybrid",
                Description = "Ausgewogene Allokation für unbekannte Klasse",
                StrWeight = 20f,
                Agi = 20f,
                Vit = 20f,
                Int = 15f,
                Dex = 15f,
                Luk = 10f,
            });
        }
    }

    /// <summary>
    /// Ein Stat-Build-Template für eine bestimmte Klasse und Spielstil.
    /// </summary>
    public class StatBuildTemplate
    {
        public string Id { get; set; } = "";
        public string BaseClass { get; set; } = "";
        public string Playstyle { get; set; } = "Hybrid";
        public string Description { get; set; } = "";

        /// <summary>Relative Gewichtung für Stärke (0-100).</summary>
        public float StrWeight { get; set; } = 20f;
        /// <summary>Relative Gewichtung für Agilität (0-100).</summary>
        public float AgiWeight { get; set; } = 20f;
        /// <summary>Relative Gewichtung für Vitalität (0-100).</summary>
        public float VitWeight { get; set; } = 20f;
        /// <summary>Relative Gewichtung für Intelligenz (0-100).</summary>
        public float IntWeight { get; set; } = 20f;
        /// <summary>Relative Gewichtung für Geschicklichkeit (0-100).</summary>
        public float DexWeight { get; set; } = 20f;
        /// <summary>Relative Gewichtung für Glück (0-100).</summary>
        public float LukWeight { get; set; } = 0f;

        // Hilfs-Eigenschaften für Serialisierung/Einfachheit (spiegeln Weight wider)
        public float Agi { get => AgiWeight; set => AgiWeight = value; }
        public float Vit { get => VitWeight; set => VitWeight = value; }
        public float Int { get => IntWeight; set => IntWeight = value; }
        public float Dex { get => DexWeight; set => DexWeight = value; }
        public float Luk { get => LukWeight; set => LukWeight = value; }
    }

    /// <summary>
    /// Ergebnis einer Stat-Allokations-Empfehlung.
    /// </summary>
    public class StatAllocation
    {
        public int Str { get; set; }
        public int Agi { get; set; }
        public int Vit { get; set; }
        public int Int { get; set; }
        public int Dex { get; set; }
        public int Luk { get; set; }
        public int TotalPoints { get; set; }
    }

    /// <summary>
    /// Ergebnis einer Stat-Build-Empfehlung.
    /// </summary>
    public class StatBuildRecommendation
    {
        public StatBuildTemplate Template { get; set; } = new();
        public float MatchScore { get; set; }
        public StatAllocation SuggestedStats { get; set; } = new();
    }
}
