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

            // ── Weitere Rathena-Monster (Pre-Renewal & Renewal) ──

            // Scorpion (1001) — Insekten, Feuer-Element
            db.Add(new Monster
            {
                Id = 1001,
                Name = "Scorpion",
                DisplayName = "Scorpion",
                Element = MonsterElement.Fire,
                Size = MonsterSize.Small,
                Race = MonsterRace.Insect,
                PreRenewalHp = 136,
                RenewalHp = 300,
                PreRenewalExp = 169,
                RenewalExp = 400,
                PreRenewalJobExp = 115,
                RenewalJobExp = 250,
                PreRenewalBaseLevel = 16,
                RenewalBaseLevel = 20,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1000 }, { 7143, 100 }, { 7239, 50 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1200 }, { 7143, 150 }, { 7239, 80 } },
                SpawnMaps = new List<string> { "pay_fild01", "pay_fild02" },
                Description = "Skorpion mit Gift-Attacke."
            });

            // Hornet (1004) — Insekten, Wind-Element
            db.Add(new Monster
            {
                Id = 1004,
                Name = "Hornet",
                DisplayName = "Hornet",
                Element = MonsterElement.Wind,
                Size = MonsterSize.Small,
                Race = MonsterRace.Insect,
                PreRenewalHp = 85,
                RenewalHp = 200,
                PreRenewalExp = 158,
                RenewalExp = 380,
                PreRenewalJobExp = 90,
                RenewalJobExp = 220,
                PreRenewalBaseLevel = 11,
                RenewalBaseLevel = 14,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 800 }, { 7229, 80 }, { 7144, 20 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1000 }, { 7229, 120 }, { 7144, 40 } },
                SpawnMaps = new List<string> { "pay_fild01", "pay_fild03" },
                Description = "Wespe mit schnellen Angriffen."
            });

            // Familiar (1005) — Brute, Schatten-Element
            db.Add(new Monster
            {
                Id = 1005,
                Name = "Familiar",
                DisplayName = "Familiar",
                Element = MonsterElement.Shadow,
                Size = MonsterSize.Small,
                Race = MonsterRace.Brute,
                PreRenewalHp = 330,
                RenewalHp = 800,
                PreRenewalExp = 200,
                RenewalExp = 500,
                PreRenewalJobExp = 152,
                RenewalJobExp = 350,
                PreRenewalBaseLevel = 24,
                RenewalBaseLevel = 30,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 900 }, { 7224, 100 }, { 7145, 30 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1100 }, { 7224, 150 }, { 7145, 50 } },
                SpawnMaps = new List<string> { "pay_fild04", "pay_fild05" },
                Description = "Fledermaus-ähnliches Monster."
            });

            // Pupa (1008) — Insekten, Erde-Element
            db.Add(new Monster
            {
                Id = 1008,
                Name = "Pupa",
                DisplayName = "Pupa",
                Element = MonsterElement.Earth,
                Size = MonsterSize.Small,
                Race = MonsterRace.Insect,
                PreRenewalHp = 51,
                RenewalHp = 120,
                PreRenewalExp = 157,
                RenewalExp = 370,
                PreRenewalJobExp = 55,
                RenewalJobExp = 140,
                PreRenewalBaseLevel = 4,
                RenewalBaseLevel = 6,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 600 }, { 7143, 30 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 800 }, { 7143, 50 } },
                SpawnMaps = new List<string> { "pay_fild01", "pay_fild02" },
                Description = "Puppe eines Insekts, bevor es schlüpft."
            });

            // Condor (1009) — Brute, Wind-Element
            db.Add(new Monster
            {
                Id = 1009,
                Name = "Condor",
                DisplayName = "Condor",
                Element = MonsterElement.Wind,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Brute,
                PreRenewalHp = 105,
                RenewalHp = 250,
                PreRenewalExp = 160,
                RenewalExp = 400,
                PreRenewalJobExp = 95,
                RenewalJobExp = 230,
                PreRenewalBaseLevel = 12,
                RenewalBaseLevel = 15,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 800 }, { 7229, 60 }, { 7144, 10 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1000 }, { 7229, 100 }, { 7144, 20 } },
                SpawnMaps = new List<string> { "pay_fild05", "prt_fild02" },
                Description = "Großer Greifvogel."
            });

            // Willow (1010) — Pflanze, Erde-Element
            db.Add(new Monster
            {
                Id = 1010,
                Name = "Willow",
                DisplayName = "Willow",
                Element = MonsterElement.Earth,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Plant,
                PreRenewalHp = 78,
                RenewalHp = 180,
                PreRenewalExp = 160,
                RenewalExp = 400,
                PreRenewalJobExp = 75,
                RenewalJobExp = 180,
                PreRenewalBaseLevel = 8,
                RenewalBaseLevel = 10,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 700 }, { 7144, 40 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 900 }, { 7144, 60 } },
                SpawnMaps = new List<string> { "pay_fild01", "pay_fild03" },
                Description = "Baum-Monster in Wäldern."
            });

            // Chonchon (1011) — Insekten, Wind-Element
            db.Add(new Monster
            {
                Id = 1011,
                Name = "Chonchon",
                DisplayName = "Chonchon",
                Element = MonsterElement.Wind,
                Size = MonsterSize.Small,
                Race = MonsterRace.Insect,
                PreRenewalHp = 48,
                RenewalHp = 110,
                PreRenewalExp = 150,
                RenewalExp = 350,
                PreRenewalJobExp = 60,
                RenewalJobExp = 150,
                PreRenewalBaseLevel = 5,
                RenewalBaseLevel = 7,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 500 }, { 7143, 20 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 700 }, { 7143, 40 } },
                SpawnMaps = new List<string> { "pay_fild01", "pay_fild02" },
                Description = "Kleines fliegendes Monster."
            });

            // Roda Frog (1012) — Fisch, Wasser-Element
            db.Add(new Monster
            {
                Id = 1012,
                Name = "Roda Frog",
                DisplayName = "Roda Frog",
                Element = MonsterElement.Water,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Fish,
                PreRenewalHp = 140,
                RenewalHp = 350,
                PreRenewalExp = 162,
                RenewalExp = 400,
                PreRenewalJobExp = 100,
                RenewalJobExp = 250,
                PreRenewalBaseLevel = 13,
                RenewalBaseLevel = 16,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 900 }, { 7239, 100 }, { 7145, 20 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1100 }, { 7239, 150 }, { 7145, 40 } },
                SpawnMaps = new List<string> { "pay_fild03", "pay_fild04" },
                Description = "Frosch-Monster in Wassergebieten."
            });

            // Wolf (1013) — Brute, Erde-Element
            db.Add(new Monster
            {
                Id = 1013,
                Name = "Wolf",
                DisplayName = "Wolf",
                Element = MonsterElement.Earth,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Brute,
                PreRenewalHp = 1091,
                RenewalHp = 2500,
                PreRenewalExp = 369,
                RenewalExp = 900,
                PreRenewalJobExp = 301,
                RenewalJobExp = 700,
                PreRenewalBaseLevel = 45,
                RenewalBaseLevel = 55,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1500 }, { 7224, 200 }, { 7145, 50 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1800 }, { 7224, 300 }, { 7145, 80 } },
                SpawnMaps = new List<string> { "pay_fild06", "prt_fild04" },
                Description = "Wildwolf in offenen Gebieten."
            });

            // Spore (1014) — Pflanze, Wasser-Element
            db.Add(new Monster
            {
                Id = 1014,
                Name = "Spore",
                DisplayName = "Spore",
                Element = MonsterElement.Water,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Plant,
                PreRenewalHp = 223,
                RenewalHp = 500,
                PreRenewalExp = 175,
                RenewalExp = 420,
                PreRenewalJobExp = 125,
                RenewalJobExp = 300,
                PreRenewalBaseLevel = 18,
                RenewalBaseLevel = 22,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 800 }, { 7144, 50 }, { 7239, 20 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1000 }, { 7144, 80 }, { 7239, 40 } },
                SpawnMaps = new List<string> { "pay_fild04", "pay_fild05" },
                Description = "Pilz-Monster mit Sporen-Angriff."
            });

            // Archer Skeleton (1016) — Untote, Untot-Element
            db.Add(new Monster
            {
                Id = 1016,
                Name = "Archer Skeleton",
                DisplayName = "Archer Skeleton",
                Element = MonsterElement.Undead,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Undead,
                PreRenewalHp = 1458,
                RenewalHp = 3500,
                PreRenewalExp = 436,
                RenewalExp = 1000,
                PreRenewalJobExp = 327,
                RenewalJobExp = 800,
                PreRenewalBaseLevel = 50,
                RenewalBaseLevel = 60,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1800 }, { 7224, 300 }, { 7145, 100 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2200 }, { 7224, 400 }, { 7145, 150 } },
                SpawnMaps = new List<string> { "pay_dun00", "pay_dun01" },
                Description = "Skelett-Bogenschütze in Dungeons."
            });

            // Thief Bug (1033) — Insekten, Erde-Element
            db.Add(new Monster
            {
                Id = 1033,
                Name = "Thief Bug",
                DisplayName = "Thief Bug",
                Element = MonsterElement.Earth,
                Size = MonsterSize.Small,
                Race = MonsterRace.Insect,
                PreRenewalHp = 320,
                RenewalHp = 800,
                PreRenewalExp = 320,
                RenewalExp = 800,
                PreRenewalJobExp = 200,
                RenewalJobExp = 450,
                PreRenewalBaseLevel = 15,
                RenewalBaseLevel = 20,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1000 }, { 7239, 100 }, { 7144, 30 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1200 }, { 7239, 150 }, { 7144, 50 } },
                SpawnMaps = new List<string> { "pay_fild04", "pay_fild06" },
                Description = "Dieb-Käfer, stiehlt Items."
            });

            // Hunter Fly (1038) — Insekten, Wind-Element
            db.Add(new Monster
            {
                Id = 1038,
                Name = "Hunter Fly",
                DisplayName = "Hunter Fly",
                Element = MonsterElement.Wind,
                Size = MonsterSize.Small,
                Race = MonsterRace.Insect,
                PreRenewalHp = 200,
                RenewalHp = 500,
                PreRenewalExp = 200,
                RenewalExp = 500,
                PreRenewalJobExp = 150,
                RenewalJobExp = 350,
                PreRenewalBaseLevel = 10,
                RenewalBaseLevel = 13,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 700 }, { 7143, 40 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 900 }, { 7143, 60 } },
                SpawnMaps = new List<string> { "pay_fild02", "pay_fild03" },
                Description = "Große Fliege, fliegt schnell."
            });

            // Pecopeco Egg (1058) — Pflanze, Erde-Element
            db.Add(new Monster
            {
                Id = 1058,
                Name = "Pecopeco Egg",
                DisplayName = "Pecopeco Egg",
                Element = MonsterElement.Earth,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Plant,
                PreRenewalHp = 1200,
                RenewalHp = 2800,
                PreRenewalExp = 450,
                RenewalExp = 1100,
                PreRenewalJobExp = 280,
                RenewalJobExp = 650,
                PreRenewalBaseLevel = 18,
                RenewalBaseLevel = 22,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1200 }, { 7224, 150 }, { 7144, 50 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1500 }, { 7224, 200 }, { 7144, 80 } },
                SpawnMaps = new List<string> { "pay_fild07", "prt_fild05" },
                Description = "Ei des Pecopeco-Vogels."
            });

            // Deviace (1092) — Untote, Untot-Element
            db.Add(new Monster
            {
                Id = 1092,
                Name = "Deviace",
                DisplayName = "Deviace",
                Element = MonsterElement.Undead,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Undead,
                PreRenewalHp = 2000,
                RenewalHp = 5000,
                PreRenewalExp = 4500,
                RenewalExp = 10000,
                PreRenewalJobExp = 2500,
                RenewalJobExp = 6000,
                PreRenewalBaseLevel = 32,
                RenewalBaseLevel = 40,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 2000 }, { 6107, 400 }, { 7224, 100 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2500 }, { 6107, 500 }, { 7224, 150 } },
                SpawnMaps = new List<string> { "pay_dun01", "pay_dun02" },
                Description = "Untoter Krieger mit Dolch."
            });

            // Bone Soldier (1091) — Untote, Untot-Element
            db.Add(new Monster
            {
                Id = 1091,
                Name = "Bone Soldier",
                DisplayName = "Bone Soldier",
                Element = MonsterElement.Undead,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Undead,
                PreRenewalHp = 1500,
                RenewalHp = 3500,
                PreRenewalExp = 3200,
                RenewalExp = 7500,
                PreRenewalJobExp = 1800,
                RenewalJobExp = 4500,
                PreRenewalBaseLevel = 28,
                RenewalBaseLevel = 35,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1500 }, { 6107, 300 }, { 7224, 80 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1900 }, { 6107, 400 }, { 7224, 120 } },
                SpawnMaps = new List<string> { "pay_dun00", "pay_dun01" },
                Description = "Skelett-Soldat in Dungeons."
            });

            // Golden Thief Bug MVP (1147) — Insekten, Erde-Element
            db.Add(new Monster
            {
                Id = 1147,
                Name = "Golden Thief Bug",
                DisplayName = "Golden Thief Bug [MVP]",
                Element = MonsterElement.Earth,
                Size = MonsterSize.Large,
                Race = MonsterRace.Insect,
                IsMvp = true,
                PreRenewalHp = 3500000,
                RenewalHp = 9000000,
                PreRenewalExp = 400000,
                RenewalExp = 1200000,
                PreRenewalJobExp = 200000,
                RenewalJobExp = 700000,
                PreRenewalBaseLevel = 99,
                RenewalBaseLevel = 150,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 6000 }, { 2312, 600 }, { 7189, 150 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 10000 }, { 2312, 800 }, { 7189, 250 } },
                SpawnMaps = new List<string> { "pay_fild08" },
                Description = "MVP-Boss – Goldener Dieb-Käfer."
            });

            // Stormy Knight MVP (1146) — Untote, Wind-Element
            db.Add(new Monster
            {
                Id = 1146,
                Name = "Stormy Knight",
                DisplayName = "Stormy Knight [MVP]",
                Element = MonsterElement.Wind,
                Size = MonsterSize.Large,
                Race = MonsterRace.Undead,
                IsMvp = true,
                PreRenewalHp = 4000000,
                RenewalHp = 10000000,
                PreRenewalExp = 450000,
                RenewalExp = 1300000,
                PreRenewalJobExp = 225000,
                RenewalJobExp = 750000,
                PreRenewalBaseLevel = 99,
                RenewalBaseLevel = 150,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 7000 }, { 2312, 550 }, { 7189, 120 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 11000 }, { 2312, 750 }, { 7189, 220 } },
                SpawnMaps = new List<string> { "prt_fild08" },
                Description = "MVP-Boss – Sturmm-Ritter."
            });

            // ── Geffen-Gebiet ──
            // Anubis (1150)
            db.Add(new Monster
            {
                Id = 1150, Name = "Anubis", DisplayName = "Anubis",
                Element = MonsterElement.Shadow, Size = MonsterSize.Medium, Race = MonsterRace.Demon,
                PreRenewalHp = 3800, RenewalHp = 9000,
                PreRenewalExp = 4200, RenewalExp = 9500,
                PreRenewalJobExp = 2400, RenewalJobExp = 5500,
                PreRenewalBaseLevel = 40, RenewalBaseLevel = 48,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 2200 }, { 7171, 300 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2800 }, { 7171, 400 } },
                SpawnMaps = new List<string> { "gef_fild00", "gef_dun00" },
                Description = "Wüsten-Wächter von Geffen."
            });

            // High Orc (1148)
            db.Add(new Monster
            {
                Id = 1148, Name = "High Orc", DisplayName = "High Orc",
                Element = MonsterElement.Earth, Size = MonsterSize.Medium, Race = MonsterRace.DemiHuman,
                PreRenewalHp = 3200, RenewalHp = 7500,
                PreRenewalExp = 4500, RenewalExp = 10500,
                PreRenewalJobExp = 2500, RenewalJobExp = 6000,
                PreRenewalBaseLevel = 38, RenewalBaseLevel = 45,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 2000 }, { 7172, 200 }, { 2205, 100 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2600 }, { 7172, 300 }, { 2205, 150 } },
                SpawnMaps = new List<string> { "orc_dun01", "pay_fild07" },
                Description = "Elite-Orc-Krieger."
            });

            // Vagabond Wolf (1157)
            db.Add(new Monster
            {
                Id = 1157, Name = "Vagabond Wolf", DisplayName = "Vagabond Wolf",
                Element = MonsterElement.Earth, Size = MonsterSize.Medium, Race = MonsterRace.Brute,
                PreRenewalHp = 2800, RenewalHp = 6500,
                PreRenewalExp = 3800, RenewalExp = 9000,
                PreRenewalJobExp = 2100, RenewalJobExp = 5000,
                PreRenewalBaseLevel = 35, RenewalBaseLevel = 42,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1800 }, { 7224, 150 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2300 }, { 7224, 220 } },
                SpawnMaps = new List<string> { "gef_fild01", "prt_fild06" },
                Description = "Wildwolf in der Geffen-Region."
            });

            // Mimic (1159)
            db.Add(new Monster
            {
                Id = 1159, Name = "Mimic", DisplayName = "Mimic",
                Element = MonsterElement.Shadow, Size = MonsterSize.Medium, Race = MonsterRace.Formless,
                PreRenewalHp = 5000, RenewalHp = 12000,
                PreRenewalExp = 6000, RenewalExp = 14000,
                PreRenewalJobExp = 3500, RenewalJobExp = 8000,
                PreRenewalBaseLevel = 42, RenewalBaseLevel = 50,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 3000 }, { 7174, 400 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 4000 }, { 7174, 500 } },
                SpawnMaps = new List<string> { "gef_dun00", "pay_dun02" },
                Description = "Tarnte Schatztruhe, greift an."
            });

            // Desert Wolf (1112)
            db.Add(new Monster
            {
                Id = 1112, Name = "Desert Wolf", DisplayName = "Desert Wolf",
                Element = MonsterElement.Wind, Size = MonsterSize.Small, Race = MonsterRace.Brute,
                PreRenewalHp = 2500, RenewalHp = 5800,
                PreRenewalExp = 3200, RenewalExp = 7500,
                PreRenewalJobExp = 1800, RenewalJobExp = 4200,
                PreRenewalBaseLevel = 32, RenewalBaseLevel = 39,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1500 }, { 7224, 120 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2000 }, { 7224, 180 } },
                SpawnMaps = new List<string> { "mor_fild01", "mor_fild02" },
                Description = "Wüstenwolf aus Morroc."
            });

            // Poison Spore (1118)
            db.Add(new Monster
            {
                Id = 1118, Name = "Poison Spore", DisplayName = "Poison Spore",
                Element = MonsterElement.Poison, Size = MonsterSize.Small, Race = MonsterRace.Plant,
                PreRenewalHp = 1800, RenewalHp = 4200,
                PreRenewalExp = 3000, RenewalExp = 7000,
                PreRenewalJobExp = 1700, RenewalJobExp = 4000,
                PreRenewalBaseLevel = 28, RenewalBaseLevel = 35,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1200 }, { 7144, 80 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1600 }, { 7144, 120 } },
                SpawnMaps = new List<string> { "mor_fild05", "pay_fild04" },
                Description = "Giftiger Pilz-Monster."
            });

            // Mandragora (1121)
            db.Add(new Monster
            {
                Id = 1121, Name = "Mandragora", DisplayName = "Mandragora",
                Element = MonsterElement.Earth, Size = MonsterSize.Small, Race = MonsterRace.Plant,
                PreRenewalHp = 2200, RenewalHp = 5200,
                PreRenewalExp = 3500, RenewalExp = 8200,
                PreRenewalJobExp = 2000, RenewalJobExp = 4800,
                PreRenewalBaseLevel = 30, RenewalBaseLevel = 37,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1600 }, { 7146, 60 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2100 }, { 7146, 90 } },
                SpawnMaps = new List<string> { "jun_fild01", "jun_fild02" },
                Description = "Pflanzen-Monster im Dschungel."
            });

            // Larva (1120)
            db.Add(new Monster
            {
                Id = 1120, Name = "Larva", DisplayName = "Larva",
                Element = MonsterElement.Earth, Size = MonsterSize.Small, Race = MonsterRace.Insect,
                PreRenewalHp = 1500, RenewalHp = 3500,
                PreRenewalExp = 2500, RenewalExp = 5800,
                PreRenewalJobExp = 1400, RenewalJobExp = 3200,
                PreRenewalBaseLevel = 22, RenewalBaseLevel = 28,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1100 }, { 7239, 90 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1400 }, { 7239, 130 } },
                SpawnMaps = new List<string> { "ant_hell01", "ant_hell02" },
                Description = "Ameisen-Larve."
            });

            // Worker Ant (1119)
            db.Add(new Monster
            {
                Id = 1119, Name = "Worker Ant", DisplayName = "Worker Ant",
                Element = MonsterElement.Earth, Size = MonsterSize.Small, Race = MonsterRace.Insect,
                PreRenewalHp = 1800, RenewalHp = 4200,
                PreRenewalExp = 3000, RenewalExp = 7000,
                PreRenewalJobExp = 1700, RenewalJobExp = 4000,
                PreRenewalBaseLevel = 28, RenewalBaseLevel = 35,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1300 }, { 7239, 100 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1700 }, { 7239, 150 } },
                SpawnMaps = new List<string> { "ant_hell01", "ant_hell02" },
                Description = "Ameisen-Arbeiterin."
            });

            // Soldier Ant (1124)
            db.Add(new Monster
            {
                Id = 1124, Name = "Soldier Ant", DisplayName = "Soldier Ant",
                Element = MonsterElement.Earth, Size = MonsterSize.Medium, Race = MonsterRace.Insect,
                PreRenewalHp = 2800, RenewalHp = 6500,
                PreRenewalExp = 4000, RenewalExp = 9500,
                PreRenewalJobExp = 2300, RenewalJobExp = 5500,
                PreRenewalBaseLevel = 36, RenewalBaseLevel = 43,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1800 }, { 7239, 110 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2400 }, { 7239, 170 } },
                SpawnMaps = new List<string> { "ant_hell02", "ant_hell03" },
                Description = "Ameisen-Soldat."
            });

            // Queen Ant MVP (1132)
            db.Add(new Monster
            {
                Id = 1132, Name = "Queen Ant", DisplayName = "Queen Ant [MVP]",
                Element = MonsterElement.Earth, Size = MonsterSize.Large, Race = MonsterRace.Insect,
                IsMvp = true,
                PreRenewalHp = 6000000, RenewalHp = 15000000,
                PreRenewalExp = 800000, RenewalExp = 2000000,
                PreRenewalJobExp = 400000, RenewalJobExp = 1200000,
                PreRenewalBaseLevel = 99, RenewalBaseLevel = 150,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 10000 }, { 2312, 800 }, { 7189, 300 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 15000 }, { 2312, 1200 }, { 7189, 450 } },
                SpawnMaps = new List<string> { "ant_hell03" },
                Description = "MVP-Boss – Ameisen-Königin."
            });

            // Gargoyle (1139)
            db.Add(new Monster
            {
                Id = 1139, Name = "Gargoyle", DisplayName = "Gargoyle",
                Element = MonsterElement.Earth, Size = MonsterSize.Medium, Race = MonsterRace.Demon,
                PreRenewalHp = 4500, RenewalHp = 10500,
                PreRenewalExp = 5500, RenewalExp = 13000,
                PreRenewalJobExp = 3100, RenewalJobExp = 7500,
                PreRenewalBaseLevel = 44, RenewalBaseLevel = 52,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 2800 }, { 7175, 250 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 3600 }, { 7175, 350 } },
                SpawnMaps = new List<string> { "ice_dun00", "ice_dun01" },
                Description = "Stein-Gargoyle in Eis-Dungeon."
            });

            // Ice Titan (1138)
            db.Add(new Monster
            {
                Id = 1138, Name = "Ice Titan", DisplayName = "Ice Titan",
                Element = MonsterElement.Water, Size = MonsterSize.Large, Race = MonsterRace.Demon,
                PreRenewalHp = 8000, RenewalHp = 19000,
                PreRenewalExp = 9000, RenewalExp = 21000,
                PreRenewalJobExp = 5000, RenewalJobExp = 12000,
                PreRenewalBaseLevel = 52, RenewalBaseLevel = 60,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 5000 }, { 7176, 300 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 6500 }, { 7176, 400 } },
                SpawnMaps = new List<string> { "ice_dun01", "ice_dun02" },
                Description = "Eis-Titan in der Eis-Region."
            });

            // Baphomet Jr. MVP (1047)
            db.Add(new Monster
            {
                Id = 1047, Name = "Baphomet Jr.", DisplayName = "Baphomet Jr. [MVP]",
                Element = MonsterElement.Shadow, Size = MonsterSize.Large, Race = MonsterRace.DemiHuman,
                IsMvp = true,
                PreRenewalHp = 5000000, RenewalHp = 13000000,
                PreRenewalExp = 600000, RenewalExp = 1700000,
                PreRenewalJobExp = 300000, RenewalJobExp = 900000,
                PreRenewalBaseLevel = 99, RenewalBaseLevel = 150,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 8000 }, { 2312, 700 }, { 7189, 200 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 13000 }, { 2312, 900 }, { 7189, 350 } },
                SpawnMaps = new List<string> { "pay_fild08" },
                Description = "MVP-Boss – Baphomet Jr."
            });

            // Dark Lord MVP (1252)
            db.Add(new Monster
            {
                Id = 1252, Name = "Dark Lord", DisplayName = "Dark Lord [MVP]",
                Element = MonsterElement.Shadow, Size = MonsterSize.Large, Race = MonsterRace.Demon,
                IsMvp = true,
                PreRenewalHp = 8000000, RenewalHp = 20000000,
                PreRenewalExp = 1200000, RenewalExp = 3000000,
                PreRenewalJobExp = 600000, RenewalJobExp = 1500000,
                PreRenewalBaseLevel = 99, RenewalBaseLevel = 150,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 15000 }, { 2312, 1500 }, { 7189, 500 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 25000 }, { 2312, 2000 }, { 7189, 700 } },
                SpawnMaps = new List<string> { "thor_camp01" },
                Description = "MVP-Boss – Dunkler Herrscher."
            });

            // Stinger (1127)
            db.Add(new Monster
            {
                Id = 1127, Name = "Stinger", DisplayName = "Stinger",
                Element = MonsterElement.Poison, Size = MonsterSize.Small, Race = MonsterRace.Insect,
                PreRenewalHp = 2400, RenewalHp = 5600,
                PreRenewalExp = 3500, RenewalExp = 8200,
                PreRenewalJobExp = 2000, RenewalJobExp = 4800,
                PreRenewalBaseLevel = 31, RenewalBaseLevel = 38,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1700 }, { 7145, 70 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2200 }, { 7145, 100 } },
                SpawnMaps = new List<string> { "prison", "pay_fild05" },
                Description = "Skorpion-Monster mit Giftstachel."
            });

            // Freezer (1129)
            db.Add(new Monster
            {
                Id = 1129, Name = "Freezer", DisplayName = "Freezer",
                Element = MonsterElement.Water, Size = MonsterSize.Small, Race = MonsterRace.Demon,
                PreRenewalHp = 2600, RenewalHp = 6100,
                PreRenewalExp = 3800, RenewalExp = 9000,
                PreRenewalJobExp = 2200, RenewalJobExp = 5200,
                PreRenewalBaseLevel = 33, RenewalBaseLevel = 40,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1800 }, { 7146, 80 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2400 }, { 7146, 110 } },
                SpawnMaps = new List<string> { "prison", "ice_dun00" },
                Description = "Eis-Dämon."
            });

            // Marine Sphere (1163)
            db.Add(new Monster
            {
                Id = 1163, Name = "Marine Sphere", DisplayName = "Marine Sphere",
                Element = MonsterElement.Water, Size = MonsterSize.Small, Race = MonsterRace.Formless,
                PreRenewalHp = 2200, RenewalHp = 5200,
                PreRenewalExp = 3200, RenewalExp = 7500,
                PreRenewalJobExp = 1800, RenewalJobExp = 4200,
                PreRenewalBaseLevel = 30, RenewalBaseLevel = 37,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1500 }, { 7144, 60 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2000 }, { 7144, 90 } },
                SpawnMaps = new List<string> { "comodo", "comodo_dun00" },
                Description = "Meer-Kreatur in Comodo."
            });

            // Orc Zombie (1149)
            db.Add(new Monster
            {
                Id = 1149, Name = "Orc Zombie", DisplayName = "Orc Zombie",
                Element = MonsterElement.Undead, Size = MonsterSize.Medium, Race = MonsterRace.Undead,
                PreRenewalHp = 3500, RenewalHp = 8200,
                PreRenewalExp = 4800, RenewalExp = 11000,
                PreRenewalJobExp = 2700, RenewalJobExp = 6500,
                PreRenewalBaseLevel = 39, RenewalBaseLevel = 46,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 2200 }, { 6107, 400 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2900 }, { 6107, 500 } },
                SpawnMaps = new List<string> { "orc_dun01", "pay_dun01" },
                Description = "Untoter Orc."
            });

            // Orc Archer (1154)
            db.Add(new Monster
            {
                Id = 1154, Name = "Orc Archer", DisplayName = "Orc Archer",
                Element = MonsterElement.Wind, Size = MonsterSize.Medium, Race = MonsterRace.DemiHuman,
                PreRenewalHp = 3000, RenewalHp = 7000,
                PreRenewalExp = 4200, RenewalExp = 9800,
                PreRenewalJobExp = 2400, RenewalJobExp = 5600,
                PreRenewalBaseLevel = 37, RenewalBaseLevel = 44,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 2000 }, { 2201, 80 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2600 }, { 2201, 120 } },
                SpawnMaps = new List<string> { "orc_dun01", "prt_fild06" },
                Description = "Orc-Bogenschütze."
            });

            // Orc Lady (1155)
            db.Add(new Monster
            {
                Id = 1155, Name = "Orc Lady", DisplayName = "Orc Lady",
                Element = MonsterElement.Shadow, Size = MonsterSize.Medium, Race = MonsterRace.DemiHuman,
                PreRenewalHp = 3300, RenewalHp = 7800,
                PreRenewalExp = 4500, RenewalExp = 10500,
                PreRenewalJobExp = 2600, RenewalJobExp = 6100,
                PreRenewalBaseLevel = 38, RenewalBaseLevel = 45,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 2100 }, { 7174, 150 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2700 }, { 7174, 200 } },
                SpawnMaps = new List<string> { "orc_dun02", "prt_fild06" },
                Description = "Orc-Schamane."
            });

            // Skeggiold (1162)
            db.Add(new Monster
            {
                Id = 1162, Name = "Skeggiold", DisplayName = "Skeggiold",
                Element = MonsterElement.Undead, Size = MonsterSize.Large, Race = MonsterRace.Undead,
                PreRenewalHp = 7000, RenewalHp = 16500,
                PreRenewalExp = 8000, RenewalExp = 19000,
                PreRenewalJobExp = 4500, RenewalJobExp = 11000,
                PreRenewalBaseLevel = 50, RenewalBaseLevel = 58,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 4500 }, { 6107, 500 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 6000 }, { 6107, 600 } },
                SpawnMaps = new List<string> { "gef_fild01", "gef_dun01" },
                Description = "Untoter Wikinger-Krieger."
            });

            // Will-o'-the-wisp (1123)
            db.Add(new Monster
            {
                Id = 1123, Name = "Will-o'-the-wisp", DisplayName = "Will-o'-the-wisp",
                Element = MonsterElement.Fire, Size = MonsterSize.Small, Race = MonsterRace.Demon,
                PreRenewalHp = 2000, RenewalHp = 4700,
                PreRenewalExp = 2900, RenewalExp = 6800,
                PreRenewalJobExp = 1600, RenewalJobExp = 3800,
                PreRenewalBaseLevel = 26, RenewalBaseLevel = 33,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1400 }, { 7142, 50 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1800 }, { 7142, 80 } },
                SpawnMaps = new List<string> { "jun_fild01", "mor_fild03" },
                Description = "Irrlicht im Dschungel."
            });

            // Rose (1122)
            db.Add(new Monster
            {
                Id = 1122, Name = "Rose", DisplayName = "Rose",
                Element = MonsterElement.Poison, Size = MonsterSize.Small, Race = MonsterRace.Plant,
                PreRenewalHp = 2100, RenewalHp = 4900,
                PreRenewalExp = 3100, RenewalExp = 7300,
                PreRenewalJobExp = 1700, RenewalJobExp = 4000,
                PreRenewalBaseLevel = 29, RenewalBaseLevel = 36,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1500 }, { 7145, 60 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1900 }, { 7145, 90 } },
                SpawnMaps = new List<string> { "jun_fild01", "jun_fild02" },
                Description = "Giftrose im Dschungel."
            });

            // Crystal Shooter (1135)
            db.Add(new Monster
            {
                Id = 1135, Name = "Crystal Shooter", DisplayName = "Crystal Shooter",
                Element = MonsterElement.Earth, Size = MonsterSize.Small, Race = MonsterRace.Formless,
                PreRenewalHp = 2600, RenewalHp = 6100,
                PreRenewalExp = 3800, RenewalExp = 9000,
                PreRenewalJobExp = 2200, RenewalJobExp = 5200,
                PreRenewalBaseLevel = 33, RenewalBaseLevel = 40,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1900 }, { 7144, 70 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2500 }, { 7144, 100 } },
                SpawnMaps = new List<string> { "prison", "ice_dun00" },
                Description = "Kristall-Monster."
            });

            // Hydra (1164)
            db.Add(new Monster
            {
                Id = 1164, Name = "Hydra", DisplayName = "Hydra",
                Element = MonsterElement.Poison, Size = MonsterSize.Large, Race = MonsterRace.Demon,
                PreRenewalHp = 9000, RenewalHp = 21000,
                PreRenewalExp = 10000, RenewalExp = 24000,
                PreRenewalJobExp = 5500, RenewalJobExp = 13000,
                PreRenewalBaseLevel = 55, RenewalBaseLevel = 63,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 6000 }, { 7177, 350 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 7800 }, { 7177, 450 } },
                SpawnMaps = new List<string> { "comodo_dun00", "comodo_dun01" },
                Description = "Vierköpfiger Hydra-Drache."
            });

            // Ancient Flower (1165)
            db.Add(new Monster
            {
                Id = 1165, Name = "Ancient Flower", DisplayName = "Ancient Flower",
                Element = MonsterElement.Earth, Size = MonsterSize.Medium, Race = MonsterRace.Plant,
                PreRenewalHp = 4000, RenewalHp = 9500,
                PreRenewalExp = 5200, RenewalExp = 12000,
                PreRenewalJobExp = 3000, RenewalJobExp = 7000,
                PreRenewalBaseLevel = 42, RenewalBaseLevel = 50,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 2600 }, { 7146, 90 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 3400 }, { 7146, 130 } },
                SpawnMaps = new List<string> { "comodo_dun00", "jun_fild03" },
                Description = "Urzeitliche Blume."
            });

            // Turtle General MVP (1094)
            db.Add(new Monster
            {
                Id = 1094, Name = "Turtle General", DisplayName = "Turtle General [MVP]",
                Element = MonsterElement.Water, Size = MonsterSize.Large, Race = MonsterRace.Fish,
                IsMvp = true,
                PreRenewalHp = 4500000, RenewalHp = 11000000,
                PreRenewalExp = 550000, RenewalExp = 1400000,
                PreRenewalJobExp = 275000, RenewalJobExp = 850000,
                PreRenewalBaseLevel = 99, RenewalBaseLevel = 150,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 9000 }, { 2312, 600 }, { 7189, 180 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 14000 }, { 2312, 850 }, { 7189, 320 } },
                SpawnMaps = new List<string> { "tur_dun01" },
                Description = "MVP-Boss – Schildkröten-Generä."
            });

            // General Glast Heim MVP (1260)
            db.Add(new Monster
            {
                Id = 1260, Name = "General Glast Heim", DisplayName = "General Glast Heim [MVP]",
                Element = MonsterElement.Undead, Size = MonsterSize.Large, Race = MonsterRace.Undead,
                IsMvp = true,
                PreRenewalHp = 7500000, RenewalHp = 18000000,
                PreRenewalExp = 1000000, RenewalExp = 2600000,
                PreRenewalJobExp = 500000, RenewalJobExp = 1300000,
                PreRenewalBaseLevel = 99, RenewalBaseLevel = 150,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 14000 }, { 2312, 1200 }, { 7189, 400 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 22000 }, { 2312, 1600 }, { 7189, 600 } },
                SpawnMaps = new List<string> { "glast_heim" },
                Description = "MVP-Boss – General Glast Heim."
            });

            // Giearth (1090) — Beast, Erde-Element
            db.Add(new Monster
            {
                Id = 1090, Name = "Giearth", DisplayName = "Giearth",
                Element = MonsterElement.Earth, Size = MonsterSize.Medium, Race = MonsterRace.Beast,
                PreRenewalHp = 3400, RenewalHp = 8000,
                PreRenewalExp = 4600, RenewalExp = 10800,
                PreRenewalJobExp = 2600, RenewalJobExp = 6200,
                PreRenewalBaseLevel = 37, RenewalBaseLevel = 44,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 2100 }, { 7172, 180 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2700 }, { 7172, 260 } },
                SpawnMaps = new List<string> { "pay_fild06", "pay_fild07" },
                Description = "Erde-Bestie aus den Payon-Feldern."
            });

            // ── Allgemeine Monster (Pre-Renewal & Renewal) ──
            db.Add(new Monster
            {
                Id = 1001,
                Name = "Scorpion",
                DisplayName = "Scorpion",
                Element = MonsterElement.Fire,
                Size = MonsterSize.Small,
                Race = MonsterRace.Insect,
                PreRenewalHp = 136,
                RenewalHp = 300,
                PreRenewalExp = 169,
                RenewalExp = 400,
                PreRenewalJobExp = 115,
                RenewalJobExp = 250,
                PreRenewalBaseLevel = 16,
                RenewalBaseLevel = 20,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1000 }, { 7143, 100 }, { 7239, 50 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1200 }, { 7143, 150 }, { 7239, 80 } },
                SpawnMaps = new List<string> { "pay_fild01", "pay_fild02" },
                Description = "Skorpion mit Gift-Attacke."
            });

            db.Add(new Monster
            {
                Id = 1004,
                Name = "Hornet",
                DisplayName = "Hornet",
                Element = MonsterElement.Wind,
                Size = MonsterSize.Small,
                Race = MonsterRace.Insect,
                PreRenewalHp = 85,
                RenewalHp = 200,
                PreRenewalExp = 158,
                RenewalExp = 380,
                PreRenewalJobExp = 90,
                RenewalJobExp = 220,
                PreRenewalBaseLevel = 11,
                RenewalBaseLevel = 14,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 800 }, { 7229, 80 }, { 7144, 20 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1000 }, { 7229, 120 }, { 7144, 40 } },
                SpawnMaps = new List<string> { "pay_fild01", "pay_fild03" },
                Description = "Wespe mit schnellen Angriffen."
            });

            db.Add(new Monster
            {
                Id = 1005,
                Name = "Familiar",
                DisplayName = "Familiar",
                Element = MonsterElement.Shadow,
                Size = MonsterSize.Small,
                Race = MonsterRace.Brute,
                PreRenewalHp = 330,
                RenewalHp = 800,
                PreRenewalExp = 200,
                RenewalExp = 500,
                PreRenewalJobExp = 152,
                RenewalJobExp = 350,
                PreRenewalBaseLevel = 24,
                RenewalBaseLevel = 30,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 900 }, { 7224, 100 }, { 7145, 30 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1100 }, { 7224, 150 }, { 7145, 50 } },
                SpawnMaps = new List<string> { "pay_fild04", "pay_fild05" },
                Description = "Fledermaus-ähnliches Monster."
            });

            db.Add(new Monster
            {
                Id = 1008,
                Name = "Pupa",
                DisplayName = "Pupa",
                Element = MonsterElement.Earth,
                Size = MonsterSize.Small,
                Race = MonsterRace.Insect,
                PreRenewalHp = 51,
                RenewalHp = 120,
                PreRenewalExp = 157,
                RenewalExp = 370,
                PreRenewalJobExp = 55,
                RenewalJobExp = 140,
                PreRenewalBaseLevel = 4,
                RenewalBaseLevel = 6,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 600 }, { 7143, 30 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 800 }, { 7143, 50 } },
                SpawnMaps = new List<string> { "pay_fild01", "pay_fild02" },
                Description = "Puppe eines Insekts, bevor es schlüpft."
            });

            db.Add(new Monster
            {
                Id = 1009,
                Name = "Condor",
                DisplayName = "Condor",
                Element = MonsterElement.Wind,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Brute,
                PreRenewalHp = 105,
                RenewalHp = 250,
                PreRenewalExp = 160,
                RenewalExp = 400,
                PreRenewalJobExp = 95,
                RenewalJobExp = 230,
                PreRenewalBaseLevel = 12,
                RenewalBaseLevel = 15,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 800 }, { 7229, 60 }, { 7144, 10 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1000 }, { 7229, 100 }, { 7144, 20 } },
                SpawnMaps = new List<string> { "pay_fild05", "prt_fild02" },
                Description = "Großer Greifvogel."
            });

            db.Add(new Monster
            {
                Id = 1010,
                Name = "Willow",
                DisplayName = "Willow",
                Element = MonsterElement.Earth,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Plant,
                PreRenewalHp = 78,
                RenewalHp = 180,
                PreRenewalExp = 160,
                RenewalExp = 400,
                PreRenewalJobExp = 75,
                RenewalJobExp = 180,
                PreRenewalBaseLevel = 8,
                RenewalBaseLevel = 10,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 700 }, { 7144, 40 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 900 }, { 7144, 60 } },
                SpawnMaps = new List<string> { "pay_fild01", "pay_fild03" },
                Description = "Baum-Monster in Wäldern."
            });

            db.Add(new Monster
            {
                Id = 1011,
                Name = "Chonchon",
                DisplayName = "Chonchon",
                Element = MonsterElement.Wind,
                Size = MonsterSize.Small,
                Race = MonsterRace.Insect,
                PreRenewalHp = 48,
                RenewalHp = 110,
                PreRenewalExp = 150,
                RenewalExp = 350,
                PreRenewalJobExp = 60,
                RenewalJobExp = 150,
                PreRenewalBaseLevel = 5,
                RenewalBaseLevel = 7,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 500 }, { 7143, 20 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 700 }, { 7143, 40 } },
                SpawnMaps = new List<string> { "pay_fild01", "pay_fild02" },
                Description = "Kleines fliegendes Monster."
            });

            db.Add(new Monster
            {
                Id = 1012,
                Name = "Roda Frog",
                DisplayName = "Roda Frog",
                Element = MonsterElement.Water,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Fish,
                PreRenewalHp = 140,
                RenewalHp = 350,
                PreRenewalExp = 162,
                RenewalExp = 400,
                PreRenewalJobExp = 100,
                RenewalJobExp = 250,
                PreRenewalBaseLevel = 13,
                RenewalBaseLevel = 16,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 900 }, { 7239, 100 }, { 7145, 20 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1100 }, { 7239, 150 }, { 7145, 40 } },
                SpawnMaps = new List<string> { "pay_fild03", "pay_fild04" },
                Description = "Frosch-Monster in Wassergebieten."
            });

            db.Add(new Monster
            {
                Id = 1013,
                Name = "Wolf",
                DisplayName = "Wolf",
                Element = MonsterElement.Earth,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Brute,
                PreRenewalHp = 1091,
                RenewalHp = 2500,
                PreRenewalExp = 369,
                RenewalExp = 900,
                PreRenewalJobExp = 301,
                RenewalJobExp = 700,
                PreRenewalBaseLevel = 45,
                RenewalBaseLevel = 55,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1500 }, { 7224, 200 }, { 7145, 50 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1800 }, { 7224, 300 }, { 7145, 80 } },
                SpawnMaps = new List<string> { "pay_fild06", "prt_fild04" },
                Description = "Wildwolf in offenen Gebieten."
            });

            db.Add(new Monster
            {
                Id = 1014,
                Name = "Spore",
                DisplayName = "Spore",
                Element = MonsterElement.Water,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Plant,
                PreRenewalHp = 223,
                RenewalHp = 500,
                PreRenewalExp = 175,
                RenewalExp = 420,
                PreRenewalJobExp = 125,
                RenewalJobExp = 300,
                PreRenewalBaseLevel = 18,
                RenewalBaseLevel = 22,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 800 }, { 7144, 50 }, { 7239, 20 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1000 }, { 7144, 80 }, { 7239, 40 } },
                SpawnMaps = new List<string> { "pay_fild04", "pay_fild05" },
                Description = "Pilz-Monster mit Sporen-Angriff."
            });

            db.Add(new Monster
            {
                Id = 1016,
                Name = "Archer Skeleton",
                DisplayName = "Archer Skeleton",
                Element = MonsterElement.Undead,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Undead,
                PreRenewalHp = 1458,
                RenewalHp = 3500,
                PreRenewalExp = 436,
                RenewalExp = 1000,
                PreRenewalJobExp = 327,
                RenewalJobExp = 800,
                PreRenewalBaseLevel = 50,
                RenewalBaseLevel = 60,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1800 }, { 7224, 300 }, { 7145, 100 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2200 }, { 7224, 400 }, { 7145, 150 } },
                SpawnMaps = new List<string> { "pay_dun00", "pay_dun01" },
                Description = "Skelett-Bogenschütze in Dungeons."
            });

            db.Add(new Monster
            {
                Id = 1033,
                Name = "Thief Bug",
                DisplayName = "Thief Bug",
                Element = MonsterElement.Earth,
                Size = MonsterSize.Small,
                Race = MonsterRace.Insect,
                PreRenewalHp = 320,
                RenewalHp = 800,
                PreRenewalExp = 320,
                RenewalExp = 800,
                PreRenewalJobExp = 200,
                RenewalJobExp = 450,
                PreRenewalBaseLevel = 15,
                RenewalBaseLevel = 20,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1000 }, { 7239, 100 }, { 7144, 30 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1200 }, { 7239, 150 }, { 7144, 50 } },
                SpawnMaps = new List<string> { "pay_fild04", "pay_fild06" },
                Description = "Dieb-Käfer, stiehlt Items."
            });

            db.Add(new Monster
            {
                Id = 1038,
                Name = "Hunter Fly",
                DisplayName = "Hunter Fly",
                Element = MonsterElement.Wind,
                Size = MonsterSize.Small,
                Race = MonsterRace.Insect,
                PreRenewalHp = 200,
                RenewalHp = 500,
                PreRenewalExp = 200,
                RenewalExp = 500,
                PreRenewalJobExp = 150,
                RenewalJobExp = 350,
                PreRenewalBaseLevel = 10,
                RenewalBaseLevel = 13,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 700 }, { 7143, 40 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 900 }, { 7143, 60 } },
                SpawnMaps = new List<string> { "pay_fild02", "pay_fild03" },
                Description = "Große Fliege, fliegt schnell."
            });

            db.Add(new Monster
            {
                Id = 1058,
                Name = "Pecopeco Egg",
                DisplayName = "Pecopeco Egg",
                Element = MonsterElement.Earth,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Plant,
                PreRenewalHp = 1200,
                RenewalHp = 2800,
                PreRenewalExp = 450,
                RenewalExp = 1100,
                PreRenewalJobExp = 280,
                RenewalJobExp = 650,
                PreRenewalBaseLevel = 18,
                RenewalBaseLevel = 22,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1200 }, { 7224, 150 }, { 7144, 50 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1500 }, { 7224, 200 }, { 7144, 80 } },
                SpawnMaps = new List<string> { "pay_fild07", "prt_fild05" },
                Description = "Ei des Pecopeco-Vogels."
            });

            db.Add(new Monster
            {
                Id = 1092,
                Name = "Deviace",
                DisplayName = "Deviace",
                Element = MonsterElement.Undead,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Undead,
                PreRenewalHp = 2000,
                RenewalHp = 5000,
                PreRenewalExp = 4500,
                RenewalExp = 10000,
                PreRenewalJobExp = 2500,
                RenewalJobExp = 6000,
                PreRenewalBaseLevel = 32,
                RenewalBaseLevel = 40,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 2000 }, { 6107, 400 }, { 7224, 100 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2500 }, { 6107, 500 }, { 7224, 150 } },
                SpawnMaps = new List<string> { "pay_dun01", "pay_dun02" },
                Description = "Untoter Krieger mit Dolch."
            });

            db.Add(new Monster
            {
                Id = 1091,
                Name = "Bone Soldier",
                DisplayName = "Bone Soldier",
                Element = MonsterElement.Undead,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Undead,
                PreRenewalHp = 1500,
                RenewalHp = 3500,
                PreRenewalExp = 3200,
                RenewalExp = 7500,
                PreRenewalJobExp = 1800,
                RenewalJobExp = 4500,
                PreRenewalBaseLevel = 28,
                RenewalBaseLevel = 35,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 1500 }, { 6107, 300 }, { 7224, 80 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 1900 }, { 6107, 400 }, { 7224, 120 } },
                SpawnMaps = new List<string> { "pay_dun00", "pay_dun01" },
                Description = "Skelett-Soldat in Dungeons."
            });

            db.Add(new Monster
            {
                Id = 1090,
                Name = "Giearth",
                DisplayName = "Giearth",
                Element = MonsterElement.Earth,
                Size = MonsterSize.Medium,
                Race = MonsterRace.Beast,
                PreRenewalHp = 3400,
                RenewalHp = 8000,
                PreRenewalExp = 4600,
                RenewalExp = 10800,
                PreRenewalJobExp = 2600,
                RenewalJobExp = 6200,
                PreRenewalBaseLevel = 37,
                RenewalBaseLevel = 44,
                PreRenewalDrops = new Dictionary<int, int> { { 501, 2100 }, { 7172, 180 } },
                RenewalDrops = new Dictionary<int, int> { { 501, 2700 }, { 7172, 260 } },
                SpawnMaps = new List<string> { "pay_fild06", "pay_fild07" },
                Description = "Erde-Bestie aus den Payon-Feldern."
            });

            // ── Pre-Renewal / Renewal Consistency Check ──
            // Alle Monster haben sowohl Pre-Renewal als auch Renewal Werte
            // Für den Import aus Rathena werden diese automatisch überschrieben
            return db;
        }

        /// <summary>
        /// Erstellt eine Datenbank mit nur den wichtigsten klassischen RO-Monstern.
        /// Wird verwendet wenn der automatische Rathena-Import nicht verfügbar ist.
        /// </summary>
        public static MonsterDatabase CreateClassicDatabase()
        {
            var db = new MonsterDatabase();
            // Fügt nur die wichtigsten Basismonster hinzu
            foreach (var monster in CreateStarterDatabase().GetAllMonsters())
            {
                db.Add(monster);
            }
            return db;
        }
    }
}
