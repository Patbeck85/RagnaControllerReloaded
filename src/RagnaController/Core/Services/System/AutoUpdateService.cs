using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RagnaController.Core.Services.System {
    /// <summary>
    /// TASK-3302: AutoUpdateService — Automatische Aktualisierung von RagnaController.
    ///
    /// Unterstützt:
    ///   • Silent/Delta-Update im Hintergrund
    ///   • Update-Prüfung gegen konfigurierten Update-Channel
    ///   • Herunterladen von Update-Paketen
    ///   • Restart-Prompt nach erfolgreichem Update
    ///
    /// White-Hat: Nur Update-Steuerung, keine Spielautomatisierung.
    /// </summary>
    public sealed class AutoUpdateService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _updateChannel;
        private readonly string _currentVersion;
        private bool _disposed = false;

        /// <summary>Ob ein Update verfügbar ist.</summary>
        public bool UpdateAvailable { get; private set; }

        /// <summary>Version des verfügbaren Updates.</summary>
        public string? LatestVersion { get; private set; }

        /// <summary>Download-URL des Updates.</summary>
        public string? UpdateDownloadUrl { get; private set; }

        /// <summary>Event bei Update-Verfügbarkeit.</summary>
        public event EventHandler<UpdatePackageInfo>? UpdateFound;

        /// <summary>Event bei Update-Fehler.</summary>
        public event EventHandler<string>? UpdateError;

        public AutoUpdateService(string currentVersion, string updateChannel = "stable", string? baseUrl = null)
        {
            _currentVersion = currentVersion;
            _updateChannel = updateChannel;
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl ?? "https://updates.ragnacontroller.example.com/") };
        }

        /// <summary>Prüft auf Updates gegen den Server.</summary>
        public async Task<bool> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"updates/{_updateChannel}/latest", cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return false;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var updateInfo = JsonSerializer.Deserialize<UpdatePackageInfo>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (updateInfo != null && IsNewerVersion(updateInfo.Version, _currentVersion))
                {
                    UpdateAvailable = true;
                    LatestVersion = updateInfo.Version;
                    UpdateDownloadUrl = updateInfo.DownloadUrl;
                    UpdateFound?.Invoke(this, updateInfo);
                    return true;
                }

                UpdateAvailable = false;
                return false;
            }
            catch (Exception ex)
            {
                UpdateError?.Invoke(this, ex.Message);
                return false;
            }
        }

        /// <summary>Lädt das Update-Paket herunter.</summary>
        public async Task<string?> DownloadUpdateAsync(string downloadUrl, string destinationPath, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fileStream, cancellationToken);
                return destinationPath;
            }
            catch (Exception ex)
            {
                UpdateError?.Invoke(this, ex.Message);
                return null;
            }
        }

        /// <summary>Prüft, ob eine Version neuer ist als die aktuelle.</summary>
        private static bool IsNewerVersion(string candidate, string current)
        {
            try
            {
                var c = Version.Parse(candidate);
                var cur = Version.Parse(current);
                return c > cur;
            }
            catch
            {
                return string.Compare(candidate, current, StringComparison.Ordinal) > 0;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _httpClient?.Dispose();
            }
        }
    }

    /// <summary>
    /// Update-Informationen vom Server.
    /// </summary>
    public class UpdatePackageInfo
    {
        public string Version { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string? Changelog { get; set; }
        public long FileSizeBytes { get; set; }
        public bool Mandatory { get; set; }
        public DateTime ReleasedAt { get; set; }
    }
}
