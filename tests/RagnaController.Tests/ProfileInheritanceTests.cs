using System;
using System.Collections.Generic;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-020: Profile Inheritance Unit Tests (TASK-2301).
    /// Deckt BaseProfile → ClassProfile → CharProfile Vererbung,
    /// Override-Mechanik (ResolveOverride, Effective-Properties) ab.
    /// </summary>
    public class ProfileInheritanceTests
    {
        #region BaseProfile

        [Fact]
        public void BaseProfile_HasDefaultValues()
        {
            // Arrange & Act
            var profile = new TestBaseProfile();

            // Assert
            Assert.Equal("New Profile", profile.Name);
            Assert.Equal("Melee", profile.Class);
            Assert.Equal("#005AD2", profile.LightbarColor);
            Assert.Equal(Settings.OverlayThemeType.Neon, profile.OverlayTheme);
            Assert.True(profile.IsEnabled);
            Assert.Equal(0, profile.Priority);
        }

        #endregion

        #region ClassProfile

        [Fact]
        public void ClassProfile_InheritsBaseProfile()
        {
            // Arrange & Act
            var classProfile = new ClassProfile
            {
                RoClass = "Mage",
                Name = "Mage Class",
                LightbarColor = "#FF0000",
            };

            // Assert
            Assert.Equal("Mage", classProfile.RoClass);
            Assert.Equal("Mage Class", classProfile.Name);
            Assert.Equal("#FF0000", classProfile.LightbarColor);
            Assert.Equal(Settings.OverlayThemeType.Neon, classProfile.OverlayTheme); // inherited default
        }

        [Fact]
        public void ClassProfile_CreateCharProfile_ReturnsCharProfile()
        {
            // Arrange
            var classProfile = new ClassProfile
            {
                RoClass = "Archer",
                LightbarColor = "#00FF00",
            };

            // Act
            var charProfile = classProfile.CreateCharProfile("MeinBogenschütze");

            // Assert
            Assert.NotNull(charProfile);
            Assert.Equal("MeinBogenschütze", charProfile.CharName);
            Assert.Equal("MeinBogenschütze", charProfile.Name);
            Assert.Equal("Archer", charProfile.Class);
            Assert.Same(classProfile, charProfile.BaseProfileRef);
        }

        #endregion

        #region CharProfile Override Resolution

        [Fact]
        public void CharProfile_EffectiveLightbarColor_UsesCharValueWhenSet()
        {
            // Arrange
            var classProfile = new ClassProfile { RoClass = "Mage", LightbarColor = "#FF0000" };
            var charProfile = classProfile.CreateCharProfile("TestChar");
            charProfile.LightbarColor = "#0000FF";
            charProfile.BaseProfileRef = classProfile;

            // Act & Assert
            Assert.Equal("#0000FF", charProfile.EffectiveLightbarColor);
        }

        [Fact]
        public void CharProfile_EffectiveLightbarColor_FallsBackToClassProfile()
        {
            // Arrange
            var classProfile = new ClassProfile { RoClass = "Mage", LightbarColor = "#FF0000" };
            var charProfile = classProfile.CreateCharProfile("TestChar");
            charProfile.LightbarColor = ""; // not set → fall back
            charProfile.BaseProfileRef = classProfile;

            // Act & Assert
            Assert.Equal("#FF0000", charProfile.EffectiveLightbarColor);
        }

        [Fact]
        public void CharProfile_EffectiveLightbarColor_FallsBackToBaseDefault()
        {
            // Arrange
            var classProfile = new ClassProfile { RoClass = "Mage", LightbarColor = "" };
            var charProfile = classProfile.CreateCharProfile("TestChar");
            charProfile.LightbarColor = ""; // not set
            charProfile.BaseProfileRef = classProfile;

            // Act & Assert
            Assert.Equal("#005AD2", charProfile.EffectiveLightbarColor); // BaseProfile default
        }

        [Fact]
        public void CharProfile_EffectiveClass_UsesCharClassWhenSet()
        {
            // Arrange
            var classProfile = new ClassProfile { RoClass = "Mage" };
            var charProfile = classProfile.CreateCharProfile("TestChar");
            charProfile.Class = "Bishop";
            charProfile.BaseProfileRef = classProfile;

            // Act & Assert
            Assert.Equal("Bishop", charProfile.EffectiveClass);
        }

        [Fact]
        public void CharProfile_EffectiveClass_FallsBackToRoClass()
        {
            // Arrange
            var classProfile = new ClassProfile { RoClass = "Archer" };
            var charProfile = classProfile.CreateCharProfile("TestChar");
            charProfile.Class = ""; // not set
            charProfile.BaseProfileRef = classProfile;

            // Act & Assert
            Assert.Equal("Archer", charProfile.EffectiveClass);
        }

        [Fact]
        public void CharProfile_EffectiveOverlayTheme_UsesCharValueWhenSet()
        {
            // Arrange
            var classProfile = new ClassProfile { RoClass = "Mage" };
            var charProfile = classProfile.CreateCharProfile("TestChar");
            charProfile.OverlayTheme = Settings.OverlayThemeType.Dark;
            charProfile.BaseProfileRef = classProfile;

            // Act & Assert
            Assert.Equal(Settings.OverlayThemeType.Dark, charProfile.EffectiveOverlayTheme);
        }

        [Fact]
        public void CharProfile_EffectiveOverlayTheme_FallsBackToBaseDefault()
        {
            // Arrange
            var classProfile = new ClassProfile { RoClass = "Mage" };
            var charProfile = classProfile.CreateCharProfile("TestChar");
            charProfile.OverlayTheme = Settings.OverlayThemeType.Neon; // default = not set
            charProfile.BaseProfileRef = classProfile;

            // Act & Assert
            Assert.Equal(Settings.OverlayThemeType.Neon, charProfile.EffectiveOverlayTheme);
        }

        #endregion

        #region ResolveOverride Struct

        [Fact]
        public void ResolveOverride_Int_CharValueWins()
        {
            // Act
            var result = CharProfile.ResolveOverride<int>(42, 10, 0);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public void ResolveOverride_Int_ClassValueWinsWhenCharDefault()
        {
            // Act
            var result = CharProfile.ResolveOverride<int>(0, 10, 0);

            // Assert
            Assert.Equal(10, result);
        }

        [Fact]
        public void ResolveOverride_Int_BaseValueWinsWhenBothDefault()
        {
            // Act
            var result = CharProfile.ResolveOverride<int>(0, 0, 99);

            // Assert
            Assert.Equal(99, result);
        }

        [Fact]
        public void ResolveOverride_String_CharValueWins()
        {
            // Act
            var result = CharProfile.ResolveOverride("char", "class", "base");

            // Assert
            Assert.Equal("char", result);
        }

        [Fact]
        public void ResolveOverride_String_ClassValueWinsWhenCharEmpty()
        {
            // Act
            var result = CharProfile.ResolveOverride("", "class", "base");

            // Assert
            Assert.Equal("class", result);
        }

        [Fact]
        public void ResolveOverride_String_BaseValueWinsWhenBothEmpty()
        {
            // Act
            var result = CharProfile.ResolveOverride("", "", "base");

            // Assert
            Assert.Equal("base", result);
        }

        #endregion
    }

    /// <summary>
    /// Concrete implementation of BaseProfile for testing (BaseProfile is abstract).
    /// </summary>
    public class TestBaseProfile : BaseProfile { }
}
