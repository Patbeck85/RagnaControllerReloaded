using System;
using System.Collections.Generic;
using Xunit;
using RagnaController.Core;
using RagnaController.Core.Community;

namespace RagnaController.Tests
{
    /// <summary>
    /// TASK-2706: Unit-Tests für Epic 18 (Community & Social Features).
    /// Deckt PartyMember, PartyOverlayService, ProfileRating, ProfileReviewService ab.
    /// </summary>
    public class Epic18CommunityTests
    {
        #region PartyMember Tests

        [Fact]
        public void PartyMember_AddBuff_ShouldAddBuff()
        {
            // Arrange
            var member = new PartyMember { Name = "Test", HpPercent = 100 };

            // Act
            member.AddBuff("Blessing");

            // Assert
            Assert.Contains("Blessing", member.Buffs);
        }

        [Fact]
        public void PartyMember_AddDuplicateBuff_ShouldNotDuplicate()
        {
            // Arrange
            var member = new PartyMember { Name = "Test" };
            member.AddBuff("Blessing");

            // Act
            member.AddBuff("Blessing");

            // Assert
            Assert.Single(member.Buffs);
        }

        [Fact]
        public void PartyMember_RemoveBuff_ShouldRemoveBuff()
        {
            // Arrange
            var member = new PartyMember { Name = "Test" };
            member.AddBuff("Blessing");

            // Act
            member.RemoveBuff("Blessing");

            // Assert
            Assert.DoesNotContain("Blessing", member.Buffs);
        }

        [Fact]
        public void PartyMember_AddDebuff_ShouldAddDebuff()
        {
            // Arrange
            var member = new PartyMember { Name = "Test" };

            // Act
            member.AddDebuff("Stun");

            // Assert
            Assert.Contains("Stun", member.Debuffs);
        }

        [Fact]
        public void PartyMember_Clone_ShouldCreateCopy()
        {
            // Arrange
            var member = new PartyMember
            {
                Name = "Test",
                ClassName = "Knight",
                HpPercent = 50,
                SpPercent = 30,
                IsDead = true,
                Distance = 5,
            };
            member.AddBuff("Blessing");
            member.AddDebuff("Stun");

            // Act
            var clone = member.Clone();

            // Assert
            Assert.Equal("Test", clone.Name);
            Assert.Equal("Knight", clone.ClassName);
            Assert.Equal(50, clone.HpPercent);
            Assert.Equal(30, clone.SpPercent);
            Assert.True(clone.IsDead);
            Assert.Equal(5, clone.Distance);
            Assert.Contains("Blessing", clone.Buffs);
            Assert.Contains("Stun", clone.Debuffs);
        }

        [Fact]
        public void PartyMember_HpPercent_ShouldClamp()
        {
            // Arrange
            var member = new PartyMember { Name = "Test" };

            // Act
            member.HpPercent = 150;
            member.SpPercent = -10;

            // Assert
            Assert.Equal(100, member.HpPercent);
            Assert.Equal(0, member.SpPercent);
        }

        #endregion

        #region PartyOverlayService Tests

        [Fact]
        public void PartyOverlayService_UpsertMember_ShouldAddMember()
        {
            // Arrange
            var service = new PartyOverlayService();

            // Act
            service.UpsertMember("Alice", "Knight", 80, 60, true, false, 2, "prontera");

            // Assert
            Assert.Equal(2, service.Count);
            var member = service.GetMember("Alice");
            Assert.NotNull(member);
            Assert.Equal("Knight", member?.ClassName);
            Assert.Equal(80, member?.HpPercent);
            Assert.Equal(60, member?.SpPercent);
        }

        [Fact]
        public void PartyOverlayService_UpsertMember_ShouldUpdateExisting()
        {
            // Arrange
            var service = new PartyOverlayService();
            service.UpsertMember("Alice", "Knight", 80, 60, true, false, 2, "prontera");

            // Act
            service.UpsertMember("Alice", "Knight", 30, 10, true, false, 5, "pay_fild01");

            // Assert
            Assert.Equal(2, service.Count); // Self + Alice
            var member = service.GetMember("Alice");
            Assert.Equal(30, member?.HpPercent);
            Assert.Equal(10, member?.SpPercent);
            Assert.Equal("pay_fild01", member?.Map);
        }

        [Fact]
        public void PartyOverlayService_RemoveMember_ShouldRemoveMember()
        {
            // Arrange
            var service = new PartyOverlayService();
            service.UpsertMember("Alice", "Knight", 80, 60, true, false);

            // Act
            var removed = service.RemoveMember("Alice");

            // Assert
            Assert.True(removed);
            Assert.Equal(1, service.Count); // Nur Self übrig
            Assert.Null(service.GetMember("Alice"));
        }

        [Fact]
        public void PartyOverlayService_RemoveSelf_ShouldNotRemove()
        {
            // Arrange
            var service = new PartyOverlayService();

            // Act
            var removed = service.RemoveMember("Self");

            // Assert
            Assert.False(removed);
            Assert.Equal(1, service.Count);
        }

        [Fact]
        public void PartyOverlayService_GetWeakestMember_ShouldReturnLowestHp()
        {
            // Arrange
            var service = new PartyOverlayService();
            service.UpsertMember("Alice", "Knight", 80, 60, true, false);
            service.UpsertMember("Bob", "Wizard", 30, 90, true, false);
            service.UpsertMember("Charlie", "Priest", 50, 40, true, false);

            // Act
            var weakest = service.GetWeakestMember();

            // Assert
            Assert.NotNull(weakest);
            Assert.Equal("Bob", weakest?.Name);
            Assert.Equal(30, weakest?.HpPercent);
        }

        [Fact]
        public void PartyOverlayService_GetDeadMembers_ShouldReturnDead()
        {
            // Arrange
            var service = new PartyOverlayService();
            service.UpsertMember("Alice", "Knight", 80, 60, true, false);
            service.UpsertMember("Bob", "Wizard", 0, 0, true, true);
            service.UpsertMember("Charlie", "Priest", 50, 40, true, false);

            // Act
            var dead = service.GetDeadMembers();

            // Assert
            Assert.Single(dead);
            Assert.Equal("Bob", dead[0].Name);
        }

        [Fact]
        public void PartyOverlayService_GetLowHpMembers_ShouldReturnBelowThreshold()
        {
            // Arrange
            var service = new PartyOverlayService();
            service.UpsertMember("Alice", "Knight", 80, 60, true, false);
            service.UpsertMember("Bob", "Wizard", 30, 90, true, false);
            service.UpsertMember("Charlie", "Priest", 45, 40, true, false);

            // Act
            var low = service.GetLowHpMembers(50);

            // Assert
            Assert.Equal(2, low.Count);
        }

        [Fact]
        public void PartyOverlayService_ClearParty_ShouldRemoveAllExceptSelf()
        {
            // Arrange
            var service = new PartyOverlayService();
            service.UpsertMember("Alice", "Knight", 80, 60, true, false);
            service.UpsertMember("Bob", "Wizard", 30, 90, true, false);

            // Act
            service.ClearParty();

            // Assert
            Assert.Equal(1, service.Count);
            Assert.Null(service.GetMember("Alice"));
            Assert.Null(service.GetMember("Bob"));
            Assert.NotNull(service.GetMember("Self"));
        }

        #endregion

        #region ProfileRating Tests

        [Fact]
        public void ProfileRating_Create_ShouldSetDefaults()
        {
            // Act
            var rating = new ProfileRating("profile1", "Reviewer1", 5, "Great profile!");

            // Assert
            Assert.Equal("profile1", rating.ProfileId);
            Assert.Equal("Reviewer1", rating.ReviewerName);
            Assert.Equal(5, rating.Stars);
            Assert.Equal("Great profile!", rating.Comment);
            Assert.False(rating.IsVerifiedPurchase);
        }

        [Fact]
        public void ProfileRating_Stars_ShouldClamp()
        {
            // Act
            var rating = new ProfileRating("p1", "r1", 10);
            var rating2 = new ProfileRating("p1", "r1", 0);

            // Assert
            Assert.Equal(5, rating.Stars);
            Assert.Equal(1, rating2.Stars);
        }

        [Fact]
        public void ProfileRating_AverageStars_ShouldCalculate()
        {
            // Arrange
            var ratings = new List<ProfileRating>
            {
                new() { Stars = 5 },
                new() { Stars = 4 },
                new() { Stars = 3 },
            };

            // Act
            var avg = ProfileRating.AverageStars(ratings);

            // Assert
            Assert.Equal(4.0, avg);
        }

        [Fact]
        public void ProfileRating_AverageStars_Empty_ShouldReturnZero()
        {
            // Act
            var avg = ProfileRating.AverageStars(new List<ProfileRating>());

            // Assert
            Assert.Equal(0.0, avg);
        }

        [Fact]
        public void ProfileRating_StarDistribution_ShouldCount()
        {
            // Arrange
            var ratings = new List<ProfileRating>
            {
                new() { Stars = 5 },
                new() { Stars = 5 },
                new() { Stars = 3 },
                new() { Stars = 1 },
            };

            // Act
            var dist = ProfileRating.StarDistribution(ratings);

            // Assert
            Assert.Equal(1, dist[0]); // 1 Stern
            Assert.Equal(0, dist[1]); // 2 Sterne
            Assert.Equal(1, dist[2]); // 3 Sterne
            Assert.Equal(0, dist[3]); // 4 Sterne
            Assert.Equal(2, dist[4]); // 5 Sterne
        }

        #endregion

        #region ProfileReviewService Tests

        [Fact]
        public void ProfileReviewService_AddRating_ShouldAdd()
        {
            // Arrange
            var service = new ProfileReviewService();

            // Act
            var result = service.AddOrUpdateRating("profile1", "Reviewer1", "rev1", 5, "Great!");

            // Assert
            Assert.True(result);
            Assert.Equal(1, service.GetRatingCount("profile1"));
        }

        [Fact]
        public void ProfileReviewService_AddDuplicateReviewer_ShouldUpdate()
        {
            // Arrange
            var service = new ProfileReviewService();
            service.AddOrUpdateRating("profile1", "Reviewer1", "rev1", 5, "Great!");

            // Act
            var result = service.AddOrUpdateRating("profile1", "Reviewer1", "rev1", 3, "Not so great");

            // Assert
            Assert.True(result);
            Assert.Equal(1, service.GetRatingCount("profile1"));
            var ratings = service.GetRatings("profile1");
            Assert.Single(ratings);
            Assert.Equal(3, ratings[0].Stars);
            Assert.Equal("Not so great", ratings[0].Comment);
        }

        [Fact]
        public void ProfileReviewService_GetAverageStars_ShouldCalculate()
        {
            // Arrange
            var service = new ProfileReviewService();
            service.AddOrUpdateRating("profile1", "Reviewer1", "rev1", 5, "Great!");
            service.AddOrUpdateRating("profile1", "Reviewer2", "rev2", 3, "OK");

            // Act
            var avg = service.GetAverageStars("profile1");

            // Assert
            Assert.Equal(4.0, avg);
        }

        [Fact]
        public void ProfileReviewService_RemoveRating_ShouldRemove()
        {
            // Arrange
            var service = new ProfileReviewService();
            service.AddOrUpdateRating("profile1", "Reviewer1", "rev1", 5, "Great!");

            // Act
            var result = service.RemoveRating("profile1", "rev1");

            // Assert
            Assert.True(result);
            Assert.Equal(0, service.GetRatingCount("profile1"));
        }

        [Fact]
        public void ProfileReviewService_AddHelpfulVote_ShouldAdd()
        {
            // Arrange
            var service = new ProfileReviewService();
            service.AddOrUpdateRating("profile1", "Reviewer1", "rev1", 5, "Great!");
            var ratingId = service.GetRatings("profile1")[0].Id;

            // Act
            var result = service.AddHelpfulVote("profile1", ratingId, "voter1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ProfileReviewService_AddDuplicateHelpfulVote_ShouldNotAdd()
        {
            // Arrange
            var service = new ProfileReviewService();
            service.AddOrUpdateRating("profile1", "Reviewer1", "rev1", 5, "Great!");
            var ratingId = service.GetRatings("profile1")[0].Id;
            service.AddHelpfulVote("profile1", ratingId, "voter1");

            // Act
            var result = service.AddHelpfulVote("profile1", ratingId, "voter1");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ProfileReviewService_GetRatedProfileIds_ShouldReturnIds()
        {
            // Arrange
            var service = new ProfileReviewService();
            service.AddOrUpdateRating("profile1", "Reviewer1", "rev1", 5, "Great!");
            service.AddOrUpdateRating("profile2", "Reviewer2", "rev2", 3, "OK");

            // Act
            var ids = service.GetRatedProfileIds();

            // Assert
            Assert.Contains("profile1", ids);
            Assert.Contains("profile2", ids);
            Assert.Equal(2, ids.Count);
        }

        #endregion
    }
}
