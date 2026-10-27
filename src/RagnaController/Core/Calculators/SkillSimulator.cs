using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RagnaController.Core.Calculators {
    /// <summary>
    /// TASK-3004: SkillSimulator — Skill-Punkte verteilen → Build-Code für Forums/Guides.
    ///
    /// Ermöglicht das virtuelle Verteilen von Skill-Punkten und generiert
    /// Build-Code (Text) für Foren und Guides.
    /// White-Hat: Reine Planung, keine automatische In-Game-Verteilung.
    /// </summary>
    public sealed class SkillSimulator
    {
        private readonly List<SimSkill> _skills = new();
        private int _availablePoints;

        public SkillSimulator(int baseLevel, int jobLevel)
        {
            BaseLevel = baseLevel;
            JobLevel = jobLevel;
            _availablePoints = CalculateTotalPoints(baseLevel);
        }

        public int BaseLevel { get; }
        public int JobLevel { get; }
        public int AvailablePoints => _availablePoints;
        public int TotalPointsSpent => _skills.Sum(s => s.Level);
        public int PointsRemaining => _availablePoints - TotalPointsSpent;

        public IReadOnlyList<SimSkill> Skills => _skills;

        public void AddSkill(int id, string name, int maxLevel, int minBaseLevel, int minJobLevel,
            string description = "", string skillType = "Active", string iconPath = "")
        {
            _skills.Add(new SimSkill
            {
                Id = id,
                Name = name,
                Description = description,
                SkillType = skillType,
                IconPath = iconPath,
                MaxLevel = maxLevel,
                MinBaseLevel = minBaseLevel,
                MinJobLevel = minJobLevel
            });
        }

        /// <summary>Fügt ein Skill mit Voraussetzungen hinzu.</summary>
        public void AddSkillWithPrereq(int id, string name, int maxLevel, int minBaseLevel, int minJobLevel,
            Dictionary<int, int> prerequisites, string description = "", string skillType = "Active")
        {
            _skills.Add(new SimSkill
            {
                Id = id,
                Name = name,
                Description = description,
                SkillType = skillType,
                MaxLevel = maxLevel,
                MinBaseLevel = minBaseLevel,
                MinJobLevel = minJobLevel,
                PrerequisiteSkills = prerequisites
            });
        }

        public bool CanUpgrade(SimSkill skill) =>
            skill.Level < skill.MaxLevel &&
            PointsRemaining > 0 &&
            BaseLevel >= skill.MinBaseLevel &&
            JobLevel >= skill.MinJobLevel &&
            PrerequisitesMet(skill);

        private bool PrerequisitesMet(SimSkill skill)
        {
            foreach (var (prereqId, reqLevel) in skill.PrerequisiteSkills)
            {
                var prereqSkill = _skills.FirstOrDefault(s => s.Id == prereqId);
                if (prereqSkill == null || prereqSkill.Level < reqLevel)
                    return false;
            }
            return true;
        }

        public bool UpgradeSkill(SimSkill skill)
        {
            if (!CanUpgrade(skill)) return false;
            skill.Level++;
            return true;
        }

        public bool DowngradeSkill(SimSkill skill)
        {
            if (skill.Level <= 0) return false;
            skill.Level--;
            return true;
        }

        public string GenerateBuildCode()
        {
            return GenerateBuildCode(BuildCodeFormat.PlainText);
        }

        /// <summary>Export format for skill build code.</summary>
        public enum BuildCodeFormat
        {
            PlainText,
            IroWiki,
            Json
        }

        /// <summary>Generates build code in the specified format.</summary>
        public string GenerateBuildCode(BuildCodeFormat format)
        {
            return format switch
            {
                BuildCodeFormat.IroWiki => GenerateIroWikiCode(),
                BuildCodeFormat.Json => GenerateJsonCode(),
                _ => GeneratePlainTextCode()
            };
        }

        private string GeneratePlainTextCode()
        {
            var lines = new List<string>();
            lines.Add("// RagnaController Skill Build");
            lines.Add($"// Base Level: {BaseLevel}, Job Level: {JobLevel}");
            lines.Add($"// Total Points: {_availablePoints}, Spent: {TotalPointsSpent}, Remaining: {PointsRemaining}");
            lines.Add("");

            foreach (var skill in _skills.Where(s => s.Level > 0))
            {
                lines.Add($"{skill.Name}: {skill.Level}/{skill.MaxLevel}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string GenerateIroWikiCode()
        {
            var lines = new List<string>();
            lines.Add("{| class=\"wikitable\"");
            lines.Add("! Skill !! Level !! Max Level");
            foreach (var skill in _skills.Where(s => s.Level > 0))
            {
                lines.Add($"|- | [[{skill.Name}]] || {skill.Level} || {skill.MaxLevel}");
            }
            lines.Add("|}");
            return string.Join(Environment.NewLine, lines);
        }

        private string GenerateJsonCode()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"baseLevel\": {BaseLevel},");
            sb.AppendLine($"  \"jobLevel\": {JobLevel},");
            sb.AppendLine($"  \"totalPoints\": {_availablePoints},");
            sb.AppendLine($"  \"pointsSpent\": {TotalPointsSpent},");
            sb.AppendLine($"  \"pointsRemaining\": {PointsRemaining},");
            sb.AppendLine($"  \"skills\": [");
            var skillLines = _skills.Where(s => s.Level > 0).Select(s =>
                $"    {{\"id\": {s.Id}, \"name\": \"{s.Name}\", \"level\": {s.Level}, \"maxLevel\": {s.MaxLevel}}}");
            sb.AppendLine(string.Join(",\n", skillLines));
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        public void Reset()
        {
            foreach (var skill in _skills) skill.Level = 0;
        }

        /// <summary>Lädt vordefinierte RO-Skills für eine Jobklasse.</summary>
        public static void LoadClassSkills(SkillSimulator sim, string className)
        {
            var skills = className.ToLowerInvariant() switch
            {
                // ── 1. Klasse (Base Jobs) ──
                "novice" => NoviceSkills(),
                "swordman" => SwordmanSkills(),
                "mage" => MageSkills(),
                "archer" => ArcherSkills(),
                "acolyte" => AcolyteSkills(),
                "merchant" => MerchantSkills(),
                "thief" => ThiefSkills(),

                // ── 2. Klasse (Pre-Renewal 2nd Jobs) ──
                "knight" => KnightSkills(),
                "crusader" => CrusaderSkills(),
                "wizard" => WizardSkills(),
                "hunter" => HunterSkills(),
                "priest" => PriestSkills(),
                "blacksmith" or "creator" => BlacksmithSkills(),
                "assassin" => AssassinSkills(),

                // ── Transcendent 2nd Jobs ──
                "lord knight" => LordKnightSkills(),
                "high wizard" => HighWizardSkills(),
                "sniper" => SniperSkills(),
                "high priest" => HighPriestSkills(),
                "professor" => ProfessorSkills(),
                "stalker" or "assassin cross" => StalkerSkills(),

                // ── 3. Klasse (Renewal 3rd Jobs) ──
                "rune knight" => RuneKnightSkills(),
                "royal guard" => RoyalGuardSkills(),
                "sorcerer" => SorcererSkills(),
                "shadow chaser" => ShadowChaserSkills(),
                "genetic" => GeneticSkills(),
                "guillotine cross" => GuillotineCrossSkills(),
                "arch bishop" => ArchBishopSkills(),
                "minstrel" => MinstrelSkills(),
                "wanderer" => WandererSkills(),
                "paladin" => PaladinSkills(),
                "super novice" => SuperNoviceSkills(),
                "taekwon boy" or "taekwon girl" => TaekwonSkills(),
                "star gladiator" => StarGladiatorSkills(),
                "soul linker" => SoulLinkerSkills(),
                "gangster" => GangsterSkills(),
                "kagerou" or "oboro" or "ninja" => NinjaSkills(),
                "rebellion" => RebellionSkills(),
                "bard" => BardSkills(),
                "dancer" => DancerSkills(),
                // ── Baby-Klassen ──
                "baby novice" => BabyNoviceSkills(),
                "baby swordman" => BabySwordmanSkills(),
                "baby mage" => BabyMageSkills(),
                "baby archer" => BabyArcherSkills(),
                "baby acolyte" => BabyAcolyteSkills(),
                "baby merchant" => BabyMerchantSkills(),
                "baby thief" => BabyThiefSkills(),
                // ── Baby Transcendent ──
                "baby knight" => BabyKnightSkills(),
                "baby crusader" => BabyCrusaderSkills(),
                "baby wizard" => BabyWizardSkills(),
                "baby high wizard" => BabyHighWizardSkills(),
                "baby hunter" => BabyHunterSkills(),
                "baby sniper" => BabySniperSkills(),
                "baby priest" => BabyPriestSkills(),
                "baby high priest" => BabyHighPriestSkills(),
                "baby blacksmith" => BabyBlacksmithSkills(),
                "baby creator" => BabyCreatorSkills(),
                "baby assassin" => BabyAssassinSkills(),
                "baby assassin cross" => BabyAssassinCrossSkills(),
                // ── 3. Klasse (weitere) ──
                "gunslinger" => GunslingerSkills(),
                _ => NoviceSkills(),
            };

            foreach (var (id, name, maxLevel, minBase, minJob) in skills)
            {
                sim.AddSkill(id, name, maxLevel, minBase, minJob);
            }
        }

        /// <summary>Gibt alle unterstützten Jobklassen zurück.</summary>
        public static IReadOnlyList<string> GetSupportedClasses() => new List<string>
        {
            // 1. Klasse
            "Novice", "Swordman", "Mage", "Archer", "Acolyte", "Merchant", "Thief",
            // 2. Klasse
            "Knight", "Crusader", "Wizard", "Hunter", "Priest", "Blacksmith", "Assassin",
            // Transcendent
            "Lord Knight", "High Wizard", "Sniper", "High Priest", "Professor", "Stalker",
            // 3. Klasse
            "Rune Knight", "Royal Guard", "Sorcerer", "Shadow Chaser", "Genetic", "Guillotine Cross", "Arch Bishop", "Minstrel", "Wanderer", "Paladin", "Gunslinger",
            // Erweiterte / Sonderklassen
            "Super Novice", "Taekwon Boy", "Taekwon Girl", "Star Gladiator", "Soul Linker", "Gangster", "Kagerou", "Oboro", "Rebellion", "Bard", "Dancer",
            // Baby-Klassen
            "Baby Novice", "Baby Swordman", "Baby Mage", "Baby Archer", "Baby Acolyte", "Baby Merchant", "Baby Thief",
            // Baby Transcendent
            "Baby Knight", "Baby Crusader", "Baby Wizard", "Baby High Wizard", "Baby Hunter", "Baby Sniper", "Baby Priest", "Baby High Priest", "Baby Blacksmith", "Baby Creator", "Baby Assassin", "Baby Assassin Cross",
            // 3. Klasse (weitere)
            "Gunslinger",
        }.AsReadOnly();

        #region 1. Klasse (Base Jobs)

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] NoviceSkills() =>
            new[]
            {
                (901, "Punch", 1, 1, 1),
                (902, "Stones", 1, 1, 1),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] SwordmanSkills() =>
            new[]
            {
                (1, "Sword Mastery", 10, 1, 1),
                (2, "Spear Mastery", 10, 1, 1),
                (3, "Two-Handed Sword Mastery", 10, 1, 1),
                (4, "Two-Handed Spear Mastery", 10, 1, 1),
                (5, "Pole Arm Mastery", 10, 1, 1),
                (6, "Bash", 10, 1, 1),
                (7, "Magnum Break", 10, 1, 1),
                (8, "Provoke", 10, 1, 1),
                (9, "Weapon Research", 10, 1, 1),
                (10, "First Aid", 5, 1, 1),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] MageSkills() =>
            new[]
            {
                (101, "Fire Bolt", 10, 1, 1),
                (102, "Fire Ball", 10, 1, 1),
                (103, "Fire Wall", 10, 1, 1),
                (104, "Meteor Storm", 10, 1, 1),
                (105, "Lightning Bolt", 10, 1, 1),
                (106, "Thunder Storm", 10, 1, 1),
                (107, "Frost Diver", 10, 1, 1),
                (108, "Ice Wall", 10, 1, 1),
                (109, "Energy Coat", 1, 1, 1),
                (110, "Increase SP Recovery", 1, 1, 1),
                (111, "Heal", 10, 1, 1),
                (112, "Blessing", 10, 1, 1),
                (113, "Increase AGI", 10, 1, 1),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] ArcherSkills() =>
            new[]
            {
                (201, "Double Strafe", 10, 1, 1),
                (202, "Arrow Storm", 10, 1, 1),
                (203, "Blaze", 10, 1, 1),
                (204, "Concentration", 1, 1, 1),
                (205, "Critical Explosion", 1, 1, 1),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] AcolyteSkills() =>
            new[]
            {
                (301, "Heal", 10, 1, 1),
                (302, "Blessing", 10, 1, 1),
                (303, "Increase AGI", 10, 1, 1),
                (304, "Resurrection", 10, 1, 1),
                (305, "Glory", 1, 1, 1),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] MerchantSkills() =>
            new[]
            {
                (401, "Discount", 10, 1, 1),
                (402, "Overcharge", 10, 1, 1),
                (403, "Mammonite", 10, 1, 1),
                (404, "Coin", 10, 1, 1),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] ThiefSkills() =>
            new[]
            {
                (501, "Steal", 10, 1, 1),
                (502, "Hide", 1, 1, 1),
                (503, "Double Attack", 10, 1, 1),
                (504, "Critical Strike", 1, 1, 1),
            };

        #endregion

        #region 2. Klasse (Pre-Renewal 2nd Jobs)

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] KnightSkills() =>
            new[]
            {
                (601, "Spear Mastery", 10, 50, 50),
                (602, "Two-Handed Spear Mastery", 10, 50, 50),
                (603, "Pole Arm Mastery", 10, 50, 50),
                (604, "Spear Boom", 5, 50, 50),
                (605, "Skid Trap", 5, 50, 50),
                (606, "Change Material", 5, 50, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] CrusaderSkills() =>
            new[]
            {
                (611, "Spear Mastery", 10, 50, 50),
                (612, "Two-Handed Spear Mastery", 10, 50, 50),
                (613, "Pole Arm Mastery", 10, 50, 50),
                (614, "Spear Boom", 5, 50, 50),
                (615, "Skid Trap", 5, 50, 50),
                (616, "Change Material", 5, 50, 50),
                (617, "Spirit of Sword", 10, 50, 50),
                (618, "Provoke", 10, 50, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] WizardSkills() =>
            new[]
            {
                (701, "Fire Ball", 10, 50, 50),
                (702, "Fire Wall", 10, 50, 50),
                (703, "Meteor Storm", 10, 50, 50),
                (704, "Lightning Bolt", 10, 50, 50),
                (705, "Thunder Storm", 10, 50, 50),
                (706, "Frost Diver", 10, 50, 50),
                (707, "Ice Wall", 10, 50, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] HunterSkills() =>
            new[]
            {
                (801, "Double Strafe", 10, 50, 50),
                (802, "Arrow Storm", 10, 50, 50),
                (803, "Blaze", 10, 50, 50),
                (804, "Concentration", 10, 50, 50),
                (805, "Critical Explosion", 5, 50, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] PriestSkills() =>
            new[]
            {
                (901, "Heal", 10, 50, 50),
                (902, "Blessing", 10, 50, 50),
                (903, "Increase AGI", 10, 50, 50),
                (904, "Resurrection", 10, 50, 50),
                (905, "Glory", 1, 50, 50),
                (906, "Kyrie Eleison", 10, 50, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BlacksmithSkills() =>
            new[]
            {
                (1001, "Mammonite", 10, 50, 50),
                (1002, "Overcharge", 10, 50, 50),
                (1003, "Discount", 10, 50, 50),
                (1004, "Coin", 10, 50, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] AssassinSkills() =>
            new[]
            {
                (1101, "Steal", 10, 50, 50),
                (1102, "Hide", 1, 50, 50),
                (1103, "Double Attack", 10, 50, 50),
                (1104, "Critical Strike", 10, 50, 50),
            };

        #endregion

        #region Transcendent 2nd Jobs

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] LordKnightSkills() =>
            new[]
            {
                (1201, "Sword Mastery", 10, 70, 70),
                (1202, "Spear Mastery", 10, 70, 70),
                (1203, "Two-Handed Sword Mastery", 10, 70, 70),
                (1204, "Bowling Bash", 10, 70, 70),
                (1205, "Brandish Spear", 10, 70, 70),
                (1206, "Spiral Pierce", 10, 70, 70),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] HighWizardSkills() =>
            new[]
            {
                (1301, "Fire Ball", 10, 70, 70),
                (1302, "Fire Wall", 10, 70, 70),
                (1303, "Meteor Storm", 10, 70, 70),
                (1304, "Storm Gust", 10, 70, 70),
                (1305, "Lord of Vermilion", 10, 70, 70),
                (1306, "Varetyr Spear", 10, 70, 70),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] SniperSkills() =>
            new[]
            {
                (1401, "Double Strafe", 10, 70, 70),
                (1402, "Arrow Storm", 10, 70, 70),
                (1403, "Blaze", 10, 70, 70),
                (1404, "Sharpshooting", 10, 70, 70),
                (1405, "Fletcherize", 1, 70, 70),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] HighPriestSkills() =>
            new[]
            {
                (1501, "Heal", 10, 70, 70),
                (1502, "Blessing", 10, 70, 70),
                (1503, "Increase AGI", 10, 70, 70),
                (1504, "Resurrection", 10, 70, 70),
                (1505, "Magnificat", 10, 70, 70),
                (1506, "Turn Undead", 10, 70, 70),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] ProfessorSkills() =>
            new[]
            {
                (1601, "Fire Ball", 10, 70, 70),
                (1602, "Fire Wall", 10, 70, 70),
                (1603, "Meteor Storm", 10, 70, 70),
                (1604, "Storm Gust", 10, 70, 70),
                (1605, "Lord of Vermilion", 10, 70, 70),
                (1606, "Varetyr Spear", 10, 70, 70),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] StalkerSkills() =>
            new[]
            {
                (1701, "Steal", 10, 70, 70),
                (1702, "Hide", 1, 70, 70),
                (1703, "Double Attack", 10, 70, 70),
                (1704, "Critical Strike", 10, 70, 70),
                (1705, "Enchant Poison", 10, 70, 70),
            };

        #endregion

        #region 3. Klasse (Renewal 3rd Jobs)

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] RuneKnightSkills() =>
            new[]
            {
                (1801, "Dragon Breath", 10, 99, 50),
                (1802, "Runic Strike", 10, 99, 50),
                (1803, "Dragon Training", 5, 99, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] RoyalGuardSkills() =>
            new[]
            {
                (1901, "Spear Mastery", 10, 99, 50),
                (1902, "Brandish Spear", 10, 99, 50),
                (1903, "Spiral Pierce", 10, 99, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] SorcererSkills() =>
            new[]
            {
                (2001, "Fire Ball", 10, 99, 50),
                (2002, "Fire Wall", 10, 99, 50),
                (2003, "Meteor Storm", 10, 99, 50),
                (2004, "Storm Gust", 10, 99, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] ShadowChaserSkills() =>
            new[]
            {
                (2101, "Steal", 10, 99, 50),
                (2102, "Hide", 1, 99, 50),
                (2103, "Double Attack", 10, 99, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] GeneticSkills() =>
            new[]
            {
                (2201, "Mammonite", 10, 99, 50),
                (2202, "Overcharge", 10, 99, 50),
                (2203, "Discount", 10, 99, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] GuillotineCrossSkills() =>
            new[]
            {
                (2301, "Steal", 10, 99, 50),
                (2302, "Hide", 1, 99, 50),
                (2303, "Double Attack", 10, 99, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] ArchBishopSkills() =>
            new[]
            {
                (2401, "Heal", 10, 99, 50),
                (2402, "Blessing", 10, 99, 50),
                (2403, "Increase AGI", 10, 99, 50),
                (2404, "Resurrection", 10, 99, 50),
                (2405, "Turn Undead", 10, 99, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] MinstrelSkills() =>
            new[]
            {
                (2501, "Swing Dance", 5, 99, 50),
                (2502, "Saturn Frosty Day", 5, 99, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] WandererSkills() =>
            new[]
            {
                (2601, "Swing Dance", 5, 99, 50),
                (2602, "Saturn Frosty Day", 5, 99, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] PaladinSkills() =>
            new[]
            {
                (2701, "Sword Mastery", 10, 99, 50),
                (2702, "Spear Mastery", 10, 99, 50),
                (2703, "Two-Handed Sword Mastery", 10, 99, 50),
            };

        #endregion

        #region Erweiterte / Sonderklassen

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] SuperNoviceSkills() =>
            new[]
            {
                (2801, "Punch", 1, 1, 1),
                (2802, "Stones", 1, 1, 1),
                (2803, "Berserk", 10, 1, 1),
                (2804, "Providence", 10, 1, 1),
                (2805, "Impositio Manus", 10, 1, 1),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] TaekwonSkills() =>
            new[]
            {
                (2901, "TaeKwon", 10, 70, 50),
                (2902, "Bash", 10, 70, 50),
                (2903, "Counter Attack", 10, 70, 50),
                (2904, "Rage Strike", 10, 70, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] StarGladiatorSkills() =>
            new[]
            {
                (3001, "Shining Star", 10, 99, 50),
                (3002, "Meteor Assault", 10, 99, 50),
                (3003, "Cosmic Ray", 10, 99, 50),
                (3004, "Soul Expansion", 5, 99, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] SoulLinkerSkills() =>
            new[]
            {
                (3101, "Berserk", 10, 99, 50),
                (3102, "Providence", 10, 99, 50),
                (3103, "Impositio Manus", 10, 99, 50),
                (3104, "Heal", 10, 99, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] GangsterSkills() =>
            new[]
            {
                (3201, "Steal", 10, 1, 1),
                (3202, "Hide", 1, 1, 1),
                (3203, "Backstab", 10, 1, 1),
                (3204, "Double Attack", 10, 1, 1),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] NinjaSkills() =>
            new[]
            {
                (3301, "Throw Kunai", 10, 99, 50),
                (3302, "Kunai Storm", 10, 99, 50),
                (3303, "Hide", 1, 99, 50),
                (3304, "Double Attack", 10, 99, 50),
                (3305, "Enchant Poison", 10, 99, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] RebellionSkills() =>
            new[]
            {
                (3401, "Gunslinger", 10, 99, 50),
                (3402, "Desperado", 10, 99, 50),
                (3403, "Full Buster", 10, 99, 50),
                (3404, "Snowflake Throw", 10, 99, 50),
            };

        #endregion

        #region Baby-Klassen

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyNoviceSkills() =>
            new[]
            {
                (3501, "Punch", 1, 1, 1),
                (3502, "Stones", 1, 1, 1),
                (3503, "Provoke", 5, 1, 1),
                (3504, "First Aid", 5, 1, 1),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabySwordmanSkills() =>
            new[]
            {
                (3601, "Sword Mastery", 10, 1, 1),
                (3602, "Spear Mastery", 10, 1, 1),
                (3603, "Bash", 10, 1, 1),
                (3604, "Magnum Break", 10, 1, 1),
                (3605, "Provoke", 10, 1, 1),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyMageSkills() =>
            new[]
            {
                (3701, "Fire Bolt", 10, 1, 1),
                (3702, "Fire Ball", 10, 1, 1),
                (3703, "Lightning Bolt", 10, 1, 1),
                (3704, "Frost Diver", 10, 1, 1),
                (3705, "Heal", 10, 1, 1),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyArcherSkills() =>
            new[]
            {
                (3801, "Double Strafe", 10, 1, 1),
                (3802, "Arrow Storm", 10, 1, 1),
                (3803, "Concentration", 5, 1, 1),
                (3804, "Critical Explosion", 5, 1, 1),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyAcolyteSkills() =>
            new[]
            {
                (3901, "Heal", 10, 1, 1),
                (3902, "Blessing", 10, 1, 1),
                (3903, "Increase AGI", 10, 1, 1),
                (3904, "Resurrection", 10, 1, 1),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyMerchantSkills() =>
            new[]
            {
                (4001, "Discount", 10, 1, 1),
                (4002, "Overcharge", 10, 1, 1),
                (4003, "Mammonite", 10, 1, 1),
                (4004, "Coin", 10, 1, 1),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyThiefSkills() =>
            new[]
            {
                (4101, "Steal", 10, 1, 1),
                (4102, "Hide", 1, 1, 1),
                (4103, "Double Attack", 10, 1, 1),
                (4104, "Critical Strike", 10, 1, 1),
            };

        #endregion

        #region Baby Transcendent

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyKnightSkills() =>
            new[]
            {
                (4201, "Sword Mastery", 10, 70, 70),
                (4202, "Spear Mastery", 10, 70, 70),
                (4203, "Bash", 10, 70, 70),
                (4204, "Magnum Break", 10, 70, 70),
                (4205, "Provoke", 10, 70, 70),
                (4206, "Brandish Spear", 10, 70, 70),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyCrusaderSkills() =>
            new[]
            {
                (4211, "Spear Mastery", 10, 70, 70),
                (4212, "Two-Handed Spear Mastery", 10, 70, 70),
                (4213, "Pole Arm Mastery", 10, 70, 70),
                (4214, "Spear Boom", 5, 70, 70),
                (4215, "Skid Trap", 5, 70, 70),
                (4216, "Change Material", 5, 70, 70),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyWizardSkills() =>
            new[]
            {
                (4301, "Fire Ball", 10, 70, 70),
                (4302, "Fire Wall", 10, 70, 70),
                (4303, "Meteor Storm", 10, 70, 70),
                (4304, "Lightning Bolt", 10, 70, 70),
                (4305, "Thunder Storm", 10, 70, 70),
                (4306, "Frost Diver", 10, 70, 70),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyHighWizardSkills() =>
            new[]
            {
                (4321, "Fire Ball", 10, 70, 70),
                (4322, "Fire Wall", 10, 70, 70),
                (4323, "Meteor Storm", 10, 70, 70),
                (4324, "Storm Gust", 10, 70, 70),
                (4325, "Lord of Vermilion", 10, 70, 70),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyHunterSkills() =>
            new[]
            {
                (4401, "Double Strafe", 10, 70, 70),
                (4402, "Arrow Storm", 10, 70, 70),
                (4403, "Blaze", 10, 70, 70),
                (4404, "Concentration", 10, 70, 70),
                (4405, "Critical Explosion", 5, 70, 70),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabySniperSkills() =>
            new[]
            {
                (4431, "Double Strafe", 10, 70, 70),
                (4432, "Arrow Storm", 10, 70, 70),
                (4433, "Blaze", 10, 70, 70),
                (4434, "Sharpshooting", 10, 70, 70),
                (4435, "Fletcherize", 1, 70, 70),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyPriestSkills() =>
            new[]
            {
                (4501, "Heal", 10, 70, 70),
                (4502, "Blessing", 10, 70, 70),
                (4503, "Increase AGI", 10, 70, 70),
                (4504, "Resurrection", 10, 70, 70),
                (4505, "Glory", 1, 70, 70),
                (4506, "Kyrie Eleison", 10, 70, 70),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyHighPriestSkills() =>
            new[]
            {
                (4531, "Heal", 10, 70, 70),
                (4532, "Blessing", 10, 70, 70),
                (4533, "Increase AGI", 10, 70, 70),
                (4534, "Resurrection", 10, 70, 70),
                (4535, "Magnificat", 10, 70, 70),
                (4536, "Turn Undead", 10, 70, 70),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyBlacksmithSkills() =>
            new[]
            {
                (4601, "Mammonite", 10, 70, 70),
                (4602, "Overcharge", 10, 70, 70),
                (4603, "Discount", 10, 70, 70),
                (4604, "Coin", 10, 70, 70),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyCreatorSkills() =>
            new[]
            {
                (4631, "Mammonite", 10, 70, 70),
                (4632, "Overcharge", 10, 70, 70),
                (4633, "Discount", 10, 70, 70),
                (4634, "Coin", 10, 70, 70),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyAssassinSkills() =>
            new[]
            {
                (4701, "Steal", 10, 70, 70),
                (4702, "Hide", 1, 70, 70),
                (4703, "Double Attack", 10, 70, 70),
                (4704, "Critical Strike", 10, 70, 70),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BabyAssassinCrossSkills() =>
            new[]
            {
                (4731, "Steal", 10, 70, 70),
                (4732, "Hide", 1, 70, 70),
                (4733, "Double Attack", 10, 70, 70),
                (4734, "Critical Strike", 10, 70, 70),
                (4735, "Enchant Poison", 10, 70, 70),
            };

        #endregion

        #region 3. Klasse (weitere)

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] GunslingerSkills() =>
            new[]
            {
                (4801, "Desperado", 10, 99, 50),
                (4802, "Full Buster", 10, 99, 50),
                (4803, "Snowflake Throw", 10, 99, 50),
                (4804, "Gunslinger", 10, 99, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] BardSkills() =>
            new[]
            {
                (4901, "Swing Dance", 5, 99, 50),
                (4902, "Saturn Frosty Day", 5, 99, 50),
                (4903, "Winter Rose of Wind", 5, 99, 50),
            };

        private static (int Id, string Name, int MaxLevel, int MinBase, int MinJob)[] DancerSkills() =>
            new[]
            {
                (5001, "Swing Dance", 5, 99, 50),
                (5002, "Saturn Frosty Day", 5, 99, 50),
                (5003, "Winter Rose of Wind", 5, 99, 50),
            };

        #endregion

        private static int CalculateTotalPoints(int baseLevel)
        {
            // RO: 1 Skill-Punkt pro Base-Level ab Level 1
            return Math.Max(0, baseLevel - 1);
        }
    }

    public class SimSkill
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string SkillType { get; set; } = "Active"; // Active, Passive, Buff, Debuff
        public string IconPath { get; set; } = "";
        public int MaxLevel { get; set; } = 10;
        public int MinBaseLevel { get; set; } = 1;
        public int MinJobLevel { get; set; } = 1;
        public int Level { get; set; } = 0;
        // Prerequisite skills: skill ID → required level
        public Dictionary<int, int> PrerequisiteSkills { get; set; } = new();
    }
}
