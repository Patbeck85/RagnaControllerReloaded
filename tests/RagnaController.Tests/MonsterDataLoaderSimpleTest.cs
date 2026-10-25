using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using RagnaController.Core.Services;
using RagnaController.Core.Services.Database;
using RagnaController.Models;

namespace RagnaController.Tests
{
    public class MonsterDataLoaderSimpleTest
    {
        private const string TestYaml = @"Body:
  - Id: 1001
    AegisName: SCORPION
    Name: Scorpion
    Level: 24
    Hp: 1109
    BaseExp: 287
    JobExp: 176
    Size: Small
    Race: Insect
    Element: Fire
    Drops:
    - Item: Jellopy
      Rate: 7000
  - Id: 1002
    AegisName: PORING
    Name: Poring
    Level: 1
    Hp: 50
    BaseExp: 2
    JobExp: 1
    Size: Medium
    Race: Plant
    Element: Water
    Drops:
    - Item: Jellopy
      Rate: 7000";

        [Fact]
        public void ParseYaml_SimpleTest_Loads2Monsters()
        {
            var loader = new MonsterDataLoader();
            try
            {
                // Debug: Print the test YAML to see what we're parsing
                Console.WriteLine("=== TEST YAML ===");
                Console.WriteLine(TestYaml);
                Console.WriteLine("=== END YAML ===");
                
                var monsters = loader.LoadFromYamlFromString(TestYaml, false);

                Console.WriteLine($"DEBUG: Found {monsters.Count} monsters");
                foreach (var m in monsters)
                {
                    Console.WriteLine($"  ID={m.Id}, Name={m.Name}, Hp={m.PreRenewalHp}, Race={m.Race}, Element={m.Element}");
                }

                Assert.Equal(2, monsters.Count);
                Assert.Equal(1001, monsters[0].Id);
                Assert.Equal("Scorpion", monsters[0].Name);
                Assert.Equal(1109, monsters[0].PreRenewalHp);
                Assert.Equal(1002, monsters[1].Id);
                Assert.Equal("Poring", monsters[1].Name);
                Assert.Equal(50, monsters[1].PreRenewalHp);
            }
            finally
            {
                loader.Dispose();
            }
        }
    }
}