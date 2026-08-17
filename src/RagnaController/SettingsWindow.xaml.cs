using System;
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

        public SettingsWindow(HybridEngine engine, ProfileManager manager, Settings s)
        {
            InitializeComponent();
            _engine = engine;
            _manager = manager;
            _s = s;
        
            // Initialize all settings from saved values
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            // Profile Settings
            if (ProfileCombo.Items.Contains(_s.LastProfileName))
            {
                ProfileCombo.SelectedItem = _s.LastProfileName;
            }
            CurrentProfileText.Text = string.IsNullOrEmpty(_s.LastProfileName) 
                ? "No profile loaded" 
                : $"Current: {_s.LastProfileName}";

            // Engine Settings
            ChkStartWithWindows.IsChecked = _s.StartWithWindows;
            ChkMinimizeToTray.IsChecked = _s.MinimizeToTray;
        }

        private void TurboToggle_Click(object sender, MouseButtonEventArgs e)
        {
            // Toggle turbo mode
            _s.TurboMode = !_s.TurboMode;
            TurboToggleThumb.Fill = _s.TurboMode ? Brushes.LimeGreen : Brushes.Gray;
            TurboStatusText.Text = _s.TurboMode ? "TURBO ACTIVE" : "TURBO OFF";
            _s.Save();
        }

        private void ChkAutoLoadProfile_Click(object sender, RoutedEventArgs e)
        {
            // Auto-load profile feature removed in v1.7.0
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save settings when window is closing
            if (_onSave != null)
            {
                _s.AutoStart = ChkAutoStart.IsChecked == true;
                _s.SoundEnabled = ChkSound.IsChecked == true;
                _s.RumbleEnabled = ChkRumble.IsChecked == true;
                _s.EnableHapticMetronome = ChkHapticMetronome.IsChecked == true;
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

                // i18n: Handle language switching
                if (LanguageCombo.SelectedItem is ComboBoxItem item && item.Tag is string langCode)
                {
                    _s.AppLanguage = langCode;
                    LocalizationManager.Instance.CurrentLanguage = langCode;
                }

                _s.Save();
                _onSave?.Invoke(_s);
            }
        }
    }
}
