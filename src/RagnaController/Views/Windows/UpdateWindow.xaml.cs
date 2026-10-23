using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using RagnaController.Core.Services;
using RagnaController.Models;

namespace RagnaController.Views.Windows
{
    /// <summary>
    /// Update-Fenster: Prüfung, Download und Installation von Updates.
    /// </summary>
    public partial class UpdateWindow : Window, INotifyPropertyChanged
    {
        private readonly UpdateService? _updateService;
        private readonly CancellationTokenSource _cts = new();
        private UpdateInfo? _availableUpdate;
        private bool _isChecking;
        private bool _isDownloading;
        private bool _isInstalling;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string CurrentVersion => _updateService?.CurrentVersion ?? "unknown";

        public bool AutoUpdateEnabled
        {
            get => Settings.Default.AutoUpdateEnabled;
            set
            {
                if (Settings.Default.AutoUpdateEnabled != value)
                {
                    Settings.Default.AutoUpdateEnabled = value;
                    Settings.Default.Save();
                    OnPropertyChanged();
                }
            }
        }

        public int UpdateCheckIntervalHours
        {
            get => Settings.Default.UpdateCheckIntervalHours;
            set
            {
                if (Settings.Default.UpdateCheckIntervalHours != value)
                {
                    Settings.Default.UpdateCheckIntervalHours = value;
                    Settings.Default.Save();
                    OnPropertyChanged();
                }
            }
        }

        public UpdateWindow()
        {
            InitializeComponent();

            try
            {
                var assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                _updateService = new UpdateService(assemblyVersion?.ToString() ?? "0.0.0");

                _updateService.UpdateAvailable += OnUpdateAvailable;
                _updateService.DownloadProgressChanged += OnDownloadProgressChanged;
                _updateService.InstallationCompleted += OnInstallationCompleted;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateWindow] Init failed: {ex.Message}");
                ShowError("Update-Dienst konnte nicht initialisiert werden.");
            }

            DataContext = this;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private async void BtnCheckNow_Click(object sender, RoutedEventArgs e)
        {
            if (_isChecking || _updateService == null) return;

            _isChecking = true;
            TxtStatus.Text = "Suche nach Updates...";
            HideAllPanels();
            ErrorPanel.Visibility = Visibility.Collapsed;

            try
            {
                var update = await _updateService.CheckForUpdatesAsync(_cts.Token);

                if (update == null)
                {
                    TxtStatus.Text = "Keine Updates verfügbar.";
                    NoUpdatePanel.Visibility = Visibility.Visible;
                    Settings.Default.LastUpdateCheckAt = DateTime.Now;
                    Settings.Default.Save();
                }
                else
                {
                    _availableUpdate = update;
                    TxtNewVersion.Text = $"v{update.Version}";
                    TxtReleaseNotes.Text = string.IsNullOrWhiteSpace(update.ReleaseNotes)
                        ? "Keine Versionshinweise verfügbar."
                        : update.ReleaseNotes.Length > 500
                            ? update.ReleaseNotes.Substring(0, 500) + "..."
                            : update.ReleaseNotes;
                    UpdateAvailablePanel.Visibility = Visibility.Visible;
                    TxtStatus.Text = $"Update v{update.Version} verfügbar.";
                }
            }
            catch (OperationCanceledException)
            {
                TxtStatus.Text = "Update-Prüfung abgebrochen.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateWindow] Check failed: {ex.Message}");
                ShowError($"Update-Prüfung fehlgeschlagen: {ex.Message}");
            }
            finally
            {
                _isChecking = false;
            }
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (_isDownloading || _availableUpdate == null || _updateService == null) return;

            _isDownloading = true;
            BtnDownload.IsEnabled = false;
            ProgressDownload.Visibility = Visibility.Visible;
            ProgressDownload.Value = 0;
            TxtDownloadStatus.Text = "Lade herunter...";

            try
            {
                var zipPath = await _updateService.DownloadUpdateAsync(_availableUpdate, _cts.Token);

                if (zipPath != null)
                {
                    TxtDownloadStatus.Text = "Download abgeschlossen. Klicken Sie auf Installieren.";
                    BtnDownload.Content = "Installieren";
                    BtnDownload.IsEnabled = true;
                    BtnDownload.Click -= BtnDownload_Click;
                    BtnDownload.Click += BtnInstall_Click;
                    _isInstalling = true;
                }
                else
                {
                    ShowError("Download fehlgeschlagen.");
                }
            }
            catch (OperationCanceledException)
            {
                TxtDownloadStatus.Text = "Download abgebrochen.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateWindow] Download failed: {ex.Message}");
                ShowError($"Download fehlgeschlagen: {ex.Message}");
            }
            finally
            {
                if (!_isInstalling)
                {
                    _isDownloading = false;
                    BtnDownload.IsEnabled = true;
                }
            }
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling || _updateService == null) return;

            var result = MessageBox.Show(
                "Die Anwendung wird nach der Installation neu gestartet. Möchten Sie fortfahren?",
                "Update installieren",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            _isInstalling = true;
            BtnDownload.IsEnabled = false;
            TxtDownloadStatus.Text = "Installiere Update...";

            try
            {
                var zipPath = Path.Combine(Path.GetTempPath(), $"RagnaController_{_availableUpdate!.Version}.zip");

                if (!File.Exists(zipPath))
                {
                    ShowError("Update-Datei nicht gefunden. Bitte erneut herunterladen.");
                    return;
                }

                var success = await _updateService.InstallUpdateAsync(zipPath);

                if (success)
                {
                    TxtDownloadStatus.Text = "Installation erfolgreich. Anwendung wird neu gestartet...";
                    TxtStatus.Text = "Installation abgeschlossen.";

                    // Kurze Verzögerung dann Neustart
                    await Task.Delay(1500, _cts.Token);
                    RestartApplication();
                }
                else
                {
                    ShowError("Installation fehlgeschlagen.");
                }
            }
            catch (OperationCanceledException)
            {
                TxtDownloadStatus.Text = "Installation abgebrochen.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateWindow] Install failed: {ex.Message}");
                ShowError($"Installation fehlgeschlagen: {ex.Message}");
            }
            finally
            {
                _isInstalling = false;
                BtnDownload.IsEnabled = true;
            }
        }

        private void BtnSkip_Click(object sender, RoutedEventArgs e)
        {
            _availableUpdate = null;
            UpdateAvailablePanel.Visibility = Visibility.Collapsed;
            NoUpdatePanel.Visibility = Visibility.Visible;
            TxtStatus.Text = "Update übersprungen.";
        }

        private void BtnRemindLater_Click(object sender, RoutedEventArgs e)
        {
            _availableUpdate = null;
            UpdateAvailablePanel.Visibility = Visibility.Collapsed;
            NoUpdatePanel.Visibility = Visibility.Visible;
            TxtStatus.Text = "Erinnere mich später.";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnUpdateAvailable(object? sender, UpdateInfo e)
        {
            // Wird vom UpdateService aufgerufen (möglicherweise auf Hintergrund-Thread)
            Dispatcher.Invoke(() =>
            {
                _availableUpdate = e;
                TxtNewVersion.Text = $"v{e.Version}";
                UpdateAvailablePanel.Visibility = Visibility.Visible;
                TxtStatus.Text = $"Update v{e.Version} verfügbar.";
            });
        }

        private void OnDownloadProgressChanged(object? sender, int progress)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressDownload.Value = progress;
                TxtDownloadStatus.Text = $"Download: {progress}%";
            });
        }

        private void OnInstallationCompleted(object? sender, bool success)
        {
            Dispatcher.Invoke(() =>
            {
                _isInstalling = false;
                BtnDownload.IsEnabled = true;

                if (success)
                {
                    TxtDownloadStatus.Text = "Installation erfolgreich.";
                }
                else
                {
                    ShowError("Installation fehlgeschlagen.");
                }
            });
        }

        private void ShowError(string message)
        {
            HideAllPanels();
            ErrorPanel.Visibility = Visibility.Visible;
            TxtError.Text = message;
            TxtStatus.Text = "Fehler aufgetreten.";
        }

        private void HideAllPanels()
        {
            UpdateAvailablePanel.Visibility = Visibility.Collapsed;
            NoUpdatePanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            ProgressDownload.Visibility = Visibility.Collapsed;
        }

        private void RestartApplication()
        {
            try
            {
                var exePath = Path.Combine(AppContext.BaseDirectory, "RagnaController.exe");
                if (File.Exists(exePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    });
                }
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UpdateWindow] Restart failed: {ex.Message}");
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _cts.Cancel();
            _updateService?.Dispose();
        }
    }
}
