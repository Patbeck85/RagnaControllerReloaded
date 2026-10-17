using System;
using System.Diagnostics;

namespace RagnaController.Core
{
    /// <summary>
    /// Verbindet <see cref="GyroService"/> mit einer <see cref="IMouseEmulationStrategy"/>:
    /// Jeder Tick ruft <c>GyroService.GetCursorDelta</c> ab und führt
    /// <c>MoveRelative(dx, dy)</c> aus — damit wird der DualSense-Gyro
    /// zum Maus-Ersatz für Aim/Look in RO (Steam Deck Handheld).
    ///
    /// Architektur:
    ///   - GyroService liefert Pitch/Yaw Angular Velocity (deg/s)
    ///   - Adapter wandelt in Pixel-Delta um und emuliert Maus-Bewegung
    ///   - Nur aktiv wenn GyroService.IsEnabled == true
    ///   - Vollständig headless-testbar (kein echtes HID-Device nötig)
    /// </summary>
    public sealed class GyroCursorAdapter : IDisposable
    {
        private readonly GyroService _gyro;
        private readonly IMouseEmulationStrategy _mouse;
        private bool _disposed;
        private bool _enabled;

        public GyroCursorAdapter(GyroService gyro, IMouseEmulationStrategy mouse)
        {
            _gyro  = gyro  ?? throw new ArgumentNullException(nameof(gyro));
            _mouse = mouse ?? throw new ArgumentNullException(nameof(mouse));
        }

        /// <summary>Adapter aktiv? Nur dann werden Gyro-Delta emuliert.</summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_disposed) return;
                _enabled = value;
                if (!_enabled)
                {
                    _gyro.IsEnabled = false;
                }
            }
        }

        /// <summary>Einzelner Engine-Tick: Gyro-Delta abrufen und als Maus-Relative-Bewegung emulieren.</summary>
        /// <param name="tickMs">Engine-Tick-Dauer in Millisekunden (z. B. 16 @ 60 Hz).</param>
        public void Tick(int tickMs)
        {
            if (_disposed || !Enabled || tickMs <= 0) return;

            _gyro.IsEnabled = true;
            _gyro.GetCursorDelta(tickMs, out var dx, out var dy);

            if (dx != 0 || dy != 0)
            {
                _mouse.MoveRelative(dx, dy);
                Debug.WriteLine($"[GyroCursorAdapter] dx={dx}, dy={dy} (tickMs={tickMs})");
            }
        }

        /// <summary>Adapter zurücksetzen (z. B. nach Sleep/Resume oder Profil-Wechsel).</summary>
        public void Reset()
        {
            if (_disposed) return;
            _gyro.Reset();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Enabled = false;
        }
    }
}
