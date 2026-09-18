using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// FEAT-014: Session-Replay Roundtrip-Tests (DoD: aufzeichnen → abspielen → identische
    /// Aktionssequenz). Deckt JSONL-Roundtrip, Rotation über mehrere Parts, State-Throttling
    /// und JSON-Escaping von User-defined Labels ab.
    /// </summary>
    public class SessionRecorderTests : IDisposable
    {
        private readonly string _sessionDir;

        public SessionRecorderTests()
        {
            // Isoliertes Temp-Verzeichnis pro Test (Workspace-Isolation, keine Projekt-Verschmutzung).
            _sessionDir = Path.Combine(Path.GetTempPath(), "ragna-replay-test-" + Guid.NewGuid().ToString("N"));
        }

        public void Dispose()
        {
            try { Directory.Delete(_sessionDir, true); } catch { /* best effort */ }
        }

        [Fact]
        public void Roundtrip_InputsAndActions_ReplayIdenticalSequence()
        {
            // DoD-Kern: aufzeichnen → abspielen → identische Aktionssequenz.
            var recorder = new SessionRecorder(_sessionDir, "roundtrip");
            recorder.Start();

            var expectedInputs = new List<string>
            {
                nameof(CmdType.MouseRel),
                nameof(CmdType.LeftDown),
                nameof(CmdType.KeyDown),
                nameof(CmdType.LeftUp)
            };
            var expectedActions = new List<string> { "Haste", "Blink", "Sanctuary" };

            recorder.RecordInput(new InputCmd(CmdType.MouseRel, 12, -34));
            recorder.RecordInput(new InputCmd(CmdType.LeftDown));
            recorder.RecordAction("Haste", ActionFiredKind.Skill);
            recorder.RecordInput(new InputCmd(CmdType.KeyDown, (ushort)0x41));
            recorder.RecordAction("Blink", ActionFiredKind.Skill);
            recorder.RecordInput(new InputCmd(CmdType.LeftUp));
            recorder.RecordAction("Sanctuary", ActionFiredKind.Combo);
            recorder.Stop();

            var events = SessionReplay.Load(_sessionDir);

            Assert.Equal(expectedInputs, SessionReplay.ReplayedInputs(events));
            Assert.Equal(expectedActions, SessionReplay.ReplayedActions(events));
        }

        [Fact]
        public void Roundtrip_AcrossRotation_MergesPartsInRecordedOrder()
        {
            // 300-Byte-Limit zwingt ~40 Events über mehrere Parts → der Merge muss die
            // exakte Aufzeichnungsreihenfolge über Part-Grenzen hinweg erhalten.
            var recorder = new SessionRecorder(_sessionDir, "rotation", maxBytesPerPart: 300);
            recorder.Start();

            const int n = 40;
            var expectedInputs = new List<string>(n);
            for (int i = 0; i < n; i++)
            {
                recorder.RecordInput(new InputCmd(CmdType.MouseRel, i, -i));
                expectedInputs.Add(nameof(CmdType.MouseRel));
            }
            recorder.Stop();

            // Rotation hat tatsächlich mehrere Parts erzeugt (sonst testet der Fall nichts).
            var partFiles = Directory.GetFiles(_sessionDir, "replay-part*.jsonl");
            Assert.True(partFiles.Length >= 2, $"Erwarte ≥2 Rotations-Parts, gefunden: {partFiles.Length}");

            var events = SessionReplay.Load(_sessionDir);
            Assert.Equal(expectedInputs, SessionReplay.ReplayedInputs(events));
        }

        [Fact]
        public void ReplayedActions_ReturnsOnlyActionEventsInOrder()
        {
            var recorder = new SessionRecorder(_sessionDir, "actions-only");
            recorder.Start();

            recorder.RecordInput(new InputCmd(CmdType.LeftDown));
            recorder.RecordAction("Alpha", ActionFiredKind.Skill);
            recorder.RecordInput(new InputCmd(CmdType.KeyDown, (ushort)0x42));
            recorder.RecordAction("Beta", ActionFiredKind.Click);
            recorder.Stop();

            var events = SessionReplay.Load(_sessionDir);

            Assert.Equal(2, SessionReplay.ReplayedActions(events).Count);
            Assert.Equal(new[] { "Alpha", "Beta" }, SessionReplay.ReplayedActions(events));
            // Input-Events bleiben in der Gesamtliste erhalten (K=1).
            Assert.Equal(2, events.Count(e => e.K == SessionReplay.KInput));
        }

        [Fact]
        public void RecordState_ThrottlesToApprox10Hz()
        {
            var recorder = new SessionRecorder(_sessionDir, "state-throttle");
            recorder.Start();

            // Erstes State-Event nach >100ms → wird geschrieben; das sofort folgende wird gedrosselt.
            Thread.Sleep(150);
            recorder.RecordState(new ControllerSnapshot { LayerText = "COMBO", TargetName = "Mob", SkillCooldownMs = 42 });
            recorder.RecordState(new ControllerSnapshot { LayerText = "COMBO", TargetName = "Mob", SkillCooldownMs = 43 });
            recorder.Stop();

            var events = SessionReplay.Load(_sessionDir);
            var states = events.Where(e => e.K == SessionReplay.KState).ToList();

            Assert.Single(states);
            Assert.Equal("COMBO|Mob", states[0].Label);
            Assert.Equal(42, states[0].A); // SkillCooldownMs des ersten (nicht gedrosselten) Events
        }

        [Fact]
        public void RecordAction_EscapesQuotesAndBackslashes_Roundtrips()
        {
            var label = "A\"B\\C"; // User-defined Label mit Quote + Backslash
            var recorder = new SessionRecorder(_sessionDir, "escaping");
            recorder.Start();
            recorder.RecordAction(label, ActionFiredKind.Special);
            recorder.Stop();

            var events = SessionReplay.Load(_sessionDir);
            Assert.Equal(new[] { label }, SessionReplay.ReplayedActions(events));
        }

        [Fact]
        public void Load_SkipsHeaderLine()
        {
            var recorder = new SessionRecorder(_sessionDir, "header");
            recorder.Start();
            recorder.RecordInput(new InputCmd(CmdType.Wait) { X = 50 });
            recorder.Stop();

            var events = SessionReplay.Load(_sessionDir);

            // Nur das eine Input-Event — die Header-Zeile (k=0) wird übersprungen.
            Assert.Single(events);
        }
    }
}
