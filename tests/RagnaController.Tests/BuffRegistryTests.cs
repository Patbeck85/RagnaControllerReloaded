using System;
using System.Linq;
using Xunit;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// GEX-011: BuffRegistry validation tests.
    /// Ensures the embedded BuffRegistry.json loads correctly and contains expected data.
    /// </summary>
    public class BuffRegistryTests
    {
        [Fact]
        public void Initialize_LoadsRegistry_WithoutError()
        {
            // Act & Assert - should not throw
            BuffRegistry.Initialize();
            Assert.NotNull(BuffRegistry.AllBuffs);
            Assert.NotNull(BuffRegistry.AllDebuffs);
        }

        [Fact]
        public void Registry_ContainsExpectedBuffCount()
        {
            BuffRegistry.Initialize();

            // Mindestens 100 Buffs (wie in der JSON definiert)
            Assert.True(BuffRegistry.AllBuffs.Count >= 100,
                $"Expected >= 100 buffs, got {BuffRegistry.AllBuffs.Count}");
        }

        [Fact]
        public void Registry_ContainsExpectedDebuffCount()
        {
            BuffRegistry.Initialize();

            Assert.True(BuffRegistry.AllDebuffs.Count >= 10,
                $"Expected >= 10 debuffs, got {BuffRegistry.AllDebuffs.Count}");
        }

        [Fact]
        public void KeyBuffs_ArePresent_WithCorrectDurations()
        {
            BuffRegistry.Initialize();

            // Priest core buffs
            var blessing = BuffRegistry.GetBuffByName("Blessing");
            Assert.NotNull(blessing);
            Assert.Equal(240, blessing.DurationSec);
            Assert.Equal(10, blessing.MaxStacks);
            Assert.Equal("PR_BLESSING", blessing.SkillId);

            var agi = BuffRegistry.GetBuffByName("Increase AGI");
            Assert.NotNull(agi);
            Assert.Equal(240, agi.DurationSec);
            Assert.Equal(10, agi.MaxStacks);

            var assumptio = BuffRegistry.GetBuffByName("Assumptio");
            Assert.NotNull(assumptio);
            Assert.Equal(180, assumptio.DurationSec);
            Assert.Equal(5, assumptio.MaxStacks);

            // Knight buffs
            var quicken = BuffRegistry.GetBuffByName("Two-Hand Quicken");
            Assert.NotNull(quicken);
            Assert.Equal(180, quicken.DurationSec);
            Assert.Equal(1, quicken.MaxStacks);

            // Soul Linker
            var kaizel = BuffRegistry.GetBuffByName("Kaizel");
            Assert.NotNull(kaizel);
            Assert.Equal(300, kaizel.DurationSec);
            Assert.Equal("SL_KAIZEL", kaizel.SkillId);
        }

        [Fact]
        public void Debuffs_ArePresent_WithCorrectDurations()
        {
            BuffRegistry.Initialize();

            var poison = BuffRegistry.GetDebuffByName("Poison");
            Assert.NotNull(poison);
            Assert.Equal(60, poison.DurationSec);

            var stun = BuffRegistry.GetDebuffByName("Stun");
            Assert.NotNull(stun);
            Assert.Equal(10, stun.DurationSec);

            var freeze = BuffRegistry.GetDebuffByName("Freeze");
            Assert.NotNull(freeze);
            Assert.Equal(10, freeze.DurationSec);
        }

        [Fact]
        public void GetDefaultDuration_ReturnsRegistryValue_OrFallback()
        {
            BuffRegistry.Initialize();

            // Known buff
            Assert.Equal(240, BuffRegistry.GetDefaultDuration("Blessing"));

            // Unknown buff -> fallback 60s
            Assert.Equal(60, BuffRegistry.GetDefaultDuration("TotallyUnknownBuff123"));
        }

        [Fact]
        public void GetDefaultWarningSec_ReturnsRegistryValue_OrFallback()
        {
            BuffRegistry.Initialize();

            // Known buff (default warning is 10 in registry)
            Assert.Equal(10, BuffRegistry.GetDefaultWarningSec("Blessing"));

            // Unknown buff -> fallback 10s
            Assert.Equal(10, BuffRegistry.GetDefaultWarningSec("TotallyUnknownBuff123"));
        }

        [Fact]
        public void IsValidBuff_Works()
        {
            BuffRegistry.Initialize();

            Assert.True(BuffRegistry.IsValidBuff("Blessing"));
            Assert.True(BuffRegistry.IsValidBuff("Two-Hand Quicken"));
            Assert.False(BuffRegistry.IsValidBuff("NotARealBuff123"));
        }

        [Fact]
        public void IsValidDebuff_Works()
        {
            BuffRegistry.Initialize();

            Assert.True(BuffRegistry.IsValidDebuff("Poison"));
            Assert.True(BuffRegistry.IsValidDebuff("Stun"));
            Assert.False(BuffRegistry.IsValidDebuff("NotARealDebuff123"));
        }

        [Fact]
        public void SkillId_Lookup_Works()
        {
            BuffRegistry.Initialize();

            var bySkill = BuffRegistry.GetBuffBySkillId("PR_BLESSING");
            Assert.NotNull(bySkill);
            Assert.Equal("Blessing", bySkill.Name);

            var bySkill2 = BuffRegistry.GetBuffBySkillId("KN_2HQUICKEN");
            Assert.NotNull(bySkill2);
            Assert.Equal("Two-Hand Quicken", bySkill2.Name);
        }

        [Fact]
        public void NoDuplicateBuffIds()
        {
            BuffRegistry.Initialize();

            var ids = BuffRegistry.AllBuffs.Select(b => b.Id).ToList();
            var uniqueIds = ids.Distinct().ToList();

            Assert.Equal(ids.Count, uniqueIds.Count);
        }

        [Fact]
        public void NoDuplicateDebuffIds()
        {
            BuffRegistry.Initialize();

            var ids = BuffRegistry.AllDebuffs.Select(d => d.Id).ToList();
            var uniqueIds = ids.Distinct().ToList();

            Assert.Equal(ids.Count, uniqueIds.Count);
        }

        [Fact]
        public void BuffEntry_HasAllRequiredFields()
        {
            BuffRegistry.Initialize();

            foreach (var buff in BuffRegistry.AllBuffs)
            {
                Assert.True(buff.Id > 0, $"Buff '{buff.Name}' has invalid Id");
                Assert.False(string.IsNullOrEmpty(buff.Name), $"Buff Id {buff.Id} has empty Name");
                Assert.True(buff.DurationSec >= 0, $"Buff '{buff.Name}' has negative DurationSec");
                Assert.True(buff.MaxStacks >= 1, $"Buff '{buff.Name}' has MaxStacks < 1");
                Assert.False(string.IsNullOrEmpty(buff.Class), $"Buff '{buff.Name}' has empty Class");
                Assert.False(string.IsNullOrEmpty(buff.SkillId), $"Buff '{buff.Name}' has empty SkillId");
            }
        }
    }
}