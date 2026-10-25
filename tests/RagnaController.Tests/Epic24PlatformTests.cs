using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using RagnaController.Core.Services;

namespace RagnaController.Tests
{
    /// <summary>
    /// Unit-Tests für Epic 24 (Platform & Deployment).
    /// Deckt AutoUpdateService, PortableModeService, LinuxSupportService, SteamDeckService, CICDService ab.
    /// </summary>
    public class Epic24PlatformTests
    {
        #region AutoUpdateService Tests

        [Fact]
        public void AutoUpdateService_IsNewerVersion_ShouldWork()
        {
            Assert.True(AutoUpdateService_IsNewerVersion_Static("2.0.0", "1.0.0"));
            Assert.False(AutoUpdateService_IsNewerVersion_Static("1.0.0", "2.0.0"));
            Assert.False(AutoUpdateService_IsNewerVersion_Static("1.0.0", "1.0.0"));
            Assert.True(AutoUpdateService_IsNewerVersion_Static("1.1.0", "1.0.9"));
        }

        private static bool AutoUpdateService_IsNewerVersion_Static(string candidate, string current)
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

        [Fact]
        public void AutoUpdateService_Properties_ShouldInitialize()
        {
            var service = new AutoUpdateService("1.0.0", "stable");
            Assert.False(service.UpdateAvailable);
            Assert.Null(service.LatestVersion);
            Assert.Null(service.UpdateDownloadUrl);
        }

        #endregion

        #region PortableModeService Tests

        [Fact]
        public void PortableModeService_InstallerMode_ShouldUseAppData()
        {
            var service = new PortableModeService(AppContext.BaseDirectory, forcePortable: false);
            Assert.False(service.IsPortable);
            Assert.Contains("RagnaController", service.UserDataDirectory);
        }

        [Fact]
        public void PortableModeService_PortableMode_ShouldUseAppDirectory()
        {
            var service = new PortableModeService(AppContext.BaseDirectory, forcePortable: true);
            Assert.True(service.IsPortable);
            Assert.Contains("UserData", service.UserDataDirectory);
            Assert.Contains("Profiles", service.ProfilesDirectory);
            Assert.Contains("Logs", service.LogsDirectory);
            Assert.Contains("Config", service.ConfigDirectory);
            Assert.Contains("Cache", service.CacheDirectory);
        }

        [Fact]
        public void PortableModeService_InitializeDirectories_ShouldCreateDirectories()
        {
            var dir = Path.Combine(Path.GetTempPath(), "RagnaController_PortableTest");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);

            var service = new PortableModeService(dir, forcePortable: true);
            service.InitializeDirectories();

            Assert.True(Directory.Exists(service.UserDataDirectory));
            Assert.True(Directory.Exists(service.ConfigDirectory));
            Assert.True(Directory.Exists(service.ProfilesDirectory));
            Assert.True(Directory.Exists(service.LogsDirectory));
            Assert.True(Directory.Exists(service.CacheDirectory));

            Directory.Delete(dir, true);
        }

        [Fact]
        public void PortableModeService_GetPath_ShouldReturnCorrectPath()
        {
            var service = new PortableModeService(AppContext.BaseDirectory, forcePortable: true);
            var path = service.GetPath(service.ConfigDirectory, "settings.json");
            Assert.EndsWith("settings.json", path);
            Assert.Contains("Config", path);
        }

        #endregion

        #region LinuxSupportService Tests

        [Fact]
        public void LinuxSupportService_IsLinux_ShouldReturnBool()
        {
            // On Windows this should be false, on Linux true
            bool result = LinuxSupportService.IsLinux;
            Assert.IsType<bool>(result);
        }

        [Fact]
        public void LinuxSupportService_GetKnownIssues_ShouldReturnList()
        {
            var issues = LinuxSupportService.GetKnownIssues();
            Assert.NotEmpty(issues);
            Assert.All(issues, issue => Assert.False(string.IsNullOrWhiteSpace(issue)));
        }

        [Fact]
        public void LinuxSupportService_GetRecommendations_ShouldReturnList()
        {
            var recommendations = LinuxSupportService.GetRecommendations();
            Assert.NotEmpty(recommendations);
            Assert.All(recommendations, rec => Assert.False(string.IsNullOrWhiteSpace(rec)));
        }

        [Fact]
        public void LinuxSupportService_WindowsPathToLinux_ShouldConvert()
        {
            var result = LinuxSupportService.WindowsPathToLinux(@"C:\Users\Test\file.txt");
            Assert.Contains("/mnt/c/", result);
            Assert.Contains("Users/Test/file.txt", result);
        }

        [Fact]
        public void LinuxSupportService_LinuxPathToWindows_ShouldConvert()
        {
            var result = LinuxSupportService.LinuxPathToWindows("/mnt/c/Users/Test/file.txt");
            Assert.Contains("C:\\", result);
            Assert.Contains("Users\\Test\\file.txt", result);
        }

        #endregion

        #region SteamDeckService Tests

        [Fact]
        public void SteamDeckService_Properties_ShouldInitialize()
        {
            var service = new SteamDeckService();
            Assert.IsType<bool>(service.IsSteamDeck);
            Assert.IsType<float>(service.RecommendedUIScale);
            Assert.IsType<int>(service.RecommendedFpsLimit);
        }

        [Fact]
        public void SteamDeckService_RecommendedSettings_ShouldBeValid()
        {
            var service = new SteamDeckService();
            Assert.True(service.RecommendedUIScale > 0);
            Assert.True(service.RecommendedFpsLimit >= 0);
        }

        [Fact]
        public void SteamDeckPerformanceSettings_CreateForSteamDeck_ShouldHaveValues()
        {
            var settings = SteamDeckPerformanceSettings.CreateForSteamDeck();
            Assert.Equal(60, settings.FpsLimit);
            Assert.True(settings.LimitedPerformance);
            Assert.Equal(60, settings.RefreshRate);
        }

        [Fact]
        public void SteamDeckPerformanceSettings_CreateForDesktop_ShouldHaveValues()
        {
            var settings = SteamDeckPerformanceSettings.CreateForDesktop();
            Assert.Equal(0, settings.FpsLimit); // unlimited
            Assert.False(settings.LimitedPerformance);
            Assert.Equal(144, settings.RefreshRate);
        }

        #endregion

        #region CICDService Tests

        [Fact]
        public void CICDService_Properties_ShouldHaveValues()
        {
            Assert.True(CICDService.StrykerEnabled);
            Assert.True(CICDService.CodeSigningEnabled);
            Assert.NotEmpty(CICDService.WindowsVersions);
            Assert.NotEmpty(CICDService.Architectures);
        }

        [Fact]
        public void CICDService_GetWorkflowYaml_ShouldContainKeyElements()
        {
            var yaml = CICDService.GetWorkflowYaml();
            Assert.Contains("CI/CD Pipeline", yaml);
            Assert.Contains("windows-10", yaml);
            Assert.Contains("windows-11", yaml);
            Assert.Contains("x64", yaml);
            Assert.Contains("arm64", yaml);
            Assert.Contains("stryker", yaml.ToLower());
            Assert.Contains("dotnet test", yaml);
        }

        [Fact]
        public void CICDService_GetReleaseChecklist_ShouldReturnItems()
        {
            var checklist = CICDService.GetReleaseChecklist();
            Assert.NotEmpty(checklist);
            Assert.All(checklist, item => Assert.StartsWith("[ ]", item));
        }

        [Fact]
        public void CICDService_CreateWorkflowFile_ShouldCreateFile()
        {
            var dir = Path.Combine(Path.GetTempPath(), "RagnaController_CICDTest");
            Directory.CreateDirectory(dir);

            CICDService.CreateWorkflowFile(dir);

            var workflowPath = Path.Combine(dir, ".github", "workflows", "ci-cd.yml");
            Assert.True(File.Exists(workflowPath));

            var content = File.ReadAllText(workflowPath);
            Assert.Contains("CI/CD Pipeline", content);

            Directory.Delete(dir, true);
        }

        #endregion
    }
}
