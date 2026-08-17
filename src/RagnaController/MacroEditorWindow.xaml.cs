using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Text.Json;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController
{
    public partial class MacroEditorWindow : Window
    {
        private Macro _macro = null!;
        private readonly string _path;
        private ObservableCollection<MacroStepVM> _steps = null!;

        /// <summary>Ergebnis nach ShowDialog() — nur gesetzt wenn OK geklickt.</summary>
        public Macro? EditedMacro { get; private set; }

        // Öffnet eine Macro-Datei über Pfad
        public MacroEditorWindow(string path)
        {
            InitializeComponent();
            _path = path;
            Load();
        }

        // Öffnet ein bestehendes Macro-Objekt (aus MacroRecorder)
        public MacroEditorWindow(Macro macro)
        {
            InitializeComponent();
            _path = string.Empty;
            _macro = macro;
            LoadFromObject();
        }

        private void LoadFromObject()
        {
            if (_macro == null) return;
            
            NameText.Text = _macro.Name;
            LoopCountText.Text = _macro.LoopCount.ToString();
            _steps = new ObservableCollection<MacroStepVM>(_macro.Steps.Select((s, i) => new MacroStepVM(i + 1, s)));
            StepsGrid.ItemsSource = _steps;
            UpdateDur();
        }

        private void Load()
        {
            try {
                _macro = File.Exists(_path) ? JsonSerializer.Deserialize<Macro>(File.ReadAllText(_path)) ?? new Macro() : new Macro();
                if (_macro == null) { Close(); return; }
                
                NameText.Text = _macro.Name;
                LoopCountText.Text = _macro.LoopCount.ToString();
                _steps = new ObservableCollection<MacroStepVM>(_macro.Steps.Select((s, i) => new MacroStepVM(i + 1, s)));
                StepsGrid.ItemsSource = _steps;
                UpdateDur();
            } catch { Close(); }
        }

        private void UpdateDur() { if (_steps != null) DurationText.Text = $"Duration: {_steps.Sum(s => s.DelayMs)}ms"; }
        private void Reindex() { for (int i = 0; i < _steps.Count; i++) _steps[i].Index = i + 1; }
        private void BtnMoveUp_Click(object sender, RoutedEventArgs e) { if (sender is Button b && b.Tag is MacroStepVM s) { int i = _steps.IndexOf(s); if (i > 0) { _steps.Move(i, i - 1); Reindex(); } } }
        private void BtnMoveDown_Click(object sender, RoutedEventArgs e) { if (sender is Button b && b.Tag is MacroStepVM s) { int i = _steps.IndexOf(s); if (i < _steps.Count - 1) { _steps.Move(i, i + 1); Reindex(); } } }
        private void BtnDeleteStep_Click(object sender, RoutedEventArgs e) { if (sender is Button b && b.Tag is MacroStepVM s) { _steps.Remove(s); Reindex(); UpdateDur(); } }
        private void BtnSpeedUp_Click(object sender, RoutedEventArgs e) { foreach (var s in _steps) s.DelayMs = Math.Max(30, s.DelayMs / 2); UpdateDur(); }
        private void BtnSlowDown_Click(object sender, RoutedEventArgs e) { foreach (var s in _steps) s.DelayMs *= 2; UpdateDur(); }
        private void BtnOptimize_Click(object sender, RoutedEventArgs e) { var opt = _steps.Where(s => s.DelayMs >= 30).ToList(); _steps.Clear(); foreach (var s in opt) _steps.Add(s); Reindex(); UpdateDur(); }
        private void BtnAddStep_Click(object sender, RoutedEventArgs e) { var s = new MacroStepVM(_steps.Count + 1, new MacroStep { Type = MacroStepType.Delay, DelayMs = 200 }); _steps.Add(s); UpdateDur(); StepsGrid.ScrollIntoView(s); }
        private void BtnPreview_Click(object sender, RoutedEventArgs e) { MessageBox.Show(string.Join("\n", _steps.Take(10).Select(s => $"{s.Index}. {s.Type} ({s.DelayMs}ms)"))); }
        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
        private void BtnSave_Click(object sender, RoutedEventArgs e) {
            try
            {
                _macro.Name      = NameText?.Text ?? _macro.Name;
                _macro.LoopCount = int.TryParse(LoopCountText?.Text, out int l) ? Math.Max(0, l) : 1;
                _macro.Steps     = _steps?.Select(v => new MacroStep { Type = v.Type, Key = v.Key, DelayMs = v.DelayMs }).ToList() ?? new();
                if (!string.IsNullOrEmpty(_path))
                    File.WriteAllText(_path, JsonSerializer.Serialize(_macro, new JsonSerializerOptions { WriteIndented = true }));
                EditedMacro  = _macro;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Save failed: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    public class MacroStepVM : System.ComponentModel.INotifyPropertyChanged {
        private int _idx, _ms;
        public int Index { get => _idx; set { _idx = value; PropertyChanged?.Invoke(this, new(nameof(Index))); } }
        public MacroStepType Type { get; set; }
        public VirtualKey Key { get; set; }
        public int DelayMs { get => _ms; set { _ms = value; PropertyChanged?.Invoke(this, new(nameof(DelayMs))); } }
        public MacroStepVM(int i, MacroStep s) { _idx = i; Type = s.Type; Key = s.Key; _ms = s.DelayMs; }
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
}