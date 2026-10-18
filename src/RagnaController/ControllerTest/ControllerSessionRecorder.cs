using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using RagnaController.Controller;
using RagnaController.Models;

namespace RagnaController.ControllerTest
{
    /// <summary>
    /// Ein einzelnes aufgenommener Hardware-Frame im exakten Format des CI-Replay-Fixtures
    /// (tests/RagnaController.Tests/fixtures/controller_session_001.json). Buttons is die
    /// GamepadButtonFlags-Bitmasks; lx/ly/rx/ry/lt/rt are ROH-Werte (no Deadzone, keine
    /// Normalisierung — das is Aufgabe der Backend-Layer InputReader/XInputFallbackService).
    /// </summary>
    public readonly record struct RecordingSample(
        uint Buttons, float Lx, float Ly, float Rx, float Ry, float Lt, float Rt, bool Connected)
    {
        private const int BtnA = 1 << 0, BtnB = 1 << 1, BtnX = 1 << 2, BtnY = 1 << 3;
        private const int L1 = 1 << 4, R1 = 1 << 5, L3 = 1 << 8, R3 = 1 << 9;
        private const int DPadUp = 1 << 10, DPadDown = 1 << 11, DPadLeft = 1 << 12, DPadRight = 1 << 13;
        private const int Start = 1 << 14, Back = 1 << 15;

        /// <summary>
        /// Baut ein Sample aus dem vereinheitlichten ButtonState. The Bitmask entspricht EXAKT der
        /// RawButtons-Maske der Produktion (Core/InputReader.Read): digitale Buttons + L1/R1/L3/R3/
        /// DPad/Start/Back; L2/R2 are analoge Trigger and NICHT Teil der Mask.
        /// </summary>
        public static RecordingSample FromButtonState(
            ButtonState s, float lx, float ly, float rx, float ry, float lt, float rt)
        {
            int mask = 0;
            if (s.APressed) mask |= BtnA;
            if (s.BPressed) mask |= BtnB;
            if (s.XPressed) mask |= BtnX;
            if (s.YPressed) mask |= BtnY;
            if (s.L1Pressed) mask |= L1;
            if (s.R1Pressed) mask |= R1;
            if (s.L3Pressed) mask |= L3;
            if (s.R3Pressed) mask |= R3;
            if (s.DPadUp) mask |= DPadUp;
            if (s.DPadDown) mask |= DPadDown;
            if (s.DPadLeft) mask |= DPadLeft;
            if (s.DPadRight) mask |= DPadRight;
            if (s.StartPressed) mask |= Start;
            if (s.BackPressed) mask |= Back;
            return new RecordingSample((uint)mask, lx, ly, rx, ry, lt, rt, true);
        }

        /// <summary>Checks whether the named button bit is set (for Diagnose/Replay). The names
        /// correspond to <see cref="ButtonState"/>; L2/R2 are analog triggers and NOT part of the mask.
        /// Single Source of Truth for button bits — no copy in other classes.
        /// </summary>
        /// <param name="name">Button name.</param>
        /// <returns>True if the button is pressed.</returns>
        public bool HasButton(string name) => name switch
        {
            "A"         => (Buttons & BtnA) != 0,
            "B"         => (Buttons & BtnB) != 0,
            "X"         => (Buttons & BtnX) != 0,
            "Y"         => (Buttons & BtnY) != 0,
            "L1"        => (Buttons & L1) != 0,
            "R1"        => (Buttons & R1) != 0,
            "L3"        => (Buttons & L3) != 0,
            "R3"        => (Buttons & R3) != 0,
            "DPadUp"    => (Buttons & DPadUp) != 0,
            "DPadDown"  => (Buttons & DPadDown) != 0,
            "DPadLeft"  => (Buttons & DPadLeft) != 0,
            "DPadRight" => (Buttons & DPadRight) != 0,
            "Start"     => (Buttons & Start) != 0,
            "Back"      => (Buttons & Back) != 0,
            _           => false
        };

        /// <summary>Disconnect-Frame: all Buttons cleared, Analogwerte null, connected=false.</summary>
        public static readonly RecordingSample Disconnected =
            new(0, 0f, 0f, 0f, 0f, 0f, 0f, false);
    }

    /// <summary>
    /// TEST-016: Recording function for controller sessions (ControllerTestWindow).
    ///
    /// Nimmt pro Tick einen <see cref="RecordingSample"/> on and speichert die komplette Sitzung als
    /// JSON im EXAKTEN Format der CI-Replay-Fixtures (fixtures/controller_session_001.json), sodass
    /// a real recording can be used directly as replay golden master playback. Unit-testbar ohne
    /// UI/SDL: Samples are injiziert, das JSON is deterministisch erzeugt.
    /// </summary>
    /// <summary>
    /// Test harness class for controller validation.
    /// </summary>
    public sealed class ControllerSessionRecorder
    {
        private readonly List<RecordingSample> _frames = new();

        /// <summary>Number of recorded frames.</summary>
        public int FrameCount => _frames.Count;

        /// <summary>Is recording currently running.</summary>
        public bool IsRecording { get; private set; }

        /// <summary>Starts recording (starts with empty frame list).</summary>
        public void Start()
        {
            _frames.Clear();
            IsRecording = true;
        }

        /// <summary>Stops recording. After this <see cref="Save"/> is possible.</summary>
        public void Stop() => IsRecording = false;

        /// <summary>Takes a frame (only if recording is running).</summary>
        public void RecordFrame(RecordingSample sample)
        {
            if (!IsRecording) return;
            _frames.Add(sample);
        }

        /// <summary>
        /// Writes the session as JSON in fixture format to <paramref name="path"/>.
        /// Floats are written with InvariantCulture (dot as decimal separator).
        /// </summary>
        public void Save(string path)
        {
            using var ms = new MemoryStream();
            using (var w = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true }))
            {
                w.WriteStartObject();
                w.WriteString("id", $"session-{DateTime.Now:yyyyMMdd-HHmmss}");
                w.WriteString("description",
                    "Recorded controller session from ControllerTestWindow (RagnaController). " +
                    "Buttons are GamepadButtonFlags bitmasks. Analog values pass through raw (NO " +
                    "deadzone/normalization — that's the backend layer InputReader/" +
                    "XInputFallbackService responsibility and outside the replay scope).");
                w.WriteStartArray("frames");
                foreach (var f in _frames)
                {
                    w.WriteStartObject();
                    w.WriteNumber("buttons", (long)f.Buttons);
                    WriteFloat(w, "lx", f.Lx);
                    WriteFloat(w, "ly", f.Ly);
                    WriteFloat(w, "rx", f.Rx);
                    WriteFloat(w, "ry", f.Ry);
                    WriteFloat(w, "lt", f.Lt);
                    WriteFloat(w, "rt", f.Rt);
                    w.WriteBoolean("connected", f.Connected);
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                w.WriteEndObject();
            }

            string dir = Path.GetDirectoryName(path) ?? "";
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, ms.ToArray());
        }

        private static void WriteFloat(Utf8JsonWriter w, string name, float v)
            => w.WriteNumber(name, v);
    }
}
