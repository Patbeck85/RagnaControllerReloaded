using System.Collections.Concurrent;

namespace RagnaController.Core
{
    /// <summary>
    /// Provides zero-allocation string caching and object pooling for the hot path.
    /// Prevents generating new strings like "L1+A" or "BASE" over and over during gameplay.
    /// </summary>
    public sealed class EngineOptimizationPool
    {
        public static EngineOptimizationPool Instance { get; } = new();

        // Thread-safe string interning / caching using ConcurrentDictionary
        // Eliminates lock contention in hot path
        private readonly ConcurrentDictionary<string, string> _stringCache = new();

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
                _stringCache.TryAdd(p, p);
                foreach (var b in buttons)
                {
                    _stringCache.TryAdd(p + b, p + b);
                }
            }

            var staticStrings = new[] { 
                "BASE", "IDLE", "ENGAGED", "SEEKING", "ATTACKING", 
                "COMBO", "MAGE", "AUTO", "LOCKED",
                "MAGE MODE", "AUTO ATTACK"
            };
            foreach (var s in staticStrings)
            {
                _stringCache.TryAdd(s, s);
            }
        }

        /// <summary>
        /// Returns interned string from cache, or adds it atomically on first miss.
        /// Lock-free using ConcurrentDictionary.
        /// </summary>
        public string GetString(string input)
        {
            return _stringCache.GetOrAdd(input, input);
        }
    }
}