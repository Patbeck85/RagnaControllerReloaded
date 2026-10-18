using System;
using System.Windows.Threading;
using static RagnaController.Core.NativeMethods;

namespace RagnaController.Core
{
    /// <summary>
    /// WPF-specific tick provider. Wraps <see cref="DispatcherTimer"/> with
    /// 1 ms multimedia timer precision.
    /// This is the only file in Core that references <c>System.Windows.Threading</c>.
    /// </summary>
    public sealed class WpfTickProvider : ITickProvider
    {
        private readonly DispatcherTimer _timer;
        private bool _timerPeriodSet;

        public int IntervalMs { get; }
        public event EventHandler? Tick;

        public WpfTickProvider(int intervalMs = 8)
        {
            IntervalMs = intervalMs;
            // Request 1ms scheduler resolution — reduces jitter from ±5ms to ±0.5ms
            if (timeBeginPeriod(1) == 0) _timerPeriodSet = true;

            _timer = new DispatcherTimer(DispatcherPriority.Send)
            {
                Interval = TimeSpan.FromMilliseconds(intervalMs)
            };
            _timer.Tick += (s, e) => Tick?.Invoke(s, e);
        }

        public void Start() => _timer.Start();
        public void Stop()  => _timer.Stop();

        public void Dispose()
        {
            _timer.Stop();
            if (_timerPeriodSet) { timeEndPeriod(1); _timerPeriodSet = false; }
        }
    }
}
