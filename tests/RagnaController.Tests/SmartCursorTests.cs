using System;
using System.Collections.Generic;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Core.Navigation;

namespace RagnaController.Tests
{
    /// <summary>
    /// TASK-2505: Unit-Tests für Epic 16 (Smart Cursor Extensions).
    /// Deckt ShopNavigationService, StorageOrganizationService, SkillTreeNavigationService, ChatMacroService ab.
    /// </summary>
    public class SmartCursorTests
    {
        #region Mock Classes

        private sealed class MockFeedbackProvider : IFeedbackProvider
        {
            public List<FeedbackType> TriggeredTypes = new();
            public void StopAll() { }
            public void SetLED(byte r, byte g, byte b) { }
            public void Tick() { }
            public void Trigger(FeedbackType type) => TriggeredTypes.Add(type);
            public void TriggerSkillFired() { }
            public void StopRumble() { }
            public void Dispose() { }
        }

        private sealed class MockInputCommandQueue : InputCommandQueue
        {
            public List<(int ItemId, int Quantity)> BoughtItems = new();
            public List<(int ItemId, int Quantity)> SoldItems = new();

            public MockInputCommandQueue() : base() { }

            public override void BuyItem(int itemId, int quantity)
            {
                BoughtItems.Add((itemId, quantity));
            }

            public override void SellItem(int itemId, int quantity)
            {
                SoldItems.Add((itemId, quantity));
            }
        }

        #endregion

        #region ShopNavigationService Tests

        [Fact]
        public void ShopNavigationService_OpenClose_ShouldToggleState()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var shop = new ShopNavigationService(queue, feedback);

            // Act
            shop.OpenShop();
            Assert.True(shop.IsShopOpen);
            Assert.Contains(FeedbackType.ShopOpened, feedback.TriggeredTypes);

            shop.CloseShop();
            Assert.False(shop.IsShopOpen);
            Assert.Contains(FeedbackType.ShopClosed, feedback.TriggeredTypes);
        }

        [Fact]
        public void ShopNavigationService_CategoryNavigation_ShouldChangeCategory()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var shop = new ShopNavigationService(queue, feedback);
            shop.OpenShop();

            // Act
            var result = shop.ProcessInput(new ParsedInput { DPadDown = true });

            // Assert
            Assert.True(result);
            Assert.Equal(1, shop.SelectedCategoryIndex);
            Assert.Equal(0, shop.SelectedItemIndex); // Reset on category change
        }

        [Fact]
        public void ShopNavigationService_ItemNavigation_ShouldChangeItem()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var shop = new ShopNavigationService(queue, feedback);
            shop.OpenShop();
            shop.LoadItems("Waffen", new List<ShopItem>
            {
                new ShopItem { Id = 1, Name = "Dolch", Price = 100 },
                new ShopItem { Id = 2, Name = "Schwert", Price = 500 },
            });

            // Act
            shop.ProcessInput(new ParsedInput { DPadRight = true });

            // Assert
            Assert.Equal(1, shop.SelectedItemIndex);
            var item = shop.GetSelectedItem();
            Assert.Equal("Schwert", item?.Name);
        }

        [Fact]
        public void ShopNavigationService_BuyItem_ShouldQueueBuy()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var shop = new ShopNavigationService(queue, feedback);
            shop.OpenShop();
            shop.LoadItems("Waffen", new List<ShopItem>
            {
                new ShopItem { Id = 1, Name = "Dolch", Price = 100 },
            });
            shop.BuyQuantity = 3;

            // Act
            var result = shop.ProcessInput(new ParsedInput { BtnA = true });

            // Assert
            Assert.True(result);
            Assert.Single(queue.BoughtItems);
            Assert.Equal(1, queue.BoughtItems[0].ItemId);
            Assert.Equal(3, queue.BoughtItems[0].Quantity);
            Assert.Contains(FeedbackType.ItemBought, feedback.TriggeredTypes);
        }

        [Fact]
        public void ShopNavigationService_CloseShop_ShouldConsumeInput()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var shop = new ShopNavigationService(queue, feedback);
            shop.OpenShop();

            // Act
            var result = shop.ProcessInput(new ParsedInput { BtnB = true });

            // Assert
            Assert.True(result);
            Assert.False(shop.IsShopOpen);
        }

        #endregion

        #region StorageOrganizationService Tests

        [Fact]
        public void StorageOrganizationService_AddRemoveItem_ShouldUpdateCount()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var storage = new StorageOrganizationService(queue, feedback);

            // Act
            storage.AddItem(new StorageItem { Id = 1, Name = "Apfel", Quantity = 5 });
            storage.AddItem(new StorageItem { Id = 2, Name = "Brot", Quantity = 3 });

            // Assert
            Assert.Equal(2, storage.Items.Count);
            Assert.Equal(2, storage.UsedSlots);
            Assert.Equal(78, storage.FreeSlots);
        }

        [Fact]
        public void StorageOrganizationService_SortByName_ShouldSortAlphabetically()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var storage = new StorageOrganizationService(queue, feedback);
            storage.AddItem(new StorageItem { Id = 1, Name = "Zelt" });
            storage.AddItem(new StorageItem { Id = 2, Name = "Apfel" });
            storage.AddItem(new StorageItem { Id = 3, Name = "Brot" });

            // Act
            storage.CurrentSortBy = StorageOrganizationService.SortBy.Name;
            storage.SortItems();

            // Assert
            Assert.Equal("Apfel", storage.Items[0].Name);
            Assert.Equal("Brot", storage.Items[1].Name);
            Assert.Equal("Zelt", storage.Items[2].Name);
        }

        [Fact]
        public void StorageOrganizationService_MergeStacks_ShouldCombineSameItems()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var storage = new StorageOrganizationService(queue, feedback);
            storage.AddItem(new StorageItem { Id = 1, Name = "Apfel", Quantity = 5 });
            storage.AddItem(new StorageItem { Id = 1, Name = "Apfel", Quantity = 3 });
            storage.AddItem(new StorageItem { Id = 2, Name = "Brot", Quantity = 2 });

            // Act
            var merged = storage.MergeStacks();

            // Assert
            Assert.Equal(1, merged);
            Assert.Equal(2, storage.Items.Count);
            Assert.Equal(8, storage.Items[0].Quantity);
        }

        [Fact]
        public void StorageOrganizationService_Defragment_ShouldReindexSlots()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var storage = new StorageOrganizationService(queue, feedback);
            storage.AddItem(new StorageItem { Id = 1, Name = "A", SlotIndex = 5 });
            storage.AddItem(new StorageItem { Id = 2, Name = "B", SlotIndex = 2 });
            storage.AddItem(new StorageItem { Id = 3, Name = "C", SlotIndex = 10 });

            // Act
            storage.Defragment();

            // Assert
            Assert.Equal(0, storage.Items[0].SlotIndex);
            Assert.Equal(1, storage.Items[1].SlotIndex);
            Assert.Equal(2, storage.Items[2].SlotIndex);
        }

        [Fact]
        public void StorageOrganizationService_CycleSortBy_ShouldCycleCriteria()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var storage = new StorageOrganizationService(queue, feedback);

            // Act
            Assert.Equal(StorageOrganizationService.SortBy.Name, storage.CurrentSortBy);
            storage.CycleSortBy();
            Assert.Equal(StorageOrganizationService.SortBy.Category, storage.CurrentSortBy);
            storage.CycleSortBy();
            Assert.Equal(StorageOrganizationService.SortBy.Price, storage.CurrentSortBy);
        }

        #endregion

        #region SkillTreeNavigationService Tests

        [Fact]
        public void SkillTreeNavigationService_LevelUp_ShouldConsumePoint()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var skillTree = new SkillTreeNavigationService(queue, feedback);
            skillTree.OpenSkillTree();
            skillTree.AvailableSkillPoints = 5;
            skillTree.BaseLevel = 50;
            skillTree.JobLevel = 30;
            skillTree.AddSkill(new SkillNode
            {
                Id = 1, Name = "Schlag", Level = 0, MaxLevel = 5,
                RequiredBaseLevel = 1, RequiredJobLevel = 1,
            });

            // Act
            var result = skillTree.LevelUpSelectedSkill();

            // Assert
            Assert.True(result);
            Assert.Equal(1, skillTree.GetSelectedSkill()?.Level);
            Assert.Equal(4, skillTree.AvailableSkillPoints);
            Assert.Contains(FeedbackType.TargetLocked, feedback.TriggeredTypes);
        }

        [Fact]
        public void SkillTreeNavigationService_LevelUp_InsufficientPoints_ShouldFail()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var skillTree = new SkillTreeNavigationService(queue, feedback);
            skillTree.OpenSkillTree();
            skillTree.AvailableSkillPoints = 0;
            skillTree.BaseLevel = 50;
            skillTree.JobLevel = 30;
            skillTree.AddSkill(new SkillNode
            {
                Id = 1, Name = "Schlag", Level = 0, MaxLevel = 5,
                RequiredBaseLevel = 1, RequiredJobLevel = 1,
            });

            // Act
            var result = skillTree.LevelUpSelectedSkill();

            // Assert
            Assert.False(result);
            Assert.Equal(0, skillTree.GetSelectedSkill()?.Level);
        }

        [Fact]
        public void SkillTreeNavigationService_LevelUp_PrerequisiteNotMet_ShouldFail()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var skillTree = new SkillTreeNavigationService(queue, feedback);
            skillTree.OpenSkillTree();
            skillTree.AvailableSkillPoints = 5;
            skillTree.BaseLevel = 50;
            skillTree.JobLevel = 30;
            skillTree.AddSkill(new SkillNode
            {
                Id = 1, Name = "Basis", Level = 0, MaxLevel = 5,
                RequiredBaseLevel = 1, RequiredJobLevel = 1,
            });
            skillTree.AddSkill(new SkillNode
            {
                Id = 2, Name = "Fortgeschritten", Level = 0, MaxLevel = 5,
                RequiredBaseLevel = 1, RequiredJobLevel = 1,
                PrerequisiteSkillId = 1,
            });
            skillTree.SelectedSkillIndex = 1;

            // Act
            var result = skillTree.LevelUpSelectedSkill();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SkillTreeNavigationService_GetCompletionPercent_ShouldCalculate()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var skillTree = new SkillTreeNavigationService(queue, feedback);
            skillTree.AddSkill(new SkillNode { Id = 1, Name = "A", Level = 5, MaxLevel = 10 });
            skillTree.AddSkill(new SkillNode { Id = 2, Name = "B", Level = 5, MaxLevel = 10 });

            // Act
            var percent = skillTree.GetCompletionPercent();

            // Assert
            Assert.Equal(50f, percent);
        }

        #endregion

        #region ChatMacroService Tests

        [Fact]
        public void ChatMacroService_SendSelectedMacro_ShouldTriggerFeedback()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var chat = new ChatMacroService(queue, feedback);
            chat.OpenChat();
            chat.SelectedMacroIndex = 0; // "Hallo" macro

            // Act
            chat.SendSelectedMacro();

            // Assert
            Assert.Contains(FeedbackType.TargetLocked, feedback.TriggeredTypes);
        }

        [Fact]
        public void ChatMacroService_NavigateUpDown_ShouldChangeSelection()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var chat = new ChatMacroService(queue, feedback);
            chat.OpenChat();

            // Act
            chat.ProcessInput(new ParsedInput { DPadDown = true });
            chat.ProcessInput(new ParsedInput { DPadDown = true });

            // Assert
            Assert.Equal(2, chat.SelectedMacroIndex);
        }

        [Fact]
        public void ChatMacroService_AddRemoveMacro_ShouldUpdateList()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var chat = new ChatMacroService(queue, feedback);

            // Act
            chat.AddMacro(new ChatMacro { Id = 100, Name = "Test", Text = "/test", Category = "Test" });
            Assert.Equal(9, chat.Macros.Count);

            var removed = chat.RemoveMacro(100);
            Assert.True(removed);
            Assert.Equal(8, chat.Macros.Count);
        }

        [Fact]
        public void ChatMacroService_CloseChat_ShouldConsumeInput()
        {
            // Arrange
            var queue = new MockInputCommandQueue();
            var feedback = new MockFeedbackProvider();
            var chat = new ChatMacroService(queue, feedback);
            chat.OpenChat();

            // Act
            var result = chat.ProcessInput(new ParsedInput { BtnB = true });

            // Assert
            Assert.True(result);
            Assert.False(chat.IsChatOpen);
        }

        #endregion
    }
}
