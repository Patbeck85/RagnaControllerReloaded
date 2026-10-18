using System.Collections.Generic;
using RagnaController.Core;
using RagnaController.Profiles;
using RagnaController.Models;


namespace RagnaController.Core
{
    /// <summary>
    /// Generates pre-built <see cref="RadialItem"/> lists for Ragnarok Online
    /// user-interface navigation via the radial menu.
    ///
    /// Unlike the emote preset (which sends chat commands), UI menu items send
    /// direct keyboard shortcuts to the RO client.  Most panels respond to single
    /// virtual keys; a few require Alt+Key combos (handled via <see cref="RadialItem.ModifierKey"/>).
    ///
    /// Usage — swap the active radial preset at runtime:
    /// <code>
    ///   // In MainWindow: wire to a toggle button or Back+DPadLeft shortcut
    ///   _engine.SetRadialPreset(RoUiMenuService.UiNavigationPreset());
    ///   // Restore emotes:
    ///   _engine.SetRadialPreset(currentProfile.RadialMenuItems);
    /// </code>
    ///
    /// Default RO keyboard shortcuts used (iRO / kRO Renewal client):
    ///   I         — Inventory
    ///   Alt+S     — Status window
    ///   Alt+K     — Skill window
    ///   Alt+E     — Equipment window
    ///   Alt+V     — Party window
    ///   Alt+G     — Guild window
    ///   M         — Mini-map toggle
    ///   F10       — System menu / options
    ///
    /// Keyboard layout note: these shortcuts match the default keybindings
    /// for the original and renewal kRO/iRO clients.  Custom-keybind servers
    /// may use different keys — adjust the <c>Key</c> / <c>ModifierKey</c>
    /// values in the returned list accordingly.
    /// </summary>
    public static class RoUiMenuService
    {
        // ── RO UI panel shortcuts ─────────────────────────────────────────

        /// <summary>
        /// Returns an 8-item radial preset for navigating the core RO UI panels.
        /// Each item is assigned to one of the 8 compass directions.
        /// </summary>
        public static List<RadialItem> UiNavigationPreset() => new()
        {
            // ↑ UP — Inventory  (I — no modifier needed in most clients)
            new RadialItem
            {
                Name        = "📦 BAG",
                Key         = VirtualKey.I,
                ModifierKey = VirtualKey.None,
                IsEmote     = false,
            },

            // ↗ UP-RIGHT — Status window  (Alt+S)
            new RadialItem
            {
                Name        = "📊 STATUS",
                Key         = VirtualKey.S,
                ModifierKey = VirtualKey.AltLeft,
                IsEmote     = false,
            },

            // → RIGHT — Skill window  (Alt+K or K)
            new RadialItem
            {
                Name        = "⚡ SKILLS",
                Key         = VirtualKey.K,
                ModifierKey = VirtualKey.AltLeft,
                IsEmote     = false,
            },

            // ↘ DOWN-RIGHT — Equipment  (Alt+E)
            new RadialItem
            {
                Name        = "🛡 EQUIP",
                Key         = VirtualKey.E,
                ModifierKey = VirtualKey.AltLeft,
                IsEmote     = false,
            },

            // ↓ DOWN — Mini-map  (M)
            new RadialItem
            {
                Name        = "🗺 MAP",
                Key         = VirtualKey.M,
                ModifierKey = VirtualKey.None,
                IsEmote     = false,
            },

            // ↙ DOWN-LEFT — Guild  (Alt+G)
            new RadialItem
            {
                Name        = "⚔ GUILD",
                Key         = VirtualKey.G,
                ModifierKey = VirtualKey.AltLeft,
                IsEmote     = false,
            },

            // ← LEFT — Party  (Alt+V)
            new RadialItem
            {
                Name        = "👥 PARTY",
                Key         = VirtualKey.V,
                ModifierKey = VirtualKey.AltLeft,
                IsEmote     = false,
            },

            // ↖ UP-LEFT — System menu  (F10 — escape / options)
            new RadialItem
            {
                Name        = "⚙ MENU",
                Key         = VirtualKey.F10,
                ModifierKey = VirtualKey.None,
                IsEmote     = false,
            },
        };

        // ── Optional: hotbar row preset ───────────────────────────────────
        // RO skill hotbar rows are switched with Alt+1..Alt+9.
        // Useful for quickly switching between hotbar pages mid-battle.

        /// <summary>
        /// Returns an 8-item preset for switching hotbar row pages (Alt+1 … Alt+8).
        /// </summary>
        public static List<RadialItem> HotbarRowPreset() => new()
        {
            new RadialItem { Name = "ROW 1", Key = VirtualKey.D1, ModifierKey = VirtualKey.AltLeft, IsEmote = false },
            new RadialItem { Name = "ROW 2", Key = VirtualKey.D2, ModifierKey = VirtualKey.AltLeft, IsEmote = false },
            new RadialItem { Name = "ROW 3", Key = VirtualKey.D3, ModifierKey = VirtualKey.AltLeft, IsEmote = false },
            new RadialItem { Name = "ROW 4", Key = VirtualKey.D4, ModifierKey = VirtualKey.AltLeft, IsEmote = false },
            new RadialItem { Name = "ROW 5", Key = VirtualKey.D5, ModifierKey = VirtualKey.AltLeft, IsEmote = false },
            new RadialItem { Name = "ROW 6", Key = VirtualKey.D6, ModifierKey = VirtualKey.AltLeft, IsEmote = false },
            new RadialItem { Name = "ROW 7", Key = VirtualKey.D7, ModifierKey = VirtualKey.AltLeft, IsEmote = false },
            new RadialItem { Name = "ROW 8", Key = VirtualKey.D8, ModifierKey = VirtualKey.AltLeft, IsEmote = false },
        };
    }
}
