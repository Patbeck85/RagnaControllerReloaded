using System;
using RagnaController.Models;
using RagnaController.Core;

namespace RagnaController.Core
{
    /// <summary>
    /// ItemManagerEngine — FEAT-011: Auto-potion and item management.
    /// Monitors HP/SP thresholds per configured item and fires hotkeys with per-item cooldowns.
    /// Zero-allocation tick path using parallel arrays for O(n) iteration.
    /// </summary>
    public class ItemManagerEngine : IDisposable
    {
        private readonly InputCommandQueue _queue;

        // Parallel-Arrays to _items → index-basierte Iteration ohne Allokation (PERF: O(n), n = Items)
        private ItemConfig[] _items = Array.Empty<ItemConfig>();
        private long[] _nextCheck = Array.Empty<long>();     // next allowed check (ms, TickCount64 basis)
        private long[] _nextEligible = Array.Empty<long>();  // Cooldown-Ende (ms, TickCount64-Basis)

        private volatile bool _isRunning;
        private readonly Func<long> _clock;

        /// <summary>True if engine actively checks (start/stop controlled).</summary>
        public bool IsRunning { get => _isRunning; }

        /// <summary>Feuert if ein Item eingesetzt wurde (for Logging/Telemetrie).</summary>
        public event Action<ItemConfig>? ItemFired;

        /// <summary>
        /// Erzeugt die ItemManagerEngine.
        /// </summary>
        /// <param name="clock">time source (test injection possible; default: Environment.TickCount64).</param>
        public ItemManagerEngine(InputCommandQueue queue, Func<long>? clock = null)
        {
            _queue = queue;
            _clock = clock ?? (() => Environment.TickCount64);
        }

        /// <summary>
        /// Configures managed items (profile load). Resets all timers.
        /// Thread-Note: is vom UI-Thread beim Profil-Load aufgerufen, Update() vom Tick-Thread —
        /// Array-Referenz-Swap is atomar, identisch with dem etablierten Engine-Pattern (CombatEngine._profile).
        /// </summary>
        public void Configure(IReadOnlyList<ItemConfig>? items)
        {
            int count = items?.Count ?? 0;
            _items = new ItemConfig[count];
            _nextCheck = new long[count];
            _nextEligible = new long[count];

            for (int i = 0; i < count; i++)
                _items[i] = items![i];
        }

        /// <summary>Enables the check (profile flag ItemManagerEnabled).</summary>
        public void Start() => _isRunning = true;

        /// <summary>Disables the check (pause/disconnect/profile switch).</summary>
        public void Stop() => _isRunning = false;

        /// <summary>
        /// Reset: delete all items and timers (profile load / shutdown).
        /// </summary>
        public void Reset()
        {
            _isRunning = false;
            _items = Array.Empty<ItemConfig>();
            _nextCheck = Array.Empty<long>();
            _nextEligible = Array.Empty<long>();
        }

        /// <summary>
        /// Tick path: checks all items against HP/SP thresholds. Zero-allocation (only array iteration).
        /// </summary>
        /// <param name="sp">Aktueller SP-Wert.</param>
        /// <param name="deltaMs">Delta des Ticks in ms (only zur Signatur-Konsistenz with anderen Engines).</param>
        public void Update(int hpPercent, int sp, int deltaMs)
        {
            if (!_isRunning) return;

            long now = _clock();
            for (int i = 0; i < _items.Length; i++)
            {
                var item = _items[i];
                if (!item.Enabled) continue;
                if (now < _nextCheck[i]) continue;

                // Schwelle: feuert erst ab UNTERSCHRITTENEM Wert (hp <= threshold).
                // "About" der Schwelle → only Intervall-Timer setzen, no Fire.
                if (hpPercent > item.HpThresholdPercent)
                {
                    _nextCheck[i] = now + Math.Max(1, item.CheckIntervalMs);
                    continue;
                }

                if (sp < item.MinSpRequired)
                {
                    _nextCheck[i] = now + Math.Max(1, item.CheckIntervalMs);
                    continue;
                }

                // Cooldown active → re-check exactly at cooldown end (no interval loss).
                if (now < _nextEligible[i])
                {
                    _nextCheck[i] = _nextEligible[i];
                    continue;
                }

                // ── FIRE ────────────────────────────────────────────────
                _queue.TapKey(item.Key);
                ItemFired?.Invoke(item);
                _nextEligible[i] = now + Math.Max(1, item.CooldownMs);
                _nextCheck[i] = now + Math.Max(1, item.CheckIntervalMs);
            }
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose() => Stop();
    }
}
