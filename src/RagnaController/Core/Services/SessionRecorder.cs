using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// FEAT-014: Session-Replay — JSONL-Recorder für deterministische Wiedergabe.
    /// Eine Zeile pro Event (Input/Action/State), gepuffertes StreamWriter, 50 MB-Rotation.
    /// Overhead im Hot-Path &lt;1ms: wiederverwendeter StringBuilder (kein LOH-Churn),
    /// ein Lock, relative Zeitbasis via Stopwatch (keine DateTime.Now-Abrufe pro Event).
    /// </summary>
    public sealed class SessionRecorder : IDisposable
    {
        private const long DefaultMaxBytesPerPart = 50L * 1024 * 1024; // 50 MB → Rotation

        private readonly object _lock = new();
        private readonly StringBuilder _sb = new(96);
        private readonly string _sessionDir;
        private readonly long _maxBytesPerPart;
        private readonly Stopwatch _clock = new();
        private StreamWriter? _writer;
        private long _bytesInPart;
        private int _partIndex;
        private long _lastStateMs; // Volatile.Read: State-Throttle (10 Hz)

        public SessionRecorder(string sessionDir, string sessionName)
            : this(sessionDir, sessionName, DefaultMaxBytesPerPart) { }

        /// <summary>Test-Konstruktor mit konfigurierbarer Rotationsgröße.</summary>
        internal SessionRecorder(string sessionDir, string sessionName, long maxBytesPerPart)
        {
            _sessionDir = sessionDir;
            _maxBytesPerPart = maxBytesPerPart;
        }

        /// <summary>Startet die Aufnahme: legt Ordner + Part 0 (mit Header-Zeile) an.</summary>
        public void Start()
        {
            lock (_lock)
            {
                Directory.CreateDirectory(_sessionDir);
                OpenPart(0);
                WriteLine("{\"k\":0,\"l\":\"header\",\"v\":1}");
                _clock.Restart();
            }
        }

        /// <summary>Input-Command aufzeichnen (Hook: InputCommandQueue.OnCommandEnqueued).</summary>
        public void RecordInput(InputCmd cmd)
        {
            lock (_lock)
            {
                if (_writer == null) return;
                _sb.Clear();
                _sb.Append("{\"t\":").Append(_clock.ElapsedMilliseconds)
                   .Append(",\"k\":1,\"l\":\"").Append(cmd.Type.ToString())
                   .Append("\",\"a\":").Append(cmd.Key)
                   .Append(",\"b\":").Append(cmd.X)
                   .Append(",\"c\":").Append(cmd.Y).Append('}');
                WriteLine(_sb);
            }
        }

        /// <summary>Gefeuerte Aktion aufzeichnen (Label + Kind).</summary>
        public void RecordAction(string label, ActionFiredKind kind)
        {
            lock (_lock)
            {
                if (_writer == null) return;
                _sb.Clear();
                _sb.Append("{\"t\":").Append(_clock.ElapsedMilliseconds)
                   .Append(",\"k\":2,\"l\":\"").Append(EscapeJson(label ?? ""))
                   .Append("\",\"a\":").Append((int)kind).Append('}');
                WriteLine(_sb);
            }
        }

        /// <summary>
        /// Engine-Zustand aufzeichnen (intern 10 Hz gedrosselt — KISS: der Recorder
        /// drosselt selbst, der Tick-Pfad muss nicht zählen).
        /// </summary>
        public void RecordState(ControllerSnapshot snap)
        {
            if (_clock.ElapsedMilliseconds - Volatile.Read(ref _lastStateMs) < 100) return;
            lock (_lock)
            {
                if (_writer == null) return;
                _lastStateMs = _clock.ElapsedMilliseconds;
                _sb.Clear();
                _sb.Append("{\"t\":").Append(_lastStateMs)
                   .Append(",\"k\":3,\"l\":\"")
                   .Append(EscapeJson(snap.LayerText ?? ""))
                   .Append("|").Append(EscapeJson(snap.TargetName ?? ""))
                   .Append("\",\"a\":").Append(snap.SkillCooldownMs).Append('}');
                WriteLine(_sb);
            }
        }

        /// <summary>JSON-String-Escape (Labels sind User-defined — Quotes/Backslash müssen weg).</summary>
        private static void EscapeJson(StringBuilder sb, string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
        }

        private static string EscapeJson(string s)
        {
            var sb = new StringBuilder(s.Length);
            EscapeJson(sb, s);
            return sb.ToString();
        }

        /// <summary>Stoppt die Aufnahme und flusht die Datei.</summary>
        public void Stop()
        {
            lock (_lock)
            {
                _clock.Stop();
                if (_writer == null) return;
                try
                {
                    _writer.Flush();
                    _writer.Dispose();
                }
                finally
                {
                    _writer = null;
                }
            }
        }

        public void Dispose() => Stop();

        private string PartPath(int index) =>
            Path.Combine(_sessionDir, $"replay-part{index:D3}.jsonl"); // 3-stellig: Ordinal-Sort bleibt bis part999 korrekt

        private void OpenPart(int index)
        {
            _writer?.Dispose();
            _partIndex = index;
            _bytesInPart = 0;
            _writer = new StreamWriter(PartPath(index), false, new UTF8Encoding(false), 64 * 1024);
        }

        private void WriteLine(string line)
        {
            if (_writer == null) return;
            int len = Encoding.UTF8.GetByteCount(line) + 1;
            // Rotation: Teildatei ist voll → nächster Part (KISS: keine Re-Kompression).
            // ACHTUNG: nach OpenPart() ist _writer der NEUE Writer — nie einen gecachten Ref nutzen.
            if (_bytesInPart + len > _maxBytesPerPart && _partIndex < 999)
                OpenPart(_partIndex + 1);
            _writer?.WriteLine(line);
            _bytesInPart += len;
        }

        private void WriteLine(StringBuilder sb) => WriteLine(sb.ToString());
    }

    /// <summary>
    /// FEAT-014: Replay-Player — lädt eine aufgenommene Session (alle Parts, geordnet)
    /// und liefert die deterministische Aktionssequenz für Regressionstests
    /// und Bug-Report-Debugging.
    /// </summary>
    public static class SessionReplay
    {
        public const byte KInput = 1;
        public const byte KAction = 2;
        public const byte KState = 3;

        /// <summary>Eines Replay-Events (T = relative ms seit Session-Start).</summary>
        public sealed record SessionEvent(long T, byte K, string? Label, int A, int B, int C);

        private sealed class ReplayLineDto
        {
            // KISS: Property-Namen = exakte JSONL-Schlüssel (lowercase) — System.Text.Json
            // ist case-sensitive, das vermeidet Options-Konfiguration.
            public long t { get; set; }
            public byte k { get; set; }
            public string? l { get; set; }
            public int a { get; set; }
            public int b { get; set; }
            public int c { get; set; }
        }

        /// <summary>Lädt alle Parts einer Session-Verzeichnis in deterministischer Reihenfolge.</summary>
        public static List<SessionEvent> Load(string sessionDir)
        {
            var events = new List<SessionEvent>();
            var files = Directory.GetFiles(sessionDir, "replay-part*.jsonl");
            Array.Sort(files, StringComparer.Ordinal); // part0, part1, … — deterministisch
            foreach (var file in files) LoadFile(file, events);
            return events;
        }

        /// <summary>Lädt eine einzelne JSONL-Datei anhängend an die Event-Liste.</summary>
        public static void LoadFile(string path, List<SessionEvent> events)
        {
            foreach (var line in File.ReadLines(path))
            {
                if (line.Length == 0) continue;
                var dto = JsonSerializer.Deserialize<ReplayLineDto>(line);
                if (dto == null || dto.k == 0) continue; // Header-Zeile überspringen
                events.Add(new SessionEvent(dto.t, dto.k, dto.l, dto.a, dto.b, dto.c));
            }
        }

        /// <summary>Die gefeuerte Aktionssequenz einer Session (DoD: aufzeichnen → abspielen → identisch).</summary>
        public static IReadOnlyList<string> ReplayedActions(IEnumerable<SessionEvent> events) =>
            events.Where(e => e.K == KAction).Select(e => e.Label ?? "").ToList();

        /// <summary>Die Input-Sequenz (CmdType-Namen) einer Session — für Roundtrip-Vergleich.</summary>
        public static IReadOnlyList<string> ReplayedInputs(IEnumerable<SessionEvent> events) =>
            events.Where(e => e.K == KInput).Select(e => e.Label ?? "").ToList();
    }
}
