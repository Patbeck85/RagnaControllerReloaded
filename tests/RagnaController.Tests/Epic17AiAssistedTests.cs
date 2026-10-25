using System;
using System.Collections.Generic;
using Xunit;
using RagnaController.Core;
using RagnaController.Core.Calculators;
using RagnaController.Core.AI;

namespace RagnaController.Tests
{
    /// <summary>
    /// TASK-2606: Unit-Tests für Epic 17 (AI-Assisted Features / White-Hat).
    /// Deckt SkillRecommendationEngine und AutoBuildOptimizer ab.
    /// </summary>
    public class Epic17AiAssistedTests
    {
        #region SkillRecommendationEngine Tests

        [Fact]
        public void SkillRecommendationEngine_RecommendSwordmanKnightPvm_ShouldReturnBuild()
        {
            // Arrange
            var engine = new SkillRecommendationEngine();

            // Act
            var recommendations = engine.Recommend("Swordman", "Knight", 50, 40, "PvM", 5);

            // Assert
            Assert.NotEmpty(recommendations);
            Assert.Contains(recommendations, r => r.Template.Id == "knight_pvm");
        }

        [Fact]
        public void SkillRecommendationEngine_RecommendSwordmanKnightPvp_ShouldReturnPvpBuild()
        {
            // Arrange
            var engine = new SkillRecommendationEngine();

            // Act
            var recommendations = engine.Recommend("Swordman", "Knight", 50, 40, "PvP", 5);

            // Assert
            Assert.NotEmpty(recommendations);
            Assert.Contains(recommendations, r => r.Template.Id == "knight_pvp");
        }

        [Fact]
        public void SkillRecommendationEngine_RecommendMagicianWizard_ShouldReturnWizardBuild()
        {
            // Arrange
            var engine = new SkillRecommendationEngine();

            // Act
            var recommendations = engine.Recommend("Magician", "Wizard", 70, 50, "PvM", 5);

            // Assert
            Assert.NotEmpty(recommendations);
            Assert.Contains(recommendations, r => r.Template.Id == "wizard_pvm");
        }

        [Fact]
        public void SkillRecommendationEngine_RecommendArcherHunter_ShouldReturnHunterBuild()
        {
            // Arrange
            var engine = new SkillRecommendationEngine();

            // Act
            var recommendations = engine.Recommend("Archer", "Hunter", 50, 40, "PvM", 5);

            // Assert
            Assert.NotEmpty(recommendations);
            Assert.Contains(recommendations, r => r.Template.Id == "hunter_pvm");
        }

        [Fact]
        public void SkillRecommendationEngine_RecommendAcolytePriestSupport_ShouldReturnPriestBuild()
        {
            // Arrange
            var engine = new SkillRecommendationEngine();

            // Act
            var recommendations = engine.Recommend("Acolyte", "Priest", 50, 40, "Support", 5);

            // Assert
            Assert.NotEmpty(recommendations);
            Assert.Contains(recommendations, r => r.Template.Id == "priest_support");
        }

        [Fact]
        public void SkillRecommendationEngine_UnknownClass_ShouldReturnGenericOrEmpty()
        {
            // Arrange
            var engine = new SkillRecommendationEngine();

            // Act
            var recommendations = engine.Recommend("UnknownClass", null, 1, 1, null, 5);

            // Assert - sollte keine Empfehlungen für unbekannte Klasse geben
            Assert.Empty(recommendations);
        }

        [Fact]
        public void SkillRecommendationEngine_LevelTooLow_ShouldFilterOutSkills()
        {
            // Arrange
            var engine = new SkillRecommendationEngine();

            // Act - Level 1 sollte keine Skills mit Mindestlevel 50 zurückgeben
            var recommendations = engine.Recommend("Magician", "Wizard", 1, 1, "PvM", 5);

            // Assert
            Assert.NotEmpty(recommendations);
            var wizardRec = Assert.Single(recommendations);
            // Meteor Storm erfordert BaseLevel 50, sollte nicht empfohlen werden
            Assert.DoesNotContain(wizardRec.RecommendedSkills, s => s.Name == "Meteor Storm");
        }

        [Fact]
        public void SkillRecommendationEngine_AddTemplate_ShouldBeIncluded()
        {
            // Arrange
            var engine = new SkillRecommendationEngine();
            var customTemplate = new SkillBuildTemplate
            {
                Id = "custom_test",
                BaseClass = "TestClass",
                JobClass = null,
                Playstyle = "Hybrid",
                MinBaseLevel = 1,
                MinJobLevel = 1,
                RecommendedBaseLevel = 1,
                RecommendedJobLevel = 1,
                Description = "Test",
                RecommendedSkills = new List<SkillRecommendationEntry>
                {
                    new() { Id = 999, Name = "Test Skill", MinBaseLevel = 1, MinJobLevel = 1, Priority = 1, MaxLevel = 1 },
                },
            };

            // Act
            engine.AddTemplate(customTemplate);
            var recommendations = engine.Recommend("TestClass", null, 1, 1, null, 5);

            // Assert
            Assert.Single(recommendations);
            Assert.Equal("custom_test", recommendations[0].Template.Id);
        }

        [Fact]
        public void SkillRecommendationEngine_RecommendationSortedByScore()
        {
            // Arrange
            var engine = new SkillRecommendationEngine();

            // Act
            var recommendations = engine.Recommend("Swordman", "Knight", 99, 50, "PvM", 5);

            // Assert
            for (int i = 1; i < recommendations.Count; i++)
            {
                Assert.True(recommendations[i - 1].MatchScore >= recommendations[i].MatchScore);
            }
        }

        #endregion

        #region AutoBuildOptimizer Tests

        [Fact]
        public void AutoBuildOptimizer_RecommendSwordmanPvm_ShouldReturnStats()
        {
            // Arrange
            var optimizer = new AutoBuildOptimizer();

            // Act
            var recommendations = optimizer.Recommend("Swordman", "PvM", 99, 3);

            // Assert
            Assert.NotEmpty(recommendations);
            Assert.Contains(recommendations, r => r.Template.Id == "knight_pvm");
        }

        [Fact]
        public void AutoBuildOptimizer_RecommendSwordmanPvp_ShouldReturnPvpStats()
        {
            // Arrange
            var optimizer = new AutoBuildOptimizer();

            // Act
            var recommendations = optimizer.Recommend("Swordman", "PvP", 99, 3);

            // Assert
            Assert.NotEmpty(recommendations);
            Assert.Contains(recommendations, r => r.Template.Id == "knight_pvp");
        }

        [Fact]
        public void AutoBuildOptimizer_RecommendWizardPvm_ShouldReturnIntFocusedStats()
        {
            // Arrange
            var optimizer = new AutoBuildOptimizer();

            // Act
            var recommendations = optimizer.Recommend("Magician", "PvM", 99, 3);

            // Assert
            Assert.NotEmpty(recommendations);
            var wizardRec = Assert.Single(recommendations);
            Assert.True(wizardRec.SuggestedStats.Int > wizardRec.SuggestedStats.Str);
        }

        [Fact]
        public void AutoBuildOptimizer_RecommendHunterPvm_ShouldReturnDexAgiStats()
        {
            // Arrange
            var optimizer = new AutoBuildOptimizer();

            // Act
            var recommendations = optimizer.Recommend("Archer", "PvM", 99, 3);

            // Assert
            Assert.NotEmpty(recommendations);
            var hunterRec = Assert.Single(recommendations);
            Assert.True(hunterRec.SuggestedStats.Dex > 0);
            Assert.True(hunterRec.SuggestedStats.Agi > 0);
        }

        [Fact]
        public void AutoBuildOptimizer_RecommendPriestSupport_ShouldReturnIntVitStats()
        {
            // Arrange
            var optimizer = new AutoBuildOptimizer();

            // Act
            var recommendations = optimizer.Recommend("Acolyte", "Support", 99, 3);

            // Assert
            Assert.NotEmpty(recommendations);
            var priestRec = Assert.Single(recommendations);
            Assert.True(priestRec.SuggestedStats.Int > 0);
            Assert.True(priestRec.SuggestedStats.Vit > 0);
        }

        [Fact]
        public void AutoBuildOptimizer_UnknownClass_ShouldReturnEmpty()
        {
            // Arrange
            var optimizer = new AutoBuildOptimizer();

            // Act
            var recommendations = optimizer.Recommend("UnknownClass", null, 99, 3);

            // Assert
            Assert.Empty(recommendations);
        }

        [Fact]
        public void AutoBuildOptimizer_TotalPointsShouldNotExceedTarget()
        {
            // Arrange
            var optimizer = new AutoBuildOptimizer();

            // Act
            var recommendations = optimizer.Recommend("Swordman", "PvM", 50, 3);

            // Assert
            Assert.NotEmpty(recommendations);
            var rec = recommendations[0];
            // Bei Level 50 sollten es ca. 49 Punkte sein (1 Punkt pro Level über 1)
            Assert.True(rec.SuggestedStats.TotalPoints <= 49);
        }

        [Fact]
        public void AutoBuildOptimizer_AddTemplate_ShouldBeIncluded()
        {
            // Arrange
            var optimizer = new AutoBuildOptimizer();
            var customTemplate = new StatBuildTemplate
            {
                Id = "custom_test",
                BaseClass = "TestClass",
                Playstyle = "Hybrid",
                Description = "Test",
                StrWeight = 50f,
                AgiWeight = 50f,
                VitWeight = 0f,
                IntWeight = 0f,
                DexWeight = 0f,
                LukWeight = 0f,
            };

            // Act
            optimizer.AddTemplate(customTemplate);
            var recommendations = optimizer.Recommend("TestClass", null, 99, 3);

            // Assert
            Assert.Single(recommendations);
            Assert.Equal("custom_test", recommendations[0].Template.Id);
        }

        #endregion
    }
}
