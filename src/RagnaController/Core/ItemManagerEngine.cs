using System;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// FEAT-011: Auto-Potion &amp; Item-Verwaltung.
    /// Überwacht HP/SP-Schwellwerte pro konfiguriertem Item und feuert die Hotkeys,
    /// respektiert Cooldowns (per-item) und Check-Intervalle. Zero-Allokation im Tick-Pfad.
    /// </summary>
    public class ItemManagerEngine : IDisposable
    {
        private readonly InputCommandQueue _queue;

        // Parallel-Arrays zu _items → index-basierte Iteration ohne Allokation (PERF: O(n), n = Items)
        private ItemConfig[] _items = Array.Empty<ItemConfig>();
        private long[] _nextCheck = Array.Empty<long>();     // nächstes erlaubtes Prüfen (ms, TickCount64-Basis)
        private long[] _nextEligible = Array.Empty<long>();  // Cooldown-Ende (ms, TickCount64-Basis)

        private volatile bool _isRunning;
        private readonly Func<long> _clock;

        /// <summary>True wenn die Engine aktiv prüft (Start/Stop gesteuert).</summary>
        public bool IsRunning { get => _isRunning; }

        /// <summary>Feuert wenn ein Item eingesetzt wurde (für Logging/Telemetrie).</summary>
        public event Action<ItemConfig>? ItemFired;

        /// <summary>
        /// Erzeugt die ItemManagerEngine.
        /// </summary>
        /// <param name="queue">Input-Queue für das Firen der Hotkeys.</param>
        /// <param name="clock">Zeitquelle (Test-Injektion möglich; Default: Environment.TickCount64).</param>
        public ItemManagerEngine(InputCommandQueue queue, Func<long>? clock = null)
        {
            _queue = queue;
            _clock = clock ?? (() => Environment.TickCount64);
        }

        /// <summary>
        /// Konfiguriert die verwalteten Items (Profil-Load). Setzt alle Timer zurück.
        /// Thread-Hinweis: wird vom UI-Thread beim Profil-Load aufgerufen, Update() vom Tick-Thread —
        /// Array-Referenz-Swap ist atomar, identisch mit dem etablierten Engine-Pattern (CombatEngine._profile).
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

        /// <summary>Aktiviert die Prüfung (Profil-Flag ItemManagerEnabled).</summary>
        public void Start() => _isRunning = true;

        /// <summary>Deaktiviert die Prüfung (Pause/Disconnect/Profil-Switch).</summary>
        public void Stop() => _isRunning = false;

        /// <summary>
        /// Reset: alle Items und Timer löschen (Profil-Load / Shutdown).
        /// </summary>
        public void Reset()
        {
            _isRunning = false;
            _items = Array.Empty<ItemConfig>();
            _nextCheck = Array.Empty<long>();
            _nextEligible = Array.Empty<long>();
        }

        /// <summary>
        /// Tick-Pfad: prüft alle Items gegen HP/SP-Schwellen. Zero-Allokation (nur Array-Iteration).
        /// </summary>
        /// <param name="hpPercent">Aktuelles HP in Prozent (0-100).</param>
        /// <param name="sp">Aktueller SP-Wert.</param>
        /// <param name="deltaMs">Delta des Ticks in ms (nur zur Signatur-Konsistenz mit anderen Engines).</param>
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
                // "Über" der Schwelle → nur Intervall-Timer setzen, kein Fire.
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

                // Cooldown aktiv → exakt zum Cooldown-Ende erneut prüfen (kein Intervall-Verlust).
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

        public void Dispose() => Stop();
    }
}
