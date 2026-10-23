using System;
using System.Collections.Generic;

namespace RagnaController.Models
{
    /// <summary>
    /// Element eines Monsters (beeinflusst Schaden, Resistenzen).
    /// </summary>
    public enum MonsterElement
    {
        Neutral,
        Water,
        Earth,
        Fire,
        Wind,
        Poison,
        Holy,
        Shadow,
        Ghost,
        Undead
    }

    /// <summary>
    /// Größe eines Monsters (beeinflusst Trefferfläche, Knockback).
    /// </summary>
    public enum MonsterSize
    {
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// Rasse eines Monsters (beeinflusst Rassen-spezifische Skills/Waffen).
    /// </summary>
    public enum MonsterRace
    {
        Formless,
        Undead,
        Beast,
        Plant,
        Insect,
        Fish,
        Demon,
        DemiHuman,
        Angel,
        Dragon
    }

    /// <summary>
    /// Stellt einen Ragnarok Online Monster-Eintrag dar mit Pre-Renewal und Renewal spezifischen Daten.
    /// Je nach aktivem GameMode (Pre-Renewal / Renewal) werden unterschiedliche Werte zurückgegeben.
    /// </summary>
    public class Monster
    {
        /// <summary>Eindeutige Monster-ID.</summary>
        public int Id { get; set; }

        /// <summary>Name des Monsters.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Anzeigename (kann lokalisiert werden).</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Element (Neutral, Water, Fire, etc.).</summary>
        public MonsterElement Element { get; set; } = MonsterElement.Neutral;

        /// <summary>Größe (Small, Medium, Large).</summary>
        public MonsterSize Size { get; set; } = MonsterSize.Medium;

        /// <summary>Rasse (Formless, Undead, Beast, etc.).</summary>
        public MonsterRace Race { get; set; } = MonsterRace.Formless;

        /// <summary>Ob es ein MVP / Miniboss ist.</summary>
        public bool IsMvp { get; set; }

        /// <summary>Ob es ein Miniboss ist.</summary>
        public bool IsMiniboss { get; set; }

        /// <summary>Pre-Renewal: Maximale HP.</summary>
        public int PreRenewalHp { get; set; }

        /// <summary>Renewal: Maximale HP.</summary>
        public int RenewalHp { get; set; }

        /// <summary>Pre-Renewal: EXP bei Tod.</summary>
        public int PreRenewalExp { get; set; }

        /// <summary>Renewal: EXP bei Tod.</summary>
        public int RenewalExp { get; set; }

        /// <summary>Pre-Renewal: JEXP bei Tod.</summary>
        public int PreRenewalJobExp { get; set; }

        /// <summary>Renewal: JEXP bei Tod.</summary>
        public int RenewalJobExp { get; set; }

        /// <summary>Pre-Renewal: Base Level.</summary>
        public int PreRenewalBaseLevel { get; set; }

        /// <summary>Renewal: Base Level.</summary>
        public int RenewalBaseLevel { get; set; }

        /// <summary>Pre-Renewal: Drops (Item-ID -> Chance in 1/10000).</summary>
        public Dictionary<int, int> PreRenewalDrops { get; set; } = new();

        /// <summary>Renewal: Drops (Item-ID -> Chance in 1/10000).</summary>
        public Dictionary<int, int> RenewalDrops { get; set; } = new();

        /// <summary>Pre-Renewal: Steal-Items (Item-ID -> Chance in 1/10000).</summary>
        public Dictionary<int, int> PreRenewalSteal { get; set; } = new();

        /// <summary>Renewal: Steal-Items (Item-ID -> Chance in 1/10000).</summary>
        public Dictionary<int, int> RenewalSteal { get; set; } = new();

        /// <summary>Spawn-Karten (z.B. "pay_fild01", "prt_fild08").</summary>
        public List<string> SpawnMaps { get; set; } = new();

        /// <summary>Beschreibung / Notizen.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Gibt die HP je nach GameMode zurück.</summary>
        public int GetHp(bool isRenewal) => isRenewal ? RenewalHp : PreRenewalHp;

        /// <summary>Gibt die EXP je nach GameMode zurück.</summary>
        public int GetExp(bool isRenewal) => isRenewal ? RenewalExp : PreRenewalExp;

        /// <summary>Gibt die JEXP je nach GameMode zurück.</summary>
        public int GetJobExp(bool isRenewal) => isRenewal ? RenewalJobExp : PreRenewalJobExp;

        /// <summary>Gibt das Base Level je nach GameMode zurück.</summary>
        public int GetBaseLevel(bool isRenewal) => isRenewal ? RenewalBaseLevel : PreRenewalBaseLevel;

        /// <summary>Gibt die Drops je nach GameMode zurück.</summary>
        public Dictionary<int, int> GetDrops(bool isRenewal) => isRenewal ? RenewalDrops : PreRenewalDrops;

        /// <summary>Gibt die Steal-Items je nach GameMode zurück.</summary>
        public Dictionary<int, int> GetSteal(bool isRenewal) => isRenewal ? RenewalSteal : PreRenewalSteal;

        /// <summary>Erstellt eine flache Kopie des Monsters.</summary>
        public Monster Clone()
        {
            return new Monster
            {
                Id = Id,
                Name = Name,
                DisplayName = DisplayName,
                Element = Element,
                Size = Size,
                Race = Race,
                IsMvp = IsMvp,
                IsMiniboss = IsMiniboss,
                PreRenewalHp = PreRenewalHp,
                RenewalHp = RenewalHp,
                PreRenewalExp = PreRenewalExp,
                RenewalExp = RenewalExp,
                PreRenewalJobExp = PreRenewalJobExp,
                RenewalJobExp = RenewalJobExp,
                PreRenewalBaseLevel = PreRenewalBaseLevel,
                RenewalBaseLevel = RenewalBaseLevel,
                PreRenewalDrops = new Dictionary<int, int>(PreRenewalDrops),
                RenewalDrops = new Dictionary<int, int>(RenewalDrops),
                PreRenewalSteal = new Dictionary<int, int>(PreRenewalSteal),
                RenewalSteal = new Dictionary<int, int>(RenewalSteal),
                SpawnMaps = new List<string>(SpawnMaps),
                Description = Description
            };
        }
    }
}
