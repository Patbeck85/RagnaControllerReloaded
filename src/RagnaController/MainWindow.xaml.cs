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

namespace RagnaController
{
    public partial class MainWindow : Window
    {
        private readonly HybridEngine   _engine;
        private readonly ProfileManager _manager;
        private readonly Settings       _settings = Settings.Load();
        private readonly System.Collections.Generic.List<IDisposable> _subs = new();
        private bool _isMiniMode = false, _actionRpgOn = true;
        private readonly List<string> _logBuffer = new();
        private MiniModeWindow? _miniWindow;
        private bool _wasFocusLocked;

        private MainViewModel? _vm;
        private DiscordRpcService? _discordRpc;
        private TtsAnnouncerService? _ttsService;
        private InGameOverlayWindow? _gameOverlay;

#pragma warning disable CS0649 // WPF: Fields initialized in XAML
        private Button? BtnToggleMiniMode;
        private Button? BtnToggleGameOverlay;
        private Ellipse? DeadzoneRing;
        private Button? BtnScanController_ClickHandler;
        private Button? BtnSettings_ClickHandler;
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
                if (StatusTextDisplay != null)
                {
                    StatusTextDisplay.Text       = s == EngineStatus.Running ? "RUNNING"
                                             : s == EngineStatus.NoController ? "NO CONTROLLER"
                                             : "PAUSED";
                    StatusTextDisplay.Foreground = s == EngineStatus.Running ? Brushes.Lime : Brushes.OrangeRed;
                }
                _vm?.ApplyEngineStatus(s, _engine.ControllerName);
            });

            _subs.Add(_engine.Messenger.Subscribe<SnapshotReadyMessage>(msg =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    _vm?.ApplySnapshot(msg.Snapshot);
                    if (AutoStatusText != null) AutoStatusText.Text = _vm?.FocusLockHint?.Length > 0
                        ? _vm?.FocusLockHint
                        : _engine.ControllerName + " — active";

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
                            string battery = _engine.ControllerSvc?.GetBatteryLevel() ?? "-";
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
            });

            _engine.VoiceStatusChanged += msg => Dispatcher.BeginInvoke(() =>
            {
                if (VoiceStatusText1 != null)
                {
                    VoiceStatusText1.Text       = msg;
                    VoiceStatusText1.Foreground = msg.StartsWith("🎤")
                        ? Brushes.Lime : new SolidColorBrush(Color.FromRgb(85, 94, 106));
                }
            });

            _engine.BatteryChanged += level => Dispatcher.BeginInvoke(() =>
            {
                if (BatteryFill == null || BatteryLevelText == null) return;
                var (fillWidth, fillColor, label) = level switch
                {
                    "Full"  => (22.0, Color.FromRgb( 57, 255,  20), "Full"),
                    "High"  => (18.0, Color.FromRgb( 57, 255,  20), "High"),
                    "Mid"   => (12.0, Color.FromRgb(255, 184,   0), "Mid"),
                    "Low"   => ( 5.0, Color.FromRgb(255,  58,  82), "Low!"),
                    "Empty" => ( 2.0, Color.FromRgb(255,  58,  82), "Empty"),
                    _       => ( 0.0, Color.FromRgb( 85,  94,106), "–")
                };
                BatteryFill.Width      = fillWidth;
                BatteryFill.Background = new SolidColorBrush(fillColor);
                BatteryLevelText.Text       = label;
                // Sync secondary battery display (left panel)
                if (BatteryFill2 != null)     { BatteryFill2.Width = fillWidth; BatteryFill2.Background = new SolidColorBrush(fillColor); }
                if (BatteryLevelText2 != null)  BatteryLevelText2.Text = label;
                BatteryLevelText.Foreground = new SolidColorBrush(fillColor);
            });

            _engine.ControllerConnected += name => Dispatcher.BeginInvoke(() =>
            {
                if (ControllerNameText != null)
                {
                    ControllerNameText.Text       = name;
                    ControllerNameText.Foreground = Brushes.Lime;
                    ControllerNameText.Tag        = true;
                }
                if (ControllerNameText2 != null)
                {
                    ControllerNameText2.Text       = name;
                    ControllerNameText2.Foreground = Brushes.Lime;
                }
                _ttsService?.Speak("Controller connected");
                SetFooterButtonsEnabled(true);
            });

            _engine.ControllerDisconnected += () => Dispatcher.BeginInvoke(() =>
            {
                if (ControllerNameText != null)
                {
                    ControllerNameText.Text       = GetLocalizedString("ControllerName_NoController");
                    ControllerNameText.Foreground = new SolidColorBrush(Color.FromRgb(85, 94, 106));
                    ControllerNameText.Tag        = false;
                }
                if (ControllerNameText2 != null)
                {
                    ControllerNameText2.Text       = GetLocalizedString("ControllerName_NoController");
                    ControllerNameText2.Foreground = new SolidColorBrush(Color.FromRgb(85, 94, 106));
                }
                if (BatteryFill     != null) BatteryFill.Width = 0;
                if (BatteryLevelText != null) BatteryLevelText.Text = GetLocalizedString("BatteryLevel_Empty");
                _ttsService?.Speak("Controller disconnected");
                SetFooterButtonsEnabled(false);
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

            // Restore last game mode
            if (GameModeCombo != null)
            {
                foreach (ComboBoxItem modeItem in GameModeCombo.Items)
                {
                    if (modeItem.Tag?.ToString() == _settings.LastGameMode)
                    {
                        GameModeCombo.SelectedItem = modeItem;
                        break;
                    }
                }
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

        private void DeadzoneSlider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DeadzoneValue != null) DeadzoneValue.Text = e.NewValue.ToString("F2");
            UpdateDeadzoneRing((float)e.NewValue);
            if (!_suppressSliderEvents) _engine.LiveUpdateDeadzone((float)e.NewValue);
        }

        private void DeadzoneReset_Click(object s, RoutedEventArgs e)
        {
            if (ProfileCombo?.SelectedItem is Profile p && DeadzoneSlider != null)
            {
                DeadzoneSlider.Value = p.Deadzone;
                DeadzoneValue.Text = DeadzoneSlider.Value.ToString("F2");
            }
        }

        private void CurveSlider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CurveValue != null) CurveValue.Text = e.NewValue.ToString("F2");
            if (!_suppressSliderEvents) _engine.LiveUpdateCurve((float)e.NewValue);
        }

        private void CurveReset_Click(object s, RoutedEventArgs e)
        {
            if (ProfileCombo?.SelectedItem is Profile p && CurveSlider != null)
            {
                CurveSlider.Value = p.MovementCurve;
                CurveValue.Text = CurveSlider.Value.ToString("F2");
            }
        }

        private void ActionSpeedSlider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ActionSpeedValue != null) ActionSpeedValue.Text = e.NewValue.ToString("F2");
            if (!_suppressSliderEvents) _engine.LiveUpdateActionSpeed((float)e.NewValue);
        }

        private void ActionSpeedReset_Click(object s, RoutedEventArgs e)
        {
            if (ProfileCombo?.SelectedItem is Profile p && ActionSpeedSlider != null)
            {
                ActionSpeedSlider.Value = p.ActionSpeed;
                ActionSpeedValue.Text = ActionSpeedSlider.Value.ToString("F2");
            }
        }

        private void SensitivitySlider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SensitivityValue != null) SensitivityValue.Text = e.NewValue.ToString();
            if (!_suppressSliderEvents) _engine.LiveUpdateCursorSpeed((float)e.NewValue);
        }

        private void SensitivityReset_Click(object s, RoutedEventArgs e)
        {
            if (ProfileCombo?.SelectedItem is Profile p && SensitivitySlider != null)
            {
                SensitivitySlider.Value = p.CursorMaxSpeed;
                SensitivityValue.Text = SensitivitySlider.Value.ToString();
            }
        }

        private void MoveModeToggle_Click(object s, RoutedEventArgs e)
        {
            _actionRpgOn = !_actionRpgOn;
            ToggleThumb.HorizontalAlignment = _actionRpgOn ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            _engine.LiveUpdateActionRpg(_actionRpgOn);
        }
        
        private void ProfileCombo_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            if (ProfileCombo?.SelectedItem is not Profile p) return;
            ApplyProfile(p);
        }

        private bool _suppressSliderEvents = false;

        private void ApplyProfile(Profile p)
        {
            _manager.SetActive(p.Name);
            _engine.LoadProfile(p);
            _vm?.UpdateClassImage(p.Class);

            // Class Badge aktualisieren
            if (ClassBadgeText != null)
            {
                ClassBadgeText.Text = p.Class ?? p.Name;
                ClassBadgeText.Foreground = new SolidColorBrush(Color.FromRgb(229, 184, 66));
            }
            if (ClassBadge != null)
                ClassBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(229, 184, 66));

            // Sync sliders to new profile values (suppress ValueChanged → engine feedback loop)
            _suppressSliderEvents = true;
            try
            {
                if (DeadzoneSlider   != null) DeadzoneSlider.Value   = p.Deadzone;
                if (SensitivitySlider!= null) SensitivitySlider.Value = p.CursorMaxSpeed;
                if (CurveSlider      != null) CurveSlider.Value      = p.MovementCurve;
                if (ActionSpeedSlider!= null) ActionSpeedSlider.Value = p.ActionSpeed;
                if (DeadzoneValue    != null) DeadzoneValue.Text      = p.Deadzone.ToString("F2");
                if (SensitivityValue != null) SensitivityValue.Text   = p.CursorMaxSpeed.ToString();
                if (CurveValue       != null) CurveValue.Text         = p.MovementCurve.ToString("F2");
                if (ActionSpeedValue != null) ActionSpeedValue.Text   = p.ActionSpeed.ToString("F2");
                UpdateDeadzoneRing((float)p.Deadzone);
            }
            finally { _suppressSliderEvents = false; }

            // Refresh active tab mapping display
            if (_activeTabPanel != null) PopulateTabPanel(_activeTabPanel);

            ShowToast($"Profil geladen: {p.Name} ({p.Class})");
        }
        
        private void GameModeCombo_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            if (GameModeCombo?.SelectedItem is not ComboBoxItem item) return;
            string mode = item.Tag?.ToString() ?? "Ren";
            bool isRenewal = mode != "Pre";
            _engine.ApplyGameMode(isRenewal);
            _settings.LastGameMode = mode;
            _settings.Save();
            ShowToast($"Game Mode: {item.Content}");
        }

        private void TabBase_Click(object s, RoutedEventArgs e) => SelectTab(PanelBase, null);
        private void TabL1_Click(object s, RoutedEventArgs e) => SelectTab(PanelL1, null);
        private void TabR1_Click(object s, RoutedEventArgs e) => SelectTab(PanelR1, null);
        private void TabL2_Click(object s, RoutedEventArgs e) => SelectTab(PanelL2, null);
        private void TabR2_Click(object s, RoutedEventArgs e) => SelectTab(PanelR2, null);
        private void TabInfo_Click(object s, RoutedEventArgs e)
        {
            // Reset tab button styles
            var allTabBtns = new[] { TabBtnBase, TabBtnL1, TabBtnR1, TabBtnL2, TabBtnR2, TabBtnInfo, TabBtnLog };
            foreach (var btn in allTabBtns)
                if (btn != null) btn.Style = (Style)FindResource("TabButton");
            if (TabBtnInfo != null) TabBtnInfo.Style = (Style)FindResource("TabButtonActive");
            // Show engine + controller info in a toast-style summary
            var info = $"Engine: {(_engine.IsRunning ? "Running" : "Stopped")} | Controller: {_engine.ControllerName} | Profile: {(_manager.ActiveProfile?.Name ?? "None")}";
            ShowToast(info);
        }
        private void TabLog_Click(object s, RoutedEventArgs e)
        {
            // Reset tab button styles
            var allTabBtns = new[] { TabBtnBase, TabBtnL1, TabBtnR1, TabBtnL2, TabBtnR2, TabBtnInfo, TabBtnLog };
            foreach (var btn in allTabBtns)
                if (btn != null) btn.Style = (Style)FindResource("TabButton");
            if (TabBtnLog != null) TabBtnLog.Style = (Style)FindResource("TabButtonActive");
            // Refresh log display and scroll to bottom
            ApplyLogFilter();
        }
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
            var allTabBtns = new[] { TabBtnBase, TabBtnL1, TabBtnR1, TabBtnL2, TabBtnR2, TabBtnInfo, TabBtnLog, TabBtnHealth };
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
                _ => showInfo ? TabBtnInfo : TabBtnLog
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
                    Fill = new SolidColorBrush(_engine.ControllerSvc?.GetBatteryLevel() switch {
                        "Full" or "High" => Color.FromRgb(57, 255, 20),
                        "Mid" => Color.FromRgb(255, 184, 0),
                        "Low" or "Empty" => Color.FromRgb(255, 58, 82),
                        _ => Color.FromRgb(85, 94, 106)
                    })
                });
                batteryStack.Children.Add(new TextBlock { 
                    Text = _engine.ControllerSvc?.GetBatteryLevel() ?? "-", 
                    Foreground = new SolidColorBrush(_engine.ControllerSvc?.GetBatteryLevel() switch {
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
                _gameOverlay = new InGameOverlayWindow(_engine.Messenger, _engine.WindowTracker);
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
            if (BtnToggleMiniMode != null) BtnToggleMiniMode.IsEnabled = enabled;
            if (BtnToggleGameOverlay != null) BtnToggleGameOverlay.IsEnabled = enabled;
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
                var toast = new Border
                {
                    Background = isError ? Brushes.OrangeRed : Brushes.Blue,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(16, 16, 16, 16),
                    Child = new TextBlock
                    {
                        Text = message,
                        Foreground = Brushes.White,
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold
                    }
                };

                // Add toast to window
                if (this.Content is Grid grid)
                {
                    grid.Children.Add(toast);
                    toast.Loaded += (s2, e2) =>
                    {
                        this.Dispatcher.BeginInvoke(
                            new Action(() =>
                            {
                                this.Dispatcher.BeginInvoke(
                                    () => grid.Children.Remove(toast));
                            }));
                    };
                }
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
            if (BtnToggleMiniMode != null) BtnToggleMiniMode.Click += BtnToggleMiniMode_Click;
            if (BtnToggleGameOverlay != null) BtnToggleGameOverlay.Click += BtnToggleGameOverlay_Click;
            if (BtnScanController_ClickHandler != null) BtnScanController_ClickHandler.Click += BtnScanController_Click;
            if (BtnSettings_ClickHandler != null) BtnSettings_ClickHandler.Click += BtnSettings_Click;
        }

        private void BtnScanController_Click(object s, RoutedEventArgs e)
        {
            _engine?.Start();
        }

        private void BtnSettings_Click(object s, RoutedEventArgs e)
        {
            var settings = new SettingsWindow(_engine, _manager, _settings);
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

        private void ChkTurboTest_Click(object s, RoutedEventArgs e)
        {
            // Turbo test checkbox click handler
        }

        private void BtnTurboTest_Click(object s, RoutedEventArgs e)
        {
            // Turbo test button click handler
        }

        private void BtnRemap_Click(object s, RoutedEventArgs e)
        {
            // Remap button click handler
        }

        private void BtnMacro_Click(object s, RoutedEventArgs e)
        {
            // Macro button click handler
        }

        private void BtnRadial_Click(object s, RoutedEventArgs e)
        {
            // Radial menu button click handler
        }

        private void BtnCombo_Click(object s, RoutedEventArgs e)
        {
            // Combo button click handler
        }

        private void UpdateDeadzoneRing(float deadzone)
        {
            try
            {
                if (DeadzoneRing != null)
                {
                    double radius = Math.Min(DeadzoneRing.ActualWidth, DeadzoneRing.ActualHeight) / 2;
                    double innerRadius = deadzone * radius;
                    
                    DeadzoneRing.Opacity = 1.0 - (deadzone / 100.0);
                }
            }
            catch { }
        }

    }
}