using System;
using DiscordRPC;
using RagnaController.Profiles;
using RagnaController.Models;

namespace RagnaController.Core
{
    public sealed class DiscordRpcService : IDisposable
    {
        // Placeholder ID. The developer will replace this later.
        public const string DiscordClientId = "1234567890123456789"; 
        
        private DiscordRpcClient? _client;
        private bool _isEnabled;

        public DiscordRpcService(Settings settings)
        {
            _isEnabled = settings.EnableDiscordRPC;
            if (_isEnabled) Initialize();
        }

        private void Initialize()
        {
            try
            {
                _client = new DiscordRpcClient(DiscordClientId);
                _client.Initialize();
                SetIdlePresence();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DiscordRPC] Failed to initialize: {ex.Message}");
            }
        }

        public void SetEnabled(bool enable)
        {
            if (_isEnabled == enable) return;
            _isEnabled = enable;

            if (enable)
            {
                Initialize();
            }
            else
            {
                _client?.ClearPresence();
                _client?.Dispose();
                _client = null;
            }
        }

        public void UpdateProfile(Profile p)
        {
            if (!_isEnabled || _client == null) return;

            string gameMode = p.Name.Contains("(PRE)") ? "Pre-Renewal" : "Renewal"; // Or use the app's current state

            _client.SetPresence(new RichPresence()
            {
                Details = $"Class: {p.Class}",
                State = $"Profile: {p.Name}",
                Assets = new Assets()
                {
                    LargeImageKey = "ragnacontroller_logo", // Must be uploaded to Discord Portal
                    LargeImageText = $"RagnaController v{AppVersion.Current}",
                    SmallImageKey = GetClassIcon(p.Class),
                    SmallImageText = p.Class
                }
            });
        }

        public void SetIdlePresence()
        {
            if (!_isEnabled || _client == null) return;

            _client.SetPresence(new RichPresence()
            {
                Details = "In Menus",
                State = "Idle",
                Assets = new Assets()
                {
                    LargeImageKey = "ragnacontroller_logo",
                    LargeImageText = $"RagnaController v{AppVersion.Current}"
                }
            });
        }

        private static string GetClassIcon(string className)
        {
            // Maps the class name to asset names uploaded in the Discord Developer Portal
            string lower = className.ToLowerInvariant();
            if (lower.Contains("mage") || lower.Contains("wizard") || lower.Contains("sage")) return "icon_mage";
            if (lower.Contains("archer") || lower.Contains("hunter") || lower.Contains("sniper") || lower.Contains("bard") || lower.Contains("dancer")) return "icon_archer";
            if (lower.Contains("thief") || lower.Contains("assassin") || lower.Contains("rogue")) return "icon_thief";
            if (lower.Contains("acolyte") || lower.Contains("priest") || lower.Contains("monk")) return "icon_acolyte";
            if (lower.Contains("merchant") || lower.Contains("blacksmith") || lower.Contains("alchemist")) return "icon_merchant";
            if (lower.Contains("swordsman") || lower.Contains("knight") || lower.Contains("crusader")) return "icon_swordsman";
            return "icon_melee"; // fallback
        }

        public void Dispose()
        {
            _client?.ClearPresence();
            _client?.Dispose();
        }
    }
}
