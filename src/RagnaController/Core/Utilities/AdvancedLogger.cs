using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RagnaController.Core
{
    /// <summary>
    /// Thread-sicherer Logger via System.Threading.Channels.
    /// Ein einzelner Backgroand-Consumer schreibt sequenziell in die Datei —
    /// no Task-Spam, no Lock-Contention.
    /// </summary>
    public sealed class AdvancedLogger : IDisposable
    {
        private readonly string _path;
        private readonly string _logDirectory;
        private readonly string _logFileName;
        // Normale Logs (Debug/Info): bounded with DropOldest → Memory-Backpressure bei Last,
        // nicht-kritische Einträge dürfen unter extremem Druck verworfen are.
        private readonly Channel<string> _channel;
        // Kritische Logs (Warn/Error): UNBOUNDED → are NIEMALS verworfen (TECH-024).
        // Niedriges Volumen (no Hot-Path) → unbounded is hier sicher and garantiert Durchkommen.
        private readonly Channel<string> _errorChannel;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _consumer;

        // ── TECH-066: Log Rotation ─────────────────────────────────────────
        private const long MaxLogSizeBytes = 10L * 1024 * 1024; // 10 MB
        private const int MaxRotatedFiles = 5;
        private StreamWriter? _writer;

        public int LogLevel { get; set; } = 1; // 0=Debug, 1=Info, 2=Warn, 3=Error

        public event Action<string>? LiveLogReceived;

        /// <summary>
        /// Erzeugt einen Logger. <paramref name="normalCapacity"/> is die Größe des bounded
        /// Kanals for Debug/Info (DropOldest). Kritische Logs (Warn/Error) laufen about einen
        /// eigenen unbounded Kanal and are from this Kapazität unabhängig — they are nie verworfen.
        /// </summary>
        public AdvancedLogger(string path, int normalCapacity = 4096)
        {
            _path = path;
            _logDirectory = Path.GetDirectoryName(path) ?? ".";
            _logFileName = Path.GetFileName(path);
            _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(Math.Max(1, normalCapacity))
            {
                FullMode      = BoundedChannelFullMode.DropOldest,
                SingleReader  = true,
                SingleWriter  = false
            });
            _errorChannel = Channel.CreateUnbounded<string>();
            _consumer     = _ = Task.Run(() => ConsumeAsync(_cts.Token));
        }

        // ── Öffentliche API ───────────────────────────────────────────────
        /// <summary>
        /// Debug method.
        /// <param name="msg">msg parameter.</param>
        /// </summary>
        public void Debug(string msg)  { if (LogLevel <= 0) Enqueue("[DBG]", msg, critical: false); }
        /// <summary>
        /// Info method.
        /// <param name="msg">msg parameter.</param>
        /// </summary>
        public void Info(string msg)   { if (LogLevel <= 1) Enqueue("[INF]", msg, critical: false); }
        /// <summary>
        /// Warn method.
        /// <param name="msg">msg parameter.</param>
        /// </summary>
        public void Warn(string msg)   { if (LogLevel <= 2) Enqueue("[WRN]", msg, critical: true); }
        /// <summary>
        /// Error method.
        /// <param name="msg">msg parameter.</param>
        /// </summary>
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

        // ── TECH-066: Log Rotation ──────────────────────────────────────
        private void EnsureWriter()
        {
            if (_writer != null) return;
            Directory.CreateDirectory(_logDirectory);
            var fs = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _writer = new StreamWriter(fs, System.Text.Encoding.UTF8) { AutoFlush = false };
        }

        private void RotateIfNeeded()
        {
            try
            {
                if (!File.Exists(_path)) return;
                var info = new FileInfo(_path);
                if (info.Length < MaxLogSizeBytes) return;

                // Schließe aktuellen Writer
                _writer?.Flush();
                _writer?.Dispose();
                _writer = null;

                // Rotierte Dateien verschieben: .log.4 → .log.5 (löschen), .log.3 → .log.4, etc.
                for (int i = MaxRotatedFiles - 1; i >= 1; i--)
                {
                    string from = Path.Combine(_logDirectory, $"{_logFileName}.{i}");
                    string to = Path.Combine(_logDirectory, $"{_logFileName}.{i + 1}");
                    if (File.Exists(from))
                    {
                        if (i == MaxRotatedFiles - 1)
                            File.Delete(from); // älteste löschen
                        else
                            File.Move(from, to);
                    }
                }

                // Aktuelle Datei to .log.1 verschieben
                string rotated = Path.Combine(_logDirectory, $"{_logFileName}.1");
                if (File.Exists(rotated)) File.Delete(rotated);
                File.Move(_path, rotated);

                // Neuer Writer is beim nächsten EnsureWriter() erstellt
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Logger] Rotation failed: {ex.Message}");
            }
        }

        // ── Consumer-Loop (einzelner Thread, drained beide Kanäle) ─────────
        private async Task ConsumeAsync(CancellationToken token)
        {
            try
            {
                EnsureWriter();

                while (true)
                {
                    // TECH-066: Prüfe Rotation vor jedem Schreibzyklus
                    RotateIfNeeded();
                    EnsureWriter();

                    // Warte, bis mindestens ein Kanal Daten has.
                    var nWait = _channel.Reader.WaitToReadAsync(token);
                    var eWait = _errorChannel.Reader.WaitToReadAsync(token);
                    await Task.WhenAny(nWait.AsTask(), eWait.AsTask());

                    // Alles Verfügbare aus BEIDEN Kanälen schreiben (pro Kanal Reihenfolge erhalten).
                    while (_channel.Reader.TryRead(out string? n))
                        await _writer!.WriteLineAsync(n);
                    while (_errorChannel.Reader.TryRead(out string? e))
                        await _writer!.WriteLineAsync(e);

                    await _writer!.FlushAsync();

                    // Beide Kanäle abgeschlossen & leer → es kommt nothing mehr; sauber beenden.
                    if (_channel.Reader.Completion.IsCompleted && _errorChannel.Reader.Completion.IsCompleted)
                        break;
                }

                // Batch-Flush am Ende (Queue is jetzt leer).
                await _writer!.FlushAsync();
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown — normal cancellation
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Logger] Consumer-Fehler: {ex.Message}");
            }
            finally
            {
                _writer?.Dispose();
                _writer = null;
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

            _writer?.Dispose();
            _writer = null;

            // Unsubscribe event to prevent memory leaks
            LiveLogReceived = null;
        }
    }
}
