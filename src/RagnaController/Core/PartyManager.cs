using System;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// FEAT-012: Party Manager — verwaltet Party-Mitglieder (max. 5) und feuert den
    /// autonomen Heal-Loop: ist ein Mitglied unter der HP-Schwelle, wird per Tab-Drücken
    /// im Party-Ring zum schwächsten Member zyklisch navigiert und der konfigurierte
    /// Heal-Key gefeuert (konfigurierbares Intervall).
    /// Zero-Allokation im Tick-Pfad (Fix-Size-Array, index-basiert).
    /// Manuelles Verhalten (Y/R1 in SupportEngine) bleibt unverändert.
    /// </summary>
    public class PartyManager : IDisposable
    {
        /// <summary>Maximale Anzahl verwalteter Party-Mitglieder (RO-Party-Limit).</summary>
        public const int MaxMembers = 5;

        private readonly InputCommandQueue _queue;
        private readonly Func<long> _clock;

        // Fix-Size-Array → Zero-Allokation im Tick-Pfad (PERF: O(n), n ≤ 5)
        private readonly int[] _memberHp = new int[MaxMembers];

        private volatile bool _isRunning;
        private long _nextEligible;   // ms, TickCount64-Basis — nächster erlaubter Heal
        private int _currentIndex;    // aktuelle Zielposition im Party-Ring (0-basiert)

        /// <summary>Anzahl verwalteter Mitglieder (1..5, wird bei Update geclampt).</summary>
        public int MemberCount { get; set; } = 1;

        /// <summary>HP-Schwelle in Prozent: feuert erst ab unterschrittenem Wert (hp &lt;= Schwelle).</summary>
        public int HealThresholdPercent { get; set; } = 70;

        /// <summary>Intervall in ms zwischen zwei autonomen Heals.</summary>
        public int HealIntervalMs { get; set; } = 5000;

        /// <summary>Hotkey für die Party-Heal-Aktion. Default: Z (90).</summary>
        public VirtualKey HealKeyVK { get; set; } = (VirtualKey)90;

        /// <summary>True wenn der Auto-Heal-Loop aktiv ist (Start/Stop gesteuert).</summary>
        public bool IsRunning => _isRunning;

        /// <summary>Feuert wenn ein Mitglied geheilt wurde (Index des schwächsten Members).</summary>
        public event Action<int>? MemberHealed;

        /// <summary>
        /// Erzeugt den PartyManager.
        /// </summary>
        /// <param name="queue">Input-Queue für Tab-Zyklen und Heal-Key.</param>
        /// <param name="clock">Zeitquelle (Test-Injektion möglich; Default: Environment.TickCount64).</param>
        public PartyManager(InputCommandQueue queue, Func<long>? clock = null)
        {
            _queue = queue;
            _clock = clock ?? (() => Environment.TickCount64);
            // Unbekannter Zustand = voll (100 %), konsistent mit Reset():
            // Auto-Heal feuert nicht für Mitglieder, deren HP noch nie geparst wurde.
            for (int i = 0; i < MaxMembers; i++)
                _memberHp[i] = 100;
        }

        /// <summary>Aktiviert den Auto-Heal-Loop (Profil-Flag PartyManagerEnabled).</summary>
        public void Start()
        {
            _isRunning = true;
            _nextEligible = 0; // sofort feuern, wenn die Bedingung erfüllt ist (kein künstlicher Delay)
        }

        /// <summary>Deaktiviert den Auto-Heal-Loop (Pause/Disconnect/Profil-Switch).</summary>
        public void Stop() => _isRunning = false;

        /// <summary>
        /// Setzt den HP-Wert eines Mitglieds (Index 0..MemberCount-1).
        /// Out-of-Range-Indizes werden ignoriert (defensive, Zero-Cost).
        /// </summary>
        public void SetMemberHp(int index, int hpPercent)
        {
            if (index < 0 || index >= Math.Min(MemberCount, MaxMembers)) return;
            _memberHp[index] = Math.Clamp(hpPercent, 0, 100);
        }

        /// <summary>Reset: alle Mitglieder auf 100%, Loop gestoppt (Profil-Load / Shutdown).</summary>
        public void Reset()
        {
            _isRunning = false;
            _nextEligible = 0;
            _currentIndex = 0;
            for (int i = 0; i < MaxMembers; i++)
                _memberHp[i] = 100;
        }

        /// <summary>
        /// Tick-Pfad: prüft alle Mitglieder gegen die HP-Schwelle und feuert den Heal-Loop.
        /// Zero-Allokation (nur Fix-Size-Array-Iteration, n ≤ 5).
        /// </summary>
        public void Update()
        {
            if (!_isRunning) return;

            int count = Math.Clamp(MemberCount, 1, MaxMembers);
            long now = _clock();

            // Schwächstes Mitglied unter der Schwelle suchen (Tie → niedrigerer Index).
            int weakest = -1;
            for (int i = 0; i < count; i++)
            {
                if (_memberHp[i] > HealThresholdPercent) continue;
                if (weakest < 0 || _memberHp[i] < _memberHp[weakest])
                    weakest = i;
            }

            // Kein Mitglied unter der Schwelle oder Intervall noch aktiv → kein Fire.
            if (weakest < 0 || now < _nextEligible) return;

            // Vorwärts-Zyklus im Party-Ring: Ctrl+Tab-Drücke bis zum schwächsten Member
            // (RO-Konvention: Tab = Mob-Ziel, Ctrl+Tab = Party-Ziel — konsistent mit SupportEngine).
            int distance = (weakest - _currentIndex + count) % count;
            for (int i = 0; i < distance; i++)
                _queue.TapKeyWithModifier(VirtualKey.ControlLeft, VirtualKey.Tab);
            if (distance > 0)
                _queue.Wait(15); // Zielwechsel dem Spiel verarbeiten lassen

            _queue.TapKey(HealKeyVK);
            _currentIndex = weakest;
            _nextEligible = now + Math.Max(1, HealIntervalMs);
            MemberHealed?.Invoke(weakest);
        }

        public void Dispose() => Stop();
    }
}
