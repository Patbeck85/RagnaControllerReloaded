using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using RagnaController.Core;

namespace RagnaController
{
    public partial class MiniModeWindow : Window
    {
        private bool _clickThrough = false;

        public MiniModeWindow()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Default position: Bottom-Right corner, just above the taskbar
            var area = SystemParameters.WorkArea;
            this.Left = area.Right - this.Width - 20;
            this.Top = area.Bottom - this.Height - 20;
        }

        // Method called by MainWindow when a new Snapshot arrives
        public void UpdateSnapshot(ControllerSnapshot snap, string profileName, bool engineRunning, string batteryLevel)
        {
            Dispatcher.BeginInvoke(() =>
            {
                // Update Profile & State
                ProfileText.Text = profileName.ToUpperInvariant();
                
                if (snap.FocusLocked)
                {
                    StateText.Text = "FOCUS LOCKED";
                    StateText.Foreground = Brushes.OrangeRed;
                    StatusDot.Fill = Brushes.OrangeRed;
                    if (StatusDot.Effect is DropShadowEffect dse1) dse1.Color = Color.FromRgb(255, 69, 0);
                    RootBorder.Opacity = 0.5; // Dim the widget if locked
                }
                else if (!engineRunning)
                {
                    StateText.Text = "PAUSED";
                    StateText.Foreground = Brushes.Gray;
                    StatusDot.Fill = Brushes.Gray;
                    if (StatusDot.Effect is DropShadowEffect _dse2) _dse2.Color = Colors.Gray;
                    RootBorder.Opacity = 0.5;
                }
                else
                {
                    StateText.Text = snap.StateLabel.ToUpper();
                    StateText.Foreground = new SolidColorBrush(Color.FromRgb(229, 184, 66)); // Gold
                    StatusDot.Fill = new SolidColorBrush(Color.FromRgb(61, 219, 110)); // Green
                    if (StatusDot.Effect is DropShadowEffect _dse) _dse.Color = Color.FromRgb(61, 219, 110);
                    RootBorder.Opacity = 1.0;
                }

                // Update Battery
                BatteryText.Text = batteryLevel;
                BatteryFill.Width = batteryLevel switch
                {
                    "Full" => 20,
                    "High" => 15,
                    "Mid" => 10,
                    "Low" => 4,
                    "Empty" => 1,
                    _ => 0
                };
                BatteryFill.Background = batteryLevel is "Low" or "Empty" ? Brushes.Red : Brushes.LimeGreen;
            });
        }

        // ── Drag & Click-Through Logic ────────────────────────────────────

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_clickThrough) 
            {
                try { this.DragMove(); } catch { }
            }
        }

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _clickThrough = !_clickThrough;
            UpdateClickThrough();

            if (_clickThrough)
            {
                RootBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(58, 142, 255)); // Blue
                ClickThroughHint.Visibility = Visibility.Visible;
                
                // Auto-hide the hint after 2 seconds so they can see the stats again
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                timer.Tick += (s, args) => 
                { 
                    ClickThroughHint.Visibility = Visibility.Collapsed; 
                    timer.Stop(); 
                };
                timer.Start();
            }
            else
            {
                RootBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(42, 50, 69)); // Default gray
                ClickThroughHint.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateClickThrough()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            long currentStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
            long newStyle = _clickThrough 
                ? currentStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED
                : currentStyle & ~NativeMethods.WS_EX_TRANSPARENT;

            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(newStyle));
        }
    }
}
