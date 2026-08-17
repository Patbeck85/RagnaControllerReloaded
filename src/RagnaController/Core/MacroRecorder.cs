using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using RagnaController.Models; // Nutzt die Definition aus Models

namespace RagnaController.Core
{
    public class MacroRecorder
    {
        public bool IsRecording { get; private set; }
        public bool IsPlaying { get; private set; }

        private readonly List<MacroStep> _recordedSteps = new List<MacroStep>();
        private readonly Stopwatch _recordingTimer = new Stopwatch();

        private Macro? _activeMacro;
        private int _currentStepIndex;
        private int _msUntilNextStep;
        private int _remainingLoops;

        public event Action? RecordingStarted;
        public event Action? RecordingStopped;
        public event Action<MacroStep>? StepRecorded;

        private readonly InputCommandQueue _queue;

        public MacroRecorder(InputCommandQueue queue)
        {
            _queue = queue;
        }

        public MacroRecorder() : this(new InputCommandQueue())
        {
        }

        public void Start() {
            _recordedSteps.Clear();
            IsRecording = true;
            _recordingTimer.Restart();
            RecordingStarted?.Invoke();
        }

        public void StartRecording() {
            Start();
        }

        public void StopRecording() {
            Stop();
        }

        public void Stop() {
            IsRecording = false;
            _recordingTimer.Stop();
            RecordingStopped?.Invoke();
        }

        public void Stop(string name) {
            IsRecording = false;
            _recordingTimer.Stop();
            RecordingStopped?.Invoke();
        }

        public void RecordKey(VirtualKey k) {
            if (!IsRecording) return;
            int elapsed = (int)_recordingTimer.ElapsedMilliseconds;
            _recordingTimer.Restart();

            var step = new MacroStep {
                Index = _recordedSteps.Count + 1,
                Type = MacroStepType.KeyPress,
                Key = k,
                // FIX: Wenn es der allererste Schritt im Makro ist, muss das Delay zwingend auf ein Minimum (10ms) gesetzt werden
                // so that nothing happens for 4 seconds during playback before it finally starts
                DelayMs = _recordedSteps.Count == 0 ? 10 : Math.Max(elapsed, 10)
            };
            _recordedSteps.Add(step);
            StepRecorded?.Invoke(step);
        }

        public Macro GetRecordedMacro(string name) {
            return new Macro {
                Name = name,
                Steps = new List<MacroStep>(_recordedSteps)
            };
        }

        public void Play(Macro m, int loops = 1) {
            _activeMacro = m;
            _remainingLoops = loops;
            _currentStepIndex = 0;
            _msUntilNextStep = 0;
            IsPlaying = true;
        }

        public void StopPlayback() {
            IsPlaying = false;
            _activeMacro = null;
            _currentStepIndex = 0;
            _msUntilNextStep = 0;
        }

        public void UpdatePlayback(int deltaMs) {
            if (!IsPlaying || _activeMacro == null) return;
            _msUntilNextStep -= deltaMs;
            if (_msUntilNextStep <= 0) {
                var step = _activeMacro.Steps[_currentStepIndex];
                // FIX: InputSimulator is a static class - call methods directly without null check
                if (step.Type == MacroStepType.KeyPress)        _queue.TapKey(step.Key);
                else if (step.Type == MacroStepType.LeftClick)  _queue.LeftClick();
                else if (step.Type == MacroStepType.RightClick) _queue.RightClick();

                _currentStepIndex++;
                if (_currentStepIndex >= _activeMacro.Steps.Count) {
                    _remainingLoops--;
                    if (_remainingLoops > 0) {
                        _currentStepIndex = 0;
                        _msUntilNextStep = _activeMacro.Steps[0].DelayMs;
                    }
                    else IsPlaying = false;
                }
                else _msUntilNextStep = _activeMacro.Steps[_currentStepIndex].DelayMs + _msUntilNextStep; // FIX: Überhang anrechnen (negativ = Zeitgewinn)
            }
        }

        public static void SaveMacro(Macro m) {
            try {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RagnaController", "Macros");
                Directory.CreateDirectory(dir);
                // Sanitize name to prevent path traversal
                string safeName = string.Concat(m.Name.Split(Path.GetInvalidFileNameChars()));
                if (string.IsNullOrWhiteSpace(safeName)) safeName = "Unnamed";
                File.WriteAllText(Path.Combine(dir, safeName + ".json"), JsonSerializer.Serialize(m));
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"[MacroRecorder] SaveMacro failed: {ex.Message}");
            }
        }

        public static Macro? LoadMacro(string path) {
            if (!File.Exists(path)) return null;
            try {
                return JsonSerializer.Deserialize<Macro>(File.ReadAllText(path));
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"[MacroRecorder] LoadMacro failed: {ex.Message}");
                return null;
            }
        }
    }
}
