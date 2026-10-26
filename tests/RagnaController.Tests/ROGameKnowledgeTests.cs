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






        #endregion

        #region SkillSimulator Tests







        #endregion

        #region StatCalculator Tests


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

        [Fact]
        public void ExperienceCalculator_GetExpForBaseLevel_Level1_ShouldReturnZero()
        {
            var exp = ExperienceCalculator.GetExpForBaseLevel(1);
            Assert.Equal(0, exp);
        }

        #region StatCalculator Tests

        [Fact]
        public void StatCalculator_GetAvailableStatPoints_ShouldCalculate()
        {
            Assert.Equal(0, StatCalculator.GetAvailableStatPoints(1));
            Assert.Equal(10, StatCalculator.GetAvailableStatPoints(11));
            Assert.Equal(98, StatCalculator.GetAvailableStatPoints(99));
        }

        [Fact]
        public void StatCalculator_GetAvailableJobStatPoints_ShouldCalculate()
        {
            Assert.Equal(0, StatCalculator.GetAvailableJobStatPoints(1));
            Assert.Equal(3, StatCalculator.GetAvailableJobStatPoints(2));
            Assert.Equal(147, StatCalculator.GetAvailableJobStatPoints(50));
        }

        [Fact]
        public void StatCalculator_CalculateMaxHP_PreRenewal_Vs_Renewal()
        {
            var hpPre = StatCalculator.CalculateMaxHP(50, 20, "Swordman", StatCalculator.GameMode.PreRenewal);
            var hpRenewal = StatCalculator.CalculateMaxHP(50, 20, "Swordman", StatCalculator.GameMode.Renewal);

            Assert.True(hpRenewal > hpPre);
            Assert.True(hpPre > 0);
            Assert.True(hpRenewal > 0);
        }

        [Fact]
        public void StatCalculator_CalculateMaxSP_PreRenewal_Vs_Renewal()
        {
            var spPre = StatCalculator.CalculateMaxSP(50, 20, "Mage", StatCalculator.GameMode.PreRenewal);
            var spRenewal = StatCalculator.CalculateMaxSP(50, 20, "Mage", StatCalculator.GameMode.Renewal);

            Assert.True(spRenewal > spPre);
            Assert.True(spPre > 0);
            Assert.True(spRenewal > 0);
        }

        [Fact]
        public void StatCalculator_ClassSpecificFactors_ShouldDiffer()
        {
            var hpMage = StatCalculator.CalculateMaxHP(50, 10, "Mage", StatCalculator.GameMode.PreRenewal);
            var hpKnight = StatCalculator.CalculateMaxHP(50, 10, "Knight", StatCalculator.GameMode.PreRenewal);

            Assert.True(hpKnight > hpMage);
        }

        [Fact]
        public void StatCalculator_CalculateAttackSpeed_ShouldHaveMinimum()
        {
            var aspdPre = StatCalculator.CalculateAttackSpeed(99, 99, "Novice", StatCalculator.GameMode.PreRenewal);
            var aspdRenewal = StatCalculator.CalculateAttackSpeed(99, 99, "Novice", StatCalculator.GameMode.Renewal);

            Assert.True(aspdPre >= 50);
            Assert.True(aspdRenewal >= 10);
        }

        [Fact]
        public void StatCalculator_CalculateHitRate_Renewal_LukBonus()
        {
            var hitPre = StatCalculator.CalculateHitRate(10, 50, "Novice", StatCalculator.GameMode.PreRenewal);
            var hitRenewal = StatCalculator.CalculateHitRate(10, 50, "Novice", StatCalculator.GameMode.Renewal);

            Assert.True(hitRenewal > hitPre);
        }

        [Fact]
        public void StatCalculator_CalculateFlee_Renewal_LukBonus()
        {
            var fleePre = StatCalculator.CalculateFlee(10, 50, "Novice", StatCalculator.GameMode.PreRenewal);
            var fleeRenewal = StatCalculator.CalculateFlee(10, 50, "Novice", StatCalculator.GameMode.Renewal);

            Assert.True(fleeRenewal > fleePre);
        }

        [Fact]
        public void StatCalculator_CalculateAllStats_ShouldReturnAllValues()
        {
            var result = StatCalculator.CalculateAllStats(
                baseLevel: 50, jobLevel: 30,
                str: 20, agi: 20, vit: 20, int_: 20, dex: 20, luk: 20,
                className: "Swordman", mode: StatCalculator.GameMode.PreRenewal);

            Assert.Equal(50, result.BaseLevel);
            Assert.Equal(30, result.JobLevel);
            Assert.Equal("Swordman", result.ClassName);
            Assert.Equal(StatCalculator.GameMode.PreRenewal, result.Mode);
            Assert.True(result.MaxHP > 0);
            Assert.True(result.MaxSP > 0);
            Assert.True(result.AttackSpeed > 0);
            Assert.True(result.HitRate > 0);
            Assert.True(result.Flee > 0);
            Assert.True(result.MaxWeight > 0);
            Assert.Equal(49, result.AvailableStatPoints);
            Assert.Equal(87, result.AvailableJobStatPoints);
        }

        [Fact]
        public void StatCalculator_CalculateMaxWeight_ShouldBeStrDependent()
        {
            var weight1 = StatCalculator.CalculateMaxWeight(10);
            var weight2 = StatCalculator.CalculateMaxWeight(50);

            Assert.True(weight2 > weight1);
            Assert.Equal(400, weight1);
            Assert.Equal(800, weight2);
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
        public void ExperienceCalculator_GetExpForBaseLevel_Renewal_Vs_PreRenewal()
        {
            var expPre = ExperienceCalculator.GetExpForBaseLevel(50, ExperienceCalculator.GameMode.PreRenewal);
            var expRenewal = ExperienceCalculator.GetExpForBaseLevel(50, ExperienceCalculator.GameMode.Renewal);

            Assert.True(expPre > 0);
            Assert.True(expRenewal > 0);
        }

        [Fact]
        public void ExperienceCalculator_GetExpForBaseLevel_HighLevel()
        {
            var exp99 = ExperienceCalculator.GetExpForBaseLevel(99, ExperienceCalculator.GameMode.PreRenewal);
            Assert.True(exp99 > 0);

            var exp175 = ExperienceCalculator.GetExpForBaseLevel(175, ExperienceCalculator.GameMode.Renewal);
            Assert.True(exp175 > 0);
            Assert.True(exp175 > exp99);
        }

        [Fact]
        public void ExperienceCalculator_GetExpForJobLevel_HighLevel()
        {
            var exp70 = ExperienceCalculator.GetExpForJobLevel(70, ExperienceCalculator.GameMode.PreRenewal);
            Assert.True(exp70 > 0);
        }

        [Fact]
        public void ExperienceCalculator_CalculateKillCount_ShouldReturnPositive()
        {
            var kills = ExperienceCalculator.CalculateKillCount(10000, 100);
            Assert.True(kills > 0);
            Assert.Equal(100, kills);
        }

        [Fact]
        public void ExperienceCalculator_CalculateKillCount_ZeroMonsterExp_ShouldReturnZero()
        {
            var kills = ExperienceCalculator.CalculateKillCount(10000, 0);
            Assert.Equal(0, kills);
        }

        [Fact]
        public void ExperienceCalculator_CalculateKillCount_PartyBonus()
        {
            var killsSolo = ExperienceCalculator.CalculateKillCount(1000, 100, partySize: 1, hasPartyBonus: false);
            var killsParty = ExperienceCalculator.CalculateKillCount(1000, 100, partySize: 3, hasPartyBonus: true);

            Assert.True(killsParty <= killsSolo);
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

        [Fact]
        public void SkillSimulator_LoadClassSkills_Swordman_ShouldLoadSkills()
        {
            var sim = new SkillSimulator(50, 30);
            SkillSimulator.LoadClassSkills(sim, "Swordman");
            Assert.NotEmpty(sim.Skills);
            Assert.Contains(sim.Skills, s => s.Name == "Bash");
            Assert.Contains(sim.Skills, s => s.Name == "Magnum Break");
            Assert.Contains(sim.Skills, s => s.Name == "Provoke");
        }

        [Fact]
        public void SkillSimulator_LoadClassSkills_Mage_ShouldLoadSkills()
        {
            var sim = new SkillSimulator(50, 30);
            SkillSimulator.LoadClassSkills(sim, "Mage");
            Assert.Contains(sim.Skills, s => s.Name == "Fire Bolt");
            Assert.Contains(sim.Skills, s => s.Name == "Fire Ball");
            Assert.Contains(sim.Skills, s => s.Name == "Meteor Storm");
        }

        [Fact]
        public void SkillSimulator_LoadClassSkills_RuneKnight_ShouldLoadSkills()
        {
            var sim = new SkillSimulator(99, 50);
            SkillSimulator.LoadClassSkills(sim, "Rune Knight");
            Assert.Contains(sim.Skills, s => s.Name == "Dragon Breath");
        }

        [Fact]
        public void SkillSimulator_LoadClassSkills_SuperNovice_ShouldLoadSkills()
        {
            var sim = new SkillSimulator(50, 30);
            SkillSimulator.LoadClassSkills(sim, "Super Novice");
            Assert.Contains(sim.Skills, s => s.Name == "Berserk");
            Assert.Contains(sim.Skills, s => s.Name == "Providence");
        }

        [Fact]
        public void SkillSimulator_LoadClassSkills_UnknownClass_ShouldLoadNovice()
        {
            var sim = new SkillSimulator(50, 30);
            SkillSimulator.LoadClassSkills(sim, "UnknownClass");
            Assert.Contains(sim.Skills, s => s.Name == "Punch");
            Assert.Contains(sim.Skills, s => s.Name == "Stones");
        }

        [Fact]
        public void SkillSimulator_GetSupportedClasses_ShouldReturnAllClasses()
        {
            var classes = SkillSimulator.GetSupportedClasses();
            Assert.NotEmpty(classes);
            Assert.Contains("Novice", classes);
            Assert.Contains("Swordman", classes);
            Assert.Contains("Mage", classes);
            Assert.Contains("Knight", classes);
            Assert.Contains("Rune Knight", classes);
            Assert.Contains("Super Novice", classes);
            Assert.Contains("Gunslinger", classes);
            Assert.True(classes.Count >= 50);
        }

        [Fact]
        public void SkillSimulator_CanUpgrade_ShouldCheckRequirements()
        {
            var sim = new SkillSimulator(10, 5);
            sim.AddSkill(1, "Bash", 10, 20, 10);
            var skill = sim.Skills[0];
            Assert.False(sim.CanUpgrade(skill));
        }

        #endregion

        #region MapDatabaseService Tests

        [Fact]
        public void MapDatabaseService_GetMapsForLevelRangePreRenewal_ShouldFilter()
        {
            var db = new MapDatabaseService();
            var maps = db.GetMapsForLevelRangePreRenewal(20);

            Assert.NotEmpty(maps);
            foreach (var m in maps)
            {
                Assert.True(m.PreRenewalLevelMin <= 20 && m.PreRenewalLevelMax >= 20);
            }
        }

        [Fact]
        public void MapDatabaseService_GetMapsForLevelRangeRenewal_ShouldFilter()
        {
            var db = new MapDatabaseService();
            var maps = db.GetMapsForLevelRangeRenewal(100);

            Assert.NotEmpty(maps);
            foreach (var m in maps)
            {
                Assert.True(m.RenewalLevelMin <= 100 && m.RenewalLevelMax >= 100);
            }
        }

        [Fact]
        public void MapDatabaseService_GetMapsWithMonster_ShouldReturnMaps()
        {
            var db = new MapDatabaseService();
            var maps = db.GetMapsWithMonster(1002); // Poring

            Assert.NotEmpty(maps);
            Assert.Contains(maps, m => m.Name == "pay_fild01");
        }

        [Fact]
        public void MapDatabaseService_GetMonsterSpawns_ShouldReturnDictionary()
        {
            var db = new MapDatabaseService();
            var spawns = db.GetMonsterSpawns("pay_fild01");

            Assert.NotEmpty(spawns);
            Assert.True(spawns.ContainsKey(1002)); // Poring spawnt auf pay_fild01
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
            var items = db.Search(name: "iron sword");
            Assert.NotEmpty(items);
            Assert.All(items, i => Assert.Contains("iron sword", i.Name, StringComparison.OrdinalIgnoreCase));
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

        [Fact]
        public void ItemDatabaseService_GetItemsByDropMonster_ShouldReturnItems()
        {
            var db = new ItemDatabaseService();
            var items = db.GetItemsByDropMonster(1002); // Poring

            Assert.NotEmpty(items);
            Assert.Contains(items, i => i.Name == "Poring Card");
        }

        [Fact]
        public void ItemDatabaseService_GetMvpCards_ShouldReturnOnlyMvp()
        {
            var db = new ItemDatabaseService();
            var mvpCards = db.GetMvpCards();

            Assert.NotEmpty(mvpCards);
            foreach (var card in mvpCards)
            {
                Assert.True(card.IsMvp);
                Assert.Equal(ItemType.Card, card.Type);
            }
        }

        [Fact]
        public void ItemDatabaseService_GetConsumables_ShouldReturnOnlyConsumables()
        {
            var db = new ItemDatabaseService();
            var consumables = db.GetConsumables();

            Assert.NotEmpty(consumables);
            foreach (var item in consumables)
                Assert.Equal(ItemType.Consumable, item.Type);
        }

        [Fact]
        public void ItemDatabaseService_Search_PreRenewalMode_ShouldExcludeRenewalItems()
        {
            var db = new ItemDatabaseService();
            var preRenewal = db.Search(mode: GameMode.PreRenewal);
            var renewal = db.Search(mode: GameMode.Renewal);

            foreach (var item in preRenewal)
                Assert.False(item.IsRenewal);

            Assert.True(renewal.Count >= preRenewal.Count);
        }

        [Fact]
        public void ItemDatabaseService_ItemEntry_ShouldHaveCorrectProperties()
        {
            var db = new ItemDatabaseService();
            var item = db.GetItem(40001); // Poring Card

            Assert.NotNull(item);
            Assert.Equal(ItemType.Card, item.Type);
            Assert.False(item.IsMvp);
            Assert.Equal(1002, item.DropsFrom);
        }

        #endregion
    }
}
