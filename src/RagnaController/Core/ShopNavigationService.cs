using System;
using System.Collections.Generic;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// TASK-2501: ShopNavigationService — Automatisches Navigieren in Shop-/NPC-Menüs.
    ///
    /// Ermöglicht das bequeme Kaufen und Verkaufen von Items über Controller-Eingaben
    /// in RO Shop-Menüs. Unterstützt:
    ///   • Kategorien-Auswahl (D-Pad Hoch/Runter)
    ///   • Item-Auswahl innerhalb einer Kategorie (D-Pad Links/Rechts)
    ///   • Menge einstellen (Trigger LT/RT)
    ///   • Kaufen (A-Button) / Verkaufen (X-Button)
    ///   • Shop verlassen (B-Button / Back)
    /// </summary>
    public sealed class ShopNavigationService
    {
        private readonly InputCommandQueue _queue;
        private readonly IFeedbackProvider _feedback;

        // ── Shop State ──────────────────────────────────────────────────────
        public bool IsShopOpen { get; private set; }
        public int SelectedCategoryIndex { get; private set; } = 0;
        public int SelectedItemIndex { get; private set; } = 0;
        public int BuyQuantity { get; set; } = 1;

        // ── Configuration ───────────────────────────────────────────────────
        public int MaxBuyQuantity { get; set; } = 999;
        public int QuantityStep { get; set; } = 10;

        // ── Categories ──────────────────────────────────────────────────────
        public List<string> Categories { get; } = new()
        {
            "Waffen",
            "Rüstungen",
            "Zubehör",
            "Heilung",
            "Sonstiges",
        };

        // ── Items per Category (placeholder, wird vom Spiel gefüllt) ─────────
        public Dictionary<string, List<ShopItem>> ItemsByCategory { get; } = new();

        public ShopNavigationService(InputCommandQueue queue, IFeedbackProvider feedback)
        {
            _queue = queue;
            _feedback = feedback;
        }

        /// <summary>Öffnet den Shop-Navigationsmodus.</summary>
        public void OpenShop()
        {
            IsShopOpen = true;
            SelectedCategoryIndex = 0;
            SelectedItemIndex = 0;
            BuyQuantity = 1;
            _feedback.Trigger(FeedbackType.ShopOpened);
        }

        /// <summary>Schließt den Shop-Navigationsmodus.</summary>
        public void CloseShop()
        {
            IsShopOpen = false;
            SelectedCategoryIndex = 0;
            SelectedItemIndex = 0;
            BuyQuantity = 1;
            _feedback.Trigger(FeedbackType.ShopClosed);
        }

        /// <summary>
        /// Verarbeitet Controller-Eingaben für Shop-Navigation.
        /// Sollte pro Frame aufgerufen werden, wenn IsShopOpen == true.
        /// </summary>
        /// <param name="input">Aktueller ParsedInput.</param>
        /// <returns>True, wenn die Eingabe konsumiert wurde.</returns>
        public bool ProcessInput(ParsedInput input)
        {
            if (!IsShopOpen)
                return false;

            // ── Shop verlassen (B / Back) ───────────────────────────────────
            if (input.BtnB || input.Back)
            {
                CloseShop();
                return true;
            }

            // ── Kategorie wechseln (D-Pad Hoch/Runter) ──────────────────────
            if (input.DPadUp)
            {
                SelectedCategoryIndex = Math.Clamp(SelectedCategoryIndex - 1, 0, Categories.Count - 1);
                SelectedItemIndex = 0;
                _feedback.Trigger(FeedbackType.Warning);
                return true;
            }
            if (input.DPadDown)
            {
                SelectedCategoryIndex = Math.Clamp(SelectedCategoryIndex + 1, 0, Categories.Count - 1);
                SelectedItemIndex = 0;
                _feedback.Trigger(FeedbackType.Warning);
                return true;
            }

            // ── Item innerhalb Kategorie wechseln (D-Pad Links/Rechts) ──────
            var category = Categories[SelectedCategoryIndex];
            if (ItemsByCategory.TryGetValue(category, out var items) && items.Count > 0)
            {
                if (input.DPadRight)
                {
                    SelectedItemIndex = Math.Clamp(SelectedItemIndex + 1, 0, items.Count - 1);
                    _feedback.Trigger(FeedbackType.Warning);
                    return true;
                }
                if (input.DPadLeft)
                {
                    SelectedItemIndex = Math.Clamp(SelectedItemIndex - 1, 0, items.Count - 1);
                    _feedback.Trigger(FeedbackType.Warning);
                    return true;
                }
            }

            // ── Menge einstellen (LT = verringern, RT = erhöhen) ────────────
            if (input.LT > 0.5f)
            {
                BuyQuantity = Math.Max(1, BuyQuantity - QuantityStep);
                _feedback.Trigger(FeedbackType.Warning);
                return true;
            }
            if (input.RT > 0.5f)
            {
                BuyQuantity = Math.Min(MaxBuyQuantity, BuyQuantity + QuantityStep);
                _feedback.Trigger(FeedbackType.Warning);
                return true;
            }

            // ── Kaufen (A-Button) ───────────────────────────────────────────
            if (input.BtnA)
            {
                BuySelectedItem();
                return true;
            }

            // ── Verkaufen (X-Button) ────────────────────────────────────────
            if (input.BtnX)
            {
                SellSelectedItem();
                return true;
            }

            return false;
        }

        /// <summary>Kauft das aktuell ausgewählte Item in der eingestellten Menge.</summary>
        private void BuySelectedItem()
        {
            var category = Categories[SelectedCategoryIndex];
            if (!ItemsByCategory.TryGetValue(category, out var items) || items.Count == 0)
                return;

            var item = items[SelectedItemIndex];
            _queue.BuyItem(item.Id, BuyQuantity);
            _feedback.Trigger(FeedbackType.ItemBought);
        }

        /// <summary>Verkauft das aktuell ausgewählte Item.</summary>
        private void SellSelectedItem()
        {
            var category = Categories[SelectedCategoryIndex];
            if (!ItemsByCategory.TryGetValue(category, out var items) || items.Count == 0)
                return;

            var item = items[SelectedItemIndex];
            _queue.SellItem(item.Id, 1);
            _feedback.Trigger(FeedbackType.ItemSold);
        }

        /// <summary>Gibt das aktuell ausgewählte Item zurück (für UI-Anzeige).</summary>
        public ShopItem? GetSelectedItem()
        {
            var category = Categories[SelectedCategoryIndex];
            if (ItemsByCategory.TryGetValue(category, out var items) && items.Count > 0)
                return items[SelectedItemIndex];
            return null;
        }

        /// <summary>Lädt Items für eine Kategorie (wird vom Spiel-Overlay befüllt).</summary>
        public void LoadItems(string category, List<ShopItem> items)
        {
            ItemsByCategory[category] = items;
        }
    }

    /// <summary>
    /// Repräsentiert ein Item im Shop.
    /// </summary>
    public class ShopItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Price { get; set; }
        public int Stock { get; set; } = -1; // -1 = unendlich
        public string Description { get; set; } = "";
    }
}
