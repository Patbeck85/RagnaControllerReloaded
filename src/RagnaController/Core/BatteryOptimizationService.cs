using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace RagnaController.Core
{
    /// <summary>
    /// Battery-Optimierung für Steam Deck / Handheld.
    /// Überwacht den Windows-Batterie-Status und empfiehlt aggressive
    /// Tick-Rate-Reduktion sowie Smart-Standby-Verstärkung beim
    /// Batterie-Betrieb (nicht am Netzteil), um Akkulaufzeit zu verlängern.
    ///
    /// Architektur:
    ///   - SystemInformation.PowerStatus überwacht AC/Batterie
    ///   - Func<PowerStatus> ist injizierbar → unit-testbar ohne echte Hardware
    ///   - PowerStatusChanged-Event bei Modus-Wechsel
    ///   - GetRecommendedTickRate() liefert Hz je nach Modus (AC: 125, Battery: 60)
    /// </summary>
    public sealed class BatteryOptimizationService : IDisposable
    {
        private bool _disposed;
        private bool _isOnBattery;
        private int _batteryPercent = 100;
        private bool _isCharging;
        private PowerStatus _lastStatus;
        private readonly Func<PowerStatus>? _getPowerStatus;

        /// <summary>True wenn das Gerät auf Batterie läuft (nicht am Netzteil).</summary>
        public bool IsOnBattery => _isOnBattery;

        /// <summary>Aktueller Batterie-Ladezustand in Prozent (0–100).</summary>
        public int BatteryPercent => _batteryPercent;

        /// <summary>True wenn das Gerät gerade geladen wird.</summary>
        public bool IsCharging => _isCharging;

        /// <summary>Event bei PowerSource-Wechsel (AC ↔ Batterie).</summary>
        public event EventHandler? PowerStatusChanged;

        /// <summary>
        /// Erzeugt Service aus dem echten Windows PowerStatus (Production).
        /// </summary>
        /// <summary>Empfohlene Tick-Rate in Hz (AC: 125, Battery: 60 zur Akku-Ersparnis).</summary>
        public int GetRecommendedTickRateHz()
        {
            if (_disposed) return 125;
            return IsOnBattery ? 60 : 125;
        }

        /// <summary>Empfohlener Smart-Standby-Modus (Battery: aggressiver).</summary>
        public bool GetRecommendedAggressiveStandby()
        {
            if (_disposed) return false;
            return IsOnBattery;
        }

        public BatteryOptimizationService() : this(() => SystemInformation.PowerStatus)
        {
        }

        /// <summary>
        /// Erzeugt Service mit einfachen Werten (für Unit-Tests).
        /// </summary>
        public BatteryOptimizationService(bool isOnBattery, int batteryPercent = 100, bool isCharging = false)
        {
            _isOnBattery = isOnBattery;
            _batteryPercent = batteryPercent;
            _isCharging = isCharging;
        }

        /// <summary>
        /// Konstruktor mit injizierbarem PowerStatus-Factory (für Unit-Tests).
        /// </summary>
        private BatteryOptimizationService(Func<PowerStatus> getPowerStatus)
        {
            _getPowerStatus = getPowerStatus ?? throw new ArgumentNullException(nameof(getPowerStatus));
            _lastStatus = getPowerStatus();
            _isOnBattery = _lastStatus.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery &&
                           _lastStatus.BatteryChargeStatus != BatteryChargeStatus.Unknown &&
                           _lastStatus.PowerLineStatus != PowerLineStatus.Online;
            _batteryPercent = _lastStatus.BatteryLifePercent == 255 ? 100 : (int)_lastStatus.BatteryLifePercent;
            _isCharging = _lastStatus.BatteryChargeStatus.HasFlag(BatteryChargeStatus.Charging);
        }

        /// <summary>
        /// Prüft ob sich der PowerStatus geändert hat und feuert ggf. PowerStatusChanged.
        /// Sollte regelmäßig (z. B. pro Tick oder per Timer) aufgerufen werden.
        /// Nutzt nur den injizierten Factory-Delegaten (nicht die einfachen Werte).
        /// </summary>
        public void PollPowerStatus()
        {
            if (_disposed || _getPowerStatus == null) return;

            var newStatus = _getPowerStatus();
            bool newIsOnBattery = newStatus.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery &&
                                  newStatus.BatteryChargeStatus != BatteryChargeStatus.Unknown &&
                                  newStatus.PowerLineStatus != PowerLineStatus.Online;
            int newPercent = newStatus.BatteryLifePercent == 255 ? 100 : (int)newStatus.BatteryLifePercent;
            bool newIsCharging = newStatus.BatteryChargeStatus.HasFlag(BatteryChargeStatus.Charging);

            if (newIsOnBattery != _isOnBattery || newPercent != _batteryPercent || newIsCharging != _isCharging)
            {
                _isOnBattery = newIsOnBattery;
                _batteryPercent = newPercent;
                _isCharging = newIsCharging;
                Debug.WriteLine($"[BatteryOptimizationService] Power source changed: " +
                    $"OnBattery={_isOnBattery}, Percent={_batteryPercent}%");
                PowerStatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
