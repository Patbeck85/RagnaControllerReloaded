using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Animation;
using RagnaController.Models;

namespace RagnaController.Core.Services
{
    /// <summary>
    /// TASK-2803: ReducedMotionService — Deaktiviert Animationen wenn
    /// Reduzierte Bewegung bevorzugt wird (Windows-Einstellung oder App-Einstellung).
    ///
    /// Verhindert vestibuläre Reize durch schnelle Bewegungen, Blenden
    /// und pulsierende Effekte für Nutzer mit Bewegungsempfindlichkeit.
    /// </summary>
    public sealed class ReducedMotionService
    {
        private readonly Settings _settings;
        private readonly List<WeakReference<Storyboard>> _storyboards = new();
        private bool _disposed;
        private bool _reducedMotionActive;

        /// <summary>Ob der reduzierte Bewegungsmodus aktiv ist.</summary>
        public bool IsReducedMotionActive => _reducedMotionActive;

        public ReducedMotionService(Settings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _reducedMotionActive = settings.ReducedMotionEnabled ||
                AccessibilityService.IsReducedMotionPreferred;
        }

        /// <summary>Registriert ein Storyboard zur Motion-Reduktion.</summary>
        public void RegisterStoryboard(Storyboard storyboard)
        {
            if (storyboard == null) return;
            _storyboards.Add(new WeakReference<Storyboard>(storyboard));
            ApplyReducedMotion(storyboard);
        }

        /// <summary>Entfernt ein Storyboard aus der Verwaltung.</summary>
        public void UnregisterStoryboard(Storyboard storyboard)
        {
            if (storyboard == null) return;
            for (int i = _storyboards.Count - 1; i >= 0; i--)
            {
                if (_storyboards[i].TryGetTarget(out var existing) && existing == storyboard)
                {
                    _storyboards.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>Aktualisiert den Reduced-Motion-Zustand (z.B. nach Einstellungsänderung).</summary>
        public void UpdateReducedMotion(bool enabled)
        {
            _reducedMotionActive = enabled;
            foreach (var weakRef in _storyboards)
            {
                if (weakRef.TryGetTarget(out var sb))
                    ApplyReducedMotion(sb);
            }
        }

        /// <summary>Wendet den Reduced-Motion-Zustand auf ein Storyboard an.
        /// Animationen werden entweder deaktiviert (Dauer auf 0) oder sofort beendet.</summary>
        private void ApplyReducedMotion(Storyboard storyboard)
        {
            if (!_reducedMotionActive)
                return;

            // Alle Animationen im Storyboard auf Dauer 0 setzen (sofortiger Abschluss)
            // Wir können nicht direkt auf die Animationen zugreifen, aber wir können
            // den Storyboard stoppen und alle unterliegenden Animationen deaktivieren.
            try
            {
                storyboard.Stop();
            }
            catch
            {
                // Storyboard konnte nicht gestoppt werden — ignorieren
            }
        }

        /// <summary>Erstellt eine sofort-abgeschlossene Animation (Dauer 0) als Ersatz für animierte Übergänge.</summary>
        public static Duration ZeroDuration => TimeSpan.Zero;

        /// <summary>Gibt zurück, ob eine gegebene Animationsdauer angewendet werden soll
        /// oder auf 0 gesetzt werden muss.</summary>
        public Duration GetEffectiveDuration(Duration originalDuration)
        {
            if (_reducedMotionActive)
                return ZeroDuration;
            return originalDuration;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _storyboards.Clear();
            }
        }
    }
}
