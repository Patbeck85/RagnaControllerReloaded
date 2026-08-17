using System;
using System.Runtime.InteropServices;

namespace RagnaController.Core
{
    /// <summary>
    /// Erkennt Handheld-PCs (ROG Ally, Legion Go, Steam Deck, etc.) ohne UI-Thread zu blockieren.
    /// Ergebnis wird gecacht — verwendet plattformunabhängige Erkennung.
    /// </summary>
    public static class HandheldDetector
    {
        private static readonly object _lock = new();
        private static bool? _cache;
        private static string _deviceName = "Unknown";

        public static string DeviceName => _deviceName;

        public static bool IsHandheldDevice()
        {
            lock (_lock)
            {
                if (_cache.HasValue) return _cache.Value;

                try
                {
                    // Plattformunabhängige Erkennung — prüft auf bekannte Handheld-Modelle
                    var deviceName = Environment.MachineName.ToLowerInvariant();
                    
                    // Steam Deck
                    if (deviceName.Contains("steamdeck") || Environment.GetEnvironmentVariable("STEAM_DECK") != null)
                        return Cache(true, "Steam Deck");

                    // ASUS ROG Ally
                    if (deviceName.Contains("rog-ally") || Environment.GetEnvironmentVariable("ROG_ALLY") != null)
                        return Cache(true, "ASUS ROG Ally");

                    // Lenovo Legion Go
                    if (deviceName.Contains("legion-go") || Environment.GetEnvironmentVariable("LEGION_GO") != null)
                        return Cache(true, "Lenovo Legion Go");

                    // AYANEO
                    if (deviceName.Contains("ayaneo") || Environment.GetEnvironmentVariable("AYANEO") != null)
                        return Cache(true, "AYANEO");

                    // GPD
                    if (deviceName.Contains("gpd") || Environment.GetEnvironmentVariable("GPD") != null)
                        return Cache(true, "GPD");

                    // AYN Loki
                    if (deviceName.Contains("ayn") || Environment.GetEnvironmentVariable("AYN") != null)
                        return Cache(true, "Ayn Loki");

                    // OneXPlayer
                    if (deviceName.Contains("onexplayer") || Environment.GetEnvironmentVariable("ONEXPLAYER") != null)
                        return Cache(true, "OneXPlayer");

                    // Fallback: Check if running in handheld mode (environment variable)
                    var handheldMode = Environment.GetEnvironmentVariable("HANDHELD_MODE");
                    if (!string.IsNullOrEmpty(handheldMode))
                        return Cache(true, $"Handheld Mode ({handheldMode})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[HandheldDetector] {ex.Message}");
                }

                return Cache(false, "Desktop");
            }
        }

        private static bool Cache(bool result, string name)
        {
            _cache      = result;
            _deviceName = name;
            return result;
        }

        /// <summary>Cache invalidieren — für Tests oder Settings-Override.</summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _cache = null;
            }
        }
    }
}
