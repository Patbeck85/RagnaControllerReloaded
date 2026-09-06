using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RagnaController.Controller;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController
{
    /// <summary>
    /// Interaction logic for InGameOverlayWindow.xaml
    /// </summary>
    public partial class InGameOverlayWindow : Window
    {
        private static readonly Lazy<LocalizationManager> _localization = new(() => LocalizationManager.Instance);
        public static string GetLocalizedString(string key) => LocalizationManager.GetLocalizedString(key);

        // Controller state from ControllerManager
        private readonly ControllerManager? _controllerManager;
        private readonly Settings? _settings;

        // Backing fields for overlay display
        private string _profileName = "NOVICE";
        private string _classType = "Novice";
        private string _batteryLevel = "100%";
        private int _cooldownRemaining = 0;
        private string _currentLayer = "BASE";

        // Constructor with ControllerManager (for new code using unified abstraction)
        public InGameOverlayWindow(IMessenger messenger, Core.WindowTracker tracker, ControllerManager controllerManager, Settings settings = null) : base()
        {
            InitializeComponent();
            _controllerManager = controllerManager;
            _settings = settings;

            // Subscribe to controller manager events
            if (_controllerManager != null)
            {
                _controllerManager.ControllerConnected += OnControllerConnected;
                _controllerManager.ControllerDisconnected += OnControllerDisconnected;
                _controllerManager.ProviderChanged += OnProviderChanged;
                _controllerManager.IsConnectedChanged += OnIsConnectedChanged;
            }

            // Subscribe to profile changes if available
            InitializeOverlayState();

            // Initialize timer for cooldown tracking
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += (s, e) => UpdateCooldownTimer();
            timer.Start();
        }

        // Backward compatibility constructor (for existing code without ControllerManager)
        public InGameOverlayWindow(IMessenger messenger, Core.WindowTracker tracker, Settings settings = null) : this(messenger, tracker, null!, settings)
        {
        }

        private void InitializeOverlayState()
        {
            // Load default profile state - try to get from ProfileManager
            _profileName = "NOVICE";
            _classType = "Novice";
            _batteryLevel = "100%";
            
            // Apply theme settings
            ApplyThemeSettings();
            
            UpdateDisplay();
        }
        
        private void ApplyThemeSettings()
                {
                    if (_settings == null) return;
           
                   // Apply opacity
                    Dispatcher.Invoke(() => 
                    {
                        this.Opacity = _settings.OverlayOpacity;
                    });
           
                   // Apply font scale
                    Dispatcher.Invoke(() => 
                    {
                        // Apply font scale to all text elements
                        var scale = _settings.OverlayFontScale;
                        if (ProfileText != null) ProfileText.FontSize = 11 * scale;
                        if (LayerText != null) LayerText.FontSize = 11 * scale;
                        if (StateText != null) StateText.FontSize = 12 * scale;
                        if (BatteryText != null) BatteryText.FontSize = 10 * scale;
                        if (CooldownText != null) CooldownText.FontSize = 9 * scale;
                    });
           
                   // Apply theme colors
                    ApplyThemeColors(_settings.OverlayTheme);
                }
       
               private void ApplyThemeColors(Settings.OverlayThemeType theme)
        {
            Dispatcher.Invoke(() =>
            {
                switch (theme)
                {
                    case Settings.OverlayThemeType.Neon:
                        // Neon: Bright cyberpunk colors with strong glows
                        if (ProfileText != null) ProfileText.Foreground = (Brush)FindResource("AccentBlue");
                        if (StateText != null) StateText.Foreground = (Brush)FindResource("AccentPurple");
                        if (LayerText != null) LayerText.Foreground = (Brush)FindResource("Live");
                        if (BatteryText != null) BatteryText.Foreground = (Brush)FindResource("TextSecondary");
                        if (CooldownText != null) CooldownText.Foreground = (Brush)FindResource("TextPrimary");
                        break;
                    case Settings.OverlayThemeType.Soft:
                        // Soft: Subtle colors with gentle glows
                        if (ProfileText != null) ProfileText.Foreground = (Brush)FindResource("TextPrimary");
                        if (StateText != null) StateText.Foreground = (Brush)FindResource("TextSecondary");
                        if (LayerText != null) LayerText.Foreground = (Brush)FindResource("TextPrimary");
                        if (BatteryText != null) BatteryText.Foreground = (Brush)FindResource("TextTertiary");
                        if (CooldownText != null) CooldownText.Foreground = (Brush)FindResource("TextSecondary");
                        break;
                    case Settings.OverlayThemeType.Dark:
                        // Dark: Minimal, high contrast, low glow
                        if (ProfileText != null) ProfileText.Foreground = (Brush)FindResource("TextPrimary");
                        if (StateText != null) StateText.Foreground = (Brush)FindResource("TextPrimary");
                        if (LayerText != null) LayerText.Foreground = (Brush)FindResource("TextPrimary");
                        if (BatteryText != null) BatteryText.Foreground = (Brush)FindResource("TextPrimary");
                        if (CooldownText != null) CooldownText.Foreground = (Brush)FindResource("TextPrimary");
                        break;
                }
            });
        }

        private void OnControllerConnected(object? sender, EventArgs e)
        {
            // Update overlay when controller connects
            UpdateControllerInfo();
            UpdateDisplay();
        }

        private void OnControllerDisconnected(object? sender, EventArgs e)
        {
            // Reset controller info when disconnected
            _batteryLevel = "Unknown";
            _cooldownRemaining = 0;
            UpdateDisplay();
        }

        private void OnProviderChanged(object? sender, EventArgs e)
        {
            // Provider changed (SDL2 -> XInput or vice versa)
            UpdateControllerInfo();
        }

        private void OnIsConnectedChanged(object? sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void UpdateControllerInfo()
        {
            if (_controllerManager != null && _controllerManager.IsConnected)
            {
                _profileName = _controllerManager.ControllerName ?? "NOVICE";
                _classType = _controllerManager.ControllerType ?? "Unknown";
                _batteryLevel = _controllerManager.BatteryLevel ?? "Unknown";
                _currentLayer = GetCurrentLayerFromController();
            }
            else
            {
                _profileName = "NOVICE";
                _classType = "Unknown";
                _batteryLevel = "Unknown";
            }
        }

        private string GetCurrentLayerFromController()
        {
            // Determine current layer based on button states
            if (_buttonStates.HasValue)
            {
                if (_buttonStates.Value.DPadUp) return "L1+";
                if (_buttonStates.Value.DPadDown) return "R1+";
                if (_buttonStates.Value.DPadLeft) return "L2+";
                if (_buttonStates.Value.DPadRight) return "R2+";
            }
            return "BASE";
        }

        private void UpdateDisplay()
        {
            // Dispatcher-safe UI update
            Dispatcher.Invoke(() =>
            {
                ProfileText.Text = _profileName;
                StateText.Text = _classType; // Reusing StateText for class badge
                LayerText.Text = _currentLayer;
                BatteryText.Text = _batteryLevel;

                // Update class icon based on class type
                UpdateClassIcon(_classType);
            });
        }

        private void UpdateClassIcon(string classType)
        {
            string imagePath = $"/Assets/Classes/{classType?.ToLower() ?? "novice"}.png";
            try
            {
                var brush = (ImageBrush)FindResource("ClassIconBrush");
                brush.ImageSource = new System.Windows.Media.ImageSourceConverter().ConvertFromString(imagePath) as System.Windows.Media.ImageSource;
            }
            catch
            {
                // Fallback to novice icon
                var brush = (ImageBrush)FindResource("ClassIconBrush");
                brush.ImageSource = new System.Windows.Media.Imaging.BitmapImage(new System.Uri("/Assets/Classes/Novice.png", System.UriKind.Relative));
            }
        }

        // ParsedInput from controller for layer detection
        private ButtonState? _buttonStates;
        public ButtonState? ButtonStates
        {
            get => _buttonStates;
            set
            {
                _buttonStates = value;
                OnPropertyChanged();
                // Update layer when buttons change
                if (value.HasValue)
                {
                    _currentLayer = GetCurrentLayerFromController();
                    Dispatcher.Invoke(() => LayerText.Text = _currentLayer);
                }
            }
        }

        // Cooldown timer update
        private int _cooldownTimer = 0;
        private void UpdateCooldownTimer()
        {
            _cooldownTimer++;
            if (_cooldownTimer >= 10) // Update every 1 second (10 * 100ms)
            {
                _cooldownTimer = 0;
                _cooldownRemaining--;
                if (_cooldownRemaining < 0) _cooldownRemaining = 0;
                CooldownText.Text = _cooldownRemaining + "s";
            }
        }

        // PropertyChanged helper (simplified)
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) { }
    }
}