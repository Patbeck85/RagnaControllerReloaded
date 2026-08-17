using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController
{
    public partial class CommunityBrowserWindow : Window
    {
        private static readonly Lazy<LocalizationManager> _localization = new(() => LocalizationManager.Instance);
        public static string GetLocalizedString(string key) => LocalizationManager.GetLocalizedString(key);

        // Removed unused field: _engine (never used in this window)
        // IMPORTANT: Replace this placeholder URL with your actual GitHub Gist URL!
        // To create a gist: https://gist.github.com/new
        // Then update this constant with: "https://gist.githubusercontent.com/YOUR_USERNAME/YOUR_GIST_ID/raw/registry.json"
        private const string RegistryUrl = "https://gist.githubusercontent.com/Patbeck85/YOUR_GIST_ID/raw/registry.json";
        
        private readonly ProfileManager _manager;
        private static readonly HttpClient _http;
        private List<CommunityEntry> _allEntries = new();
        private readonly object _lock = new(); // Thread-Safety für _allEntries

        static CommunityBrowserWindow()
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        }

        public CommunityBrowserWindow(ProfileManager manager)
        {
            InitializeComponent();
            _manager = manager;
            Loaded += (_, _) => _ = LoadRegistryAsync();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try { await LoadRegistryAsync(); }
            catch (Exception ex) { ErrorText.Text = ex.Message; ErrorOverlay.Visibility = Visibility.Visible; }
        }

        private async Task LoadRegistryAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            ErrorOverlay.Visibility = Visibility.Collapsed;

            try
            {
                // To avoid caching issues from GitHub raw content
                string urlWithCacheBust = $"{RegistryUrl}?t={DateTime.UtcNow.Ticks}";
                string json = await _http.GetStringAsync(urlWithCacheBust);

                var entries = JsonSerializer.Deserialize<List<CommunityEntry>>(json, AppJsonContext.Default.ListCommunityEntry);
                
                if (entries != null)
                {
                    _allEntries = entries;
                    ApplyFilter();
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                ErrorText.Text = $"Could not connect to the Community Hub.\n\nError: {ex.Message}";
                LoadingOverlay.Visibility = Visibility.Collapsed;
                ErrorOverlay.Visibility = Visibility.Visible;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string query = SearchBox.Text.ToLowerInvariant();
            
            var filtered = string.IsNullOrWhiteSpace(query) 
                ? _allEntries 
                : _allEntries.Where(x => 
                    !string.IsNullOrEmpty(x.Name) && x.Name.ToLowerInvariant().Contains(query) || 
                    !string.IsNullOrEmpty(x.Class) && x.Class.ToLowerInvariant().Contains(query) || 
                    !string.IsNullOrEmpty(x.Author) && x.Author.ToLowerInvariant().Contains(query)).ToList();

            CommunityList.ItemsSource = filtered;
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
            if (sender is Button btn && btn.Tag is string shareCode)
            {
                btn.IsEnabled = false;
                btn.Content = GetLocalizedString("CommunityBrowser_Downloading");

                try
                {
                    // Uses the existing ProfileShareService logic!
                    var result = await ProfileShareService.DownloadAsync(shareCode);
                    
                    if (result.Success && result.Profile != null)
                    {
                        // Overwrite existing profile if name matches, or create new
                        _manager.AddAndSave(result.Profile);
                        
                        btn.Content = GetLocalizedString("CommunityBrowser_Installed");
                        btn.Foreground = System.Windows.Media.Brushes.Lime;
                    }
                    else
                    {
                        MessageBox.Show($"Download failed:\n{result.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        btn.Content = GetLocalizedString("CommunityBrowser_DownloadButton");
                        btn.IsEnabled = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unexpected error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    btn.Content = GetLocalizedString("CommunityBrowser_DownloadButton");
                    btn.IsEnabled = true;
                }
            }
            }
            catch (Exception outerEx)
            {
                MessageBox.Show($"Error: {outerEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            // _http is static and shared — do NOT dispose here or subsequent window opens will throw ObjectDisposedException
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
