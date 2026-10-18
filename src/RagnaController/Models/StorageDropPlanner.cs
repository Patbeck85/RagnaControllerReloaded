using System;
using System.Collections.Generic;
using System.Linq;

namespace RagnaController.Models
{
    /// <summary>
    /// FEAT-023: An occupied inventory slot (pure data model, headless-testable).
    /// The actual detection "which item is in the slot" is a separate
    /// integration point (OCR/window reading of game UI) — analogous FEAT-015/FEAT-024.
    /// </summary>
    public readonly record struct InventorySlotItem(
        string Name,
        StorageDropPriority Rarity,
        int StackCount);

    /// <summary>
    /// FEAT-023: Priority class of an item for auto-storage (Storage-Drop).
    /// Order = release order: first Consumables stored, then Buffs dropped.
    /// QuestItems and Equipment are never automatically released.
    /// </summary>
    public enum StorageDropPriority
    {
        /// <summary>Potions/elixirs — stored in storage (no value loss).</summary>
        Consumable = 0,
        /// <summary>Buff items with expired value — dropped.</summary>
        BuffItem = 1,
        /// <summary>Quest items — never automatically released.</summary>
        QuestItem = 2,
        /// <summary>Equipment — never automatically released.</summary>
        Equipment = 3
    }

    /// <summary>FEAT-023: The planned action for a slot.</summary>
    public enum StorageDropAction
    {
        /// <summary>Store in storage (Storage-Drop).</summary>
        Stash,
        /// <summary>Drop on the ground.</summary>
        Drop,
        /// <summary>Leave as-is (protected).</summary>
        Keep
    }

    /// <summary>FEAT-023: A single plan entry (slot + item + action + reason).</summary>
    public readonly record struct StorageDropIntent(
        int SlotIndex,
        InventorySlotItem Item,
        StorageDropAction Action,
        string Reason);

    /// <summary>
    /// FEAT-023: Reine, deterministische Planungslogik for die Auto-Item-Einlagerung.
    /// Entscheidet, welche Inventar-Slots befreit are, if das Inventar voll is:
    /// Consumables → Storage-Drop (Stash), Buffs → Drop, QuestItems/Equipment → Keep.
    /// </summary>
    /// <remarks>
    /// Intentionally NO cursor/UI access (KISS/YAGNI): the execution (cursor to
    /// Storage-UI navigieren, Items physisch verschieben) is ein separater
    /// Integrationspunkt about RoUiMenuService/SmartCursorService.
    /// </remarks>
    public static class StorageDropPlanner
    {
        /// <summary>
        /// Plant die Befreiungs-Aktionen for ein volles Inventar.
        /// </summary>
        /// <param name="usedSlots">Anzahl belegter Slots.</param>
        /// <param name="slots">Belegte Slots (indexiert wie im Inventar); darf leer/null sein.</param>
        /// <returns>Aktionsliste in Slot-Reihenfolge; leer, if das Inventar nicht voll is.</returns>
        public static IReadOnlyList<StorageDropIntent> Plan(
            int capacity,
            int usedSlots,
            IReadOnlyList<InventorySlotItem>? slots)
        {
            var plan = new List<StorageDropIntent>();
            if (slots == null || slots.Count == 0) return plan;
            if (capacity <= 0 || usedSlots < capacity) return plan; // nicht voll → nichts tun

            for (int i = 0; i < slots.Count && i < usedSlots; i++)
            {
                var item = slots[i];
                switch (item.Rarity)
                {
                    case StorageDropPriority.Consumable:
                        plan.Add(new StorageDropIntent(i, item, StorageDropAction.Stash, "Consumable → Storage-Drop"));
                        break;
                    case StorageDropPriority.BuffItem:
                        plan.Add(new StorageDropIntent(i, item, StorageDropAction.Drop, "Buff-Item ohne Nutzenwert → Drop"));
                        break;
                    default: // QuestItem, Equipment
                        plan.Add(new StorageDropIntent(i, item, StorageDropAction.Keep, "Protected (quest/equipment)"));
                        break;
                }
            }

            return plan;
        }

        /// <summary>FEAT-023: Standard capacity of a Classic RO inventory.</summary>
        public const int DefaultCapacity = 24;

        /// <summary>
        /// FEAT-023: plans with standard capacity (24 slots).
        /// </summary>
        public static IReadOnlyList<StorageDropIntent> PlanDefault(int usedSlots, IReadOnlyList<InventorySlotItem>? slots)
            => Plan(DefaultCapacity, usedSlots, slots);

        /// <summary>
        /// FEAT-023: counts the actually freeing actions (stash + drop) in a plan.
        /// </summary>
        public static int CountReleasingActions(IReadOnlyList<StorageDropIntent> plan)
            => plan.Count(x => x.Action != StorageDropAction.Keep);

        /// <summary>
        /// FEAT-023: checks if a plan frees at least <paramref name="neededSlots"/> slots.
        /// Standard 1 = "a new pickup is possible".
        /// </summary>
        public static bool FreesEnough(IReadOnlyList<StorageDropIntent> plan, int neededSlots = 1)
            => CountReleasingActions(plan) >= neededSlots;
    }
}
