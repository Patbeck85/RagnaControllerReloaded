using System;
using System.Threading;
using System.Threading.Tasks;

namespace RagnaController.Core
{
    public sealed class BackgroundTickProvider : ITickProvider
    {
        public int  IntervalMs      { get; }
        public bool BatteryThrottle { get; set; }

        public event EventHandler? Tick;

        private CancellationTokenSource? _cts;
        private Task? _loop;

        public BackgroundTickProvider(int intervalMs = 8) => IntervalMs = intervalMs;

        public void Start()
        {
            if (_loop != null) return;
            NativeMethods.timeBeginPeriod(1); // Windows-Scheduler auf 1ms Präzision
            _cts  = new CancellationTokenSource();
            var cts = _cts;
            _loop = _ = Task.Run(() => RunLoop(cts.Token));
        }

        public void Stop()
        {
            // ROB-001: Loop deterministisch beenden BEVOR State nullt wird — sonst kann ein
            // Watchdog-Restart (Stop→Start) zwei parallele Tick-Loops erzeugen.
            var cts = _cts;
            var loop = _loop;

            cts?.Cancel();
            try
            {
                // Bounded wait: Loop läuft nur Task.Delay/PeriodicTimer, bricht prompt ab.
                if (loop != null) loop.Wait(500);
            }
            catch (AggregateException)
            {
                // Loop fängt intern alle Exceptions; Abbruch-Exceptions sind erwartet.
            }

            cts?.Dispose(); // erst NACH dem Wait: Token wird nicht mehr gebraucht
            _cts = null;
            NativeMethods.timeEndPeriod(1);
            _loop = null;
        }

        private async Task RunLoop(CancellationToken token)
        {
            // Timer läuft immer mit dem Basis-Intervall.
            // BatteryThrottle überspringt jeden zweiten Tick intern —
            // so ändert sich das Throttling zur Laufzeit ohne Timer-Neustart.
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(IntervalMs));
            try
            {
                while (await timer.WaitForNextTickAsync(token))
                {
                    if (BatteryThrottle)
                    {
                        // Tick verschlucken → effektiv 2× Intervall
                        await timer.WaitForNextTickAsync(token);
                    }

                    Tick?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (OperationCanceledException)
                        {
                            // Expected on shutdown — normal cancellation
                        }
                        catch (Exception ex)
                        {
                            // CRITICAL: Alle Exceptions fangen, damit ein einzelner Bug in einem Makro oder Profil
                            // niemals den kompletten 125Hz-Thread tötet. Loggen und weiterlaufen.
                            System.Diagnostics.Debug.WriteLine($"[BackgroundTickProvider] Exception im Tick-Loop: {ex.GetType().Name}: {ex.Message}");
                        }
        }

        public void Dispose() => Stop();
    }
}
