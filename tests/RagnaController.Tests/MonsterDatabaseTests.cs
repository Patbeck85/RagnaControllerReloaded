using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using RagnaController.Core.Services;
using RagnaController.Core.Services.Database;
using RagnaController.Models;

namespace RagnaController.Tests
{
    public class MonsterTests
    {
        [Fact]
        public void Monster_GetHp_ReturnsPreRenewalHp_WhenNotRenewal()
        {
            var monster = new Monster
            {
                Id = 1002,
                Name = "Poring",
                PreRenewalHp = 85,
                RenewalHp = 200
            };

            Assert.Equal(85, monster.GetHp(isRenewal: false));
            Assert.Equal(200, monster.GetHp(isRenewal: true));
        }

        [Fact]
        public void Monster_GetExp_ReturnsRenewalExp_WhenRenewal()
        {
            var monster = new Monster
            {
                Id = 1002,
                Name = "Poring",
                PreRenewalExp = 25,
                RenewalExp = 60
            };

            Assert.Equal(25, monster.GetExp(isRenewal: false));
            Assert.Equal(60, monster.GetExp(isRenewal: true));
        }

        [Fact]
        public void Monster_GetDrops_ReturnsCorrectModeDrops()
        {
            var monster = new Monster
            {
                Id = 1002,
                Name = "Poring",
                PreRenewalDrops = new Dictionary<int, int> { { 501, 500 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 600 } }
            };

            var preDrops = monster.GetDrops(isRenewal: false);
            var renDrops = monster.GetDrops(isRenewal: true);

            Assert.Equal(500, preDrops[501]);
            Assert.Equal(600, renDrops[501]);
        }

        [Fact]
        public void Monster_Clone_CreatesIndependentCopy()
        {
            var monster = new Monster
            {
                Id = 1002,
                Name = "Poring",
                PreRenewalHp = 85,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 500 } }
            };

            var clone = monster.Clone();
            clone.PreRenewalHp = 999;
            clone.PreRenewalDrops[501] = 777;

            Assert.Equal(85, monster.PreRenewalHp);
            Assert.Equal(500, monster.PreRenewalDrops[501]);
            Assert.Equal(999, clone.PreRenewalHp);
            Assert.Equal(777, clone.PreRenewalDrops[501]);
        }

        [Fact]
        public void Monster_IsMvp_FlagWorks()
        {
            var mvp = new Monster { Id = 1301, Name = "Lord Knight", IsMvp = true };
            var regular = new Monster { Id = 1002, Name = "Poring", IsMvp = false };

            Assert.True(mvp.IsMvp);
            Assert.False(regular.IsMvp);
        }
    }

    public class MonsterDatabaseTests
    {
        [Fact]
        public void Database_AddAndGetMonster_Works()
        {
            var db = new MonsterDatabase();
            var monster = new Monster { Id = 1002, Name = "Poring" };
            db.Add(monster);

            Assert.Equal(1, db.Count);
            Assert.Same(monster, db.GetMonster(1002));
            Assert.Same(monster, db.GetMonster("Poring"));
        }

        [Fact]
        public void Database_GetMonster_UnknownId_ReturnsNull()
        {
            var db = new MonsterDatabase();
            Assert.Null(db.GetMonster(99999));
            Assert.Null(db.GetMonster("UnknownMonster"));
        }

        [Fact]
        public void Database_RemoveMonster_Works()
        {
            var db = new MonsterDatabase();
            db.Add(new Monster { Id = 1002, Name = "Poring" });

            Assert.True(db.Remove(1002));
            Assert.Null(db.GetMonster(1002));
            Assert.Equal(0, db.Count);
        }

        [Fact]
        public void Database_RemoveUnknownId_ReturnsFalse()
        {
            var db = new MonsterDatabase();
            Assert.False(db.Remove(99999));
        }

        [Fact]
        public void Database_GetByElement_FiltersCorrectly()
        {
            var db = new MonsterDatabase();
            db.Add(new Monster { Id = 1, Name = "FireMob", Element = MonsterElement.Fire });
            db.Add(new Monster { Id = 2, Name = "WaterMob", Element = MonsterElement.Water });
            db.Add(new Monster { Id = 3, Name = "FireMob2", Element = MonsterElement.Fire });

            var fireMobs = db.GetByElement(MonsterElement.Fire);
            Assert.Equal(2, fireMobs.Count);
            Assert.All(fireMobs, m => Assert.Equal(MonsterElement.Fire, m.Element));
        }

        [Fact]
        public void Database_GetMvps_ReturnsOnlyMvps()
        {
            var db = new MonsterDatabase();
            db.Add(new Monster { Id = 1, Name = "Regular", IsMvp = false });
            db.Add(new Monster { Id = 2, Name = "MVP1", IsMvp = true });
            db.Add(new Monster { Id = 3, Name = "MVP2", IsMvp = true });

            var mvps = db.GetMvps();
            Assert.Equal(2, mvps.Count);
            Assert.All(mvps, m => Assert.True(m.IsMvp));
        }

        [Fact]
        public void Database_Search_FindsByName()
        {
            var db = new MonsterDatabase();
            db.Add(new Monster { Id = 1, Name = "Poring" });
            db.Add(new Monster { Id = 2, Name = "Poporing" });
            db.Add(new Monster { Id = 3, Name = "Marin" });

            var results = db.Search("poring");
            Assert.Equal(2, results.Count);
            Assert.Contains("Poring", results.Select(m => m.Name));
            Assert.Contains("Poporing", results.Select(m => m.Name));
        }

        [Fact]
        public void Database_Search_CaseInsensitive()
        {
            var db = new MonsterDatabase();
            db.Add(new Monster { Id = 1, Name = "Poring" });

            Assert.Single(db.Search("PORING"));
            Assert.Single(db.Search("poring"));
            Assert.Single(db.Search("Poring"));
        }

        [Fact]
        public void Database_CreateStarterDatabase_HasMonsters()
        {
            var db = MonsterDatabase.CreateStarterDatabase();
            Assert.True(db.Count > 0);
            Assert.NotNull(db.GetMonster("Poring"));
            Assert.NotNull(db.GetMonster("Lord Knight"));
        }

        [Fact]
        public void Database_StarterDb_HasMvp()
        {
            var db = MonsterDatabase.CreateStarterDatabase();
            var mvps = db.GetMvps();
            Assert.NotEmpty(mvps);
            Assert.Contains("Lord Knight", mvps.Select(m => m.Name));
        }
    }

    public class MonsterDatabaseServiceTests
    {
        [Fact]
        public void Service_IsRenewal_DefaultsToTrue()
        {
            var service = new MonsterDatabaseService();
            Assert.True(service.IsRenewal);
        }

        [Fact]
        public void Service_SwitchGameMode_UpdatesFilteredMonsters()
        {
            var service = new MonsterDatabaseService();
            var poring = service.GetMonster("Poring");
            Assert.NotNull(poring);

            service.IsRenewal = false;
            Assert.False(service.IsRenewal);
        }

        [Fact]
        public void Service_FilterText_FiltersList()
        {
            var service = new MonsterDatabaseService();
            service.FilterText = "Poring";
            Assert.True(service.FilteredMonsters.Count >= 1);
            Assert.All(service.FilteredMonsters, m =>
                Assert.Contains("Poring", m.Name, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Service_ElementFilter_FiltersList()
        {
            var service = new MonsterDatabaseService();
            service.ElementFilter = MonsterElement.Fire;
            Assert.All(service.FilteredMonsters, m =>
                Assert.Equal(MonsterElement.Fire, m.Element));
        }

        [Fact]
        public void Service_ShowMvpOnly_FiltersList()
        {
            var service = new MonsterDatabaseService();
            service.ShowMvpOnly = true;
            Assert.All(service.FilteredMonsters, m => Assert.True(m.IsMvp));
        }

        [Fact]
        public void Service_ViewModel_HpDependsOnGameMode()
        {
            var service = new MonsterDatabaseService();

            // Poring: Pre-Renewal HP = 85, Renewal HP = 200
            service.IsRenewal = true;
            var poringVmRenewal = service.FilteredMonsters.FirstOrDefault(m => m.Name == "Poring");
            Assert.NotNull(poringVmRenewal);
            Assert.Equal(200, poringVmRenewal.Hp);

            service.IsRenewal = false;
            var poringVmPreRenewal = service.FilteredMonsters.FirstOrDefault(m => m.Name == "Poring");
            Assert.NotNull(poringVmPreRenewal);
            Assert.Equal(85, poringVmPreRenewal.Hp);
        }

        [Fact]
        public void Service_AllElements_ReturnsAllEnumValues()
        {
            var service = new MonsterDatabaseService();
            var elements = service.AllElements.ToList();
            Assert.Contains(MonsterElement.Fire, elements);
            Assert.Contains(MonsterElement.Water, elements);
            Assert.Contains(MonsterElement.Neutral, elements);
        }

        [Fact]
        public void Service_AllRaces_ReturnsAllEnumValues()
        {
            var service = new MonsterDatabaseService();
            var races = service.AllRaces.ToList();
            Assert.Contains(MonsterRace.Undead, races);
            Assert.Contains(MonsterRace.Beast, races);
            Assert.Contains(MonsterRace.Formless, races);
        }

        [Fact]
        public void Database_HasMoreThan30Monsters()
        {
            var db = MonsterDatabase.CreateStarterDatabase();
            Assert.True(db.Count > 30, $"Erwartet >30 Monster, aber nur {db.Count} gefunden.");
        }

        [Fact]
        public void Database_HasMultipleMvpBosses()
        {
            var db = MonsterDatabase.CreateStarterDatabase();
            var mvps = db.GetMvps();
            Assert.True(mvps.Count >= 5, $"Erwartet >=5 MVP-Bosse, aber nur {mvps.Count} gefunden.");
            Assert.Contains("Lord Knight", mvps.Select(m => m.Name));
            Assert.Contains("Golden Thief Bug", mvps.Select(m => m.Name));
            Assert.Contains("Stormy Knight", mvps.Select(m => m.Name));
            Assert.Contains("Queen Ant", mvps.Select(m => m.Name));
            Assert.Contains("Baphomet Jr.", mvps.Select(m => m.Name));
            Assert.Contains("Dark Lord", mvps.Select(m => m.Name));
            Assert.Contains("Turtle General", mvps.Select(m => m.Name));
            Assert.Contains("General Glast Heim", mvps.Select(m => m.Name));
        }

        [Fact]
        public void Database_PreRenewalVsRenewal_HaveDifferentValues()
        {
            var db = MonsterDatabase.CreateStarterDatabase();
            var poring = db.GetMonster("Poring");
            Assert.NotNull(poring);
            // Poring sollte unterschiedliche Pre-Renewal und Renewal Werte haben
            Assert.NotEqual(poring.PreRenewalHp, poring.RenewalHp);
            Assert.NotEqual(poring.PreRenewalExp, poring.RenewalExp);
        }

        [Fact]
        public void Database_CoversAllElementTypes()
        {
            var db = MonsterDatabase.CreateStarterDatabase();
            var elements = db.GetAllMonsters().Select(m => m.Element).Distinct().ToList();
            Assert.Contains(MonsterElement.Water, elements);
            Assert.Contains(MonsterElement.Earth, elements);
            Assert.Contains(MonsterElement.Fire, elements);
            Assert.Contains(MonsterElement.Wind, elements);
            Assert.Contains(MonsterElement.Poison, elements);
            Assert.Contains(MonsterElement.Shadow, elements);
            Assert.Contains(MonsterElement.Undead, elements);
        }

        [Fact]
        public void Database_CoversAllRaceTypes()
        {
            var db = MonsterDatabase.CreateStarterDatabase();
            var races = db.GetAllMonsters().Select(m => m.Race).Distinct().ToList();
            Assert.Contains(MonsterRace.Formless, races);
            Assert.Contains(MonsterRace.Undead, races);
            Assert.Contains(MonsterRace.Beast, races);
            Assert.Contains(MonsterRace.Brute, races);
            Assert.Contains(MonsterRace.Plant, races);
            Assert.Contains(MonsterRace.Insect, races);
            Assert.Contains(MonsterRace.Fish, races);
            Assert.Contains(MonsterRace.Demon, races);
            Assert.Contains(MonsterRace.DemiHuman, races);
        }

        [Fact]
        public void Database_CoversAllSizeTypes()
        {
            var db = MonsterDatabase.CreateStarterDatabase();
            var sizes = db.GetAllMonsters().Select(m => m.Size).Distinct().ToList();
            Assert.Contains(MonsterSize.Small, sizes);
            Assert.Contains(MonsterSize.Medium, sizes);
            Assert.Contains(MonsterSize.Large, sizes);
        }

        [Fact]
        public void Database_GetByElement_ReturnsCorrectMonsters()
        {
            var db = MonsterDatabase.CreateStarterDatabase();
            var fireMobs = db.GetByElement(MonsterElement.Fire);
            Assert.NotEmpty(fireMobs);
            Assert.All(fireMobs, m => Assert.Equal(MonsterElement.Fire, m.Element));
        }

        [Fact]
        public void Database_GetByRace_ReturnsCorrectMonsters()
        {
            var db = MonsterDatabase.CreateStarterDatabase();
            var undeadMobs = db.GetByRace(MonsterRace.Undead);
            Assert.NotEmpty(undeadMobs);
            Assert.All(undeadMobs, m => Assert.Equal(MonsterRace.Undead, m.Race));
        }

        [Fact]
        public void Database_GetMinibosses_ReturnsOnlyMinibosses()
        {
            var db = MonsterDatabase.CreateStarterDatabase();
            var minibosses = db.GetMinibosses();
            // Drops ist ein Miniboss (Id 1050)
            Assert.Contains("Drops", minibosses.Select(m => m.Name));
            Assert.All(minibosses, m => Assert.True(m.IsMiniboss));
        }

        [Fact]
        public void Database_SpawnMaps_ArePopulated()
        {
            var db = MonsterDatabase.CreateStarterDatabase();
            var poring = db.GetMonster("Poring");
            Assert.NotNull(poring);
            Assert.NotEmpty(poring.SpawnMaps);
        }

        [Fact]
        public void Database_MonsterIds_AreUnique()
        {
            var db = MonsterDatabase.CreateStarterDatabase();
            var ids = db.GetAllMonsters().Select(m => m.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }
    }
}
