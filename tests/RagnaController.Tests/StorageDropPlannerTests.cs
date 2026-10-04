using System.Collections.Generic;
using RagnaController.Models;
using Xunit;

namespace RagnaController.Tests
{
    /// <summary>
    /// FEAT-023: Unit-Tests für StorageDropPlanner (Auto-Item-Einlagerung bei vollem Inventar).
    /// Reine Planungslogik — headless/CI-sicher, keine UI-/Cursor-Zugriffe.
    /// </summary>
    public class StorageDropPlannerTests
    {
        private static InventorySlotItem Item(StorageDropPriority rarity, string name = "X", int stack = 1)
            => new(name, rarity, stack);

        [Fact]
        public void Plan_NichtVollesInventarGibtLeerenPlan()
        {
            var slots = new List<InventorySlotItem> { Item(StorageDropPriority.Consumable), Item(StorageDropPriority.BuffItem) };
            var plan = StorageDropPlanner.Plan(capacity: 24, usedSlots: 2, slots);
            Assert.Empty(plan);
        }

        [Fact]
        public void Plan_VollesInventarStashConsumables()
        {
            var slots = new List<InventorySlotItem>
            {
                Item(StorageDropPriority.Consumable, "Potion"),
                Item(StorageDropPriority.Equipment, "Sword")
            };
            var plan = StorageDropPlanner.Plan(capacity: 2, usedSlots: 2, slots);

            Assert.Equal(2, plan.Count);
            Assert.Equal(StorageDropAction.Stash, plan[0].Action);
            Assert.Equal("Potion", plan[0].Item.Name);
            Assert.Equal(0, plan[0].SlotIndex);
        }

        [Fact]
        public void Plan_BuffItemsWerdenGedroppt()
        {
            var slots = new List<InventorySlotItem> { Item(StorageDropPriority.BuffItem, "Buff") };
            var plan = StorageDropPlanner.Plan(capacity: 1, usedSlots: 1, slots);

            Assert.Single(plan);
            Assert.Equal(StorageDropAction.Drop, plan[0].Action);
        }

        [Fact]
        public void Plan_QuestUndEquipmentWerdenBehalten()
        {
            var slots = new List<InventorySlotItem>
            {
                Item(StorageDropPriority.QuestItem, "Quest"),
                Item(StorageDropPriority.Equipment, "Sword")
            };
            var plan = StorageDropPlanner.Plan(capacity: 2, usedSlots: 2, slots);

            Assert.Equal(2, plan.Count);
            Assert.Equal(StorageDropAction.Keep, plan[0].Action);
            Assert.Equal(StorageDropAction.Keep, plan[1].Action);
        }

        [Fact]
        public void Plan_SlotReihenfolgeWirdBeibehalten()
        {
            var slots = new List<InventorySlotItem>
            {
                Item(StorageDropPriority.Equipment),   // 0: Keep
                Item(StorageDropPriority.BuffItem),    // 1: Drop
                Item(StorageDropPriority.Consumable)   // 2: Stash
            };
            var plan = StorageDropPlanner.Plan(capacity: 3, usedSlots: 3, slots);

            Assert.Equal(3, plan.Count);
            Assert.Equal(0, plan[0].SlotIndex);
            Assert.Equal(1, plan[1].SlotIndex);
            Assert.Equal(2, plan[2].SlotIndex);
        }

        [Fact]
        public void Plan_NullOderLeereSlotsGibtLeerenPlan()
        {
            Assert.Empty(StorageDropPlanner.Plan(capacity: 1, usedSlots: 1, null));
            Assert.Empty(StorageDropPlanner.Plan(capacity: 1, usedSlots: 1, new List<InventorySlotItem>()));
        }

        [Fact]
        public void Plan_NichtPositivKapazitätGibtLeerenPlan()
        {
            var slots = new List<InventorySlotItem> { Item(StorageDropPriority.Consumable) };
            Assert.Empty(StorageDropPlanner.Plan(capacity: 0, usedSlots: 1, slots));
        }

        [Fact]
        public void PlanDefault_NutztStandardKapazitat24()
        {
            var slots = new List<InventorySlotItem> { Item(StorageDropPriority.Consumable) };
            // 23 belegt → nicht voll (Kapazität 24) → leer.
            Assert.Empty(StorageDropPlanner.PlanDefault(usedSlots: 23, slots));
            // 24 belegt → voll → Plan mit Stash.
            var plan = StorageDropPlanner.PlanDefault(usedSlots: 24, slots);
            Assert.Single(plan);
            Assert.Equal(StorageDropAction.Stash, plan[0].Action);
        }

        [Fact]
        public void CountReleasingActions_ZaehltNurStashUndDrop()
        {
            var slots = new List<InventorySlotItem>
            {
                Item(StorageDropPriority.Consumable),  // Stash
                Item(StorageDropPriority.BuffItem),    // Drop
                Item(StorageDropPriority.QuestItem)    // Keep
            };
            var plan = StorageDropPlanner.Plan(capacity: 3, usedSlots: 3, slots);

            Assert.Equal(2, StorageDropPlanner.CountReleasingActions(plan));
        }

        [Fact]
        public void FreesEnough_StimmtWennGenugFreigegeben()
        {
            var slots = new List<InventorySlotItem>
            {
                Item(StorageDropPriority.Consumable),  // Stash
                Item(StorageDropPriority.QuestItem)    // Keep
            };
            var plan = StorageDropPlanner.Plan(capacity: 2, usedSlots: 2, slots);

            Assert.True(StorageDropPlanner.FreesEnough(plan, neededSlots: 1));   // 1 freigeben → Pickup möglich
            Assert.False(StorageDropPlanner.FreesEnough(plan, neededSlots: 2));  // 2 nötig, nur 1 möglich
        }

        [Fact]
        public void FreesEnough_NurKeepGibtFalse()
        {
            var slots = new List<InventorySlotItem> { Item(StorageDropPriority.Equipment) };
            var plan = StorageDropPlanner.Plan(capacity: 1, usedSlots: 1, slots);

            Assert.False(StorageDropPlanner.FreesEnough(plan));
        }
    }
}
