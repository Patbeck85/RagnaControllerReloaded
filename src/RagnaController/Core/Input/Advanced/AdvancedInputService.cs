using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RagnaController.Core.Input.Advanced {
    /// <summary>
    /// TASK-3101-3105: AdvancedInputService — Erweiterte Eingabefunktionen.
    ///
    /// Unterstützt:
    ///   • Chorded Input (gleichzeitige Tastenkombinationen, z.B. L1+R1+A)
    ///   • Sequential Chords (A→B→X innerhalb Zeitfenster)
    ///   • Hold+Tap Distinction (kurz drücken = Aktion A, halten = Aktion B)
    ///   • Analog Trigger Zones (0-30% Walk, 30-70% Run, 70-100% Sprint)
    ///   • Stick Gesture Recognition (Kreis, Zick-Zack, Tippen)
    ///
    /// White-Hat: Nur Eingabeverarbeitung, keine automatische Spielsteuerung.
    /// </summary>
    public sealed class AdvancedInputService
    {
        private readonly int _chordMaxIntervalMs;
        private readonly bool _chordedInputEnabled;

        // Chord tracking
        private readonly HashSet<GamepadButton> _currentlyPressed = new();
        private DateTime _chordStartTime;

        // Sequential chord tracking
        private readonly List<SequentialChordEntry> _sequenceBuffer = new();
        private DateTime _sequenceStartTime;

        // Hold+Tap tracking
        private readonly Dictionary<GamepadButton, DateTime> _buttonPressTimes = new();

        // Trigger zone tracking
        private float _leftTriggerValue;
        private float _rightTriggerValue;

        // Stick gesture tracking
        private readonly List<(float X, float Y, DateTime Time)> _stickHistory = new();
        private const int MaxStickHistory = 50;

        public AdvancedInputService(bool chordedInputEnabled = true, int chordMaxIntervalMs = 150)
        {
            _chordedInputEnabled = chordedInputEnabled;
            _chordMaxIntervalMs = chordMaxIntervalMs;
            _chordStartTime = DateTime.UtcNow;
            _sequenceStartTime = DateTime.UtcNow;
        }

        #region Chorded Input (TASK-3101)

        /// <summary>Registriert einen Tastendruck.</summary>
        public void ButtonDown(GamepadButton button)
        {
            if (!_chordedInputEnabled) return;
            _currentlyPressed.Add(button);
            _buttonPressTimes[button] = DateTime.UtcNow;
        }

        /// <summary>Registriert eine Tastenfreigabe.</summary>
        public void ButtonUp(GamepadButton button)
        {
            if (!_chordedInputEnabled) return;
            _currentlyPressed.Remove(button);
            // Press-Zeit behalten für Tap-Detektion
        }

        /// <summary>Prüft, ob eine bestimmte Kombination gleichzeitig gedrückt ist.</summary>
        public bool IsChordPressed(params GamepadButton[] buttons)
        {
            if (!_chordedInputEnabled) return false;
            foreach (var b in buttons)
                if (!_currentlyPressed.Contains(b))
                    return false;
            return true;
        }

        /// <summary>Prüft, ob eine Kombination innerhalb des Zeitfensters gedrückt wurde.</summary>
        public bool IsChordPressedWithinInterval(params GamepadButton[] buttons)
        {
            if (!_chordedInputEnabled) return false;
            if (!IsChordPressed(buttons)) return false;

            var elapsed = (DateTime.UtcNow - _chordStartTime).TotalMilliseconds;
            return elapsed <= _chordMaxIntervalMs;
        }

        #endregion

        #region Sequential Chords (TASK-3102)

        /// <summary>Registriert eine sequenzielle Eingabe für Chord-Erkennung.</summary>
        public void RecordSequenceInput(GamepadButton button)
        {
            if (!_chordedInputEnabled) return;

            var now = DateTime.UtcNow;
            if ((now - _sequenceStartTime).TotalMilliseconds > _chordMaxIntervalMs)
            {
                _sequenceBuffer.Clear();
                _sequenceStartTime = now;
            }

            _sequenceBuffer.Add(new SequentialChordEntry { Button = button, Time = now });
            while (_sequenceBuffer.Count > 10)
                _sequenceBuffer.RemoveAt(0);
        }

        /// <summary>Prüft, ob eine bestimmte Sequenz erkannt wurde (z.B. A→B→X).</summary>
        public bool IsSequenceDetected(params GamepadButton[] sequence)
        {
            if (!_chordedInputEnabled || sequence.Length == 0) return false;
            if (_sequenceBuffer.Count < sequence.Length) return false;

            for (int i = 0; i < sequence.Length; i++)
            {
                var idx = _sequenceBuffer.Count - sequence.Length + i;
                if (_sequenceBuffer[idx].Button != sequence[i])
                    return false;
            }
            return true;
        }

        #endregion

        #region Hold+Tap Distinction (TASK-3103)

        /// <summary>Prüft, ob eine Taste kurz getippt wurde (Tap vs Hold).</summary>
        public bool WasButtonTapped(GamepadButton button, int tapMaxMs = 200)
        {
            if (!_buttonPressTimes.TryGetValue(button, out var pressTime)) return false;
            var elapsed = (DateTime.UtcNow - pressTime).TotalMilliseconds;
            return elapsed <= tapMaxMs;
        }

        /// <summary>Prüft, ob eine Taste gehalten wird (Hold).</summary>
        public bool IsButtonHeld(GamepadButton button, int holdMinMs = 500)
        {
            if (!_buttonPressTimes.TryGetValue(button, out var pressTime)) return false;
            var elapsed = (DateTime.UtcNow - pressTime).TotalMilliseconds;
            return elapsed >= holdMinMs && _currentlyPressed.Contains(button);
        }

        #endregion

        #region Analog Trigger Zones (TASK-3104)

        /// <summary>Setzt den linken Trigger-Wert (0.0 bis 1.0).</summary>
        public void SetLeftTrigger(float value)
        {
            _leftTriggerValue = Math.Clamp(value, 0f, 1f);
        }

        /// <summary>Setzt den rechten Trigger-Wert (0.0 bis 1.0).</summary>
        public void SetRightTrigger(float value)
        {
            _rightTriggerValue = Math.Clamp(value, 0f, 1f);
        }

        /// <summary>Gibt die Trigger-Zone für den linken Trigger zurück.</summary>
        public TriggerZone GetLeftTriggerZone()
        {
            return ClassifyTriggerZone(_leftTriggerValue);
        }

        /// <summary>Gibt die Trigger-Zone für den rechten Trigger zurück.</summary>
        public TriggerZone GetRightTriggerZone()
        {
            return ClassifyTriggerZone(_rightTriggerValue);
        }

        private static TriggerZone ClassifyTriggerZone(float value)
        {
            if (value < 0.30f) return TriggerZone.Walk;
            if (value < 0.70f) return TriggerZone.Run;
            return TriggerZone.Sprint;
        }

        /// <summary>Rohwert des linken Triggers.</summary>
        public float LeftTriggerValue => _leftTriggerValue;

        /// <summary>Rohwert des rechten Triggers.</summary>
        public float RightTriggerValue => _rightTriggerValue;

        #endregion

        #region Stick Gesture Recognition (TASK-3105)

        /// <summary>Registriert einen Stick-Wert für Gesture-Erkennung.</summary>
        public void RecordStickPosition(float x, float y)
        {
            var now = DateTime.UtcNow;
            _stickHistory.Add((x, y, now));
            while (_stickHistory.Count > MaxStickHistory)
                _stickHistory.RemoveAt(0);
        }

        /// <summary>Erkennt eine Kreis-Geste.</summary>
        public bool IsCircleGestureDetected()
        {
            if (_stickHistory.Count < 10) return false;

            // Vereinfacht: Prüfe ob Stick-Bewegung alle Quadranten durchläuft
            var hasPositiveX = _stickHistory.Exists(p => p.X > 0.3f);
            var hasNegativeX = _stickHistory.Exists(p => p.X < -0.3f);
            var hasPositiveY = _stickHistory.Exists(p => p.Y > 0.3f);
            var hasNegativeY = _stickHistory.Exists(p => p.Y < -0.3f);

            return hasPositiveX && hasNegativeX && hasPositiveY && hasNegativeY;
        }

        /// <summary>Erkennt eine Zick-Zack-Geste.</summary>
        public bool IsZigZagGestureDetected()
        {
            if (_stickHistory.Count < 6) return false;

            var directionChanges = 0;
            for (int i = 2; i < _stickHistory.Count; i++)
            {
                var prevDir = GetDirection(_stickHistory[i - 2], _stickHistory[i - 1]);
                var currDir = GetDirection(_stickHistory[i - 1], _stickHistory[i]);
                if (prevDir != currDir && currDir != Direction.None)
                    directionChanges++;
            }

            return directionChanges >= 3;
        }

        private static Direction GetDirection((float X, float Y, DateTime Time) from, (float X, float Y, DateTime Time) to)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;

            if (Math.Abs(dx) < 0.1f && Math.Abs(dy) < 0.1f)
                return Direction.None;

            if (Math.Abs(dx) > Math.Abs(dy))
                return dx > 0 ? Direction.Right : Direction.Left;
            return dy > 0 ? Direction.Down : Direction.Up;
        }

        /// <summary>Erkennt einen schnellen Tip (Tap) auf dem Stick.</summary>
        public bool IsStickTapDetected(float threshold = 0.8f)
        {
            if (_stickHistory.Count < 3) return false;

            // Schnelle Bewegung von Mitte nach Außen und zurück
            var hasLargeMovement = _stickHistory.Exists(p =>
                Math.Abs(p.X) > threshold || Math.Abs(p.Y) > threshold);
            var hasReturnToCenter = _stickHistory.Exists(p =>
                Math.Abs(p.X) < 0.2f && Math.Abs(p.Y) < 0.2f);

            return hasLargeMovement && hasReturnToCenter;
        }

        #endregion

        /// <summary>Setzt den Chord-Start-Zeitpunkt zurück.</summary>
        public void ResetChordTimer()
        {
            _chordStartTime = DateTime.UtcNow;
        }

        /// <summary>Setzt die Sequenz zurück.</summary>
        public void ResetSequence()
        {
            _sequenceBuffer.Clear();
            _sequenceStartTime = DateTime.UtcNow;
        }
    }

    /// <summary>Gamepad-Buttons für AdvancedInputService.</summary>
    public enum GamepadButton
    {
        L1, R1, L2, R2, L3, R3,
        BtnA, BtnB, BtnX, BtnY,
        DPadUp, DPadDown, DPadLeft, DPadRight,
        Start, Back,
    }

    /// <summary>Trigger-Zonen für Analog-Trigger.</summary>
    public enum TriggerZone
    {
        Walk,   // 0-30%
        Run,    // 30-70%
        Sprint, // 70-100%
    }

    /// <summary>Richtungen für Stick-Gesten.</summary>
    enum Direction
    {
        None, Up, Down, Left, Right,
    }

    /// <summary>Ein Eintrag im sequentiellen Chord-Puffer.</summary>
    class SequentialChordEntry
    {
        public GamepadButton Button { get; set; }
        public DateTime Time { get; set; }
    }
}
