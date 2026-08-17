using System.Runtime.CompilerServices;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
[assembly: InternalsVisibleTo("RagnaController.Tests")]

namespace RagnaController.Core
{
    public class UpdateChecker
    {
        private static readonly HttpClient _http;

        private const string GithubApiUrl = "https://api.github.com/repos/Patbeck85/RagnaController/releases/latest";
        private static readonly string CurrentVersion = AppVersion.Current;
        private const string UserAgent = "RagnaController-UpdateChecker";

        static UpdateChecker()
        {
            _http = new HttpClient(new SocketsHttpHandler
            {
                PooledConnectionLifetime = System.TimeSpan.FromMinutes(5),
                ConnectTimeout           = System.TimeSpan.FromSeconds(8),
                AutomaticDecompression   = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            })
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        }

        public static async Task<UpdateInfo?> CheckAsync()
        {
            try
            {
                if (GithubApiUrl.Contains("OWNER")) return null;
                var json = await _http.GetStringAsync(GithubApiUrl);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string latest = root.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "0.0.0";
                if (!IsNewer(latest, CurrentVersion)) return null;

                string? downloadUrl = null;
                string? assetName = null;
                long assetSize = 0;

                if (root.TryGetProperty("assets", out var assets))
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        var name = asset.GetProperty("name").GetString() ?? "";
                        if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset.GetProperty("browser_download_url").GetString();
                            assetName = name;
                            assetSize = asset.GetProperty("size").GetInt64();
                            break;
                        }
                    }
                }

                return new UpdateInfo
                {
                    CurrentVersion = CurrentVersion,
                    LatestVersion = latest,
                    ReleaseNotes = root.GetProperty("body").GetString() ?? "",
                    PublishedAt = root.GetProperty("published_at").GetDateTime(),
                    DownloadUrl = downloadUrl ?? root.GetProperty("html_url").GetString() ?? "",
                    AssetName = assetName ?? "RagnaController_v" + CurrentVersion + "_win-framework.zip",
                    AssetSizeBytes = assetSize,
                    IsDirectDownload = downloadUrl != null
                };
            }
            catch { return null; }
        }

        public static async Task CheckAndNotifyAsync(Window owner)
        {
            var info = await CheckAsync();
            if (info == null) return;

            string size = info.AssetSizeBytes > 0 ? $" ({info.AssetSizeBytes / 1048576.0:F1} MB)" : "";
            string msg = $"A new version is available!\n\nCurrent: {info.CurrentVersion}\nNew: {info.LatestVersion}\n\n{Truncate(info.ReleaseNotes, 200)}\n\nDownload now?{size}";

            if (MessageBox.Show(owner, msg, "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                if (info.IsDirectDownload) await DownloadAndOpenAsync(info);
                else try { Process.Start(new ProcessStartInfo { FileName = info.DownloadUrl, UseShellExecute = true }); } catch { /* browser not available */ }
            }
        }

        private static async Task DownloadAndOpenAsync(UpdateInfo info)
        {
            try
            {
                string path = Path.Combine(Path.GetTempPath(), info.AssetName);
                var bytes = await _http.GetByteArrayAsync(info.DownloadUrl);
                await File.WriteAllBytesAsync(path, bytes);
                try { Process.Start("explorer.exe", $"/select,\"{path}\""); } catch { }
            }
            catch (Exception ex) { MessageBox.Show("Download failed: " + ex.Message); }
        }

        internal static bool IsNewer(string v1, string v2)
        {
            try { return new Version(v1) > new Version(v2); } catch { return false; }
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "...";

        public static void Dispose()
        {
            _http?.Dispose();
        }
    }

    public class UpdateInfo
    {
        public string CurrentVersion { get; set; } = "";
        public string LatestVersion { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public DateTime PublishedAt { get; set; }
        public string DownloadUrl { get; set; } = "";
        public string AssetName { get; set; } = "";
        public long AssetSizeBytes { get; set; }
        public bool IsDirectDownload { get; set; }
    }
}