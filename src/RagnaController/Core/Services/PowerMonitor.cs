using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace RagnaController.Core
{
    /// <summary>
    /// Pollt Windows-Batteriestatus via GetSystemPowerStatus.
    /// Feuert PowerModeChanged wenn der Lade-Zustand wechselt (AC ↔ Batterie).
    /// </summary>
    public sealed class PowerMonitor : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_POWER_STATUS
        {
            public byte  ACLineStatus;       // 0=Batterie, 1=Netz, 255=unbekannt
            public byte  BatteryFlag;
            public byte  BatteryLifePercent; // 0–100, 255=unbekannt
            public byte  SystemStatusFlag;
            public uint  BatteryLifeTime;
            public uint  BatteryFullLifeTime;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);

        private bool _lastOnBattery;
        private CancellationTokenSource _cts = new();

        public bool IsOnBattery
        {
            get
            {
                if (!GetSystemPowerStatus(out var s)) return false;
                return s.ACLineStatus == 0;
            }
        }

        public int BatteryPercent
        {
            get
            {
                if (!GetSystemPowerStatus(out var s)) return 100;
                return s.BatteryLifePercent == 255 ? 100 : s.BatteryLifePercent;
            }
        }

        /// <summary>Feuert mit true wenn auf Batterie gewechselt, false wenn wieder am Netz.</summary>
        public event Action<bool>? PowerModeChanged;

        public PowerMonitor()
        {
            _lastOnBattery = IsOnBattery;
            // Alle 30 Sekunden prüfen — ausreichend für Netz/Batterie-Wechsel
            _ = PollLoop(_cts.Token);
        }

        private async Task PollLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try { await Task.Delay(30_000, token); } catch (TaskCanceledException) { return; }
                bool onBattery = IsOnBattery;
                if (onBattery != _lastOnBattery)
                {
                    _lastOnBattery = onBattery;
                    PowerModeChanged?.Invoke(onBattery);
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
