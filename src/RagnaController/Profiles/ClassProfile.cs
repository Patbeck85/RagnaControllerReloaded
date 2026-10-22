using System;
using System.Collections.Generic;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Profiles
{
    /// <summary>
    /// TASK-2301: ClassProfile — klasesspezifische Defaults (z.B. Mage, Archer, Swordie, Acolyte, Merchant, Thief).
    /// Erbt von BaseProfile und fügt klassenspezifische Einstellungen hinzu.
    /// </summary>
    public class ClassProfile : BaseProfile
    {
        /// <summary>RO-Klasse (Melee, Mage, Archer, Acolyte, Merchant, Thief, etc.).</summary>
        public string RoClass { get; set; } = "Melee";

        /// <summary>Klassenspezifische Skill-Empfehlungen.</summary>
        public List<string> ClassSkillRecommendations { get; set; } = new();

        /// <summary>Beschreibung der Klasse für die UI-Anzeige.</summary>
        public string ClassDescription { get; set; } = "";

        /// <summary>Empfohlene Start-Attribute für diese Klasse.</summary>
        public Dictionary<string, int> RecommendedStats { get; set; } = new();

        /// <summary>
        /// TASK-2301: Erzeugt ein CharProfile auf Basis dieses ClassProfiles mit den übergebenen charakter-spezifischen Werten.
        /// </summary>
        public CharProfile CreateCharProfile(string charName)
        {
            return new CharProfile
            {
                BaseProfileRef = this,
                CharName = charName,
                Class = this.RoClass,
                Name = charName,
            };
        }
    }
}
