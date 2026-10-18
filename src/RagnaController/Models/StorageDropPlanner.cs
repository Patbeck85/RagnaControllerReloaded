using System;
using System.Collections.Generic;
using System.Linq;

namespace RagnaController.Models
{
    /// <summary>
    /// FEAT-023: Ein belegter Inventar-Slot (reines Datenmodell, headless-testbar).
    /// The eigentliche Erkennung „welches Item liegt im Slot" is ein separater
    /// Integrationspunkt (OCR-/Fenster-Lesung der Game-UI) — analog FEAT-015/FEAT-024.
    /// </summary>
    public readonly record struct InventorySlotItem(
        string Name,
        StorageDropPriority Rarity,
        int StackCount);

    /// <summary>
    /// FEAT-023: Prioritätsklasse eines Items for die Auto-Einlagerung (Storage-Drop).
    /// Reihenfolge = Freigabe-Reihenfolge: erst Consumables einlagern, then Buffs dropen.
    /// QuestItems and Equipment are niemals automatic freigegeben.
    /// </summary>
    public enum StorageDropPriority
    {
        /// <summary>Potione/Tränke — are in die Storage eingelagert (no Value-Verlust).</summary>
        Consumable = 0,
        /// <summary>Buff-Items with abgelaufenem Nutzenwert — are gedroppt.</summary>
        BuffItem = 1,
        /// <summary>Quest-Items — niemals automatic freigeben.</summary>
        QuestItem = 2,
        /// <summary>Ausrüstung — niemals automatic freigeben.</summary>
        Equipment = 3
    }

    /// <summary>FEAT-023: The geplante Aktion for einen Slot.</summary>
    public enum StorageDropAction
    {
        /// <summary>In die Storage einlagern (Storage-Drop).</summary>
        Stash,
        /// <summary>On dem Boden dropen.</summary>
        Drop,
        /// <summary>Belassen (geschützt).</summary>
        Keep
    }

    /// <summary>FEAT-023: Ein einzelner Plan-Eintrag (Slot + Item + Aktion + Begründung).</summary>
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
    /// Bewusst KEIN Cursor-/UI-Zugriff (KISS/YAGNI): The Ausführung (Cursor zur
    /// Storage-UI navigieren, Items physisch verschieben) is ein separater
    /// Integrationspunkt about RoUiMenuService/SmartCursorService.
    /// </remarks>
    public static class StorageDropPlanner
    {
        /// <summary>
        /// Plant die Befreiungs-Aktionen for ein volles Inventar.
        /// </summary>
        /// <param name="capacity">Kapazität in Slots (Standard 24 = Classic-RO-Inventar).</param>
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
                        plan.Add(new StorageDropIntent(i, item, StorageDropAction.Keep, "Geschützt (Quest/Ausrüstung)"));
                        break;
                }
            }

            return plan;
        }

        /// <summary>FEAT-023: Standard-Kapazität eines Classic-RO-Inventars.</summary>
        public const int DefaultCapacity = 24;

        /// <summary>
        /// FEAT-023: Plant with der Standard-Kapazität (24 Slots).
        /// </summary>
        public static IReadOnlyList<StorageDropIntent> PlanDefault(int usedSlots, IReadOnlyList<InventorySlotItem>? slots)
            => Plan(DefaultCapacity, usedSlots, slots);

        /// <summary>
        /// FEAT-023: Zählt die tatsächlich befreienden Aktionen (Stash + Drop) in einem Plan.
        /// </summary>
        public static int CountReleasingActions(IReadOnlyList<StorageDropIntent> plan)
            => plan.Count(x => x.Action != StorageDropAction.Keep);

        /// <summary>
        /// FEAT-023: Prüft, ob ein Plan mindestens <paramref name="neededSlots"/> Slots befreit.
        /// Standard 1 = „ein neuer Pickup is möglich".
        /// </summary>
        public static bool FreesEnough(IReadOnlyList<StorageDropIntent> plan, int neededSlots = 1)
            => CountReleasingActions(plan) >= neededSlots;
    }
}
