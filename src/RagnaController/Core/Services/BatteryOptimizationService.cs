using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace RagnaController.Core
{
    /// <summary>
    /// Battery-Optimierung for Steam Deck / Handheld.
    /// Monitors Windows battery status and recommends aggressive
    /// tick rate reduction and smart standby reinforcement on
    /// battery operation (not on power), to extend battery life.
    ///
    /// Architektur:
    ///   - SystemInformation.PowerStatus monitors AC/battery
    ///   - Func<PowerStatus> is injizierbar → unit-testbar ohne echte Hardware
    ///   - PowerStatusChanged-Event bei Modus-Wechsel
    ///   - GetRecommendedTickRate() liefert Hz je nach Modus (AC: 125, Battery: 60)
    /// </summary>
    /// <summary>
    /// Background service for game functionality.
    /// </summary>
    public sealed class BatteryOptimizationService : IDisposable
    {
        private bool _disposed;
        private bool _isOnBattery;
        private int _batteryPercent = 100;
        private bool _isCharging;
#pragma warning disable CS8618 // _lastStatus is set in private constructor before any read
        private PowerStatus? _lastStatus;
#pragma warning restore CS8618
        private readonly Func<PowerStatus>? _getPowerStatus;

        /// <summary>True if device runs on battery (not on power).</summary>
        public bool IsOnBattery => _isOnBattery;

        /// <summary>Aktueller Batterie-Ladezustand in percent (0–100).</summary>
        public int BatteryPercent => _batteryPercent;

        /// <summary>True if device is currently charging.</summary>
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
        /// Erzeugt Service with einfachen Werten (for Unit-Tests).
        /// </summary>
        public BatteryOptimizationService(bool isOnBattery, int batteryPercent = 100, bool isCharging = false)
        {
            _isOnBattery = isOnBattery;
            _batteryPercent = batteryPercent;
            _isCharging = isCharging;
        }

        /// <summary>
        /// Konstruktor with injizierbarem PowerStatus-Factory (for Unit-Tests).
        /// </summary>
        private BatteryOptimizationService(Func<PowerStatus> getPowerStatus)
        {
            _getPowerStatus = getPowerStatus ?? throw new ArgumentNullException(nameof(getPowerStatus));
            var status = getPowerStatus();
            _lastStatus = status;
            _isOnBattery = status.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery &&
                           status.BatteryChargeStatus != BatteryChargeStatus.Unknown &&
                           status.PowerLineStatus != PowerLineStatus.Online;
            _batteryPercent = status.BatteryLifePercent == 255 ? 100 : (int)status.BatteryLifePercent;
            _isCharging = status.BatteryChargeStatus.HasFlag(BatteryChargeStatus.Charging);
        }

        /// <summary>
        /// Checks if power status changed and fires PowerStatusChanged if so.
        /// Should be called regularly (e.g. per tick or per timer).
        /// Nutzt only den injizierten Factory-Delegaten (nicht die einfachen Werte).
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
