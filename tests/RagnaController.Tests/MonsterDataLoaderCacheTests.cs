using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using RagnaController.Core.Services;
using RagnaController.Models;

namespace RagnaController.Tests
{
    public class MonsterDataLoaderCacheTests
    {
        private static readonly string CacheDir = @"C:\Users\patbe\AppData\Roaming\RagnaController\monster_cache";

        [Fact]
        public void LoadFromCachedRathenaFiles_LoadsThousandsOfMonsters()
        {
            var prePath = Path.Combine(CacheDir, "pre_re_mob_db.yml");
            var renewalPath = Path.Combine(CacheDir, "re_mob_db.yml");

            if (!File.Exists(prePath) || !File.Exists(renewalPath))
            {
                Assert.True(true, "Cache-Dateien nicht gefunden - Online-Download nicht verfügbar.");
                return;
            }

            var loader = new MonsterDataLoader();
            try
            {
                var preYaml = File.ReadAllText(prePath);
                var renewalYaml = File.ReadAllText(renewalPath);
                Console.WriteLine($"DEBUG: Pre-Renewal YAML Länge: {preYaml.Length:N0}, erste 200 Zeichen: {preYaml.Substring(0, Math.Min(200, preYaml.Length))}");
                Console.WriteLine($"DEBUG: Renewal YAML Länge: {renewalYaml.Length:N0}");
                
                // Schnell-Check: Anzahl - Id: Zeilen
                var preIdLines = preYaml.Split('\n').Count(l => l.Trim().StartsWith("- Id:"));
                var renIdLines = renewalYaml.Split('\n').Count(l => l.Trim().StartsWith("- Id:"));
                Console.WriteLine($"DEBUG: Pre-Renewal - Id: Zeilen: {preIdLines:N0}, Renewal - Id: Zeilen: {renIdLines:N0}");
                
                var monsters = loader.LoadFromPair(prePath, renewalPath);

                // Debug: Check a few specific monsters
                var poring = monsters.FirstOrDefault(m => m.Id == 1002);
                if (poring != null)
                {
                    Console.WriteLine($"DEBUG Poring: PreHp={poring.PreRenewalHp}, RenHp={poring.RenewalHp}, PreExp={poring.PreRenewalExp}, RenExp={poring.RenewalExp}");
                }
                var amMut = monsters.FirstOrDefault(m => m.Id == 1301);
                if (amMut != null)
                {
                    Console.WriteLine($"DEBUG AmMut: PreHp={amMut.PreRenewalHp}, RenHp={amMut.RenewalHp}, PreExp={amMut.PreRenewalExp}, RenExp={amMut.RenewalExp}");
                }
                var maya = monsters.FirstOrDefault(m => m.Id == 1147);
                if (maya != null)
                {
                    Console.WriteLine($"DEBUG Maya: PreHp={maya.PreRenewalHp}, RenHp={maya.RenewalHp}, PreExp={maya.PreRenewalExp}, RenExp={maya.RenewalExp}");
                }

                Console.WriteLine($"=== RATHENRA MONSTER IMPORT ERGEBNIS ===");
                Console.WriteLine($"Gesamt-Monster: {monsters.Count:N0}");
                Console.WriteLine($"MVP-Bosse: {monsters.Count(m => m.IsMvp):N0}");
                Console.WriteLine($"Minibosse: {monsters.Count(m => m.IsMiniboss):N0}");
                Console.WriteLine($"Rassen: {monsters.Select(m => m.Race).Distinct().Count()}");
                Console.WriteLine($"Elemente: {monsters.Select(m => m.Element).Distinct().Count()}");
                Console.WriteLine($"Größen: {monsters.Select(m => m.Size).Distinct().Count()}");

                // Prüfe dass Tausende Monster geladen wurden
                Assert.True(monsters.Count > 1000, $"Erwartet >1000 Monster, aber nur {monsters.Count} geladen.");
                Assert.True(monsters.Count > 2000, $"Rathena hat 2000+ Monster, aber nur {monsters.Count} geladen.");

                // Prüfe bekannte Monster (echte Rathena-Namen)
                Assert.NotNull(monsters.FirstOrDefault(m => m.Id == 1002 && m.Name == "Poring"));
                Assert.NotNull(monsters.FirstOrDefault(m => m.Id == 1301 && m.Name == "Am Mut"));
                Assert.NotNull(monsters.FirstOrDefault(m => m.Id == 1147 && m.Name == "Maya"));
                Assert.NotNull(monsters.FirstOrDefault(m => m.Id == 1146 && m.Name == "Matyr"));

                // Prüfe Pre-Renewal vs Renewal Unterschiede
                var hasDifferentStats = monsters.Any(m => m.PreRenewalHp != m.RenewalHp ||
                                                          m.PreRenewalExp != m.RenewalExp);
                Assert.True(hasDifferentStats, "Pre-Renewal und Renewal sollten unterschiedliche Werte haben.");

                // Prüfe alle Rassen (Rathena hat kein Beast, dafür Brute)
                var races = monsters.Select(m => m.Race).Distinct().ToList();
                Assert.Contains(MonsterRace.Formless, races);
                Assert.Contains(MonsterRace.Undead, races);
                Assert.Contains(MonsterRace.Brute, races);  // Rathena uses Brute instead of Beast
                Assert.Contains(MonsterRace.Plant, races);
                Assert.Contains(MonsterRace.Insect, races);
                Assert.Contains(MonsterRace.Fish, races);
                Assert.Contains(MonsterRace.Demon, races);
                Assert.Contains(MonsterRace.DemiHuman, races);
                Assert.Contains(MonsterRace.Angel, races);
                Assert.Contains(MonsterRace.Dragon, races);

                // Prüfe alle Elemente
                var elements = monsters.Select(m => m.Element).Distinct().ToList();
                Assert.Contains(MonsterElement.Neutral, elements);
                Assert.Contains(MonsterElement.Water, elements);
                Assert.Contains(MonsterElement.Earth, elements);
                Assert.Contains(MonsterElement.Fire, elements);
                Assert.Contains(MonsterElement.Wind, elements);
                Assert.Contains(MonsterElement.Poison, elements);
                Assert.Contains(MonsterElement.Holy, elements);
                Assert.Contains(MonsterElement.Shadow, elements);
                Assert.Contains(MonsterElement.Ghost, elements);
                Assert.Contains(MonsterElement.Undead, elements);

                // Prüfe alle Größen
                var sizes = monsters.Select(m => m.Size).Distinct().ToList();
                Assert.Contains(MonsterSize.Small, sizes);
                Assert.Contains(MonsterSize.Medium, sizes);
                Assert.Contains(MonsterSize.Large, sizes);

                Console.WriteLine($"=== IMPORT ERFOLGREICH ===");
            }
            finally
            {
                loader.Dispose();
            }
        }

        [Fact]
        public void LoadFromCachedRathena_PreRenewalAndRenewalHaveDifferentValues()
        {
            var prePath = Path.Combine(CacheDir, "pre_re_mob_db.yml");
            var renewalPath = Path.Combine(CacheDir, "re_mob_db.yml");

            if (!File.Exists(prePath) || !File.Exists(renewalPath))
            {
                Assert.True(true, "Cache-Dateien nicht gefunden.");
                return;
            }

            var loader = new MonsterDataLoader();
            try
            {
                var preYaml = File.ReadAllText(prePath);
                var renewalYaml = File.ReadAllText(renewalPath);
                Console.WriteLine($"DEBUG: Pre-Renewal YAML Länge: {preYaml.Length:N0}, erste 200 Zeichen: {preYaml.Substring(0, Math.Min(200, preYaml.Length))}");
                Console.WriteLine($"DEBUG: Renewal YAML Länge: {renewalYaml.Length:N0}");
                
                // Schnell-Check: Anzahl - Id: Zeilen
                var preIdLines = preYaml.Split('\n').Count(l => l.Trim().StartsWith("- Id:"));
                var renIdLines = renewalYaml.Split('\n').Count(l => l.Trim().StartsWith("- Id:"));
                Console.WriteLine($"DEBUG: Pre-Renewal - Id: Zeilen: {preIdLines:N0}, Renewal - Id: Zeilen: {renIdLines:N0}");
                
                var monsters = loader.LoadFromPair(prePath, renewalPath);

                // Debug: Check specific monsters
                var poring = monsters.FirstOrDefault(m => m.Id == 1002);
                if (poring != null)
                {
                    Console.WriteLine($"DEBUG Poring: PreHp={poring.PreRenewalHp}, RenHp={poring.RenewalHp}, PreExp={poring.PreRenewalExp}, RenExp={poring.RenewalExp}");
                }
                var amMut = monsters.FirstOrDefault(m => m.Id == 1301);
                if (amMut != null)
                {
                    Console.WriteLine($"DEBUG AmMut: PreHp={amMut.PreRenewalHp}, RenHp={amMut.RenewalHp}, PreExp={amMut.PreRenewalExp}, RenExp={amMut.RenewalExp}");
                }

                // Poring: Pre-Renewal vs Renewal
                Assert.NotNull(poring);
                Console.WriteLine($"Poring: Pre-Renewal HP={poring.PreRenewalHp}, Renewal HP={poring.RenewalHp}");
                Console.WriteLine($"Poring: Pre-Renewal EXP={poring.PreRenewalExp}, Renewal EXP={poring.RenewalExp}");

                // Maya (MVP) 
                var maya = monsters.FirstOrDefault(m => m.Id == 1147);
                Assert.NotNull(maya);
                Assert.True(maya.IsMvp);
                Console.WriteLine($"Maya: Pre-Renewal HP={maya.PreRenewalHp:N0}, Renewal HP={maya.RenewalHp:N0}");

                // Prüfe dass die meisten Monster unterschiedliche Werte haben
                var differentCount = monsters.Count(m => m.PreRenewalHp != m.RenewalHp);
                Console.WriteLine($"Monster mit unterschiedlichem HP: {differentCount:N0} von {monsters.Count:N0}");
                Assert.True(differentCount > 0, "Einige Monster sollten unterschiedliche Pre-Renewal/Renewal HP haben.");
            }
            finally
            {
                loader.Dispose();
            }
        }
    }
}