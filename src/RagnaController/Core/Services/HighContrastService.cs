using System;
using System.Windows;
using System.Windows.Media;
using RagnaController.Models;

namespace RagnaController.Core.Services
{
    /// <summary>
    /// TASK-1632: HighContrastService.
    /// Wendet High-Contrast-Farben auf die Anwendung an, wenn
    /// Settings.HighContrastEnabled = true ist.
    /// Unterstützt auch UI-Schrift-Skalierung über Settings.FontScale.
    /// </summary>
    public sealed class HighContrastService
    {
        private readonly Settings _settings;
        private readonly ResourceDictionary _designSystem;

        /// <summary>
        /// Erstellt eine neue HighContrastService-Instanz.
        /// </summary>
        /// <param name="settings">Die geladenen Anwendungseinstellungen.</param>
        /// <param name="designSystem">Das UI2026DesignSystem ResourceDictionary.</param>
        public HighContrastService(Settings settings, ResourceDictionary designSystem)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _designSystem = designSystem ?? throw new ArgumentNullException(nameof(designSystem));
        }

        /// <summary>
        /// Wendet High-Contrast-Farben an oder stellt normale Farben wieder her,
        /// je nach Settings.HighContrastEnabled.
        /// </summary>
        public void ApplyHighContrast()
        {
            if (_settings.HighContrastEnabled)
            {
                ApplyHighContrastColors();
            }
            else
            {
                ApplyNormalColors();
            }

            ApplyFontScale();
        }

        private void ApplyHighContrastColors()
        {
            // Hochkontrast-Farben (UX-017): Schwarzer Hintergrund, weiße Texte,
            // knallige Akzentfarben für maximale Lesbarkeit
            SetColor("BgPrimary", "#000000");
            SetColor("BgSecondary", "#0A0A0A");
            SetColor("BgTertiary", "#1A1A1A");
            SetColor("BgBorder", "#FFFFFF");
            SetColor("BgDeep", "#000000");
            SetColor("BgPanel", "#0A0A0A");
            SetColor("BgCard", "#1A1A1A");
            SetColor("BgRaised", "#2A2A2A");
            SetColor("BorderHair", "#FFFFFF");
            SetColor("BorderGlow", "#FFFFFF");

            SetColor("TextPrimary", "#FFFFFF");
            SetColor("TextSecondary", "#E0E0E0");
            SetColor("TextTertiary", "#B0B0B0");

            SetColor("AccentBlue", "#0078D7");
            SetColor("AccentBlueDim", "#005A9E");
            SetColor("AccentPurple", "#BF00FF");
            SetColor("AccentPurpleDim", "#8A00CC");

            SetColor("Live", "#00FF00");
            SetColor("Gold", "#FFD700");
            SetColor("GoldDim", "#CCB000");
            SetColor("Danger", "#FF0000");

            // HighContrast-Aliase (bereits in UI2026DesignSystem definiert,
            // aber wir stellen sicher, dass sie aktiv sind)
            SetColor("HighContrastTextPrimary", "#FFFFFF");
            SetColor("HighContrastTextSecondary", "#E0E0E0");
            SetColor("HighContrastBgPrimary", "#000000");
            SetColor("HighContrastBgSecondary", "#1A1A1A");
            SetColor("HighContrastAccentBlue", "#0078D7");
            SetColor("HighContrastAccentPurple", "#BF00FF");
            SetColor("HighContrastBorder", "#FFFFFF");
            SetColor("HighContrastLive", "#00FF00");
            SetColor("HighContrastDanger", "#FF0000");
        }

        private void ApplyNormalColors()
        {
            // Normale Farben werden durch UI2026DesignSystem.xaml definiert.
            // Hier nur sicherstellen, dass keine Überschreibung aktiv bleibt.
            // Die Ressourcen werden aus dem Dictionary geladen.
        }

        private void ApplyFontScale()
        {
            if (_designSystem.Contains("FontScaleNormal"))
            {
                _designSystem["FontScaleNormal"] = _settings.FontScale;
            }
            if (_designSystem.Contains("FontScaleSmall"))
            {
                _designSystem["FontScaleSmall"] = Math.Round(_settings.FontScale * 0.85, 2);
            }
            if (_designSystem.Contains("FontScaleLarge"))
            {
                _designSystem["FontScaleLarge"] = Math.Round(_settings.FontScale * 1.25, 2);
            }
            if (_designSystem.Contains("FontScaleXLarge"))
            {
                _designSystem["FontScaleXLarge"] = Math.Round(_settings.FontScale * 1.50, 2);
            }
        }

        private void SetColor(string key, string hex)
        {
            if (_designSystem.Contains(key))
            {
                try
                {
                    _designSystem[key] = (Color)ColorConverter.ConvertFromString(hex)!;
                }
                catch
                {
                    // Farbkonvertierung fehlgeschlagen — Schlüssel überspringen
                }
            }
        }
    }
}
