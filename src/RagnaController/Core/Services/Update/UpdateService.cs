using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RagnaController.Core.Services.Update {
    /// <summary>
    /// Service für automatische Update-Prüfung, Download und Installation.
    /// Nutzt die GitHub Releases API (öffentlich, kein Auth erforderlich für öffentliche Repos).
    /// </summary>
    public class UpdateService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _owner = "Patbeck85";
        private readonly string _repo = "RagnaControllerReloaded";
        private bool _disposed;

        /// <summary>Event ausgelöst wenn ein neues Update gefunden wurde.</summary>
        public event EventHandler<Models.UpdateInfo>? UpdateAvailable;

        /// <summary>Event ausgelöst beim Fortschritt des Downloads (0-100).</summary>
        public event EventHandler<int>? DownloadProgressChanged;

        /// <summary>Event ausgelöst wenn Installation abgeschlossen.</summary>
        public event EventHandler<bool>? InstallationCompleted;

        /// <summary>Aktuelle Installations-Version.</summary>
        public string CurrentVersion { get; }

        public UpdateService(string currentVersion)
        {
            CurrentVersion = currentVersion;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RagnaController-Updater/1.0");
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        /// <summary>
        /// Prüft die GitHub Releases API auf ein neues Update.
        /// Gibt UpdateInfo zurück wenn eine neuere Version verfügbar ist, sonst null.
        /// </summary>
        public async Task<Models.UpdateInfo?> CheckForUpdatesAsync(CancellationToken ct = default)
        {
            try
            {
                var apiUrl = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
                using var response = await _httpClient.GetAsync(apiUrl, ct);
                ct.ThrowIfCancellationRequested();

                if (!response.IsSuccessStatusCode)
                {
                    // Kein kritischer Fehler — Update-Check kann fehlschlagen ohne App-Auswirkung
                    global::System.Diagnostics.Debug.WriteLine($"[UpdateService] GitHub API returned {response.StatusCode}");
                    return null;
                }

                using var contentStream = await response.Content.ReadAsStreamAsync(ct);
                var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(contentStream, cancellationToken: ct);
                ct.ThrowIfCancellationRequested();

                if (release == null || string.IsNullOrWhiteSpace(release.TagName))
                    return null;

                var updateInfo = new Models.UpdateInfo
                {
                    Version = release.TagName.TrimStart('v'),
                    Title = release.Name ?? release.TagName,
                    ReleaseNotes = release.Body ?? string.Empty,
                    PublishedAt = release.PublishedAt,
                    IsMandatory = release.TagName.Contains("critical", StringComparison.OrdinalIgnoreCase)
                };

                // ZIP-Asset finden
                foreach (var asset in release.Assets)
                {
                    if (asset?.Name?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        updateInfo.DownloadUrl = asset.BrowserDownloadUrl!;
                        break;
                    }
                }

                if (!updateInfo.IsNewerThan(CurrentVersion))
                    return null;

                UpdateAvailable?.Invoke(this, updateInfo);
                return updateInfo;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                global::System.Diagnostics.Debug.WriteLine($"[UpdateService] Update-Check fehlgeschlagen: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lädt das Update-ZIP herunter und speichert es im temporären Verzeichnis.
        /// </summary>
        public async Task<string?> DownloadUpdateAsync(Models.UpdateInfo updateInfo, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(updateInfo.DownloadUrl))
                    return null;

                var tempPath = Path.Combine(Path.GetTempPath(), $"RagnaController_{updateInfo.Version}.zip");
                using var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                ct.ThrowIfCancellationRequested();

                if (!response.IsSuccessStatusCode)
                {
                    global::System.Diagnostics.Debug.WriteLine($"[UpdateService] Download failed: {response.StatusCode}");
                    return null;
                }

                using var contentStream = await response.Content.ReadAsStreamAsync(ct);
                var totalBytes = response.Content.Headers.ContentLength ?? 0L;
                var downloadedBytes = 0L;

                using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
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
                global::System.Diagnostics.Debug.WriteLine($"[UpdateService] Download complete: {tempPath} ({downloadedBytes} bytes)");
                return tempPath;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                global::System.Diagnostics.Debug.WriteLine($"[UpdateService] Download fehlgeschlagen: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Installiert das heruntergeladene Update. Erstellt ein Backup der aktuellen EXE,
        /// entpackt das ZIP ins Installationsverzeichnis und gibt true zurück bei Erfolg.
        /// Die App muss nach der Installation neu gestartet werden.
        /// </summary>
        public async Task<bool> InstallUpdateAsync(string zipPath, string? installDirectory = null)
        {
            try
            {
                if (!File.Exists(zipPath))
                    return false;

                var targetDir = installDirectory ?? AppContext.BaseDirectory;
                var backupDir = Path.Combine(targetDir, "backup_pre_update");

                // Backup der aktuellen EXE
                var exePath = Path.Combine(targetDir, "RagnaController.exe");
                if (File.Exists(exePath))
                {
                    Directory.CreateDirectory(backupDir);
                    var backupExe = Path.Combine(backupDir, "RagnaController.exe");
                    File.Copy(exePath, backupExe, overwrite: true);
                }

                // ZIP entpacken (überschreibt bestehende Dateien)
                ZipFile.ExtractToDirectory(zipPath, targetDir, overwriteFiles: true);

                global::System.Diagnostics.Debug.WriteLine($"[UpdateService] Installation complete in {targetDir}");
                InstallationCompleted?.Invoke(this, true);
                return true;
            }
            catch (Exception ex)
            {
                global::System.Diagnostics.Debug.WriteLine($"[UpdateService] Installation fehlgeschlagen: {ex.Message}");
                InstallationCompleted?.Invoke(this, false);
                return false;
            }
            finally
            {
                // Temp-ZIP aufräumen
                try
                {
                    if (File.Exists(zipPath))
                        File.Delete(zipPath);
                }
                catch
                {
                    // Temp-Datei-Löschung ist nicht kritisch
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _httpClient.Dispose();
        }
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
