using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Core.Services.Database;

namespace RagnaController.Views.Windows
{
    /// <summary>
    /// Monster-Import-Fenster: Lädt die vollständige Rathena Monster-Datenbank
    /// (Pre-Renewal + Renewal) von GitHub und importiert sie in die lokale Datenbank.
    /// </summary>
    public partial class MonsterImportWindow : Window, INotifyPropertyChanged
    {
        private readonly MonsterDatabaseService _monsterService;
        private readonly MonsterDataLoader _loader;
        private readonly CancellationTokenSource _cts = new();
        private bool _isImporting;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MonsterImportWindow(MonsterDatabaseService? monsterService = null)
        {
            InitializeComponent();

            _monsterService = monsterService ?? new MonsterDatabaseService();
            _loader = new MonsterDataLoader();

            _loader.ProgressChanged += OnProgressChanged;
            _loader.ImportCompleted += OnImportCompleted;

            DataContext = this;
        }

#pragma warning disable CS0067 // Public event, subscribed externally
        public event PropertyChangedEventHandler? PropertyChangedInternal;
#pragma warning restore CS0067
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private async void BtnImportFromRathena_Click(object sender, RoutedEventArgs e)
        {
            if (_isImporting) return;

            _isImporting = true;
            BtnImportFromRathena.IsEnabled = false;
            BtnCancel.IsEnabled = false;
            ProgressImport.Visibility = Visibility.Visible;
            ProgressImport.Value = 0;
            TxtImportStatus.Text = "Initialisiere Import...";
            TxtImportResult.Text = string.Empty;
            StatsPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
            TxtFooter.Text = "Import läuft...";

            try
            {
                var monsters = await _loader.LoadFromRathenaOnlineAsync(_cts.Token);

                if (monsters.Count > 0)
                {
                    _monsterService.LoadMonsters(monsters);
                    TxtImportResult.Text = $"✓ {monsters.Count} Monster erfolgreich importiert!";
                    TxtImportResult.Foreground = (System.Windows.Media.Brush)FindResource("AccentGreen");
                    TxtFooter.Text = "Import abgeschlossen.";

                    StatsPanel.Visibility = Visibility.Visible;
                    TxtTotalCount.Text = monsters.Count.ToString("N0");
                    TxtMvpCount.Text = _monsterService.GetMvpCount().ToString("N0");
                    TxtGameMode.Text = _monsterService.IsRenewal ? "Renewal" : "Pre-Renewal";
                }
                else
                {
                    ShowError("Keine Monster konnten geladen werden.");
                }
            }
            catch (OperationCanceledException)
            {
                TxtImportStatus.Text = "Import abgebrochen.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MonsterImportWindow] Import fehlgeschlagen: {ex.Message}");
                ShowError($"Import fehlgeschlagen: {ex.Message}");
            }
            finally
            {
                _isImporting = false;
                BtnImportFromRathena.IsEnabled = true;
                BtnCancel.IsEnabled = true;
                ProgressImport.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            TxtImportStatus.Text = "Abbruch wird ausgeführt...";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnProgressChanged(object? sender, (int Percent, string Status) e)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressImport.Value = e.Percent;
                TxtImportStatus.Text = e.Status;
            });
        }

        private void OnImportCompleted(object? sender, int count)
        {
            Dispatcher.Invoke(() =>
            {
                TxtFooter.Text = $"Import abgeschlossen: {count} Monster";
            });
        }

        private void ShowError(string message)
        {
            HideAllPanels();
            ErrorPanel.Visibility = Visibility.Visible;
            TxtError.Text = message;
            TxtFooter.Text = "Fehler beim Import.";
        }

        private void HideAllPanels()
        {
            StatsPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _cts.Cancel();
            _loader.ProgressChanged -= OnProgressChanged;
            _loader.ImportCompleted -= OnImportCompleted;
            _loader.Dispose();
        }
    }
}
