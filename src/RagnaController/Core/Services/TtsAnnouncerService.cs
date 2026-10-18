using System;
using System.Linq;
using System.Threading.Tasks;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// v1.7.2: TTS Announcer Service (ersetzt System.Speech with Hexa.NET.TTS or disabled)
    /// Fallback-Implementierung ohne externe Abhängigkeiten
    /// </summary>
    public sealed class TtsAnnouncerService : IDisposable
    {
        private bool _isEnabled;
        private bool _disposed;

        public TtsAnnouncerService(Settings settings)
        {
            _isEnabled = settings.EnableVoiceAnnouncements;
            // System.Speech wurde entfernt - TTS is about Text-to-Speech API or externe Bibliothek geladen
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
                    // Placeholder: TTS is about externe Bibliothek (z.B. Hexa.NET.TTS) or Text-to-Speech API geladen
                    // System.Speech wurde entfernt - implementiere hier die neue TTS-Logik
                    // Example: Hexa.NET.TTS or andere TTS-Bibliothek
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[TtsAnnouncerService] Speak failed: {ex.Message}");
                }
            });
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
