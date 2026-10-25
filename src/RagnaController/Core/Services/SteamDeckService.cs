using System;
using System.IO;

namespace RagnaController.Core.Services
{
    /// <summary>
    /// TASK-3305: SteamDeckService — Steam Deck Unterstützung und Optimierungen.
    ///
    /// Bietet Detektion und Konfiguration für den Steam Deck:
    ///   • Steam Deck Hardware-Detektion
    ///   • Big Picture Mode Optimierungen
    ///   • Performance-Einstellungen für Steam Deck APU
    ///   • UI-Skalierung für 1280x800 Display
    ///   • Controller-Eingabe-Optimierung
    ///
    /// White-Hat: Nur UI/Performance-Steuerung, keine Spielautomatisierung.
    /// </summary>
    public sealed class SteamDeckService
    {
        private readonly string? _steamDeckModel;

        public SteamDeckService()
        {
            _steamDeckModel = DetectSteamDeckModel();
        }

        /// <summary>Ob auf einem Steam Deck ausgeführt wird.</summary>
        public bool IsSteamDeck => _steamDeckModel != null;

        /// <summary>Erkanntes Steam Deck Modell (z.B. "Steam Deck", "Steam Deck OLED").</summary>
        public string? SteamDeckModel => _steamDeckModel;

        /// <summary>Ob Big Picture Mode aktiv ist.</summary>
        public bool IsBigPictureMode => Environment.GetEnvironmentVariable("SteamDeck") == "1" ||
                                        Environment.GetEnvironmentVariable("STEAM_BIGPICTURE") == "1";

        /// <summary>Empfohlene UI-Skalierung für Steam Deck (1280x800).</summary>
        public float RecommendedUIScale => IsSteamDeck ? 1.25f : 1.0f;

        /// <summary>Empfohlene Schriftgrößen-Skalierung.</summary>
        public float RecommendedFontScale => IsSteamDeck ? 1.25f : 1.0f;

        /// <summary>Ob die Limited Performance Mode empfohlen wird (für Akku-Laufzeit).</summary>
        public bool RecommendLimitedPerformance => IsSteamDeck;

        /// <summary>Empfohlene FPS-Grenze für Steam Deck.</summary>
        public int RecommendedFpsLimit => IsSteamDeck ? 60 : 0; // 0 = unbegrenzt

        /// <summary>Erkennt das Steam Deck Modell über Umgebungsvariablen und Hardware-Informationen.</summary>
        private static string? DetectSteamDeckModel()
        {
            // Steam Deck wird über Umgebungsvariablen oder DMI-Informationen erkannt
            var steamDeck = Environment.GetEnvironmentVariable("SteamDeck");
            if (!string.IsNullOrEmpty(steamDeck))
                return "Steam Deck";

            // Versuchen, über /sys/class/dmi/id/product_name zu erkennen (Linux)
            try
            {
                var dmiPath = "/sys/class/dmi/id/product_name";
                if (File.Exists(dmiPath))
                {
                    var productName = File.ReadAllText(dmiPath).Trim();
                    if (productName.Contains("Steam Deck", StringComparison.OrdinalIgnoreCase))
                    {
                        if (productName.Contains("OLED", StringComparison.OrdinalIgnoreCase))
                            return "Steam Deck OLED";
                        return "Steam Deck";
                    }
                }
            }
            catch
            {
                // Datei nicht lesbar oder nicht vorhanden
            }

            return null;
        }
    }

    /// <summary>
    /// Steam Deck Performance-Einstellungen.
    /// </summary>
    public class SteamDeckPerformanceSettings
    {
        public int FpsLimit { get; set; } = 60;
        public bool LimitedPerformance { get; set; } = true;
        public float UpscaleFactor { get; set; } = 1.0f;
        public bool ReduceShadowQuality { get; set; } = true;
        public bool ReduceTextureQuality { get; set; } = false;
        public int RefreshRate { get; set; } = 60;

        public static SteamDeckPerformanceSettings CreateForSteamDeck()
        {
            return new SteamDeckPerformanceSettings
            {
                FpsLimit = 60,
                LimitedPerformance = true,
                UpscaleFactor = 1.0f,
                ReduceShadowQuality = true,
                ReduceTextureQuality = false,
                RefreshRate = 60,
            };
        }

        public static SteamDeckPerformanceSettings CreateForDesktop()
        {
            return new SteamDeckPerformanceSettings
            {
                FpsLimit = 0, // unbegrenzt
                LimitedPerformance = false,
                UpscaleFactor = 1.0f,
                ReduceShadowQuality = false,
                ReduceTextureQuality = false,
                RefreshRate = 144,
            };
        }
    }
}
