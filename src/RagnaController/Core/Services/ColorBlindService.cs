using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using RagnaController.Models;

namespace RagnaController.Core.Services
{
    /// <summary>
    /// TASK-2802: ColorBlindService — Farbpaletten für Farbsehstörungen.
    ///
    /// Unterstützt drei Arten von Farbblindheit:
    ///   • Deuteranopie (Grün-Blindheit, am häufigsten, ~6% der Männer)
    ///   • Protanopie (Rot-Blindheit, ~1% der Männer)
    ///   • Tritanopie (Blau-Gelb-Blindheit, selten, <0.01%)
    ///
    /// Die Paletten werden auf das UI2026DesignSystem angewendet, sodass
    /// wichtige UI-Elemente auch für farbenblinde Nutzer unterscheidbar bleiben.
    /// </summary>
    public sealed class ColorBlindService
    {
        private readonly Settings _settings;
        private readonly ResourceDictionary _designSystem;
        private ColorBlindMode _currentMode = ColorBlindMode.None;

        /// <summary>Aktueller Farbblindheits-Modus.</summary>
        public ColorBlindMode CurrentMode => _currentMode;

        public ColorBlindService(Settings settings, ResourceDictionary designSystem)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _designSystem = designSystem ?? throw new ArgumentNullException(nameof(designSystem));
        }

        /// <summary>Wendet den angegebenen Farbblindheits-Modus an.</summary>
        public void SetMode(ColorBlindMode mode)
        {
            if (_currentMode == mode)
                return;

            _currentMode = mode;
            _settings.ColorBlindMode = mode;

            switch (mode)
            {
                case ColorBlindMode.None:
                    ApplyNormalColors();
                    break;
                case ColorBlindMode.Deuteranopia:
                    ApplyDeuteranopia();
                    break;
                case ColorBlindMode.Protanopia:
                    ApplyProtanopia();
                    break;
                case ColorBlindMode.Tritanopia:
                    ApplyTritanopia();
                    break;
            }
        }

        /// <summary>Schaltet zwischen den Modi durch (None → Deuteranopia → Protanopia → Tritanopia → None).</summary>
        public ColorBlindMode CycleMode()
        {
            var next = _currentMode switch
            {
                ColorBlindMode.None => ColorBlindMode.Deuteranopia,
                ColorBlindMode.Deuteranopia => ColorBlindMode.Protanopia,
                ColorBlindMode.Protanopia => ColorBlindMode.Tritanopia,
                ColorBlindMode.Tritanopia => ColorBlindMode.None,
                _ => ColorBlindMode.None,
            };
            SetMode(next);
            return next;
        }

        /// <summary>Setzt normale Farben zurück.</summary>
        private void ApplyNormalColors()
        {
            // Normale Farben bleiben aus UI2026DesignSystem — keine Überschreibung nötig
        }

        /// <summary>Deuteranopie: Grün und Rot werden schwer unterscheidbar.
        /// Ersetzt problematische Farben durch sichere Alternativen (Blau/Gelb-Kontraste).</summary>
        private void ApplyDeuteranopia()
        {
            // AccentBlue behalten (gut sichtbar für Deuteranopie)
            SetColor("AccentBlue", "#0078D7");
            SetColor("AccentBlueDim", "#005A9E");
            // AccentPurple → Blau (violett ist oft problematisch)
            SetColor("AccentPurple", "#6A5ACD");
            SetColor("AccentPurpleDim", "#483D8B");
            // Live/Grün → Orange/Gelb (statt Grün)
            SetColor("Live", "#FFA500");
            SetColor("Green", "#FFA500");
            // Gold behalten
            SetColor("Gold", "#FFD700");
            SetColor("GoldDim", "#CCB000");
            // Danger/Rot → Dunkelrot (blasser)
            SetColor("Danger", "#8B0000");
            // Text bleibt
            SetColor("TextPrimary", "#FFFFFF");
            SetColor("TextSecondary", "#E0E0E0");
            SetColor("TextTertiary", "#B0B0B0");
        }

        /// <summary>Protanopie: Rot und Grün werden schwer unterschieden.
        /// Blau und Gelb bleiben gut unterscheidbar.</summary>
        private void ApplyProtanopia()
        {
            // Blau als Hauptakzent behalten
            SetColor("AccentBlue", "#0078D7");
            SetColor("AccentBlueDim", "#005A9E");
            // Violett → Blau
            SetColor("AccentPurple", "#4169E1");
            SetColor("AccentPurpleDim", "#27408B");
            // Rot → Dunkelblau
            SetColor("Danger", "#00008B");
            // Grün → Gelb/Orange
            SetColor("Live", "#FFA500");
            SetColor("Green", "#FFA500");
            SetColor("Gold", "#FFD700");
            SetColor("GoldDim", "#CCB000");
            SetColor("TextPrimary", "#FFFFFF");
            SetColor("TextSecondary", "#E0E0E0");
            SetColor("TextTertiary", "#B0B0B0");
        }

        /// <summary>Tritanopie: Blau und Gelb werden schwer unterschieden.
        /// Rot und Grün bleiben gut unterscheidbar.</summary>
        private void ApplyTritanopia()
        {
            // Blau → Grün/Teal (statt Blau)
            SetColor("AccentBlue", "#008080");
            SetColor("AccentBlueDim", "#006666");
            // Violett → Magenta/Rot
            SetColor("AccentPurple", "#C71585");
            SetColor("AccentPurpleDim", "#8B004B");
            // Rot/Grün behalten
            SetColor("Live", "#39FF8C");
            SetColor("Green", "#39FF8C");
            SetColor("Danger", "#FF5252");
            // Gold → Orange (statt Gelb)
            SetColor("Gold", "#FF8C00");
            SetColor("GoldDim", "#CC6600");
            SetColor("TextPrimary", "#FFFFFF");
            SetColor("TextSecondary", "#E0E0E0");
            SetColor("TextTertiary", "#B0B0B0");
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

    /// <summary>
    /// Unterstützte Farbblindheits-Modi.
    /// </summary>
    public enum ColorBlindMode
    {
        /// <summary>Keine Farbblindheits-Anpassung (Standard).</summary>
        None,
        /// <summary>Deuteranopie (Grün-Blindheit) — Blau/Gelb-Kontraste.</summary>
        Deuteranopia,
        /// <summary>Protanopie (Rot-Blindheit) — Blau/Gelb-Kontraste.</summary>
        Protanopia,
        /// <summary>Tritanopie (Blau-Gelb-Blindheit) — Rot/Grün-Kontraste.</summary>
        Tritanopia,
    }
}
