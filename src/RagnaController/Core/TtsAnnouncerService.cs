using System;
using System.Linq;
using System.Threading.Tasks;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// v1.7.2: TTS Announcer Service (ersetzt System.Speech mit Hexa.NET.TTS oder deaktiviert)
    /// Fallback-Implementierung ohne externe Abhängigkeiten
    /// </summary>
    public sealed class TtsAnnouncerService : IDisposable
    {
        private bool _isEnabled;
        private bool _disposed;

        public TtsAnnouncerService(Settings settings)
        {
            _isEnabled = settings.EnableVoiceAnnouncements;
            // System.Speech wurde entfernt - TTS wird über Text-to-Speech API oder externe Bibliothek geladen
        }

        public void SetEnabled(bool enable)
        {
            if (_isEnabled == enable) return;
            _isEnabled = enable;
        }

        public void Speak(string text)
        {
            if (!_isEnabled || _disposed || string.IsNullOrWhiteSpace(text)) return;

            // Fire-and-forget: Do not block the caller!
            Task.Run(() => 
            {
                try
                {
                    // Placeholder: TTS wird über externe Bibliothek (z.B. Hexa.NET.TTS) oder Text-to-Speech API geladen
                    // System.Speech wurde entfernt - implementiere hier die neue TTS-Logik
                    // Beispiel: Hexa.NET.TTS oder andere TTS-Bibliothek
                }
                catch { } // Ignore if audio device disconnects mid-speech
            });
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
