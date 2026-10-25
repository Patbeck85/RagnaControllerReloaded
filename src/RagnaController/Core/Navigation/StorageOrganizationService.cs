using System;
using System.Collections.Generic;
using System.Linq;
using RagnaController.Models;

namespace RagnaController.Core.Navigation {
    /// <summary>
    /// TASK-2502: StorageOrganizationService — Items sortieren, Stacks zusammenführen, Kategorien.
    ///
    /// Verwaltet die Organisation von Items im RO-Speicher (Storage/Kart).
    /// Unterstützt:
    ///   • Automatisches Sortieren nach Kategorie, Name, Preis, Menge
    ///   • Stack-Zusammenführung (gleiche Items zu einem Stapel)
    ///   • Kategorien-basierte Gruppenansicht
    ///   • Leere Slots defragmentieren (Items nach links rücken)
    /// </summary>
    public sealed class StorageOrganizationService
    {
        private readonly InputCommandQueue _queue;
        private readonly IFeedbackProvider _feedback;

        // ── Storage State ───────────────────────────────────────────────────
        public bool IsStorageOpen { get; private set; }
        public int TotalSlots { get; set; } = 80; // Standard RO storage: 80 Slots
        public int UsedSlots { get; private set; } = 0;
        public int FreeSlots => TotalSlots - UsedSlots;

        // ── Items im Storage ────────────────────────────────────────────────
        public List<StorageItem> Items { get; } = new();

        // ── Sortierung ──────────────────────────────────────────────────────
        public enum SortBy
        {
            Name,
            Category,
            Price,
            Quantity,
            Type,
        }

        public SortBy CurrentSortBy { get; set; } = SortBy.Name;
        public bool SortAscending { get; set; } = true;

        public StorageOrganizationService(InputCommandQueue queue, IFeedbackProvider feedback)
        {
            _queue = queue;
            _feedback = feedback;
        }

        /// <summary>Öffnet den Storage-Organisationsmodus.</summary>
        public void OpenStorage()
        {
            IsStorageOpen = true;
            _feedback.Trigger(FeedbackType.StandbyOn);
        }

        /// <summary>Schließt den Storage-Organisationsmodus.</summary>
        public void CloseStorage()
        {
            IsStorageOpen = false;
            _feedback.Trigger(FeedbackType.StandbyOff);
        }

        /// <summary>Fügt ein Item zum Storage hinzu (für Test/Overlay-Befüllung).</summary>
        public void AddItem(StorageItem item)
        {
            Items.Add(item);
            UpdateUsedSlots();
        }

        /// <summary>Entfernt ein Item aus dem Storage.</summary>
        public void RemoveItem(StorageItem item)
        {
            Items.Remove(item);
            UpdateUsedSlots();
        }

        /// <summary>Sortiert alle Items nach der aktuellen Sortierung.</summary>
        public void SortItems()
        {
            var sorted = CurrentSortBy switch
            {
                SortBy.Name => SortAscending
                    ? Items.OrderBy(i => i.Name).ToList()
                    : Items.OrderByDescending(i => i.Name).ToList(),
                SortBy.Category => SortAscending
                    ? Items.OrderBy(i => i.Category).ThenBy(i => i.Name).ToList()
                    : Items.OrderByDescending(i => i.Category).ThenByDescending(i => i.Name).ToList(),
                SortBy.Price => SortAscending
                    ? Items.OrderBy(i => i.Price).ToList()
                    : Items.OrderByDescending(i => i.Price).ToList(),
                SortBy.Quantity => SortAscending
                    ? Items.OrderBy(i => i.Quantity).ToList()
                    : Items.OrderByDescending(i => i.Quantity).ToList(),
                SortBy.Type => SortAscending
                    ? Items.OrderBy(i => i.ItemType).ThenBy(i => i.Name).ToList()
                    : Items.OrderByDescending(i => i.ItemType).ThenByDescending(i => i.Name).ToList(),
                _ => Items,
            };

            Items.Clear();
            foreach (var item in sorted)
                Items.Add(item);

            _feedback.Trigger(FeedbackType.PhaseChange);
        }

        /// <summary>Wechselt die Sortierrichtung.</summary>
        public void ToggleSortDirection()
        {
            SortAscending = !SortAscending;
            SortItems();
        }

        /// <summary>Wechselt das Sortierkriterium.</summary>
        public void CycleSortBy()
        {
            var values = Enum.GetValues<SortBy>();
            var currentIndex = Array.IndexOf(values, CurrentSortBy);
            var nextIndex = (currentIndex + 1) % values.Length;
            CurrentSortBy = values[nextIndex];
            SortItems();
        }

        /// <summary>Führt alle gleichen Item-Stacks zusammen.</summary>
        public int MergeStacks()
        {
            var merged = 0;
            var groups = Items.GroupBy(i => new { i.Id, i.Name });

            foreach (var group in groups)
            {
                var items = group.ToList();
                if (items.Count <= 1)
                    continue;

                var totalQty = items.Sum(i => i.Quantity);
                var first = items.First();
                first.Quantity = totalQty;

                // Alle außer dem ersten entfernen
                foreach (var item in items.Skip(1))
                {
                    Items.Remove(item);
                    merged++;
                }
            }

            UpdateUsedSlots();

            if (merged > 0)
                _feedback.Trigger(FeedbackType.ItemBought); // Wiederverwenden als "Zusammengeführt"-Feedback

            return merged;
        }

        /// <summary>Defragmentiert den Storage — rückt alle Items nach links (Slot 0, 1, 2...).</summary>
        public void Defragment()
        {
            // Sortiere nach Slot-Index und ordne die Liste neu
            Items.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));
            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].SlotIndex = i;
            }

            UpdateUsedSlots();
            _feedback.Trigger(FeedbackType.PhaseChange);
        }

        /// <summary>Gibt Items einer bestimmten Kategorie zurück.</summary>
        public List<StorageItem> GetItemsByCategory(string category)
        {
            return Items.Where(i => i.Category == category).ToList();
        }

        /// <summary>Gibt alle verfügbaren Kategorien zurück.</summary>
        public List<string> GetCategories()
        {
            return Items.Select(i => i.Category).Distinct().OrderBy(c => c).ToList();
        }

        /// <summary>Verarbeitet Controller-Eingaben für Storage-Organisation.</summary>
        public bool ProcessInput(ParsedInput input)
        {
            if (!IsStorageOpen)
                return false;

            // Storage verlassen (B / Back)
            if (input.BtnB || input.Back)
            {
                CloseStorage();
                return true;
            }

            // Sortierkriterium wechseln (L1 + R1)
            if (input.L1 && input.R1)
            {
                CycleSortBy();
                return true;
            }

            // Sortierrichtung umkehren (L1 allein)
            if (input.L1)
            {
                ToggleSortDirection();
                return true;
            }

            // Stacks zusammenführen (R1 allein)
            if (input.R1)
            {
                MergeStacks();
                return true;
            }

            // Defragmentieren (X-Button)
            if (input.BtnX)
            {
                Defragment();
                return true;
            }

            // Item auswählen (D-Pad)
            if (input.DPadUp || input.DPadDown || input.DPadLeft || input.DPadRight)
            {
                _feedback.Trigger(FeedbackType.Warning);
                return true;
            }

            // Item verschieben (A-Button)
            if (input.BtnA)
            {
                _feedback.Trigger(FeedbackType.Warning);
                return true;
            }

            return false;
        }

        private void UpdateUsedSlots()
        {
            UsedSlots = Items.Count;
        }
    }

    /// <summary>
    /// Repräsentiert ein Item im Storage.
    /// </summary>
    public class StorageItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Quantity { get; set; } = 1;
        public int MaxStack { get; set; } = 100;
        public int Price { get; set; }
        public string Category { get; set; } = "Sonstiges";
        public string ItemType { get; set; } = "Item";
        public int SlotIndex { get; set; } = 0;
        public bool IsEquipped { get; set; } = false;
        public string Description { get; set; } = "";
    }
}
