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
                if (ProfileText != null) ProfileText.Text = profileName.ToUpperInvariant();
                
                var gold = (Brush)FindResource("Gold");
                var live = (Brush)FindResource("Live");
                var danger = (Brush)FindResource("Danger");
                var textSecondary = (Brush)FindResource("TextSecondary");
                var bgPrimary = (Brush)FindResource("BgPrimary");
                var accentBlue = (Brush)FindResource("AccentBlue");

                if (snap.FocusLocked)
                {
                    if (StateText != null) { StateText.Text = "FOCUS LOCKED"; StateText.Foreground = danger; }
                    if (StatusDot != null) { StatusDot.Fill = danger; if (StatusDot.Effect is DropShadowEffect dse1) dse1.Color = ((SolidColorBrush)danger).Color; }
                    if (RootBorder != null) RootBorder.Opacity = 0.5; // Dim the widget if locked
                }
                else if (!engineRunning)
                {
                    if (StateText != null) { StateText.Text = "PAUSED"; StateText.Foreground = textSecondary; }
                    if (StatusDot != null) { StatusDot.Fill = textSecondary; if (StatusDot.Effect is DropShadowEffect _dse2) _dse2.Color = ((SolidColorBrush)textSecondary).Color; }
                    if (RootBorder != null) RootBorder.Opacity = 0.5;
                }
                else
                {
                    if (StateText != null) { StateText.Text = snap.StateLabel.ToUpper(); StateText.Foreground = gold; }
                    if (StatusDot != null) { StatusDot.Fill = live; if (StatusDot.Effect is DropShadowEffect _dse) _dse.Color = ((SolidColorBrush)live).Color; }
                    if (RootBorder != null) RootBorder.Opacity = 1.0;
                }

                // Update Battery
                if (BatteryText != null) BatteryText.Text = batteryLevel;
                if (BatteryFill != null)
                {
                    BatteryFill.Width = batteryLevel switch
                    {
                        "Full" => 20,
                        "High" => 15,
                        "Mid" => 10,
                        "Low" => 4,
                        "Empty" => 1,
                        _ => 0
                    };
                    BatteryFill.Background = batteryLevel is "Low" or "Empty" ? danger : live;
                }
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

            var accentBlue = (Brush)FindResource("AccentBlue");
            var bgBorder = (Brush)FindResource("BgBorder");

            if (_clickThrough)
            {
                if (RootBorder != null) RootBorder.BorderBrush = accentBlue;
                if (ClickThroughHint != null) ClickThroughHint.Visibility = Visibility.Visible;
                
                // Auto-hide the hint after 2 seconds so they can see the stats again
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                timer.Tick += (s, args) => 
                { 
                    if (ClickThroughHint != null) ClickThroughHint.Visibility = Visibility.Collapsed; 
                    timer.Stop(); 
                };
                timer.Start();
            }
            else
            {
                if (RootBorder != null) RootBorder.BorderBrush = bgBorder;
                if (ClickThroughHint != null) ClickThroughHint.Visibility = Visibility.Collapsed;
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
