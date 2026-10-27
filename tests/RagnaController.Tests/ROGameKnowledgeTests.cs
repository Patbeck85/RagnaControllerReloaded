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
            var hpPre = StatCalculator.CalculateMaxHP(50, 20, "Swordman", ExperienceCalculator.GameMode.PreRenewal);
            var hpRenewal = StatCalculator.CalculateMaxHP(50, 20, "Swordman", ExperienceCalculator.GameMode.Renewal);

            Assert.True(hpRenewal > hpPre);
            Assert.True(hpPre > 0);
            Assert.True(hpRenewal > 0);
        }

        [Fact]
        public void StatCalculator_CalculateMaxSP_PreRenewal_Vs_Renewal()
        {
            var spPre = StatCalculator.CalculateMaxSP(50, 20, "Mage", ExperienceCalculator.GameMode.PreRenewal);
            var spRenewal = StatCalculator.CalculateMaxSP(50, 20, "Mage", ExperienceCalculator.GameMode.Renewal);

            Assert.True(spRenewal > spPre);
            Assert.True(spPre > 0);
            Assert.True(spRenewal > 0);
        }

        [Fact]
        public void StatCalculator_ClassSpecificFactors_ShouldDiffer()
        {
            var hpMage = StatCalculator.CalculateMaxHP(50, 10, "Mage", ExperienceCalculator.GameMode.PreRenewal);
            var hpKnight = StatCalculator.CalculateMaxHP(50, 10, "Knight", ExperienceCalculator.GameMode.PreRenewal);

            Assert.True(hpKnight > hpMage);
        }

        [Fact]
        public void StatCalculator_CalculateAttackSpeed_ShouldHaveMinimum()
        {
            var aspdPre = StatCalculator.CalculateAttackSpeed(99, 99, "Novice", ExperienceCalculator.GameMode.PreRenewal);
            var aspdRenewal = StatCalculator.CalculateAttackSpeed(99, 99, "Novice", ExperienceCalculator.GameMode.Renewal);

            Assert.True(aspdPre >= 50);
            Assert.True(aspdRenewal >= 10);
        }

        [Fact]
        public void StatCalculator_CalculateHitRate_Renewal_LukBonus()
        {
            var hitPre = StatCalculator.CalculateHitRate(10, 50, "Novice", ExperienceCalculator.GameMode.PreRenewal);
            var hitRenewal = StatCalculator.CalculateHitRate(10, 50, "Novice", ExperienceCalculator.GameMode.Renewal);

            Assert.True(hitRenewal > hitPre);
        }

        [Fact]
        public void StatCalculator_CalculateFlee_Renewal_LukBonus()
        {
            var fleePre = StatCalculator.CalculateFlee(10, 50, "Novice", ExperienceCalculator.GameMode.PreRenewal);
            var fleeRenewal = StatCalculator.CalculateFlee(10, 50, "Novice", ExperienceCalculator.GameMode.Renewal);

            Assert.True(fleeRenewal > fleePre);
        }

        [Fact]
        public void StatCalculator_CalculateAllStats_ShouldReturnAllValues()
        {
            var result = StatCalculator.CalculateAllStats(
                baseLevel: 50, jobLevel: 30,
                str: 20, agi: 20, vit: 20, int_: 20, dex: 20, luk: 20,
                className: "Swordman", mode: ExperienceCalculator.GameMode.PreRenewal);

            Assert.Equal(50, result.BaseLevel);
            Assert.Equal(30, result.JobLevel);
            Assert.Equal("Swordman", result.ClassName);
            Assert.Equal(ExperienceCalculator.GameMode.PreRenewal, result.Mode);
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


        #region StatCalculator Extended Tests

        [Fact]
        public void StatCalculator_CalculateAllStats_Renewal_ShouldHaveRenewalMultiplier()
        {
            var result = StatCalculator.CalculateAllStats(
                baseLevel: 99, jobLevel: 50,
                str: 50, agi: 50, vit: 50, int_: 50, dex: 50, luk: 50,
                className: "Knight", mode: ExperienceCalculator.GameMode.Renewal);

            Assert.Equal(ExperienceCalculator.GameMode.Renewal, result.Mode);
            Assert.True(result.MaxHP > 3000, $"Renewal MaxHP should be > 3000, got {result.MaxHP}");
            Assert.True(result.MaxSP > 1000, $"Renewal MaxSP should be high, got {result.MaxSP}");
            Assert.True(result.AttackSpeed >= 10, $"Renewal ASPD minimum should be 10, got {result.AttackSpeed}");
        }

        [Fact]
        public void StatCalculator_CalculateAllStats_PreRenewal_ShouldHavePreRenewalValues()
        {
            var result = StatCalculator.CalculateAllStats(
                baseLevel: 99, jobLevel: 50,
                str: 50, agi: 50, vit: 50, int_: 50, dex: 50, luk: 50,
                className: "Knight", mode: ExperienceCalculator.GameMode.PreRenewal);

            Assert.Equal(ExperienceCalculator.GameMode.PreRenewal, result.Mode);
            Assert.True(result.MaxHP > 0);
            Assert.True(result.MaxSP > 0);
            Assert.True(result.AttackSpeed >= 50, $"Pre-Renewal ASPD minimum should be 50, got {result.AttackSpeed}");
        }

        [Fact]
        public void StatCalculator_AllSupportedClasses_ShouldCalculateWithoutError()
        {
            var classes = new[] { "Novice", "Swordman", "Mage", "Archer", "Acolyte", "Merchant", "Thief",
                "Knight", "Wizard", "Hunter", "Priest", "Blacksmith", "Assassin",
                "Crusader", "High Wizard", "Sniper", "High Priest", "Professor", "Stalker", "Creator", "Champion" };

            foreach (var className in classes)
            {
                var hp = StatCalculator.CalculateMaxHP(50, 30, className, ExperienceCalculator.GameMode.PreRenewal);
                var sp = StatCalculator.CalculateMaxSP(50, 30, className, ExperienceCalculator.GameMode.PreRenewal);
                var aspd = StatCalculator.CalculateAttackSpeed(50, 50, className, ExperienceCalculator.GameMode.PreRenewal);
                var hit = StatCalculator.CalculateHitRate(50, 30, className, ExperienceCalculator.GameMode.PreRenewal);
                var flee = StatCalculator.CalculateFlee(50, 30, className, ExperienceCalculator.GameMode.PreRenewal);

                Assert.True(hp > 0, $"{className} HP should be > 0");
                Assert.True(sp > 0, $"{className} SP should be > 0");
                Assert.True(aspd > 0, $"{className} ASPD should be > 0");
                Assert.True(hit >= 0, $"{className} Hit should be >= 0");
                Assert.True(flee >= 0, $"{className} Flee should be >= 0");
            }
        }

        [Fact]
        public void StatCalculator_Renewal_HpShouldBeHigherThanPreRenewal()
        {
            var hpPre = StatCalculator.CalculateMaxHP(50, 30, "Swordman", ExperienceCalculator.GameMode.PreRenewal);
            var hpRenewal = StatCalculator.CalculateMaxHP(50, 30, "Swordman", ExperienceCalculator.GameMode.Renewal);

            Assert.True(hpRenewal > hpPre, $"Renewal HP ({hpRenewal}) should be > Pre-Renewal HP ({hpPre})");
        }

        [Fact]
        public void StatCalculator_Renewal_SpShouldBeHigherThanPreRenewal()
        {
            var spPre = StatCalculator.CalculateMaxSP(50, 30, "Mage", ExperienceCalculator.GameMode.PreRenewal);
            var spRenewal = StatCalculator.CalculateMaxSP(50, 30, "Mage", ExperienceCalculator.GameMode.Renewal);

            Assert.True(spRenewal > spPre, $"Renewal SP ({spRenewal}) should be > Pre-Renewal SP ({spPre})");
        }

        #endregion

        #region SkillSimulator Extended Tests

        [Fact]
        public void SkillSimulator_AddSkill_WithDescriptionAndType_ShouldStoreProperties()
        {
            var sim = new SkillSimulator(50, 50);
            sim.AddSkill(1, "Sword Mastery", 10, 1, 1, "Increases sword damage", "Passive", "skills/sword_mastery.png");

            var skill = sim.Skills.First();
            Assert.Equal(1, skill.Id);
            Assert.Equal("Sword Mastery", skill.Name);
            Assert.Equal("Increases sword damage", skill.Description);
            Assert.Equal("Passive", skill.SkillType);
            Assert.Equal("skills/sword_mastery.png", skill.IconPath);
        }

        [Fact]
        public void SkillSimulator_AddSkillWithPrereq_ShouldBlockUpgradeUntilMet()
        {
            var sim = new SkillSimulator(50, 50);
            sim.AddSkill(101, "Fire Bolt", 10, 1, 1);
            sim.AddSkillWithPrereq(102, "Fire Ball", 10, 1, 1,
                new Dictionary<int, int> { { 101, 5 } }, "Fire Ball description");

            var fireBall = sim.Skills.First(s => s.Id == 102);
            Assert.False(sim.CanUpgrade(fireBall), "Fire Ball should be blocked until Fire Bolt is level 5");

            // Upgrade Fire Bolt to level 5
            var fireBolt = sim.Skills.First(s => s.Id == 101);
            for (int i = 0; i < 5; i++) sim.UpgradeSkill(fireBolt);

            Assert.True(sim.CanUpgrade(fireBall), "Fire Ball should be upgradable after Fire Bolt reaches level 5");
        }

        [Fact]
        public void SkillSimulator_GenerateBuildCode_PlainText_ShouldContainSkillLevels()
        {
            var sim = new SkillSimulator(50, 50);
            sim.AddSkill(1, "Sword Mastery", 10, 1, 1);
            sim.UpgradeSkill(sim.Skills.First());

            var code = sim.GenerateBuildCode();
            Assert.Contains("Sword Mastery: 1/10", code);
            Assert.Contains("// RagnaController Skill Build", code);
            Assert.Contains("Base Level: 50", code);
        }

        [Fact]
        public void SkillSimulator_GenerateBuildCode_IroWiki_ShouldContainWikiFormat()
        {
            var sim = new SkillSimulator(50, 50);
            sim.AddSkill(1, "Sword Mastery", 10, 1, 1);
            sim.UpgradeSkill(sim.Skills.First());

            var code = sim.GenerateBuildCode(SkillSimulator.BuildCodeFormat.IroWiki);
            Assert.Contains("{| class=\"wikitable\"", code);
            Assert.Contains("Sword Mastery", code);
        }

        [Fact]
        public void SkillSimulator_GenerateBuildCode_Json_ShouldContainJsonStructure()
        {
            var sim = new SkillSimulator(50, 50);
            sim.AddSkill(1, "Sword Mastery", 10, 1, 1);
            sim.UpgradeSkill(sim.Skills.First());

            var code = sim.GenerateBuildCode(SkillSimulator.BuildCodeFormat.Json);
            Assert.Contains("\"baseLevel\": 50", code);
            Assert.Contains("\"name\": \"Sword Mastery\"", code);
            Assert.Contains("\"level\": 1", code);
        }

        [Fact]
        public void SkillSimulator_CanUpgrade_WithInsufficientPoints_ShouldReturnFalse()
        {
            var sim = new SkillSimulator(1, 1); // 0 available points
            sim.AddSkill(1, "Sword Mastery", 10, 1, 1);

            var skill = sim.Skills.First();
            Assert.False(sim.CanUpgrade(skill), "Should not be upgradable with 0 points");
        }

        [Fact]
        public void SkillSimulator_Reset_ShouldSetAllLevelsToZero()
        {
            var sim = new SkillSimulator(50, 50);
            sim.AddSkill(1, "Sword Mastery", 10, 1, 1);
            sim.UpgradeSkill(sim.Skills.First());

            sim.Reset();

            Assert.All(sim.Skills, s => Assert.Equal(0, s.Level));
        }

        #endregion
    }
}
