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

        // ── TASK-1705: DesignSystem-Integrationstests ──────────────────────

        [Fact]
        public void ApplyHighContrast_FullDesignSystem_SetsAllHighContrastColors()
        {
            // Arrange — simuliert UI2026DesignSystem mit allen relevanten Schlüsseln
            var settings = new Settings { HighContrastEnabled = true, FontScale = 1.0 };
            var designSystem = new ResourceDictionary
            {
                ["BgPrimary"] = Colors.White,
                ["BgSecondary"] = Colors.Gray,
                ["BgTertiary"] = Colors.DarkGray,
                ["BgBorder"] = Colors.Black,
                ["BgDeep"] = Colors.White,
                ["BgPanel"] = Colors.Gray,
                ["BgCard"] = Colors.DarkGray,
                ["BgRaised"] = Colors.LightGray,
                ["BorderHair"] = Colors.Black,
                ["BorderGlow"] = Colors.Black,
                ["TextPrimary"] = Colors.Black,
                ["TextSecondary"] = Colors.DarkGray,
                ["TextTertiary"] = Colors.Gray,
                ["AccentBlue"] = Colors.Blue,
                ["AccentBlueDim"] = Colors.DarkBlue,
                ["AccentPurple"] = Colors.Purple,
                ["AccentPurpleDim"] = Colors.DarkMagenta,
                ["Live"] = Colors.Green,
                ["Gold"] = Colors.Gold,
                ["GoldDim"] = Colors.DarkGoldenrod,
                ["Danger"] = Colors.Red,
            };
            var service = new HighContrastService(settings, designSystem);

            // Act
            service.ApplyHighContrast();

            // Assert — alle Farben sollten auf High-Contrast-Werte gesetzt sein
            Assert.Equal(Colors.Black, (Color)designSystem["BgPrimary"]!);
            Assert.Equal(Colors.White, (Color)designSystem["TextPrimary"]!);
            Assert.Equal(Color.FromRgb(0, 120, 215), (Color)designSystem["AccentBlue"]!); // #0078D7
            Assert.Equal(Color.FromRgb(191, 0, 255), (Color)designSystem["AccentPurple"]!); // #BF00FF
            Assert.Equal(Color.FromRgb(0, 255, 0), (Color)designSystem["Live"]!); // #00FF00
            Assert.Equal(Color.FromRgb(255, 0, 0), (Color)designSystem["Danger"]!); // #FF0000
        }

        [Fact]
        public void ApplyHighContrast_PartialDesignSystem_SetsOnlyPresentKeys()
        {
            // Arrange — nur einige Schlüssel vorhanden (wie in echtem partial DesignSystem)
            var settings = new Settings { HighContrastEnabled = true, FontScale = 1.0 };
            var designSystem = new ResourceDictionary
            {
                ["BgPrimary"] = Colors.White,
                ["TextPrimary"] = Colors.Black,
                // AccentBlue fehlt absichtlich
            };
            var service = new HighContrastService(settings, designSystem);

            // Act & Assert — sollte nicht werfen
            service.ApplyHighContrast();

            // Vorhandene Schlüssel geändert, fehlende ignoriert
            Assert.Equal(Colors.Black, (Color)designSystem["BgPrimary"]!);
            Assert.Equal(Colors.White, (Color)designSystem["TextPrimary"]!);
        }

        [Theory]
        [InlineData(0.85)]
        [InlineData(1.50)]
        public void ApplyFontScale_EdgeValues_UpdatesCorrectly(double scale)
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
        public void ApplyHighContrast_HighContrastMode_FontScaleKeysStillUpdated()
        {
            // Arrange — HighContrast aktiv UND FontScale != 1.0
            var settings = new Settings { HighContrastEnabled = true, FontScale = 1.25 };
            var designSystem = new ResourceDictionary
            {
                ["BgPrimary"] = Colors.White,
                ["FontScaleNormal"] = 1.0,
                ["FontScaleSmall"] = 0.85,
            };
            var service = new HighContrastService(settings, designSystem);

            // Act
            service.ApplyHighContrast();

            // Assert — sowohl Farben als auch Schrift-Skalierung angewendet
            Assert.Equal(Colors.Black, (Color)designSystem["BgPrimary"]!);
            Assert.Equal(1.25, (double)designSystem["FontScaleNormal"]!);
            Assert.Equal(Math.Round(1.25 * 0.85, 2), (double)designSystem["FontScaleSmall"]!);
        }

        [Fact]
        public void Constructor_ValidSettingsAndDesignSystem_DoesNotThrow()
        {
            // Arrange & Act & Assert
            var settings = new Settings();
            var designSystem = new ResourceDictionary();
            var service = new HighContrastService(settings, designSystem);
            Assert.NotNull(service);
        }
    }
}
