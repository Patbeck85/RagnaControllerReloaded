using System;
using System.IO;
using System.Text.Json;
using RagnaController.Controller;
using RagnaController.ControllerTest;
using Xunit;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-016: Unit-Tests für die ControllerSessionRecorder (Aufnahmefunktion im
    /// ControllerTestWindow). Verifiziert, dass das erzeugte JSON exakt das Format der
    /// CI-Replay-Fixtures hat und die ButtonState→Mask-Mapping korrekt ist.
    /// </summary>
    public class ControllerSessionRecorderTests : IDisposable
    {
        private readonly string _tmpDir;

        public ControllerSessionRecorderTests()
            => _tmpDir = Path.Combine(Path.GetTempPath(), "rc_recorder_tests_" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(_tmpDir)) Directory.Delete(_tmpDir, true);
        }

        private string NewFile() => Path.Combine(_tmpDir, $"session_{Guid.NewGuid():N}.json");

        [Fact]
        public void Recorder_Start_Record_Stop_Saves_FixtureFormatJson()
        {
            var rec = new ControllerSessionRecorder();
            Assert.False(rec.IsRecording);
            Assert.Equal(0, rec.FrameCount);

            string path = NewFile();
            rec.Start();
            Assert.True(rec.IsRecording);

            // 3 Frames: A gedrückt (Mask=1), dann B|X (Mask=5), dann Disconnect.
            rec.RecordFrame(new RecordingSample(1u, 0.25f, -0.5f, 0f, 0f, 0f, 0f, true));
            rec.RecordFrame(new RecordingSample(5u, 0f, 0f, 0.5f, 0f, 0.1875f, 0f, true));
            rec.RecordFrame(RecordingSample.Disconnected);
            Assert.Equal(3, rec.FrameCount);

            // RecordFrame nach Stop wird ignoriert.
            rec.Stop();
            Assert.False(rec.IsRecording);
            rec.RecordFrame(new RecordingSample(1u, 0, 0, 0, 0, 0, 0, true));
            Assert.Equal(3, rec.FrameCount);

            rec.Save(path);

            // JSON im Fixture-Format validieren.
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("id", out _));
            Assert.True(root.TryGetProperty("description", out _));
            Assert.True(root.TryGetProperty("frames", out var frames));
            Assert.Equal(3, frames.GetArrayLength());

            var f0 = frames[0];
            Assert.Equal(1L, f0.GetProperty("buttons").GetInt64());
            Assert.Equal(0.25f, f0.GetProperty("lx").GetSingle(), 3);
            Assert.Equal(-0.5f, f0.GetProperty("ly").GetSingle(), 3);
            Assert.True(f0.GetProperty("connected").GetBoolean());

            var f1 = frames[1];
            Assert.Equal(5L, f1.GetProperty("buttons").GetInt64());
            Assert.Equal(0.1875f, f1.GetProperty("lt").GetSingle(), 3);

            var f2 = frames[2];
            Assert.False(f2.GetProperty("connected").GetBoolean());
        }

        [Fact]
        public void FromButtonState_Builds_Correct_Mask()
        {
            // A=1, B=2, X=4, Y=8, L1=16, R1=32, Start=1<<14, Back=1<<15.
            var s = new ButtonState
            {
                APressed = true,    // 1
                XPressed = true,    // 4
                L1Pressed = true,   // 16
                StartPressed = true // 1<<14 = 16384
            };

            var sample = RecordingSample.FromButtonState(s, 0.5f, -0.25f, 0f, 0f, 0.1875f, 0.25f);
            Assert.Equal(1u + 4u + 16u + (1u << 14), sample.Buttons);
            Assert.True(sample.Connected);
            Assert.Equal(0.5f, sample.Lx);
            Assert.Equal(-0.25f, sample.Ly);
            Assert.Equal(0.1875f, sample.Lt);
            Assert.Equal(0.25f, sample.Rt);
        }

        [Fact]
        public void FromButtonState_All_Cleared_Is_Zero()
        {
            var s = new ButtonState(); // alle false
            var sample = RecordingSample.FromButtonState(s, 0, 0, 0, 0, 0, 0);
            Assert.Equal(0u, sample.Buttons);
            Assert.True(sample.Connected);
        }

        [Fact]
        public void Disconnected_Sample_Is_Zeroed_And_NotConnected()
        {
            var d = RecordingSample.Disconnected;
            Assert.False(d.Connected);
            Assert.Equal(0u, d.Buttons);
            Assert.Equal(0f, d.Lx);
            Assert.Equal(0f, d.Lt);
            Assert.Equal(0f, d.Rt);
        }

        [Fact]
        public void Start_Clears_Previous_Frames()
        {
            var rec = new ControllerSessionRecorder();
            rec.Start();
            rec.RecordFrame(new RecordingSample(1u, 0, 0, 0, 0, 0, 0, true));
            Assert.Equal(1, rec.FrameCount);

            // Neustart → Frames werden zurückgesetzt.
            rec.Start();
            Assert.Equal(0, rec.FrameCount);
        }
    }
}
