using System;
using System.Collections.Generic;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Profiles
{
    /// <summary>
    /// TASK-2301: CharProfile — charakter-spezifisches Profil mit Override-Mechanik.
    /// Wenn eine Eigenschaft nicht gesetzt ist (null/default), wird der Wert aus der Vererbungskette
    /// ClassProfile → BaseProfile aufgelöst.
    /// </summary>
    public class CharProfile : ClassProfile
    {
        /// <summary>Name des Charakters (z.B. "MeinMagier").</summary>
        public string CharName { get; set; } = "";

        /// <summary>Referenz auf das übergeordnete ClassProfile für Override-Auflösung.</summary>
        public ClassProfile? BaseProfileRef { get; set; }

        /// <summary>
        /// TASK-2301: Löst einen Eigenschaftswert über die Vererbungskette auf:
        /// CharProfile-Wert → ClassProfile-Wert → BaseProfile-Default.
        /// </summary>
        /// <typeparam name="T">Der Eigenschaftentyp.</typeparam>
        /// <param name="charValue">Der auf CharProfile gesetzte Wert (kann null/default sein).</param>
        /// <param name="classValue">Der auf ClassProfile gesetzte Wert.</param>
        /// <param name="baseValue">Der auf BaseProfile gesetzte Default-Wert.</param>
        /// <returns>Der erste nicht-null/non-default Wert in der Kette.</returns>
        public static T ResolveOverride<T>(T? charValue, T? classValue, T baseValue) where T : struct
        {
            if (charValue.HasValue && !charValue.Value.Equals(default(T)))
                return charValue.Value;
            if (classValue.HasValue && !classValue.Value.Equals(default(T)))
                return classValue.Value;
            return baseValue;
        }

        /// <summary>
        /// TASK-2301: Löst einen Referenztyp-Wert über die Vererbungskette auf.
        /// </summary>
        public static T? ResolveOverride<T>(T? charValue, T? classValue, T? baseValue) where T : class
        {
            if (!string.IsNullOrEmpty(charValue?.ToString()))
                return charValue;
            if (!string.IsNullOrEmpty(classValue?.ToString()))
                return classValue;
            return baseValue;
        }

        /// <summary>
        /// TASK-2301: Gibt den effektiven Klassennamen zurück (CharProfile-Class oder ClassProfile-RoClass).
        /// </summary>
        public string EffectiveClass
        {
            get
            {
                if (!string.IsNullOrEmpty(Class)) return Class;
                if (BaseProfileRef != null && !string.IsNullOrEmpty(BaseProfileRef.RoClass)) return BaseProfileRef.RoClass;
                return RoClass;
            }
        }

        /// <summary>
        /// TASK-2301: Gibt die effektive Lightbar-Farbe zurück (mit Override-Auflösung).
        /// </summary>
        public string EffectiveLightbarColor
        {
            get
            {
                if (!string.IsNullOrEmpty(LightbarColor)) return LightbarColor;
                if (BaseProfileRef != null && !string.IsNullOrEmpty(BaseProfileRef.LightbarColor)) return BaseProfileRef.LightbarColor;
                return "#005AD2";
            }
        }

        /// <summary>
        /// TASK-2301: Gibt den effektiven Overlay-Theme zurück (mit Override-Auflösung).
        /// </summary>
        public Settings.OverlayThemeType EffectiveOverlayTheme
        {
            get
            {
                if (OverlayTheme != Settings.OverlayThemeType.Neon) return OverlayTheme;
                if (BaseProfileRef != null && BaseProfileRef.OverlayTheme != Settings.OverlayThemeType.Neon) return BaseProfileRef.OverlayTheme;
                return Settings.OverlayThemeType.Neon;
            }
        }
    }
}
