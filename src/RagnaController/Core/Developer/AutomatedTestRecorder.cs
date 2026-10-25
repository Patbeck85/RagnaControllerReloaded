using System;
using System.Collections.Generic;
using System.Linq;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Core.Developer {
    /// <summary>
    /// TASK-2905: AutomatedTestRecorder — Zeichnet User-Aktionen auf und generiert
    /// Integration-Tests aus den aufgezeichneten Sequenzen.
    ///
    /// White-Hat: Nur Aufzeichnung von UI-Aktionen für Test-Generierung,
    /// keine automatische Spielsteuerung.
    /// </summary>
    public sealed class AutomatedTestRecorder
    {
        private bool _isRecording = false;
        private readonly List<TestAction> _recordedActions = new();
        private DateTime? _recordingStartTime;

        /// <summary>Ob gerade aufgezeichnet wird.</summary>
        public bool IsRecording => _isRecording;

        /// <summary>Aufgezeichnete Aktionen.</summary>
        public IReadOnlyList<TestAction> RecordedActions => _recordedActions;

        /// <summary>Startet die Aufzeichnung.</summary>
        public void StartRecording()
        {
            _isRecording = true;
            _recordingStartTime = DateTime.UtcNow;
            _recordedActions.Clear();
        }

        /// <summary>Stoppt die Aufzeichnung.</summary>
        public void StopRecording()
        {
            _isRecording = false;
            _recordingStartTime = null;
        }

        /// <summary>Registriert eine User-Aktion während der Aufzeichnung.</summary>
        public void RecordAction(string actionType, string target, object? value = null)
        {
            if (!_isRecording) return;

            var action = new TestAction
            {
                TimestampMs = _recordingStartTime.HasValue
                    ? (DateTime.UtcNow - _recordingStartTime.Value).TotalMilliseconds
                    : 0,
                ActionType = actionType,
                Target = target,
                Value = value?.ToString() ?? "",
            };
            _recordedActions.Add(action);
        }

        /// <summary>Generiert xUnit-Test-Code aus den aufgezeichneten Aktionen.</summary>
        public string GenerateTestCode(string testClassName, string testMethodName)
        {
            var lines = new List<string>();
            lines.Add($"[Fact]");
            lines.Add($"public void {testMethodName}()");
            lines.Add("{");
            lines.Add($"    // Arrangiert aus aufgezeichneter Session");
            lines.Add($"    var queue = new InputCommandQueue();");

            foreach (var action in _recordedActions)
            {
                lines.Add($"    // {action.ActionType} on {action.Target} at {action.TimestampMs:F0}ms");
                switch (action.ActionType.ToLowerInvariant())
                {
                    case "buttonclick":
                    case "click":
                        lines.Add($"    queue.LeftClick();");
                        break;
                    case "rightclick":
                        lines.Add($"    queue.RightClick();");
                        break;
                    case "doubleclick":
                        lines.Add($"    queue.DoubleClick();");
                        break;
                    case "keydown":
                        lines.Add($"    queue.KeyDown(VirtualKey.{action.Target});");
                        break;
                    case "keyup":
                        lines.Add($"    queue.KeyUp(VirtualKey.{action.Target});");
                        break;
                    case "mousemove":
                        lines.Add($"    queue.MouseMoveRelative({action.Value});");
                        break;
                    default:
                        lines.Add($"    // {action.ActionType} on {action.Target} = {action.Value}");
                        break;
                }
            }

            lines.Add($"    // Assert: Prüfe erwartete Ergebnisse");
            lines.Add($"    Assert.True(queue != null);");
            lines.Add($"}}");

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>Löscht alle aufgezeichneten Aktionen.</summary>
        public void Clear()
        {
            _recordedActions.Clear();
        }
    }

    /// <summary>
    /// Eine aufgezeichnete Test-Aktion.
    /// </summary>
    public class TestAction
    {
        public double TimestampMs { get; set; }
        public string ActionType { get; set; } = "";
        public string Target { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
