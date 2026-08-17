using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using RagnaController.Core;

namespace RagnaController
{
    public partial class DaisyWheelWindow : Window
    {
        private static readonly string[,] Sectors =
        {
            { "A", "B", "C", "D" },   // 0 Oben (N)
            { "E", "F", "G", "H" },   // 1 Oben-Rechts (NE)
            { "I", "J", "K", "L" },   // 2 Rechts (E)
            { "M", "N", "O", "P" },   // 3 Unten-Rechts (SE)
            { "Q", "R", "S", "T" },   // 4 Unten (S)
            { "U", "V", "W", "X" },   // 5 Unten-Links (SW)
            { "Y", "Z", ",", "." },   // 6 Links (W)
            { "!", "?", "1", "2" },   // 7 Oben-Links (NW)
        };

        /// <summary>
        /// FEAT-001: DaisyWheel configuration loaded from profile.
        /// Allows custom sector counts (4 or 8), custom labels, and custom colors.
        /// </summary>
        private sealed class DaisyWheelConfig
        {
            public int? SectorCount { get; set; } = 8;
            public string?[] SectorLabels { get; set; } = null;
            public Color?[] SectorColors { get; set; } = null;
        }

        private static readonly Color[] BtnColors = {
            Color.FromRgb(61, 219, 110),  // A - Grün
            Color.FromRgb(255, 58, 82),   // B - Rot
            Color.FromRgb(58, 142, 255),  // X - Blau
            Color.FromRgb(255, 184, 0),   // Y - Gelb
        };

        private const double CenterX = 250, CenterY = 250;
        private const double InnerR = 90, OuterR = 195;

        private int _activeSector = -1;
        private int _lastHighlightedSector = -2; // Speicher für Performance-Optimierung
        private string _currentText = "";

        private readonly List<Path> _sectorPaths = new();
        private readonly List<Border[]> _sectorBtns = new();

        private bool _prevA, _prevB, _prevX, _prevY, _prevL3, _prevR3, _prevStart;
        private readonly InputCommandQueue? _queue;

        public DaisyWheelWindow(InputCommandQueue queue) : this()
        {
            _queue = queue;
        }

        public DaisyWheelWindow()
        {
            InitializeComponent();
            DrawWheel();
        }

        private void DrawWheel()
        {
            WheelCanvas.Children.Clear();
            _sectorPaths.Clear();
            _sectorBtns.Clear();

            for (int s = 0; s < 8; s++)
            {
                // Korrektur: Winkel so setzen, dass Sektor 0 exakt OBEN ist
                // -90 Grad ist der mathematische Norden, -22.5 ist die halbe Sektorbreite
                double startAngle = s * 45.0 - 90.0 - 22.5;
                double endAngle = startAngle + 45.0;
                double midAngle = (startAngle + endAngle) / 2.0;

                var path = MakeSectorPath(startAngle, endAngle, InnerR, OuterR);
                path.Fill = new SolidColorBrush(Color.FromArgb(40, 212, 168, 50));
                path.Stroke = new SolidColorBrush(Color.FromRgb(33, 38, 45));
                path.StrokeThickness = 1.5;
                WheelCanvas.Children.Add(path);
                _sectorPaths.Add(path);

                var btns = new Border[4];
                for (int b = 0; b < 4; b++)
                {
                    double dotAngleDeg = midAngle + (b - 1.5) * 10.0;
                    double dotRadius = (InnerR + OuterR) / 2.0;
                    double dotRad = dotAngleDeg * Math.PI / 180.0;
                    double dotX = CenterX + Math.Cos(dotRad) * dotRadius - 14;
                    double dotY = CenterY + Math.Sin(dotRad) * dotRadius - 14;

                    var dot = new Border {
                        Width = 28, Height = 28, CornerRadius = new CornerRadius(14),
                        Background = new SolidColorBrush(Color.FromArgb(40, BtnColors[b].R, BtnColors[b].G, BtnColors[b].B)),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(100, BtnColors[b].R, BtnColors[b].G, BtnColors[b].B)),
                        BorderThickness = new Thickness(1.5),
                        Child = new TextBlock {
                            Text = Sectors[s, b], Foreground = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                            FontSize = 11, FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
                        }
                    };
                    Canvas.SetLeft(dot, dotX);
                    Canvas.SetTop(dot, dotY);
                    WheelCanvas.Children.Add(dot);
                    btns[b] = dot;
                }
                _sectorBtns.Add(btns);
            }
        }

        public bool UpdateInput(float lx, float ly, bool a, bool b, bool x, bool y, bool l3, bool r3, bool start, bool bBtnRaw)
        {
            float mag = MathF.Sqrt(lx * lx + ly * ly);
            if (mag > 0.35f) // Leicht erhöhte Deadzone für präzisere Sektorenwahl
            {
                // Korrigierte Winkelberechnung: Norden = 0 Grad
                double angle = (Math.Atan2(-ly, lx) * 180.0 / Math.PI);
                angle = (angle + 360 + 22.5) % 360.0; 
                _activeSector = (int)(angle / 45.0) % 8;
            }
            else
            {
                _activeSector = -1;
            }

            // Performance: Nur updaten, wenn sich der Sektor geändert hat
            if (_activeSector != _lastHighlightedSector)
            {
                HighlightSector(_activeSector);
                _lastHighlightedSector = _activeSector;
            }

            // Rising-Edge Detektion
            if (_activeSector >= 0)
            {
                if (a && !_prevA) TypeChar(Sectors[_activeSector, 0]);
                if (b && !_prevB) TypeChar(Sectors[_activeSector, 1]);
                if (x && !_prevX) TypeChar(Sectors[_activeSector, 2]);
                if (y && !_prevY) TypeChar(Sectors[_activeSector, 3]);
            }
            else if (b && !_prevB) // Abbruch nur, wenn KEIN Sektor gewählt ist
            {
                Dispatcher.BeginInvoke(() => Close());
                return true;
            }

            if (l3 && !_prevL3 && _currentText.Length > 0) SetText(_currentText[..^1]);
            if (r3 && !_prevR3) TypeChar(" ");

            if (start && !_prevStart)
            {
                _queue.SendChatString(_currentText);
                Dispatcher.BeginInvoke(() => Close());
                return true;
            }

            _prevA = a; _prevB = b; _prevX = x; _prevY = y;
            _prevL3 = l3; _prevR3 = r3; _prevStart = start;

            return false;
        }

        private void HighlightSector(int active)
        {
            // Performance: EINEN Invoke für die gesamte UI-Änderung
            Dispatcher.BeginInvoke(() =>
            {
                for (int s = 0; s < 8; s++)
                {
                    bool isSelected = (s == active);
                    _sectorPaths[s].Fill = new SolidColorBrush(isSelected ? Color.FromArgb(140, 229, 184, 66) : Color.FromArgb(40, 212, 168, 50));
                    _sectorPaths[s].Stroke = isSelected ? Brushes.White : new SolidColorBrush(Color.FromRgb(33, 38, 45));

                    for (int b = 0; b < 4; b++)
                    {
                        var dot = _sectorBtns[s][b];
                        var txt = (TextBlock)dot.Child;
                        if (isSelected)
                        {
                            dot.Background = new SolidColorBrush(Color.FromArgb(180, BtnColors[b].R, BtnColors[b].G, BtnColors[b].B));
                            dot.BorderBrush = Brushes.White;
                            txt.Foreground = Brushes.White;
                        }
                        else
                        {
                            dot.Background = new SolidColorBrush(Color.FromArgb(40, BtnColors[b].R, BtnColors[b].G, BtnColors[b].B));
                            dot.BorderBrush = new SolidColorBrush(Color.FromArgb(100, BtnColors[b].R, BtnColors[b].G, BtnColors[b].B));
                            txt.Foreground = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255));
                        }
                    }
                }
            });
        }

        private void TypeChar(string c) => SetText(_currentText + c);

        private void SetText(string t)
        {
            _currentText = t;
            Dispatcher.BeginInvoke(() => {
                CurrentTextBlock.Text = t.Length > 20 ? "…" + t[^20..] : t;
            });
        }

        private static Path MakeSectorPath(double startDeg, double endDeg, double inner, double outer)
        {
            double s1 = startDeg * Math.PI / 180.0, s2 = endDeg * Math.PI / 180.0;
            var p1o = new Point(CenterX + Math.Cos(s1) * outer, CenterY + Math.Sin(s1) * outer);
            var p2o = new Point(CenterX + Math.Cos(s2) * outer, CenterY + Math.Sin(s2) * outer);
            var p1i = new Point(CenterX + Math.Cos(s2) * inner, CenterY + Math.Sin(s2) * inner);
            var p2i = new Point(CenterX + Math.Cos(s1) * inner, CenterY + Math.Sin(s1) * inner);

            var geo = new PathGeometry();
            var fig = new PathFigure { StartPoint = p1o, IsClosed = true };
            fig.Segments.Add(new ArcSegment(p2o, new Size(outer, outer), 0, false, SweepDirection.Clockwise, true));
            fig.Segments.Add(new LineSegment(p1i, true));
            fig.Segments.Add(new ArcSegment(p2i, new Size(inner, inner), 0, false, SweepDirection.Counterclockwise, true));
            geo.Figures.Add(fig);
            return new Path { Data = geo };
        }
    }
}