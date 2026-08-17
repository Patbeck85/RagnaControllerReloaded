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
        private readonly Channel<string> _channel;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _consumer;

        public int LogLevel { get; set; } = 1; // 0=Debug, 1=Info, 2=Warn, 3=Error

        public event Action<string>? LiveLogReceived;

        public AdvancedLogger(string path)
        {
            _path    = path;
            _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(4096)
            {
                FullMode      = BoundedChannelFullMode.DropOldest,
                SingleReader  = true,
                SingleWriter  = false
            });
            _consumer = _ = Task.Run(() => ConsumeAsync(_cts.Token));
        }

        // ── Öffentliche API ───────────────────────────────────────────────
        public void Debug(string msg)  { if (LogLevel <= 0) Enqueue($"[DBG] {msg}"); }
        public void Info(string msg)   { if (LogLevel <= 1) Enqueue($"[INF] {msg}"); }
        public void Warn(string msg)   { if (LogLevel <= 2) Enqueue($"[WRN] {msg}"); }
        public void Error(string msg)  { if (LogLevel <= 3) Enqueue($"[ERR] {msg}"); }

        private void Enqueue(string line)
        {
            string entry = $"{DateTime.Now:HH:mm:ss.fff} {line}";
            
            // FIX: Broadcast to the Developer Console if it is open
            LiveLogReceived?.Invoke(entry); 
            
            _channel.Writer.TryWrite(entry); // nie blockieren — DropOldest falls voll
        }

        // ── Consumer-Loop (einzelner Thread) ─────────────────────────────
        private async Task ConsumeAsync(CancellationToken token)
        {
            try
            {
                // FileShare.ReadWrite: mehrere App-Instanzen (Multi-Boxing) können gleichzeitig loggen
                // FIX: AutoFlush = false — Batch-Flush beim Dispose (Performance!)
                var fs = new System.IO.FileStream(_path, System.IO.FileMode.Append,
                    System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite);
                await using var writer = new StreamWriter(fs, System.Text.Encoding.UTF8)
                {
                    AutoFlush = false  // FIX: Batch-Flush für Performance und SSD-Lebensdauer
                };

                // Lese-Schleife — ohne diese wurde nie etwas in die Datei geschrieben!
                await foreach (var line in _channel.Reader.ReadAllAsync(token))
                {
                    await writer.WriteLineAsync(line);
                }
                // FIX: Batch-Flush beim Ende der Schleife (Queue ist jetzt leer)
                await writer.FlushAsync();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Logger] Consumer-Fehler: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _channel.Writer.TryComplete();
            _cts.Cancel();
            try { _consumer.Wait(TimeSpan.FromSeconds(2)); } catch { }
            _cts.Dispose();
        }
    }
}
