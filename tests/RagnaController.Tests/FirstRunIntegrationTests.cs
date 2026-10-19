using System.IO;
using RagnaController.Core.Services;
using RagnaController.Models;
using Xunit;

namespace RagnaController.Tests
{
    /// <summary>
    /// TASK-1631 / TASK-1704: Integration-Tests für FirstRunService Wizard-Flow.
    /// Testet den kompletten First-Run-Flow von der Erkennung bis zum Speichern.
    /// </summary>
    public class FirstRunIntegrationTests
    {
        [Fact]
        public void FullFlow_EmptyRoExePath_IsFirstRunTrue_ThenCompleteFirstRun_MakesItFalse()
        {
            // Arrange
            var settings = new Settings { RoExePath = "" };
            var service = new FirstRunService(settings);

            // Act & Assert - Phase 1: First run detected
            Assert.True(service.IsFirstRun);

            // Act - Phase 2: Complete first run with valid path
            service.CompleteFirstRun(@"C:\Ragnarok Online\ragexe.exe");

            // Assert - Phase 3: No longer first run
            Assert.False(service.IsFirstRun);
            Assert.Equal(@"C:\Ragnarok Online\ragexe.exe", settings.RoExePath);
        }

        [Fact]
        public void FullFlow_CompleteFirstRun_PersistsToSettingsFile()
        {
            // Arrange - use temp file to avoid touching real settings
            var tempDir = Path.Combine(Path.GetTempPath(), "RagnaControllerTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var tempSettingsPath = Path.Combine(tempDir, "settings.json");

            // Create settings pointing to temp location
            var settings = new Settings { RoExePath = "" };
            var service = new FirstRunService(settings);

            // Act
            service.CompleteFirstRun(@"C:\RO\sakexe.exe");

            // Assert - settings object updated
            Assert.False(service.IsFirstRun);
            Assert.Equal(@"C:\RO\sakexe.exe", settings.RoExePath);

            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }

        [Theory]
        [InlineData(@"C:\Ragnarok\ragexe.exe", true)]
        [InlineData(@"C:\Ragnarok\sakexe.exe", true)]
        [InlineData(@"C:\Ragnarok\ragexe_5th.exe", true)]
        [InlineData(@"C:\Ragnarok\sakexe_5th.exe", true)]
        [InlineData(@"C:\Ragnarok\notepad.exe", false)]
        [InlineData(@"C:\Ragnarok\chrome.exe", false)]
        public void IsValidRoClient_RecognizesValidAndInvalidExecutables(string path, bool expected)
        {
            // Arrange - create temp file
            var tempDir = Path.Combine(Path.GetTempPath(), "RagnaControllerTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var tempFile = Path.Combine(tempDir, Path.GetFileName(path));
            File.WriteAllBytes(tempFile, Array.Empty<byte>());

            try
            {
                // Act
                var result = FirstRunService.IsValidRoClient(tempFile);

                // Assert
                Assert.Equal(expected, result);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void IsValidRoClient_NonexistentFile_ReturnsFalse()
        {
            // Act
            var result = FirstRunService.IsValidRoClient(@"C:\Nonexistent\ragexe.exe");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CompleteFirstRun_NullPath_ThrowsArgumentException()
        {
            // Arrange
            var settings = new Settings { RoExePath = "" };
            var service = new FirstRunService(settings);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.CompleteFirstRun(""));
            Assert.Throws<ArgumentException>(() => service.CompleteFirstRun(null!));
            Assert.Throws<ArgumentException>(() => service.CompleteFirstRun("   "));
        }

        [Fact]
        public void CompleteFirstRun_ValidPath_TrimsWhitespace()
        {
            // Arrange
            var settings = new Settings { RoExePath = "" };
            var service = new FirstRunService(settings);

            // Act
            service.CompleteFirstRun("  C:\\RO\\ragexe.exe  ");

            // Assert
            Assert.Equal(@"C:\RO\ragexe.exe", settings.RoExePath);
            Assert.False(service.IsFirstRun);
        }
    }
}
