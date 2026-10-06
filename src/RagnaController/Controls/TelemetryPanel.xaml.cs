using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using RagnaController.Core;

namespace RagnaController.Controls
{
    /// <summary>
    /// UI-011: Live-Telemetrie-Dashboard (Developer-Tab).
    /// Zeigt thread-safere Snapshots der Perf-Tracker in Echtzeit (2 Hz):
    /// Input-Latenz, Memory/GC, Frame-Budget, GPU-Overlay.
    /// TRACKER-APIs sind Interlocked-basiert → keine UI-Blockaden (PERF-001).
    /// </summary>
    public partial class TelemetryPanel : UserControl
    {
        // ── Theme colors (konsistent mit MainWindow) ───────────────────────
        private static readonly Color CfgText = Color.FromRgb(161, 176, 197);   // #A1B0C5
        private static readonly Color CfgMuted = Color.FromRgb(85, 94, 106);    // #555E6A
        private static readonly Color CfgGood = Color.FromRgb(57, 255, 20);     // #39FF14
        private static readonly Color CfgWarn = Color.FromRgb(229, 184, 66);    // #E5B842
        private static readonly Color CfgBad = Color.FromRgb(255, 58, 82);      // #FF3A52

        /// <summary>PERF-001: 2 Hz Refresh — Interlocked-Snapshots, O(kleine Kartenanzahl).</summary>
        private const int RefreshIntervalMs = 500;

        /// <summary>PERF-005 Ziel: P99 End-to-End Latenz ≤ 5 ms.</summary>
        private const double LatencyTargetMs = 5.0;

        private readonly DispatcherTimer _timer;
        private HybridEngine? _engine;

        public TelemetryPanel()
        {
            InitializeComponent();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(RefreshIntervalMs) };
            _timer.Tick += (_, _) => RefreshNow();

            // Timer nur laufen lassen, wenn Panel sichtbar ist (PERF-001: keine UI-Blockaden im Hintergrund).
            IsVisibleChanged += (_, e) =>
            {
                bool visible = e.NewValue is bool b && b;
                if (visible) _timer.Start(); else _timer.Stop();
            };
        }

        /// <summary>
        /// Verdrahtet die Engine (einmalig beim MainWindow-Start).
        /// Startet sofort einen Refresh, falls ein Tracker vorhanden ist.
        /// </summary>
        public void SetEngine(HybridEngine? engine)
        {
            _engine = engine;
            if (engine != null) RefreshNow();
        }

        /// <summary>Stoppt den Timer explizit (Window_Closing, MEMORY-001).</summary>
        public void StopUpdates() => _timer.Stop();

        private void RefreshNow()
        {
            try
            {
                // THREAD-001: DispatcherTimer läuft auf dem UI-Thread; Tracker-Snapshots sind Interlocked-basiert.
                UpdateTelemetry(_engine?.LatencyTracker, _engine?.MemoryTracker);
            }
            catch (Exception ex)
            {
                // ERROR-001: Kein stilles Verschlucken — Fehler in die Karte zeigen, Timer läuft weiter.
                try
                {
                    var err = new TextBlock
                    {
                        Text = $"Refresh-Fehler: {ex.GetType().Name}: {ex.Message}",
                        Foreground = new SolidColorBrush(CfgBad),
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 9,
                        Margin = new Thickness(0, 4, 0, 0)
                    };
                    CardStack.Children.Add(err);
                }
                catch (Exception renderEx)
                {
                    // Panel-Rendering-Fehler dürfen den Timer nicht killen — aber loggen
                    System.Diagnostics.Debug.WriteLine($"[TelemetryPanel] Render error: {renderEx.Message}");
                }
            }
        }

        /// <summary>
        /// Füllt alle vier Karten aus den Tracker-Snapshots (public für Unit-Tests, thread-safe).
        /// </summary>
        public void UpdateTelemetry(InputLatencyTracker? latency, MemoryAllocationTracker? memory)
        {
            CardStack.Children.Clear();
            CardStack.Children.Add(BuildLatencyCard(latency));
            CardStack.Children.Add(BuildMemoryCard(memory));
            CardStack.Children.Add(BuildFrameBudgetCard());
            CardStack.Children.Add(BuildGpuOverlayCard());

            UpdateStamp.Text = $"updated {DateTime.Now:HH:mm:ss}";
        }

        // ── Card 1: Input Latency (PERF-005) ────────────────────────────────
        private Border BuildLatencyCard(InputLatencyTracker? tracker)
        {
            if (tracker == null)
                return BuildInactiveCard("INPUT LATENCY", "not active");

            var p = tracker.GetPercentiles();
            double p99 = p.Total.P99;
            Color statusColor = p99 <= LatencyTargetMs ? CfgGood : p99 <= LatencyTargetMs * 1.6 ? CfgWarn : CfgBad;

            return MakeCard("INPUT LATENCY", statusColor,
                ("P50 Total", $"{p.Total.P50:F2} ms"),
                ("P95 Total", $"{p.Total.P95:F2} ms"),
                ("P99 Target 5ms", $"{p99:F2} ms"),
                ("Over Budget", $"{p.OverBudgetCount} ({p.OverBudgetPercent:F1}%)"),
                ("Samples", p.SampleCount.ToString())).Card;
        }

        // ── Card 2: Memory & GC (PERF-004) ──────────────────────────────────
        private Border BuildMemoryCard(MemoryAllocationTracker? tracker)
        {
            if (tracker == null)
                return BuildInactiveCard("MEMORY / GC", "not active");

            var agg = tracker.GetAggregateStats();
            Color memColor = agg.CurrentWorkingSetMb <= 512 ? CfgGood : agg.CurrentWorkingSetMb <= 1024 ? CfgWarn : CfgBad;

            var (card, rows) = MakeCard("MEMORY / GC", memColor,
                ("Working Set", $"{agg.CurrentWorkingSetMb} MB"),
                ("Total Allocated", $"{agg.TotalAllocatedMb} MB"),
                ("GC Gen0/1/2", $"{agg.TotalGen0}/{agg.TotalGen1}/{agg.TotalGen2}"));

            // Object-Pool Hit-Rate (top 3 Pools)
            var pools = tracker.GetPoolStats();
            int shown = 0;
            foreach (var pool in pools)
            {
                if (shown >= 3) break;
                Color poolColor = pool.Value.HitRatePercent >= 90 ? CfgGood : pool.Value.HitRatePercent >= 75 ? CfgWarn : CfgBad;
                AddRow(rows, $"{pool.Key} Hit%", $"{pool.Value.HitRatePercent:F1}%", poolColor);
                shown++;
            }
            if (shown == 0)
                AddRow(rows, "Object Pools", "none registered", CfgMuted);

            return card;
        }

        // ── Card 3: Frame Budget (Registry-basiert, ehrlicher Status) ────────
        private Border BuildFrameBudgetCard()
        {
            var monitors = FrameBudgetRegistry.GetAll();
            if (monitors.Count == 0)
                return BuildInactiveCard("FRAME BUDGET", "not active");

            var (card, rows) = MakeCard("FRAME BUDGET", CfgGood, ("Monitors", monitors.Count.ToString()));
            foreach (var mon in monitors.Values)
            {
                var stats = mon.GetStats();
                Color c = stats.OverBudgetPercent <= 5 ? CfgGood : stats.OverBudgetPercent <= 15 ? CfgWarn : CfgBad;
                AddRow(rows, $"{mon.GetType().Name} P99", $"{stats.OverBudgetPercent:F1}% over budget", c);
            }
            return card;
        }

        // ── Card 4: GPU Overlay (Registry-basiert, ehrlicher Status) ─────────
        private Border BuildGpuOverlayCard()
        {
            var profilers = GpuOverlayProfilerRegistry.GetAll();
            if (profilers.Count == 0)
                return BuildInactiveCard("GPU OVERLAY", "not active");

            var (card, rows) = MakeCard("GPU OVERLAY", CfgGood, ("Overlays", profilers.Count.ToString()));
            foreach (var prof in profilers.Values)
            {
                var m = prof.GetMetrics();
                Color c = m.DroppedFrames == 0 ? CfgGood : m.DroppedFrames < 10 ? CfgWarn : CfgBad;
                AddRow(rows, $"{m.OverlayType} Frame", $"{m.LastFrameTimeMs:F2} ms / {m.DroppedFrames} dropped", c);
            }
            return card;
        }

        // ── Card-Builder-Helfer ──────────────────────────────────────────────

        private Border BuildInactiveCard(string title, string status)
        {
            var (card, _) = MakeCard(title, CfgMuted, (status, "—"));
            return card;
        }

        /// <summary>Erzeugt eine Telemetrie-Karte mit Header + Zeilen. Gibt (Border, StackPanel) zurück.</summary>
        private (Border Card, StackPanel Rows) MakeCard(string title, Color statusColor, params (string Label, string Value)[] rows)
        {
            var stack = new StackPanel();

            // Header: Titel links, Status-Punkt rechts
            var header = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.Children.Add(new TextBlock
            {
                Text = title,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(CfgText)
            });
            var dot = new System.Windows.Shapes.Ellipse
            {
                Width = 7,
                Height = 7,
                Fill = new SolidColorBrush(statusColor),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(dot, 1);
            header.Children.Add(dot);
            stack.Children.Add(header);

            foreach (var (label, value) in rows)
                AddRow(stack, label, value, CfgText);

            var card = new Border
            {
                Style = TryFindResource("TelemetryCard") as Style,
                Child = stack
            };
            return (card, stack);
        }

        private static void AddRow(StackPanel rows, string label, string value, Color valueColor)
        {
            var row = new Grid { Margin = new Thickness(0, 1, 0, 1) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var lbl = new TextBlock
            {
                Text = label,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9.5,
                Foreground = new SolidColorBrush(CfgMuted)
            };
            row.Children.Add(lbl);

            var val = new TextBlock
            {
                Text = value,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(valueColor)
            };
            Grid.SetColumn(val, 1);
            row.Children.Add(val);

            rows.Children.Add(row);
        }
    }
}
