using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using RagnaController.Models;
using RagnaController.Core;
using RagnaController.Profiles;
using RagnaController.Controller;

namespace RagnaController
{
    public partial class SettingsWindow : Window
    {
        private static readonly Lazy<LocalizationManager> _localization = new(() => LocalizationManager.Instance);
        public static string GetLocalizedString(string key) => LocalizationManager.GetLocalizedString(key);

        private readonly HybridEngine _engine;
        private readonly ProfileManager _manager;
        private readonly Settings _s;
#pragma warning disable CS0649 // WPF: Field initialized in XAML
        private readonly Action<Settings>? _onSave;
#pragma warning restore CS0649

        public SettingsWindow(HybridEngine engine, ProfileManager manager, Settings s, Action<Settings>? onSave = null)
                {
                    InitializeComponent();
                    _engine = engine;
                    _manager = manager;
                    _s = s;
                    _onSave = onSave;
      
                    // Initialize all settings from saved values
                    InitializeSettings();
                }

        private void InitializeSettings()
        {
            // Profile Settings - populate with available profiles
            ProfileCombo.ItemsSource = _manager.Profiles;
            var lastProfile = _manager.Profiles.FirstOrDefault(p => p.Name == _s.LastProfileName);
            if (lastProfile != null)
            {
                ProfileCombo.SelectedItem = lastProfile;
            }
            CurrentProfileText.Text = string.IsNullOrEmpty(_s.LastProfileName) 
                ? "No profile loaded" 
                : $"Current: {_s.LastProfileName}";

            // Engine Settings
            ChkStartWithWindows.IsChecked = _s.StartWithWindows;
            ChkMinimizeToTray.IsChecked = _s.MinimizeToTray;

            // Initialize LogLevelCombo
            InitializeLogLevelCombo();

            // Initialize LanguageCombo
            InitializeLanguageCombo();

            // Initialize RoExePathCombo
            InitializeRoExePathCombo();

            // Initialize Overlay Customization Settings
            InitializeOverlayCustomization();
        }

        private void InitializeOverlayCustomization()
        {
            // OverlayThemeCombo
            var themes = new[]
            {
                new ComboBoxItem { Content = "Neon", Tag = Settings.OverlayThemeType.Neon },
                new ComboBoxItem { Content = "Soft", Tag = Settings.OverlayThemeType.Soft },
                new ComboBoxItem { Content = "Dark", Tag = Settings.OverlayThemeType.Dark }
            };
            OverlayThemeCombo.ItemsSource = themes;
            OverlayThemeCombo.SelectedItem = themes.FirstOrDefault(item => (Settings.OverlayThemeType?)item.Tag == _s.OverlayTheme) ?? themes[0];

            // OverlayOpacitySlider
            OverlayOpacitySlider.Value = _s.OverlayOpacity;
            OverlayOpacityValue.Text = $"{_s.OverlayOpacity:P0}";

            // OverlayFontScaleSlider
            OverlayFontScaleSlider.Value = _s.OverlayFontScale;
            OverlayFontScaleValue.Text = $"{_s.OverlayFontScale:P0}";
        }

        private void ChkAutoLoadProfile_Click(object sender, RoutedEventArgs e)
        {
            // Auto-load profile feature removed in v1.7.0
        }

        private void InitializeLogLevelCombo()
        {
            // Log levels matching Settings.LogLevel: 0=Debug, 1=Info, 2=Warning, 3=Error
            var logLevels = new[]
            {
                new ComboBoxItem { Content = "Debug", Tag = 0 },
                new ComboBoxItem { Content = "Info", Tag = 1 },
                new ComboBoxItem { Content = "Warning", Tag = 2 },
                new ComboBoxItem { Content = "Error", Tag = 3 }
            };
            LogLevelCombo.ItemsSource = logLevels;
            LogLevelCombo.SelectedItem = logLevels.FirstOrDefault(item => (int?)item.Tag == _s.LogLevel) ?? logLevels[1];
        }

        private void InitializeLanguageCombo()
        {
            // Language options matching the localization files
            var languages = new[]
            {
                new ComboBoxItem { Content = "English", Tag = "en" },
                new ComboBoxItem { Content = "Deutsch (German)", Tag = "de" },
                new ComboBoxItem { Content = "Español (Spanish)", Tag = "es" },
                new ComboBoxItem { Content = "Français (French)", Tag = "fr" },
                new ComboBoxItem { Content = "Italiano (Italian)", Tag = "it" },
                new ComboBoxItem { Content = "Português (Portuguese)", Tag = "pt" },
                new ComboBoxItem { Content = "Nederlands (Dutch)", Tag = "nl" },
                new ComboBoxItem { Content = "Polski (Polish)", Tag = "pl" },
                new ComboBoxItem { Content = "Русский (Russian)", Tag = "ru" },
                new ComboBoxItem { Content = "中文 (Chinese)", Tag = "zh" },
                new ComboBoxItem { Content = "日本語 (Japanese)", Tag = "ja" },
                new ComboBoxItem { Content = "한국어 (Korean)", Tag = "ko" }
            };
            LanguageCombo.ItemsSource = languages;
            LanguageCombo.SelectedItem = languages.FirstOrDefault(item => item.Tag?.ToString() == _s.AppLanguage) ?? languages[0];
        }

        private void InitializeRoExePathCombo()
        {
            // Get recent RO paths from settings or discover common locations
            var paths = new List<string>();
            
            // Add current setting if present
            if (!string.IsNullOrEmpty(_s.RoExePath))
            {
                paths.Add(_s.RoExePath);
            }
            
            // Add common default locations
            var defaultPaths = new[]
            {
                @"C:\Program Files\Ragnarok Online\ragexe.exe",
                @"C:\Program Files (x86)\Ragnarok Online\ragexe.exe",
                @"C:\Games\Ragnarok Online\ragexe.exe",
                @"D:\Ragnarok Online\ragexe.exe",
                @"D:\Games\Ragnarok Online\ragexe.exe"
            };
            
            foreach (var path in defaultPaths)
            {
                if (File.Exists(path) && !paths.Contains(path))
                    paths.Add(path);
            }
            
            // Add a "Browse..." option at the end
            paths.Add("Browse...");
            
            RoExePathCombo.ItemsSource = paths;
            
            // Select the first valid path or "Browse..."
            if (!string.IsNullOrEmpty(_s.RoExePath) && paths.Contains(_s.RoExePath))
            {
                RoExePathCombo.SelectedItem = _s.RoExePath;
            }
            else if (paths.Count > 0)
            {
                RoExePathCombo.SelectedItem = paths[0];
            }
            
            // Update status label
            UpdateRoExeStatusLabel();
        }

        private void UpdateRoExeStatusLabel()
        {
            if (RoExePathCombo.SelectedItem is string selectedPath)
            {
                if (selectedPath == "Browse...")
                {
                    LblRoExeStatus.Text = "Click to browse for RO executable";
                }
                else if (File.Exists(selectedPath))
                {
                    LblRoExeStatus.Text = $"Found: {selectedPath}";
                }
                else
                {
                    LblRoExeStatus.Text = "Path not found";
                }
            }
            else
            {
                LblRoExeStatus.Text = "No path selected";
            }
        }

        private void ChkStartWithWindows_Click(object sender, RoutedEventArgs e)
        {
            _s.StartWithWindows = ChkStartWithWindows.IsChecked == true;
            Core.AutoStartManager.SetAutoStart(_s.StartWithWindows);
            _s.Save();
        }

        private void ChkMinimizeToTray_Click(object sender, RoutedEventArgs e)
        {
            _s.MinimizeToTray = ChkMinimizeToTray.IsChecked == true;
            _s.Save();
        }

        private void ChkShowLatency_Click(object sender, RoutedEventArgs e)
        {
            // Show latency feature removed in v1.7.0
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            // Apply settings
            _s.Save();
            _onSave?.Invoke(_s);
            Close();
        }

        private void LblSettingsPath_Click(object sender, MouseButtonEventArgs e)
        {
            try { Process.Start("explorer.exe", $"\"\"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\RagnaController\"\""); } catch { }
        }

        private void BtnDevConsole_Click(object sender, RoutedEventArgs e)
        {
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RagnaController");
            Directory.CreateDirectory(logDir);
            var logger = new Core.AdvancedLogger(logDir);
            var consoleWin = new DeveloperConsoleWindow(logger) { Owner = this };
            // Dispose logger when console window closes to flush & release file handle
            consoleWin.Closed += (s, _) => logger.Dispose();
            consoleWin.Show();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnTelemetryInfo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Anonymous Telemetry Information\n\n" +
                "This feature sends anonymous data to help improve RagnaController:\n\n" +
                "• App version and operating system\n" +
                "• Crash reports with stack traces\n" +
                "• Basic usage statistics (no personal data)\n\n" +
                "No sensitive information like usernames, passwords, or game progress is collected.\n\n" +
                "Data is sent via Discord Webhook for easy monitoring.",
                "Telemetry Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private async void BtnCalibrate_Click(object sender, RoutedEventArgs e)
        {
            // Use a short-lived ControllerService — properly disposed after calibration
            using var controllerSvc = new ControllerService();
           
            // Wait briefly for background SDL init to complete
            await Task.Delay(800);
           
            if (!controllerSvc.IsConnected)
            {
                EngineStateText.Text = "No gamepad connected";
                EngineStateText.Foreground = Brushes.OrangeRed;
                return;
            }

            BtnCalibrate.IsEnabled = false;
            ChkStartWithWindows.IsEnabled = false;
            EngineStateText.Foreground = Brushes.Yellow;

            float maxDrift = 0f;
            var inputReader = new InputReader(controllerSvc);

            // 3-Second countdown — run async so UI thread stays responsive
            for (int i = 3; i > 0; i--)
            {
                EngineStateText.Text = string.Format("Calibrating... ({i})", i);
               
                // Sample multiple times per second for accuracy
                for (int sample = 0; sample < 10; sample++)
                {
                    var input = inputReader.Read();
                    if (input.IsConnected)
                    {
                        float highest = Math.Max(Math.Max(Math.Abs(input.LeftX), Math.Abs(input.LeftY)),
                                                 Math.Max(Math.Abs(input.RightX), Math.Abs(input.RightY)));
                        if (highest > maxDrift) maxDrift = highest;
                    }
                    await Task.Delay(100);
                }
            }

            // Add a 2% safety buffer to the maximum detected drift
            float finalDeadzone = (float)Math.Round(maxDrift + 0.02f, 2);
           
            // Hard caps to prevent crazy values if the user touched the stick
            if (finalDeadzone > 0.40f) finalDeadzone = 0.40f; 
            if (finalDeadzone < 0.05f) finalDeadzone = 0.05f;

            // Save globally (Assume ProfileManager will pick this up or we update the active profile)
            // Note: Since Deadzone is currently a Profile property, we should update the ACTIVE profile.
            var profileManager = new ProfileManager();
            if (profileManager?.ActiveProfile != null)
            {
                profileManager.ActiveProfile.Deadzone = finalDeadzone;
                profileManager.ActiveProfile.CursorDeadzone = finalDeadzone;
                profileManager.SaveProfile(profileManager.ActiveProfile);
            }

            EngineStateText.Foreground = Brushes.LimeGreen;
            EngineStateText.Text = $"Calibration complete: {finalDeadzone} deadzone";
           
            BtnCalibrate.IsEnabled = true;
            ChkStartWithWindows.IsEnabled = true;
        }

        private void BtnReportBug_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "https://github.com/Patbeck85/RagnaController/issues",
                    UseShellExecute = true // CRITICAL for .NET 8 to open URLs
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open the browser:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnTutorial_Click(object sender, RoutedEventArgs e)
        {
            var tutWin = new TutorialWindow { Owner = this };
            tutWin.ShowDialog();
        }

        private void BtnInstallDriver_Click(object sender, RoutedEventArgs e)
        {
            string installerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AntiCheat", "install-interception.exe");

            if (!File.Exists(installerPath))
            {
                MessageBox.Show("Installer not found! Please ensure that the AntiCheat folder has been extracted correctly.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show(
                "This process installs the Interception kernel driver.\n\n" +
                "Administrator rights are required. After installation, the PC MUST be restarted.\n\n" +
                "Install now?", 
                "Driver Installation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Starts the installation "silent" (without annoying CMD window)
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = installerPath,
                        Arguments = "/install",
                        UseShellExecute = true,
                        Verb = "runas", // Zwingt UAC (Admin-Abfrage)
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    System.Diagnostics.Process? process = null;
                    try { process = Process.Start(processInfo); } catch (Exception ex) { MessageBox.Show($"Launch failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }
                    process?.WaitForExit();

                    MessageBox.Show("Installation completed!\n\nPlease restart your PC now for the bypass to work.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Installation aborted or failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnBrowseRoExe_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Ragnarok Online .exe",
                Filter = "Executable files|*.exe",
                CheckFileExists = true,
                ValidateNames = true
            };

            if (dialog.ShowDialog() == true)
            {
                // Extract the process name (without ".exe") from the selected path
                // and apply it as the FocusLock target — this is what actually
                // gets matched against the foreground window's process name.
                string processName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                if (!string.IsNullOrWhiteSpace(processName))
                {
                    _s.FocusLockProcess = processName;
                    _s.Save();
                }
            }
        }

        private void RoExePathCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateRoExeStatusLabel();
            
            // If "Browse..." is selected, open file dialog
            if (RoExePathCombo.SelectedItem is string selectedPath && selectedPath == "Browse...")
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select Ragnarok Online .exe",
                    Filter = "Executable files|*.exe",
                    CheckFileExists = true,
                    ValidateNames = true
                };

                if (dialog.ShowDialog() == true)
                {
                    string selectedFile = dialog.FileName;
                    // Add to the combo box if not already present
                    var paths = RoExePathCombo.ItemsSource as List<string>;
                    if (paths != null && !paths.Contains(selectedFile))
                    {
                        paths.Insert(paths.Count - 1, selectedFile); // Insert before "Browse..."
                        RoExePathCombo.Items.Refresh();
                    }
                    RoExePathCombo.SelectedItem = selectedFile;
                    _s.RoExePath = selectedFile;
                    _s.Save();
                }
                else
                {
                    // User cancelled - revert to previous selection
                    if (!string.IsNullOrEmpty(_s.RoExePath))
                    {
                        RoExePathCombo.SelectedItem = _s.RoExePath;
                    }
                    else if (RoExePathCombo.Items.Count > 0)
                    {
                        RoExePathCombo.SelectedIndex = 0;
                    }
                }
            }
            else if (RoExePathCombo.SelectedItem is string validPath)
            {
                _s.RoExePath = validPath;
                _s.Save();
            }
        }

        private void LogLevelCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LogLevelCombo.SelectedItem is ComboBoxItem item && item.Tag is int level)
            {
                _s.LogLevel = level;
                _s.Save();
            }
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageCombo.SelectedItem is ComboBoxItem item && item.Tag is string langCode)
            {
                _s.AppLanguage = langCode;
                LocalizationManager.Instance.CurrentLanguage = langCode;
                _s.Save();
            }
        }

        private void OverlayThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OverlayThemeCombo.SelectedItem is ComboBoxItem item && item.Tag is Settings.OverlayThemeType theme)
            {
                _s.OverlayTheme = theme;
                _s.Save();
            }
        }

        private void OverlayOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _s.OverlayOpacity = e.NewValue;
            OverlayOpacityValue.Text = $"{e.NewValue:P0}";
            _s.Save();
        }

        private void OverlayFontScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _s.OverlayFontScale = e.NewValue;
            OverlayFontScaleValue.Text = $"{e.NewValue:P0}";
            _s.Save();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save settings when window is closing
            if (_onSave != null)
            {
                _s.AutoStart = ChkAutoStart.IsChecked == true;
                _s.SoundEnabled = ChkSound.IsChecked == true;
                _s.RumbleEnabled = ChkRumble.IsChecked == true;
                _s.StartInMiniMode = ChkStartInMiniMode.IsChecked == true;

                // Smart Standby (AFK Battery Saver)
                _s.EnableSmartStandby = ChkSmartStandby.IsChecked == true;
                if (int.TryParse(TxtStandbyMinutes.Text, out int mins)) 
                    _s.StandbyTimeoutMinutes = Math.Max(1, mins);

                // v1.7.0: Silent Mode Settings
                _s.MinimizeToTray = ChkMinimizeToTray.IsChecked == true;
                bool autoStart = ChkStartWithWindows.IsChecked == true;
                _s.StartWithWindows = autoStart;
                Core.AutoStartManager.SetAutoStart(autoStart);

                _s.FocusLockEnabled = ChkFocusLock.IsChecked == true;

                // Discord Rich Presence
                _s.EnableDiscordRPC = ChkDiscordRPC.IsChecked == true;

                // Voice Announcements
                _s.EnableVoiceAnnouncements = ChkVoiceAnnouncements.IsChecked == true;

                // Anonymous Telemetry
                _s.EnableTelemetry = ChkTelemetry.IsChecked == true;

                _s.Save();
                _onSave?.Invoke(_s);
            }
        }
    }
}