using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// MVVM ViewModel for MainWindow and MiniModeWindow.
    /// Exposes bindable properties derived from <see cref="ControllerSnapshot"/>.
    /// Replaces hundreds of lines of imperative <c>if (XyzLabel != null) XyzLabel.Text = ...</c>
    /// in the code-behind.
    /// </summary>
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // ── Engine status ─────────────────────────────────────────────────
        private string _statusText = "Waiting for Controller…";
        public string StatusText { get => _statusText; set => Set(ref _statusText, value); }

        private Brush _statusDotColor = Brushes.Gray;
        public Brush StatusDotColor { get => _statusDotColor; set => Set(ref _statusDotColor, value); }

        private string _controllerName = "—";
        public string ControllerName { get => _controllerName; set => Set(ref _controllerName, value); }

        private string _batteryText = "";
        public string BatteryText { get => _batteryText; set => Set(ref _batteryText, value); }

        // ── Profile ───────────────────────────────────────────────────────
        private string _profileName = "—";
        public string ProfileName { get => _profileName; set => Set(ref _profileName, value); }

        private string _classLabel = "—";
        public string ClassLabel { get => _classLabel; set => Set(ref _classLabel, value); }

        // ── Hero Image (Class Sprite) ────────────────────────────────────
        private string _classImageSource = "/Assets/Classes/unknown.png";
        public string ClassImageSource { get => _classImageSource; set => Set(ref _classImageSource, value); }

        // ── Tick / window ─────────────────────────────────────────────────
        private string _latencyText = "";
        public string LatencyText { get => _latencyText; set => Set(ref _latencyText, value); }

        private bool _windowTracked;
        public bool WindowTracked { get => _windowTracked; set => Set(ref _windowTracked, value); }

        // ── Input layer ───────────────────────────────────────────────────
        private string _layerText = "BASE";
        public string LayerText { get => _layerText; set => Set(ref _layerText, value); }

        private Brush _layerColor = Brushes.Gray;
        public Brush LayerColor { get => _layerColor; set => Set(ref _layerColor, value); }

        // ── Engine state ──────────────────────────────────────────────────
        private string _stateLabel = "IDLE";
        public string StateLabel { get => _stateLabel; set => Set(ref _stateLabel, value); }

        private Brush _stateColor = new SolidColorBrush(Color.FromRgb(0x3D, 0xDB, 0x6E));
        public Brush StateColor { get => _stateColor; set => Set(ref _stateColor, value); }

        private bool _focusLocked;
        public bool FocusLocked { get => _focusLocked; set => Set(ref _focusLocked, value); }

        private string _focusLockHint = "";
        public string FocusLockHint { get => _focusLockHint; set => Set(ref _focusLockHint, value); }

        // ── Stick visualiser (for MainWindow radar) ───────────────────────
        private double _leftDotX = 20;
        public double LeftDotX { get => _leftDotX; set => Set(ref _leftDotX, value); }

        private double _leftDotY = 20;
        public double LeftDotY { get => _leftDotY; set => Set(ref _leftDotY, value); }

        private double _rightDotX = 20;
        public double RightDotX { get => _rightDotX; set => Set(ref _rightDotX, value); }

        private double _rightDotY = 20;
        public double RightDotY { get => _rightDotY; set => Set(ref _rightDotY, value); }

        // ── Health & RSI Stats (v1.7.0) ───────────────────────────────────
        private string _sessionSavedClicksTxt = "0";
        public string SessionSavedClicksTxt { get => _sessionSavedClicksTxt; set => Set(ref _sessionSavedClicksTxt, value); }

        private string _sessionSavedKeysTxt = "0";
        public string SessionSavedKeysTxt { get => _sessionSavedKeysTxt; set => Set(ref _sessionSavedKeysTxt, value); }

        private string _lifetimeSavedClicksTxt = "0";
        public string LifetimeSavedClicksTxt { get => _lifetimeSavedClicksTxt; set => Set(ref _lifetimeSavedClicksTxt, value); }

        private string _lifetimeSavedKeysTxt = "0";
        public string LifetimeSavedKeysTxt { get => _lifetimeSavedKeysTxt; set => Set(ref _lifetimeSavedKeysTxt, value); }

        public void UpdateHealthStats(long sessionClicks, long sessionKeys, long lifeClicks, long lifeKeys)
        {
            SessionSavedClicksTxt = sessionClicks.ToString("N0");
            SessionSavedKeysTxt = sessionKeys.ToString("N0");
            LifetimeSavedClicksTxt = (lifeClicks + sessionClicks).ToString("N0");
            LifetimeSavedKeysTxt = (lifeKeys + sessionKeys).ToString("N0");
        }

        // ── Update from snapshot ──────────────────────────────────────────
        private static readonly Color ColRunning     = Color.FromRgb(0x3D, 0xDB, 0x6E);
        private static readonly Color ColWarning     = Color.FromRgb(0xFF, 0xA5, 0x00);
        private static readonly Color ColDanger      = Color.FromRgb(0xFF, 0x3A, 0x52);
        private static readonly Color ColFocusLocked = Color.FromRgb(0xFF, 0x3A, 0x52);
        private static readonly Color ColGold        = Color.FromRgb(0xE5, 0xB8, 0x42);
        private static readonly Color ColGrey        = Color.FromRgb(0x7D, 0x8B, 0x9E);
        private static readonly Color ColCyan        = Color.FromRgb(0x00, 0xE5, 0xFF);

        public void ApplySnapshot(ControllerSnapshot snap)
        {
            // State badge
            StateLabel = snap.StateLabel;
            StateColor = new SolidColorBrush(snap.StateLabel switch
            {
                "VACUUM"  => ColCyan,
                "SWEEP"   => ColWarning,
                "PANIC!"  => ColDanger,
                "RADIAL"  => ColGold,
                var s when s.StartsWith("COMBO") => Color.FromRgb(0xFF, 0xD7, 0x00),
                _         => ColRunning,
            });

            // Layer badge
            LayerText  = snap.LayerText;
            LayerColor = new SolidColorBrush(snap.LayerText switch
            {
                "L1+" or "R1+" => ColGold,
                "L2+" or "R2+" => ColWarning,
                _              => ColGrey,
            });

            // Focus Lock
            FocusLocked    = snap.FocusLocked;
            FocusLockHint  = snap.FocusLocked ? "⛔ FOCUS LOCK — switch to RO" : "";

            // Status dot
            StatusDotColor = snap.FocusLocked
                ? new SolidColorBrush(ColFocusLocked)
                : new SolidColorBrush(ColRunning);

            // Latency
            LatencyText = snap.WindowTracked
                ? $"{snap.TickMs:F1}ms | RO {snap.WindowDpiScale:F2}x DPI"
                : $"{snap.TickMs:F1}ms | RO: not found";
            WindowTracked = snap.WindowTracked;

            // Stick dots (canvas 50×50, dot 10×10, formula 20+x*20)
            LeftDotX  = 20 + snap.LeftX  * 20;
            LeftDotY  = 20 - snap.LeftY  * 20;
            RightDotX = 20 + snap.RightX * 20;
            RightDotY = 20 - snap.RightY * 20;
        }

        public void ApplyEngineStatus(EngineStatus status, string controllerName)
        {
            ControllerName = controllerName;
            StatusText = status switch
            {
                EngineStatus.Running      => "RUNNING",
                EngineStatus.NoController => "NO CONTROLLER",
                _                         => "PAUSED",
            };
            StatusDotColor = status == EngineStatus.Running
                ? new SolidColorBrush(ColRunning)
                : status == EngineStatus.Stopped
                    ? new SolidColorBrush(ColWarning)
                    : new SolidColorBrush(Color.FromRgb(0x55, 0x5E, 0x7A));
        }

        public void UpdateClassImage(string className)
        {
            string formattedName = className.ToLowerInvariant().Replace(" ", "_");
            string expectedPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Classes", $"{formattedName}.png");
            
            if (System.IO.File.Exists(expectedPath))
            {
                // Must use absolute URI for dynamic local files in WPF outside of packed Resources
                ClassImageSource = new Uri(expectedPath, UriKind.Absolute).ToString();
            }
            else
            {
                // Fallback to empty or a default embedded resource
                ClassImageSource = "/Assets/Classes/unknown.png";
            }
        }
    }
}
