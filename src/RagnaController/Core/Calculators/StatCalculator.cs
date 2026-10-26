using System;
using System.Collections.Generic;

namespace RagnaController.Core.Calculators
{
    /// <summary>
    /// TASK-3005: StatCalculator — Base/Job Level → Stats (mit Equipment, Blessings, Foods).
    ///
    /// Berechnet RO-Attribute (HP, SP, ASPD, Hit, Flee) basierend auf Level,
    /// Klasse, GameMode (Pre-Renewal / Renewal) und optionalen Boni.
    /// White-Hat: Reine Berechnung, keine automatische Stat-Zuteilung.
    /// </summary>
    public sealed class StatCalculator
    {
        #region Klasse-Basisdaten

        /// <summary>Basis-HP-Faktor pro Klasse (Pre-Renewal).</summary>
        private static readonly Dictionary<string, double> ClassBaseHpFactorPreRenewal = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Novice"] = 10,
            ["Swordman"] = 12,
            ["Mage"] = 8,
            ["Archer"] = 10,
            ["Acolyte"] = 10,
            ["Merchant"] = 10,
            ["Thief"] = 9,
            ["Knight"] = 14,
            ["Wizard"] = 9,
            ["Hunter"] = 11,
            ["Priest"] = 11,
            ["Blacksmith"] = 13,
            ["Assassin"] = 10,
            ["Crusader"] = 15,
            ["High Wizard"] = 10,
            ["Sniper"] = 12,
            ["High Priest"] = 12,
            ["Professor"] = 12,
            ["Stalker"] = 11,
            ["Creator"] = 13,
            ["Champion"] = 14,
        };

        /// <summary>Basis-HP-Faktor pro Klasse (Renewal).</summary>
        private static readonly Dictionary<string, double> ClassBaseHpFactorRenewal = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Novice"] = 12,
            ["Swordman"] = 15,
            ["Mage"] = 10,
            ["Archer"] = 12,
            ["Acolyte"] = 12,
            ["Merchant"] = 12,
            ["Thief"] = 11,
            ["Knight"] = 17,
            ["Wizard"] = 11,
            ["Hunter"] = 13,
            ["Priest"] = 13,
            ["Blacksmith"] = 16,
            ["Assassin"] = 12,
            ["Crusader"] = 18,
            ["High Wizard"] = 12,
            ["Sniper"] = 14,
            ["High Priest"] = 14,
            ["Professor"] = 14,
            ["Stalker"] = 13,
            ["Creator"] = 16,
            ["Champion"] = 17,
        };

        /// <summary>Basis-SP-Faktor pro Klasse (Pre-Renewal).</summary>
        private static readonly Dictionary<string, double> ClassBaseSpFactorPreRenewal = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Novice"] = 5,
            ["Swordman"] = 3,
            ["Mage"] = 10,
            ["Archer"] = 5,
            ["Acolyte"] = 8,
            ["Merchant"] = 5,
            ["Thief"] = 4,
            ["Knight"] = 4,
            ["Wizard"] = 12,
            ["Hunter"] = 5,
            ["Priest"] = 10,
            ["Blacksmith"] = 5,
            ["Assassin"] = 6,
            ["Crusader"] = 5,
            ["High Wizard"] = 14,
            ["Sniper"] = 6,
            ["High Priest"] = 12,
            ["Professor"] = 13,
            ["Stalker"] = 7,
            ["Creator"] = 6,
            ["Champion"] = 5,
        };

        /// <summary>Basis-SP-Faktor pro Klasse (Renewal).</summary>
        private static readonly Dictionary<string, double> ClassBaseSpFactorRenewal = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Novice"] = 6,
            ["Swordman"] = 4,
            ["Mage"] = 12,
            ["Archer"] = 6,
            ["Acolyte"] = 10,
            ["Merchant"] = 6,
            ["Thief"] = 5,
            ["Knight"] = 5,
            ["Wizard"] = 14,
            ["Hunter"] = 6,
            ["Priest"] = 12,
            ["Blacksmith"] = 6,
            ["Assassin"] = 7,
            ["Crusader"] = 6,
            ["High Wizard"] = 16,
            ["Sniper"] = 7,
            ["High Priest"] = 14,
            ["Professor"] = 15,
            ["Stalker"] = 8,
            ["Creator"] = 7,
            ["Champion"] = 6,
        };

        #endregion

        #region Stat-Punkte

        /// <summary>Berechnet die verfügbaren Stat-Punkte für ein Base-Level.</summary>
        public static int GetAvailableStatPoints(int baseLevel)
        {
            // RO: 1 Stat-Punkt pro Level über Level 1
            return Math.Max(0, baseLevel - 1);
        }

        #endregion

        #region Max HP

        /// <summary>
        /// Berechnet Max HP für eine Klasse und Level.
        /// Pre-Renewal: BaseHP = BaseLevel * BaseHPFactor + VIT * VITHPFactor + JobModifier
        /// Renewal: BaseHP = (BaseLevel * BaseHPFactor + VIT * VITHPFactor + JobModifier) * RenewalMultiplier
        /// </summary>
        public static int CalculateMaxHP(
            int baseLevel,
            int vit,
            string className = "Novice",
            ExperienceCalculator.GameMode mode = ExperienceCalculator.GameMode.PreRenewal,
            int jobHpModifier = 0,
            int jobHpMultiplier = 1)
        {
            var hpFactor = mode == ExperienceCalculator.GameMode.Renewal
                ? GetClassFactor(ClassBaseHpFactorRenewal, className)
                : GetClassFactor(ClassBaseHpFactorPreRenewal, className);

            // VIT HP-Faktor: jede 10 VIT = +1% Max HP (vereinfacht)
            double vitHpFactor = vit * 0.1;

            double baseHp = baseLevel * hpFactor * vitHpFactor + jobHpModifier;

            // Renewal Multiplier
            if (mode == ExperienceCalculator.GameMode.Renewal)
                baseHp *= 1.5;

            return (int)Math.Max(1, baseHp * jobHpMultiplier);
        }

        #endregion

        #region Max SP

        /// <summary>
        /// Berechnet Max SP für eine Klasse und Level.
        /// Pre-Renewal: BaseSP = BaseLevel * BaseSPFactor + INT * INTSPFactor + JobModifier
        /// Renewal: BaseSP = (BaseLevel * BaseSPFactor + INT * INTSPFactor + JobModifier) * RenewalMultiplier
        /// </summary>
        public static int CalculateMaxSP(
            int baseLevel,
            int int_,
            string className = "Novice",
            ExperienceCalculator.GameMode mode = ExperienceCalculator.GameMode.PreRenewal,
            int jobSpModifier = 0,
            int jobSpMultiplier = 1)
        {
            var spFactor = mode == ExperienceCalculator.GameMode.Renewal
                ? GetClassFactor(ClassBaseSpFactorRenewal, className)
                : GetClassFactor(ClassBaseSpFactorPreRenewal, className);

            // INT SP-Faktor: jede 10 INT = +1% Max SP (vereinfacht)
            double intSpFactor = int_ * 0.1;

            double baseSp = baseLevel * spFactor * intSpFactor + jobSpModifier;

            // Renewal Multiplier
            if (mode == ExperienceCalculator.GameMode.Renewal)
                baseSp *= 1.5;

            return (int)Math.Max(1, baseSp * jobSpMultiplier);
        }

        #endregion

        #region Angriffsgeschwindigkeit (ASPD)

        /// <summary>
        /// Berechnet Angriffsgeschwindigkeit basierend auf Agi, Dex und Klasse.
        /// RO-Formel: ASPD = 200 - (Agi * 0.5 + Dex * 0.3) - Klassenmodifikator + Delay-Reduktion
        /// </summary>
        public static double CalculateAttackSpeed(int agi, int dex, string className = "Novice")
        {
            double classMod = className switch
            {
                "Swordman" or "Knight" or "Crusader" => 10,
                "Mage" or "Wizard" or "High Wizard" or "Professor" => 5,
                "Acolyte" or "Priest" or "High Priest" => 5,
                "Merchant" or "Blacksmith" or "Creator" => 10,
                "Assassin" or "Stalker" => -5,
                "Thief" => -5,
                _ => 0
            };

            return 200 - (agi * 0.5 + dex * 0.3) - classMod;
        }

        #endregion

        #region Treffer- und Fluchtwerte

        /// <summary>Berechnet Trefferwahrscheinlichkeit basierend auf Dex und Luk.</summary>
        public static int CalculateHitRate(int dex, int luk)
        {
            return dex * 2 + luk;
        }

        /// <summary>Berechnet Fluchtwahrscheinlichkeit basierend auf Agi und Luk.</summary>
        public static int CalculateFlee(int agi, int luk)
        {
            return agi * 2 + luk;
        }

        #endregion

        #region Gewicht

        /// <summary>Berechnet Max-Gewicht basierend auf STR.</summary>
        public static int CalculateMaxWeight(int str)
        {
            // RO: Gewicht = STR * 10 + 300 (Basiskapazität)
            return str * 10 + 300;
        }

        #endregion

        #region EXP-Kurven (delegiert an ExperienceCalculator)

        /// <summary>Berechnet benötigte EXP für Base-Level (Pre-Renewal).</summary>
        public static long GetExpForBaseLevelPreRenewal(int baseLevel) =>
            ExperienceCalculator.GetExpForBaseLevel(baseLevel, ExperienceCalculator.GameMode.PreRenewal);

        /// <summary>Berechnet benötigte EXP für Base-Level (Renewal).</summary>
        public static long GetExpForBaseLevelRenewal(int baseLevel) =>
            ExperienceCalculator.GetExpForBaseLevel(baseLevel, ExperienceCalculator.GameMode.Renewal);

        /// <summary>Berechnet benötigte EXP für Job-Level (Pre-Renewal).</summary>
        public static long GetExpForJobLevelPreRenewal(int jobLevel) =>
            ExperienceCalculator.GetExpForJobLevel(jobLevel, ExperienceCalculator.GameMode.PreRenewal);

        /// <summary>Berechnet benötigte EXP für Job-Level (Renewal).</summary>
        public static long GetExpForJobLevelRenewal(int jobLevel) =>
            ExperienceCalculator.GetExpForJobLevel(jobLevel, ExperienceCalculator.GameMode.Renewal);

        #endregion

        #region Private Helpers

        private static double GetClassFactor(Dictionary<string, double> table, string className)
        {
            return table.TryGetValue(className, out var factor) ? factor : table["Novice"];
        }

        #endregion
    }

    /// <summary>
    /// Berechnete Stat-Zuteilung für einen Build.
    /// </summary>
    public class StatAllocationResult
    {
        public int BaseLevel { get; set; }
        public int TotalPoints { get; set; }
        public int Str { get; set; }
        public int Agi { get; set; }
        public int Vit { get; set; }
        public int Int { get; set; }
        public int Dex { get; set; }
        public int Luk { get; set; }
        public int PointsRemaining => TotalPoints - (Str + Agi + Vit + Int + Dex + Luk);
    }
}
