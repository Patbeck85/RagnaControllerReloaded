using System;
using System.Diagnostics;
using System.Threading;

namespace RagnaController.Core
{
    /// <summary>
    /// Haptic Feedback (Rumble) für Steam Deck / Handheld.
    /// Spielt vordefinierte Vibration-Patterns ab:
    ///   - Hit:         kurzer Einzelimpuls (ca. 100ms)
    ///   - Death:       langer Einzelimpuls (ca. 500ms)
    ///   - BuffExpired: doppelter kurzer Impuls (2×100ms, 100ms Pause)
    ///   - SkillCast:   mittlerer Einzelimpuls (ca. 250ms)
    ///
    /// Architektur:
    ///   - IRumbleProvider ist injizierbar → unit-testbar ohne echte Hardware
    ///   - Patterns laufen auf einem separaten Timer-Thread (nicht blockierend)
    ///   - Nur ein Pattern gleichzeitig (neues bricht altes ab)
    /// </summary>
    public sealed class HapticFeedbackService : IDisposable
    {
        private readonly IRumbleProvider _rumble;
        private readonly object _lock = new();
        private bool _disposed;
        private bool _playing;
        private Timer? _patternTimer;
        private int _patternStep;
        private int[] _patternDurationsMs = Array.Empty<int>();
        private float _patternLeft = 0f;
        private float _patternRight = 0f;

        /// <summary>Rumble-Provider (XInputFallbackService in Production, Mock in Tests).</summary>
        public IRumbleProvider Rumble => _rumble;

        public HapticFeedbackService(IRumbleProvider rumble)
        {
            _rumble = rumble ?? throw new ArgumentNullException(nameof(rumble));
        }

        #region Public Pattern API

        /// <summary>Kurzer Einzelimpuls — Treffer / Impact.</summary>
        public void PlayHit()
        {
            PlayPattern(new[] { 100 }, 0.8f, 0.4f);
        }

        /// <summary>Langer Einzelimpuls — Charakter-Tod.</summary>
        public void PlayDeath()
        {
            PlayPattern(new[] { 500 }, 1.0f, 1.0f);
        }

        /// <summary>Doppelter kurzer Impuls — Buff abgelaufen.</summary>
        public void PlayBuffExpired()
        {
            PlayPattern(new[] { 100, 100, 100 }, 0.6f, 0.6f); // On-Off-On
        }

        /// <summary>Mittlerer Einzelimpuls — Skill-Cast.</summary>
        public void PlaySkillCast()
        {
            PlayPattern(new[] { 250 }, 0.5f, 0.5f);
        }

        /// <summary>Custom Pattern: durationsMs abwechselnd an/aus, beide Motoren mit gleicher Intensität.</summary>
        public void PlayCustom(int[] durationsMs, float intensity)
        {
            PlayPattern(durationsMs, intensity, intensity);
        }

        /// <summary>Alle Vibration sofort stoppen.</summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (!_playing) return;
                _playing = false;
                _patternTimer?.Dispose();
                _patternTimer = null;
                _patternStep = 0;
                _patternDurationsMs = Array.Empty<int>();
            }
            _rumble.SetRumble(0f, 0f);
        }

        #endregion

        #region Pattern Engine

        private void PlayPattern(int[] durationsMs, float left, float right)
        {
            if (_disposed || durationsMs == null || durationsMs.Length == 0) return;

            lock (_lock)
            {
                Stop(); // Laufendes Pattern abbrechen

                _patternDurationsMs = durationsMs;
                _patternLeft = left;
                _patternRight = right;
                _patternStep = 0;
                _playing = true;
            }

            _patternTimer = new Timer(PatternTick, null, 0, Timeout.Infinite);
        }

        private void PatternTick(object? state)
        {
            if (_disposed) return;

            lock (_lock)
            {
                if (!_playing || _patternStep >= _patternDurationsMs.Length)
                {
                    _playing = false;
                    _patternTimer?.Dispose();
                    _patternTimer = null;
                    _patternDurationsMs = Array.Empty<int>();
                    _rumble.SetRumble(0f, 0f);
                    return;
                }
            }

            int duration = _patternDurationsMs[_patternStep];

            if (_patternStep % 2 == 0)
            {
                // An-Phase: Rumble aktiv
                _rumble.SetRumble(_patternLeft, _patternRight);
            }
            else
            {
                // Aus-Phase: Rumble aus
                _rumble.SetRumble(0f, 0f);
            }

            _patternStep++;

            // Nächsten Tick nach dieser Dauer planen
            _patternTimer?.Change(duration, Timeout.Infinite);
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }

    /// <summary>
    /// Abstraktion über Rumble/Hardware-Vibration.
    /// Production: XInputFallbackService. Tests: Mock.
    /// </summary>
    public interface IRumbleProvider
    {
        /// <summary>Linke und rechte Motor-Geschwindigkeit (0.0–1.0).</summary>
        void SetRumble(float left, float right);
    }
}
