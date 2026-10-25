using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RagnaController.Controller;
using RagnaController.Core;
using RagnaController.Core.Services;
using RagnaController.Models;

namespace RagnaController.Views.Overlays
{
    using RagnaController.Core.Services.Monitoring;
    /// <summary>
    /// Interaction logic for InGameOverlayWindow.xaml
    /// </summary>
    public partial class InGameOverlayWindow : Window
    {
        private static readonly Lazy<LocalizationManager> _localization = new(() => LocalizationManager.Instance);
        /// <summary>
        /// GetLocalizedString method.
        /// <param name="key">key parameter.</param>
        /// <returns>Result of GetLocalizedString.</returns>
        /// </summary>
        public static string GetLocalizedString(string key) => LocalizationManager.GetLocalizedString(key);

        // Controller state from ControllerManager
                private readonly ControllerManager? _controllerManager;
                private readonly Settings? _settings;
                private readonly BuffManager? _buffManager;
                private readonly OcrService? _ocrService;

        // PERF-008: GPU/Overlay render profiling (registry-backed, one per overlay type)
        private GpuOverlayProfiler? _gpuProfiler;
        private FrameBudgetMonitor? _frameBudgetMonitor;
        private long _lastRenderTick = -1;

        // Backing fields for overlay display
                private string _profileName = "NOVICE";
                private string _classType = "Novice";
                private string _batteryLevel = "100%";
                private int _cooldownRemaining = 0;
                private string _currentLayer = "BASE";

                /// <summary>HWND des RO-Clients, dem dieses Overlay zugeordnet ist.</summary>
                public IntPtr ClientHwnd { get; private set; } = IntPtr.Zero;

                // FEAT-031: Skill cooldown tracking (F1-F12)
                private readonly Dictionary<string, SkillCooldown> _skillCooldowns = new();
                public IEnumerable<SkillCooldown> SkillCooldowns => _skillCooldowns.Values;

                // FEAT-036: Damage Numbers
                private readonly ObservableCollection<DamageNumber> _damageNumbers = new();
                public ObservableCollection<DamageNumber> DamageNumbers => _damageNumbers;

                /// <summary>Zeigt eine Schadenszahl am angegebenen Punkt an.</summary>
                public void ShowDamageNumber(int amount, double x, double y)
                {
                    if (!_settings?.OverlayDamageNumbersEnabled == true) return;

                    var dmg = new DamageNumber(amount, x, y);
                    _damageNumbers.Add(dmg);

                    // Entferne nach Ablauf der Dauer
                    var removeTimer = new System.Windows.Threading.DispatcherTimer();
                    removeTimer.Interval = TimeSpan.FromMilliseconds(_settings.OverlayDamageDurationMs);
                    removeTimer.Tick += (s, e) =>
                    {
                        _damageNumbers.Remove(dmg);
                        removeTimer.Stop();
                    };
                    removeTimer.Start();

                    // Begrenze maximale Anzahl
                    while (_damageNumbers.Count > _settings.OverlayDamageMaxVisible)
                    {
                        _damageNumbers.RemoveAt(0);
                    }
                }

                /// <summary>Sets or updates a skill cooldown.</summary>
                public void SetSkillCooldown(string keyName, int remainingSeconds, int maxSeconds, System.Windows.Media.Color color)
                {
                    var sc = new SkillCooldown(keyName, remainingSeconds, maxSeconds, color);
                    _skillCooldowns[keyName] = sc;
                    Dispatcher.Invoke(() =>
                    {
                        // ItemsControl will refresh via INotifyPropertyChanged on SkillCooldown
                    });
                }

                /// <summary>Clears all skill cooldowns.</summary>
                                public void ClearSkillCooldowns()
                                {
                                    _skillCooldowns.Clear();
                                }

                                // FEAT-032: Buff/Debuff timer tracking
                                private readonly Dictionary<string, BuffTimerViewModel> _buffTimers = new(StringComparer.OrdinalIgnoreCase);
                                public IEnumerable<BuffTimerViewModel> BuffTimers => _buffTimers.Values.OrderByDescending(b => b.RemainingSec).ThenBy(b => b.Name);

                                /// <summary>Sets or updates a buff/debuff timer.</summary>
                                public void SetBuffTimer(string name, bool isDebuff, int remainingSec, int maxSec, int warnSec = 5)
                                {
                                    var btv = new BuffTimerViewModel(name, isDebuff, remainingSec, maxSec, warnSec);
                                    _buffTimers[name] = btv;
                                }

                                /// <summary>Clears all buff/debuff timers.</summary>
                                public void ClearBuffTimers()
                                {
                                    _buffTimers.Clear();
                                }

                                /// <summary>Updates all buff timers from BuffManager.</summary>
                                public void UpdateBuffTimersFromManager()
                                {
                                    if (_buffManager == null) return;

                                    ClearBuffTimers();

                                    int count = 0;
                                    foreach (var name in _buffManager.ActiveBuffNames)
                                    {
                                        if (count >= (_settings?.OverlayBuffMaxVisible ?? 8)) break;
                                        var remaining = _buffManager.GetBuffRemainingSec(name);
                                        if (remaining > 0)
                                        {
                                            var duration = BuffRegistry.GetDefaultDuration(name);
                                            SetBuffTimer(name, false, (int)remaining, duration, (_settings?.OverlayBuffWarnSec ?? 5));
                                            count++;
                                        }
                                    }

                                    foreach (var name in _buffManager.ActiveDebuffNames)
                                    {
                                        if (count >= (_settings?.OverlayBuffMaxVisible ?? 8)) break;
                                        var remaining = _buffManager.GetDebuffRemainingSec(name);
                                        if (remaining > 0)
                                        {
                                            var duration = BuffRegistry.GetDefaultDuration(name);
                                            SetBuffTimer(name, true, (int)remaining, duration, (_settings?.OverlayBuffWarnSec ?? 5));
                                            count++;
                                        }
                                    }
                                }

                        // Constructor with ControllerManager (for new code using unified abstraction)
                        #pragma warning disable CS8625
                        public InGameOverlayWindow(IMessenger messenger, Core.WindowTracker tracker, ControllerManager? controllerManager, Settings settings = null, IntPtr clientHwnd = default, BuffManager? buffManager = null) : base()
                        #pragma warning restore CS8625
                {
                    InitializeComponent();
                                DataContext = this;
                                ClientHwnd = clientHwnd;
                                _controllerManager = controllerManager;
                                _settings = settings;
                                _buffManager = buffManager;
                                _ocrService = new OcrService();

                    // UX-021: Restore saved overlay position OR position near client window
                    if (_settings?.OverlayPositionX.HasValue == true && _settings.OverlayPositionY.HasValue)
                    {
                        this.Left = _settings.OverlayPositionX.Value;
                        this.Top = _settings.OverlayPositionY.Value;
                    }
                    else if (clientHwnd != IntPtr.Zero)
                    {
                        // FEAT-030: Position overlay near its client window (top-right with offset)
                        PositionNearClient(clientHwnd);
                    }
                    else
                    {
                        // FEAT-030: Position overlay on selected monitor
                        ApplyMonitorPosition();
                    }

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
            int ocrTick = 0;
            int targetOcrTick = 0;
            timer.Tick += (s, e) =>
            {
                UpdateCooldownTimer();

                // OCR nur alle 2 Sekunden (20 Ticks * 100ms) um CPU zu schonen
                if (_settings?.OverlayHpSpEnabled == true && _ocrService?.IsAvailable == true && ClientHwnd != IntPtr.Zero)
                {
                    ocrTick++;
                    if (ocrTick >= 20)
                    {
                        ocrTick = 0;
                        ReadHpSpViaOcr();
                    }
                }

                // Target-Info OCR alle 3 Sekunden (30 Ticks * 100ms)
                if (_settings?.OverlayTargetInfoEnabled == true && _ocrService?.IsAvailable == true && ClientHwnd != IntPtr.Zero)
                {
                    targetOcrTick++;
                    if (targetOcrTick >= 30)
                    {
                        targetOcrTick = 0;
                        ReadTargetInfoViaOcr();
                    }
                }
            };
            timer.Start();

            // PERF-008: GPU/Overlay render profiling — one profiler + frame-budget monitor
            // per overlay instance. WPF has no layout/render phase hooks, so the
            // CompositionTarget.Rendering loop measures real inter-frame deltas.
            _overlayKey = "InGameOverlay-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            _frameBudgetMonitor = FrameBudgetRegistry.GetOrCreate(_overlayKey, 16.7);
            _gpuProfiler = GpuOverlayProfilerRegistry.GetOrCreate(_overlayKey, null, _frameBudgetMonitor);
            CompositionTarget.Rendering += OnRenderFrame;
            Closed += OnWindowClosed;
        }

        // PERF-008: unique registry key for this overlay instance (allows parallel overlays)
        private string _overlayKey = "InGameOverlay";

        /// <summary>
        /// CompositionTarget.Rendering fires once per WPF render frame on the UI thread.
        /// Measures the real inter-frame delta and records it in the GPU/overlay profiler.
        /// </summary>
        private void OnRenderFrame(object? sender, EventArgs e)
        {
            var profiler = _gpuProfiler;
            if (profiler == null) return;

            long now = Stopwatch.GetTimestamp();
            if (_lastRenderTick >= 0)
            {
                double frameMs = (now - _lastRenderTick) / (double)Stopwatch.Frequency * 1000.0;
                profiler.EndFrame(frameMs);
            }
            _lastRenderTick = now;
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            CompositionTarget.Rendering -= OnRenderFrame;
            // Dispose this instance's shared resources (MEMORY-001: no leaked timers/registry entries)
            GpuOverlayProfilerRegistry.Remove(_overlayKey);
            FrameBudgetRegistry.Remove(_overlayKey);
            _gpuProfiler = null;
            _frameBudgetMonitor = null;

            // UX-021: Save overlay position
            if (_settings != null)
            {
                _settings.OverlayPositionX = this.Left;
                _settings.OverlayPositionY = this.Top;
                _settings.Save();
            }
        }

        // UX-021: Drag overlay with left mouse button (except on buttons)
        private void Overlay_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Only drag if clicking on the backgroand border, not on interactive elements
            if (e.OriginalSource is System.Windows.Controls.Button) return;
            this.DragMove();
        }

        // Backward compatibility constructor (for existing code without ControllerManager)
                #pragma warning disable CS8625
                public InGameOverlayWindow(IMessenger messenger, Core.WindowTracker tracker, Settings settings = null) : this(messenger, tracker, null, settings)
                #pragma warning restore CS8625
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
            
                    // Check initial controller connection state
                    UpdateControllerInfo();
            
                    UpdateDisplay();
                }
        
        // FEAT-030: Position overlay on selected monitor
                private void ApplyMonitorPosition()
                {
                    if (_settings == null) return;

                    var monitors = Core.MonitorHelper.GetMonitors();
                    int index = Math.Clamp(_settings.OverlayMonitorIndex, 0, monitors.Count - 1);
                    var monitor = monitors[index];

                    // Position overlay at top-right of selected monitor with some margin
                    this.Left = monitor.Right - this.Width - 20;
                    this.Top = monitor.Top + 20;
                }

                /// <summary>
                /// Positioniert das Overlay oben rechts neben dem angegebenen Client-HWND.
                /// Fällt auf Monitor-Positionierung zurück, wenn Client nicht auffindbar ist.
                /// </summary>
                private void PositionNearClient(IntPtr clientHwnd)
                {
                    try
                    {
                        if (clientHwnd == IntPtr.Zero) { ApplyMonitorPosition(); return; }

                        var rect = new RECT();
                        if (!NativeMethods.GetWindowRect(clientHwnd, out rect)) { ApplyMonitorPosition(); return; }

                        // Überlay rechts oben vom Client-Fenster, mit 20px Abstand
                        double clientRight = rect.Right;
                        double clientTop = rect.Top;

                        // Stellen wir sicher, dass das Overlay innerhalb des primären Monitors bleibt
                        double overlayWidth = this.Width > 0 ? this.Width : 300;
                        double overlayHeight = this.Height > 0 ? this.Height : 200;

                        this.Left = clientRight - overlayWidth - 20;
                        this.Top = clientTop + 20;

                        // Fallback falls außerhalb des Bildschirms
                        if (this.Left < 0 || this.Top < 0)
                            ApplyMonitorPosition();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("[InGameOverlay] PositionNearClient Fehler: " + ex.Message);
                        ApplyMonitorPosition();
                    }
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
                    // Reset controller info when disconnected - show disconnected state
                    _profileName = "DISCONNECTED";
                    _classType = "No Controller";
                    _batteryLevel = "N/A";
                    _currentLayer = "—";
                    UpdateDisplay();
                }

        private void OnProviderChanged(object? sender, EventArgs e)
        {
            // Provider changed (SDL2 -> XInput or vice versa)
            UpdateControllerInfo();
        }

        private void OnIsConnectedChanged(object? sender, EventArgs e)
                {
                    UpdateControllerInfo();
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
                        _profileName = "DISCONNECTED";
                        _classType = "No Controller";
                        _batteryLevel = "N/A";
                        _currentLayer = "—";
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
            catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($@"[InGameOverlay] Icon load failed for '{classType}': {ex.Message} — fallback to novice icon");
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

        // FEAT-033: HP/SP via OCR aus RO-Client-Fenster auslesen
        private void ReadHpSpViaOcr()
        {
            if (_ocrService == null || _settings == null || ClientHwnd == IntPtr.Zero) return;

            try
            {
                using var screenshot = OcrService.CaptureWindow(ClientHwnd);
                if (screenshot == null) return;

                var sw = screenshot.Width;
                var sh = screenshot.Height;

                // HP-Region
                var hpRegion = new System.Drawing.Rectangle(
                    (int)(sw * _settings.OverlayHpRegionX),
                    (int)(sh * _settings.OverlayHpRegionY),
                    (int)(sw * _settings.OverlayHpRegionW),
                    (int)(sh * _settings.OverlayHpRegionH));

                var hp = _ocrService.ExtractNumber(screenshot, hpRegion);

                // SP-Region
                var spRegion = new System.Drawing.Rectangle(
                    (int)(sw * _settings.OverlaySpRegionX),
                    (int)(sh * _settings.OverlaySpRegionY),
                    (int)(sw * _settings.OverlaySpRegionW),
                    (int)(sh * _settings.OverlaySpRegionH));

                var sp = _ocrService.ExtractNumber(screenshot, spRegion);

                Dispatcher.Invoke(() =>
                {
                    HpText.Text = hp.HasValue ? hp.Value.ToString() : "--";
                    SpText.Text = sp.HasValue ? sp.Value.ToString() : "--";
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[InGameOverlay] OCR HP/SP Fehler: " + ex.Message);
            }
        }

        // FEAT-034: Target-Info via OCR aus RO-Client-Fenster auslesen
        private void ReadTargetInfoViaOcr()
        {
            if (_ocrService == null || _settings == null || ClientHwnd == IntPtr.Zero) return;

            try
            {
                using var screenshot = OcrService.CaptureWindow(ClientHwnd);
                if (screenshot == null) return;

                var sw = screenshot.Width;
                var sh = screenshot.Height;

                // Target-Name (Text, nicht nur Zahl)
                var nameRegion = new System.Drawing.Rectangle(
                    (int)(sw * _settings.OverlayTargetNameRegionX),
                    (int)(sh * _settings.OverlayTargetNameRegionY),
                    (int)(sw * _settings.OverlayTargetNameRegionW),
                    (int)(sh * _settings.OverlayTargetNameRegionH));
                var targetName = _ocrService.ExtractText(screenshot, nameRegion);

                // Target-Level (Zahl)
                var levelRegion = new System.Drawing.Rectangle(
                    (int)(sw * _settings.OverlayTargetLevelRegionX),
                    (int)(sh * _settings.OverlayTargetLevelRegionY),
                    (int)(sw * _settings.OverlayTargetLevelRegionW),
                    (int)(sh * _settings.OverlayTargetLevelRegionH));
                var targetLevel = _ocrService.ExtractNumber(screenshot, levelRegion);

                // Target-HP (Zahl)
                var targetHpRegion = new System.Drawing.Rectangle(
                    (int)(sw * _settings.OverlayTargetHpRegionX),
                    (int)(sh * _settings.OverlayTargetHpRegionY),
                    (int)(sw * _settings.OverlayTargetHpRegionW),
                    (int)(sh * _settings.OverlayTargetHpRegionH));
                var targetHp = _ocrService.ExtractNumber(screenshot, targetHpRegion);

                // Target-Distanz (Zahl)
                var targetDistRegion = new System.Drawing.Rectangle(
                    (int)(sw * _settings.OverlayTargetDistRegionX),
                    (int)(sh * _settings.OverlayTargetDistRegionY),
                    (int)(sw * _settings.OverlayTargetDistRegionW),
                    (int)(sh * _settings.OverlayTargetDistRegionH));
                var targetDist = _ocrService.ExtractNumber(screenshot, targetDistRegion);

                Dispatcher.Invoke(() =>
                {
                    TargetNameText.Text = !string.IsNullOrWhiteSpace(targetName) ? targetName : "--";
                    TargetLevelText.Text = targetLevel.HasValue ? targetLevel.Value.ToString() : "--";
                    TargetHpText.Text = targetHp.HasValue ? targetHp.Value.ToString() : "--";
                    TargetDistText.Text = targetDist.HasValue ? targetDist.Value.ToString() : "--";
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[InGameOverlay] OCR Target-Info Fehler: " + ex.Message);
            }
        }

        // PropertyChanged helper (simplified)
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) { }
    }

    // FEAT-031: Skill cooldown data model for overlay binding
        public class SkillCooldown : System.ComponentModel.INotifyPropertyChanged
        {
            public string KeyName { get; }
            public System.Windows.Media.Color Color { get; }

            private int _remainingSeconds;
            public int RemainingSeconds
            {
                get => _remainingSeconds;
                set
                {
                    if (_remainingSeconds != value)
                    {
                        _remainingSeconds = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(RemainingTime));
                        OnPropertyChanged(nameof(RemainingPercent));
                    }
                }
            }

            private readonly int _maxSeconds;

            public string RemainingTime => RemainingSeconds > 0 ? RemainingSeconds.ToString() : "0";

            public double RemainingPercent => _maxSeconds > 0
                ? Math.Clamp((double)RemainingSeconds / _maxSeconds, 0.0, 1.0)
                : 0.0;

            public SkillCooldown(string keyName, int remainingSeconds, int maxSeconds, System.Windows.Media.Color color)
            {
                KeyName = keyName;
                _maxSeconds = maxSeconds;
                _remainingSeconds = remainingSeconds;
                Color = color;
            }

            public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
                => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
                }

    // FEAT-032: Buff/Debuff timer data model for overlay binding
    public class BuffTimerViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public string Name { get; }
        public bool IsDebuff { get; }
        public int MaxSec { get; }

        private int _remainingSec;
        public int RemainingSec
        {
            get => _remainingSec;
            set
            {
                if (_remainingSec != value)
                {
                    _remainingSec = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RemainingText));
                    OnPropertyChanged(nameof(RemainingPercent));
                    OnPropertyChanged(nameof(IsWarning));
                }
            }
        }

        public string RemainingText => RemainingSec > 0 ? $"{RemainingSec}s" : "0s";

        public double RemainingPercent => MaxSec > 0
            ? Math.Clamp((double)RemainingSec / MaxSec, 0.0, 1.0)
            : 0.0;

        public bool IsWarning => RemainingSec > 0 && RemainingSec <= WarnSec;

        public int WarnSec { get; }

        public System.Windows.Media.Color BarColor => IsDebuff
            ? System.Windows.Media.Color.FromRgb(239, 68, 68)   // Red for debuffs
            : System.Windows.Media.Color.FromRgb(59, 130, 246); // Blue for buffs

        public System.Windows.Media.Color WarnColor => System.Windows.Media.Color.FromRgb(250, 204, 21); // Amber for warning

        public BuffTimerViewModel(string name, bool isDebuff, int remainingSec, int maxSec, int warnSec = 5)
        {
            Name = name;
            IsDebuff = isDebuff;
            MaxSec = maxSec;
            WarnSec = warnSec;
            _remainingSec = remainingSec;
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }

    // FEAT-036: Damage Number data model for overlay binding
    public class DamageNumber : System.ComponentModel.INotifyPropertyChanged
    {
        public int Amount { get; }
        public string AmountText => Amount > 0 ? "-" + Amount : Amount.ToString();
        public double X { get; }
        public double Y { get; }

        private double _opacity = 1.0;
        public double Opacity
        {
            get => _opacity;
            set
            {
                if (_opacity != value)
                {
                    _opacity = value;
                    OnPropertyChanged();
                }
            }
        }

        public DamageNumber(int amount, double x, double y)
        {
            Amount = amount;
            X = x;
            Y = y;

            // Fade out over time
            var fadeTimer = new System.Windows.Threading.DispatcherTimer();
            fadeTimer.Interval = TimeSpan.FromMilliseconds(50);
            int fadeStep = 0;
            fadeTimer.Tick += (s, e) =>
            {
                fadeStep++;
                Opacity = Math.Max(0, 1.0 - (fadeStep / 20.0));
                if (Opacity <= 0) fadeTimer.Stop();
            };
            fadeTimer.Start();
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
}