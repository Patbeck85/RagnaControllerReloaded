using System;
using System.Collections.Generic;
using System.Linq;

namespace RagnaController.Core.Database
{
    /// <summary>
    /// TASK-3003: ItemDatabaseService — Item-Suche mit Filtern (Type, Slot, Weight, Price, Drop-Monster).
    ///
    /// Verwaltet RO-Item-Daten für Suche und Filterung.
    /// Unterstützt Pre-Renewal und Renewal mit separaten Preisen und Stats.
    /// White-Hat: Reine Datenbereitstellung.
    /// </summary>
    public sealed class ItemDatabaseService
    {
        private readonly List<ItemEntry> _items = new();

        public ItemDatabaseService()
        {
            InitializeStarterItems();
        }

        public IReadOnlyList<ItemEntry> Items => _items;

        public void InitializeStarterItems()
        {
            if (_items.Count > 0) return; // Bereits initialisiert
            _items.AddRange(BuildAllItems());
        }

        #region Item Factory (alle klassischen RO-Items)

        private static IEnumerable<ItemEntry> BuildAllItems()
        {
            foreach (var i in BuildConsumables()) yield return i;
            foreach (var i in BuildWeapons()) yield return i;
            foreach (var i in BuildArmors()) yield return i;
            foreach (var i in BuildAccessories()) yield return i;
            foreach (var i in BuildCards()) yield return i;
            foreach (var i in BuildEtc()) yield return i;
        }

        // ── Verbrauchsgüter ──
        private static IEnumerable<ItemEntry> BuildConsumables()
        {
            yield return Item(101, "Apple", ItemType.Consumable, 0, 10, 100, hp: 25, description: "Heilt 25 HP");
            yield return Item(102, "Orange", ItemType.Consumable, 0, 10, 200, hp: 50, description: "Heilt 50 HP");
            yield return Item(103, "Meat", ItemType.Consumable, 0, 10, 400, hp: 120, description: "Heilt 120 HP");
            yield return Item(104, "Red Potion", ItemType.Consumable, 0, 10, 80, hp: 150, description: "Heilt 150 HP");
            yield return Item(105, "Red Potion Small", ItemType.Consumable, 0, 10, 50, hp: 50, description: "Heilt 50 HP");
            yield return Item(106, "White Potion", ItemType.Consumable, 0, 10, 100, hp: 500, description: "Heilt 500 HP");
            yield return Item(107, "White Potion Small", ItemType.Consumable, 0, 10, 60, hp: 250, description: "Heilt 250 HP");
            yield return Item(108, "Blue Potion", ItemType.Consumable, 0, 10, 120, sp: 150, description: "Heilt 150 SP");
            yield return Item(109, "Blue Potion Small", ItemType.Consumable, 0, 10, 80, sp: 50, description: "Heilt 50 SP");
            yield return Item(110, "Pineapple", ItemType.Consumable, 0, 10, 300, sp: 100, description: "Heilt 100 SP");
            yield return Item(111, "Orange Potion", ItemType.Consumable, 0, 10, 150, hp: 250, sp: 100, description: "Heilt 250 HP und 100 SP");
            yield return Item(112, "Yggdrasil Berry", ItemType.Consumable, 0, 1, 2000, hp: 10000, sp: 10000, description: "Vollständige Heilung HP und SP");
            yield return Item(113, "Jellopy", ItemType.Consumable, 0, 1, 10, description: "Schleim von Monstern, geringwertig");

            yield return Item(201, "Holy Water", ItemType.Consumable, 0, 10, 150, description: "Entfernt Fluch-Status");
            yield return Item(202, "Panacea", ItemType.Consumable, 0, 10, 500, description: "Entfernt alle negativen Statuseffekte");
            yield return Item(203, "Awakening Potion", ItemType.Consumable, 0, 10, 400, description: "Entfernt Schlaf-Status");
            yield return Item(204, "Antidote", ItemType.Consumable, 0, 10, 50, description: "Entfernt Vergiftung");
            yield return Item(205, "Green Potion", ItemType.Consumable, 0, 10, 300, description: "Entfernt Versteinerung");

            yield return Item(301, "Fly Wing", ItemType.Consumable, 0, 1, 70, description: "Zufälliger Teleport");
            yield return Item(302, "Butterfly Wing", ItemType.Consumable, 0, 1, 200, description: "Teleport zur gespeicherten Position");
            yield return Item(303, "Return Scroll", ItemType.Consumable, 0, 1, 100, description: "Teleport zur Startstadt");
        }

        // ── Waffen ──
        private static IEnumerable<ItemEntry> BuildWeapons()
        {
            yield return Item(1001, "Dagger", ItemType.Weapon, 1, 100, 50, attack: 20, minBaseLevel: 1, minJobLevel: 1, description: "Einfacher Dolch");
            yield return Item(1002, "Main Gauche", ItemType.Weapon, 1, 150, 100, attack: 35, minBaseLevel: 10, minJobLevel: 1, description: "Linkshand-Dolch");
            yield return Item(1003, "Iron Dagger", ItemType.Weapon, 1, 200, 200, attack: 50, minBaseLevel: 15, minJobLevel: 1, description: "Eiserner Dolch");
            yield return Item(1004, "Main Gauche [2]", ItemType.Weapon, 1, 150, 500, attack: 35, slots: 2, description: "Main Gauche mit 2 Slots");

            yield return Item(2001, "Short Sword", ItemType.Weapon, 1, 200, 300, attack: 60, minBaseLevel: 1, minJobLevel: 1, description: "Kurzschwert");
            yield return Item(2002, "Iron Sword", ItemType.Weapon, 1, 400, 1000, attack: 80, minBaseLevel: 10, minJobLevel: 1, description: "Eisenschwert");
            yield return Item(2003, "Main Gauche [1]", ItemType.Weapon, 1, 150, 300, attack: 35, slots: 1, description: "Main Gauche mit 1 Slot");
            yield return Item(2004, "Broadsword", ItemType.Weapon, 1, 600, 2500, attack: 110, minBaseLevel: 20, minJobLevel: 1, description: "Breitschwert");
            yield return Item(2005, "Slasher", ItemType.Weapon, 1, 500, 3000, attack: 130, minBaseLevel: 25, minJobLevel: 1, description: "Zweihändiges Schwert");

            yield return Item(3001, "Short Bow", ItemType.Weapon, 1, 100, 400, attack: 50, minBaseLevel: 1, minJobLevel: 1, description: "Kurzbogen");
            yield return Item(3002, "Bow", ItemType.Weapon, 1, 300, 1200, attack: 80, minBaseLevel: 10, minJobLevel: 1, description: "Bogen");
            yield return Item(3003, "Long Bow", ItemType.Weapon, 1, 500, 2500, attack: 110, minBaseLevel: 20, minJobLevel: 1, description: "Langbogen");
            yield return Item(3004, "Hunter Bow", ItemType.Weapon, 1, 800, 5000, attack: 150, minBaseLevel: 40, minJobLevel: 1, description: "Jägerbogen");

            yield return Item(4001, "Short Staff", ItemType.Weapon, 1, 100, 200, attack: 10, minBaseLevel: 1, minJobLevel: 1, description: "Kurzstab");
            yield return Item(4002, "Staff", ItemType.Weapon, 1, 200, 600, attack: 30, minBaseLevel: 10, minJobLevel: 1, description: "Zauberstab");
            yield return Item(4003, "Long Staff", ItemType.Weapon, 1, 400, 1500, attack: 60, minBaseLevel: 20, minJobLevel: 1, description: "Langstab");

            yield return Item(5001, "Main Gauche [3]", ItemType.Weapon, 1, 150, 1500, attack: 35, slots: 3, minBaseLevel: 30, minJobLevel: 1, description: "Main Gauche mit 3 Slots");
            yield return Item(5002, "Sword [4]", ItemType.Weapon, 1, 600, 8000, attack: 110, slots: 4, minBaseLevel: 40, minJobLevel: 1, description: "Schwert mit 4 Slots");

            yield return Item(6001, "Crystal Sword", ItemType.Weapon, 1, 900, 15000, attack: 180, minBaseLevel: 60, minJobLevel: 1, description: "Kristall-Schwert", isRenewal: true);
            yield return Item(6002, "Basilisk", ItemType.Weapon, 1, 1200, 25000, attack: 220, minBaseLevel: 70, minJobLevel: 1, description: "Basilisk-Schwert", isRenewal: true);
            yield return Item(6003, "Valkyrie Manteau", ItemType.Weapon, 1, 800, 20000, attack: 0, defense: 30, minBaseLevel: 70, minJobLevel: 1, description: "Valkyrie-Mantel", isRenewal: true);
        }

        // ── Rüstungen ──
        private static IEnumerable<ItemEntry> BuildArmors()
        {
            yield return Item(7001, "Padded Armor", ItemType.Armor, 5, 500, 1000, defense: 20, minBaseLevel: 1, minJobLevel: 1, description: "Gepolsterte Rüstung");
            yield return Item(7002, "Tunic", ItemType.Armor, 5, 300, 500, defense: 10, minBaseLevel: 1, minJobLevel: 1, description: "Tunika");
            yield return Item(7003, "Leather Armor", ItemType.Armor, 5, 400, 800, defense: 15, minBaseLevel: 5, minJobLevel: 1, description: "Lederrüstung");
            yield return Item(7004, "Mantle", ItemType.Armor, 5, 200, 300, defense: 5, minBaseLevel: 1, minJobLevel: 1, description: "Umhang");
            yield return Item(7005, "Cape", ItemType.Armor, 5, 150, 200, defense: 3, minBaseLevel: 1, minJobLevel: 1, description: "Umhang");

            yield return Item(8001, "Iron Helm", ItemType.Armor, 3, 200, 500, defense: 15, minBaseLevel: 1, minJobLevel: 1, description: "Eisenhelm");
            yield return Item(8002, "Bronze Helm", ItemType.Armor, 3, 150, 300, defense: 10, minBaseLevel: 1, minJobLevel: 1, description: "Bronzehelm");
            yield return Item(8003, "Padded Armor [1]", ItemType.Armor, 5, 500, 1500, defense: 20, slots: 1, description: "Gepolsterte Rüstung mit 1 Slot");
            yield return Item(8004, "Plate Armor", ItemType.Armor, 5, 800, 4000, defense: 40, minBaseLevel: 25, minJobLevel: 1, description: "Plattenrüstung");
            yield return Item(8005, "Mithril Plate", ItemType.Armor, 5, 1200, 8000, defense: 60, minBaseLevel: 45, minJobLevel: 1, description: "Mithril-Plattenrüstung");

            yield return Item(9001, "Iron Boots", ItemType.Armor, 6, 300, 600, defense: 10, minBaseLevel: 1, minJobLevel: 1, description: "Eisenstiefel");
            yield return Item(9002, "Sandals", ItemType.Armor, 6, 100, 200, defense: 2, minBaseLevel: 1, minJobLevel: 1, description: "Sandalen");
            yield return Item(9003, "Tights", ItemType.Armor, 6, 50, 100, defense: 1, minBaseLevel: 1, minJobLevel: 1, description: "Strumpfhose");

            yield return Item(10001, "Valkyrie Armor", ItemType.Armor, 5, 1500, 50000, defense: 80, minBaseLevel: 70, minJobLevel: 1, description: "Valkyrie-Rüstung", isRenewal: true);
            yield return Item(10002, "Peco Coat", ItemType.Armor, 5, 1000, 30000, defense: 50, minBaseLevel: 60, minJobLevel: 1, description: "Peco-Mantel", isRenewal: true);
            yield return Item(10003, "Drooping Benedicta", ItemType.Armor, 5, 800, 40000, defense: 70, minBaseLevel: 65, minJobLevel: 1, description: "Hängende Benedicta", isRenewal: true);
        }

        // ── Accessoires ──
        private static IEnumerable<ItemEntry> BuildAccessories()
        {
            yield return Item(11001, "Earring", ItemType.Accessory, 4, 50, 100, minBaseLevel: 1, minJobLevel: 1, description: "Ohrring");
            yield return Item(11002, "Pendant", ItemType.Accessory, 4, 50, 100, minBaseLevel: 1, minJobLevel: 1, description: "Anhänger");
            yield return Item(11003, "Muffler", ItemType.Accessory, 4, 100, 300, minBaseLevel: 1, minJobLevel: 1, description: "Schal");
            yield return Item(11004, "Robe", ItemType.Accessory, 4, 300, 1000, minBaseLevel: 10, minJobLevel: 1, description: "Robe");
            yield return Item(11005, "Sunglasses", ItemType.Accessory, 4, 100, 500, minBaseLevel: 1, minJobLevel: 1, description: "Sonnenbrille");

            yield return Item(12001, "Sunglasses [1]", ItemType.Accessory, 4, 100, 800, slots: 1, minBaseLevel: 10, minJobLevel: 1, description: "Sonnenbrille mit 1 Slot");
            yield return Item(12002, "Furry Cap", ItemType.Accessory, 4, 200, 1500, minBaseLevel: 15, minJobLevel: 1, description: "Pelzmütze");
            yield return Item(12003, "Earmuffs", ItemType.Accessory, 4, 150, 800, minBaseLevel: 10, minJobLevel: 1, description: "Ohrenschützer");

            yield return Item(13001, "Hermes' Shoes", ItemType.Accessory, 4, 500, 100000, minBaseLevel: 70, minJobLevel: 1, description: "Hermes' Schuhe", isRenewal: true);
            yield return Item(13002, "Manteau [1]", ItemType.Accessory, 4, 800, 80000, slots: 1, minBaseLevel: 70, minJobLevel: 1, description: "Mantel mit 1 Slot", isRenewal: true);
        }

        // ── Karten (Monster-Karten) ──
        private static IEnumerable<ItemEntry> BuildCards()
        {
            yield return Item(40001, "Poring Card", ItemType.Card, 4, 1, 50000, description: "Poring-Karte", dropsFrom: 1002);
            yield return Item(40002, "Poporing Card", ItemType.Card, 4, 1, 80000, description: "Poporing-Karte", dropsFrom: 1012);
            yield return Item(40003, "Marin Card", ItemType.Card, 4, 1, 100000, description: "Marin-Karte", dropsFrom: 1030);
            yield return Item(40004, "Fabre Card", ItemType.Card, 4, 1, 150000, description: "Fabre-Karte", dropsFrom: 1086);
            yield return Item(40005, "Picky Card", ItemType.Card, 4, 1, 120000, description: "Picky-Karte", dropsFrom: 1113);
            yield return Item(40006, "Bapho Jr. Card", ItemType.Card, 4, 1, 300000, description: "Bapho Jr.-Karte", dropsFrom: 1087);
            yield return Item(40007, "Orc Warrior Card", ItemType.Card, 4, 1, 200000, description: "Orc-Krieger-Karte", dropsFrom: 1096);
            yield return Item(40008, "Zombie Card", ItemType.Card, 4, 1, 180000, description: "Zombie-Karte", dropsFrom: 1046);
            yield return Item(40009, "Deviace Card", ItemType.Card, 4, 1, 500000, description: "Deviace-Karte", dropsFrom: 1092);
            yield return Item(40010, "Bone Soldier Card", ItemType.Card, 4, 1, 400000, description: "Bone Soldier-Karte", dropsFrom: 1091);

            yield return Item(41001, "Baphomet Jr. Card", ItemType.Card, 4, 1, 5000000, description: "MVP-Karte – Baphomet Jr.", dropsFrom: 1047, isMvp: true);
            yield return Item(41002, "Golden Thief Bug Card", ItemType.Card, 4, 1, 4000000, description: "MVP-Karte – Golden Thief Bug", dropsFrom: 1147, isMvp: true);
            yield return Item(41003, "Lord Knight Card", ItemType.Card, 4, 1, 6000000, description: "MVP-Karte – Lord Knight", dropsFrom: 1301, isMvp: true);
            yield return Item(41004, "General Glast Heim Card", ItemType.Card, 4, 1, 8000000, description: "MVP-Karte – General Glast Heim", dropsFrom: 1260, isMvp: true);
            yield return Item(41005, "Stormy Knight Card", ItemType.Card, 4, 1, 5000000, description: "MVP-Karte – Stormy Knight", dropsFrom: 1146, isMvp: true);
            yield return Item(41006, "Turtle General Card", ItemType.Card, 4, 1, 4500000, description: "MVP-Karte – Turtle General", dropsFrom: 1094, isMvp: true);
            yield return Item(41007, "Queen Ant Card", ItemType.Card, 4, 1, 7000000, description: "MVP-Karte – Queen Ant", dropsFrom: 1132, isMvp: true);
            yield return Item(41008, "Dark Lord Card", ItemType.Card, 4, 1, 10000000, description: "MVP-Karte – Dark Lord", dropsFrom: 1252, isMvp: true);
        }

        // ── Sonstige Items ──
        private static IEnumerable<ItemEntry> BuildEtc()
        {
            yield return Item(50001, "Jellopy", ItemType.Etc, 0, 1, 10, description: "Schleim von Monstern");
            yield return Item(50002, "Red Blood", ItemType.Etc, 0, 1, 50, description: "Blut von Untoten");
            yield return Item(50003, "Green Blood", ItemType.Etc, 0, 1, 100, description: "Grünes Blut von Monstern");
            yield return Item(50004, "Blue Blood", ItemType.Etc, 0, 1, 200, description: "Blaues Blut von Monstern");
            yield return Item(50005, "Spider Web", ItemType.Etc, 0, 1, 80, description: "Spinnennetz");
            yield return Item(50006, "Fluffy Wing", ItemType.Etc, 0, 1, 100, description: "Feder von Monstern");
            yield return Item(50007, "Orcish Axe", ItemType.Etc, 0, 200, 500, description: "Ork-Axt");
            yield return Item(50008, "Old Blue Box", ItemType.Etc, 0, 500, 10000, description: "Altes Blaues Paket");
            yield return Item(50009, "Old Violet Box", ItemType.Etc, 0, 500, 20000, description: "Altes Violettes Paket");
            yield return Item(50010, "Old Red Box", ItemType.Etc, 0, 500, 30000, description: "Altes Rotes Paket");

            yield return Item(51001, "Iron Ore", ItemType.Etc, 0, 50, 200, description: "Eisenerz");
            yield return Item(51002, "Steel", ItemType.Etc, 0, 100, 500, description: "Stahl");
            yield return Item(51003, "Oridecon", ItemType.Etc, 0, 10, 10000, description: "Oridecon (Verstärkungsmaterial)");
            yield return Item(51004, "Oridecon Rough", ItemType.Etc, 0, 10, 3000, description: "Rohes Oridecon");
            yield return Item(51005, "Emveretarcon", ItemType.Etc, 0, 5, 50000, description: "Emveretarcon (Verstärkungsmaterial)");
            yield return Item(51006, "Emveretarcon Rough", ItemType.Etc, 0, 5, 15000, description: "Rohes Emveretarcon");

            yield return Item(52001, "Map Piece 1", ItemType.Etc, 0, 1, 5000, description: "Karten-Fragment 1");
            yield return Item(52002, "Map Piece 2", ItemType.Etc, 0, 1, 5000, description: "Karten-Fragment 2");
            yield return Item(52003, "Map Piece 3", ItemType.Etc, 0, 1, 5000, description: "Karten-Fragment 3");
            yield return Item(52004, "Map Piece 4", ItemType.Etc, 0, 1, 5000, description: "Karten-Fragment 4");
        }

        private static ItemEntry Item(
            int id, string name, ItemType type, int slot, int weight, int price,
            int hp = 0, int sp = 0, int attack = 0, int defense = 0, int slots = 0,
            int minBaseLevel = 1, int minJobLevel = 1, string description = "",
            int? dropsFrom = null, bool isMvp = false, bool isRenewal = false, int sellPrice = 0)
        {
            return new ItemEntry
            {
                Id = id,
                Name = name,
                Type = type,
                Slot = slot,
                Weight = weight,
                Price = price,
                SellPrice = sellPrice > 0 ? sellPrice : price / 2,
                Attack = attack,
                Defense = defense,
                HpHeal = hp,
                SpHeal = sp,
                Slots = slots,
                MinBaseLevel = minBaseLevel,
                MinJobLevel = minJobLevel,
                Description = description,
                DropsFrom = dropsFrom,
                IsMvp = isMvp,
                IsRenewal = isRenewal,
            };
        }

        #endregion

        #region Abfragen

        public ItemEntry? GetItem(int id) => _items.FirstOrDefault(i => i.Id == id);

        public ItemEntry? GetItem(string name) => _items.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public List<ItemEntry> Search(string? name = null, ItemType? type = null, int? minPrice = null, int? maxPrice = null,
            GameMode mode = GameMode.Both, int? minSlots = null, bool? isMvp = null)
        {
            var query = _items.AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(i => i.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

            if (type.HasValue)
                query = query.Where(i => i.Type == type.Value);

            if (minPrice.HasValue)
                query = query.Where(i => i.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(i => i.Price <= maxPrice.Value);

            if (minSlots.HasValue)
                query = query.Where(i => i.Slots >= minSlots.Value);

            if (isMvp.HasValue)
                query = query.Where(i => i.IsMvp == isMvp.Value);

            if (mode == GameMode.PreRenewal)
                query = query.Where(i => !i.IsRenewal);
            else if (mode == GameMode.Renewal)
                query = query.Where(i => i.IsRenewal);
            // GameMode.Both = keine Filterung nach GameMode

            return query.ToList();
        }

        public List<ItemEntry> GetItemsByDropMonster(int monsterId) =>
            _items.Where(i => i.DropsFrom == monsterId).ToList();

        public List<ItemEntry> GetMvpCards() => _items.Where(i => i.Type == ItemType.Card && i.IsMvp).ToList();

        public List<ItemEntry> GetConsumables() => _items.Where(i => i.Type == ItemType.Consumable).ToList();

        public List<ItemEntry> GetWeapons() => _items.Where(i => i.Type == ItemType.Weapon).ToList();

        public List<ItemEntry> GetArmors() => _items.Where(i => i.Type == ItemType.Armor).ToList();

        public List<ItemEntry> GetAccessories() => _items.Where(i => i.Type == ItemType.Accessory).ToList();

        public List<ItemEntry> GetEtc() => _items.Where(i => i.Type == ItemType.Etc).ToList();

        public List<ItemEntry> GetItemsWithSlots(int minSlots = 1) => _items.Where(i => i.Slots >= minSlots).ToList();

        public List<ItemEntry> GetRenewalItems() => _items.Where(i => i.IsRenewal).ToList();

        public List<ItemEntry> GetPreRenewalItems() => _items.Where(i => !i.IsRenewal).ToList();

        #endregion
    }

    public class ItemEntry
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public ItemType Type { get; set; }
        public int Slot { get; set; }
        public int Weight { get; set; }
        public int Price { get; set; }
        public int SellPrice { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int HpHeal { get; set; }
        public int SpHeal { get; set; }
        public int Slots { get; set; }
        public int MinBaseLevel { get; set; } = 1;
        public int MinJobLevel { get; set; } = 1;
        public string Description { get; set; } = "";
        public int? DropsFrom { get; set; }
        public bool IsMvp { get; set; }
        public bool IsRenewal { get; set; }
    }

    public enum ItemType
    {
        Consumable,
        Weapon,
        Armor,
        Accessory,
        Card,
        Etc,
    }
}
