using System;
using System.Collections.Generic;
using System.Linq;

namespace RagnaController.Core
{
    /// <summary>
    /// TASK-3002: MapDatabaseService — Warp-Punkte, Kafra, Dungeon-Eingänge, Levelling-Spots.
    ///
    /// Verwaltet RO-Karten-Daten für Navigation und Levelling-Unterstützung.
    /// White-Hat: Reine Datenbereitstellung, keine automatische Navigation.
    /// </summary>
    public sealed class MapDatabaseService
    {
        private readonly List<MapEntry> _maps = new();

        public MapDatabaseService()
        {
            InitializeStarterMaps();
        }

        public IReadOnlyList<MapEntry> Maps => _maps;

        public void InitializeStarterMaps()
        {
            _maps.AddRange(new[]
            {
                new MapEntry { Id = 1, Name = "prontera", DisplayName = "Prontera", Type = MapType.City, BaseLevelMin = 1, BaseLevelMax = 15, IsKafra = true },
                new MapEntry { Id = 2, Name = "payon", DisplayName = "Payon", Type = MapType.City, BaseLevelMin = 1, BaseLevelMax = 15, IsKafra = true },
                new MapEntry { Id = 3, Name = "geffen", DisplayName = "Geffen", Type = MapType.City, BaseLevelMin = 1, BaseLevelMax = 15, IsKafra = true },
                new MapEntry { Id = 4, Name = "pay_fild01", DisplayName = "Payon Field 01", Type = MapType.Field, BaseLevelMin = 1, BaseLevelMax = 25 },
                new MapEntry { Id = 5, Name = "pay_fild04", DisplayName = "Payon Field 04", Type = MapType.Field, BaseLevelMin = 10, BaseLevelMax = 40 },
                new MapEntry { Id = 6, Name = "prt_fild01", DisplayName = "Prontera Field 01", Type = MapType.Field, BaseLevelMin = 10, BaseLevelMax = 50 },
                new MapEntry { Id = 7, Name = "prt_dun01", DisplayName = "Prontera Dungeon 01", Type = MapType.Dungeon, BaseLevelMin = 30, BaseLevelMax = 70 },
                new MapEntry { Id = 8, Name = "pay_dun01", DisplayName = "Payon Dungeon 01", Type = MapType.Dungeon, BaseLevelMin = 50, BaseLevelMax = 90 },
                new MapEntry { Id = 9, Name = "comodo", DisplayName = "Comodo", Type = MapType.City, BaseLevelMin = 40, BaseLevelMax = 70, IsKafra = true },
                new MapEntry { Id = 10, Name = "jawaii", DisplayName = "Jawaii", Type = MapType.Field, BaseLevelMin = 50, BaseLevelMax = 80 },
            });
        }

        public MapEntry? GetMap(int id) => _maps.FirstOrDefault(m => m.Id == id);
        public MapEntry? GetMap(string name) => _maps.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public List<MapEntry> GetMapsForLevelRange(int baseLevel)
        {
            return _maps.Where(m => m.BaseLevelMin <= baseLevel && m.BaseLevelMax >= baseLevel).ToList();
        }

        public List<MapEntry> GetKafraMaps() => _maps.Where(m => m.IsKafra).ToList();
        public List<MapEntry> GetMapsByType(MapType type) => _maps.Where(m => m.Type == type).ToList();
    }

    public class MapEntry
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public MapType Type { get; set; }
        public int BaseLevelMin { get; set; }
        public int BaseLevelMax { get; set; }
        public bool IsKafra { get; set; }
        public List<int> WarpDestinations { get; set; } = new();
    }

    public enum MapType
    {
        City,
        Field,
        Dungeon,
        Instance,
    }
}
