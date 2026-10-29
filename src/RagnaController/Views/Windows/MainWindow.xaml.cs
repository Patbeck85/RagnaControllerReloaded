using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;
using RagnaController.ControllerTest;
using RagnaController.Core.Services.ProfileManagement;
using RagnaController.Presentation.Services;
using RagnaController.Views.Overlays;
using RagnaController.Views.Editors;
using RagnaController.Views.Dialogs;

namespace RagnaController.Views.Windows
{
    /// <summary>
    /// TASK-2106: ClientTabItem — represents a single RO client in the multi-client tab bar.
    /// </summary>
    public class ClientTabItem : INotifyPropertyChanged
    {
        public IntPtr Hwnd { get; set; }
        public string ProcessName { get; set; } = "";
        public uint Pid { get; set; }
        public string DisplayName => $"{ProcessName} (PID {Pid})";
        public string ProfileName { get; set; } = "—";

        private EngineStatus _status = EngineStatus.Stopped;
        public EngineStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); OnPropertyChanged(nameof(StatusColor)); }
        }

        public System.Windows.Media.Brush StatusColor => Status switch
        {
            EngineStatus.Running => System.Windows.Media.Brushes.LimeGreen,
            EngineStatus.NoController => System.Windows.Media.Brushes.Gray,
            _ => System.Windows.Media.Brushes.Red
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// MainWindow — Primary application window with multi-client tab navigation and status display.
    /// Hosts the main UI layout, engine status indicators, and window switching controls.
    /// </summary>
    public partial class MainWindow : Window
    {
        // ── Multi-Client Engine Orchestrator ──
        private readonly MultiClientEngineOrchestrator _multiEngine;
        private readonly ProfileManager _manager;
        private Settings _settings = Settings.Load();
        private readonly List<IDisposable> _subs = new();
        private bool _isMiniMode = false;
        private bool _isBigPictureMode = false;
        private readonly List<string> _logBuffer = new();
        private MiniModeWindow? _miniWindow;
        private readonly ToastService _toastService = new();
        private readonly ProfileService _profileService;

        // Client Tab Management
        public ObservableCollection<ClientTabItem> ClientTabs { get; } = new();
        private ClientTabItem? _selectedClientTab;

        // ── TASK-1604: Multi-Client Client-Switcher ──
        private readonly INativeMethods _nativeMethods = new NativeMethodsImpl();
        private readonly IProcessNameResolver _processNames = new ProcessNameResolver();
        private MultiClientWindowTracker? _windowTracker;
        private MultiClientInputRouter? _multiClientRouter;
        private bool _clientComboUpdating = false;

        // ── TASK-1611: Profil-Preset-System ──
        private readonly ProfilePresetManager _presetManager = null!;
        private bool _presetComboUpdating = false;

        private MainViewModel? _vm;
        private DiscordRpcService? _discordRpc;
        private TtsAnnouncerService? _ttsService;
        private InGameOverlayWindow? _gameOverlay;
        private ControllerTestWindow? _controllerTestWindow;
        // UX-012: Gamepad-Navigation in der Hauptfenster (DPad/Tab-Fokus)
        private GamepadUiNavigator? _navigator;

#pragma warning disable CS0169 // Event handler delegates for proper cleanup in Window_Closing - assigned externally
        private Action<EngineInstance, EngineStatus>? _statusChangedHandler;
        private Action<int>? _profileQuickSwitchHandler;
        private Action<EngineInstance, string>? _logMessageHandler;
        private Action<string>? _batteryChangedHandler;
        private Action<string>? _controllerConnectedHandler;
        private Action? _controllerDisconnectedHandler;
        private Action? _restoreMainWindowRequestedHandler;
#pragma warning restore CS0169

#pragma warning disable CS0649 // WPF: Fields initialized in XAML
        // Toast/Notifications (NOT in XAML - created dynamically)
        private Border? ToastBorder;
        private TextBlock? ToastText;
        private SolidColorBrush? ToastBg;
#pragma warning restore CS0649

        /// <summary>
        /// Constructor for multi-client mode (uses MultiClientEngineOrchestrator)
        /// </summary>
        public MainWindow(MultiClientEngineOrchestrator multiEngine, ProfileManager manager, MainViewModel vm)
        {
            _multiEngine = multiEngine;
            _manager = manager;
            _vm = vm;
            InitializeComponent();
            _toastService.Initialize(ToastBorder!, ToastText!, ToastBg!);
            App.InitializeToastService(_toastService);
            _profileService = new ProfileService(_manager, _settings, null, msg => ShowToast(msg));
            DataContext = vm;

            // Bind ClientTabs to TabControl
            ClientTabControl.ItemsSource = ClientTabs;

            // UX-019: Keyboard shortcuts
            PreviewKeyDown += Window_PreviewKeyDown;

            // UX-012: Gamepad-Navigation (DPad/Tab) — vor der VM-Guard-Klausel initialisieren,
            // damit auch bei early return der Navigator sauber in Window_Closing disposed is.
            _navigator = new GamepadUiNavigator(_multiEngine.SharedControllerService) { ActiveWindow = this };

            // Initialize ViewModel first before setting up services
            if (vm == null)
            {
                ShowToast("ViewModel not initialized", isError: true);
                return;
            }

            _discordRpc = new DiscordRpcService(_settings);
            _ttsService = new TtsAnnouncerService(_settings);

            // Subscribe to MultiClientEngineOrchestrator events
            _multiEngine.ClientStarted += (sender, instance) => Dispatcher.BeginInvoke(() => AddClientTab(instance));
            _multiEngine.ClientStopped += (sender, hwnd) => Dispatcher.BeginInvoke(() => RemoveClientTab(hwnd));
            _multiEngine.LogMessage += (instance, msg) => Dispatcher.BeginInvoke(() => ShowToast($"[{instance.ProcessName}] {msg}"));

            // Subscribe to active client instance events
            _multiEngine.ClientStarted += (sender, instance) => SubscribeToInstanceEvents(instance);

            // Initialize multi-client services
            InitializeMultiClientServices();
            RefreshClientList();

            // Initialize preset manager
            _presetManager = new ProfilePresetManager();
            RefreshPresetList();

            // Initialize character class list
            RefreshClassList();
        }

        /// <summary>
        /// Initializes the multi-client window tracker and input router.
        /// Shared by both constructors to avoid duplicate initialization code.
        /// </summary>
        private void InitializeMultiClientServices()
        {
            _windowTracker = new MultiClientWindowTracker(_nativeMethods, _processNames);
            _multiClientRouter = new MultiClientInputRouter(_nativeMethods, _processNames, _windowTracker);
        }

        /// <summary>
        /// Constructor for single-client mode (uses HybridEngine) - backwards compatibility
        /// </summary>
        public MainWindow(HybridEngine engine, ProfileManager manager, MainViewModel vm)
        {
            _multiEngine = null!;
            _manager = manager;
            _vm = vm;
            InitializeComponent();
            _toastService.Initialize(ToastBorder!, ToastText!, ToastBg!);
            App.InitializeToastService(_toastService);
            _profileService = new ProfileService(_manager, _settings, engine, msg => ShowToast(msg));
            DataContext = vm;

            // Bind ClientTabs to TabControl (empty in single-client mode)
            ClientTabControl.ItemsSource = ClientTabs;

            // UX-019: Keyboard shortcuts
            PreviewKeyDown += Window_PreviewKeyDown;

            // UX-012: Gamepad-Navigation (DPad/Tab)
            _navigator = new GamepadUiNavigator(engine.ControllerSvc) { ActiveWindow = this };

            if (vm == null)
            {
                ShowToast("ViewModel not initialized", isError: true);
                return;
            }

            _discordRpc = new DiscordRpcService(_settings);
            _ttsService = new TtsAnnouncerService(_settings);

            // Initialize multi-client services (for client switcher UI)
            InitializeMultiClientServices();
            RefreshClientList();

            // Initialize preset manager
            _presetManager = new ProfilePresetManager();
            RefreshPresetList();

            // Initialize character class list
            RefreshClassList();
        }

        private void SubscribeToInstanceEvents(EngineInstance instance)
        {
            instance.StatusChanged += (inst, status) => Dispatcher.BeginInvoke(() =>
            {
                var tab = ClientTabs.FirstOrDefault(t => t.Hwnd == inst.ClientHwnd);
                if (tab != null) tab.Status = status;
            });

            instance.SnapshotUpdated += (inst, snap) => Dispatcher.BeginInvoke(() =>
                        {
                            if (inst.ClientHwnd == _selectedClientTab?.Hwnd)
                            {
                                _vm?.ApplySnapshot(snap);
                            }
                        });

                        instance.LogMessage += (inst, msg) => Dispatcher.BeginInvoke(() =>
                        {
                            if (inst.ClientHwnd == _selectedClientTab?.Hwnd)
                            {
                                _logBuffer.Add($"[{inst.ProcessName}] {msg}");
                                if (_logBuffer.Count > 500) _logBuffer.RemoveRange(0, 100);
                                ApplyLogFilter();
                                LogScrollViewer?.ScrollToEnd();
                            }
                        });
        }

        private void AddClientTab(EngineInstance instance)
        {
            var tab = new ClientTabItem
            {
                Hwnd = instance.ClientHwnd,
                ProcessName = instance.ProcessName,
                Pid = instance.ProcessId,
                Status = instance.Status
            };
            ClientTabs.Add(tab);

            // Auto-select first tab
            if (ClientTabs.Count == 1)
            {
                ClientTabControl.SelectedItem = tab;
            }
        }

        private void RemoveClientTab(IntPtr hwnd)
        {
            var tab = ClientTabs.FirstOrDefault(t => t.Hwnd == hwnd);
            if (tab != null) ClientTabs.Remove(tab);
        }

        private void ClientTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClientTabControl.SelectedItem is ClientTabItem selectedTab)
            {
                _selectedClientTab = selectedTab;
                var instance = _multiEngine.GetInstance(selectedTab.Hwnd);
                if (instance != null)
                {
                    // Update MultiClientInputRouter to route to this client
                    _multiClientRouter!.SetPreferredClientHwnd(selectedTab.ProcessName, selectedTab.Hwnd);
                    _multiClientRouter!.SetActiveProcessName(selectedTab.ProcessName);

                    // Update UI for selected client
                    UpdateUIForClient(instance);
                }
            }
        }

        private void UpdateUIForClient(EngineInstance instance)
        {
            // Update profile combo for this client
            ProfileCombo.ItemsSource = _profileService.GetAllProfiles();
            var profile = _profileService.LoadLastOrFirst();
            if (profile != null)
            {
                ProfileCombo.SelectedItem = profile;
            }
        }

        public void SwitchFromMiniMode()
        {
            _isMiniMode = false;
            Show(); WindowState = WindowState.Normal; Activate();
            if (_miniWindow != null)
            {
                var w = _miniWindow;
                _miniWindow = null;
                try { w.Close(); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] SwitchFromMiniMode Close failed: {ex.Message}");
                    ShowToast($"Mini-Modus schließen fehlgeschlagen: {ex.Message}", isError: true);
                }
            }
        }

        private void RestoreWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void SwitchToMiniMode()
        {
            if (_isMiniMode) return;
            _isMiniMode = true;
            _miniWindow = new MiniModeWindow { Owner = this };
            _miniWindow.Closed += (_, _) =>
            {
                if (_isMiniMode) SwitchFromMiniMode();
            };
            _miniWindow.Show();
            Hide();
        }

        private void Window_MouseLeftButtonDown(object s, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void BtnMinimize_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnClose_Click(object s, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnBigPicture_Click(object s, RoutedEventArgs e)
        {
            // Big Picture Mode for Steam Deck: Maximieren + UI-Skalierung aktivieren
            _isBigPictureMode = !_isBigPictureMode;
            ApplyBigPictureMode(_isBigPictureMode);
        }

        /// <summary>
        /// Wendet das Big Picture Mode Layout an: 150% Font Scaling,
        /// min. 44×44px touch targets, larger spacing for touch operation.
        /// </summary>
        private void ApplyBigPictureMode(bool enabled)
        {
            // Fenster maximiert for Big Picture
            WindowState = enabled ? WindowState.Maximized : WindowState.Normal;

            // Design-System Resource for Font Scale setzen (about App.Resources)
            var scaleKey = enabled ? "FontScaleLarge" : "FontScaleNormal";
            if (Application.Current.Resources.Contains(scaleKey))
            {
                Application.Current.Resources["FontScaleNormal"] = Application.Current.Resources[scaleKey];
            }

            // Enlarge touch targets for main buttons (min. 44×44px)
            SetButtonMinSize(BtnCalibrateQuick, enabled ? 56 : 44);
            SetButtonMinSize(BtnTestInput, enabled ? 56 : 44);
            SetButtonMinSize(BtnOpenRemap, enabled ? 56 : 44);
            SetButtonMinSize(BtnRadial, enabled ? 56 : 44);
            SetButtonMinSize(BtnBigPicture, enabled ? 48 : 36);

            // Enlarge window controls (Min/Max/Close) for touch
            SetButtonMinSize(BtnMinimize, enabled ? 48 : 36);
            SetButtonMinSize(BtnMaximize, enabled ? 48 : 36);
            SetButtonMinSize(BtnClose, enabled ? 48 : 36);

            // Enlarge ComboBox for touch
            if (ProfileCombo != null)
            {
                ProfileCombo.MinHeight = enabled ? 44 : 32;
                ProfileCombo.Height = enabled ? 44 : double.NaN;
            }

            // Log-Buttons for Touch
            SetButtonMinSize(BtnClearLog, enabled ? 44 : 28);
            SetButtonMinSize(BtnExportLog, enabled ? 44 : 28);

            System.Diagnostics.Debug.WriteLine($"[MainWindow] Big Picture Mode: {(enabled ? "ON" : "OFF")}");
        }

        private void SetButtonMinSize(Button? btn, double minSize)
        {
            if (btn != null)
            {
                btn.MinWidth = minSize;
                btn.MinHeight = minSize;
                if (btn.Width == 0 || btn.Width == double.NaN) btn.Width = minSize;
                if (btn.Height == 0 || btn.Height == double.NaN) btn.Height = minSize;
            }
        }

        private void TabBase_Click(object s, RoutedEventArgs e) => SelectTab(PanelBase, null);
        private void TabL1_Click(object s, RoutedEventArgs e) => SelectTab(PanelL1, null);
        private void TabR1_Click(object s, RoutedEventArgs e) => SelectTab(PanelR1, null);
        private void TabL2_Click(object s, RoutedEventArgs e) => SelectTab(PanelL2, null);
        private void TabR2_Click(object s, RoutedEventArgs e) => SelectTab(PanelR2, null);
        private void TabHealth_Click(object s, RoutedEventArgs e) => SelectTab(PanelHealth, null);
        private void TabTelemetry_Click(object s, RoutedEventArgs e) => SelectTelemetryTab();

        /// <summary>UI-011: Telemetrie-Tab aktivieren (no Mapping-Panel → eigener Pfad).</summary>
        private void SelectTelemetryTab()
        {
            // All Mapping-Panels ausblenden
            var allPanels = new[] { PanelBase, PanelL1, PanelR1, PanelL2, PanelR2, PanelHealth };
            foreach (var p2 in allPanels)
                if (p2 != null) p2.Visibility = Visibility.Collapsed;

            // Reset aller Tab-Buttons inkl. Telemetrie
            var allTabBtns = new[] { TabBtnBase, TabBtnL1, TabBtnR1, TabBtnL2, TabBtnR2, TabBtnHealth, TabBtnTelemetry };
            foreach (var btn in allTabBtns)
                if (btn != null) btn.Style = (Style)FindResource("TabButton");

            if (TabBtnTelemetry != null) TabBtnTelemetry.Style = (Style)FindResource("TabButtonActive");

            // Log-Panel ausblenden (konsistent with Mapping-Tabs)
            if (LogPanel != null)
                LogPanel.Visibility = Visibility.Collapsed;

            TelemetryPanelControl.Visibility = Visibility.Visible;
            _activeTabPanel = null; // Telemetrie ist kein Profil-Mapping-Panel
        }

        private Border? _activeTabPanel;
        private readonly Dictionary<object, Button?> _panelToTabBtn = new();
        private void InitTabBtnMap()
        {
            _panelToTabBtn[PanelBase] = TabBtnBase;
            _panelToTabBtn[PanelL1] = TabBtnL1;
            _panelToTabBtn[PanelR1] = TabBtnR1;
            _panelToTabBtn[PanelL2] = TabBtnL2;
            _panelToTabBtn[PanelR2] = TabBtnR2;
            _panelToTabBtn[PanelHealth] = TabBtnHealth;
        }

        private void SelectTab(Border? panel, Border? mappings, bool showInfo = false)
        {
            // Hide all mapping panels including Health + Telemetrie (UI-011)
            var allPanels = new[] { PanelBase, PanelL1, PanelR1, PanelL2, PanelR2, PanelHealth };
            foreach (var p2 in allPanels)
                if (p2 != null) p2.Visibility = Visibility.Collapsed;

            // Telemetrie-Panel ausblenden (eigener Tab-Pfad, UI-011)
            if (TelemetryPanelControl != null)
                TelemetryPanelControl.Visibility = Visibility.Collapsed;

            // Reset all tab button styles
            var allTabBtns = new[] { TabBtnBase, TabBtnL1, TabBtnR1, TabBtnL2, TabBtnR2, TabBtnHealth, TabBtnTelemetry };
            foreach (var btn in allTabBtns)
                if (btn != null) btn.Style = (Style)FindResource("TabButton");

            // Activate the correct tab button
            Button? activeBtn = panel switch
            {
                _ when panel == PanelBase => TabBtnBase,
                _ when panel == PanelL1 => TabBtnL1,
                _ when panel == PanelR1 => TabBtnR1,
                _ when panel == PanelL2 => TabBtnL2,
                _ when panel == PanelR2 => TabBtnR2,
                _ when panel == PanelHealth => TabBtnHealth,
                _ => null
            };
            if (activeBtn != null) activeBtn.Style = (Style)FindResource("TabButtonActive");

            bool isMappingTab = panel != null;

            // Show/hide Log panel — hidden when a mapping tab is active
            if (LogPanel != null)
                LogPanel.Visibility = isMappingTab ? Visibility.Collapsed : Visibility.Visible;

            if (panel != null)
            {
                panel.Visibility = Visibility.Visible;
                _activeTabPanel = panel;
                PopulateTabPanel(panel);
            }
        }

        private void PopulateTabPanel(Border panel)
        {
            if (ProfileCombo?.SelectedItem is not Profiles.Profile p)
            {
                // Show empty-state with CTA to Profile Library when no profile selected
                var emptyStack = new StackPanel { Margin = new Thickness(16), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

                var icon = new TextBlock
                {
                    Text = "📋",
                    FontSize = 48,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 16)
                };
                emptyStack.Children.Add(icon);

                var title = new TextBlock
                {
                    Text = GetLocalizedString("Tab_NoProfileLoaded"),
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(201, 209, 217)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                emptyStack.Children.Add(title);

                var desc = new TextBlock
                {
                    Text = GetLocalizedString("Tab_NoProfileLoaded_Desc"),
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(136, 144, 156)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 300,
                    Margin = new Thickness(0, 0, 0, 24)
                };
                emptyStack.Children.Add(desc);

                var ctaBtn = new Button
                {
                    Content = GetLocalizedString("Tab_OpenProfileLibrary"),
                    Style = (Style)FindResource("PrimaryButton"),
                    Width = 200,
                    Height = 44,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                ctaBtn.Click += (s, e) => OpenProfileLibrary();
                emptyStack.Children.Add(ctaBtn);

                panel.Child = new ScrollViewer { Content = emptyStack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                return;
            }

            // Determine which button group this panel represents
            string layerKey = panel == PanelBase ? "" : panel == PanelL1 ? "L1+" :
                              panel == PanelR1 ? "R1+" : panel == PanelL2 ? "L2+" : panel == PanelR2 ? "R2+" : "";

            var stack = new StackPanel { Margin = new Thickness(8) };

            // Health Panel has special content
            if (panel == PanelHealth)
            {
                var healthStack = new StackPanel();

                // Engine Status
                var engineStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                engineStack.Children.Add(new TextBlock
                {
                    Text = "Engine:",
                    Foreground = new SolidColorBrush(Color.FromRgb(161, 176, 197)),
                    FontSize = 10.5, FontWeight = System.Windows.FontWeights.SemiBold, Margin = new Thickness(0, 0, 8, 0)
                });
                var activeInstance = _multiEngine?.GetActiveInstance();
                engineStack.Children.Add(new TextBlock
                {
                    Text = activeInstance?.Orchestrator.IsRunning == true ? "RUNNING" : "STOPPED",
                    Foreground = Brushes.Lime,
                    FontSize = 13, FontWeight = System.Windows.FontWeights.Bold
                });
                var engineBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(10, 12, 20)),
                    CornerRadius = new CornerRadius(4), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 8),
                    Child = engineStack
                };
                healthStack.Children.Add(engineBorder);

                // Profile Info
                var profileStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                profileStack.Children.Add(new TextBlock
                {
                    Text = "Profile:",
                    Foreground = new SolidColorBrush(Color.FromRgb(161, 176, 197)),
                    FontSize = 10.5, FontWeight = System.Windows.FontWeights.SemiBold, Margin = new Thickness(0, 0, 8, 0)
                });
                profileStack.Children.Add(new TextBlock
                {
                    Text = p.Name ?? "None",
                    Foreground = Brushes.White,
                    FontSize = 11, FontWeight = System.Windows.FontWeights.SemiBold
                });
                var profileBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(10, 12, 20)),
                    CornerRadius = new CornerRadius(4), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 8),
                    Child = profileStack
                };
                healthStack.Children.Add(profileBorder);

                var classStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                classStack.Children.Add(new TextBlock
                {
                    Text = "Class:",
                    Foreground = new SolidColorBrush(Color.FromRgb(161, 176, 197)),
                    FontSize = 10.5, FontWeight = System.Windows.FontWeights.SemiBold, Margin = new Thickness(0, 0, 8, 0)
                });
                classStack.Children.Add(new TextBlock
                {
                    Text = p.Class ?? "N/A",
                    Foreground = new SolidColorBrush(Color.FromRgb(229, 184, 66)),
                    FontSize = 11, FontWeight = System.Windows.FontWeights.SemiBold
                });
                var classBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(10, 12, 20)),
                    CornerRadius = new CornerRadius(4), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 8),
                    Child = classStack
                };
                healthStack.Children.Add(classBorder);

                // Battery Status
                var batteryStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                batteryStack.Children.Add(new TextBlock
                {
                    Text = "Battery:",
                    Foreground = new SolidColorBrush(Color.FromRgb(161, 176, 197)),
                    FontSize = 10.5, FontWeight = System.Windows.FontWeights.SemiBold, Margin = new Thickness(0, 0, 8, 0)
                });
                var activeInst = _multiEngine?.GetActiveInstance();
                batteryStack.Children.Add(new Ellipse { Width = 7, Height = 7, VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Fill = new SolidColorBrush(activeInst?.Orchestrator.Controller?.BatteryLevel switch {
                        "Full" or "High" => Color.FromRgb(57, 255, 20),
                        "Mid" => Color.FromRgb(255, 184, 0),
                        "Low" or "Empty" => Color.FromRgb(255, 58, 82),
                        _ => Color.FromRgb(85, 94, 106)
                    })
                });
                batteryStack.Children.Add(new TextBlock
                {
                    Text = activeInst?.Orchestrator.Controller?.BatteryLevel ?? "-",
                    Foreground = new SolidColorBrush(activeInst?.Orchestrator.Controller?.BatteryLevel switch {
                        "Full" or "High" => Color.FromRgb(57, 255, 20),
                        "Mid" => Color.FromRgb(255, 184, 0),
                        "Low" or "Empty" => Color.FromRgb(255, 58, 82),
                        _ => Color.FromRgb(85, 94, 106)
                    }),
                    FontSize = 11, VerticalAlignment = System.Windows.VerticalAlignment.Center
                });
                var batteryBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(10, 12, 20)),
                    CornerRadius = new CornerRadius(4), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 8),
                    Child = batteryStack
                };
                healthStack.Children.Add(batteryBorder);

                if (!healthStack.Children.OfType<UIElement>().Any())
                {
                    healthStack.Children.Add(new TextBlock
                    {
                        Text = GetLocalizedString("Tab_NoMappings"),
                        Foreground = new SolidColorBrush(Color.FromRgb(85, 94, 106)),
                        FontSize = 11, Margin = new Thickness(0, 8, 0, 0)
                    });
                }

                panel.Child = new ScrollViewer { Content = healthStack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                return;
            }

            var relevantMappings = p.ButtonMappings
                            .Where(kv => layerKey == ""
                                ? !kv.Key.ToString().Contains('+')
                                : kv.Key.ToString().StartsWith(layerKey))
                            .OrderBy(kv => kv.Key.ToString());

            foreach (var kv in relevantMappings)
            {
                var row = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                row.Children.Add(new TextBlock
                {
                    Text = kv.Key.ToString().Replace(layerKey, "") + ":",
                    Width = 60,
                    Foreground = new SolidColorBrush(Color.FromRgb(161, 176, 197)),
                    FontSize = 11
                });
                row.Children.Add(new TextBlock
                {
                    Text = kv.Value.Label ?? kv.Value.Type.ToString(),
                    Foreground = new SolidColorBrush(Color.FromRgb(229, 184, 66)),
                    FontSize = 11
                });
                stack.Children.Add(row);
            }

            if (!stack.Children.OfType<UIElement>().Any())
            {
                stack.Children.Add(new TextBlock
                {
                    Text = GetLocalizedString("Tab_NoMappings"),
                    Foreground = new SolidColorBrush(Color.FromRgb(85, 94, 106)),
                    FontSize = 11, Margin = new Thickness(0, 8, 0, 0)
                });
            }

            panel.Child = new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private void LogFilter_Changed(object s, RoutedEventArgs e) => ApplyLogFilter();

        private void ApplyLogFilter()
        {
            if (LogTextBlock == null || _logBuffer.Count == 0) return;

            bool showEngine = LogFilterEngine?.IsChecked ?? true;
            bool showInput = LogFilterInput?.IsChecked ?? true;
            bool showProfile = LogFilterProfile?.IsChecked ?? true;

            var filtered = _logBuffer.Where(msg =>
            {
                if (msg.Contains("[Engine]") || msg.Contains("[HybridEngine]") || msg.Contains("[Tick]"))
                    return showEngine;
                if (msg.Contains("[Input]") || msg.Contains("[Controller]") || msg.Contains("[SDL]"))
                    return showInput;
                if (msg.Contains("[Profile]") || msg.Contains("[ProfileManager]"))
                    return showProfile;
                return true; // Show untagged messages always
            });

            LogTextBlock.Text = string.Join(Environment.NewLine, filtered);

            // Auto-scroll to bottom
            if (LogScrollViewer != null)
                LogScrollViewer.ScrollToBottom();
        }

        private void BtnLogClear_Click(object s, RoutedEventArgs e)
        {
            _logBuffer.Clear();
            if (LogTextBlock != null) LogTextBlock.Text = string.Empty;
            ShowToast("Log cleared");
        }

        private void BtnLogExport_Click(object s, RoutedEventArgs e)
        {
            try
            {
                var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", $"ragna_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
                File.WriteAllLines(path, _logBuffer);
                ShowToast($"Exported: {System.IO.Path.GetFileName(path)}");
            }
            catch (Exception ex) { ShowToast($"Export failed: {ex.Message}", isError: true); }
        }

        private void DeadzoneLabel_DblClick(object s, RoutedEventArgs e) => DeadzoneReset_Click(null!, null!);
        private void CurveLabel_DblClick(object s, RoutedEventArgs e) => CurveReset_Click(null!, null!);
        private void ActionSpeedLabel_DblClick(object s, RoutedEventArgs e) => ActionSpeedReset_Click(null!, null!);
        private void SensitivityLabel_DblClick(object s, RoutedEventArgs e) => SensitivityReset_Click(null!, null!);

        private void BtnToggleMiniMode_Click(object s, RoutedEventArgs e) => SwitchToMiniMode();
        private void BtnToggleGameOverlay_Click(object s, RoutedEventArgs e)
        {
            if (_gameOverlay == null)
            {
                var activeInstance = _multiEngine?.GetActiveInstance();
                _gameOverlay = new InGameOverlayWindow(
                    activeInstance!.Orchestrator.Messenger,
                    activeInstance!.Orchestrator.WinTracker,
                    activeInstance!.Orchestrator.ControllerManager,
                    _settings,
                    buffManager: activeInstance!.Orchestrator.BuffManager);
                _gameOverlay.Show();
            }
            else
            {
                _gameOverlay?.Close();
                _gameOverlay = null;
            }
        }

        private void SetFooterButtonsEnabled(bool enabled)
        {
            if (BtnCalibrateQuick != null) BtnCalibrateQuick.IsEnabled = enabled;
            if (BtnTestInput != null) BtnTestInput.IsEnabled = enabled;
            if (BtnOpenRemap != null) BtnOpenRemap.IsEnabled = enabled;
            if (BtnRadial != null) BtnRadial.IsEnabled = enabled;
        }

        private void ShowControllerTestWindow()
        {
            var activeInstance = _multiEngine?.GetActiveInstance();
            if (_controllerTestWindow == null && activeInstance?.Orchestrator.ControllerSvc != null)
            {
                _controllerTestWindow = new ControllerTestWindow(activeInstance.Orchestrator.ControllerSvc);
                _controllerTestWindow.Closed += (s, e) => _controllerTestWindow = null;
                _controllerTestWindow.Show();
            }
        }

        private void Window_Closing(object s, EventArgs e)
        {
            // UX-012: Gamepad-Navigation stoppen (MEMORY-001: Timer sauber freigeben)
            _navigator?.Stop();
            _navigator?.Dispose();

            // UI-011: Stop telemetry timer (MEMORY-001: no hanging DispatcherTimers)
            if (TelemetryPanelControl != null)
                TelemetryPanelControl.StopUpdates();

            // Cleanup subscriptions
            foreach (var sub in _subs)
                try { sub.Dispose(); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] Subscription dispose failed: {ex.Message}");
                }
            _subs.Clear();

            // Unsubscribe engine events (MEMORY-001: prevent leaked references)
            // Note: MultiClientEngineOrchestrator doesn't have LogMessage event, it's on EngineInstance

            // Dispose multi-client router (MEMORY-001)
            _multiClientRouter?.Dispose();
            _windowTracker?.Dispose();

            // Save settings
            if (_manager.ActiveProfile != null)
                _settings.LastProfileName = _manager.ActiveProfile.Name;
            _settings.Save();

            // Close game overlay if open
            _gameOverlay?.Close();
        }

        // Helper methods
        private void ShowToast(string message, bool isError = false)
        {
            _toastService.Show(message, isError);
        }

        private static string GetLocalizedString(string key) => LocalizationManager.GetLocalizedString(key);

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize event handlers after component loading
            // UX-012: Gamepad-Navigation starten (DPad/Tab-Fokus in Hauptfenster)
            _navigator?.Start();
        }

        /// <summary>
        /// UX-019: Keyboard shortcuts for power users.
        /// Ctrl+O: Open Profile Library
        /// Ctrl+S: Save settings
        /// F1: Show help tooltip
        /// Esc: Exit Mini Mode / Close overlays
        /// </summary>
        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var kb = Keyboard.Modifiers;
            bool isCtrl = (kb & ModifierKeys.Control) == ModifierKeys.Control;

            if (isCtrl && e.Key == System.Windows.Input.Key.O)
            {
                e.Handled = true;
                OpenProfileLibrary();
                ShowToast("Profile Library opened (Ctrl+O)");
            }
            else if (isCtrl && e.Key == System.Windows.Input.Key.S)
            {
                e.Handled = true;
                _settings.Save();
                ShowToast("Settings saved (Ctrl+S)");
            }
            else if (e.Key == System.Windows.Input.Key.F1)
            {
                e.Handled = true;
                ShowToast("Help: Ctrl+O Profile Library · Ctrl+S Save · Esc Exit Mini Mode");
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
                if (_isMiniMode)
                    SwitchFromMiniMode();
                else if (_gameOverlay != null)
                {
                    _gameOverlay.Close();
                    _gameOverlay = null;
                }
            }
        }

        private void BtnScanController_Click(object s, RoutedEventArgs e)
        {
            var activeInstance = _multiEngine?.GetActiveInstance();
            activeInstance?.Start();
            ShowToast("Controller scan initiated");
        }

        private void BtnSettings_Click(object s, RoutedEventArgs e)
                {
                    var activeInstance = _multiEngine?.GetActiveInstance();
                    var settings = new SettingsWindow(_manager, _settings, s =>
                    {
                        // Settings are applied via onSave callback
                        _settings = s; // Update local reference
                    });
                    settings.Show();
                }

        private void ResetBtn_MouseEnter(object s, RoutedEventArgs e) { }
        private void ResetBtn_MouseLeave(object s, RoutedEventArgs e) { }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && _settings.MinimizeToTray)
            {
                Hide();
                if (_miniWindow == null)
                {
                    SwitchToMiniMode();
                }
            }
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private async void BtnCalibrateQuick_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var activeInstance = _multiEngine?.GetActiveInstance();
                if (activeInstance != null)
                    await activeInstance.Orchestrator.StartCalibrationAsync();
                ShowToast("Calibration started");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Calibration start failed: {ex.Message}");
                ShowToast($"Calibration failed: {ex.Message}", isError: true);
            }
        }

        private void BtnTestInput_Click(object sender, RoutedEventArgs e)
        {
            var activeInstance = _multiEngine?.GetActiveInstance();
            if (activeInstance?.Orchestrator.ControllerSvc != null)
            {
                var testWindow = new ControllerTestWindow(activeInstance.Orchestrator.ControllerSvc);
                testWindow.Owner = this;
                testWindow.Show();
            }
            else
            {
                ShowToast("No controller connected", isError: true);
            }
        }

        private void BtnOpenRemap_Click(object sender, RoutedEventArgs e)
        {
            var activeInstance = _multiEngine?.GetActiveInstance();
            var w = new ButtonRemappingWindow(activeInstance!.Orchestrator, _manager);
            w.Owner = this;
            w.Show();
        }

        private void ProfileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileCombo.SelectedItem is Profile profile)
            {
                _profileService.ApplyProfile(profile);
            }
        }

        private void BtnRadial_Click(object sender, RoutedEventArgs e)
        {
            var activeInstance = _multiEngine?.GetActiveInstance();
            var queue = activeInstance?.Orchestrator.CommandQueue ?? new InputCommandQueue();

            // Verwende RoUiMenuService-Preset wenn aktiviert, sonst Default-Items
            var config = new RadialMenuWindow.RadialMenuConfig();
            if (_settings.RoUiMenuEnabled)
            {
                config.Items = RoUiMenuService.UiNavigationPreset().ToArray();
            }

            var w = new RadialMenuWindow(queue, config);
            w.Owner = this;
            w.Show();
        }

        private void OpenProfileLibrary()
        {
            _profileService.OpenProfileLibrary(() =>
            {
                // Refresh profile list after library closed
                ProfileCombo.ItemsSource = null;
                ProfileCombo.ItemsSource = _profileService.GetAllProfiles();
            });
        }

        // Reset click handlers for double-click on labels
        private void DeadzoneReset_Click(object sender, RoutedEventArgs e)
        {
            var activeInstance = _multiEngine?.GetActiveInstance();
            if (activeInstance?.Orchestrator.ProfileApplier != null)
            {
                activeInstance.Orchestrator.ProfileApplier.LiveUpdateDeadzone(0.15f);
                var pm = new ProfileManager();
                pm.SaveProfile(activeInstance!.Orchestrator.CurrentProfile!);
                ShowToast("Deadzone reset to default (0.15)");
            }
        }

        private void CurveReset_Click(object sender, RoutedEventArgs e)
        {
            var activeInstance = _multiEngine?.GetActiveInstance();
            if (activeInstance?.Orchestrator.ProfileApplier != null)
            {
                activeInstance.Orchestrator.ProfileApplier.LiveUpdateCurve(1.0f);
                var pm = new ProfileManager();
                pm.SaveProfile(activeInstance!.Orchestrator.CurrentProfile!);
                ShowToast("Curve reset to default (1.0)");
            }
        }

        private void ActionSpeedReset_Click(object sender, RoutedEventArgs e)
        {
            var activeInstance = _multiEngine?.GetActiveInstance();
            if (activeInstance?.Orchestrator.ProfileApplier != null)
            {
                activeInstance.Orchestrator.ProfileApplier.LiveUpdateActionSpeed(5.0f); // Default mid-point
                var pm = new ProfileManager();
                pm.SaveProfile(activeInstance!.Orchestrator.CurrentProfile!);
                ShowToast("Action Speed reset to default (5)");
            }
        }

        private void SensitivityReset_Click(object sender, RoutedEventArgs e)
        {
            var activeInstance = _multiEngine?.GetActiveInstance();
            if (activeInstance?.Orchestrator.ProfileApplier != null)
            {
                activeInstance.Orchestrator.ProfileApplier.LiveUpdateCursorSpeed(1.0f);
                var pm = new ProfileManager();
                pm.SaveProfile(activeInstance!.Orchestrator.CurrentProfile!);
                ShowToast("Sensitivity reset to default (1.0)");
            }
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            _logBuffer.Clear();
            if (LogTextBlock != null) LogTextBlock.Text = string.Empty;
            if (LogTextBlock2 != null) LogTextBlock2.Text = string.Empty;
            ShowToast("Log cleared");
        }

        // ── TASK-1604: Multi-Client Client-Switcher UI ──
        private void RefreshClientList()
        {
            try
            {
                _clientComboUpdating = true;
                ClientCombo.Items.Clear();

                var clients = _windowTracker!.GetClients(_multiEngine?.SharedProfileManager?.ActiveProfile?.Name ?? "ragexe");
                if (!clients.Any())
                {
                    // Fallback: alle bekannten Clients anzeigen
                    clients = _windowTracker!.GetClients("ragexe");
                }

                foreach (var client in clients)
                {
                    var item = new ClientComboItem
                    {
                        Hwnd = client.Hwnd,
                        Pid = client.Pid,
                        ProcessName = client.ProcessName,
                        DisplayName = $"{client.ProcessName} (PID {client.Pid}, HWND 0x{client.Hwnd:X4})"
                    };
                    ClientCombo.Items.Add(item);
                }

                if (ClientCombo.Items.Count > 0)
                {
                    ClientCombo.SelectedIndex = 0;
                    var first = ClientCombo.Items[0] as ClientComboItem;
                    if (first != null)
                    {
                        _multiClientRouter!.SetPreferredClientHwnd(first!.ProcessName, first!.Hwnd);
                    }
                }
                else
                {
                    ClientCombo.Items.Add(new ClientComboItem
                    {
                        Hwnd = IntPtr.Zero,
                        Pid = 0,
                        ProcessName = "<none>",
                        DisplayName = "<no RO client found>"
                    });
                    ClientCombo.SelectedIndex = 0;
                    ClientCombo.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] RefreshClientList failed: {ex.Message}");
                ShowToast($"Client refresh failed: {ex.Message}", isError: true);
            }
            finally
            {
                _clientComboUpdating = false;
            }
        }

        private void ClientCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_clientComboUpdating || ClientCombo.SelectedItem is not ClientComboItem item)
                return;

            _multiClientRouter!.SetPreferredClientHwnd(item!.ProcessName, item!.Hwnd);
            ShowToast($"Client switched to {item.ProcessName} (PID {item.Pid})");
        }

        private void BtnRefreshClients_Click(object sender, RoutedEventArgs e)
        {
            RefreshClientList();
            ShowToast("Client list refreshed");
        }

        private sealed class ClientComboItem
        {
            public IntPtr Hwnd { get; set; }
            public uint Pid { get; set; }
            public string ProcessName { get; set; } = "";
            public string DisplayName { get; set; } = "";

            public override string ToString() => DisplayName;
        }

        // ── TASK-1611: Profil-Preset-System UI ──
        private void RefreshPresetList()
        {
            try
            {
                _presetComboUpdating = true;
                PresetCombo.Items.Clear();

                foreach (var kvp in _presetManager.Presets)
                {
                    var preset = kvp.Value;
                    var display = preset.UseCount > 0
                        ? $"{preset.PresetName} → {preset.TargetProfileName} (×{preset.UseCount})"
                        : $"{preset.PresetName} → {preset.TargetProfileName}";
                    PresetCombo.Items.Add(new { Key = preset.PresetName, Display = display });
                }

                if (PresetCombo.Items.Count > 0)
                {
                    PresetCombo.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] RefreshPresetList failed: {ex.Message}");
            }
            finally
            {
                _presetComboUpdating = false;
            }
        }

        private void PresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_presetComboUpdating || PresetCombo.SelectedItem is not { } selected)
                return;

            var presetName = ((dynamic)selected).Key;
            var preset = _presetManager.GetPreset(presetName);
            if (preset == null) return;

            // Ziel-Profil finden und anwenden
            var targetProfile = _manager.Profiles.FirstOrDefault(p => p.Name == preset.TargetProfileName);
            if (targetProfile != null)
            {
                _profileService.ApplyProfile(targetProfile);
                _presetManager.MarkUsed(presetName);
                RefreshPresetList(); // Refresh um UseCount zu aktualisieren
                ShowToast($"Preset angewendet: {preset.PresetName} → {preset.TargetProfileName}");
            }
            else
            {
                ShowToast($"Profil '{preset.TargetProfileName}' nicht gefunden", isError: true);
            }
        }

        private void BtnSavePreset_Click(object sender, RoutedEventArgs e)
        {
            var active = _manager.ActiveProfile;
            if (active == null)
            {
                ShowToast("Kein aktives Profil zum Speichern", isError: true);
                return;
            }

            var preset = new ProfilePreset
            {
                PresetName = active.Name,
                TargetProfileName = active.Name,
                Description = $"Saved from {active.Class}"
            };

            _presetManager.SavePreset(preset);
            RefreshPresetList();
            ShowToast($"Preset gespeichert: {preset.PresetName}");
        }

        // ── TASK-1612: Charakter-Klasse Auswahl-Dropdown ──
        private void RefreshClassList()
        {
            try
            {
                _presetComboUpdating = true;
                ClassCharacterCombo.Items.Clear();

                // Collect all distinct classes from available profiles
                var classes = _manager.Profiles
                    .Select(p => p.Class)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                foreach (var className in classes)
                {
                    var classProfileCount = _manager.Profiles.Count(p => p.Class == className);
                    var countSuffix = classProfileCount > 1 ? "e" : "";
                    ClassCharacterCombo.Items.Add(new ClassCharacterItem
                    {
                        Class = className,
                        DisplayName = $"{className} ({classProfileCount} Profil{countSuffix})"
                    });
                }

                // Select class of current active profile
                var activeClass = _manager.ActiveProfile?.Class;
                if (!string.IsNullOrEmpty(activeClass))
                {
                    for (int i = 0; i < ClassCharacterCombo.Items.Count; i++)
                    {
                        if (ClassCharacterCombo.Items[i] is ClassCharacterItem item && item.Class == activeClass)
                        {
                            ClassCharacterCombo.SelectedIndex = i;
                            break;
                        }
                    }
                }

                if (ClassCharacterCombo.Items.Count == 0)
                {
                    ClassCharacterCombo.Items.Add(new ClassCharacterItem { Class = "Unknown", DisplayName = "Unbekannt" });
                    ClassCharacterCombo.SelectedIndex = 0;
                    ClassCharacterCombo.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] RefreshClassList failed: {ex.Message}");
            }
            finally
            {
                _presetComboUpdating = false;
            }
        }

        private void ClassCharacterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_presetComboUpdating || ClassCharacterCombo.SelectedItem is not ClassCharacterItem item)
                return;

            // Finde das erste Profil mit dieser Klasse
            var targetProfile = _manager.Profiles.FirstOrDefault(p => p.Class == item.Class);
            if (targetProfile != null)
            {
                _profileService.ApplyProfile(targetProfile);
                ShowToast($"Klasse gewechselt: {item.Class} → Profil '{targetProfile.Name}'");
            }
        }

        private void BtnRefreshClasses_Click(object sender, RoutedEventArgs e)
        {
            RefreshClassList();
            ShowToast("Klassenliste aktualisiert");
        }

        private void BtnProfileValidation_Click(object sender, RoutedEventArgs e)
        {
            var win = new ProfileValidationWindow();
            win.ShowDialog();
        }

        private void BtnMonsterImport_Click(object sender, RoutedEventArgs e)
        {
            var win = new MonsterImportWindow();
            win.ShowDialog();
        }

        private sealed class ClassCharacterItem
        {
            public string Class { get; set; } = "";
            public string DisplayName { get; set; } = "";

            public override string ToString() => DisplayName;
        }

        // ── Renewal / Pre-Renewal Toggle ────────────────────────────────
        private void ChkRenewalMode_Checked(object sender, RoutedEventArgs e)
        {
            RenewalModeLabel.Text = "Renewal";
            RenewalModeLabel.Foreground = (Brush)FindResource("AccentBlue");
            ApplyRenewalMode(true);
        }

        private void ChkRenewalMode_Unchecked(object sender, RoutedEventArgs e)
        {
            RenewalModeLabel.Text = "Pre-Renewal";
            RenewalModeLabel.Foreground = (Brush)FindResource("AccentGold");
            ApplyRenewalMode(false);
        }

        private void ApplyRenewalMode(bool isRenewal)
        {
            try
            {
                var activeInstance = _multiEngine?.GetActiveInstance();
                activeInstance?.Orchestrator.ApplyGameMode(isRenewal);
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Renewal mode switched to: {(isRenewal ? "Renewal" : "Pre-Renewal")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] ApplyRenewalMode failed: {ex.Message}");
            }
        }
        private void BtnDeveloperDashboard_Click(object sender, RoutedEventArgs e)
        {
            var dashboard = new DeveloperDashboardWindow();
            dashboard.ShowDialog();
        }
    }
    }


