using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using RagnaController.Core;
using RagnaController.Profiles;
using RagnaController.Core.Hardware;

namespace RagnaController.Views.Dialogs
{
    /// <summary>
    /// TASK-2404: RumbleEditorWindow — Editor für benutzerdefinierte Rumble-Patterns.
    /// Ermöglicht das Erstellen, Bearbeiten und Löschen von Rumble-Patterns
    /// (Frequency, Amplitude, Duration, Waveform) für den DualSense-Controller.
    /// </summary>
    public partial class RumbleEditorWindow : Window
    {
        private readonly Profile _profile;
        private RumblePattern? _selectedPattern;
        private readonly List<RumblePattern> _workingPatterns;

        public RumbleEditorWindow(Profile profile)
        {
            InitializeComponent();
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _workingPatterns = new List<RumblePattern>(profile.RumblePatterns.Select(p => p.Clone()));

            InitializeWaveformCombo();
            RefreshPatternList();
        }

        private void InitializeWaveformCombo()
        {
            CmbWaveform.Items.Clear();
            foreach (var waveform in Enum.GetValues<WaveformType>())
            {
                CmbWaveform.Items.Add(new ComboBoxItem
                {
                    Content = waveform.ToString(),
                    Tag = waveform,
                });
            }
            CmbWaveform.SelectedIndex = 0;
        }

        private void RefreshPatternList()
        {
            PatternList.Items.Clear();
            foreach (var pattern in _workingPatterns)
            {
                PatternList.Items.Add(pattern);
            }
            if (_workingPatterns.Count > 0 && PatternList.SelectedIndex < 0)
            {
                PatternList.SelectedIndex = 0;
            }
        }

        private void PatternList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatternList.SelectedItem is RumblePattern pattern)
            {
                _selectedPattern = pattern;
                TxtName.Text = pattern.Name;
                TxtDescription.Text = pattern.Description;
                TxtFrequency.Text = pattern.Frequency.ToString("F1");
                TxtAmplitude.Text = pattern.Amplitude.ToString("P0");
                TxtDuration.Text = pattern.DurationMs.ToString();
                ChkLoop.IsChecked = pattern.Loop;

                // Wellenform auswählen
                foreach (ComboBoxItem item in CmbWaveform.Items)
                {
                    if ((WaveformType)item.Tag == pattern.Waveform)
                    {
                        CmbWaveform.SelectedItem = item;
                        break;
                    }
                }

                BtnDelete.IsEnabled = true;
                DrawWaveformPreview(pattern);
            }
            else
            {
                _selectedPattern = null;
                ClearEditFields();
                BtnDelete.IsEnabled = false;
            }
        }

        private void ClearEditFields()
        {
            TxtName.Text = "";
            TxtDescription.Text = "";
            TxtFrequency.Text = "10.0";
            TxtAmplitude.Text = "50%";
            TxtDuration.Text = "200";
            ChkLoop.IsChecked = false;
            WaveformCanvas.Children.Clear();
        }

        private void DrawWaveformPreview(RumblePattern pattern)
        {
            WaveformCanvas.Children.Clear();

            if (WaveformCanvas.ActualWidth == 0 || WaveformCanvas.ActualHeight == 0)
                return;

            double width = WaveformCanvas.ActualWidth;
            double height = WaveformCanvas.ActualHeight;
            double centerY = height / 2;
            double amplitude = height * 0.3 * pattern.Amplitude;
            int samples = 100;

            var points = new Point[samples];
            for (int i = 0; i < samples; i++)
            {
                double t = (double)i / (samples - 1);
                double phase = t * pattern.Frequency * 4 * Math.PI; // 2 volle Zyklen bei 1 Hz

                double y = pattern.Waveform switch
                {
                    WaveformType.Sine => Math.Sin(phase),
                    WaveformType.Square => Math.Sin(phase) >= 0 ? 1 : -1,
                    WaveformType.Sawtooth => 2 * (t - Math.Floor(t + 0.5)),
                    WaveformType.Pulse => Math.Sin(phase) > 0.5 ? 1 : -1,
                    WaveformType.DoublePulse => Math.Sin(phase) > 0.5 || Math.Sin(phase + Math.PI) > 0.5 ? 1 : -1,
                    _ => Math.Sin(phase),
                };

                points[i] = new Point(t * width, centerY - y * amplitude);
            }

            // Linie zeichnen
            for (int i = 1; i < points.Length; i++)
            {
                var line = new Line
                {
                    X1 = points[i - 1].X,
                    Y1 = points[i - 1].Y,
                    X2 = points[i].X,
                    Y2 = points[i].Y,
                    Stroke = new SolidColorBrush(Color.FromRgb(229, 184, 66)), // Gold
                    StrokeThickness = 2,
                };
                WaveformCanvas.Children.Add(line);
            }

            // Mittelinie
            var centerLine = new Line
            {
                X1 = 0,
                Y1 = centerY,
                X2 = width,
                Y2 = centerY,
                Stroke = new SolidColorBrush(Color.FromRgb(85, 94, 106)), // TextTertiary
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection(new[] { 4.0, 4.0 }),
            };
            WaveformCanvas.Children.Add(centerLine);
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var newPattern = new RumblePattern
            {
                Name = "Neues Pattern",
                Description = "",
                Waveform = WaveformType.Sine,
                Frequency = 10.0f,
                Amplitude = 0.5f,
                DurationMs = 200,
                Loop = false,
                RepeatCount = 1,
            };
            _workingPatterns.Add(newPattern);
            RefreshPatternList();
            PatternList.SelectedItem = newPattern;
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPattern == null) return;

            if (MessageBox.Show($"Pattern '{_selectedPattern.Name}' wirklich löschen?", "Löschen bestätigen",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _workingPatterns.Remove(_selectedPattern);
                _selectedPattern = null;
                RefreshPatternList();
                ClearEditFields();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Aktuelles Pattern aktualisieren falls ausgewählt
            if (_selectedPattern != null)
            {
                UpdatePatternFromFields(_selectedPattern);
            }

            // Alle Patterns ins Profil schreiben
            _profile.RumblePatterns.Clear();
            foreach (var pattern in _workingPatterns)
            {
                _profile.RumblePatterns.Add(pattern.Clone());
            }

            DialogResult = true;
            Close();
        }

        private void UpdatePatternFromFields(RumblePattern pattern)
        {
            pattern.Name = TxtName.Text.Trim();
            pattern.Description = TxtDescription.Text.Trim();

            if (float.TryParse(TxtFrequency.Text.Replace("%", "").Trim(), out float freq))
                pattern.Frequency = Math.Clamp(freq, 1.0f, 100.0f);

            if (float.TryParse(TxtAmplitude.Text.Replace("%", "").Trim(), out float amp))
                pattern.Amplitude = Math.Clamp(amp / 100.0f, 0.0f, 1.0f);

            if (int.TryParse(TxtDuration.Text.Trim(), out int dur))
                pattern.DurationMs = Math.Clamp(dur, 50, 5000);

            pattern.Loop = ChkLoop.IsChecked == true;

            if (CmbWaveform.SelectedItem is ComboBoxItem item)
                pattern.Waveform = (WaveformType)item.Tag;
        }

        // TextBox Änderungen sofort anwenden
        private void TxtName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedPattern != null)
                _selectedPattern.Name = TxtName.Text;
            RefreshPatternList();
        }

        private void TxtDescription_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedPattern != null)
                _selectedPattern.Description = TxtDescription.Text;
            RefreshPatternList();
        }

        private void TxtFrequency_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedPattern != null && float.TryParse(TxtFrequency.Text.Replace("%", "").Trim(), out float val))
                _selectedPattern.Frequency = Math.Clamp(val, 1.0f, 100.0f);
            DrawWaveformPreview(_selectedPattern);
        }

        private void TxtAmplitude_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedPattern != null && float.TryParse(TxtAmplitude.Text.Replace("%", "").Trim(), out float val))
                _selectedPattern.Amplitude = Math.Clamp(val / 100.0f, 0.0f, 1.0f);
            DrawWaveformPreview(_selectedPattern);
        }

        private void TxtDuration_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedPattern != null && int.TryParse(TxtDuration.Text.Trim(), out int val))
                _selectedPattern.DurationMs = Math.Clamp(val, 50, 5000);
        }

        private void ChkLoop_Checked(object sender, RoutedEventArgs e)
        {
            if (_selectedPattern != null)
                _selectedPattern.Loop = ChkLoop.IsChecked == true;
        }

        private void CmbWaveform_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedPattern != null && CmbWaveform.SelectedItem is ComboBoxItem item)
            {
                _selectedPattern.Waveform = (WaveformType)item.Tag;
                DrawWaveformPreview(_selectedPattern);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
