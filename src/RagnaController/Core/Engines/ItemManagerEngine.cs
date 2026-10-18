using System;
using RagnaController.Models;

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
        private long[] _nextCheck = Array.Empty<long>();     // nächstes erlaubtes Prüfen (ms, TickCount64-Basis)
        private long[] _nextEligible = Array.Empty<long>();  // Cooldown-Ende (ms, TickCount64-Basis)

        private volatile bool _isRunning;
        private readonly Func<long> _clock;

        /// <summary>True if die Engine active prüft (Start/Stop gesteuert).</summary>
        public bool IsRunning { get => _isRunning; }

        /// <summary>Feuert if ein Item eingesetzt wurde (for Logging/Telemetrie).</summary>
        public event Action<ItemConfig>? ItemFired;

        /// <summary>
        /// Erzeugt die ItemManagerEngine.
        /// </summary>
        /// <param name="clock">Zeitquelle (Test-Injektion möglich; Default: Environment.TickCount64).</param>
        public ItemManagerEngine(InputCommandQueue queue, Func<long>? clock = null)
        {
            _queue = queue;
            _clock = clock ?? (() => Environment.TickCount64);
        }

        /// <summary>
        /// Konfiguriert die verwalteten Items (Profil-Load). Setzt all Timer zurück.
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

        /// <summary>Enabled die Prüfung (Profil-Flag ItemManagerEnabled).</summary>
        public void Start() => _isRunning = true;

        /// <summary>Disabled die Prüfung (Pause/Disconnect/Profil-Switch).</summary>
        public void Stop() => _isRunning = false;

        /// <summary>
        /// Reset: all Items and Timer löschen (Profil-Load / Shutdown).
        /// </summary>
        public void Reset()
        {
            _isRunning = false;
            _items = Array.Empty<ItemConfig>();
            _nextCheck = Array.Empty<long>();
            _nextEligible = Array.Empty<long>();
        }

        /// <summary>
        /// Tick-Pfad: prüft all Items gegen HP/SP-Schwellen. Zero-Allokation (only Array-Iteration).
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

                // Cooldown active → exakt zum Cooldown-Ende erneut prüfen (no Intervall-Verlust).
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
