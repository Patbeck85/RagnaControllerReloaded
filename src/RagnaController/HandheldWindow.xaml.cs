using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using RagnaController.Core;
using RagnaController.Profiles;

namespace RagnaController
{
    public partial class HandheldWindow : Window
    {
        private readonly HybridEngine        _engine;
        private readonly ProfileManager      _manager;
        private readonly GamepadUiNavigator  _navigator;
        private InGameOverlayWindow?         _overlay;
        private SettingsWindow?              _settingsWin;

        public HandheldWindow(HybridEngine engine, ProfileManager manager)
        {
            InitializeComponent();
            _engine    = engine;
            _manager   = manager;
            _navigator = new GamepadUiNavigator(engine.ControllerSvc)
                { ActiveWindow = this };

            // Display device name in label
            DeviceLabel.Text = HandheldDetector.DeviceName.ToUpper();

            // Wire up engine events
            _engine.ControllerConnected    += name => Dispatcher.BeginInvoke(() => SetControllerStatus(name, true));
            _engine.ControllerDisconnected += ()   => Dispatcher.BeginInvoke(() => SetControllerStatus("—", false));
            _engine.StatusChanged          += _    => Dispatcher.BeginInvoke(UpdateProfileLabel);

            // B-Button = Back / Start = Play
            KeyDown += Window_KeyDown;

            // Start button on controller → play directly
            engine.ProfileQuickSwitch += _ => Dispatcher.BeginInvoke(CycleProfile);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _navigator.Start();
            BtnPlay.Focus();        // First element focused → D-Pad can navigate immediately
            UpdateProfileLabel();
            _engine.Start();
        }

        // ── Navigation ────────────────────────────────────────────────────
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Escape / B-Button (mapped as Escape in WPF) = Close app
            if (e.Key == Key.Escape) BtnExit_Click(this, new RoutedEventArgs());
        }

        // ── Tile Handlers ────────────────────────────────────────────────
        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Suche nach ragexe.exe / sakexe.exe in üblichen Pfaden
                var candidates = new[]
                {
                    @"C:\Program Files\Gravity\RO",
                    @"C:\Games\RagnarokOnline",
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Ragnarok Online"
                };
                string? exe = null;
                foreach (string dir in candidates)
                {
                    string[] matches = System.IO.Directory.Exists(dir)
                        ? System.IO.Directory.GetFiles(dir, "*exe.exe", System.IO.SearchOption.TopDirectoryOnly)
                        : Array.Empty<string>();
                    if (matches.Length > 0) { exe = matches[0]; break; }
                }

                if (exe != null)
                    try { Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true }); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[HandheldWindow] Launch failed: {ex.Message}"); }
                else
                {
                    MessageBox.Show(
                        LocalizationManager.GetLocalizedString("Handheld_Play_NoGameFound_Message"),
                        LocalizationManager.GetLocalizedString("Handheld_Play_NoGameFound_Title"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(LocalizationManager.GetLocalizedString("Handheld_Play_Error_Message") + ": " + ex.Message);
            }
        }

        private void BtnProfiles_Click(object sender, RoutedEventArgs e)
        {
            // Quick profile selection as ListBox popup
            var win = new ProfileLibraryWindow(_manager)
                { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            _navigator.ActiveWindow = win;
            win.ShowDialog();
            _navigator.ActiveWindow = this;
            UpdateProfileLabel();
        }

        private void BtnOverlay_Click(object sender, RoutedEventArgs e)
        {
            if (_overlay == null)
            {
                _overlay = new InGameOverlayWindow(_engine.Messenger, _engine.WindowTracker);
                _overlay.Closed += (_, _) => _overlay = null;
            }
            if (!_overlay.IsVisible) _overlay.Show();
            // Overlay open — Handheld UI stays in background
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsWin == null)
            {
                _settingsWin = new SettingsWindow(_engine, _manager, Models.Settings.Load());
                _settingsWin.Closed += (_, _) => { _settingsWin = null; _navigator.ActiveWindow = this; };
            }
            _navigator.ActiveWindow = _settingsWin;
            _settingsWin.Show();
        }

        private void BtnCommunity_Click(object sender, RoutedEventArgs e)
        {
            var win = new CommunityBrowserWindow(_manager)
                { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            _navigator.ActiveWindow = win;
            win.ShowDialog();
            _navigator.ActiveWindow = this;
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            _navigator.Dispose();
            _overlay?.Close();
            _settingsWin?.Close();
            _engine.Shutdown();
            Application.Current.Shutdown();
            System.Threading.Tasks.Task.Delay(500).ContinueWith(_ => Environment.Exit(0));
        }

        // ── Helper Methods ─────────────────────────────────────────────────
        private void SetControllerStatus(string name, bool connected)
        {
            ControllerStatusText.Text       = connected ? $"Connected — {name}" : "No Controller";
            ControllerStatusText.Foreground = connected
                ? new SolidColorBrush(Color.FromRgb(0x3D, 0xDB, 0x6E))
                : new SolidColorBrush(Color.FromRgb(0xFF, 0x3A, 0x52));
            ControllerDot.Fill = ControllerStatusText.Foreground;
        }

        private void UpdateProfileLabel()
        {
            var active = _manager.Profiles.FirstOrDefault(p => p.Name == _manager.ActiveProfileName);
            ActiveProfileText.Text = active != null
                ? $"{active.Class}  ·  {active.Name}"
                : "No Profile";
        }

        private void CycleProfile()
        {
            var names = _manager.GetAllNames().ToList();
            if (names.Count < 2) return;
            int idx  = names.IndexOf(_manager.ActiveProfileName);
            string next = names[(idx + 1) % names.Count];
            _manager.SetActive(next);
            var active = _manager.ActiveProfile;
            if (active != null)
            {
                _engine.LoadProfile(active);
            }
            UpdateProfileLabel();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // FIX: Cannot assign to event properties directly - use WeakEvent pattern or remove subscriptions
            // Events are already unsubscribed via _engine.Shutdown() call in MainWindow.xaml.cs
        }
    }
}
