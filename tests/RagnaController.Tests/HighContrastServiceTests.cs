using System.Windows;
using System.Windows.Media;
using RagnaController.Core.Services;
using RagnaController.Models;
using Xunit;

namespace RagnaController.Tests
{
    /// <summary>
    /// TASK-1632: Unit-Tests für HighContrastService.
    /// Prüft High-Contrast-Farbanwendung und FontScale-Skalierung.
    /// </summary>
    public class HighContrastServiceTests
    {
        [Fact]
        public void ApplyHighContrast_NormalMode_DoesNotThrow()
        {
            // Arrange
            var settings = new Settings { HighContrastEnabled = false, FontScale = 1.0 };
            var designSystem = new ResourceDictionary();
            designSystem["TestColor"] = Colors.Red;
            var service = new HighContrastService(settings, designSystem);

            // Act & Assert — sollte nicht werfen
            service.ApplyHighContrast();
        }

        [Fact]
        public void ApplyHighContrast_HighContrastMode_SetsBlackBackground()
        {
            // Arrange
            var settings = new Settings { HighContrastEnabled = true, FontScale = 1.0 };
            var designSystem = new ResourceDictionary();
            designSystem["BgPrimary"] = Colors.White; // vorher normal
            var service = new HighContrastService(settings, designSystem);

            // Act
            service.ApplyHighContrast();

            // Assert
            Assert.Equal(Colors.Black, (Color)designSystem["BgPrimary"]!);
        }

        [Fact]
        public void ApplyHighContrast_HighContrastMode_SetsWhiteText()
        {
            // Arrange
            var settings = new Settings { HighContrastEnabled = true, FontScale = 1.0 };
            var designSystem = new ResourceDictionary();
            designSystem["TextPrimary"] = Colors.Black;
            var service = new HighContrastService(settings, designSystem);

            // Act
            service.ApplyHighContrast();

            // Assert
            Assert.Equal(Colors.White, (Color)designSystem["TextPrimary"]!);
        }

        [Fact]
        public void ApplyHighContrast_NormalMode_LeavesColorsUnchanged()
        {
            // Arrange
            var settings = new Settings { HighContrastEnabled = false, FontScale = 1.0 };
            var designSystem = new ResourceDictionary();
            designSystem["BgPrimary"] = Colors.White;
            var service = new HighContrastService(settings, designSystem);

            // Act
            service.ApplyHighContrast();

            // Assert — Normal mode doesn't change colors (they come from XAML)
            Assert.Equal(Colors.White, (Color)designSystem["BgPrimary"]!);
        }

        [Theory]
        [InlineData(0.85)]
        [InlineData(1.0)]
        [InlineData(1.25)]
        [InlineData(1.50)]
        public void ApplyFontScale_UpdatesFontScaleKeys(double scale)
        {
            // Arrange
            var settings = new Settings { HighContrastEnabled = false, FontScale = scale };
            var designSystem = new ResourceDictionary
            {
                ["FontScaleNormal"] = 1.0,
                ["FontScaleSmall"] = 0.85,
                ["FontScaleLarge"] = 1.25,
                ["FontScaleXLarge"] = 1.50
            };
            var service = new HighContrastService(settings, designSystem);

            // Act
            service.ApplyHighContrast();

            // Assert
            Assert.Equal(scale, (double)designSystem["FontScaleNormal"]!);
            Assert.Equal(Math.Round(scale * 0.85, 2), (double)designSystem["FontScaleSmall"]!);
            Assert.Equal(Math.Round(scale * 1.25, 2), (double)designSystem["FontScaleLarge"]!);
            Assert.Equal(Math.Round(scale * 1.50, 2), (double)designSystem["FontScaleXLarge"]!);
        }

        [Fact]
        public void Constructor_NullSettings_ThrowsArgumentNullException()
        {
            // Arrange
            var designSystem = new ResourceDictionary();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new HighContrastService(null!, designSystem));
        }

        [Fact]
        public void Constructor_NullDesignSystem_ThrowsArgumentNullException()
        {
            // Arrange
            var settings = new Settings();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new HighContrastService(settings, null!));
        }

        [Fact]
        public void ApplyHighContrast_MissingColorKey_DoesNotThrow()
        {
            // Arrange — DesignSystem ohne irgendwelche Farbschlüssel
            var settings = new Settings { HighContrastEnabled = true, FontScale = 1.0 };
            var designSystem = new ResourceDictionary();
            var service = new HighContrastService(settings, designSystem);

            // Act & Assert — sollte nicht werfen, auch wenn Schlüssel fehlen
            service.ApplyHighContrast();
        }
    }
}
