using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RagnaController.Core
{
    /// <summary>
    /// Thread-sicherer Logger via System.Threading.Channels.
    /// Ein einzelner Background-Consumer schreibt sequenziell in die Datei —
    /// kein Task-Spam, kein Lock-Contention.
    /// </summary>
    public sealed class AdvancedLogger : IDisposable
    {
        private readonly string  _path;
        // Normale Logs (Debug/Info): bounded mit DropOldest → Memory-Backpressure bei Last,
        // nicht-kritische Einträge dürfen unter extremem Druck verworfen werden.
        private readonly Channel<string> _channel;
        // Kritische Logs (Warn/Error): UNBOUNDED → werden NIEMALS verworfen (TECH-024).
        // Niedriges Volumen (kein Hot-Path) → unbounded ist hier sicher und garantiert Durchkommen.
        private readonly Channel<string> _errorChannel;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _consumer;

        public int LogLevel { get; set; } = 1; // 0=Debug, 1=Info, 2=Warn, 3=Error

        public event Action<string>? LiveLogReceived;

        /// <summary>
        /// Erzeugt einen Logger. <paramref name="normalCapacity"/> ist die Größe des bounded
        /// Kanals für Debug/Info (DropOldest). Kritische Logs (Warn/Error) laufen über einen
        /// eigenen unbounded Kanal und sind von dieser Kapazität unabhängig — sie werden nie verworfen.
        /// </summary>
        public AdvancedLogger(string path, int normalCapacity = 4096)
        {
            _path         = path;
            _channel      = Channel.CreateBounded<string>(new BoundedChannelOptions(Math.Max(1, normalCapacity))
            {
                FullMode      = BoundedChannelFullMode.DropOldest,
                SingleReader  = true,
                SingleWriter  = false
            });
            _errorChannel = Channel.CreateUnbounded<string>();
            _consumer     = _ = Task.Run(() => ConsumeAsync(_cts.Token));
        }

        // ── Öffentliche API ───────────────────────────────────────────────
        public void Debug(string msg)  { if (LogLevel <= 0) Enqueue("[DBG]", msg, critical: false); }
        public void Info(string msg)   { if (LogLevel <= 1) Enqueue("[INF]", msg, critical: false); }
        public void Warn(string msg)   { if (LogLevel <= 2) Enqueue("[WRN]", msg, critical: true); }
        public void Error(string msg)  { if (LogLevel <= 3) Enqueue("[ERR]", msg, critical: true); }

        private void Enqueue(string tag, string msg, bool critical)
        {
            string entry = $"{DateTime.Now:HH:mm:ss.fff} {tag} {msg}";

            // FIX: Broadcast to the Developer Console if it is open
            LiveLogReceived?.Invoke(entry);

            // Kritische Logs → unbounded (nie blockieren, nie verwerfen).
            // Normale Logs  → bounded DropOldest (Memory-Backpressure bei Last).
            var target = critical ? _errorChannel : _channel;
            target.Writer.TryWrite(entry);
        }

        // ── Consumer-Loop (einzelner Thread, drained beide Kanäle) ─────────
        private async Task ConsumeAsync(CancellationToken token)
        {
            try
            {
                // FileShare.ReadWrite: mehrere App-Instanzen (Multi-Boxing) können gleichzeitig loggen
                var fs = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                await using var writer = new StreamWriter(fs, System.Text.Encoding.UTF8)
                {
                    AutoFlush = false  // Batch-Flush: Performance + SSD-Lebensdauer (Flush unten)
                };

                while (true)
                {
                    // Warte, bis mindestens ein Kanal Daten hat.
                    var nWait = _channel.Reader.WaitToReadAsync(token);
                    var eWait = _errorChannel.Reader.WaitToReadAsync(token);
                    await Task.WhenAny(nWait.AsTask(), eWait.AsTask());

                    // Alles Verfügbare aus BEIDEN Kanälen schreiben (pro Kanal Reihenfolge erhalten).
                    while (_channel.Reader.TryRead(out string? n))
                        await writer.WriteLineAsync(n);
                    while (_errorChannel.Reader.TryRead(out string? e))
                        await writer.WriteLineAsync(e);

                    // Beide Kanäle abgeschlossen & leer → es kommt nichts mehr; sauber beenden.
                    if (_channel.Reader.Completion.IsCompleted && _errorChannel.Reader.Completion.IsCompleted)
                        break;
                }

                // Batch-Flush am Ende (Queue ist jetzt leer).
                await writer.FlushAsync();
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown — normal cancellation
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Logger] Consumer-Fehler: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _channel.Writer.TryComplete();
            _errorChannel.Writer.TryComplete();
            _cts.Cancel();
            try { _consumer.Wait(TimeSpan.FromSeconds(2)); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Logger] Dispose wait failed: {ex.Message}");
            }
            _cts.Dispose();

            // Unsubscribe event to prevent memory leaks
            LiveLogReceived = null;
        }
    }
}
