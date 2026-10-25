using System;
using System.Collections.Generic;

namespace RagnaController.Core.Developer {
    /// <summary>
    /// TASK-2904: InputLatencyVisualizer — Echtzeit-Messung der Input-Latenz.
    ///
    /// Misst die Zeit von Eingabe (Controller/Keyboard) über Engine bis zum
    /// SendInput-Aufruf und stellt die Daten für UI-Visualisierung bereit.
    /// </summary>
    public sealed class InputLatencyVisualizer
    {
        private readonly Queue<long> _latencySamples = new();
        private int _maxSamples = 100;
        private DateTime? _lastInputTime;

        /// <summary>Maximale Anzahl gespeicherter Samples.</summary>
        public int MaxSamples
        {
            get => _maxSamples;
            set => _maxSamples = value;
        }

        /// <summary>Aktuelle Anzahl Samples.</summary>
        public int SampleCount => _latencySamples.Count;

        /// <summary>Letzte gemessene Latenz in Millisekunden.</summary>
        public double? LastLatencyMs => _latencySamples.Count > 0 ? (double)_latencySamples.Peek() : (double?)null;

        /// <summary>Durchschnittliche Latenz in Millisekunden.</summary>
        public double AverageLatencyMs
        {
            get
            {
                if (_latencySamples.Count == 0) return 0;
                double sum = 0;
                foreach (var s in _latencySamples) sum += s;
                return sum / _latencySamples.Count;
            }
        }

        /// <summary>Maximale Latenz in Millisekunden.</summary>
        public double MaxLatencyMs
        {
            get
            {
                if (_latencySamples.Count == 0) return 0;
                double max = 0;
                foreach (var s in _latencySamples)
                    if (s > max) max = s;
                return max;
            }
        }

        /// <summary>Minimale Latenz in Millisekunden.</summary>
        public double MinLatencyMs
        {
            get
            {
                if (_latencySamples.Count == 0) return 0;
                long min = long.MaxValue;
                foreach (var s in _latencySamples)
                    if (s < min) min = s;
                return min == long.MaxValue ? 0 : min;
            }
        }

        /// <summary>Registriert den Zeitpunkt eines Eingabe-Events (Start der Latenzmessung).</summary>
        public void MarkInputReceived()
        {
            _lastInputTime = DateTime.UtcNow;
        }

        /// <summary>Beendet die Latenzmessung für den letzten Input (Ende der Kette).</summary>
        public void MarkInputProcessed()
        {
            if (!_lastInputTime.HasValue) return;

            var elapsed = (long)(DateTime.UtcNow - _lastInputTime.Value).TotalMilliseconds;
            _lastInputTime = null;

            _latencySamples.Enqueue(elapsed);
            while (_latencySamples.Count > _maxSamples)
                _latencySamples.Dequeue();
        }

        /// <summary>Setzt alle Samples zurück.</summary>
        public void Reset()
        {
            _latencySamples.Clear();
            _lastInputTime = null;
        }
    }
}
