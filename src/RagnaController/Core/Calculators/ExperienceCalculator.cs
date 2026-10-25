using System;

namespace RagnaController.Core.Calculators {
    /// <summary>
    /// TASK-3006: ExperienceCalculator — Kill-Count für Level-Up, Optimale Maps per Level-Range.
    ///
    /// Berechnet EXP-Anforderungen und Kill-Counts für RO Level-Ups.
    /// White-Hat: Reine Berechnung, keine Automatisierung.
    /// </summary>
    public sealed class ExperienceCalculator
    {
        /// <summary>EXP benötigt für ein bestimmtes Base-Level (vereinfacht).</summary>
        public static long GetExpForBaseLevel(int level)
        {
            // Vereinfachte RO-EXP-Kurve: exponentiell ansteigend
            if (level <= 1) return 0;
            if (level <= 50) return (long)(Math.Pow(level, 2.5) * 100);
            if (level <= 99) return (long)(Math.Pow(level, 3.0) * 500);
            return (long)(Math.Pow(level, 3.5) * 1000);
        }

        /// <summary>EXP benötigt für ein bestimmtes Job-Level (vereinfacht).</summary>
        public static long GetExpForJobLevel(int level)
        {
            if (level <= 1) return 0;
            if (level <= 50) return (long)(Math.Pow(level, 2.0) * 200);
            if (level <= 70) return (long)(Math.Pow(level, 2.5) * 500);
            return (long)(Math.Pow(level, 3.0) * 1000);
        }

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
    }
}
