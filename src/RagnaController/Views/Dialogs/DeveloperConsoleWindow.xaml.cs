using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using RagnaController.Core;
using RagnaController.Views.Windows;

namespace RagnaController.Views.Dialogs
{
    public partial class DeveloperConsoleWindow : Window, IDisposable
    {
        private readonly AdvancedLogger _logger;
        private readonly List<string> _buffer = new();
        private const int MAX_LINES = 500;
        
        // TASK-2016: Log level & timestamp filter state
        private string _logLevelFilter = "ALL";
        private bool _showTimestamps = true;
        
        // Throttling timer to prevent UI thread flooding
        private readonly DispatcherTimer _flushTimer;
        private bool _isDirty = false;
        private bool _disposed = false;

        public DeveloperConsoleWindow(AdvancedLogger logger)
        {
            InitializeComponent();
            _logger = logger;
            
            _flushTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(100) // Update UI every 100ms
            };
            _flushTimer.Tick += FlushBuffer;
            _flushTimer.Start();

            // Subscribe to live events
            _logger.LiveLogReceived += OnLiveLogReceived;
            
            Closed += (_, _) => 
            {
                Dispose();
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_flushTimer != null)
            {
                _flushTimer.Stop();
                _flushTimer.Tick -= FlushBuffer;
            }
            _logger.LiveLogReceived -= OnLiveLogReceived;
        }

        private void OnLiveLogReceived(string message)
        {
            lock (_buffer)
            {
                _buffer.Add(message);
                if (_buffer.Count > MAX_LINES)
                {
                    _buffer.RemoveRange(0, _buffer.Count - MAX_LINES);
                }
                _isDirty = true;
            }
        }

        private void FlushBuffer(object? sender, EventArgs e)
        {
            if (!_isDirty) return;

            string content;
            lock (_buffer)
            {
                // TASK-2016: Apply log level filter + timestamp toggle
                var filtered = _buffer.Where(line =>
                {
                    if (_logLevelFilter == "ALL") return true;
                    return line.Contains($"[{_logLevelFilter}]");
                });

                if (!_showTimestamps)
                {
                    // Strip timestamp prefix "HH:mm:ss.fff " from each line
                    filtered = filtered.Select(line =>
                    {
                        var tsMatch = System.Text.RegularExpressions.Regex.Match(line, @"^\d{2}:\d{2}:\d{2}\.\d{3}\s");
                        return tsMatch.Success ? line[tsMatch.Length..] : line;
                    });
                }

                content = string.Join("\n", filtered);
                _isDirty = false;
            }

            LogText.Text = content;
            LogScroll.ScrollToEnd();
        }

        // TASK-2016: Log level filter changed
        private void LogLevelFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LogLevelFilterCombo.SelectedItem is ComboBoxItem item)
                _logLevelFilter = item.Content?.ToString() ?? "ALL";
            _isDirty = true;
        }

        // TASK-2016: Timestamp toggle
        private void ChkShowTimestamps_Checked(object sender, RoutedEventArgs e)
        {
            _showTimestamps = ChkShowTimestamps.IsChecked == true;
            _isDirty = true;
        }

        // TASK-2016: Copy logs to clipboard
        private void BtnCopyLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string content = LogText.Text;
                if (!string.IsNullOrEmpty(content))
                {
                    Clipboard.SetText(content);
                    System.Windows.MessageBox.Show("Log output copied to clipboard.", "Copied", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to copy: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            lock (_buffer) _buffer.Clear();
            LogText.Text = "";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
