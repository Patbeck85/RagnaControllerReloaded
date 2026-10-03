using System;
using System.IO;
using System.Text.Json;
using Xunit;
using RagnaController.Controller;
using RagnaController.ControllerTest;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-017: Unit-Tests für den Kern des geführten Eingabe-Selbsttests
    /// (<see cref="ControllerDiagnosticRunner"/>) — hardware-frei, Zeit injizierbar.
    /// </summary>
    public class ControllerDiagnosticRunnerTests
    {
        // ------------------------------------------------------------------ Helpers

        private static RecordingSample Neutral() =>
            RecordingSample.FromButtonState(default, 0f, 0f, 0f, 0f, 0f, 0f);

        private static RecordingSample Press(string button) =>
            RecordingSample.FromButtonState(new ButtonState().With(button, true), 0f, 0f, 0f, 0f, 0f, 0f);

        private static DiagnosticControl[] TwoButtons() => new[]
        {
            new DiagnosticControl("A", "Button", "Drücke A", s => s.HasButton("A")),
            new DiagnosticControl("B", "Button", "Drücke B", s => s.HasButton("B")),
        };

        // ------------------------------------------------------------------ DoD

        [Fact]
        public void AllControls_CoversAtLeastTwelveInputs()
        {
            Assert.True(DiagnosticControl.All.Length >= 12,
                $"DoD TEST-017: ≥ 12 geprüfte Eingaben erwartet, gefunden {DiagnosticControl.All.Length}");

            // Alle Kategorien vorhanden (Buttons, Sticks, Triggers, DPad).
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (var c in DiagnosticControl.All) names.Add(c.Name);

            Assert.Contains("A", names);
            Assert.Contains("DPadUp", names);
            Assert.Contains("StickLUp", names);
            Assert.Contains("TriggerL", names);
        }

        // ------------------------------------------------------------------ Happy Path

        [Fact]
        public void FullRun_AllControlsPass_WithMeasuredReactionTimes()
        {
            var runner = new ControllerDiagnosticRunner();
            int completedFired = 0;
            runner.Completed += () => completedFired++;

            runner.Start(startMs: 0);

            for (int i = 0; i < DiagnosticControl.All.Length; i++)
            {
                long t = 10L * i + 5; // steigende Zeitbasis, keine Timeouts
                var c = DiagnosticControl.All[i];
                runner.FeedSample(SampleFor(c), nowMs: t);

                Assert.True(runner.IsRunning || i == DiagnosticControl.All.Length - 1,
                    $"Runner sollte nach Kontrolle {i} noch laufen (bzw. gerade enden)");
            }

            Assert.False(runner.IsRunning);
            Assert.Null(runner.CurrentControl);
            Assert.Equal(DiagnosticControl.All.Length, runner.Results.Count);
            Assert.True(runner.AllPassed);
            Assert.Equal(1, completedFired); // Completed feuert genau einmal

            // Reaktionszeit: Kontrolle 0 = t0 - Start(0) = 5 ms; danach Δt = 10 ms.
            // n=1 pro Kontrolle → p50 == p95 == Reaktionszeit.
            Assert.Equal(5.0, runner.Results[0].P50Ms);
            Assert.Equal(runner.Results[0].P50Ms, runner.Results[0].P95Ms);
            for (int i = 1; i < runner.Results.Count; i++)
            {
                Assert.Equal(10.0, runner.Results[i].P50Ms);
                Assert.Equal(runner.Results[i].P50Ms, runner.Results[i].P95Ms);
            }
        }

        [Fact]
        public void WrongButton_DoesNotAdvance_CorrectButtonPasses()
        {
            var runner = new ControllerDiagnosticRunner(TwoButtons(), 1000);
            runner.Start(startMs: 0);

            // Falscher Button (B) während A aktiv → keine Detektion.
            runner.FeedSample(Press("B"), nowMs: 10);
            Assert.Equal(0, runner.CurrentIndex);
            Assert.Empty(runner.Results);

            // Richtiger Button (A) → bestanden, vorankommen zu B.
            runner.FeedSample(Press("A"), nowMs: 20);
            Assert.Equal(1, runner.CurrentIndex);
            Assert.Single(runner.Results);
            Assert.True(runner.Results[0].Passed);
        }

        [Fact]
        public void StickAndTriggerDetection_UseAnalogThresholds()
        {
            var controls = new[]
            {
                new DiagnosticControl("StickLUp", "Stick", "Linker Stick: nach oben", s => s.Ly > 0.6f),
                new DiagnosticControl("TriggerR", "Trigger", "Rechter Trigger (R2) durchdrücken", s => s.Rt > 0.5f),
            };
            var runner = new ControllerDiagnosticRunner(controls, 1000);
            runner.Start(startMs: 0);

            // Unterhalb der Schwelle → keine Detektion.
            runner.FeedSample(new RecordingSample(0, 0f, 0.5f, 0f, 0f, 0f, 0.49f, true), nowMs: 10);
            Assert.Equal(0, runner.CurrentIndex);

            // StickLUp über der Schwelle → bestanden (Reaktion = 25 - 0).
            // Achtung: Runner springt SOFORT zur nächsten Kontrolle (TriggerR) vor.
            runner.FeedSample(new RecordingSample(0, 0f, 0.9f, 0f, 0f, 0f, 0.8f, true), nowMs: 25);
            Assert.Equal(DiagnosticStatus.Passed, runner.Results[0].Status);
            Assert.Equal(25.0, runner.Results[0].P50Ms);
            Assert.Equal(1, runner.CurrentIndex);

            // TriggerR über der Schwelle → bestanden (Reaktion = 40 - 25).
            runner.FeedSample(new RecordingSample(0, 0f, 0f, 0f, 0f, 0f, 0.8f, true), nowMs: 40);
            Assert.True(runner.AllPassed);
            Assert.Equal(15.0, runner.Results[1].P50Ms);
        }

        // ------------------------------------------------------------------ Fehlfälle

        [Fact]
        public void Timeout_FailsControl_AdvancesToNext()
        {
            var runner = new ControllerDiagnosticRunner(TwoButtons(), timeoutMs: 100);
            runner.Start(startMs: 0);

            runner.FeedSample(Neutral(), nowMs: 50);   // noch kein Timeout
            Assert.Empty(runner.Results);

            runner.FeedSample(Neutral(), nowMs: 100);  // elapsed = 100 ≥ 100 → Fail
            Assert.Equal(1, runner.FailedCount);
            Assert.Equal(DiagnosticStatus.Failed, runner.Results[0].Status);
            Assert.Equal(1, runner.CurrentIndex);       // automatisch vorankommen

            // Nächste Kontrolle wird noch bestanden (Reaktion = 120 - 100).
            runner.FeedSample(Press("B"), nowMs: 120);
            Assert.True(runner.IsRunning == false);
            Assert.Equal(DiagnosticStatus.Passed, runner.Results[1].Status);
            Assert.Equal(20.0, runner.Results[1].P50Ms);
            Assert.False(runner.AllPassed);             // Kontrolle 0 fehlgeschlagen
        }

        [Fact]
        public void DisconnectedSample_NeverDetects_OnlyTimeoutRuns()
        {
            var runner = new ControllerDiagnosticRunner(TwoButtons(), timeoutMs: 100);
            runner.Start(startMs: 0);

            // Disconnect-Frame: keine Detektion, kein Crash, Timeout läuft weiter.
            runner.FeedSample(RecordingSample.Disconnected, nowMs: 50);
            Assert.Empty(runner.Results);

            runner.FeedSample(RecordingSample.Disconnected, nowMs: 100);
            Assert.Equal(DiagnosticStatus.Failed, runner.Results[0].Status);
        }

        [Fact]
        public void Stop_MarksActiveControlFailed()
        {
            var runner = new ControllerDiagnosticRunner(TwoButtons(), 1000);
            runner.Start(startMs: 0);

            runner.Stop();

            Assert.False(runner.IsRunning);
            Assert.Equal(-1, runner.CurrentIndex);
            Assert.Null(runner.CurrentControl);
            Assert.Single(runner.Results);
            Assert.Equal(DiagnosticStatus.Failed, runner.Results[0].Status);
        }

        [Fact]
        public void FeedSample_AfterStop_IsIgnored()
        {
            var runner = new ControllerDiagnosticRunner(TwoButtons(), 1000);
            runner.Start(startMs: 0);
            runner.Stop();

            // Muss still ignoriert werden (kein Index-Overrun, kein Ergebnis).
            runner.FeedSample(Press("A"), nowMs: 500);
            Assert.Single(runner.Results);
        }

        [Fact]
        public void Start_ResetsPreviousRun()
        {
            var runner = new ControllerDiagnosticRunner(TwoButtons(), 1000);
            runner.Start(startMs: 0);
            runner.FeedSample(Press("A"), nowMs: 5);
            Assert.Single(runner.Results);

            // Neustart: Ergebnisse zurückgesetzt, bei Kontrolle 0.
            runner.Start(startMs: 1000);
            Assert.Empty(runner.Results);
            Assert.Equal(0, runner.CurrentIndex);
        }

        // ------------------------------------------------------------------ JSON-Report

        [Fact]
        public void BuildJsonReport_ContainsAllResults_AndSummary()
        {
            var runner = new ControllerDiagnosticRunner(TwoButtons(), 1000);
            runner.Start(startMs: 0);
            runner.FeedSample(Press("A"), nowMs: 5);   // A: pass, 5 ms
            runner.FeedSample(Neutral(), nowMs: 1200); // B: timeout (1200-5=1195 ≥ 1000) → fail

            using var doc = JsonDocument.Parse(runner.BuildJsonReport());
            var root = doc.RootElement;

            Assert.Equal(2, root.GetProperty("totalControls").GetInt32());
            Assert.Equal(2, root.GetProperty("completedCount").GetInt32());
            Assert.Equal(1, root.GetProperty("passedCount").GetInt32());
            Assert.Equal(1, root.GetProperty("failedCount").GetInt32());
            Assert.False(root.GetProperty("allPassed").GetBoolean());

            var results = root.GetProperty("results");
            Assert.Equal(2, results.GetArrayLength());

            var first = results[0];
            Assert.Equal("A", first.GetProperty("name").GetString());
            Assert.Equal("passed", first.GetProperty("status").GetString());
            Assert.Equal(5.0, first.GetProperty("p50Ms").GetDouble());

            var second = results[1];
            Assert.Equal("B", second.GetProperty("name").GetString());
            Assert.Equal("failed", second.GetProperty("status").GetString());
        }

        [Fact]
        public void SaveJsonReport_WritesFile_AndCreatesParentDirectories()
        {
            string dir = Path.Combine(Path.GetTempPath(), "ragna-diag-test-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(dir, "sub", "report.json");

            try
            {
                var runner = new ControllerDiagnosticRunner(TwoButtons(), 1000);
                runner.Start(startMs: 0);
                runner.FeedSample(Press("A"), nowMs: 5);
                runner.FeedSample(Press("B"), nowMs: 12);

                runner.SaveJsonReport(path);

                Assert.True(File.Exists(path));
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                Assert.True(doc.RootElement.GetProperty("allPassed").GetBoolean());
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        // ------------------------------------------------------------------ HasButton

        [Fact]
        public void HasButton_MatchesMask_AndUnknownNamesReturnFalse()
        {
            var sample = Press("A");
            Assert.True(sample.HasButton("A"));
            Assert.False(sample.HasButton("B"));
            Assert.False(sample.HasButton("L2"));   // Trigger: NICHT Teil der Mask
            Assert.False(sample.HasButton("unbekannt"));
        }

        // ------------------------------------------------------------------ intern

        /// <summary>Baut ein Sample, das exakt die gegebene Kontrolle detektiert.</summary>
        private static RecordingSample SampleFor(DiagnosticControl c) => c.Name switch
        {
            "StickLUp"   => new RecordingSample(0, 0f, 0.9f, 0f, 0f, 0f, 0f, true),
            "StickLDown" => new RecordingSample(0, 0f, -0.9f, 0f, 0f, 0f, 0f, true),
            "StickRUp"   => new RecordingSample(0, 0f, 0f, 0f, 0.9f, 0f, 0f, true),
            "StickRDown" => new RecordingSample(0, 0f, 0f, 0f, -0.9f, 0f, 0f, true),
            "TriggerL"   => new RecordingSample(0, 0f, 0f, 0f, 0f, 0.9f, 0f, true),
            "TriggerR"   => new RecordingSample(0, 0f, 0f, 0f, 0f, 0f, 0.9f, true),
            _             => Press(c.Name), // digitale Buttons: Name == ButtonState-Name
        };
    }
}
