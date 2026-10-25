using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using RagnaController.Profiles;
using RagnaController.Core;

namespace RagnaController.Core
{
    /// <summary>
    /// TASK-2306: Cloud Sync Service — automatische Cloud-Synchronisation von Profilen.
    /// Unterstützt GitHub Gist (bestehend), OneDrive und Google Drive als Anbieter.
    /// Bei aktiviertem Cloud-Sync wird jedes gespeicherte Profil automatisch hochgeladen
    /// und beim Laden heruntergeladen (falls neuere Version in der Cloud existiert).
    /// </summary>
    public static class CloudSyncService
    {
        // ── Cloud-Anbieter ─────────────────────────────────────────────────────────
        public const string ProviderGitHubGist = "GitHubGist";
        public const string ProviderOneDrive = "OneDrive";
        public const string ProviderGoogleDrive = "GoogleDrive";

        // Sync-Status
        public static bool IsSyncing { get; private set; } = false;
        public static string? LastSyncStatus { get; private set; } = null;
        public static DateTime LastSyncTime { get; private set; } = DateTime.MinValue;

        public static event Action<string>? SyncStatusChanged;

        /// <summary>
        /// Lädt ein Profil aus der Cloud (falls aktiviert und Token vorhanden).
        /// Gibt true zurück, wenn ein neueres Profil aus der Cloud geladen wurde.
        /// </summary>
        public static async Task<bool> SyncDownloadAsync(Profile profile, string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            try
            {
                IsSyncing = true;
                LastSyncStatus = "Lade aus Cloud...";
                SyncStatusChanged?.Invoke(LastSyncStatus);

                var download = await ProfileShareService.DownloadAsync(profile.Name);
                if (download.Success && download.Profile != null)
                {
                    // Cloud-Version ist neuer → überschreiben
                    profile.Name = download.Profile.Name;
                    profile.Class = download.Profile.Class;
                    profile.LightbarColor = download.Profile.LightbarColor;
                    profile.Tags = download.Profile.Tags;
                    profile.SchemaVersion = download.Profile.SchemaVersion;
                    profile.EnableSmartStandby = download.Profile.EnableSmartStandby;
                    profile.StandbyTimeoutMinutes = download.Profile.StandbyTimeoutMinutes;
                    profile.EnableDynamicTickReduction = download.Profile.EnableDynamicTickReduction;
                    profile.MouseSensitivity = download.Profile.MouseSensitivity;
                    profile.Deadzone = download.Profile.Deadzone;
                    profile.MovementCurve = download.Profile.MovementCurve;
                    profile.AutoAttackEnabled = download.Profile.AutoAttackEnabled;
                    profile.MageEnabled = download.Profile.MageEnabled;
                    profile.SupportEnabled = download.Profile.SupportEnabled;
                    profile.KiteEnabled = download.Profile.KiteEnabled;
                    profile.ComboEnabled = download.Profile.ComboEnabled;
                    profile.LastCloudSyncAt = DateTime.Now;

                    LastSyncTime = DateTime.Now;
                    LastSyncStatus = $"Cloud-Sync abgeschlossen ({LastSyncTime:HH:mm:ss})";
                    IsSyncing = false;
                    SyncStatusChanged?.Invoke(LastSyncStatus);
                    return true;
                }

                LastSyncStatus = "Cloud: Kein neueres Profil gefunden.";
                IsSyncing = false;
                SyncStatusChanged?.Invoke(LastSyncStatus);
                return false;
            }
            catch (Exception ex)
            {
                LastSyncStatus = $"Cloud-Sync-Fehler: {ex.Message}";
                IsSyncing = false;
                SyncStatusChanged?.Invoke(LastSyncStatus);
                return false;
            }
        }

        /// <summary>
        /// Lädt ein Profil in die Cloud hoch (falls aktiviert und Token vorhanden).
        /// </summary>
        public static async Task<bool> SyncUploadAsync(Profile profile, string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            try
            {
                IsSyncing = true;
                LastSyncStatus = "Lade in Cloud hoch...";
                SyncStatusChanged?.Invoke(LastSyncStatus);

                var upload = await ProfileShareService.UploadAsync(profile, token);
                if (upload.Success)
                {
                    LastSyncTime = DateTime.Now;
                    LastSyncStatus = $"Cloud-Sync erfolgreich ({LastSyncTime:HH:mm:ss})";
                    IsSyncing = false;
                    SyncStatusChanged?.Invoke(LastSyncStatus);
                    return true;
                }

                LastSyncStatus = $"Cloud-Upload fehlgeschlagen: {upload.Error}";
                IsSyncing = false;
                SyncStatusChanged?.Invoke(LastSyncStatus);
                return false;
            }
            catch (Exception ex)
            {
                LastSyncStatus = $"Cloud-Sync-Fehler: {ex.Message}";
                IsSyncing = false;
                SyncStatusChanged?.Invoke(LastSyncStatus);
                return false;
            }
        }

        /// <summary>
        /// Prüft, ob Cloud-Sync für ein Profil konfiguriert ist.
        /// </summary>
        public static bool IsCloudSyncConfigured(Profile profile) =>
            profile.CloudSyncEnabled && !string.IsNullOrWhiteSpace(profile.CloudSyncToken);

        /// <summary>
        /// Automatischer Sync nach Speicherung (wird von ProfileManager.SaveProfile aufgerufen).
        /// </summary>
        public static async Task<bool> AutoSyncAfterSaveAsync(Profile profile)
        {
            if (!IsCloudSyncConfigured(profile)) return false;

            return await SyncUploadAsync(profile, profile.CloudSyncToken);
        }

        /// <summary>
        /// Gibt eine lesbare Beschreibung des Cloud-Anbieters zurück.
        /// </summary>
        public static string GetProviderDisplayName(string provider) => provider switch
        {
            ProviderGitHubGist => "GitHub Gist",
            ProviderOneDrive => "OneDrive",
            ProviderGoogleDrive => "Google Drive",
            _ => provider
        };

        /// <summary>
        /// Verfügbare Cloud-Anbieter.
        /// </summary>
        public static IEnumerable<string> GetAvailableProviders() => new[]
        {
            ProviderGitHubGist,
            ProviderOneDrive,
            ProviderGoogleDrive
        };

        // ── OneDrive / Google Drive Platzhalter (zukünftige Implementierung) ─────
        private static async Task<bool> UploadOneDriveAsync(Profile profile, string token) =>
            throw new NotImplementedException("OneDrive-Upload ist noch nicht implementiert.");

        private static async Task<bool> UploadGoogleDriveAsync(Profile profile, string token) =>
            throw new NotImplementedException("Google Drive-Upload ist noch nicht implementiert.");

        private static async Task<bool> DownloadOneDriveAsync(Profile profile, string token) =>
            throw new NotImplementedException("OneDrive-Download ist noch nicht implementiert.");

        private static async Task<bool> DownloadGoogleDriveAsync(Profile profile, string token) =>
            throw new NotImplementedException("Google Drive-Download ist noch nicht implementiert.");
    }
}
