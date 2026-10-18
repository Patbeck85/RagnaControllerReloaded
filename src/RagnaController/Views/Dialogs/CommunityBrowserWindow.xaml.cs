using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Views.Dialogs
{
    public partial class CommunityBrowserWindow : Window
    {
        private static readonly Lazy<LocalizationManager> _localization = new(() => LocalizationManager.Instance);
        /// <summary>
        /// GetLocalizedString method.
        /// <param name="key">key parameter.</param>
        /// <returns>Result of GetLocalizedString.</returns>
        /// </summary>
        public static string GetLocalizedString(string key) => LocalizationManager.GetLocalizedString(key);

        // Removed unused field: _engine (never used in this window)
        // Community Hub Registry — GitHub Gist
        // Registry JSON format: array of { Name, Class, Author, Description, ShareCode }
        // Upload profiles via ProfileLibraryWindow.BtnShare_Click to generate share codes
        // Gist ID: 56042cbefe3dd5381186d43c3a38af0e
        private const string RegistryUrl = "https://gist.githubusercontent.com/Patbeck85/56042cbefe3dd5381186d43c3a38af0e/raw/registry.json";

        private readonly ProfileManager _manager;
        private static readonly HttpClient _http;
        private List<CommunityEntry> _allEntries = new();
        private readonly object _lock = new(); // Thread safety for _allEntries

        /// <summary>
        /// Singleton-HttpClient (application lifetime) — public read-only for Unit-Tests (TECH-018).
        /// WICHTIG: NIE per Instanz/OnClosing disposten — only via <see cref="DisposeRegistryClient"/> beim App-Shutdown.
        /// </summary>
        public static HttpClient RegistryHttpClient => _http;

        /// <summary>
        /// Dispose des Singleton-Clients beim App-Shutdown (Socket-Cleanup, TECH-018).
        /// Is called from App.OnExit — afterwards the app is ended, no reuse possible.
        /// </summary>
        /// <summary>
        /// DisposeRegistryClient method.
        /// </summary>
        public static void DisposeRegistryClient() => _http.Dispose();

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

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _ = BtnRefresh_ClickAsync(e);
        }

        private async Task BtnRefresh_ClickAsync(RoutedEventArgs e)
        {
            try { await LoadRegistryAsync(); }
            catch (Exception ex)
            {
                ErrorText.Text = ex.Message;
                ErrorOverlay.Visibility = Visibility.Visible;
                System.Diagnostics.Debug.WriteLine($"[CommunityBrowserWindow] Refresh failed: {ex.Message}");
            }
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
                    lock (_lock) { _allEntries = entries; }
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

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            EmptyOverlay.Visibility = Visibility.Collapsed;
            CommunityList.Visibility = Visibility.Visible;
        }

        private void ApplyFilter()
        {
            string query = SearchBox.Text.ToLowerInvariant();

            List<CommunityEntry> snapshot;
            lock (_lock) { snapshot = _allEntries; }

            var filtered = string.IsNullOrWhiteSpace(query)
                ? snapshot
                : snapshot.Where(x =>
                    (!string.IsNullOrEmpty(x.Name) && x.Name.ToLowerInvariant().Contains(query)) ||
                    (!string.IsNullOrEmpty(x.Class) && x.Class.ToLowerInvariant().Contains(query)) ||
                    (!string.IsNullOrEmpty(x.Author) && x.Author.ToLowerInvariant().Contains(query))).ToList();

            CommunityList.ItemsSource = filtered;

            // Show empty state if search has query but no results
            bool hasQuery = !string.IsNullOrWhiteSpace(query);
            bool hasResults = filtered.Count > 0;

            if (hasQuery && !hasResults)
            {
                CommunityList.Visibility = Visibility.Collapsed;
                EmptyOverlay.Visibility = Visibility.Visible;
            }
            else
            {
                CommunityList.Visibility = Visibility.Visible;
                EmptyOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            _ = BtnDownload_ClickAsync(sender);
        }

        private async Task BtnDownload_ClickAsync(object sender)
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
                            btn.Foreground = TryFindResource("Live") as Brush ?? Brushes.LimeGreen;
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
            // _http is static/shared (application lifetime) — NICHT hier disposten,
            // otherwise ObjectDisposedException on reopening. Cleanup happens on
            // App-Shutdown via CommunityBrowserWindow.DisposeRegistryClient() (TECH-018).
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
