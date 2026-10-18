using System;

namespace RagnaController.Core
{
    /// <summary>
    /// Menschliche Timing-Varianz for Anti-Cheat-Umgehung.
    /// Nutzt Random.Shared - threadsicher, extrem schnell, keine Allokationen im Hot Path (125Hz).
    /// </summary>
    public static class JitterService
    {
        // Random.Shared is from Haus aus threadsicher (.NET 8+) - keine ThreadLocal-Notwendigkeit!
        public static int Apply(int baseMs, int variance = 4)
            => Math.Max(1, baseMs + Random.Shared.Next(-variance, variance + 1));

        /// <summary>
        /// ClickHold method.
        /// <returns>Result of ClickHold.</returns>
        /// </summary>
        public static int ClickHold() => Random.Shared.Next(15, 46);

        /// <summary>
        /// Chance method.
        /// <param name="probability">probability parameter.</param>
        /// <returns>Result of Chance.</returns>
        /// </summary>
        public static bool Chance(double probability) => Random.Shared.NextDouble() < probability;
    }
}
