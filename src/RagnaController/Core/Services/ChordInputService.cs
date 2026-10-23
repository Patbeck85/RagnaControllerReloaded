using System;
using System.Collections.Generic;
using System.Diagnostics;
using RagnaController.Models;

namespace RagnaController.Core.Services
{
    /// <summary>
    /// Chorded Input Service — erkennt gleichzeitige Tastenkombinationen (Chords),
    /// sequentielle Eingaben (A→B→X) und Hold+Tap (kurz drücken vs. halten).
    ///
    /// Thread-Sicherheit: Nicht thread-sicher — darf nur vom Input-Thread aufgerufen werden
    /// (wie InputRouter.RouteInput).
    /// </summary>
    public class ChordInputService : IDisposable
    {
        private bool _disposed;

        // ── Konfiguration ──────────────────────────────────────────────
        private readonly List<ChordConfig> _chords = new();
        private readonly List<SequenceConfig> _sequences = new();
        private readonly List<HoldTapConfig> _holdTaps = new();

        // ── State-Tracking (per Frame) ─────────────────────────────────
        private GamepadButtonFlags _prevButtons = GamepadButtonFlags.None;
        private long _lastTick = 0;

        // ── Events ─────────────────────────────────────────────────────
        /// <summary>Feuert wenn ein Chord erkannt wurde (mit der zugehörigen Action).</summary>
        public event EventHandler<ButtonAction>? ChordFired;

        /// <summary>Feuert wenn eine Sequenz erkannt wurde (mit der zugehörigen Action).</summary>
        public event EventHandler<ButtonAction>? SequenceFired;

        /// <summary>Feuert wenn ein Tap (kurzer Druck) erkannt wurde.</summary>
        public event EventHandler<ButtonAction>? HoldTapFired;

        public ChordInputService()
        {
            // Default-Chords für DualSense/Controller (können über Settings überschrieben werden)
            AddDefaultChords();
        }

        #region Konfiguration

        public void AddChord(ChordConfig chord) => _chords.Add(chord);
        public void AddSequence(SequenceConfig seq) => _sequences.Add(seq);
        public void AddHoldTap(HoldTapConfig ht) => _holdTaps.Add(ht);

        public void ClearChords() => _chords.Clear();
        public void ClearSequences() => _sequences.Clear();
        public void ClearHoldTaps() => _holdTaps.Clear();

        private void AddDefaultChords()
        {
            // Beispiel-Defaults (können leer bleiben — User konfiguriert über UI)
            // L1+R1+A = Skill Combo (Beispiel)
            _chords.Add(new ChordConfig
            {
                Name = "L1+R1+A",
                Buttons = GamepadButtonFlags.L1 | GamepadButtonFlags.R1 | GamepadButtonFlags.BtnA,
                MaxIntervalMs = 150,
                FireOnce = true,
                Description = "L1 + R1 + A gleichzeitig → Skill Combo"
            });
        }

        #endregion

        #region Haupt-Eingabe (wird pro Frame aufgerufen)

        /// <summary>
        /// Verarbeitet einen neuen Input-Frame. Gibt true zurück wenn eine Aktion
        /// durch Chord/Sequenz/HoldTap ausgelöst wurde.
        /// </summary>
        public bool Tick(ParsedInput input, int deltaMs, out ButtonAction? firedAction)
        {
            firedAction = null;
            if (_disposed || !input.IsConnected) return false;

            var now = Environment.TickCount64;
            var elapsed = _lastTick == 0 ? 0 : now - _lastTick;
            _lastTick = now;

            var currentButtons = input.RawButtons;

            // 1. Chord-Detektion (gleichzeitige Buttons)
            foreach (var chord in _chords)
            {
                if (!chord.Enabled) continue;
                if (chord.Matches(currentButtons))
                {
                    if (chord.Action != null)
                    {
                        firedAction = chord.Action;
                        ChordFired?.Invoke(this, chord.Action);
                        return true;
                    }
                }
            }

            // 2. Sequenz-Detektion (geordnete Tasten innerhalb Zeitfenster)
            foreach (var seq in _sequences)
            {
                if (!seq.Enabled || seq.Sequence.Length == 0) continue;

                // Prüfen ob der nächste erwartete Button gedrückt wurde
                var expectedButton = seq.Sequence[seq.CurrentIndex];
                if (currentButtons.HasFlag(expectedButton) && !_prevButtons.HasFlag(expectedButton))
                {
                    // Erster Button der Sequenz: Timer starten
                    if (seq.CurrentIndex == 0)
                        seq.StartTick = now;

                    seq.CurrentIndex++;

                    // Sequenz komplett?
                    if (seq.CurrentIndex >= seq.Sequence.Length)
                    {
                        var elapsedSeq = now - seq.StartTick;
                        if (elapsedSeq <= seq.MaxIntervalMs && seq.Action != null)
                        {
                            firedAction = seq.Action;
                            SequenceFired?.Invoke(this, seq.Action);
                            seq.CurrentIndex = 0; // Reset
                            return true;
                        }
                        else
                        {
                            // Timeout → Reset
                            seq.CurrentIndex = 0;
                        }
                    }
                }
            }

            // 3. Hold+Tap-Detektion (pro Button)
            foreach (var ht in _holdTaps)
            {
                if (!ht.Enabled) continue;

                var buttonPressed = currentButtons.HasFlag(ht.Button);
                var wasPressed = _prevButtons.HasFlag(ht.Button);

                if (buttonPressed && !wasPressed)
                {
                    // Button neu gedrückt
                    ht.PressTick = now;
                    ht.HoldFired = false;
                }
                else if (buttonPressed && wasPressed && !ht.HoldFired)
                {
                    // Button wird gehalten — prüfen ob Hold-Threshold erreicht
                    var holdElapsed = now - ht.PressTick;
                    if (holdElapsed >= ht.HoldThresholdMs && ht.HoldAction != null)
                    {
                        ht.HoldFired = true;
                        firedAction = ht.HoldAction;
                        HoldTapFired?.Invoke(this, ht.HoldAction);
                        return true;
                    }
                }
                else if (!buttonPressed && wasPressed && !ht.HoldFired)
                {
                    // Button losgelassen bevor Hold-Threshold → Tap
                    var tapElapsed = now - ht.PressTick;
                    if (tapElapsed < ht.HoldThresholdMs && ht.TapAction != null)
                    {
                        firedAction = ht.TapAction;
                        HoldTapFired?.Invoke(this, ht.TapAction);
                        return true;
                    }
                }
            }

            _prevButtons = currentButtons;
            return false;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _chords.Clear();
            _sequences.Clear();
            _holdTaps.Clear();
        }

        #endregion
    }
}
