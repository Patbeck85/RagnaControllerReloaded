using System;
using System.Collections.Generic;
using Xunit;
using RagnaController.Core;
using RagnaController.Core.Calculators;
using RagnaController.Core.Database;

namespace RagnaController.Tests
{
    /// <summary>
    /// TASK-3007: Unit-Tests für Epic 21 (RO-Specific Game Knowledge).
    /// Deckt MapDatabaseService, ItemDatabaseService, SkillSimulator, StatCalculator, ExperienceCalculator ab.
    /// </summary>
    public class ROGameKnowledgeTests
    {
        #region MapDatabaseService Tests

        [Fact]
        public void MapDatabaseService_GetMap_ById_ShouldReturnMap()
        {
            var db = new MapDatabaseService();
            var map = db.GetMap(1);
            Assert.NotNull(map);
            Assert.Equal("prontera", map?.Name);
        }

        [Fact]
        public void MapDatabaseService_GetMap_ByName_ShouldReturnMap()
        {
            var db = new MapDatabaseService();
            var map = db.GetMap("payon");
            Assert.NotNull(map);
            Assert.Equal("payon", map?.Name);
        }

        [Fact]
        public void MapDatabaseService_GetMapsForLevelRange_ShouldReturnMatchingMaps()
        {
            var db = new MapDatabaseService();
            var maps = db.GetMapsForLevelRange(20, GameMode.PreRenewal);
            Assert.NotEmpty(maps);
            foreach (var m in maps)
            {
                Assert.True(m.PreRenewalLevelMin <= 20 && m.PreRenewalLevelMax >= 20);
            }
        }

        [Fact]
        public void MapDatabaseService_GetKafraMaps_ShouldReturnOnlyKafra()
        {
            var db = new MapDatabaseService();
            var kafraMaps = db.GetKafraMaps();
            Assert.NotEmpty(kafraMaps);
            foreach (var m in kafraMaps)
                Assert.True(m.IsKafra);
        }

        [Fact]
        public void MapDatabaseService_GetMapsByType_ShouldFilterByType()
        {
            var db = new MapDatabaseService();
            var dungeons = db.GetMapsByType(MapType.Dungeon);
            Assert.NotEmpty(dungeons);
            foreach (var m in dungeons)
                Assert.Equal(MapType.Dungeon, m.Type);
        }

        #endregion

        #region ItemDatabaseService Tests

        [Fact]
        public void ItemDatabaseService_GetItem_ById_ShouldReturnItem()
        {
            var db = new ItemDatabaseService();
            var item = db.GetItem(101);
            Assert.NotNull(item);
            Assert.Equal("Apple", item?.Name);
        }

        [Fact]
        public void ItemDatabaseService_GetItem_ByName_ShouldReturnItem()
        {
            var db = new ItemDatabaseService();
            var item = db.GetItem("Iron Sword");
            Assert.NotNull(item);
            Assert.Equal("Iron Sword", item?.Name);
        }

        [Fact]
        public void ItemDatabaseService_Search_ByType_ShouldFilter()
        {
            var db = new ItemDatabaseService();
            var weapons = db.Search(type: ItemType.Weapon);
            Assert.NotEmpty(weapons);
            foreach (var w in weapons)
                Assert.Equal(ItemType.Weapon, w.Type);
        }

        [Fact]
        public void ItemDatabaseService_Search_ByName_ShouldFilter()
        {
            var db = new ItemDatabaseService();
            var items = db.Search(name: "apple");
            Assert.Single(items);
            Assert.Equal("Apple", items[0].Name);
        }

        [Fact]
        public void ItemDatabaseService_Search_ByPriceRange_ShouldFilter()
        {
            var db = new ItemDatabaseService();
            var items = db.Search(minPrice: 150, maxPrice: 500);
            Assert.NotEmpty(items);
            foreach (var i in items)
            {
                Assert.True(i.Price >= 150 && i.Price <= 500);
            }
        }

        #endregion

        #region SkillSimulator Tests

        [Fact]
        public void SkillSimulator_AddSkill_ShouldAddSkill()
        {
            var sim = new SkillSimulator(50, 30);
            sim.AddSkill(1, "Bash", 10, 1, 1);
            Assert.Single(sim.Skills);
        }

        [Fact]
        public void SkillSimulator_UpgradeSkill_ShouldConsumePoint()
        {
            var sim = new SkillSimulator(50, 30);
            sim.AddSkill(1, "Bash", 10, 1, 1);
            var skill = sim.Skills[0];

            var result = sim.UpgradeSkill(skill);

            Assert.True(result);
            Assert.Equal(1, skill.Level);
            Assert.Equal(1, sim.TotalPointsSpent);
            Assert.Equal(sim.AvailablePoints - 1, sim.PointsRemaining);
        }

        [Fact]
        public void SkillSimulator_UpgradeSkill_AtMaxLevel_ShouldFail()
        {
            var sim = new SkillSimulator(50, 30);
            sim.AddSkill(1, "Bash", 1, 1, 1);
            var skill = sim.Skills[0];
            sim.UpgradeSkill(skill);

            var result = sim.UpgradeSkill(skill);

            Assert.False(result);
            Assert.Equal(1, skill.Level);
        }

        [Fact]
        public void SkillSimulator_DowngradeSkill_ShouldRefundPoint()
        {
            var sim = new SkillSimulator(50, 30);
            sim.AddSkill(1, "Bash", 10, 1, 1);
            sim.UpgradeSkill(skill: sim.Skills[0]);
            sim.DowngradeSkill(sim.Skills[0]);

            Assert.Equal(0, sim.Skills[0].Level);
            Assert.Equal(0, sim.TotalPointsSpent);
        }

        [Fact]
        public void SkillSimulator_GenerateBuildCode_ShouldContainSkills()
        {
            var sim = new SkillSimulator(50, 30);
            sim.AddSkill(1, "Bash", 10, 1, 1);
            sim.UpgradeSkill(sim.Skills[0]);

            var code = sim.GenerateBuildCode();
            Assert.Contains("Bash", code);
            Assert.Contains("Base Level: 50", code);
        }

        [Fact]
        public void SkillSimulator_Reset_ShouldClearAllLevels()
        {
            var sim = new SkillSimulator(50, 30);
            sim.AddSkill(1, "Bash", 10, 1, 1);
            sim.UpgradeSkill(sim.Skills[0]);
            sim.Reset();

            Assert.Equal(0, sim.Skills[0].Level);
            Assert.Equal(0, sim.TotalPointsSpent);
        }

        #endregion

        #region StatCalculator Tests

        [Fact]
        public void StatCalculator_GetAvailableStatPoints_ShouldCalculate()
        {
            Assert.Equal(0, StatCalculator.GetAvailableStatPoints(1));
            Assert.Equal(10, StatCalculator.GetAvailableStatPoints(11));
            Assert.Equal(98, StatCalculator.GetAvailableStatPoints(99));
        }

        [Fact]
        public void StatCalculator_CalculateMaxHP_ShouldReturnPositive()
        {
            var hp = StatCalculator.CalculateMaxHP(50, 20);
            Assert.True(hp > 0);
        }

        [Fact]
        public void StatCalculator_CalculateMaxSP_ShouldReturnPositive()
        {
            var sp = StatCalculator.CalculateMaxSP(50, 20);
            Assert.True(sp > 0);
        }

        [Fact]
        public void StatCalculator_CalculateAttackSpeed_ShouldReturnPositive()
        {
            var atkSpd = StatCalculator.CalculateAttackSpeed(50, 50);
            Assert.True(atkSpd > 0);
        }

        [Fact]
        public void StatCalculator_CalculateHitRate_ShouldReturnPositive()
        {
            var hit = StatCalculator.CalculateHitRate(50, 20);
            Assert.True(hit > 0);
        }

        [Fact]
        public void StatCalculator_CalculateFlee_ShouldReturnPositive()
        {
            var flee = StatCalculator.CalculateFlee(50, 20);
            Assert.True(flee > 0);
        }

        #endregion

        #region ExperienceCalculator Tests

        [Fact]
        public void ExperienceCalculator_GetExpForBaseLevel_ShouldReturnPositive()
        {
            var exp = ExperienceCalculator.GetExpForBaseLevel(50);
            Assert.True(exp > 0);
        }

        [Fact]
        public void ExperienceCalculator_GetExpForJobLevel_ShouldReturnPositive()
        {
            var exp = ExperienceCalculator.GetExpForJobLevel(50);
            Assert.True(exp > 0);
        }

        [Fact]
        public void ExperienceCalculator_CalculateKillCount_ShouldReturnPositive()
        {
            var kills = ExperienceCalculator.CalculateKillCount(10000, 100);
            Assert.True(kills > 0);
        }

        [Fact]
        public void ExperienceCalculator_CalculateKillCount_ZeroMonsterExp_ShouldReturnZero()
        {
            var kills = ExperienceCalculator.CalculateKillCount(10000, 0);
            Assert.Equal(0, kills);
        }

        [Fact]
        public void ExperienceCalculator_CalculateKillCountForBaseLevel_ShouldReturnPositive()
        {
            var kills = ExperienceCalculator.CalculateKillCountForBaseLevel(1, 50, 500);
            Assert.True(kills > 0);
        }

        [Fact]
        public void ExperienceCalculator_CalculateKillCountForJobLevel_ShouldReturnPositive()
        {
            var kills = ExperienceCalculator.CalculateKillCountForJobLevel(1, 50, 500);
            Assert.True(kills > 0);
        }

        #endregion
    }
}
