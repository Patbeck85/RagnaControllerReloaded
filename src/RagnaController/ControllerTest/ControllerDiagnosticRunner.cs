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
    /// TEST-017: Eine einzelne Kontrolle des geführten Eingabe-Selbsttests.
    /// </summary>
    public enum DiagnosticStatus
    {
        Pending,
        Passed,
        Failed
    }

    /// <summary>
    /// TEST-017: Eine geprüfte Eingabe (Button/Stick/Trigger/DPad) mit Prompt und Detektions-Bedingung.
    /// Die Bedingung prüft ein einzelnes <see cref="RecordingSample"/> (Rohwerte, identisch zur
    /// CI-Replay-Fixtur), damit Diagnose und Replay dieselbe Datenbasis nutzen.
    /// </summary>
    public readonly record struct DiagnosticControl(
        string Name,
        string Category,
        string Prompt,
        Func<RecordingSample, bool> Detect)
    {
        /// <summary>
        /// Die 16 Pflicht-Kontrollen (DoD ≥ 12): alle Face-Buttons, L1/R1, Start/Back, L3/R3,
        /// komplettes DPad (4), beide Sticks (je 2 Richtungen) und beide Triggers.
        /// </summary>
        public static readonly DiagnosticControl[] All =
        {
            new("A",          "Button",  "Drücke A (□)",           s => s.HasButton("A")),
            new("B",          "Button",  "Drücke B (○)",           s => s.HasButton("B")),
            new("X",          "Button",  "Drücke X (✕)",           s => s.HasButton("X")),
            new("Y",          "Button",  "Drücke Y (△)",           s => s.HasButton("Y")),
            new("L1",         "Schulter","Drücke L1",              s => s.HasButton("L1")),
            new("R1",         "Schulter","Drücke R1",              s => s.HasButton("R1")),
            new("Start",      "System",  "Drücke Start",           s => s.HasButton("Start")),
            new("Back",       "System",  "Drücke Back/Select",     s => s.HasButton("Back")),
            new("L3",         "Stick",   "Drücke L3 (Linkstick drücken)", s => s.HasButton("L3")),
            new("R3",         "Stick",   "Drücke R3 (Rechtsstick drücken)", s => s.HasButton("R3")),
            new("DPadUp",     "DPad",    "DPad nach oben",         s => s.HasButton("DPadUp")),
            new("DPadDown",   "DPad",    "DPad nach unten",        s => s.HasButton("DPadDown")),
            new("DPadLeft",   "DPad",    "DPad nach links",        s => s.HasButton("DPadLeft")),
            new("DPadRight",  "DPad",    "DPad nach rechts",       s => s.HasButton("DPadRight")),
            new("StickLUp",   "Stick",   "Linker Stick: nach oben", s => s.Ly > 0.6f),
            new("StickLDown", "Stick",   "Linker Stick: nach unten",s => s.Ly < -0.6f),
            new("StickRUp",   "Stick",   "Rechter Stick: nach oben",s => s.Ry > 0.6f),
            new("StickRDown", "Stick",   "Rechter Stick: nach unten",s => s.Ry < -0.6f),
            new("TriggerL",   "Trigger", "Linker Trigger (L2) durchdrücken", s => s.Lt > 0.5f),
            new("TriggerR",   "Trigger", "Rechter Trigger (R2) durchdrücken",s => s.Rt > 0.5f),
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
    /// TEST-017: Unit-testbarer Kern des geführten Eingabe-Selbsttests (ControllerTestWindow).
    ///
    /// Führt eine feste Folge von <see cref="DiagnosticControl"/>-Kontrollen durch. Pro Tick wird ein
    /// <see cref="RecordingSample"/> über <see cref="FeedSample"/> eingespeist; die aktive Kontrolle
    /// ist bestanden, sobald ihr <c>Detect</c>-Prädikat wahr wird (Detektion). Die Reaktionszeit
    /// (Aktivierung → erste Detektion) wird gemessen und als p50/p95 pro Kontrolle ausgegeben
    /// (geführter Selbsttest: n=1 pro Kontrolle, daher p50 == p95 == Reaktionszeit). Ohne Detektion
    /// innerhalb des Timeouts gilt die Kontrolle als fehlgeschlagen. So erhält der Nutzer ein
    /// verifizierbares Pass/Fail-Statement („einwandfrei") statt nur einer Live-Anzeige.
    ///
    /// Hardware-/UI-frei: Samples werden injiziert, Zeit wird über <c>nowMs</c> übergeben (default
    /// <see cref="Environment.TickCount64"/>), daher vollständig unit-testbar.
    /// </summary>
    public sealed class ControllerDiagnosticRunner : IDisposable
    {
        /// <summary>Timeout pro Kontrolle: danach gilt sie als fehlgeschlagen, wenn nie detektiert.</summary>
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

        /// <summary>Timeout pro Kontrolle in ms.</summary>
        public int TimeoutMs { get; }

        /// <summary>Anzahl der definierten Kontrollen.</summary>
        public int TotalControls => _controls.Length;

        /// <summary>Index der aktuell aktiven Kontrolle (0..Total-1), -1 wenn nicht aktiv/fertig.</summary>
        public int CurrentIndex => _running ? _currentIndex : -1;

        /// <summary>Die aktive Kontrolle, null wenn keine läuft.</summary>
        public DiagnosticControl? CurrentControl => _running && _currentIndex >= 0 ? _controls[_currentIndex] : null;

        /// <summary>Ob der Selbsttest gerade aktiv ist.</summary>
        public bool IsRunning => _running;

        /// <summary>Ergebnisse aller abgeschlossenen Kontrollen (in Reihenfolge).</summary>
        public IReadOnlyList<DiagnosticControlResult> Results => _results;

        /// <summary>Der Gesamtstatus: bestanden, wenn alle Kontrollen bestanden sind.</summary>
        public bool AllPassed => _results.Count == _controls.Length && _results.All(r => r.Passed);

        /// <summary>Anzahl bestandener Kontrollen.</summary>
        public int PassedCount => _results.Count(r => r.Passed);

        /// <summary>Anzahl fehlgeschlagener Kontrollen.</summary>
        public int FailedCount => _results.Count(r => !r.Passed);

        /// <summary>Feuert, nachdem eine Kontrolle abgeschlossen ist (Pass oder Fail).</summary>
        public event Action<DiagnosticControlResult>? ControlCompleted;

        /// <summary>Feuert, wenn alle Kontrollen abgeschlossen sind.</summary>
        public event Action? Completed;

        /// <summary>Startet den Selbsttest und setzt alle Ergebnisse zurück.</summary>
        /// <param name="startMs">Optionale Startzeit (für deterministische Tests); default: jetzt.</param>
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

        /// <summary>Beendet den Selbsttest (aktive Kontrolle zählt als fehlgeschlagen).</summary>
        public void Stop()
        {
            if (!_running) return;
            FinalizeCurrent(false);
            _running = false;
            _currentIndex = -1; // Gestoppt: keine aktive Kontrolle mehr (UI zeigt Ruhestand).
        }

        /// <summary>
        /// Speist ein Sample aus dem 20-Hz-Tick ein. <paramref name="nowMs"/> ist die Referenzzeit
        /// (default: <see cref="Environment.TickCount64"/>) — in Tests injizierbar für deterministische Latenz.
        /// </summary>
        public void FeedSample(RecordingSample sample, long? nowMs = null)
        {
            if (!_running) return;

            long now = nowMs ?? Environment.TickCount64;
            _lastNowMs = now;

            // Disconnect: keine Detektion möglich — nur Timeout weiterlaufen lassen.
            if (!sample.Connected)
            {
                CheckTimeout(now);
                return;
            }

            var control = _controls[_currentIndex];

            // Erste Detektion der aktiven Kontrolle → Reaktionszeit messen und bestanden.
            // n=1 pro Kontrolle (geführter Selbsttest): p50 == p95 == Reaktionszeit.
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

        /// <summary>Erzeugt den JSON-Report (camelCase) für den Export.</summary>
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

        /// <summary>Schreibt den JSON-Report nach <paramref name="path"/> (Parent-Dirs werden angelegt).</summary>
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

            // n=1 pro Kontrolle (geführter Selbsttest): p50 == p95 == gemessene Reaktionszeit.
            double reaction = passed ? _reactionMs : 0;

            var result = new DiagnosticControlResult(
                control.Name, control.Category, control.Prompt,
                passed ? DiagnosticStatus.Passed : DiagnosticStatus.Failed,
                passed ? 1 : 0, reaction, reaction);

            _results.Add(result);

            // Nächste Kontrolle (oder Ende). Zeitbasis: zuletzt beobachtete Zeit (Tests injizieren
            // nowMs — realer Wanduhr-Zugriff hier würde deterministische Tests brechen).
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
        // IDisposable (TECH-025): Die Klasse ist eine reine Zustandsmaschine ohne
        // eigenen Timer/Thread, aber sie HÄLT per Event-Delegates auf ihre
        // Subscriber (z. B. das Window) → ein laufendes Fenster hält den Runner
        // indirekt am Leben. Dispose() bricht die Referenzen und markiert den
        // Runner als beendet: danach sind Start()/FeedSample() inert.
        // ---------------------------------------------------------------------

        /// <summary>Runner ist disposed — keine weiteren Aktionen mehr möglich.</summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Gibt Subscriber-Referenzen frei und deaktiviert den Runner (idempotent).
        /// Danach sind <see cref="Start"/> und <see cref="FeedSample"/> inert.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            _running = false;
            // Subscriber-Referenzen freigeben → kein indirekter Leak über Events mehr.
            ControlCompleted = null;
            Completed = null;
        }
    }
}
