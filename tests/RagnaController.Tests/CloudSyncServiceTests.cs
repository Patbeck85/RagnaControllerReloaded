using System;
using System.Collections.Generic;
using Xunit;
using RagnaController.Core;
using RagnaController.Profiles;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-022: CloudSyncService Unit Tests (TASK-2306).
    /// Deckt Cloud-Sync-Initialisierung, Provider-Anzeige, Sync-Status ab.
    /// </summary>
    public class CloudSyncServiceTests
    {
        #region Provider Anzeige

        [Fact]
        public void GetAvailableProviders_ReturnsThreeProviders()
        {
            // Act
            var providers = CloudSyncService.GetAvailableProviders();

            // Assert
            Assert.Equal(3, providers.Count());
            Assert.Contains("GitHubGist", providers);
            Assert.Contains("OneDrive", providers);
            Assert.Contains("GoogleDrive", providers);
        }

        [Fact]
        public void GetProviderDisplayName_ReturnsLocalizedNames()
        {
            // Arrange & Act & Assert
            Assert.Equal("GitHub Gist", CloudSyncService.GetProviderDisplayName("GitHubGist"));
            Assert.Equal("OneDrive", CloudSyncService.GetProviderDisplayName("OneDrive"));
            Assert.Equal("Google Drive", CloudSyncService.GetProviderDisplayName("GoogleDrive"));
            Assert.Equal("UnknownProvider", CloudSyncService.GetProviderDisplayName("UnknownProvider"));
        }

        #endregion

        #region IsCloudSyncConfigured

        [Fact]
        public void IsCloudSyncConfigured_Disabled_ReturnsFalse()
        {
            // Arrange
            var profile = new Profile
            {
                CloudSyncEnabled = false,
                CloudSyncToken = "token123"
            };

            // Act
            var result = CloudSyncService.IsCloudSyncConfigured(profile);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCloudSyncConfigured_NoToken_ReturnsFalse()
        {
            // Arrange
            var profile = new Profile
            {
                CloudSyncEnabled = true,
                CloudSyncToken = ""
            };

            // Act
            var result = CloudSyncService.IsCloudSyncConfigured(profile);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsCloudSyncConfigured_EnabledWithToken_ReturnsTrue()
        {
            // Arrange
            var profile = new Profile
            {
                CloudSyncEnabled = true,
                CloudSyncToken = "ghp_token123",
                CloudSyncProvider = "GitHubGist"
            };

            // Act
            var result = CloudSyncService.IsCloudSyncConfigured(profile);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region Sync Download (no network - should fail gracefully)

        [Fact]
        public async System.Threading.Tasks.Task SyncDownloadAsync_NoToken_ReturnsFalse()
        {
            // Arrange
            var profile = new Profile
            {
                Name = "TestProfile",
                CloudSyncEnabled = true,
                CloudSyncToken = ""
            };

            // Act
            var result = await CloudSyncService.SyncDownloadAsync(profile, "");

            // Assert
            Assert.False(result);
        }

        #endregion

        #region LastSyncStatus

        [Fact]
        public void LastSyncStatus_Initial_IsNull()
        {
            // Assert
            Assert.Null(CloudSyncService.LastSyncStatus);
        }

        [Fact]
        public void IsSyncing_Initial_IsFalse()
        {
            // Assert
            Assert.False(CloudSyncService.IsSyncing);
        }

        #endregion
    }
}
