using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using RagnaController.Core;
using RagnaController.Profiles;

namespace RagnaController.Views.Dialogs
{
    /// <summary>
    /// TASK-2303: Profile Validation UI.
    /// Zeigt Validierungsergebnisse aller Profile an (ProfileValidator).
    /// Farben: Grün = OK, Gelb = Warnung, Rot = Fehler.
    /// </summary>
    public partial class ProfileValidationWindow : Window, INotifyPropertyChanged
    {
        private readonly ObservableCollection<ValidationResultViewModel> _results = new();
        private string _summaryText = "Bereit — klicken Sie auf 'Alle validieren'";

        public string SummaryText
        {
            get => _summaryText;
            set { _summaryText = value; OnPropertyChanged(); }
        }

        public ProfileValidationWindow()
        {
            InitializeComponent();
            ValidationResultsList.ItemsSource = _results;
            DataContext = this;
        }

        // ── Window Drag ──────────────────────────────────────────────
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ── Validate All ─────────────────────────────────────────────
        private void BtnValidateAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var profilesDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RagnaController", "Profiles");

                _results.Clear();
                var results = ProfileValidator.ValidateAll(profilesDir);

                int valid = 0, errors = 0, warnings = 0;
                foreach (var r in results)
                {
                    _results.Add(new ValidationResultViewModel(r));
                    if (r.IsValid) valid++;
                    else errors++;
                    if (r.Warnings.Count > 0) warnings++;
                }

                SummaryText = $"Profile geprüft: {results.Count} | ✅ {valid} gültig | ❌ {errors} mit Fehlern | ⚠️ {warnings} mit Warnungen";

                DetailsText.Text = _results.Count > 0
                    ? $"Wählen Sie ein Profil aus, um Details anzuzeigen. ({_results.Count} Profile geladen)"
                    : "Keine Profile gefunden.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Validierung fehlgeschlagen: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Refresh ──────────────────────────────────────────────────
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            BtnValidateAll_Click(sender, e);
        }

        // ── Export Report ────────────────────────────────────────────
        private void BtnExportReport_Click(object sender, RoutedEventArgs e)
        {
            if (_results.Count == 0)
            {
                MessageBox.Show("Keine Validierungsergebnisse vorhanden. Bitte zuerst 'Alle validieren' klicken.", "Hinweis", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "Textdatei (*.txt)|*.txt|Alle Dateien (*.*)|*.*",
                FileName = $"ProfileValidation_Report_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                DefaultExt = "txt"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    var lines = new List<string>
                    {
                        "=== RagnaController Profile Validation Report ===",
                        $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                        $"",
                        SummaryText,
                        $"",
                    };

                    foreach (var r in _results.OrderBy(r => r.ProfileName))
                    {
                        lines.Add($"[{r.StatusText}] {r.ProfileName} ({r.FileName})");
                        foreach (var err in r.Errors)
                            lines.Add($"  ❌ ERROR: {err}");
                        foreach (var warn in r.Warnings)
                            lines.Add($"  ⚠️  WARN: {warn}");
                        lines.Add($"");
                    }

                    File.WriteAllText(sfd.FileName, string.Join(Environment.NewLine, lines));
                    MessageBox.Show($"Report gespeichert: {sfd.FileName}", "Export erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export fehlgeschlagen: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ── Selection Changed → Details ──────────────────────────────
        private void ValidationResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ValidationResultsList.SelectedItem is ValidationResultViewModel vm)
            {
                var lines = new List<string>
                {
                    $"Profil: {vm.ProfileName}",
                    $"Datei: {vm.FileName}",
                    $"Status: {vm.StatusText}",
                    $"",
                };

                if (vm.Errors.Count > 0)
                {
                    lines.Add("FEHLER:");
                    foreach (var err in vm.Errors)
                        lines.Add($"  - {err}");
                    lines.Add($"");
                }

                if (vm.Warnings.Count > 0)
                {
                    lines.Add("WARNUNGEN:");
                    foreach (var warn in vm.Warnings)
                        lines.Add($"  - {warn}");
                    lines.Add($"");
                }

                if (vm.Errors.Count == 0 && vm.Warnings.Count == 0)
                    lines.Add("Keine Probleme gefunden — Profil ist gültig.");

                DetailsText.Text = string.Join(Environment.NewLine, lines);
            }
        }

        // ── INotifyPropertyChanged ───────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// ViewModel für ein einzelnes Validierungsergebnis.
    /// </summary>
    public class ValidationResultViewModel : INotifyPropertyChanged
    {
        private readonly Core.ProfileValidator.ValidationResult _result;

        public string ProfileName => _result.ProfileName;
        public string FileName => _result.FileName;
        public List<string> Errors => _result.Errors;
        public List<string> Warnings => _result.Warnings;
        public int ErrorCount => _result.Errors.Count;
        public int WarningCount => _result.Warnings.Count;

        public string StatusText => _result.IsValid ? "OK" : "FEHLER";
        public string StatusColor => _result.IsValid ? "#39FF14" : (_result.Warnings.Count > 0 ? "#FF3A52" : "#FF3A52");
        public string ErrorColor => ErrorCount > 0 ? "#FF3A52" : "#39FF14";
        public string WarningColor => WarningCount > 0 ? "#E5B842" : "#39FF14";

        public ValidationResultViewModel(Core.ProfileValidator.ValidationResult result)
        {
            _result = result;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
