using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RagnaController.Core;
using RagnaController.Profiles;

namespace RagnaController
{
    public partial class ProfileLibraryWindow : Window
    {
        private static readonly Lazy<LocalizationManager> _localization = new(() => LocalizationManager.Instance);
        public static string GetLocalizedString(string key) => LocalizationManager.GetLocalizedString(key);

        private readonly ProfileManager _manager;
        private List<Profile> _cache = new();
        public Profile? SelectedProfile { get; private set; }

        public ProfileLibraryWindow(ProfileManager manager)
        {
            InitializeComponent();
            _manager = manager;
            Loaded += (s, e) => Refresh();
            // Initial state: empty input -> placeholder visible, download disabled.
            UpdateShareCodeValidation();
        }

        /// <summary>UX-013: Toggles watermark, red validation border and Download-button enablement.</summary>
        private void TxtShareCode_TextChanged(object sender, TextChangedEventArgs e) => UpdateShareCodeValidation();

        private void UpdateShareCodeValidation()
        {
            if (TxtShareCode == null || ShareCodePlaceholder == null || ShareCodeErrorBorder == null || BtnDownload == null) return;

            string code = TxtShareCode.Text?.Trim() ?? "";
            bool empty = string.IsNullOrEmpty(code);
            bool valid = ShareCodeValidator.IsValid(code);

            // Watermark: visible only when the field is empty.
            ShareCodePlaceholder.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;

            // Red validation border + tooltip: shown only for non-empty, invalid input (no error on empty).
            if (!empty && !valid)
            {
                ShareCodeErrorBorder.Visibility = Visibility.Visible;
                TxtShareCode.ToolTip = "Invalid format. Expected \"XX-XXXX\" (e.g. GX-A3F9) or a full Gist ID.";
            }
            else
            {
                ShareCodeErrorBorder.Visibility = Visibility.Collapsed;
                TxtShareCode.ToolTip = "Enter a share code (e.g. GX-A3F9) and click ↓ Download";
            }

            // Download button enabled only for a valid code.
            BtnDownload.IsEnabled = valid;
        }

        private void Refresh()
        {
            _cache = _manager.Profiles.ToList();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (SearchBox == null || FilterCombo == null || ProfilesList == null) return;

            string query = SearchBox.Text;
            string type = (FilterCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Profiles";

            var filtered = _cache.Where(p =>
            {
                bool matchesSearch = string.IsNullOrEmpty(query) || p.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
                bool matchesType = type switch
                {
                    "Built-in Only" => p.IsBuiltIn,
                    "Custom Only" => !p.IsBuiltIn,
                    "Melee" => p.Class == "Melee",
                    "Ranged" => p.Class == "Ranged",
                    "Mage" => p.Class == "Mage",
                    "Support" => p.Class == "Support",
                    _ => true
                };
                return matchesSearch && matchesType;
            }).ToList();

            ProfilesList.ItemsSource = filtered;
            ProfileCount.Text = $"{filtered.Count} profiles found";

            // UX-018: Empty State when no profiles found
            if (EmptyStatePanel != null)
                EmptyStatePanel.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (ProfilesList != null)
                ProfilesList.Visibility = filtered.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Profile p) { SelectedProfile = p; DialogResult = true; Close(); }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Profile p)
            {
                var sfd = new Microsoft.Win32.SaveFileDialog { Filter = "JSON|*.json", FileName = $"{p.Name}.json" };
                if (sfd.ShowDialog() == true) _manager.Export(p, sfd.FileName);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Profile p && !p.IsBuiltIn)
            {
                if (/* Toast */ _ = MessageBox.Show($"Delete {p.Name}?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _manager.Delete(p);
                    Refresh();
                }
            }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog { Filter = "JSON|*.json" };
            if (ofd.ShowDialog() == true)
            {
                var p = _manager.ImportPreview(ofd.FileName);
                if (p != null) { _manager.AddAndSave(p); Refresh(); }
            }
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            var wizard = new ProfileWizardWindow(_manager) { Owner = this };
            if (wizard.ShowDialog() == true && wizard.CreatedProfile != null) { _manager.AddAndSave(wizard.CreatedProfile); Refresh(); }
        }

        private async void BtnShare_Click(object sender, RoutedEventArgs e)
        {
            // Sicheres Pattern Matching: Überprüfen, ob sender ein Button ist UND Tag ein Profile ist
            if (sender is not Button btn || btn.Tag is not Profile p) return;
            
            btn.IsEnabled = false;
            btn.Content = GetLocalizedString("ProfileLibrary_Uploading");
            try
            {
                var result = await RagnaController.Core.ProfileShareService.UploadAsync(p);
                if (result.Success)
                {
                    // Sicherer Zugriff auf Code-Suffix (verhindert IndexOutOfRangeException)
                    string codeSuffix = result.Code.Length >= 4 ? result.Code[^4..] : "??";
                    RagnaController.Core.ShareCodeCache.Register(codeSuffix, result.GistId);
                    MessageBox.Show(
                        $"Profile shared!\n\nShare code:  {result.Code}\nGist URL:    {result.Url}\n\nGive the code to other players — they enter it in the Download box.",
                        "Share Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Upload failed:\n{result.Error}", "Share Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // ERROR-001: Upload-Fehler sichtbar machen statt stillen Crash.
                System.Diagnostics.Debug.WriteLine($"[ProfileLibrary] Share failed: {ex.Message}");
                MessageBox.Show($"Upload error:\n{ex.Message}", "Share Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { btn.IsEnabled = true; btn.Content = GetLocalizedString("ProfileLibrary_ShareButton"); }
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            string code = TxtShareCode?.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(code)) { MessageBox.Show("Enter a share code first.", "No Code", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            
            // Sicheres Pattern Matching: Compiler weiß ab hier 100%, dass 'btn' nicht null ist
            if (sender is not Button btn) return;

            btn.IsEnabled = false; 
            btn.Content = "Downloading…";
            try
            {
                var result = await RagnaController.Core.ProfileShareService.DownloadAsync(code);
                if (result.Success && result.Profile != null)
                {
                    var profile = result.Profile; 
                    _manager.AddAndSave(profile);
                    Refresh();
                    
                    // Null-Check hinzugefügt, um CS8602 zu verhindern
                    if (TxtShareCode != null) TxtShareCode.Text = "";
                    
                    // Toast-Feedback (kein blockierendes MessageBox)
                    // CS4014: Dispatcher.BeginInvoke ist synchron, kein Task - Warning ist falsch positiv
#pragma warning disable CS4014
                    Dispatcher.BeginInvoke(() => { if (StatusText != null) { StatusText.Text = $"✓ {profile.Name} downloaded"; StatusText.Foreground = (Brush)FindResource("Live"); } });
#pragma warning restore CS4014
                }
                else
                {
                    MessageBox.Show($"Download failed:\n{result.Error}", "Download Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // ERROR-001: Download-Fehler sichtbar machen statt stillen Crash.
                System.Diagnostics.Debug.WriteLine($"[ProfileLibrary] Download failed: {ex.Message}");
                MessageBox.Show($"Download error:\n{ex.Message}", "Download Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { btn.IsEnabled = true; btn.Content = "↓ Download"; }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

                private void ProfilesList_Drop(object sender, DragEventArgs e)
                {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                        foreach (string file in files)
                        {
                            if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                            {
                                var profile = _manager.ImportPreview(file);
                                if (profile != null)
                                {
                                    _manager.AddAndSave(profile);
                                }
                            }
                        }
                        Refresh();
                    }
                }

                private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove();
        }
    }
}