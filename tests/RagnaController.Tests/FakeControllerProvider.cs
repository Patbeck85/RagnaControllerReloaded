using System;
using System.Collections.Generic;
using RagnaController.Controller;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-015: Hardware-freie, skriptbare Implementierung von <see cref="IControllerProvider"/>.
    ///
    /// Dies ist die wiederverwendbare Test-Stelle für die Controller-Eingabe-Pipeline:
    /// Tests (und später der CI-Replay aus TEST-016) speisen deterministische Frames in die
    /// echte <see cref="ParsedInput"/>-Währung, ohne SDL/XInput/Hardware zu berühren.
    ///
    /// WICHTIG (kein Behavior-Change): Der Provider setzt bewusst KEINE Analog-/Deadzone-
    /// Normalisierung an — das ist Aufgabe der Backend-Layer (<c>InputReader</c>/SDL bzw.
    /// <c>XInputFallbackService</c>). Die Pipeline-Goldtests verifizieren, dass die
    /// <em>zugeflossenen</em> Stick-/Trigger-Werte bit-exakt durchreichen und die
    /// JustPressed/JustReleased-Transitionen exakt einmal feuern.
    /// </summary>
    public sealed class FakeControllerProvider : IControllerProvider
    {
        private struct Frame
        {
            public GamepadButtonFlags RawButtons;
            public float LeftX, LeftY, RightX, RightY;
            public float TriggerLeft, TriggerRight;
            public bool Connected; // false = Disconnect-Frame (alle Buttons cleared)
        }

        private readonly List<Frame> _frames = new();
        private int _index;
        private bool _disposed;

        /// <summary>Connected ist pro Frame definiert: true, wenn das aktuell angesprochene Frame ein Controller-Frame ist.</summary>
        public bool IsConnected => _frames.Count > 0 && CurrentIsConnected();
        public ButtonState ButtonStates { get; private set; }
        public string ControllerGuid { get; } = "FAKE0001";
        public string ControllerName { get; } = "Fake Controller";
        public string ControllerType { get; } = "fake";
        public string BatteryLevel { get; } = "100%";

        /// <summary>Number of frames currently scripted.</summary>
        public int FrameCount => _frames.Count;

        /// <summary>Current frame index (0-based, clamped to last frame when exhausted).</summary>
        public int CurrentFrameIndex => _index;

        private bool CurrentIsConnected()
            => _frames[Math.Min(_index, _frames.Count - 1)].Connected;

        /// <inheritdoc />
        public void DetectController() { /* no-op — scripted provider */ }

        /// <inheritdoc />
        public void SetRumble(float left, float right) { /* no-op */ }

        /// <inheritdoc />
        public void SetLED(byte r, byte g, byte b) { /* no-op */ }

        /// <summary>
        /// Script a frame with the given raw button mask (analog values zero).
        /// </summary>
        public FakeControllerProvider ScriptButton(GamepadButtonFlags buttons)
            => ScriptFrame(buttons, 0f, 0f, 0f, 0f, 0f, 0f);

        /// <summary>Script a frame with raw button mask plus analog stick/trigger values.</summary>
        public FakeControllerProvider ScriptFrame(GamepadButtonFlags buttons,
            float leftX = 0f, float leftY = 0f, float rightX = 0f, float rightY = 0f,
            float triggerLeft = 0f, float triggerRight = 0f)
        {
            _frames.Add(new Frame
            {
                RawButtons = buttons,
                LeftX = leftX, LeftY = leftY, RightX = rightX, RightY = rightY,
                TriggerLeft = triggerLeft, TriggerRight = triggerRight,
                Connected = true
            });
            return this;
        }

        /// <summary>Script a disconnected frame (buttons cleared, analogs zero).</summary>
        public FakeControllerProvider ScriptDisconnect()
        {
            _frames.Add(new Frame { Connected = false });
            return this;
        }

        /// <summary>
        /// Returns the <see cref="ParsedInput"/> for the current scripted frame.
        /// Advances to the last scripted frame (held) when exhausted; disconnected
        /// before any frame is scripted or after an explicit disconnect.
        /// </summary>
        public ParsedInput CurrentFrame()
        {
            if (!IsConnected || _frames.Count == 0) return ParsedInput.Disconnected;

            int i = Math.Min(_index, _frames.Count - 1);
            var f = _frames[i];
            return new ParsedInput
            {
                IsConnected = true,
                LeftX = f.LeftX, LeftY = f.LeftY, RightX = f.RightX, RightY = f.RightY,
                TriggerLeft = f.TriggerLeft, TriggerRight = f.TriggerRight,
                L1 = f.RawButtons.HasFlag(GamepadButtonFlags.L1),
                R1 = f.RawButtons.HasFlag(GamepadButtonFlags.R1),
                L2 = f.TriggerLeft > 0.15f,
                R2 = f.TriggerRight > 0.15f,
                L3 = f.RawButtons.HasFlag(GamepadButtonFlags.L3),
                R3 = f.RawButtons.HasFlag(GamepadButtonFlags.R3),
                BtnA = f.RawButtons.HasFlag(GamepadButtonFlags.BtnA),
                BtnB = f.RawButtons.HasFlag(GamepadButtonFlags.BtnB),
                BtnX = f.RawButtons.HasFlag(GamepadButtonFlags.BtnX),
                BtnY = f.RawButtons.HasFlag(GamepadButtonFlags.BtnY),
                Start = f.RawButtons.HasFlag(GamepadButtonFlags.Start),
                Back = f.RawButtons.HasFlag(GamepadButtonFlags.Back),
                DPadUp = f.RawButtons.HasFlag(GamepadButtonFlags.DPadUp),
                DPadDown = f.RawButtons.HasFlag(GamepadButtonFlags.DPadDown),
                DPadLeft = f.RawButtons.HasFlag(GamepadButtonFlags.DPadLeft),
                DPadRight = f.RawButtons.HasFlag(GamepadButtonFlags.DPadRight),
                RawButtons = f.RawButtons,
                PrevRawButtons = GamepadButtonFlags.None // frame-0 prev; pipeline chains the real prev
            };
        }

        /// <summary>Advance to the next scripted frame (clamped to last when exhausted).</summary>
        public void Advance()
        {
            if (_frames.Count > 0) _index = Math.Min(_index + 1, _frames.Count - 1);
        }

        /// <inheritdoc />
        public void Dispose() => _disposed = true;

        /// <summary>True once <see cref="Dispose"/> was called (verifies clean disposal).</summary>
        public bool IsDisposed => _disposed;
    }
}
