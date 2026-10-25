using System;

namespace RagnaController.Core.Calculators {
    /// <summary>
    /// TASK-3005: StatCalculator — Base/Job Level → Stats (mit Equipment, Blessings, Foods).
    ///
    /// Berechnet RO-Attribute (Str/Agi/Vit/Int/Dex/Luk) basierend auf Level
    /// und optionalen Boni.
    /// White-Hat: Reine Berechnung, keine automatische Stat-Zuteilung.
    /// </summary>
    public sealed class StatCalculator
    {
        /// <summary>Berechnet die verfügbaren Stat-Punkte für ein Base-Level.</summary>
        public static int GetAvailableStatPoints(int baseLevel)
        {
            // RO: 1 Stat-Punkt pro Level über Level 1
            return Math.Max(0, baseLevel - 1);
        }

        /// <summary>Berechnet Max HP für eine Klasse und Level (vereinfacht).</summary>
        public static int CalculateMaxHP(int baseLevel, int vit, int jobHpModifier = 10)
        {
            return baseLevel * 10 + vit * 5 + jobHpModifier;
        }

        /// <summary>Berechnet Max SP für eine Klasse und Level (vereinfacht).</summary>
        public static int CalculateMaxSP(int baseLevel, int int_, int jobSpModifier = 5)
        {
            return baseLevel * 5 + int_ * 3 + jobSpModifier;
        }

        /// <summary>Berechnet Angriffsgeschwindigkeit basierend auf Agi und Dex.</summary>
        public static double CalculateAttackSpeed(int agi, int dex)
        {
            // Vereinfachte Formel
            return 200 - (agi * 0.5 + dex * 0.3);
        }

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
