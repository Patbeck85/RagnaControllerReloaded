using System;
using System.Collections.Generic;
using System.Linq;

namespace RagnaController.Models
{
    /// <summary>
    /// Datenbank für Ragnarok Online Monster mit Pre-Renewal / Renewal Unterstützung.
    /// Monster-Daten unterscheiden sich je nach aktivem GameMode.
    /// </summary>
    public class MonsterDatabase
    {
        private readonly Dictionary<int, Monster> _byId = new();
        private readonly Dictionary<string, Monster> _byName = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Anzahl der registrierten Monster.</summary>
        public int Count => _byId.Count;

        /// <summary>Fügt ein Monster zur Datenbank hinzu.</summary>
        public void Add(Monster monster)
        {
            if (monster == null) throw new ArgumentNullException(nameof(monster));
            _byId[monster.Id] = monster;
            _byName[monster.Name] = monster;
        }

        /// <summary>Entfernt ein Monster aus der Datenbank.</summary>
        public bool Remove(int id)
        {
            if (!_byId.TryGetValue(id, out var monster)) return false;
            _byId.Remove(id);
            _byName.Remove(monster.Name);
            return true;
        }

        /// <summary>Holt ein Monster anhand der ID.</summary>
        public Monster? GetMonster(int id) => _byId.TryGetValue(id, out var m) ? m : null;

        /// <summary>Holt ein Monster anhand des Namens (case-insensitiv).</summary>
        public Monster? GetMonster(string name) => _byName.TryGetValue(name, out var m) ? m : null;

        /// <summary>Gibt alle Monster zurück.</summary>
        public IReadOnlyList<Monster> GetAllMonsters() => _byId.Values.ToList().AsReadOnly();

        /// <summary>Filtert Monster nach Element.</summary>
        public IReadOnlyList<Monster> GetByElement(MonsterElement element) =>
            _byId.Values.Where(m => m.Element == element).ToList().AsReadOnly();

        /// <summary>Filtert Monster nach Rasse.</summary>
        public IReadOnlyList<Monster> GetByRace(MonsterRace race) =>
            _byId.Values.Where(m => m.Race == race).ToList().AsReadOnly();

        /// <summary>Filtert Monster nach Größe.</summary>
        public IReadOnlyList<Monster> GetBySize(MonsterSize size) =>
            _byId.Values.Where(m => m.Size == size).ToList().AsReadOnly();

        /// <summary>Gibt alle MVP-Monster zurück.</summary>
        public IReadOnlyList<Monster> GetMvps() =>
            _byId.Values.Where(m => m.IsMvp).ToList().AsReadOnly();

        /// <summary>Gibt alle Minibosse zurück.</summary>
        public IReadOnlyList<Monster> GetMinibosses() =>
            _byId.Values.Where(m => m.IsMiniboss).ToList().AsReadOnly();

        /// <summary>Sucht Monster nach Namen (teilweise, case-insensitiv).</summary>
        public IReadOnlyList<Monster> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return GetAllMonsters();
            var q = query.Trim().ToLowerInvariant();
            return _byId.Values
                .Where(m => m.Name.ToLowerInvariant().Contains(q) ||
                            m.DisplayName.ToLowerInvariant().Contains(q))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>Lädt eine vorkonfigurierte Starter-Datenbank mit klassischen RO-Monstern.</summary>
        public static MonsterDatabase CreateStarterDatabase()
        {
            var db = new MonsterDatabase();

            // ── Poring-Serie ──
            db.Add(new Monster
            {
                Id = 1002,
                Name = "Poring",
                DisplayName = "Poring",
                Element = MonsterElement.Water,
                Size = MonsterSize.Small,
                Race = MonsterRace.Formless,
                PreRenewalHp = 85,
                RenewalHp = 200,
                PreRenewalExp = 25,
                RenewalExp = 60,
                PreRenewalJobExp = 15,
                RenewalJobExp = 35,
                PreRenewalBaseLevel = 2,
                RenewalBaseLevel = 3,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 500 }, { 7143, 50 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 600 }, { 7143, 100 } },
                SpawnMaps = new List<string> { "pay_fild01", "prt_fild01", "gef_fild01" },
                Description = "Der klassische Schleim-Anfänger-Monster. Sehr verbreitet in Anfängerzonen."
            });

            db.Add(new Monster
            {
                Id = 1050,
                Name = "Drops",
                DisplayName = "Drops",
                Element = MonsterElement.Earth,
                Size = MonsterSize.Small,
                Race = MonsterRace.Formless,
                IsMiniboss = true,
                PreRenewalHp = 1200,
                RenewalHp = 2500,
                PreRenewalExp = 1500,
                RenewalExp = 3000,
                PreRenewalJobExp = 800,
                RenewalJobExp = 1600,
                PreRenewalBaseLevel = 15,
                RenewalBaseLevel = 20,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1000 }, { 7143, 500 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1200 }, { 7143, 800 } },
                SpawnMaps = new List<string> { "pay_fild03", "prt_fild03" },
                Description = "Miniboss-Version von Poring mit mehr HP und besseren Drops."
            });

            // ── Poporing ──
            db.Add(new Monster
            {
                Id = 1012,
                Name = "Poporing",
                DisplayName = "Poporing",
                Element = MonsterElement.Water,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Formless,
                PreRenewalHp = 400,
                RenewalHp = 900,
                PreRenewalExp = 400,
                RenewalExp = 950,
                PreRenewalJobExp = 250,
                RenewalJobExp = 550,
                PreRenewalBaseLevel = 8,
                RenewalBaseLevel = 12,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 800 }, { 7143, 100 }, { 7239, 10 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1000 }, { 7143, 150 }, { 7239, 25 } },
                SpawnMaps = new List<string> { "pay_fild01", "pay_fild02" },
                Description = "Entwickelte Poring-Form mit höherem Level."
            });

            // ── Marin ──
            db.Add(new Monster
            {
                Id = 1030,
                Name = "Marin",
                DisplayName = "Marin",
                Element = MonsterElement.Wind,
                Size = MonsterSize.Small,
                Race = MonsterRace.Insect,
                PreRenewalHp = 320,
                RenewalHp = 750,
                PreRenewalExp = 320,
                RenewalExp = 700,
                PreRenewalJobExp = 200,
                RenewalJobExp = 450,
                PreRenewalBaseLevel = 7,
                RenewalBaseLevel = 10,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 600 }, { 7229, 50 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 750 }, { 7229, 80 } },
                SpawnMaps = new List<string> { "pay_fild01", "pay_arche" },
                Description = "Fliegendes Insekten-Monster an Küsten."
            });

            // ── Fabre ──
            db.Add(new Monster
            {
                Id = 1086,
                Name = "Fabre",
                DisplayName = "Fabre",
                Element = MonsterElement.Earth,
                Size = MonsterSize.Small,
                Race = MonsterRace.Insect,
                PreRenewalHp = 450,
                RenewalHp = 1100,
                PreRenewalExp = 450,
                RenewalExp = 1050,
                PreRenewalJobExp = 280,
                RenewalJobExp = 620,
                PreRenewalBaseLevel = 10,
                RenewalBaseLevel = 14,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 900 }, { 7230, 100 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1100 }, { 7230, 120 } },
                SpawnMaps = new List<string> { "pay_fild02", "pay_fild03" },
                Description = "Käfer-Monster in Feldern."
            });

            // ── Picky ──
            db.Add(new Monster
            {
                Id = 1113,
                Name = "Picky",
                DisplayName = "Picky",
                Element = MonsterElement.Fire,
                Size = MonsterSize.Small,
                Race = MonsterRace.Plant,
                PreRenewalHp = 380,
                RenewalHp = 900,
                PreRenewalExp = 380,
                RenewalExp = 900,
                PreRenewalJobExp = 240,
                RenewalJobExp = 540,
                PreRenewalBaseLevel = 9,
                RenewalBaseLevel = 13,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 700 }, { 7144, 80 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 900 }, { 7144, 120 } },
                SpawnMaps = new List<string> { "pay_fild04", "pay_fild05" },
                Description = "Pflanzen-Monster mit Feuer-Element."
            });

            // ── Bapho Jr. ──
            db.Add(new Monster
            {
                Id = 1087,
                Name = "Bapho Jr.",
                DisplayName = "Bapho Jr.",
                Element = MonsterElement.Shadow,
                Size = MonsterSize.Small,
                Race = MonsterRace.DemiHuman,
                PreRenewalHp = 1500,
                RenewalHp = 3500,
                PreRenewalExp = 2200,
                RenewalExp = 5000,
                PreRenewalJobExp = 1200,
                RenewalJobExp = 2800,
                PreRenewalBaseLevel = 25,
                RenewalBaseLevel = 32,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1500 }, { 7169, 200 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1800 }, { 7169, 300 } },
                SpawnMaps = new List<string> { "pay_fild06", "prt_fild05" },
                Description = "Kleiner Version von Baphomet Jr."
            });

            // ── Orc Warrior ──
            db.Add(new Monster
            {
                Id = 1096,
                Name = "Orc Warrior",
                DisplayName = "Orc Warrior",
                Element = MonsterElement.Earth,
                Size = MonsterSize.Medium,
                Race = MonsterRace.DemiHuman,
                PreRenewalHp = 2500,
                RenewalHp = 6000,
                PreRenewalExp = 5000,
                RenewalExp = 12000,
                PreRenewalJobExp = 2800,
                RenewalJobExp = 7000,
                PreRenewalBaseLevel = 35,
                RenewalBaseLevel = 42,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 2000 }, { 2201, 100 }, { 7224, 200 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2500 }, { 2201, 150 }, { 7224, 250 } },
                SpawnMaps = new List<string> { "pay_fild07", "prt_fild06" },
                Description = "Orc-Krieger in Dungeon-Nähe."
            });

            // ── Zombie ──
            db.Add(new Monster
            {
                Id = 1046,
                Name = "Zombie",
                DisplayName = "Zombie",
                Element = MonsterElement.Undead,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Undead,
                PreRenewalHp = 1800,
                RenewalHp = 4200,
                PreRenewalExp = 3500,
                RenewalExp = 8000,
                PreRenewalJobExp = 2000,
                RenewalJobExp = 4500,
                PreRenewalBaseLevel = 28,
                RenewalBaseLevel = 35,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1800 }, { 6107, 300 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2200 }, { 6107, 400 } },
                SpawnMaps = new List<string> { "pay_dun00", "pay_dun01" },
                Description = "Untoter in Dungeons."
            });

            // ── Lord Knight MVP ──
            db.Add(new Monster
            {
                Id = 1301,
                Name = "Lord Knight",
                DisplayName = "Lord Knight [MVP]",
                Element = MonsterElement.Earth,
                Size = MonsterSize.Large,
                Race = MonsterRace.DemiHuman,
                IsMvp = true,
                PreRenewalHp = 4500000,
                RenewalHp = 12000000,
                PreRenewalExp = 500000,
                RenewalExp = 1500000,
                PreRenewalJobExp = 250000,
                RenewalJobExp = 800000,
                PreRenewalBaseLevel = 99,
                RenewalBaseLevel = 150,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 5000 }, { 2312, 500 }, { 7189, 100 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 8000 }, { 2312, 700 }, { 7189, 200 } },
                SpawnMaps = new List<string> { "pay_fild08" },
                Description = "MVP-Boss – Lord Knight."
            });

            // ── Atroce ──
            db.Add(new Monster
            {
                Id = 1059,
                Name = "Atroce",
                DisplayName = "Atroce",
                Element = MonsterElement.Undead,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Undead,
                PreRenewalHp = 3200,
                RenewalHp = 7500,
                PreRenewalExp = 6000,
                RenewalExp = 14000,
                PreRenewalJobExp = 3400,
                RenewalJobExp = 8000,
                PreRenewalBaseLevel = 42,
                RenewalBaseLevel = 50,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 2500 }, { 6107, 500 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 3000 }, { 6107, 600 } },
                SpawnMaps = new List<string> { "pay_dun01", "pay_dun02" },
                Description = "Untoter Krieger in Payon-Dungeon."
            });

            return db;
        }
    }
}
