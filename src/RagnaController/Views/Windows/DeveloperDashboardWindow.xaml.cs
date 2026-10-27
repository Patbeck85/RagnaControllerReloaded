using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RagnaController.Core;
using RagnaController.Core.Developer;
using RagnaController.Core.Services.Monitoring;
using RagnaController.Models;

namespace RagnaController.Views.Windows
{
    /// <summary>
    /// DeveloperDashboardWindow — Zentrale Entwickler- und Monitoring-Konsole.
    ///
    /// Integriert alle Developer- und Monitoring-Services:
    /// • InputLatencyVisualizer, FrameBudgetMonitor, MemoryAllocationTracker
    /// • ActionLogService, ETWProvider
    /// • AutomatedTestRecorder, LuaScriptingService
    /// • SystemMonitor, OcrService, TelemetryService
    /// </summary>
    public partial class DeveloperDashboardWindow : Window
    {
        // ── Services (Singletons aus EngineOrchestrator oder neu instanziiert) ──
        private readonly InputLatencyVisualizer _latencyViz = new();
        private readonly FrameBudgetMonitor _frameBudget = new("Dashboard");
        private readonly MemoryAllocationTracker _memTracker = new("Dashboard");
        private readonly ActionLogService _actionLog = new();
        private readonly AutomatedTestRecorder _testRecorder = new();
        private readonly LuaScriptingService _luaService = new();
        private readonly OcrService _ocrService = new();
        // TelemetryService ist statisch - direkt aufrufen: TelemetryService.SendAppStartPing()

        // ── UI-Refresh-Timer ──
        private readonly System.Windows.Threading.DispatcherTimer _refreshTimer = new();

        public DeveloperDashboardWindow()
        {
            InitializeComponent();

            // ── ActionLogService: Einträge live anzeigen ──
            _actionLog.EntryAdded += (entry) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ActionLogList.Items.Insert(0, entry);
                    if (ActionLogList.Items.Count > 50)
                        ActionLogList.Items.RemoveAt(50);
                });
            };

            // ── Refresh-Timer für Live-Daten (alle 500ms) ──
            _refreshTimer.Interval = TimeSpan.FromMilliseconds(500);
            _refreshTimer.Tick += (s, e) => RefreshDashboardData();
            _refreshTimer.Start();

            // ── Lua-Skript-Liste initialisieren ──
            LuaScriptList.ItemsSource = _luaService.RegisteredScripts;

            // ── Standard-Tab anzeigen ──
            TabPerf_Click(BtnTabPerf, null);
        }

        // ── Window-Events ──
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Erster Refresh nach dem Laden
            RefreshDashboardData();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _refreshTimer.Stop();
            _luaService.Disable();
            _ocrService?.Dispose();
        }

        // ── Tab-Navigation ──
        private void TabPerf_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab(BtnTabPerf, TabPerf);
        }

        private void TabActions_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab(BtnTabActions, TabActions);
        }

        private void TabDev_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab(BtnTabDev, TabDev);
        }

        private void TabSystem_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab(BtnTabSystem, TabSystem);
        }

        private void SetActiveTab(Button activeButton, Border activeTab)
        {
            // Alle Buttons deaktivieren
            foreach (var btn in new[] { BtnTabPerf, BtnTabActions, BtnTabDev, BtnTabSystem })
            {
                btn.BorderBrush = null;
                btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A1A1AA"));
            }
            // Alle Tabs ausblenden
            foreach (var tab in new[] { TabPerf, TabActions, TabDev, TabSystem })
                tab.Visibility = Visibility.Collapsed;

            // Aktivieren
            activeButton.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6"));
            activeButton.Foreground = new SolidColorBrush(Colors.White);
            activeTab.Visibility = Visibility.Visible;
        }

        // ── Dashboard-Daten aktualisieren ──
        private void RefreshDashboardData()
        {
            Dispatcher.Invoke(() =>
            {
                // ── Performance Tab ──
                // Input Latency
                var latency = _latencyViz.LastLatencyMs;
                LblLatencyCurrent.Text = latency.HasValue ? $"{latency.Value:F1} ms" : "— ms";
                LblLatencyAvg.Text = $"Avg: {_latencyViz.AverageLatencyMs:F1} ms";
                LblLatencyMin.Text = $"Min: {_latencyViz.MinLatencyMs:F1} ms";
                LblLatencyMax.Text = $"Max: {_latencyViz.MaxLatencyMs:F1} ms";

                // Frame Budget
                var (p50, p95, p99) = _frameBudget.GetPercentiles();
                LblFrameP99.Text = $"{p99:F1} ms";
                LblFrameP50.Text = $"P50: {p50:F1} ms";
                LblFrameP95.Text = $"P95: {p95:F1} ms";

                // Memory
                var mem = _memTracker.GetMemoryPercentiles();
                LblMemWorkingSet.Text = $"{mem.P99WsMb} MB";
                LblMemGen0.Text = $"Gen0: {_memTracker.GetGcPercentiles().P50Gen0}";
                LblMemGen1.Text = $"Gen1: {_memTracker.GetGcPercentiles().P50Gen1}";
                LblMemGen2.Text = $"Gen2: {_memTracker.GetGcPercentiles().P50Gen2}";
            });
        }

        // ── Automated Test Recorder ──
        private void BtnRecordStart_Click(object sender, RoutedEventArgs e)
        {
            _testRecorder.StartRecording();
            LblRecordStatus.Text = "Status: Recording...";
            LblRecordStatus.Foreground = new SolidColorBrush(Colors.LimeGreen);
        }

        private void BtnRecordStop_Click(object sender, RoutedEventArgs e)
        {
            _testRecorder.StopRecording();
            LblRecordStatus.Text = "Status: Idle";
            LblRecordStatus.Foreground = new SolidColorBrush(Colors.Gray);
        }

        private void BtnRecordExport_Click(object sender, RoutedEventArgs e)
        {
            var code = _testRecorder.GenerateTestCode("GeneratedTest", "RecordedTest");
            var dialog = new System.Windows.Forms.SaveFileDialog
            {
                Title = "Export Test Code",
                Filter = "C# Files (*.cs)|*.cs|All Files (*.*)|*.*",
                FileName = "GeneratedTest.cs"
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                System.IO.File.WriteAllText(dialog.FileName, code);
                MessageBox.Show($"Test code exported to:\n{dialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ── Lua Scripting ──
        private void BtnLuaEnable_Click(object sender, RoutedEventArgs e)
        {
            _luaService.Enable();
            LblLuaStatus.Text = "Status: Enabled";
            LblLuaStatus.Foreground = new SolidColorBrush(Colors.LimeGreen);
        }

        private void BtnLuaDisable_Click(object sender, RoutedEventArgs e)
        {
            _luaService.Disable();
            LblLuaStatus.Text = "Status: Disabled";
            LblLuaStatus.Foreground = new SolidColorBrush(Colors.Gray);
        }

        private void BtnLuaRegister_Click(object sender, RoutedEventArgs e)
        {
            var name = TxtLuaName.Text.Trim();
            var code = TxtLuaCode.Text.Trim();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Please enter both script name and code.", "Missing Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_luaService.RegisterScript(name, code))
            {
                LuaScriptList.Items.Refresh();
                TxtLuaName.Clear();
                TxtLuaCode.Clear();
                MessageBox.Show($"Script '{name}' registered.", "Script Registered",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Script '{name}' already exists or invalid input.", "Registration Failed",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ── OCR Service ──
        private void BtnOcrTest_Click(object sender, RoutedEventArgs e)
        {
            if (!_ocrService.IsAvailable)
            {
                MessageBox.Show("OCR engine not available. Tesseract may not be initialized.", "OCR Unavailable",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            MessageBox.Show("OCR service is available. Capture a RO client window to test HP/SP extraction.", "OCR Service",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── Telemetry ──
        private void BtnTelPing_Click(object sender, RoutedEventArgs e)
        {
            TelemetryService.SendAppStartPing();
            MessageBox.Show("Telemetry ping sent (if webhook configured).", "Telemetry",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── Action Log ──
        private void BtnLogClear_Click(object sender, RoutedEventArgs e)
        {
            _actionLog.Clear();
            ActionLogList.Items.Clear();
        }

        private void BtnLogExport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.SaveFileDialog
            {
                Title = "Export Action Log",
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = "ActionLog.txt"
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var lines = ActionLogList.Items.Cast<ActionEntry>().Select(e =>
                    $"{e.FormattedTime} [{e.Kind}] {e.Label}");
                System.IO.File.WriteAllLines(dialog.FileName, lines);
                MessageBox.Show($"Action log exported to:\n{dialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
