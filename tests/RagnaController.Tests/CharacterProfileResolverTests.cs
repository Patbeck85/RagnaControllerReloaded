using System;
using System.IO;
using RagnaController.Profiles;
using Xunit;

namespace RagnaController.Tests
{
    /// <summary>
    /// FEAT-024: Unit-Tests für den reinen CharacterProfileResolver (Name→Profil-Zuordnung).
    /// </summary>
    public class CharacterProfileResolverTests
    {
        private readonly CharacterProfileResolver _resolver = new();

        [Fact]
        public void Normalize_TrimmsAußenleerzeichen()
            => Assert.Equal("Ragnarok", CharacterProfileResolver.Normalize("  Ragnarok  "));

        [Fact]
        public void Normalize_FasstInterneLeerzeichenLäufeZusammen()
            => Assert.Equal("a b c", CharacterProfileResolver.Normalize("a   b\t\tc"));

        [Fact]
        public void Normalize_NurLeerräumeGibtLeereZeichenkette()
            => Assert.Equal("", CharacterProfileResolver.Normalize("   \t  "));

        [Fact]
        public void Normalize_NullGibtLeereZeichenkette()
            => Assert.Equal("", CharacterProfileResolver.Normalize(null));

        [Fact]
        public void Resolve_RegistriertUndFindetCaseInsensitive()
        {
            _resolver.RegisterMapping("Ragnarok", "Novice");
            Assert.Equal("Novice", _resolver.Resolve("ragnarok"));
            Assert.Equal("Novice", _resolver.Resolve("  RAGNAROK  "));
        }

        [Fact]
        public void Resolve_UnbekannterNameGibtNull()
            => Assert.Null(_resolver.Resolve("KeinChar"));

        [Fact]
        public void Resolve_LeererNameGibtNull()
        {
            _resolver.RegisterMapping("Ragnarok", "Novice");
            Assert.Null(_resolver.Resolve("   "));
            Assert.Null(_resolver.Resolve(null));
        }

        [Fact]
        public void RegisterMapping_LeereEingabeWirdIgnoriert()
        {
            _resolver.RegisterMapping("", "Novice");
            _resolver.RegisterMapping("   ", "Novice");
            _resolver.RegisterMapping("Ragnarok", "");
            Assert.Equal(0, _resolver.Count);
        }

        [Fact]
        public void Unregister_EntferntZuordnung()
        {
            _resolver.RegisterMapping("Ragnarok", "Novice");
            Assert.True(_resolver.Unregister("ragnarok"));
            Assert.False(_resolver.IsMapped("Ragnarok"));
            Assert.Null(_resolver.Resolve("Ragnarok"));
        }

        [Fact]
        public void Unregister_UnbekanntGibtFalse()
            => Assert.False(_resolver.Unregister("NiemalsRegistriert"));

        [Fact]
        public void SnapshotUndLoadFrom_Roundtrip()
        {
            _resolver.RegisterMapping("CharA", "Novice");
            _resolver.RegisterMapping("CharB", "Warrior");
            var snap = _resolver.Snapshot();

            var fresh = new CharacterProfileResolver();
            fresh.LoadFrom(snap);
            Assert.Equal(2, fresh.Count);
            Assert.Equal("Novice", fresh.Resolve("chara"));
            Assert.Equal("Warrior", fresh.Resolve("CHARB"));
        }

        [Fact]
        public void IsMapped_WechseltMitZuordnung()
        {
            Assert.False(_resolver.IsMapped("X"));
            _resolver.RegisterMapping("X", "Novice");
            Assert.True(_resolver.IsMapped("x"));
        }
    }

    /// <summary>
    /// FEAT-024: Integrationstests — Char→Profil-Zuordnung mit Persistenz über Neustart
    /// und automatischem Profil-Switch bei Charakter-Erkennung.
    /// </summary>
    public class ProfileManagerCharacterMappingTests : IDisposable
    {
        private readonly string _dir;

        public ProfileManagerCharacterMappingTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "ragna-feat024-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); } catch { /* best effort */ }
        }

        private static Profile NewProfile(string name) => new()
        {
            Name = name,
            Class = "Melee",
            IsEnabled = true,
            IsBuiltIn = false
        };

        [Fact]
        public void RegisterPersistiertUndNeueInstanzLädtNachNeustart()
        {
            var pm1 = new ProfileManager(_dir);
            pm1.AddAndSave(NewProfile("Warrior"));
            pm1.RegisterCharacterMapping("  Ragnarok  ", "Warrior");

            // Simulierter Neustart: neue Instanz, gleicher Ordner.
            var pm2 = new ProfileManager(_dir);
            Assert.Equal("Warrior", pm2.GetProfileForCharacter("ragnarok"));
        }

        [Fact]
        public void OnCharacterDetected_SchaltetProfilAutomatisch()
        {
            var pm = new ProfileManager(_dir);
            pm.AddAndSave(NewProfile("Novice"));
            pm.AddAndSave(NewProfile("Warrior"));
            pm.SetActive("Novice");
            pm.RegisterCharacterMapping("Ragnarok", "Warrior");

            bool switched = pm.OnCharacterDetected("ragnarok");

            Assert.True(switched);
            Assert.Equal("Warrior", pm.ActiveProfileName);
        }

        [Fact]
        public void OnCharacterDetected_UnbekanntSchaltetNicht()
        {
            var pm = new ProfileManager(_dir);
            pm.AddAndSave(NewProfile("Novice"));
            pm.SetActive("Novice");

            Assert.False(pm.OnCharacterDetected("KeinChar"));
            Assert.Equal("Novice", pm.ActiveProfileName);
        }

        [Fact]
        public void OnCharacterDetected_ProfilExistiertNichtSchaltetNicht()
        {
            var pm = new ProfileManager(_dir);
            pm.AddAndSave(NewProfile("Novice"));
            pm.SetActive("Novice");
            // Mapping auf ein Profil, das nicht in der Liste ist.
            pm.RegisterCharacterMapping("Ragnarok", "GhostProfile");

            Assert.False(pm.OnCharacterDetected("Ragnarok"));
            Assert.Equal("Novice", pm.ActiveProfileName);
        }

        [Fact]
        public void UnregisterPersistiertEntfernung()
        {
            var pm1 = new ProfileManager(_dir);
            pm1.AddAndSave(NewProfile("Warrior"));
            pm1.RegisterCharacterMapping("Ragnarok", "Warrior");
            pm1.UnregisterCharacterMapping("ragnarok");

            var pm2 = new ProfileManager(_dir);
            Assert.Null(pm2.GetProfileForCharacter("Ragnarok"));
        }

        [Fact]
        public void CharacterToProfileMap_SpiegeltZuordnungen()
        {
            var pm = new ProfileManager(_dir);
            pm.AddAndSave(NewProfile("Warrior"));
            pm.RegisterCharacterMapping("CharA", "Warrior");

            Assert.True(pm.CharacterToProfileMap.ContainsKey("CharA"));
        }
    }
}
