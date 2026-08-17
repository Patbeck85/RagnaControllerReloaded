using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using RagnaController.Core;
using RagnaController.Models; // WICHTIG: Behebt den Fehler CS0246

namespace RagnaController
{
    public partial class MacroRecorderWindow : Window, IComponentConnector
    {
        private readonly MacroRecorder _recorder = new MacroRecorder();
        
        // Diese Eigenschaft hat den Fehler verursacht:
        public Macro? RecordedMacro { get; private set; }

        public MacroRecorderWindow()
        {
            InitializeComponent();
            
            _recorder.StepRecorded += (s) => Dispatcher.BeginInvoke(() => StepsList.Items.Add(s));
            _recorder.RecordingStopped += () => Dispatcher.BeginInvoke(() => {
                BtnSave.IsEnabled = true;
                BtnStop.IsEnabled = false;
                BtnRecord.IsEnabled = true;
            });
        }

        public void Connect(int targetId, object connector)
        {
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_recorder.IsRecording)
            {
                if (e.Key == Key.Escape) return;
                VirtualKey vk = (VirtualKey)KeyInterop.VirtualKeyFromKey(e.Key);
                _recorder.RecordKey(vk);
                e.Handled = true;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        // Event Handler für Buttons
        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();
        private void BtnRecord_Click(object sender, RoutedEventArgs e)
        {
            if (!_recorder.IsRecording)
            {
                _recorder.StartRecording();
                BtnRecord.IsEnabled = false;
                BtnStop.IsEnabled = true;
            }
        }
        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _recorder.StopRecording();
            BtnRecord.IsEnabled = true;
            BtnStop.IsEnabled = false;
        }
        private void BtnClear_Click(object sender, RoutedEventArgs e) => ClearSteps();
        private void BtnEdit_Click(object sender, RoutedEventArgs e) => OpenInEditor();
        private void BtnSave_Click(object sender, RoutedEventArgs e) => SaveMacro();

        private void ClearSteps()
        {
            StepsList?.Items.Clear();
        }

        private void OpenInEditor()
        {
            if (!_recorder.IsRecording && StepsList?.Items.Count > 0)
            {
                try
                {
                    string name = $"Macro_{DateTime.Now:HHmmss}";
                    var macro = _recorder.GetRecordedMacro(name);
                    MacroRecorder.SaveMacro(macro);
                    // Compute the path where SaveMacro writes to
                    string dir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "RagnaController", "Macros");
                    string safeName = string.Concat(name.Split(System.IO.Path.GetInvalidFileNameChars()));
                    string path = System.IO.Path.Combine(dir, safeName + ".json");
                    if (System.IO.File.Exists(path))
                    {
                        var editor = new MacroEditorWindow(path);
                        editor.ShowDialog();
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Open in editor failed: {ex.Message}");
                }
            }
        }

        private void SaveMacro()
        {
            try
            {
                string name = TitleText?.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(name)) name = $"Macro_{DateTime.Now:HHmmss}";
                var macro = _recorder.GetRecordedMacro(name);
                MacroRecorder.SaveMacro(macro);
                ShowStatus($"Saved: {name}");
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Save failed: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ShowStatus(string msg)
        {
            if (StatusText != null) StatusText.Text = msg;
        }
    }
}