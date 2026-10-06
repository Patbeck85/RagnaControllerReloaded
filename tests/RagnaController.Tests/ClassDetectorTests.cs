using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Tests
{
    /// <summary>
    /// TECH-030: Regression tests for ClassDetector static state.
    ///
    /// The original keyed collection initializer ([VirtualKey.F1] = new(){...}) silently
    /// overwrote earlier entries when a key appeared twice (F1, F2, D4, D6, D9, D0, E, R,
    /// I, Y, F4, F8 ...), so 51 class entries were lost at type-init time. The map is now
    /// built via BuildSkillToClassMap() with AddEntry accumulation — duplicate keys must
    /// accumulate into one list instead of overwriting each other.
    /// </summary>
    public class ClassDetectorTests
    {
        private static readonly FieldInfo MapField = typeof(ClassDetector)
            .GetField("SkillToClassMap", BindingFlags.NonPublic | BindingFlags.Static)!;

        private static Dictionary<VirtualKey, List<(string Class, int Weight, string Category)>> GetMap()
            => (Dictionary<VirtualKey, List<(string Class, int Weight, string Category)>>)MapField.GetValue(null)!;

        [Fact]
        public void SkillToClassMap_DuplicateKeys_AccumulateInsteadOfOverwriting()
        {
            var map = GetMap();

            // F1 was declared twice (Swordsman/Knight/Crusader + High Wizard).
            // Before the fix only "High Wizard" survived — now all 4 must be present.
            Assert.True(map.TryGetValue(VirtualKey.F1, out var f1));
            Assert.Contains("Swordsman", f1.Select(e => e.Class));
            Assert.Contains("Knight", f1.Select(e => e.Class));
            Assert.Contains("Crusader", f1.Select(e => e.Class));
            Assert.Contains("High Wizard", f1.Select(e => e.Class));

            // F2 was declared twice (Swordsman/Knight/Crusader + Professor).
            Assert.True(map.TryGetValue(VirtualKey.F2, out var f2));
            Assert.Contains("Professor", f2.Select(e => e.Class));
            Assert.Contains("Swordsman", f2.Select(e => e.Class));
        }

        [Fact]
        public void SkillToClassMap_ContainsAllEntries_NoSilentDataLoss()
        {
            var map = GetMap();

            // 62 initializer entries carried 113 class tuples; every one must survive.
            int totalEntries = map.Values.Sum(v => v.Count);
            Assert.Equal(113, totalEntries);
        }

        [Fact]
        public void SkillToClassMap_WeightsAndCategories_Preserved()
        {
            var map = GetMap();

            // Spot-check weights/categories from the original initializer.
            Assert.Contains(("High Wizard", 3, "signature"), map[VirtualKey.F1]);
            Assert.Contains(("Professor", 3, "signature"), map[VirtualKey.F2]);
            Assert.Contains(("Blacksmith", 3, "signature"), map[VirtualKey.D0]);
        }

        [Fact]
        public void DetectClass_ProfileWithF1Only_StillReturnsHighestWeightClass()
        {
            var profile = new Profile
            {
                Class = "Unknown",
                ButtonMappings = new Dictionary<ButtonKey, ButtonAction>
                {
                    [ButtonKey.FromVirtualKey(VirtualKey.F1)] = new ButtonAction
                    {
                        Type = ActionType.Key,
                        Key = VirtualKey.F1,
                        Label = "High Wizard"
                    }
                }
            };

            // F1 scores: High Wizard 3 > Swordsman 2 / Knight 2 / Crusader 1.
            string detected = ClassDetector.DetectClass(profile);
            Assert.Equal("High Wizard", detected);
        }

        [Fact]
        public void DetectClass_SwordsmanProfile_F1F2NowScoresSwordsman()
        {
            var profile = new Profile
            {
                Class = "Unknown",
                ButtonMappings = new Dictionary<ButtonKey, ButtonAction>
                {
                    [ButtonKey.FromVirtualKey(VirtualKey.F1)] = new ButtonAction
                    {
                        Type = ActionType.Key,
                        Key = VirtualKey.F1,
                        Label = "Swordsman"
                    },
                    [ButtonKey.FromVirtualKey(VirtualKey.F2)] = new ButtonAction
                    {
                        Type = ActionType.Key,
                        Key = VirtualKey.F2,
                        Label = "Swordsman 2"
                    }
                }
            };

            // With accumulation: Swordsman 2+2=4 beats High Wizard 3 / Professor 3.
            string detected = ClassDetector.DetectClass(profile);
            Assert.Equal("Swordsman", detected);
        }

        [Fact]
        public void DetectClass_EmptyMappings_ReturnsFallback()
        {
            var profile = new Profile { Class = "Melee" };
            Assert.Equal("Melee", ClassDetector.DetectClass(profile));
        }
    }
}
