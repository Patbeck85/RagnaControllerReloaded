using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace RagnaController.Presentation.Services
{
    /// <summary>
    /// Manages toast notifications (temporary UI messages shown at the bottom of MainWindow).
    /// Extracted from MainWindow as part of TECH-062 (God Class refactoring).
    /// </summary>
    public class ToastService
    {
        private Border? _toastBorder;
        private TextBlock? _toastText;
        private SolidColorBrush? _toastBg;
        private DispatcherTimer? _timer;

        /// <summary>
        /// Initializes the service with the toast UI elements defined in MainWindow.xaml.
        /// Must be called after InitializeComponent().
        /// </summary>
        public void Initialize(Border toastBorder, TextBlock toastText, SolidColorBrush toastBg)
        {
            _toastBorder = toastBorder;
            _toastText = toastText;
            _toastBg = toastBg;
        }

        /// <summary>
        /// Shows a toast notification that auto-hides after 3 seconds.
        /// </summary>
        /// <param name="isError">Whether to show an error color (red) or success color (green).</param>
        public void Show(string message, bool isError = false)
        {
            try
            {
                if (_toastBorder == null || _toastText == null || _toastBg == null) return;

                _toastText.Text = message;
                _toastBg.Color = isError ? Color.FromRgb(255, 58, 82) : Color.FromRgb(57, 255, 20);
                _toastBorder.Visibility = Visibility.Visible;

                // Auto-hide after 3 seconds
                _timer?.Stop();
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromSeconds(3);
                _timer.Tick += (s, e) =>
                {
                    _timer?.Stop();
                    _toastBorder.Visibility = Visibility.Collapsed;
                };
                _timer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ToastService] Show failed: {ex.Message}");
            }
        }
    }
}
