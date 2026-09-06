using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace RagnaController
{
    public partial class MainWindow : Window
    {
        private readonly HybridEngine   _engine;
        private readonly ProfileManager _manager;
        private Settings       _settings = Settings.Load();
        private readonly System.Collections.Generic.List<IDisposable> _subs = new();
        private bool _isMiniMode = false, _actionRpgOn = true;
        private readonly List<string> _logBuffer = new();
        private MiniModeWindow? _miniWindow;
        private bool _wasFocusLocked;

        private MainViewModel? _vm;
        private DiscordRpcService? _discordRpc;
        private TtsAnnouncerService? _ttsService;
        private InGameOverlayWindow? _gameOverlay;
        private ControllerTestWindow? _controllerTestWindow;

#pragma warning disable CS0649 // WPF: Fields initialized in XAML
        // Toast/Notifications (NOT in XAML - created dynamically)
        private Border? ToastBorder;
        private TextBlock? ToastText;
        private SolidColorBrush? ToastBg;

        // Controller canvas elements - NOT in XAML with x:Name (they're in a Canvas without names)
        private Ellipse? DeadzoneRing;
#pragma warning restore CS0649

        public MainWindow(HybridEngine engine, ProfileManager manager, MainViewModel vm)
        {
            _engine  = engine;
            _manager = manager;
            _vm     = vm; // Assign the passed ViewModel
            InitializeComponent();
            DataContext = vm;

            // Initialize ViewModel first before setting up services
            if (vm == null)
            {
                ShowToast("ViewModel not initialized", isError: true);
                return;
            }

            _discordRpc = new DiscordRpcService(_settings);
            _ttsService = new TtsAnnouncerService(_settings);

            _engine.StatusChanged += (s) => Dispatcher.BeginInvoke(() =>
            {
                // Status displayed in Health tab panel
                _vm?.ApplyEngineStatus(s, _engine.ControllerName);
            });

            _subs.Add(_engine.Messenger.Subscribe<SnapshotReadyMessage>(msg =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    _vm?.ApplySnapshot(msg.Snapshot);

                    if (msg.Snapshot.FocusLocked && !_wasFocusLocked)
                    {
                        _ttsService?.Speak("Game Paused");
                        _wasFocusLocked = true;
                    }
                    else if (!msg.Snapshot.FocusLocked && _wasFocusLocked)
                    {
                        _ttsService?.Speak("Game Resumed");
                        _wasFocusLocked = false;
                    }
                    if (_isMiniMode && _miniWindow != null)
                    {
                        // Battery read must happen on UI thread — SDL calls aren't thread-safe
                        // from the engine tick context. Cache via Dispatcher.
                        Dispatcher.BeginInvoke(() =>
                        {
                            string pName = (ProfileCombo?.SelectedItem is Profiles.Profile pf2) ? pf2.Name : "—";
                            string battery = _engine.ControllerSvc?.BatteryLevel ?? "-";
                            _miniWindow?.UpdateSnapshot(msg.Snapshot, pName, _engine.IsRunning, battery);
                        });
                    }
                });
            }));

            _subs.Add(_engine.Messenger.Subscribe<EngineStatusMessage>(msg =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    _vm?.ApplyEngineStatus(msg.Status, msg.ControllerName);
                });
            }));

            _subs.Add(_engine.Messenger.Subscribe<BatteryChangedMessage>(msg =>
            {
                // Battery changed - handled internally
            }));

            _subs.Add(_engine.Messenger.Subscribe<BuffWarningMessage>(msg => Dispatcher.BeginInvoke(() =>
            {
                _ttsService?.Speak($"Warning: {msg.ActionLabel} expiring.");
                ShowToast($"⏳ Buff expiring soon: {msg.ActionLabel}", isError: true);
            })));

            _engine.ProfileQuickSwitch += delta => Dispatcher.BeginInvoke(() =>
            {
                if (_manager.Profiles.Count == 0) return;
                int idx  = _manager.Profiles.IndexOf(ProfileCombo.SelectedItem as Profile ?? _manager.Profiles[0]);
                int next = (idx + delta + _manager.Profiles.Count) % _manager.Profiles.Count;
                ProfileCombo.SelectedItem = _manager.Profiles[next];
                if (ProfileCombo.SelectedItem is Profile p)
                {
                    _vm?.UpdateClassImage(p.Class);
                }
            });

            _engine.LogMessage += msg => Dispatcher.BeginInvoke(() =>
            {
                if (LogTextBlock != null)
                {
                    _logBuffer.Add(msg);
                    if (_logBuffer.Count > 500) _logBuffer.RemoveRange(0, 100);
                    ApplyLogFilter();
                    LogScrollViewer?.ScrollToEnd();
                }
                if (LogTextBlock2 != null)
                {
                    LogScrollViewer2?.ScrollToEnd();
                }
            });

            _engine.BatteryChanged += level => Dispatcher.BeginInvoke(() =>
            {
                // Battery level updated - could show in status or toast
            });

            _engine.ControllerConnected += name => Dispatcher.BeginInvoke(() =>
            {
                _ttsService?.Speak("Controller connected");
                SetFooterButtonsEnabled(true);
                
                // Show controller test window when controller connects
                ShowControllerTestWindow();
                
                // Refresh health tab if visible
                if (_activeTabPanel == PanelHealth)
                    PopulateTabPanel(PanelHealth);
            });

            _engine.ControllerDisconnected += () => Dispatcher.BeginInvoke(() =>
            {
                _ttsService?.Speak("Controller disconnected");
                SetFooterButtonsEnabled(false);
                
                // Close controller test window when controller disconnects
                _controllerTestWindow?.Close();
                _controllerTestWindow = null;
                
                // Refresh health tab if visible
                if (_activeTabPanel == PanelHealth)
                    PopulateTabPanel(PanelHealth);
            });

            _engine.RestoreMainWindowRequested += () => Dispatcher.BeginInvoke(() =>
            {
                if (_isMiniMode) SwitchFromMiniMode();
            });

            // ── Profile Dropdown initialisieren ─────────────────────────────
            ProfileCombo.ItemsSource = _manager.Profiles;

            // Letztes Profil wiederherstellen oder erstes nehmen
            var lastProfile = _manager.Profiles.FirstOrDefault(p => p.Name == _settings.LastProfileName)
                           ?? _manager.Profiles.FirstOrDefault();
            if (lastProfile != null)
            {
                ProfileCombo.SelectedItem = lastProfile;
                ApplyProfile(lastProfile);
            }

            // Initialize tab button mapping
            InitTabBtnMap();
            
            // Select default tab (Base)
            SelectTab(PanelBase, null);
        }

        public void SwitchFromMiniMode()
        {
            _isMiniMode = false;
            Show(); WindowState = WindowState.Normal; Activate();
            if (_miniWindow != null)
            {
                var w = _miniWindow;
                _miniWindow = null;
                try { w.Close(); } catch { }
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

        private void TabBase_Click(object s, RoutedEventArgs e) => SelectTab(PanelBase, null);
        private void TabL1_Click(object s, RoutedEventArgs e) => SelectTab(PanelL1, null);
        private void TabR1_Click(object s, RoutedEventArgs e) => SelectTab(PanelR1, null);
        private void TabL2_Click(object s, RoutedEventArgs e) => SelectTab(PanelL2, null);
        private void TabR2_Click(object s, RoutedEventArgs e) => SelectTab(PanelR2, null);
        private void TabHealth_Click(object s, RoutedEventArgs e) => SelectTab(PanelHealth, null);

        private Border? _activeTabPanel;

        private readonly Dictionary<object, Button?> _panelToTabBtn = new();
        private void InitTabBtnMap()
        {
            _panelToTabBtn[PanelBase] = TabBtnBase;
            _panelToTabBtn[PanelL1]   = TabBtnL1;
            _panelToTabBtn[PanelR1]   = TabBtnR1;
            _panelToTabBtn[PanelL2]   = TabBtnL2;
            _panelToTabBtn[PanelR2]   = TabBtnR2;
            _panelToTabBtn[PanelHealth] = TabBtnHealth;
        }

        private void SelectTab(Border? panel, Border? mappings, bool showInfo = false)
        {
            // Hide all mapping panels including Health
            var allPanels = new[] { PanelBase, PanelL1, PanelR1, PanelL2, PanelR2, PanelHealth };
            foreach (var p2 in allPanels)
                if (p2 != null) p2.Visibility = Visibility.Collapsed;

            // Reset all tab button styles
            var allTabBtns = new[] { TabBtnBase, TabBtnL1, TabBtnR1, TabBtnL2, TabBtnR2, TabBtnHealth };
            foreach (var btn in allTabBtns)
                if (btn != null) btn.Style = (Style)FindResource("TabButton");

            // Activate the correct tab button
            Button? activeBtn = panel switch
            {
                _ when panel == PanelBase => TabBtnBase,
                _ when panel == PanelL1   => TabBtnL1,
                _ when panel == PanelR1   => TabBtnR1,
                _ when panel == PanelL2   => TabBtnL2,
                _ when panel == PanelR2   => TabBtnR2,
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
                _activeTabPanel  = panel;
                PopulateTabPanel(panel);
            }
        }

        private void PopulateTabPanel(Border panel)
        {
            if (ProfileCombo?.SelectedItem is not Profiles.Profile p) return;

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
                engineStack.Children.Add(new TextBlock { 
                    Text = "Engine:", 
                    Foreground = new SolidColorBrush(Color.FromRgb(161, 176, 197)), 
                    FontSize = 10.5, FontWeight = System.Windows.FontWeights.SemiBold, Margin = new Thickness(0, 0, 8, 0)
                });
                engineStack.Children.Add(new TextBlock { 
                    Text = _engine.IsRunning ? "RUNNING" : "STOPPED", 
                    Foreground = Brushes.Lime, 
                    FontSize = 13, FontWeight = System.Windows.FontWeights.Bold 
                });
                var engineBorder = new Border { 
                    Background = new SolidColorBrush(Color.FromRgb(10, 12, 20)), 
                    CornerRadius = new CornerRadius(4), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 8),
                    Child = engineStack
                };
                healthStack.Children.Add(engineBorder);

                // Profile Info
                var profileStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                profileStack.Children.Add(new TextBlock { 
                    Text = "Profile:", 
                    Foreground = new SolidColorBrush(Color.FromRgb(161, 176, 197)), 
                    FontSize = 10.5, FontWeight = System.Windows.FontWeights.SemiBold, Margin = new Thickness(0, 0, 8, 0)
                });
                profileStack.Children.Add(new TextBlock { 
                    Text = p.Name ?? "None", 
                    Foreground = Brushes.White, 
                    FontSize = 11, FontWeight = System.Windows.FontWeights.SemiBold 
                });
                var profileBorder = new Border { 
                    Background = new SolidColorBrush(Color.FromRgb(10, 12, 20)), 
                    CornerRadius = new CornerRadius(4), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 8),
                    Child = profileStack
                };
                healthStack.Children.Add(profileBorder);

                var classStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                classStack.Children.Add(new TextBlock { 
                    Text = "Class:", 
                    Foreground = new SolidColorBrush(Color.FromRgb(161, 176, 197)), 
                    FontSize = 10.5, FontWeight = System.Windows.FontWeights.SemiBold, Margin = new Thickness(0, 0, 8, 0)
                });
                classStack.Children.Add(new TextBlock { 
                    Text = p.Class ?? "N/A", 
                    Foreground = new SolidColorBrush(Color.FromRgb(229, 184, 66)), 
                    FontSize = 11, FontWeight = System.Windows.FontWeights.SemiBold 
                });
                var classBorder = new Border { 
                    Background = new SolidColorBrush(Color.FromRgb(10, 12, 20)), 
                    CornerRadius = new CornerRadius(4), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 8),
                    Child = classStack
                };
                healthStack.Children.Add(classBorder);

                // Battery Status
                var batteryStack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                batteryStack.Children.Add(new TextBlock { 
                    Text = "Battery:", 
                    Foreground = new SolidColorBrush(Color.FromRgb(161, 176, 197)), 
                    FontSize = 10.5, FontWeight = System.Windows.FontWeights.SemiBold, Margin = new Thickness(0, 0, 8, 0)
                });
                batteryStack.Children.Add(new Ellipse { Width = 7, Height = 7, VerticalAlignment = System.Windows.VerticalAlignment.Center, 
                    Fill = new SolidColorBrush(_engine.ControllerSvc?.BatteryLevel switch {
                        "Full" or "High" => Color.FromRgb(57, 255, 20),
                        "Mid" => Color.FromRgb(255, 184, 0),
                        "Low" or "Empty" => Color.FromRgb(255, 58, 82),
                        _ => Color.FromRgb(85, 94, 106)
                    })
                });
                batteryStack.Children.Add(new TextBlock { 
                    Text = _engine.ControllerSvc?.BatteryLevel ?? "-", 
                    Foreground = new SolidColorBrush(_engine.ControllerSvc?.BatteryLevel switch {
                        "Full" or "High" => Color.FromRgb(57, 255, 20),
                        "Mid" => Color.FromRgb(255, 184, 0),
                        "Low" or "Empty" => Color.FromRgb(255, 58, 82),
                        _ => Color.FromRgb(85, 94, 106)
                    }), 
                    FontSize = 11, VerticalAlignment = System.Windows.VerticalAlignment.Center 
                });
                var batteryBorder = new Border { 
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

            bool showEngine  = LogFilterEngine?.IsChecked  ?? true;
            bool showInput   = LogFilterInput?.IsChecked   ?? true;
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
                _gameOverlay = new InGameOverlayWindow(_engine.Messenger, _engine.WindowTracker, _engine.ControllerManager, _settings);
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
            if (_controllerTestWindow == null && _engine?.ControllerSvc != null)
            {
                // Use the public ControllerSvc property instead of reflection
                var sdlProvider = _engine.ControllerSvc;
                
                if (sdlProvider != null)
                {
                    _controllerTestWindow = new ControllerTestWindow(sdlProvider);
                    _controllerTestWindow.Closed += (s, e) => _controllerTestWindow = null;
                    _controllerTestWindow.Show();
                }
            }
        }

        private void Window_Closing(object s, EventArgs e)
        {
            // Cleanup subscriptions
            foreach (var sub in _subs)
                try { sub.Dispose(); } catch { }
            _subs.Clear();

            // Save settings
            if (_manager.ActiveProfile != null)
                _settings.LastProfileName = _manager.ActiveProfile.Name;
            _settings.Save();

            // Close game overlay if open
            _gameOverlay?.Close();
        }

        // Helper methods to fix missing methods
        private void ShowToast(string message, bool isError = false)
        {
            try
            {
                if (ToastBorder == null || ToastText == null || ToastBg == null) return;

                ToastText.Text = message;
                ToastBg.Color = isError ? Color.FromRgb(255, 58, 82) : Color.FromRgb(57, 255, 20);
                ToastBorder.Visibility = Visibility.Visible;

                // Auto-hide after 3 seconds
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    ToastBorder.Visibility = Visibility.Collapsed;
                };
                timer.Start();
            }
            catch { }
        }

        private string GetLocalizedString(string key)
        {
            // Simple fallback - in production this would use a localization system
            return key.Replace("_", " ");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize event handlers after component loading
        }

        private void BtnScanController_Click(object s, RoutedEventArgs e)
        {
            _engine?.Start();
            ShowToast("Controller scan initiated");
        }

        private void BtnSettings_Click(object s, RoutedEventArgs e)
        {
            var settings = new SettingsWindow(_engine, _manager, _settings, s =>
            {
                _engine.FocusLockEnabled = s.FocusLockEnabled;
                _engine.FocusLockProcess = s.FocusLockProcess;
                _settings = s; // Update local reference
            });
            settings.Show();
        }

        private void ResetBtn_MouseEnter(object s, RoutedEventArgs e)
        {
            // Mouse enter handler for reset button
        }

        private void ResetBtn_MouseLeave(object s, RoutedEventArgs e)
        {
            // Mouse leave handler for reset button
        }



        

        // Missing event handlers for XAML
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

        private void BtnCalibrateQuick_Click(object sender, RoutedEventArgs e)
        {
            _engine?.StartCalibration();
            ShowToast("Calibration started");
        }

        private void BtnTestInput_Click(object sender, RoutedEventArgs e)
        {
            if (_engine?.ControllerSvc != null)
            {
                var testWindow = new ControllerTestWindow(_engine.ControllerSvc);
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
            var w = new ButtonRemappingWindow(_engine, _manager);
            w.Owner = this;
            w.Show();
        }

        // Missing event handlers for XAML
        private void ProfileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileCombo.SelectedItem is Profile profile)
            {
                ApplyProfile(profile);
            }
        }

        private void BtnRadial_Click(object sender, RoutedEventArgs e)
        {
            var queue = _engine?.CommandQueue ?? new InputCommandQueue();
            var w = new RadialMenuWindow(queue);
            w.Owner = this;
            w.Show();
        }

        private void ApplyProfile(Profile profile)
        {
            if (profile == null) return;
            
            _engine.LoadProfile(profile);
            _settings.LastProfileName = profile.Name;
            _settings.Save();
            
            // Update CurrentProfileText if it exists
            if (ProfileCombo != null)
            {
                ProfileCombo.SelectedItem = profile;
            }
            
            ShowToast($"Profile loaded: {profile.Name}");
        }

        // Reset click handlers for double-click on labels
        private void DeadzoneReset_Click(object sender, RoutedEventArgs e)
        {
            if (_engine?.CurrentProfile != null)
            {
                _engine.CurrentProfile.Deadzone = 0.15f;
                _engine.LiveUpdateDeadzone(0.15f);
                var pm = new ProfileManager();
                pm.SaveProfile(_engine.CurrentProfile);
                ShowToast("Deadzone reset to default (0.15)");
            }
        }

        private void CurveReset_Click(object sender, RoutedEventArgs e)
        {
            if (_engine?.CurrentProfile != null)
            {
                _engine.CurrentProfile.MovementCurve = 1.0f;
                _engine.LiveUpdateCurve(1.0f);
                var pm = new ProfileManager();
                pm.SaveProfile(_engine.CurrentProfile);
                ShowToast("Curve reset to default (1.0)");
            }
        }

        private void ActionSpeedReset_Click(object sender, RoutedEventArgs e)
        {
            if (_engine?.CurrentProfile != null)
            {
                _engine.LiveUpdateActionSpeed(5.0f); // Default mid-point
                var pm = new ProfileManager();
                pm.SaveProfile(_engine.CurrentProfile);
                ShowToast("Action Speed reset to default (5)");
            }
        }

        private void SensitivityReset_Click(object sender, RoutedEventArgs e)
        {
            if (_engine?.CurrentProfile != null)
            {
                _engine.LiveUpdateCursorSpeed(1.0f);
                var pm = new ProfileManager();
                pm.SaveProfile(_engine.CurrentProfile);
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
    }
}