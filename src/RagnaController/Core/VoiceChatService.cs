using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RagnaController.Core
{
    public class VoiceChatService : IDisposable
    {
        private bool _isListening;
        private int _currentSessionId;
        private readonly InputCommandQueue _queue;

        // Text-Queue für Sprach-Eingaben - puffert mehrere Sätze während des Tippens
        private readonly ConcurrentQueue<string> _chatQueue = new();
        private Task? _typingTask;

        public event Action<string>? StatusChanged;

        public VoiceChatService(InputCommandQueue queue)
        {
            _queue = queue;
        }

        public void StartListening()
        {
            if (_isListening) return;

            _isListening = true;
            _currentSessionId++;
            int sessionId = _currentSessionId;

            StatusChanged?.Invoke("🎤 Listening...");

            // Queue-Task starten - verarbeitet alle Sprach-Eingaben nacheinander
            _typingTask = Task.Run(ProcessChatQueueAsync);

            // Auto-Timeout nach 8 Sekunden — bewusst fire-and-forget
            _ = Task.Run(async () => {
                try {
                    await Task.Delay(8000);
                    if (_isListening && sessionId == _currentSessionId) {
                        StopListening();
                        StatusChanged?.Invoke("Timeout - No voice detected");
                    }
                } catch { } // ObjectDisposedException beim App-Beenden ignorieren
            });
        }

        public void StopListening()
        {
            _isListening = false;
        }

        // Typing-Task, der die Queue verarbeitet - läuft parallel zum Tick-Loop
        private async Task ProcessChatQueueAsync()
        {
            while (_isListening || !_chatQueue.IsEmpty)
            {
                if (_chatQueue.TryDequeue(out string? text))
                {
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        try {
                            await _queue.SendChatString(text);
                        } catch (Exception ex) {
                            System.Diagnostics.Debug.WriteLine($"[Voice] SendChatString Fehler: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // Keine Eingabe in der Queue - kurze Pause bevor nächster Check
                    await Task.Delay(50);
                }
            }
        }

        public void Dispose()
        {
            _isListening = false;
            // Queue-Task stoppen - Task kann nicht abgebrochen werden, aber Flag wird gesetzt
            if (_typingTask != null && !_typingTask.IsCompleted)
            {
                _typingTask.Wait(100); // Kurz warten bis Task selbst stoppt
            }
        }
    }
}
