using System;
using RagnaController.Models;

namespace RagnaController.Core.Calculators
{
    /// <summary>
    /// TASK-3006: ExperienceCalculator — Kill-Count für Level-Up, Optimale Maps per Level-Range.
    ///
    /// Berechnet EXP-Anforderungen und Kill-Counts für RO Level-Ups.
    /// Unterstützt Pre-Renewal und Renewal EXP-Kurven.
    /// White-Hat: Reine Berechnung, keine Automatisierung.
    /// </summary>
    public sealed class ExperienceCalculator
    {
        /// <summary>Spielmodus: Pre-Renewal oder Renewal.</summary>
        public enum GameMode
        {
            PreRenewal,
            Renewal
        }

        #region EXP-Kurven Pre-Renewal

        /// <summary>EXP-Tabelle für Base-Level (Pre-Renewal, max Level 99).</summary>
        private static readonly long[] BaseExpPreRenewal = BuildBaseExpTablePreRenewal();

        private static long[] BuildBaseExpTablePreRenewal()
        {
            var table = new long[100]; // Index 0-99
            for (int lvl = 1; lvl < 100; lvl++)
            {
                table[lvl] = GetPreRenewalBaseExp(lvl);
            }
            return table;
        }

        private static long GetPreRenewalBaseExp(int level)
        {
            if (level <= 1) return 0;
            if (level <= 10) return (long)(Math.Pow(level, 2.0) * 50);
            if (level <= 30) return (long)(Math.Pow(level, 2.3) * 80);
            if (level <= 50) return (long)(Math.Pow(level, 2.5) * 120);
            if (level <= 70) return (long)(Math.Pow(level, 2.8) * 250);
            if (level <= 85) return (long)(Math.Pow(level, 3.0) * 400);
            if (level <= 99) return (long)(Math.Pow(level, 3.2) * 600);
            return (long)(Math.Pow(level, 3.5) * 800);
        }

        /// <summary>EXP-Tabelle für Job-Level (Pre-Renewal, max Level 70).</summary>
        private static readonly long[] JobExpPreRenewal = BuildJobExpTablePreRenewal();

        private static long[] BuildJobExpTablePreRenewal()
        {
            var table = new long[71]; // Index 0-70
            for (int lvl = 1; lvl < 71; lvl++)
            {
                table[lvl] = GetPreRenewalJobExp(lvl);
            }
            return table;
        }

        private static long GetPreRenewalJobExp(int level)
        {
            if (level <= 1) return 0;
            if (level <= 10) return (long)(Math.Pow(level, 2.0) * 80);
            if (level <= 30) return (long)(Math.Pow(level, 2.2) * 150);
            if (level <= 50) return (long)(Math.Pow(level, 2.5) * 300);
            return (long)(Math.Pow(level, 3.0) * 600);
        }

        #endregion

        #region EXP-Kurven Renewal

        /// <summary>EXP-Tabelle für Base-Level (Renewal, max Level 175).</summary>
        private static readonly long[] BaseExpRenewal = BuildBaseExpTableRenewal();

        private static long[] BuildBaseExpTableRenewal()
        {
            var table = new long[176]; // Index 0-175
            for (int lvl = 1; lvl < 176; lvl++)
            {
                table[lvl] = GetRenewalBaseExp(lvl);
            }
            return table;
        }

        private static long GetRenewalBaseExp(int level)
        {
            if (level <= 1) return 0;
            if (level <= 50) return (long)(Math.Pow(level, 2.2) * 100);
            if (level <= 99) return (long)(Math.Pow(level, 2.7) * 300);
            if (level <= 130) return (long)(Math.Pow(level, 3.2) * 800);
            if (level <= 150) return (long)(Math.Pow(level, 3.5) * 1200);
            return (long)(Math.Pow(level, 3.8) * 2000);
        }

        /// <summary>EXP-Tabelle für Job-Level (Renewal, max Level 70 für erste Klasse, 50 für 2./3. Klasse).</summary>
        private static readonly long[] JobExpRenewal = BuildJobExpTableRenewal();

        private static long[] BuildJobExpTableRenewal()
        {
            var table = new long[71]; // Index 0-70
            for (int lvl = 1; lvl < 71; lvl++)
            {
                table[lvl] = GetRenewalJobExp(lvl);
            }
            return table;
        }

        private static long GetRenewalJobExp(int level)
        {
            if (level <= 1) return 0;
            if (level <= 10) return (long)(Math.Pow(level, 2.0) * 100);
            if (level <= 30) return (long)(Math.Pow(level, 2.4) * 200);
            if (level <= 50) return (long)(Math.Pow(level, 2.8) * 400);
            return (long)(Math.Pow(level, 3.2) * 700);
        }

        #endregion

        #region Public API

        /// <summary>EXP benötigt für ein bestimmtes Base-Level (Pre-Renewal oder Renewal).</summary>
        public static long GetExpForBaseLevel(int level, GameMode mode = GameMode.PreRenewal)
        {
            if (level <= 1) return 0;
            if (mode == GameMode.Renewal)
            {
                if (level >= BaseExpRenewal.Length) return BaseExpRenewal[BaseExpRenewal.Length - 1];
                return BaseExpRenewal[level];
            }
            if (level >= BaseExpPreRenewal.Length) return BaseExpPreRenewal[BaseExpPreRenewal.Length - 1];
            return BaseExpPreRenewal[level];
        }

        /// <summary>EXP benötigt für ein bestimmtes Job-Level (Pre-Renewal oder Renewal).</summary>
        public static long GetExpForJobLevel(int level, GameMode mode = GameMode.PreRenewal)
        {
            if (level <= 1) return 0;
            if (mode == GameMode.Renewal)
            {
                if (level >= JobExpRenewal.Length) return JobExpRenewal[JobExpRenewal.Length - 1];
                return JobExpRenewal[level];
            }
            if (level >= JobExpPreRenewal.Length) return JobExpPreRenewal[JobExpPreRenewal.Length - 1];
            return JobExpPreRenewal[level];
        }

        /// <summary>EXP benötigt für ein bestimmtes Base-Level (Legacy, Pre-Renewal).</summary>
        public static long GetExpForBaseLevel(int level) => GetExpForBaseLevel(level, GameMode.PreRenewal);

        /// <summary>EXP benötigt für ein bestimmtes Job-Level (Legacy, Pre-Renewal).</summary>
        public static long GetExpForJobLevel(int level) => GetExpForJobLevel(level, GameMode.PreRenewal);

        /// <summary>Berechnet Kill-Count für Level-Up basierend auf Monster-EXP.</summary>
        public static int CalculateKillCount(long expNeeded, int monsterExp, int partySize = 1, bool hasPartyBonus = true)
        {
            if (monsterExp <= 0) return 0;

            double effectiveExp = monsterExp;
            if (hasPartyBonus && partySize > 1)
            {
                // Party-Bonus: mehr EXP pro Kill, aber geteilt
                effectiveExp = monsterExp * (1.0 + (partySize - 1) * 0.1);
            }

            var kills = (long)Math.Ceiling(expNeeded / effectiveExp);
            return kills > int.MaxValue ? int.MaxValue : (int)kills;
        }

        /// <summary>Berechnet Kill-Count für Base-Level-Up.</summary>
        public static int CalculateKillCountForBaseLevel(int currentLevel, int targetLevel, int monsterExp, int partySize = 1, bool hasPartyBonus = true)
        {
            var expNeeded = GetExpForBaseLevel(targetLevel) - GetExpForBaseLevel(currentLevel);
            if (expNeeded <= 0) return 0;
            return CalculateKillCount(expNeeded, monsterExp, partySize, hasPartyBonus);
        }

        /// <summary>Berechnet Kill-Count für Job-Level-Up.</summary>
        public static int CalculateKillCountForJobLevel(int currentLevel, int targetLevel, int monsterExp, int partySize = 1, bool hasPartyBonus = true)
        {
            var expNeeded = GetExpForJobLevel(targetLevel) - GetExpForJobLevel(currentLevel);
            if (expNeeded <= 0) return 0;
            return CalculateKillCount(expNeeded, monsterExp, partySize, hasPartyBonus);
        }

        #endregion
    }
}
