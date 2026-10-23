using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RagnaController.Models;

namespace RagnaController.Core.Services
{
    /// <summary>
    /// Lädt Rathena mob_db.yml Dateien (Pre-Renewal und Renewal) und konvertiert sie
    /// in RagnaController Monster-Objekte. Unterstützt HTTP-Download von GitHub,
    /// lokalen Cache und Progress-Reporting für große YAML-Dateien (2000+ Monster).
    ///
    /// Die Rathena YAML-Dateien befinden sich unter:
    ///   Pre-Renewal: https://raw.githubusercontent.com/rathena/rathena/master/db/pre-re/mob_db.yml
    ///   Renewal:     https://raw.githubusercontent.com/rathena/rathena/master/db/re/mob_db.yml
    /// </summary>
    public class MonsterDataLoader : IDisposable
    {
        private readonly HttpClient _httpClient;
        private bool _disposed;

        private const string PreRenewalUrl = "https://raw.githubusercontent.com/rathena/rathena/master/db/pre-re/mob_db.yml";
        private const string RenewalUrl = "https://raw.githubusercontent.com/rathena/rathena/master/db/re/mob_db.yml";

        public event EventHandler<(int Percent, string Status)>? ProgressChanged;
        public event EventHandler<int>? ImportCompleted;

        public string CacheDirectory { get; }

        public MonsterDataLoader()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RagnaController-MonsterLoader/1.0");
            _httpClient.Timeout = TimeSpan.FromMinutes(10);

            CacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RagnaController", "monster_cache");
            Directory.CreateDirectory(CacheDirectory);
        }

        public async Task<List<Monster>> LoadFromRathenaOnlineAsync(CancellationToken ct = default)
        {
            ProgressChanged?.Invoke(this, (0, "Lade Pre-Renewal mob_db.yml..."));

            var preRenewalPath = await DownloadYamlAsync(PreRenewalUrl, "pre_re_mob_db.yml", ct);
            ProgressChanged?.Invoke(this, (25, "Lade Renewal mob_db.yml..."));

            var renewalPath = await DownloadYamlAsync(RenewalUrl, "re_mob_db.yml", ct);
            ProgressChanged?.Invoke(this, (50, "Parst Pre-Renewal Monster..."));

            var preRenewal = new Dictionary<int, Monster>();
            if (File.Exists(preRenewalPath))
            {
                var preYaml = File.ReadAllText(preRenewalPath);
                foreach (var m in ParseYaml(preYaml, false))
                    preRenewal[m.Id] = m;
            }

            ProgressChanged?.Invoke(this, (60, "Parst Renewal Monster..."));

            var renewal = new Dictionary<int, Monster>();
            if (File.Exists(renewalPath))
            {
                var renewalYaml = File.ReadAllText(renewalPath);
                foreach (var m in ParseYaml(renewalYaml, true))
                    renewal[m.Id] = m;
            }

            ProgressChanged?.Invoke(this, (75, "Merge Pre-Renewal + Renewal..."));

            var result = MergeDictionaries(preRenewal, renewal);

            ProgressChanged?.Invoke(this, (100, $"Fertig: {result.Count} Monster geladen"));
            ImportCompleted?.Invoke(this, result.Count);

            return result;
        }

        public List<Monster> LoadFromYaml(string path, bool isRenewal)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Rathena mob_db.yml nicht gefunden: {path}", path);

            var yaml = File.ReadAllText(path);
            return ParseYaml(yaml, isRenewal);
        }

        /// <summary>Parst YAML-String direkt (für Tests).</summary>
        public List<Monster> LoadFromYamlFromString(string yaml, bool isRenewal)
        {
            return ParseYaml(yaml, isRenewal);
        }

        public List<Monster> LoadFromPair(string preRenewalPath, string renewalPath)
        {
            var preRenewal = new Dictionary<int, Monster>();
            var renewal = new Dictionary<int, Monster>();

            if (File.Exists(preRenewalPath))
            {
                foreach (var m in LoadFromYaml(preRenewalPath, false))
                    preRenewal[m.Id] = m;
            }

            if (File.Exists(renewalPath))
            {
                foreach (var m in LoadFromYaml(renewalPath, true))
                    renewal[m.Id] = m;
            }

            return MergeDictionaries(preRenewal, renewal);
        }

        private async Task<string> DownloadYamlAsync(string url, string cacheFileName, CancellationToken ct)
        {
            var cachePath = Path.Combine(CacheDirectory, cacheFileName);

            if (File.Exists(cachePath))
            {
                var fileAge = DateTime.Now - File.GetLastWriteTime(cachePath);
                if (fileAge < TimeSpan.FromHours(24))
                {
                    System.Diagnostics.Debug.WriteLine($"[MonsterDataLoader] Cache-Treffer: {cacheFileName}");
                    return cachePath;
                }
            }

            ProgressChanged?.Invoke(this, (0, $"Download: {cacheFileName}"));

            try
            {
                using var response = await _httpClient.GetAsync(url, ct);
                ct.ThrowIfCancellationRequested();

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[MonsterDataLoader] HTTP {response.StatusCode} für {url}");
                    if (File.Exists(cachePath))
                        return cachePath;
                    throw new Exception($"HTTP {response.StatusCode} beim Laden von {url}");
                }

                var content = await response.Content.ReadAsStringAsync(ct);
                await File.WriteAllTextAsync(cachePath, content, ct);

                System.Diagnostics.Debug.WriteLine($"[MonsterDataLoader] Heruntergeladen: {cacheFileName} ({content.Length:N0} Bytes)");
                return cachePath;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MonsterDataLoader] Download-Fehler: {ex.Message}");
                if (File.Exists(cachePath))
                    return cachePath;
                throw;
            }
        }

        private List<Monster> MergeDictionaries(Dictionary<int, Monster> preRenewal, Dictionary<int, Monster> renewal)
        {
            var allIds = new HashSet<int>(preRenewal.Keys);
            allIds.UnionWith(renewal.Keys);

            var result = new List<Monster>(allIds.Count);
            foreach (var id in allIds.OrderBy(id => id))
            {
                var pre = preRenewal.TryGetValue(id, out var p) ? p : null;
                var ren = renewal.TryGetValue(id, out var r) ? r : null;

                var merged = Merge(pre, ren);
                if (merged != null)
                    result.Add(merged);
            }

            return result;
        }

        private Monster? Merge(Monster? preRenewal, Monster? renewal)
        {
            if (preRenewal == null && renewal == null)
                return null;

            var baseMonster = preRenewal ?? renewal!;
            var monster = baseMonster.Clone();

            if (renewal != null)
            {
                monster.RenewalHp = renewal.RenewalHp;
                monster.RenewalExp = renewal.RenewalExp;
                monster.RenewalJobExp = renewal.RenewalJobExp;
                monster.RenewalBaseLevel = renewal.RenewalBaseLevel;
                monster.RenewalDrops = new Dictionary<int, int>(renewal.RenewalDrops);
                monster.RenewalSteal = new Dictionary<int, int>(renewal.RenewalSteal);
            }

            if (preRenewal != null)
            {
                monster.PreRenewalHp = preRenewal.PreRenewalHp;
                monster.PreRenewalExp = preRenewal.PreRenewalExp;
                monster.PreRenewalJobExp = preRenewal.PreRenewalJobExp;
                monster.PreRenewalBaseLevel = preRenewal.PreRenewalBaseLevel;
                monster.PreRenewalDrops = new Dictionary<int, int>(preRenewal.PreRenewalDrops);
                monster.PreRenewalSteal = new Dictionary<int, int>(preRenewal.PreRenewalSteal);
            }

            return monster;
        }

        private List<Monster> ParseYaml(string yaml, bool isRenewal)
        {
            var monsters = new List<Monster>();
            // Split by newline character
            var lines = yaml.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
            }

            var currentBlock = new List<string>();
            var inBlock = false;

            foreach (var line in lines)
            {
                var trimmed = line.Trim('\r', ' ', '\t');

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                    continue;


                if (trimmed.StartsWith("- Id:"))
                {
                    if (inBlock && currentBlock.Count > 0)
                    {
                        var m = ParseMonsterBlock(currentBlock, isRenewal);
                        if (m != null) 
                        {
                            monsters.Add(m);
                        }
                        else
                        {
                        }
                        currentBlock.Clear();
                    }

                    currentBlock = new List<string> { trimmed.Substring(2).Trim() };
                    inBlock = true;
                    continue;
                }

                if (!inBlock)
                    continue;

                if (inBlock)
                {
                    currentBlock.Add(trimmed);
                }
            }

            if (inBlock && currentBlock.Count > 0)
            {
                var m = ParseMonsterBlock(currentBlock, isRenewal);
                if (m != null) 
                {
                    monsters.Add(m);
                }
                else
                {
                }
            }

            return monsters;
        }

        private Monster? ParseMonsterBlock(List<string> block, bool isRenewal)
        {
            for (int i = 0; i < block.Count; i++)
            {
            }
            try
            {
                var monster = new Monster();
                var drops = new Dictionary<int, int>();
                var steal = new Dictionary<int, int>();
                var inDrops = false;
                var inSteal = false;
                var inMvpDrops = false;

                var itemIdMap = BuildItemIdMap();

                foreach (var line in block)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                        continue;


                    if (trimmed.StartsWith("Drops:"))
                    {
                        inDrops = true; inSteal = false; inMvpDrops = false;
                        continue;
                    }
                    if (trimmed.StartsWith("Steal:"))
                    {
                        inSteal = true; inDrops = false; inMvpDrops = false;
                        continue;
                    }
                    if (trimmed.StartsWith("MvpDrops:"))
                    {
                        inMvpDrops = true; inDrops = false; inSteal = false;
                        continue;
                    }

                    var kvMatch = Regex.Match(trimmed, @"^(\w+):\s*(.*)$");
                    if (kvMatch.Success && !inDrops && !inSteal && !inMvpDrops)
                    {
                        var key = kvMatch.Groups[1].Value;
                        var value = kvMatch.Groups[2].Value.Trim();


                        switch (key)
                        {
                            case "Id":
                                if (int.TryParse(value, out var id)) 
                                {
                                    monster.Id = id;
                                }
                                break;
                            case "Name":
                                monster.Name = value;
                                monster.DisplayName = value;
                                break;
                            case "Level":
                                if (int.TryParse(value, out var lvl))
                                {
                                    monster.PreRenewalBaseLevel = lvl;
                                    monster.RenewalBaseLevel = lvl;
                                }
                                break;
                            case "Hp":
                                if (int.TryParse(value, out var hp))
                                {
                                    if (isRenewal) monster.RenewalHp = hp;
                                    else monster.PreRenewalHp = hp;
                                }
                                break;
                            case "BaseExp":
                                if (int.TryParse(value, out var exp))
                                {
                                    if (isRenewal) monster.RenewalExp = exp;
                                    else monster.PreRenewalExp = exp;
                                }
                                break;
                            case "JobExp":
                                if (int.TryParse(value, out var jexp))
                                {
                                    if (isRenewal) monster.RenewalJobExp = jexp;
                                    else monster.PreRenewalJobExp = jexp;
                                }
                                break;
                            case "MvpExp":
                                if (int.TryParse(value, out var mvpExp) && mvpExp > 0)
                                {
                                    monster.IsMvp = true;
                                }
                                break;
                            case "Size":
                                monster.Size = ParseSize(value);
                                break;
                            case "Race":
                                monster.Race = ParseRace(value);
                                break;
                            case "Element":
                                monster.Element = ParseElement(value);
                                break;
                            default:
                                break;
                        }
                        continue;
                    }

                    if (inDrops || inSteal || inMvpDrops)
                    {
                        var itemMatch = Regex.Match(trimmed, @"^-?\s*Item:\s*(.+)$");
                        if (itemMatch.Success)
                        {
                            var itemName = itemMatch.Groups[1].Value.Trim();
                            var itemId = itemIdMap.TryGetValue(itemName, out var id) ? id : 0;
                            currentDropItem = itemName;
                            currentDropItemId = itemId;
                            continue;
                        }

                        var rateMatch = Regex.Match(trimmed, @"^-?\s*Rate:\s*(\d+)");
                        if (rateMatch.Success && currentDropItemId > 0)
                        {
                            var rate = int.Parse(rateMatch.Groups[1].Value);
                            if (inDrops)
                                drops[currentDropItemId] = rate;
                            else if (inSteal)
                                steal[currentDropItemId] = rate;
                            currentDropItemId = 0;
                            currentDropItem = null;
                        }
                    }
                }


                if (monster.Id == 0) 
                {
                    return null;
                }

                monster.PreRenewalDrops = drops;
                monster.RenewalDrops = new Dictionary<int, int>(drops);
                monster.PreRenewalSteal = steal;
                monster.RenewalSteal = new Dictionary<int, int>(steal);

                return monster;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private string? currentDropItem;
        private int currentDropItemId;

        private Dictionary<string, int> BuildItemIdMap()
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                {"Jellopy", 501}, {"Apple", 512}, {"Orange", 517},
                {"Red_Potion", 533}, {"Yellow_Potion", 534}, {"White_Potion", 535}, {"Blue_Potion", 536},
                {"Green_Herb", 7135}, {"Blue_Herb", 7136}, {"Red_Herb", 7137}, {"White_Herb", 7138}, {"Yellow_Herb", 7139},
                {"Awakening_Potion", 7140}, {"Berserk_Potion", 7141},
                {"Fire_Arrow", 7142}, {"Wind_Arrow", 7143}, {"Earth_Arrow", 7144}, {"Water_Arrow", 7145},
                {"Poison_Arrow", 7146}, {"Holy_Arrow", 7147}, {"Shadow_Arrow", 7148}, {"Ghost_Arrow", 7149}, {"Undead_Arrow", 7150},
                {"Steel_Arrow", 502},
                {"Iron_Ore", 603}, {"Crystal_Blue", 604}, {"Crystal_Red", 605},
                {"Yellow_Gemstone", 606}, {"Blue_Gemstone", 607}, {"Red_Gemstone", 608}, {"White_Gemstone", 609},
                {"Black_Gemstone", 610}, {"Green_Gemstone", 611},
                {"Rough_Oridecon", 612}, {"Rough_Elunium", 613},
                {"Oridecon_Stone", 972}, {"Elunium_Stone", 973}, {"Oridecon", 984}, {"Elunium", 985},
                {"Fly_Wing", 709}, {"Butterfly_Wing", 710}, {"Return_Scroll", 725}, {"Teleport_Scroll", 726}, {"Warp_Scroll", 727},
                {"Elder_Willow_Leaf", 537}, {"Yggdrasil_Berry", 538}, {"Yggdrasil_Leaf", 539},
                {"Apple_Of_Archer", 714},
                {"Alcohol", 547}, {"Fire_Bottle", 651}, {"Ice_Bottle", 652}, {"Wind_Bottle", 653},
                {"Earth_Bottle", 654}, {"Poison_Bottle", 655}, {"Holy_Bottle", 656}, {"Shadow_Bottle", 657},
                {"Ghost_Bottle", 658}, {"Undead_Bottle", 659}, {"Acid_Bottle", 660},

                {"Poring_Card", 7239}, {"Fabre_Card", 7230}, {"Marine_Sphere", 7229}, {"Chonchon_Doll", 7224},
                {"Wolf_Card", 7145}, {"Spore_Card", 7144}, {"Willow_Card", 7143}, {"Condor_Card", 7142},
                {"Farmiliar_Card", 7141}, {"Pupa_Card", 7140}, {"Hornet_Card", 7139}, {"Scorpion_Card", 7138},
                {"Roda_Frog_Card", 7137}, {"Hunter_Fly_Card", 7136}, {"Thief_Bug_Card", 7135},
                {"Zombie_Card", 7134}, {"Skeleton_Card", 7133}, {"Deviace_Card", 7132},
                {"Bone_Soldier_Card", 7131}, {"Atroce_Card", 7130}, {"Orc_Warrior_Card", 7129},
                {"Bapho_Jr_Card", 7128}, {"Lord_Knight_Card", 7127}, {"Golden_Thief_Bug_Card", 7126},
                {"Stormy_Knight_Card", 7125}, {"Queen_Ant_Card", 7124}, {"Baphomet_Jr_Card", 7123},
                {"Dark_Lord_Card", 7122}, {"Turtle_General_Card", 7121}, {"General_Glast_Heim_Card", 7120},
                {"Anubis_Card", 7171}, {"High_Orc_Card", 7172}, {"Mimic_Card", 7174},
                {"Desert_Wolf_Card", 7224}, {"Poison_Spore_Card", 7144}, {"Mandragora_Card", 7146},
                {"Larva_Card", 7239}, {"Worker_Ant_Card", 7239}, {"Soldier_Ant_Card", 7239},
                {"Gargoyle_Card", 7175}, {"Ice_Titan_Card", 7176}, {"Hydra_Card", 7177},
                {"Ancient_Flower_Card", 7146}, {"Skeggiold_Card", 6107}, {"Orc_Zombie_Card", 6107},
                {"Orc_Archer_Card", 2201}, {"Orc_Lady_Card", 7174}, {"Stinger_Card", 7145},
                {"Freezer_Card", 7146}, {"Marine_Sphere_Card", 7144}, {"Will_o_the_wisp_Card", 7142},
                {"Rose_Card", 7145}, {"Crystal_Shooter_Card", 7144},

                {"Card", 0}, {"Broken_Bottle", 601}, {"Empty_Bottle", 602},
                {"Lucky_Bullet", 706}, {"Cursed_Bullet", 707}, {"Silver_Bullet", 708},
                {"Fire_Bullet", 709}, {"Water_Bullet", 710}, {"Wind_Bullet", 711}, {"Earth_Bullet", 712},
                {"Poison_Bullet", 713}, {"Holy_Bullet", 714}, {"Shadow_Bullet", 715}, {"Ghost_Bullet", 716}, {"Undead_Bullet", 717},
                {"Old_Blue_Box", 701}, {"Old_Violet_Box", 702}, {"Old_Red_Box", 703},
                {"Old_Green_Box", 704}, {"Old_Yellow_Box", 705},
                {"Memory_Of_Thanatos", 728}, {"Slim_Potion", 729},
                {"Roasted_Corn", 507}, {"Cheese", 508}, {"Melon", 509}, {"Strawberry", 510}, {"Grapes", 511},
                {"Cactus", 513}, {"Sweet_Potato", 514}, {"Cookies", 515}, {"Bread", 516},
                {"Fried_Chicken", 518}, {"Steamed_Bun", 519}, {"Rice_Cake", 520},
                {"Peach", 521}, {"Pineapple", 522}, {"Mango", 523}, {"Cherry", 524},
                {"Watermelon", 525}, {"Avocado", 526}, {"Papaya", 527}, {"Kiwi", 528},
                {"Lychee", 529}, {"Passion_Fruit", 530}, {"Guava", 531}, {"Persimmon", 532},
                {"Citron", 533}, {"Durian", 534}, {"Star_Fruit", 535}
            };
        }

        private MonsterSize ParseSize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return MonsterSize.Medium;
            return value.ToLowerInvariant() switch
            {
                "small" => MonsterSize.Small,
                "medium" => MonsterSize.Medium,
                "large" => MonsterSize.Large,
                _ => MonsterSize.Medium
            };
        }

        private MonsterRace ParseRace(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return MonsterRace.Formless;
            return value.ToLowerInvariant() switch
            {
                "formless" => MonsterRace.Formless,
                "undead" => MonsterRace.Undead,
                "beast" => MonsterRace.Beast,
                "brute" => MonsterRace.Brute,
                "plant" => MonsterRace.Plant,
                "insect" => MonsterRace.Insect,
                "fish" => MonsterRace.Fish,
                "demon" => MonsterRace.Demon,
                "demihuman" => MonsterRace.DemiHuman,
                "angel" => MonsterRace.Angel,
                "dragon" => MonsterRace.Dragon,
                "dark" => MonsterRace.Demon,  // Rathena uses "Dark" for Demon race
                _ => MonsterRace.Formless
            };
        }

        private MonsterElement ParseElement(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return MonsterElement.Neutral;
            return value.ToLowerInvariant() switch
            {
                "neutral" => MonsterElement.Neutral,
                "water" => MonsterElement.Water,
                "earth" => MonsterElement.Earth,
                "fire" => MonsterElement.Fire,
                "wind" => MonsterElement.Wind,
                "poison" => MonsterElement.Poison,
                "holy" => MonsterElement.Holy,
                "shadow" => MonsterElement.Shadow,
                "dark" => MonsterElement.Shadow,  // Rathena uses "Dark" for Shadow element
                "ghost" => MonsterElement.Ghost,
                "undead" => MonsterElement.Undead,
                _ => MonsterElement.Neutral
            };
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _httpClient.Dispose();
        }
    }
}