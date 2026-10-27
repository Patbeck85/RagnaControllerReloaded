using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RagnaController.Core.Services.Update
{
    /// <summary>
    /// Lädt den Interception-Treiber (install-interception.exe) automatisch von GitHub Releases herunter.
    /// Nutzt die GitHub Releases API (öffentlich, kein Auth erforderlich für öffentliche Repos).
    /// </summary>
    public class InterceptionDriverService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _owner = "oblitum";
        private readonly string _repo = "Interception";
        private bool _disposed;

        /// <summary>Event ausgelöst beim Download-Fortschritt (0-100).</summary>
        public event EventHandler<int>? DownloadProgressChanged;

        public InterceptionDriverService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RagnaController-InterceptionDownloader/1.0");
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        /// <summary>
        /// Prüft ob der Interception-Treiber-Installer im AntiCheat-Ordner vorhanden ist.
        /// </summary>
        public bool DriverInstallerExists(string appBaseDirectory)
        {
            var installerPath = Path.Combine(appBaseDirectory, "AntiCheat", "install-interception.exe");
            return File.Exists(installerPath);
        }

        /// <summary>
        /// Lädt die neueste Interception.zip von GitHub Releases (oblitum/Interception) herunter,
        /// entpackt sie und platziert install-interception.exe im AntiCheat-Ordner.
        /// </summary>
        public async Task<bool> DownloadDriverAsync(string appBaseDirectory, CancellationToken ct = default)
        {
            try
            {
                // 1. GitHub Releases API abfragen (Original-Repo: oblitum/Interception)
                var apiUrl = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
                using var response = await _httpClient.GetAsync(apiUrl, ct);
                ct.ThrowIfCancellationRequested();

                if (!response.IsSuccessStatusCode)
                {
                    global::System.Diagnostics.Debug.WriteLine($"[InterceptionDriverService] GitHub API returned {response.StatusCode}");
                    return false;
                }

                using var contentStream = await response.Content.ReadAsStreamAsync(ct);
                var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(contentStream, cancellationToken: ct);
                ct.ThrowIfCancellationRequested();

                if (release == null || release.Assets == null || release.Assets.Length == 0)
                {
                    global::System.Diagnostics.Debug.WriteLine("[InterceptionDriverService] No release assets found");
                    return false;
                }

                // 2. Interception.zip Asset finden
                var asset = Array.Find(release.Assets, a =>
                    a.Name != null && a.Name.Equals("Interception.zip", StringComparison.OrdinalIgnoreCase));

                if (asset == null || string.IsNullOrEmpty(asset.BrowserDownloadUrl))
                {
                    global::System.Diagnostics.Debug.WriteLine("[InterceptionDriverService] Interception.zip asset not found in release");
                    return false;
                }

                // 3. AntiCheat-Ordner erstellen
                var antiCheatDir = Path.Combine(appBaseDirectory, "AntiCheat");
                Directory.CreateDirectory(antiCheatDir);

                var zipPath = Path.Combine(antiCheatDir, "Interception.zip");

                // 4. ZIP herunterladen mit Fortschritt
                using var downloadResponse = await _httpClient.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                ct.ThrowIfCancellationRequested();

                if (!downloadResponse.IsSuccessStatusCode)
                {
                    global::System.Diagnostics.Debug.WriteLine($"[InterceptionDriverService] Download failed: {downloadResponse.StatusCode}");
                    return false;
                }

                using var contentStream2 = await downloadResponse.Content.ReadAsStreamAsync(ct);
                var totalBytes = downloadResponse.Content.Headers.ContentLength ?? 0L;
                var downloadedBytes = 0L;

                using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream2.ReadAsync(buffer, ct)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
                    downloadedBytes += bytesRead;

                    if (totalBytes > 0)
                    {
                        var percent = (int)((downloadedBytes * 100) / totalBytes);
                        DownloadProgressChanged?.Invoke(this, percent);
                    }
                }

                ct.ThrowIfCancellationRequested();
                global::System.Diagnostics.Debug.WriteLine($"[InterceptionDriverService] ZIP download complete: {zipPath} ({downloadedBytes} bytes)");

                // 5. ZIP entpacken in AntiCheat-Ordner
                try
                {
                    ZipFile.ExtractToDirectory(zipPath, antiCheatDir, overwriteFiles: true);
                    global::System.Diagnostics.Debug.WriteLine($"[InterceptionDriverService] ZIP extracted to {antiCheatDir}");
                }
                catch (Exception ex)
                {
                    global::System.Diagnostics.Debug.WriteLine($"[InterceptionDriverService] ZIP extraction failed: {ex.Message}");
                    return false;
                }
                finally
                {
                    // Temp-ZIP aufräumen
                    try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
                }

                // 6. Prüfen ob install-interception.exe jetzt vorhanden ist
                var installerPath = Path.Combine(antiCheatDir, "install-interception.exe");
                if (!File.Exists(installerPath))
                {
                    global::System.Diagnostics.Debug.WriteLine("[InterceptionDriverService] install-interception.exe not found after extraction");
                    return false;
                }

                global::System.Diagnostics.Debug.WriteLine($"[InterceptionDriverService] Driver ready: {installerPath}");
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                global::System.Diagnostics.Debug.WriteLine($"[InterceptionDriverService] Download fehlgeschlagen: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _httpClient.Dispose();
        }

        // Hilfsklassen für GitHub API Deserialisierung
        internal class GitHubRelease
        {
            public string? TagName { get; set; }
            public string? Name { get; set; }
            public string? Body { get; set; }
            public DateTime PublishedAt { get; set; }
            public GitHubReleaseAsset[] Assets { get; set; } = Array.Empty<GitHubReleaseAsset>();
        }

        internal class GitHubReleaseAsset
        {
            public string? Name { get; set; }
            public string? BrowserDownloadUrl { get; set; }
        }
    }
}
