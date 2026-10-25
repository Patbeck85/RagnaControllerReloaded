using System;
using System.Collections.Generic;
using System.Linq;
using RagnaController.Models;

namespace RagnaController.Core.Database {
    /// <summary>
    /// TASK-3003: ItemDatabaseService — Item-Suche mit Filtern (Type, Slot, Weight, Price, Drop-Monster).
    ///
    /// Verwaltet RO-Item-Daten für Suche und Filterung.
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
            _items.AddRange(new[]
            {
                new ItemEntry { Id = 101, Name = "Apple", Type = ItemType.Consumable, Slot = 0, Weight = 10, Price = 100, Description = "Heilt 25 HP" },
                new ItemEntry { Id = 102, Name = "Orange", Type = ItemType.Consumable, Slot = 0, Weight = 10, Price = 200, Description = "Heilt 50 HP" },
                new ItemEntry { Id = 201, Name = "Iron Sword", Type = ItemType.Weapon, Slot = 1, Weight = 400, Price = 1000, Attack = 50 },
                new ItemEntry { Id = 202, Name = "Main Gauche", Type = ItemType.Weapon, Slot = 1, Weight = 300, Price = 650, Attack = 40 },
                new ItemEntry { Id = 301, Name = "Padded Armor", Type = ItemType.Armor, Slot = 5, Weight = 500, Price = 1000, Defense = 20 },
                new ItemEntry { Id = 401, Name = "Iron Helm", Type = ItemType.Armor, Slot = 3, Weight = 200, Price = 500, Defense = 15 },
            });
        }

        public ItemEntry? GetItem(int id) => _items.FirstOrDefault(i => i.Id == id);
        public ItemEntry? GetItem(string name) => _items.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public List<ItemEntry> Search(string? name = null, ItemType? type = null, int? minPrice = null, int? maxPrice = null)
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
            return query.ToList();
        }
    }

    public class ItemEntry
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public ItemType Type { get; set; }
        public int Slot { get; set; }
        public int Weight { get; set; }
        public int Price { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public string Description { get; set; } = "";
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
