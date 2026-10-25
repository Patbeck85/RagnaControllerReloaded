using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RagnaController.Core.Services
{
    /// <summary>
    /// TASK-3204: ReplaySystem — Session-Aufzeichnung und Export.
    ///
    /// Zeichnet Spielsessions auf für Guides, Reviews und Analysen:
    ///   • Eingaben aufzeichnen (Tasten, Maus, Controller)
    ///   • Spielzustand speichern (Position, HP, SP, Level)
    ///   • JSON-Export für spätere Wiedergabe/Analyse
    ///   • Session-Metadaten (Datum, Dauer, Karte, Charakter)
    ///
    /// White-Hat: Reine Aufzeichnung und Export, keine automatische Wiedergabe/Steuerung.
    /// </summary>
    public sealed class ReplaySystem : IDisposable
    {
        private readonly string _recordingsDirectory;
        private ReplaySession? _currentSession;
        private bool _isRecording = false;
        private bool _disposed = false;

        /// <summary>Ob gerade aufgezeichnet wird.</summary>
        public bool IsRecording => _isRecording;

        /// <summary>Aktuelle Aufzeichnung (null wenn keine läuft).</summary>
        public ReplaySession? CurrentSession => _currentSession;

        /// <summary>Event bei Session-Start.</summary>
        public event EventHandler<ReplaySession>? SessionStarted;

        /// <summary>Event bei Session-Ende.</summary>
        public event EventHandler<ReplaySession>? SessionStopped;

        public ReplaySystem(string? recordingsDirectory = null)
        {
            _recordingsDirectory = recordingsDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RagnaController", "Replays");
            Directory.CreateDirectory(_recordingsDirectory);
        }

        /// <summary>Startet eine neue Session-Aufzeichnung.</summary>
        public ReplaySession StartRecording(string characterName, string mapName, string? notes = null)
        {
            if (_isRecording)
                throw new InvalidOperationException("Eine Aufzeichnung läuft bereits.");

            _currentSession = new ReplaySession
            {
                SessionId = Guid.NewGuid().ToString("N"),
                StartedAt = DateTime.UtcNow,
                CharacterName = characterName,
                MapName = mapName,
                Notes = notes,
                InputEvents = new List<ReplayInputEvent>(),
                StateEvents = new List<ReplayStateEvent>(),
            };
            _isRecording = true;

            SessionStarted?.Invoke(this, _currentSession);
            return _currentSession;
        }

        /// <summary>Stoppt die aktuelle Aufzeichnung und gibt die Session zurück.</summary>
        public ReplaySession? StopRecording()
        {
            if (!_isRecording || _currentSession == null)
                return null;

            _currentSession.StoppedAt = DateTime.UtcNow;
            _currentSession.DurationSeconds = (_currentSession.StoppedAt.Value - _currentSession.StartedAt).TotalSeconds;
            _isRecording = false;

            var session = _currentSession;
            _currentSession = null;

            SessionStopped?.Invoke(this, session);
            return session;
        }

        /// <summary>Registriert einen Input-Event während der Aufzeichnung.</summary>
        public void RecordInput(string inputType, string inputData, double timestampMs)
        {
            if (!_isRecording || _currentSession == null)
                return;

            _currentSession.InputEvents.Add(new ReplayInputEvent
            {
                TimestampMs = timestampMs,
                InputType = inputType,
                InputData = inputData,
            });
        }

        /// <summary>Registriert einen Spielzustand-Event während der Aufzeichnung.</summary>
        public void RecordState(string stateType, string stateData, double timestampMs)
        {
            if (!_isRecording || _currentSession == null)
                return;

            _currentSession.StateEvents.Add(new ReplayStateEvent
            {
                TimestampMs = timestampMs,
                StateType = stateType,
                StateData = stateData,
            });
        }

        /// <summary>Speichert eine Session als JSON-Datei.</summary>
        public string SaveSession(ReplaySession session, string? fileName = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = $"replay_{session.SessionId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

            var filePath = Path.Combine(_recordingsDirectory, fileName);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            var json = JsonSerializer.Serialize(session, options);
            File.WriteAllText(filePath, json);
            return filePath;
        }

        /// <summary>Lädt eine Session aus einer JSON-Datei.</summary>
        public ReplaySession? LoadSession(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ReplaySession>(json);
        }

        /// <summary>Gibt alle gespeicherten Session-Dateien zurück.</summary>
        public List<string> GetSavedSessions()
        {
            if (!Directory.Exists(_recordingsDirectory))
                return new List<string>();

            return Directory.GetFiles(_recordingsDirectory, "*.json")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToList();
        }

        /// <summary>Löscht eine gespeicherte Session.</summary>
        public bool DeleteSession(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            File.Delete(filePath);
            return true;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (_isRecording && _currentSession != null)
                {
                    _currentSession.StoppedAt = DateTime.UtcNow;
                    _currentSession.DurationSeconds = (_currentSession.StoppedAt.Value - _currentSession.StartedAt).TotalSeconds;
                }
            }
        }
    }

    /// <summary>
    /// Repräsentator eine aufgezeichnete Spielsession.
    /// </summary>
    public class ReplaySession
    {
        public string SessionId { get; set; } = "";
        public DateTime StartedAt { get; set; }
        public DateTime? StoppedAt { get; set; }
        public double DurationSeconds { get; set; }
        public string CharacterName { get; set; } = "";
        public string MapName { get; set; } = "";
        public string? Notes { get; set; }
        public List<ReplayInputEvent> InputEvents { get; set; } = new();
        public List<ReplayStateEvent> StateEvents { get; set; } = new();
    }

    /// <summary>
    /// Ein aufgezeichneter Input-Event.
    /// </summary>
    public class ReplayInputEvent
    {
        public double TimestampMs { get; set; }
        public string InputType { get; set; } = "";
        public string InputData { get; set; } = "";
    }

    /// <summary>
    /// Ein aufgezeichneter Spielzustand-Event.
    /// </summary>
    public class ReplayStateEvent
    {
        public double TimestampMs { get; set; }
        public string StateType { get; set; } = "";
        public string StateData { get; set; } = "";
    }
}
