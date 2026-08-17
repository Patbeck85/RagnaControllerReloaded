using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using RagnaController.Core;

namespace RagnaController
{
    public partial class DeveloperConsoleWindow : Window
    {
        private readonly AdvancedLogger _logger;
        private readonly List<string> _buffer = new();
        private const int MAX_LINES = 500;
        
        // Throttling timer to prevent UI thread flooding
        private readonly DispatcherTimer _flushTimer;
        private bool _isDirty = false;

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
                _flushTimer.Stop();
                _logger.LiveLogReceived -= OnLiveLogReceived;
            };
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
                content = string.Join("\n", _buffer);
                _isDirty = false;
            }

            LogText.Text = content;
            LogScroll.ScrollToEnd();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            lock (_buffer) _buffer.Clear();
            LogText.Text = "";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
