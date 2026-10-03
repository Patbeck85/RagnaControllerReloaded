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
    /// (tests/RagnaController.Tests/fixtures/controller_session_001.json). Buttons ist die
    /// GamepadButtonFlags-Bitmasks; lx/ly/rx/ry/lt/rt sind ROH-Werte (kein Deadzone, keine
    /// Normalisierung — das ist Aufgabe der Backend-Layer InputReader/XInputFallbackService).
    /// </summary>
    public readonly record struct RecordingSample(
        uint Buttons, float Lx, float Ly, float Rx, float Ry, float Lt, float Rt, bool Connected)
    {
        private const int BtnA = 1 << 0, BtnB = 1 << 1, BtnX = 1 << 2, BtnY = 1 << 3;
        private const int L1 = 1 << 4, R1 = 1 << 5, L3 = 1 << 8, R3 = 1 << 9;
        private const int DPadUp = 1 << 10, DPadDown = 1 << 11, DPadLeft = 1 << 12, DPadRight = 1 << 13;
        private const int Start = 1 << 14, Back = 1 << 15;

        /// <summary>
        /// Baut ein Sample aus dem vereinheitlichten ButtonState. Die Bitmask entspricht EXAKT der
        /// RawButtons-Maske der Produktion (Core/InputReader.Read): digitale Buttons + L1/R1/L3/R3/
        /// DPad/Start/Back; L2/R2 sind analoge Trigger und NICHT Teil der Mask.
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

        /// <summary>
        /// Prüft, ob das benannte Buttons-Bit gesetzt ist (für Diagnose/Replay). Die Namen
        /// entsprechen <see cref="ButtonState"/>; L2/R2 sind analoge Trigger und NICHT Teil der Mask.
        /// Single Source of Truth für die Buttons-Bits — keine Kopie in anderen Klassen.
        /// </summary>
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

        /// <summary>Disconnect-Frame: alle Buttons cleared, Analogwerte null, connected=false.</summary>
        public static readonly RecordingSample Disconnected =
            new(0, 0f, 0f, 0f, 0f, 0f, 0f, false);
    }

    /// <summary>
    /// TEST-016: Aufnahmefunktion für Controller-Sitzungen (ControllerTestWindow).
    ///
    /// Nimmt pro Tick einen <see cref="RecordingSample"/> auf und speichert die komplette Sitzung als
    /// JSON im EXAKTEN Format der CI-Replay-Fixtures (fixtures/controller_session_001.json), sodass
    /// eine reale Aufnahme direkt als Replay-Golden-Master-Wiedergabe nutzbar ist. Unit-testbar ohne
    /// UI/SDL: Samples werden injiziert, das JSON wird deterministisch erzeugt.
    /// </summary>
    public sealed class ControllerSessionRecorder
    {
        private readonly List<RecordingSample> _frames = new();

        /// <summary>Anzahl der aufgenommenen Frames.</summary>
        public int FrameCount => _frames.Count;

        /// <summary>Ob aktuell eine Aufnahme läuft.</summary>
        public bool IsRecording { get; private set; }

        /// <summary>Nimmt die Aufnahme auf (startet mit leerem Frame-Bestand).</summary>
        public void Start()
        {
            _frames.Clear();
            IsRecording = true;
        }

        /// <summary>Beendet die Aufnahme. Danach ist <see cref="Save"/> möglich.</summary>
        public void Stop() => IsRecording = false;

        /// <summary>Nimmt einen Frame auf (nur wenn eine Aufnahme läuft).</summary>
        public void RecordFrame(RecordingSample sample)
        {
            if (!IsRecording) return;
            _frames.Add(sample);
        }

        /// <summary>
        /// Schreibt die Sitzung als JSON im Fixture-Format nach <paramref name="path"/>.
        /// Floats werden mit InvariantCulture geschrieben (Punkt als Dezimaltrenner).
        /// </summary>
        public void Save(string path)
        {
            using var ms = new MemoryStream();
            using (var w = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true }))
            {
                w.WriteStartObject();
                w.WriteString("id", $"session-{DateTime.Now:yyyyMMdd-HHmmss}");
                w.WriteString("description",
                    "Aufgezeichnete Controller-Sitzung aus dem ControllerTestWindow (RagnaController). " +
                    "Buttons sind GamepadButtonFlags-Bitmasks. Analogwerte reichen roh durch (KEIN " +
                    "Deadzone/Normalisierung — das ist Aufgabe der Backend-Layer InputReader/" +
                    "XInputFallbackService und liegt außerhalb des Replay-Skops).");
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
