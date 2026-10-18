using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace RagnaController.ControllerTest
{
    /// <summary>
    /// TEST-017: A single check of the guided input self-test.
    /// </summary>
    public enum DiagnosticStatus
    {
        Pending,
        Passed,
        Failed
    }

    /// <summary>
    /// TEST-017: A verified input (Button/Stick/Trigger/DPad) with Prompt and Detection condition.
    /// The condition checks a single <see cref="RecordingSample"/> (raw values, identical to the
    /// CI replay fixture), so Diagnose and Replay use the same data basis.
    /// </summary>
    public readonly record struct DiagnosticControl(
        string Name,
        string Category,
        string Prompt,
        Func<RecordingSample, bool> Detect)
    {
        /// <summary>
        /// The 16 mandatory controls (DoD ≥ 12): all face buttons, L1/R1, Start/Back, L3/R3,
        /// full DPad (4), both sticks (2 directions each), and both triggers.
        /// </summary>
        public static readonly DiagnosticControl[] All =
        {
            new("A",          "Button",  "Press A (□)",           s => s.HasButton("A")),
            new("B",          "Button",  "Press B (○)",           s => s.HasButton("B")),
            new("X",          "Button",  "Press X (✕)",           s => s.HasButton("X")),
            new("Y",          "Button",  "Press Y (△)",           s => s.HasButton("Y")),
            new("L1",         "Shoulder","Press L1",              s => s.HasButton("L1")),
            new("R1",         "Shoulder","Press R1",              s => s.HasButton("R1")),
            new("Start",      "System",  "Press Start",           s => s.HasButton("Start")),
            new("Back",       "System",  "Press Back/Select",     s => s.HasButton("Back")),
            new("L3",         "Stick",   "Press L3 (Left stick press)", s => s.HasButton("L3")),
            new("R3",         "Stick",   "Press R3 (Right stick press)", s => s.HasButton("R3")),
            new("DPadUp",     "DPad",    "DPad up",         s => s.HasButton("DPadUp")),
            new("DPadDown",   "DPad",    "DPad down",        s => s.HasButton("DPadDown")),
            new("DPadLeft",   "DPad",    "DPad left",        s => s.HasButton("DPadLeft")),
            new("DPadRight",  "DPad",    "DPad right",       s => s.HasButton("DPadRight")),
            new("StickLUp",   "Stick",   "Left stick: up", s => s.Ly > 0.6f),
            new("StickLDown", "Stick",   "Left stick: down",s => s.Ly < -0.6f),
            new("StickRUp",   "Stick",   "Right stick: up",s => s.Ry > 0.6f),
            new("StickRDown", "Stick",   "Right stick: down",s => s.Ry < -0.6f),
            new("TriggerL",   "Trigger", "Press left trigger (L2)", s => s.Lt > 0.5f),
            new("TriggerR",   "Trigger", "Press right trigger (R2)",s => s.Rt > 0.5f),
        };
    }

    /// <summary>
    /// TEST-017: Ergebnis einer einzelnen Kontrolle inkl. Latenz-Prozentile (p50/p95, ms).
    /// </summary>
    public readonly record struct DiagnosticControlResult(
        string Name,
        string Category,
        string Prompt,
        DiagnosticStatus Status,
        int SampleCount,
        double P50Ms,
        double P95Ms)
    {
        public bool Passed => Status == DiagnosticStatus.Passed;
    }

    /// <summary>
    /// TEST-017: Unit-testable core of the guided input self-test (ControllerTestWindow).
    ///
    /// Runs a fixed sequence of <see cref="DiagnosticControl"/> controls. Per tick a
    /// <see cref="RecordingSample"/> fed via <see cref="FeedSample"/>; the active control
    /// is passed as soon as its <c>Detect</c> predicate true is (Detektion). The reaction time
    /// (activation → first detection) is measured and output as p50/p95 per control
    /// (guided self-test: n=1 per control, therefore p50 == p95 == reaction time). Without detection
    /// within the timeout the control counts as failed. So the user gets a
    /// verifizierbares Pass/Fail-Statement („einwandfrei") statt only einer Live-Anzeige.
    ///
    /// hardware-/UI-free: samples are injected, time is passed via <c>nowMs</c> (default
    /// <see cref="Environment.TickCount64"/>), therefore fully unit-testable.
    /// </summary>
    /// <summary>
    /// Test harness class for controller validation.
    /// </summary>
    public sealed class ControllerDiagnosticRunner : IDisposable
    {
        /// <summary>Timeout per control: afterwards it counts as failed, if never detected.</summary>
        public const int DefaultTimeoutMs = 15000;

        private readonly DiagnosticControl[] _controls;
        private readonly List<DiagnosticControlResult> _results = new();

        private int _currentIndex;
        private long _activeSinceMs;
        private long _lastNowMs;
        private double _reactionMs;
        private bool _running;

        public ControllerDiagnosticRunner() : this(DiagnosticControl.All, DefaultTimeoutMs) { }

        public ControllerDiagnosticRunner(IReadOnlyList<DiagnosticControl> controls, int timeoutMs = DefaultTimeoutMs)
        {
            _controls = controls.ToArray();
            TimeoutMs = timeoutMs;
        }

        /// <summary>Timeout per control in ms.</summary>
        public int TimeoutMs { get; }

        /// <summary>Number of defined controls.</summary>
        public int TotalControls => _controls.Length;

        /// <summary>Index of currently active control (0..Total-1), -1 if not active/done.</summary>
        public int CurrentIndex => _running ? _currentIndex : -1;

        /// <summary>The active control, null if none running.</summary>
        public DiagnosticControl? CurrentControl => _running && _currentIndex >= 0 ? _controls[_currentIndex] : null;

        /// <summary>Ob der Selbsttest gerade active is.</summary>
        public bool IsRunning => _running;

        /// <summary>Results of all completed controls (in order).</summary>
        public IReadOnlyList<DiagnosticControlResult> Results => _results;

        /// <summary>The overall status: passed, if all controls passed.</summary>
        public bool AllPassed => _results.Count == _controls.Length && _results.All(r => r.Passed);

        /// <summary>Number of passed controls.</summary>
        public int PassedCount => _results.Count(r => r.Passed);

        /// <summary>Number of failed controls.</summary>
        public int FailedCount => _results.Count(r => !r.Passed);

        /// <summary>Fires after a control is completed (Pass or Fail).</summary>
        public event Action<DiagnosticControlResult>? ControlCompleted;

        /// <summary>Fires if all controls are completed.</summary>
        public event Action? Completed;

        /// <summary>Starts the self-test and resets all results.</summary>
        /// <param name="startMs">Optionale Startzeit (for deterministische Tests); default: jetzt.</param>
        public void Start(long? startMs = null)
        {
            if (IsDisposed) return; // TECH-025: nach Dispose ist der Runner inert.
            _results.Clear();
            _currentIndex = 0;
            _activeSinceMs = startMs ?? Environment.TickCount64;
            _lastNowMs = _activeSinceMs;
            _reactionMs = 0;
            _running = true;
        }

        /// <summary>Ends the self-test (active control counts as failed).</summary>
        public void Stop()
        {
            if (!_running) return;
            FinalizeCurrent(false);
            _running = false;
            _currentIndex = -1; // Stopped: no active control anymore (UI shows idle state).
        }

        /// <summary>
        /// Speist ein Sample aus dem 20-Hz-Tick ein. <paramref name="nowMs"/> is die Referenzzeit
        /// (default: <see cref="Environment.TickCount64"/>) — in Tests injizierbar for deterministische Latenz.
        /// </summary>
        public void FeedSample(RecordingSample sample, long? nowMs = null)
        {
            if (!_running) return;

            long now = nowMs ?? Environment.TickCount64;
            _lastNowMs = now;

            // Disconnect: no detection possible — only timeout keeps running.
            if (!sample.Connected)
            {
                CheckTimeout(now);
                return;
            }

            var control = _controls[_currentIndex];

            // Erste Detektion der aktiven Kontrolle → Reaktionszeit messen and bestanden.
            // n=1 pro Kontrolle (geführter Selbsttest): p50 == p95 == reaction time.
            if (control.Detect(sample))
            {
                _reactionMs = Math.Max(0, now - _activeSinceMs);
                FinalizeCurrent(true);
            }
            else
            {
                CheckTimeout(now);
            }
        }

        /// <summary>Erzeugt den JSON-Report (camelCase) for den Export.</summary>
        public string BuildJsonReport()
        {
            using var stream = new MemoryStream();
            using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                w.WriteStartObject();
                w.WriteString("generatedAtUtc", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                w.WriteNumber("totalControls", _controls.Length);
                w.WriteNumber("completedCount", _results.Count);
                w.WriteNumber("passedCount", PassedCount);
                w.WriteNumber("failedCount", FailedCount);
                w.WriteBoolean("allPassed", AllPassed);

                w.WriteStartArray("results");
                foreach (var r in _results)
                {
                    w.WriteStartObject();
                    w.WriteString("name", r.Name);
                    w.WriteString("category", r.Category);
                    w.WriteString("prompt", r.Prompt);
                    w.WriteString("status", StatusString(r.Status));
                    w.WriteNumber("sampleCount", r.SampleCount);
                    w.WriteNumber("p50Ms", Math.Round(r.P50Ms, 3));
                    w.WriteNumber("p95Ms", Math.Round(r.P95Ms, 3));
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                w.WriteEndObject();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>Schreibt den JSON-Report nach <paramref name="path"/> (Parent-Dirs are angelegt).</summary>
        public void SaveJsonReport(string path)
        {
            string json = BuildJsonReport();
            string dir = Path.GetDirectoryName(path) ?? "";
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, json);
        }

        // ------------------------------------------------------------------ intern

        private void CheckTimeout(long nowMs)
        {
            long elapsed = nowMs - _activeSinceMs;
            if (elapsed >= TimeoutMs)
            {
                FinalizeCurrent(false);
            }
        }

        private void FinalizeCurrent(bool passed)
        {
            var control = _controls[_currentIndex];

            // n=1 pro Kontrolle (geführter Selbsttest): p50 == p95 == measured reaction time.
            double reaction = passed ? _reactionMs : 0;

            var result = new DiagnosticControlResult(
                control.Name, control.Category, control.Prompt,
                passed ? DiagnosticStatus.Passed : DiagnosticStatus.Failed,
                passed ? 1 : 0, reaction, reaction);

            _results.Add(result);

            // Next control (or end). Time base: last observed time (Tests injizieren
            // nowMs — real wall-clock access here would break deterministic tests).
            if (_currentIndex + 1 < _controls.Length)
            {
                _currentIndex++;
                _activeSinceMs = _lastNowMs;
            }
            else
            {
                _running = false;
            }

            ControlCompleted?.Invoke(result);
            if (!_running && _results.Count == _controls.Length)
            {
                Completed?.Invoke();
            }
        }

        private static string StatusString(DiagnosticStatus s) => s switch
        {
            DiagnosticStatus.Passed => "passed",
            DiagnosticStatus.Failed => "failed",
            _ => "pending"
        };

        // ---------------------------------------------------------------------
        // IDisposable (TECH-025): The Klasse is eine reine Zustandsmaschine ohne
        // own timer/thread, but they hold per event-delegates on their
        // subscribers (e.g. the window) → a running window keeps the runner
        // indirekt am Leben. Dispose() bricht die Referenzen and markiert den
        // Runner als beendet: danach are Start()/FeedSample() inert.
        // ---------------------------------------------------------------------

        /// <summary>Runner is disposed — no further actions possible.</summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Gibt Subscriber-Referenzen frei and disabled den Runner (idempotent).
        /// Danach are <see cref="Start"/> and <see cref="FeedSample"/> inert.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            _running = false;
            // Subscriber-Referenzen freigeben → no indirekter Leak about Events mehr.
            ControlCompleted = null;
            Completed = null;
        }
    }
}
