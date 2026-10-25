using System;
using System.Diagnostics;
using RagnaController.Core;

namespace RagnaController.Core
{
    /// <summary>
    /// Verbindet <see cref="GyroService"/> with einer <see cref="IMouseEmulationStrategy"/>:
    /// Every Tick ruft <c>GyroService.GetCursorDelta</c> ab and führt
    /// <c>MoveRelative(dx, dy)</c> aus — damit is der DualSense-Gyro
    /// zum Maus-Ersatz for Aim/Look in RO (Steam Deck Handheld).
    ///
    /// Architektur:
    ///   - GyroService liefert Pitch/Yaw Angular Velocity (deg/s)
    ///   - Adapter wandelt in Pixel-Delta um and emuliert Maus-Bewegung
    ///   - Only active if GyroService.IsEnabled == true
    ///   - Vollständig headless-testbar (no echtes HID-Device nötig)
    /// </summary>
    /// <summary>
    /// Background service for game functionality.
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

        /// <summary>Adapter active? Only then are Gyro-Delta emuliert.</summary>
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

        /// <summary>Einzelner Engine-Tick: Gyro-Delta abrufen and als Maus-Relative-Bewegung emulieren.</summary>
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

        /// <summary>Adapter zurücksetzen (z. B. nach Sleep/Resume or Profil-Wechsel).</summary>
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
