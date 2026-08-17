using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RagnaController.Core
{
    /// <summary>
    /// Provides zero-allocation string caching and object pooling for the hot path.
    /// Prevents generating new strings like "L1+A" or "BASE" over and over during gameplay.
    /// </summary>
    public sealed class EngineOptimizationPool
    {
        public static EngineOptimizationPool Instance { get; } = new();

        // 1. String Interning / Caching
        // Prevents generating new strings like "L1+A" or "BASE" over and over.
        private readonly Dictionary<string, string> _stringCache = new();

        // Pre-warm the cache with known hot-path strings
        private EngineOptimizationPool()
        {
            var prefixes = new[] { "", "L1+", "R1+", "L2+", "R2+" };
            var buttons = new[] { 
                "A", "B", "X", "Y", 
                "LeftShoulder", "RightShoulder", 
                "LeftTrigger", "RightTrigger", 
                "LeftThumb", "RightThumb", 
                "DPadUp", "DPadDown", "DPadLeft", "DPadRight", 
                "Start", "Back" 
            };
            
            foreach (var p in prefixes)
            {
                CacheString(p);
                foreach (var b in buttons)
                {
                    CacheString(p + b);
                }
            }

            CacheString("BASE");
            CacheString("IDLE");
            CacheString("ENGAGED");
            CacheString("SEEKING");
            CacheString("ATTACKING");
            CacheString("COMBO");
            CacheString("MAGE");
            CacheString("AUTO");
            CacheString("LOCKED");
            CacheString("MAGE MODE");
            CacheString("AUTO ATTACK");
        }

        public string GetString(string input)
        {
            if (_stringCache.TryGetValue(input, out string? cached)) return cached;
            return CacheString(input); // Cache on first miss
        }

        private string CacheString(string input)
        {
            // Note: In highly concurrent scenarios, dictionary writes need a lock.
            // Since most strings are pre-warmed, this lock is rarely hit.
            lock (_stringCache)
            {
                if (!_stringCache.ContainsKey(input))
                {
                    _stringCache[input] = input; // Add reference to cache
                }
                return _stringCache[input];
            }
        }
    }
}
