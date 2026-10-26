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
        #region GameMode

        /// <summary>Spielmodus: Pre-Renewal oder Renewal.</summary>
        public enum GameMode
        {
            PreRenewal,
            Renewal
        }

        #endregion

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

        /// <summary>Berechnet die verfügbaren Job-Stat-Punkte für ein Job-Level.</summary>
        public static int GetAvailableJobStatPoints(int jobLevel)
        {
            // RO: 3 Job-Stat-Punkte pro Job-Level über Level 1
            return Math.Max(0, (jobLevel - 1) * 3);
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
            GameMode mode = GameMode.PreRenewal,
            int jobHpModifier = 0,
            int jobHpMultiplier = 1,
            double equipmentBonusPercent = 0.0,
            double blessingBonusPercent = 0.0)
        {
            var hpFactor = mode == GameMode.Renewal
                ? GetClassFactor(ClassBaseHpFactorRenewal, className)
                : GetClassFactor(ClassBaseHpFactorPreRenewal, className);

            // Basis HP aus Level und Klasse
            double baseHp = baseLevel * hpFactor;

            // VIT Bonus: jeder VIT-Punkt erhöht HP um ca. 1% des Basis-HP (vereinfacht)
            double vitBonus = baseHp * (vit * 0.01);

            double totalHp = baseHp + vitBonus + jobHpModifier;

            // Renewal Multiplier
            if (mode == GameMode.Renewal)
                totalHp *= 1.5;

            // Equipment und Blessing Boni (prozentual)
            totalHp *= (1.0 + equipmentBonusPercent / 100.0);
            totalHp *= (1.0 + blessingBonusPercent / 100.0);

            return (int)Math.Max(1, totalHp * jobHpMultiplier);
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
            GameMode mode = GameMode.PreRenewal,
            int jobSpModifier = 0,
            int jobSpMultiplier = 1,
            double equipmentBonusPercent = 0.0,
            double blessingBonusPercent = 0.0)
        {
            var spFactor = mode == GameMode.Renewal
                ? GetClassFactor(ClassBaseSpFactorRenewal, className)
                : GetClassFactor(ClassBaseSpFactorPreRenewal, className);

            // Basis SP aus Level und Klasse
            double baseSp = baseLevel * spFactor;

            // INT Bonus: jeder INT-Punkt erhöht SP um ca. 1% des Basis-SP (vereinfacht)
            double intBonus = baseSp * (int_ * 0.01);

            double totalSp = baseSp + intBonus + jobSpModifier;

            // Renewal Multiplier
            if (mode == GameMode.Renewal)
                totalSp *= 1.5;

            // Equipment und Blessing Boni (prozentual)
            totalSp *= (1.0 + equipmentBonusPercent / 100.0);
            totalSp *= (1.0 + blessingBonusPercent / 100.0);

            return (int)Math.Max(1, totalSp * jobSpMultiplier);
        }

        #endregion

        #region Angriffsgeschwindigkeit (ASPD)

        /// <summary>
        /// Berechnet Angriffsgeschwindigkeit basierend auf Agi, Dex und Klasse.
        /// RO-Formel: ASPD = 200 - (Agi * 0.5 + Dex * 0.3) - Klassenmodifikator + Delay-Reduktion
        /// </summary>
        public static double CalculateAttackSpeed(int agi, int dex, string className = "Novice", GameMode mode = GameMode.PreRenewal, double delayReduction = 0.0)
        {
            double classMod = mode == GameMode.Renewal
                ? GetAspdClassModRenewal(className)
                : GetAspdClassModPreRenewal(className);

            double aspd = 200 - (agi * 0.5 + dex * 0.3) - classMod + delayReduction;

            // Mindest-ASPD
            double minAspd = mode == GameMode.Renewal ? 10.0 : 50.0;

            return Math.Max(minAspd, aspd);
        }

        private static double GetAspdClassModPreRenewal(string className)
        {
            return className.ToLowerInvariant() switch
            {
                "swordman" or "knight" or "crusader" => 10,
                "mage" or "wizard" or "high wizard" or "professor" => 5,
                "acolyte" or "priest" or "high priest" => 5,
                "merchant" or "blacksmith" or "creator" => 10,
                "assassin" or "stalker" => -5,
                "thief" => -5,
                _ => 0
            };
        }

        private static double GetAspdClassModRenewal(string className)
        {
            return className.ToLowerInvariant() switch
            {
                "swordman" or "knight" or "crusader" => 8,
                "mage" or "wizard" or "high wizard" or "professor" => 4,
                "acolyte" or "priest" or "high priest" => 4,
                "merchant" or "blacksmith" or "creator" => 8,
                "assassin" or "stalker" => -4,
                "thief" => -4,
                _ => 0
            };
        }

        #endregion

        #region Treffer- und Fluchtwerte

        /// <summary>
        /// Berechnet Trefferwahrscheinlichkeit (Hit Rate).
        ///
        /// Pre-Renewal: Hit = Dex * 2 + Luk + Klassenmodifikator
        /// Renewal: Hit = Dex * 2 + Luk * 1.5 + Klassenmodifikator
        /// </summary>
        public static int CalculateHitRate(int dex, int luk, string className = "Novice", GameMode mode = GameMode.PreRenewal)
        {
            double lukMod = mode == GameMode.Renewal ? luk * 1.5 : luk;
            double classMod = GetHitClassMod(className);

            return (int)Math.Max(0, dex * 2 + lukMod + classMod);
        }

        private static double GetHitClassMod(string className)
        {
            return className.ToLowerInvariant() switch
            {
                "archer" or "hunter" or "sniper" => 10,
                "assassin" or "stalker" => 8,
                "thief" => 5,
                _ => 0
            };
        }

        /// <summary>
        /// Berechnet Fluchtwahrscheinlichkeit (Flee).
        ///
        /// Pre-Renewal: Flee = Agi * 2 + Luk + Klassenmodifikator
        /// Renewal: Flee = Agi * 2 + Luk * 1.5 + Klassenmodifikator
        /// </summary>
        public static int CalculateFlee(int agi, int luk, string className = "Novice", GameMode mode = GameMode.PreRenewal)
        {
            double lukMod = mode == GameMode.Renewal ? luk * 1.5 : luk;
            double classMod = GetFleeClassMod(className);

            return (int)Math.Max(0, agi * 2 + lukMod + classMod);
        }

        private static double GetFleeClassMod(string className)
        {
            return className.ToLowerInvariant() switch
            {
                "thief" or "assassin" or "stalker" => 10,
                "archer" or "hunter" or "sniper" => 8,
                _ => 0
            };
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

        #region Komplette Stat-Berechnung (alle Werte auf einmal)

        /// <summary>
        /// Ergebnis einer vollständigen Stat-Berechnung für Pre-Renewal oder Renewal.
        /// </summary>
        public class StatResult
        {
            public int BaseLevel { get; set; }
            public int JobLevel { get; set; }
            public string ClassName { get; set; } = "Novice";
            public GameMode Mode { get; set; } = GameMode.PreRenewal;
            public int Str { get; set; }
            public int Agi { get; set; }
            public int Vit { get; set; }
            public int Int_ { get; set; }
            public int Dex { get; set; }
            public int Luk { get; set; }
            public int MaxHP { get; set; }
            public int MaxSP { get; set; }
            public double AttackSpeed { get; set; }
            public int HitRate { get; set; }
            public int Flee { get; set; }
            public int MaxWeight { get; set; }
            public int AvailableStatPoints { get; set; }
            public int AvailableJobStatPoints { get; set; }
        }

        /// <summary>Berechnet alle Stats auf einmal für einen Charakter.</summary>
        public static StatResult CalculateAllStats(
            int baseLevel,
            int jobLevel,
            int str,
            int agi,
            int vit,
            int int_,
            int dex,
            int luk,
            string className = "Novice",
            GameMode mode = GameMode.PreRenewal,
            int jobHpModifier = 0,
            int jobSpModifier = 0,
            double equipmentHpBonusPercent = 0.0,
            double equipmentSpBonusPercent = 0.0,
            double blessingHpBonusPercent = 0.0,
            double blessingSpBonusPercent = 0.0,
            double delayReduction = 0.0)
        {
            return new StatResult
            {
                BaseLevel = baseLevel,
                JobLevel = jobLevel,
                ClassName = className,
                Mode = mode,
                Str = str,
                Agi = agi,
                Vit = vit,
                Int_ = int_,
                Dex = dex,
                Luk = luk,
                MaxHP = CalculateMaxHP(baseLevel, vit, className, mode, jobHpModifier, 1, equipmentHpBonusPercent, blessingHpBonusPercent),
                MaxSP = CalculateMaxSP(baseLevel, int_, className, mode, jobSpModifier, 1, equipmentSpBonusPercent, blessingSpBonusPercent),
                AttackSpeed = CalculateAttackSpeed(agi, dex, className, mode, delayReduction),
                HitRate = CalculateHitRate(dex, luk, className, mode),
                Flee = CalculateFlee(agi, luk, className, mode),
                MaxWeight = CalculateMaxWeight(str),
                AvailableStatPoints = GetAvailableStatPoints(baseLevel),
                AvailableJobStatPoints = GetAvailableJobStatPoints(jobLevel),
            };
        }

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
