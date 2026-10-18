using System.IO;
using RagnaController.Core.Services;
using RagnaController.Models;
using Xunit;

namespace RagnaController.Tests
{
    /// <summary>
    /// TASK-1631: Unit-Tests für FirstRunService.
    /// Prüft First-Run-Erkennung, RoExePath-Speicherung und RO-Client-Validierung.
    /// </summary>
    public class FirstRunServiceTests
    {
        [Fact]
        public void IsFirstRun_ReturnsTrue_WhenRoExePathIsEmpty()
        {
            // Arrange
            var settings = new Settings { RoExePath = "" };
            var service = new FirstRunService(settings);

            // Act
            var result = service.IsFirstRun;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsFirstRun_ReturnsTrue_WhenRoExePathIsWhitespace()
        {
            // Arrange
            var settings = new Settings { RoExePath = "   " };
            var service = new FirstRunService(settings);

            // Act
            var result = service.IsFirstRun;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsFirstRun_ReturnsFalse_WhenRoExePathIsSet()
        {
            // Arrange
            var settings = new Settings { RoExePath = "C:\\Ragnarok Online\\ragexe.exe" };
            var service = new FirstRunService(settings);

            // Act
            var result = service.IsFirstRun;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CompleteFirstRun_SavesRoExePath_AndTrimsWhitespace()
        {
            // Arrange
            var settings = new Settings { RoExePath = "" };
            var service = new FirstRunService(settings);

            // Act
            service.CompleteFirstRun("  C:\\RO\\ragexe.exe  ");

            // Assert
            Assert.Equal("C:\\RO\\ragexe.exe", settings.RoExePath);
            Assert.False(service.IsFirstRun);
        }

        [Fact]
        public void CompleteFirstRun_Throws_WhenPathIsNull()
        {
            // Arrange
            var settings = new Settings { RoExePath = "" };
            var service = new FirstRunService(settings);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.CompleteFirstRun(""));
            Assert.Throws<ArgumentException>(() => service.CompleteFirstRun(null!));
        }

        [Theory]
        [InlineData("C:\\Ragnarok\\ragexe.exe")]
        [InlineData("C:\\Ragnarok\\sakexe.exe")]
        [InlineData("C:\\Ragnarok\\ragexe_5th.exe")]
        [InlineData("C:\\Ragnarok\\sakexe_5th.exe")]
        public void IsValidRoClient_ReturnsTrue_ForValidRoExecutables(string path)
        {
            // Arrange: Erstelle eine temporäre Datei mit dem gegebenen Namen
            var tempDir = Path.Combine(Path.GetTempPath(), "RagnaControllerTests");
            Directory.CreateDirectory(tempDir);
            var tempFile = Path.Combine(tempDir, Path.GetFileName(path));
            File.WriteAllBytes(tempFile, Array.Empty<byte>());

            try
            {
                // Act
                var result = FirstRunService.IsValidRoClient(tempFile);

                // Assert
                Assert.True(result);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Theory]
        [InlineData("notepad.exe")]
        [InlineData("chrome.exe")]
        [InlineData("")]
        [InlineData(null)]
        public void IsValidRoClient_ReturnsFalse_ForInvalidPaths(string? path)
        {
            // Act
            var result = FirstRunService.IsValidRoClient(path);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidRoClient_ReturnsFalse_ForNonexistentFile()
        {
            // Act
            var result = FirstRunService.IsValidRoClient("C:\\Nonexistent\\ragexe.exe");

            // Assert
            Assert.False(result);
        }
    }
}
