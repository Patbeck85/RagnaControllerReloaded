using System;

namespace RagnaController.Core
{
    /// <summary>
    /// Menschliche Timing-Varianz für Anti-Cheat-Umgehung.
    /// Nutzt Random.Shared - threadsicher, extrem schnell, keine Allokationen im Hot Path (125Hz).
    /// </summary>
    public static class JitterService
    {
        // Random.Shared ist von Haus aus threadsicher (.NET 8+) - keine ThreadLocal-Notwendigkeit!
        public static int Apply(int baseMs, int variance = 4)
            => Math.Max(1, baseMs + Random.Shared.Next(-variance, variance + 1));

        public static int ClickHold() => Random.Shared.Next(15, 46);

        public static bool Chance(double probability) => Random.Shared.NextDouble() < probability;
    }
}
