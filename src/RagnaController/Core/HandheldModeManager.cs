using System;
using RagnaController.Models;

namespace RagnaController.Core
{
    public class HandheldModeManager : IDisposable
    {
        private readonly PowerMonitor    _power;
        private readonly MageEngine      _mage;
        private readonly SnapshotBuilder _snapshot;
        private readonly GyroService     _gyro;
        private readonly CombatEngine    _combat;
        private readonly InputCommandQueue _queue;

        public bool IsEnabled { get; set; }

        public HandheldModeManager(BackgroundTickProvider? tick, SnapshotBuilder snap, MageEngine mage, OverlayRouter router, CombatEngine combat, InputCommandQueue queue)
        {
            _power    = new PowerMonitor();
            _mage     = mage;
            _snapshot = snap;
            _gyro     = new GyroService();
            _combat   = combat;
            _queue    = queue;

            _power.PowerModeChanged += (onBattery) =>
            {
                if (tick != null) tick.BatteryThrottle = onBattery;
                _snapshot.BatteryThrottle = onBattery;
            };
        }

        public void Tick(ParsedInput input, int deltaMs)
        {
            if (!IsEnabled) return;

            // FIX: Gyro aiming is now universally available for all classes while aiming a ground spell
            if (_combat.IsAimingGroundSpell && _gyro.IsAvailable)
            {
                _gyro.GetCursorDelta(deltaMs, out int dx, out int dy);
                _mage.InjectGyroDelta(dx, dy);
            }
        }

        public void Dispose() { _power.Dispose(); _gyro.Dispose(); }
    }
}
