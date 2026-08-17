using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using RagnaController.Core;
using Microsoft.Win32;
using RagnaController.Models;

namespace RagnaController
{
    public partial class MacroTimelineWindow : Window
    {
        private static readonly Lazy<LocalizationManager> _localization = new(() => LocalizationManager.Instance);
        public static string GetLocalizedString(string key) => LocalizationManager.GetLocalizedString(key);

        private Macro?              _macro;
        private double              _pxPerMs = 1.5;    // pixels per millisecond (zoom)
        private List<(Rectangle r, MacroStep s)> _rects = new();

        // ── Timeline layout constants ─────────────────────────────────────
        private const double TRACK_Y      = 20;   // top of the step blocks
        private const double TRACK_H      = 60;   // height of step blocks
        private const double RULER_Y      = 90;   // top of time ruler
        private const double LABEL_Y      = 6;    // y for key label inside block
        private const double MIN_LABEL_W  = 24;   // minimum block width to show text
        private const double CURSOR_W     = 2;
        private const double PLAYHEAD_Y   = 10;

        // Colours by step type
        private static readonly Brush KeyBrush   = new SolidColorBrush(Color.FromRgb(63,  184, 224));
        private static readonly Brush LclkBrush  = new SolidColorBrush(Color.FromRgb(74,  222, 128));
        private static readonly Brush RclkBrush  = new SolidColorBrush(Color.FromRgb(248, 113, 113));
        private static readonly Brush DelayBrush = new SolidColorBrush(Color.FromRgb(55,  65,  81));
        // Korrektur: BorderBrush umbenannt zu TimelineBorderBrush (Warnung CS0108)
        private static readonly Brush TimelineBorderBrush = new SolidColorBrush(Color.FromRgb(20,  25,  40));
        private static readonly Brush RulerBrush  = new SolidColorBrush(Color.FromRgb(74,  85, 104));
        private static readonly Brush TextBrush   = new SolidColorBrush(Color.FromRgb(226, 232, 240));

        static MacroTimelineWindow()
        {
            ((SolidColorBrush)KeyBrush).Freeze();
            ((SolidColorBrush)LclkBrush).Freeze();
            ((SolidColorBrush)RclkBrush).Freeze();
            ((SolidColorBrush)DelayBrush).Freeze();
            ((SolidColorBrush)TimelineBorderBrush).Freeze(); // Referenz angepasst
            ((SolidColorBrush)RulerBrush).Freeze();
            ((SolidColorBrush)TextBrush).Freeze();
        }

        public MacroTimelineWindow() => InitializeComponent();

        public MacroTimelineWindow(Macro macro) : this()
        {
            _macro = macro;
            Loaded += (_, _) => Render();
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Open Macro",
                Filter = "RagnaController Macro (*.json)|*.json|All Files (*.*)|*.*",
                InitialDirectory = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RagnaController", "Macros")
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                _macro = JsonSerializer.Deserialize<Macro>(File.ReadAllText(dlg.FileName));
                Render();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load macro:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (_macro == null) return;
            var recorder = new MacroRecorder();
            recorder.Play(_macro);
            Close();
        }

        private void ZoomSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _pxPerMs = e.NewValue;
            if (ZoomLabel != null) ZoomLabel.Text = $"{_pxPerMs:F1}×";
            Render();
        }

        private void Render()
        {
            TimelineCanvas.Children.Clear();
            _rects.Clear();

            if (_macro == null || _macro.Steps.Count == 0)
            {
                SubtitleText.Text = GetLocalizedString("MacroTimeline_NoSteps");
                BtnPlay.IsEnabled = false;
                return;
            }

            SubtitleText.Text  = $"{_macro.Name}  ·  {_macro.Steps.Count} steps  ·  loops: {_macro.LoopCount}";
            BtnPlay.IsEnabled  = true;
            int total          = _macro.Steps.Sum(s => s.DelayMs);
            TotalDurText.Text  = $"Total: {total} ms";

            double x = 4;
            foreach (var step in _macro.Steps)
            {
                double w = Math.Max(4, step.DelayMs * _pxPerMs);

                var rect = new Rectangle
                {
                    Width  = w - 1,
                    Height = TRACK_H,
                    Fill   = GetBrush(step.Type),
                    Stroke = TimelineBorderBrush, // Referenz angepasst
                    StrokeThickness = 1,
                    RadiusX = 3, RadiusY = 3,
                    Opacity = 0.88,
                    ToolTip = FormatTip(step)
                };

                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, TRACK_Y);
                TimelineCanvas.Children.Add(rect);
                _rects.Add((rect, step));

                if (w >= MIN_LABEL_W)
                {
                    string label = step.Type == MacroStepType.KeyPress
                        ? step.Key.ToString().Replace("VK_", "")
                        : step.Type == MacroStepType.LeftClick  ? "L"
                        : step.Type == MacroStepType.RightClick ? "R"
                        : "…";

                    var tb = new TextBlock
                    {
                        Text       = label,
                        FontSize   = 9,
                        FontWeight = FontWeights.Bold,
                        Foreground = TextBrush,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(tb, x + 3);
                    Canvas.SetTop(tb, TRACK_Y + LABEL_Y);
                    TimelineCanvas.Children.Add(tb);

                    if (w >= 50)
                    {
                        var dur = new TextBlock
                        {
                            Text       = $"{step.DelayMs}ms",
                            FontSize   = 8,
                            Foreground = RulerBrush,
                            IsHitTestVisible = false
                        };
                        Canvas.SetLeft(dur, x + 3);
                        Canvas.SetTop(dur, TRACK_Y + LABEL_Y + 12);
                        TimelineCanvas.Children.Add(dur);
                    }
                }
                x += w;
            }

            DrawRuler(total);
            TimelineCanvas.Width = Math.Max(860, x + 20);
        }

        private void DrawRuler(int totalMs)
        {
            int tickMs = _pxPerMs >= 3 ? 100 : _pxPerMs >= 1.5 ? 250 : 500;

            for (int t = 0; t <= totalMs; t += tickMs)
            {
                double x = 4 + t * _pxPerMs;

                var tick = new Line
                {
                    X1 = x, X2 = x, Y1 = RULER_Y, Y2 = RULER_Y + 6,
                    Stroke = RulerBrush, StrokeThickness = 1
                };
                TimelineCanvas.Children.Add(tick);

                var label = new TextBlock
                {
                    Text = t >= 1000 ? $"{t / 1000.0:F1}s" : $"{t}ms",
                    FontSize = 8, Foreground = RulerBrush,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(label, x + 2);
                Canvas.SetTop(label, RULER_Y + 8);
                TimelineCanvas.Children.Add(label);
            }

            var baseline = new Line
            {
                X1 = 4, X2 = 4 + totalMs * _pxPerMs,
                Y1 = RULER_Y, Y2 = RULER_Y,
                Stroke = RulerBrush, StrokeThickness = 1
            };
            TimelineCanvas.Children.Add(baseline);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            double mouseX = e.GetPosition(TimelineCanvas).X - 4;
            double cumPx  = 0;

            foreach (var step in _macro?.Steps ?? Enumerable.Empty<MacroStep>())
            {
                double w = Math.Max(4, step.DelayMs * _pxPerMs);
                if (mouseX >= cumPx && mouseX < cumPx + w)
                {
                    double startMs  = cumPx / _pxPerMs;
                    double offsetMs = (mouseX - cumPx) / _pxPerMs;
                    HoverInfo.Text =
                        $"Step {step.Index}  ·  {step.Type}  ·  Key: {step.Key}  ·  " +
                        $"Hold: {step.DelayMs} ms  ·  " +
                        $"Start: {startMs:F0} ms  ·  Cursor: +{offsetMs:F0} ms";
                    return;
                }
                cumPx += w;
            }

            HoverInfo.Text = GetLocalizedString("MacroTimeline_HoverHint");
        }

        private static Brush GetBrush(MacroStepType type) => type switch
        {
            MacroStepType.KeyPress   => KeyBrush,
            MacroStepType.LeftClick  => LclkBrush,
            MacroStepType.RightClick => RclkBrush,
            _                        => DelayBrush
        };

        private static string FormatTip(MacroStep s) =>
            $"#{s.Index} {s.Type}" +
            (s.Type == MacroStepType.KeyPress ? $" [{s.Key}]" : "") +
            $"  |  {s.DelayMs} ms";

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}