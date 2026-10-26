using System;
using System.Collections.Generic;
using System.Linq;

namespace RagnaController.Core.Database {
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
                // ── Städte ──
                new MapEntry { Id = 1, Name = "prontera", DisplayName = "Prontera", Type = MapType.City, BaseLevelMin = 1, BaseLevelMax = 15, IsKafra = true },
                new MapEntry { Id = 2, Name = "payon", DisplayName = "Payon", Type = MapType.City, BaseLevelMin = 1, BaseLevelMax = 15, IsKafra = true },
                new MapEntry { Id = 3, Name = "geffen", DisplayName = "Geffen", Type = MapType.City, BaseLevelMin = 1, BaseLevelMax = 15, IsKafra = true },
                new MapEntry { Id = 4, Name = "aldebaran", DisplayName = "AldeBaran", Type = MapType.City, BaseLevelMin = 1, BaseLevelMax = 15, IsKafra = true },
                new MapEntry { Id = 5, Name = "alberta", DisplayName = "Alberta", Type = MapType.City, BaseLevelMin = 1, BaseLevelMax = 15, IsKafra = true },
                new MapEntry { Id = 6, Name = "comodo", DisplayName = "Comodo", Type = MapType.City, BaseLevelMin = 40, BaseLevelMax = 70, IsKafra = true },
                new MapEntry { Id = 7, Name = "lighthalzen", DisplayName = "Lighthalzen", Type = MapType.City, BaseLevelMin = 70, BaseLevelMax = 120, IsKafra = true },
                new MapEntry { Id = 8, Name = "morroc", DisplayName = "Morroc", Type = MapType.City, BaseLevelMin = 20, BaseLevelMax = 60, IsKafra = true },
                new MapEntry { Id = 9, Name = "rachel", DisplayName = "Rachel", Type = MapType.City, BaseLevelMin = 70, BaseLevelMax = 120, IsKafra = true },
                new MapEntry { Id = 10, Name = "yuno", DisplayName = "Yuno", Type = MapType.City, BaseLevelMin = 70, BaseLevelMax = 120, IsKafra = true },
                new MapEntry { Id = 11, Name = "einbech", DisplayName = "Einbech", Type = MapType.City, BaseLevelMin = 60, BaseLevelMax = 100, IsKafra = true },
                new MapEntry { Id = 12, Name = "einbroch", DisplayName = "Einbroch", Type = MapType.City, BaseLevelMin = 60, BaseLevelMax = 100, IsKafra = true },
                new MapEntry { Id = 13, Name = "hugel", DisplayName = "Hugel", Type = MapType.City, BaseLevelMin = 40, BaseLevelMax = 80, IsKafra = true },
                new MapEntry { Id = 14, Name = "rache", DisplayName = "Rache", Type = MapType.City, BaseLevelMin = 70, BaseLevelMax = 120, IsKafra = true },
                new MapEntry { Id = 15, Name = "veins", DisplayName = "Veins", Type = MapType.City, BaseLevelMin = 70, BaseLevelMax = 120, IsKafra = true },
                new MapEntry { Id = 16, Name = "al_debaran", DisplayName = "AldeBaran (Alt)", Type = MapType.City, BaseLevelMin = 1, BaseLevelMax = 15, IsKafra = true },

                // ── Felder (Payon-Region) ──
                new MapEntry { Id = 101, Name = "pay_fild01", DisplayName = "Payon Field 01", Type = MapType.Field, BaseLevelMin = 1, BaseLevelMax = 25 },
                new MapEntry { Id = 102, Name = "pay_fild02", DisplayName = "Payon Field 02", Type = MapType.Field, BaseLevelMin = 5, BaseLevelMax = 30 },
                new MapEntry { Id = 103, Name = "pay_fild03", DisplayName = "Payon Field 03", Type = MapType.Field, BaseLevelMin = 10, BaseLevelMax = 35 },
                new MapEntry { Id = 104, Name = "pay_fild04", DisplayName = "Payon Field 04", Type = MapType.Field, BaseLevelMin = 10, BaseLevelMax = 40 },
                new MapEntry { Id = 105, Name = "pay_fild05", DisplayName = "Payon Field 05", Type = MapType.Field, BaseLevelMin = 15, BaseLevelMax = 45 },
                new MapEntry { Id = 106, Name = "pay_fild06", DisplayName = "Payon Field 06", Type = MapType.Field, BaseLevelMin = 20, BaseLevelMax = 50 },
                new MapEntry { Id = 107, Name = "pay_fild07", DisplayName = "Payon Field 07", Type = MapType.Field, BaseLevelMin = 25, BaseLevelMax = 55 },
                new MapEntry { Id = 108, Name = "pay_fild08", DisplayName = "Payon Field 08", Type = MapType.Field, BaseLevelMin = 30, BaseLevelMax = 60 },
                new MapEntry { Id = 109, Name = "pay_fild09", DisplayName = "Payon Field 09", Type = MapType.Field, BaseLevelMin = 35, BaseLevelMax = 65 },
                new MapEntry { Id = 110, Name = "pay_fild10", DisplayName = "Payon Field 10", Type = MapType.Field, BaseLevelMin = 40, BaseLevelMax = 70 },
                new MapEntry { Id = 111, Name = "pay_fild11", DisplayName = "Payon Field 11", Type = MapType.Field, BaseLevelMin = 45, BaseLevelMax = 75 },

                // ── Felder (Prontera-Region) ──
                new MapEntry { Id = 201, Name = "prt_fild01", DisplayName = "Prontera Field 01", Type = MapType.Field, BaseLevelMin = 10, BaseLevelMax = 50 },
                new MapEntry { Id = 202, Name = "prt_fild02", DisplayName = "Prontera Field 02", Type = MapType.Field, BaseLevelMin = 15, BaseLevelMax = 55 },
                new MapEntry { Id = 203, Name = "prt_fild03", DisplayName = "Prontera Field 03", Type = MapType.Field, BaseLevelMin = 20, BaseLevelMax = 60 },
                new MapEntry { Id = 204, Name = "prt_fild04", DisplayName = "Prontera Field 04", Type = MapType.Field, BaseLevelMin = 25, BaseLevelMax = 65 },
                new MapEntry { Id = 205, Name = "prt_fild05", DisplayName = "Prontera Field 05", Type = MapType.Field, BaseLevelMin = 30, BaseLevelMax = 70 },
                new MapEntry { Id = 206, Name = "prt_fild06", DisplayName = "Prontera Field 06", Type = MapType.Field, BaseLevelMin = 35, BaseLevelMax = 75 },
                new MapEntry { Id = 207, Name = "prt_fild07", DisplayName = "Prontera Field 07", Type = MapType.Field, BaseLevelMin = 40, BaseLevelMax = 80 },
                new MapEntry { Id = 208, Name = "prt_fild08", DisplayName = "Prontera Field 08", Type = MapType.Field, BaseLevelMin = 45, BaseLevelMax = 85 },
                new MapEntry { Id = 209, Name = "prt_fild09", DisplayName = "Prontera Field 09", Type = MapType.Field, BaseLevelMin = 50, BaseLevelMax = 90 },
                new MapEntry { Id = 210, Name = "prt_fild10", DisplayName = "Prontera Field 10", Type = MapType.Field, BaseLevelMin = 55, BaseLevelMax = 95 },
                new MapEntry { Id = 211, Name = "prt_fild11", DisplayName = "Prontera Field 11", Type = MapType.Field, BaseLevelMin = 60, BaseLevelMax = 100 },

                // ── Felder (Geffen-Region) ──
                new MapEntry { Id = 301, Name = "gef_fild00", DisplayName = "Geffen Field 00", Type = MapType.Field, BaseLevelMin = 1, BaseLevelMax = 25 },
                new MapEntry { Id = 302, Name = "gef_fild01", DisplayName = "Geffen Field 01", Type = MapType.Field, BaseLevelMin = 10, BaseLevelMax = 40 },
                new MapEntry { Id = 303, Name = "gef_fild02", DisplayName = "Geffen Field 02", Type = MapType.Field, BaseLevelMin = 15, BaseLevelMax = 45 },
                new MapEntry { Id = 304, Name = "gef_fild03", DisplayName = "Geffen Field 03", Type = MapType.Field, BaseLevelMin = 20, BaseLevelMax = 50 },
                new MapEntry { Id = 305, Name = "gef_fild04", DisplayName = "Geffen Field 04", Type = MapType.Field, BaseLevelMin = 25, BaseLevelMax = 55 },
                new MapEntry { Id = 306, Name = "gef_fild05", DisplayName = "Geffen Field 05", Type = MapType.Field, BaseLevelMin = 30, BaseLevelMax = 60 },
                new MapEntry { Id = 307, Name = "gef_fild06", DisplayName = "Geffen Field 06", Type = MapType.Field, BaseLevelMin = 35, BaseLevelMax = 65 },
                new MapEntry { Id = 308, Name = "gef_fild07", DisplayName = "Geffen Field 07", Type = MapType.Field, BaseLevelMin = 40, BaseLevelMax = 70 },
                new MapEntry { Id = 309, Name = "gef_fild08", DisplayName = "Geffen Field 08", Type = MapType.Field, BaseLevelMin = 45, BaseLevelMax = 75 },
                new MapEntry { Id = 310, Name = "gef_fild09", DisplayName = "Geffen Field 09", Type = MapType.Field, BaseLevelMin = 50, BaseLevelMax = 80 },
                new MapEntry { Id = 311, Name = "gef_fild10", DisplayName = "Geffen Field 10", Type = MapType.Field, BaseLevelMin = 55, BaseLevelMax = 85 },
                new MapEntry { Id = 312, Name = "gef_fild11", DisplayName = "Geffen Field 11", Type = MapType.Field, BaseLevelMin = 60, BaseLevelMax = 90 },
                new MapEntry { Id = 313, Name = "gef_fild12", DisplayName = "Geffen Field 12", Type = MapType.Field, BaseLevelMin = 65, BaseLevelMax = 95 },

                // ── Felder (AldeBaran-Region) ──
                new MapEntry { Id = 401, Name = "ald_fild01", DisplayName = "AldeBaran Field 01", Type = MapType.Field, BaseLevelMin = 1, BaseLevelMax = 20 },
                new MapEntry { Id = 402, Name = "ald_fild02", DisplayName = "AldeBaran Field 02", Type = MapType.Field, BaseLevelMin = 5, BaseLevelMax = 25 },
                new MapEntry { Id = 403, Name = "ald_fild03", DisplayName = "AldeBaran Field 03", Type = MapType.Field, BaseLevelMin = 10, BaseLevelMax = 30 },
                new MapEntry { Id = 404, Name = "ald_fild04", DisplayName = "AldeBaran Field 04", Type = MapType.Field, BaseLevelMin = 15, BaseLevelMax = 35 },
                new MapEntry { Id = 405, Name = "ald_fild05", DisplayName = "AldeBaran Field 05", Type = MapType.Field, BaseLevelMin = 20, BaseLevelMax = 40 },

                // ── Felder (Morroc-Region) ──
                new MapEntry { Id = 501, Name = "mor_fild01", DisplayName = "Morroc Field 01", Type = MapType.Field, BaseLevelMin = 20, BaseLevelMax = 50 },
                new MapEntry { Id = 502, Name = "mor_fild02", DisplayName = "Morroc Field 02", Type = MapType.Field, BaseLevelMin = 25, BaseLevelMax = 55 },
                new MapEntry { Id = 503, Name = "mor_fild03", DisplayName = "Morroc Field 03", Type = MapType.Field, BaseLevelMin = 30, BaseLevelMax = 60 },
                new MapEntry { Id = 504, Name = "mor_fild04", DisplayName = "Morroc Field 04", Type = MapType.Field, BaseLevelMin = 35, BaseLevelMax = 65 },
                new MapEntry { Id = 505, Name = "mor_fild05", DisplayName = "Morroc Field 05", Type = MapType.Field, BaseLevelMin = 40, BaseLevelMax = 70 },
                new MapEntry { Id = 506, Name = "mor_fild06", DisplayName = "Morroc Field 06", Type = MapType.Field, BaseLevelMin = 45, BaseLevelMax = 75 },
                new MapEntry { Id = 507, Name = "mor_fild07", DisplayName = "Morroc Field 07", Type = MapType.Field, BaseLevelMin = 50, BaseLevelMax = 80 },
                new MapEntry { Id = 508, Name = "mor_fild08", DisplayName = "Morroc Field 08", Type = MapType.Field, BaseLevelMin = 55, BaseLevelMax = 85 },
                new MapEntry { Id = 509, Name = "mor_fild09", DisplayName = "Morroc Field 09", Type = MapType.Field, BaseLevelMin = 60, BaseLevelMax = 90 },
                new MapEntry { Id = 510, Name = "mor_fild10", DisplayName = "Morroc Field 10", Type = MapType.Field, BaseLevelMin = 65, BaseLevelMax = 95 },
                new MapEntry { Id = 511, Name = "mor_fild11", DisplayName = "Morroc Field 11", Type = MapType.Field, BaseLevelMin = 70, BaseLevelMax = 100 },
                new MapEntry { Id = 512, Name = "mor_fild12", DisplayName = "Morroc Field 12", Type = MapType.Field, BaseLevelMin = 75, BaseLevelMax = 105 },
                new MapEntry { Id = 513, Name = "mor_fild13", DisplayName = "Morroc Field 13", Type = MapType.Field, BaseLevelMin = 80, BaseLevelMax = 110 },
                new MapEntry { Id = 514, Name = "mor_fild14", DisplayName = "Morroc Field 14", Type = MapType.Field, BaseLevelMin = 85, BaseLevelMax = 115 },
                new MapEntry { Id = 515, Name = "mor_fild15", DisplayName = "Morroc Field 15", Type = MapType.Field, BaseLevelMin = 90, BaseLevelMax = 120 },
                new MapEntry { Id = 516, Name = "mor_fild16", DisplayName = "Morroc Field 16", Type = MapType.Field, BaseLevelMin = 95, BaseLevelMax = 125 },

                // ── Dungeons (Payon) ──
                new MapEntry { Id = 601, Name = "pay_dun00", DisplayName = "Payon Dungeon 00", Type = MapType.Dungeon, BaseLevelMin = 30, BaseLevelMax = 60 },
                new MapEntry { Id = 602, Name = "pay_dun01", DisplayName = "Payon Dungeon 01", Type = MapType.Dungeon, BaseLevelMin = 50, BaseLevelMax = 90 },
                new MapEntry { Id = 603, Name = "pay_dun02", DisplayName = "Payon Dungeon 02", Type = MapType.Dungeon, BaseLevelMin = 60, BaseLevelMax = 100 },
                new MapEntry { Id = 604, Name = "pay_dun03", DisplayName = "Payon Dungeon 03", Type = MapType.Dungeon, BaseLevelMin = 70, BaseLevelMax = 110 },
                new MapEntry { Id = 605, Name = "pay_dun04", DisplayName = "Payon Dungeon 04", Type = MapType.Dungeon, BaseLevelMin = 80, BaseLevelMax = 120 },

                // ── Dungeons (Prontera) ──
                new MapEntry { Id = 611, Name = "prt_dun01", DisplayName = "Prontera Dungeon 01", Type = MapType.Dungeon, BaseLevelMin = 30, BaseLevelMax = 70 },
                new MapEntry { Id = 612, Name = "prt_dun02", DisplayName = "Prontera Dungeon 02", Type = MapType.Dungeon, BaseLevelMin = 50, BaseLevelMax = 90 },
                new MapEntry { Id = 613, Name = "prt_dun03", DisplayName = "Prontera Dungeon 03", Type = MapType.Dungeon, BaseLevelMin = 60, BaseLevelMax = 100 },
                new MapEntry { Id = 614, Name = "prt_dun04", DisplayName = "Prontera Dungeon 04", Type = MapType.Dungeon, BaseLevelMin = 70, BaseLevelMax = 110 },
                new MapEntry { Id = 615, Name = "prt_dun05", DisplayName = "Prontera Dungeon 05", Type = MapType.Dungeon, BaseLevelMin = 80, BaseLevelMax = 120 },

                // ── Dungeons (Geffen) ──
                new MapEntry { Id = 621, Name = "gef_dun00", DisplayName = "Geffen Dungeon 00", Type = MapType.Dungeon, BaseLevelMin = 40, BaseLevelMax = 70 },
                new MapEntry { Id = 622, Name = "gef_dun01", DisplayName = "Geffen Dungeon 01", Type = MapType.Dungeon, BaseLevelMin = 50, BaseLevelMax = 80 },
                new MapEntry { Id = 623, Name = "gef_dun02", DisplayName = "Geffen Dungeon 02", Type = MapType.Dungeon, BaseLevelMin = 60, BaseLevelMax = 90 },
                new MapEntry { Id = 624, Name = "gef_dun03", DisplayName = "Geffen Dungeon 03", Type = MapType.Dungeon, BaseLevelMin = 70, BaseLevelMax = 100 },

                // ── Dungeons (Comodo) ──
                new MapEntry { Id = 631, Name = "comodo_dun00", DisplayName = "Comodo Dungeon 00", Type = MapType.Dungeon, BaseLevelMin = 30, BaseLevelMax = 60 },
                new MapEntry { Id = 632, Name = "comodo_dun01", DisplayName = "Comodo Dungeon 01", Type = MapType.Dungeon, BaseLevelMin = 40, BaseLevelMax = 70 },
                new MapEntry { Id = 633, Name = "comodo_dun02", DisplayName = "Comodo Dungeon 02", Type = MapType.Dungeon, BaseLevelMin = 50, BaseLevelMax = 80 },

                // ── Dungeons (Eis-Region) ──
                new MapEntry { Id = 641, Name = "ice_dun01", DisplayName = "Ice Dungeon 01", Type = MapType.Dungeon, BaseLevelMin = 45, BaseLevelMax = 75 },
                new MapEntry { Id = 642, Name = "ice_dun02", DisplayName = "Ice Dungeon 02", Type = MapType.Dungeon, BaseLevelMin = 55, BaseLevelMax = 85 },
                new MapEntry { Id = 643, Name = "ice_dun03", DisplayName = "Ice Dungeon 03", Type = MapType.Dungeon, BaseLevelMin = 65, BaseLevelMax = 95 },
                new MapEntry { Id = 644, Name = "ice_dun04", DisplayName = "Ice Dungeon 04", Type = MapType.Dungeon, BaseLevelMin = 75, BaseLevelMax = 105 },

                // ── Dungeons (Ameisen-Nest) ──
                new MapEntry { Id = 651, Name = "ant_hell01", DisplayName = "Ant Hell 01", Type = MapType.Dungeon, BaseLevelMin = 20, BaseLevelMax = 40 },
                new MapEntry { Id = 652, Name = "ant_hell02", DisplayName = "Ant Hell 02", Type = MapType.Dungeon, BaseLevelMin = 30, BaseLevelMax = 50 },
                new MapEntry { Id = 653, Name = "ant_hell03", DisplayName = "Ant Hell 03", Type = MapType.Dungeon, BaseLevelMin = 40, BaseLevelMax = 60 },

                // ── Dungeons (Orc) ──
                new MapEntry { Id = 661, Name = "orc_dun01", DisplayName = "Orc Dungeon 01", Type = MapType.Dungeon, BaseLevelMin = 30, BaseLevelMax = 50 },
                new MapEntry { Id = 662, Name = "orc_dun02", DisplayName = "Orc Dungeon 02", Type = MapType.Dungeon, BaseLevelMin = 40, BaseLevelMax = 60 },
                new MapEntry { Id = 663, Name = "orc_dun03", DisplayName = "Orc Dungeon 03", Type = MapType.Dungeon, BaseLevelMin = 50, BaseLevelMax = 70 },

                // ── Dungeons (Turmschildkröte) ──
                new MapEntry { Id = 671, Name = "tur_dun01", DisplayName = "Turtle Dungeon 01", Type = MapType.Dungeon, BaseLevelMin = 40, BaseLevelMax = 70 },
                new MapEntry { Id = 672, Name = "tur_dun02", DisplayName = "Turtle Dungeon 02", Type = MapType.Dungeon, BaseLevelMin = 50, BaseLevelMax = 80 },
                new MapEntry { Id = 673, Name = "tur_dun03", DisplayName = "Turtle Dungeon 03", Type = MapType.Dungeon, BaseLevelMin = 60, BaseLevelMax = 90 },
                new MapEntry { Id = 674, Name = "tur_dun04", DisplayName = "Turtle Dungeon 04", Type = MapType.Dungeon, BaseLevelMin = 70, BaseLevelMax = 100 },
                new MapEntry { Id = 675, Name = "tur_dun05", DisplayName = "Turtle Dungeon 05", Type = MapType.Dungeon, BaseLevelMin = 80, BaseLevelMax = 110 },
                new MapEntry { Id = 676, Name = "tur_dun06", DisplayName = "Turtle Dungeon 06", Type = MapType.Dungeon, BaseLevelMin = 90, BaseLevelMax = 120 },

                // ── Dungeons (Sphinx) ──
                new MapEntry { Id = 681, Name = "sphinx_dun01", DisplayName = "Sphinx Dungeon 01", Type = MapType.Dungeon, BaseLevelMin = 50, BaseLevelMax = 80 },
                new MapEntry { Id = 682, Name = "sphinx_dun02", DisplayName = "Sphinx Dungeon 02", Type = MapType.Dungeon, BaseLevelMin = 60, BaseLevelMax = 90 },
                new MapEntry { Id = 683, Name = "sphinx_dun03", DisplayName = "Sphinx Dungeon 03", Type = MapType.Dungeon, BaseLevelMin = 70, BaseLevelMax = 100 },
                new MapEntry { Id = 684, Name = "sphinx_dun04", DisplayName = "Sphinx Dungeon 04", Type = MapType.Dungeon, BaseLevelMin = 80, BaseLevelMax = 110 },

                // ── Yuno Dungeons ──
                new MapEntry { Id = 691, Name = "yuno_dun01", DisplayName = "Yuno Dungeon 01", Type = MapType.Dungeon, BaseLevelMin = 70, BaseLevelMax = 100 },
                new MapEntry { Id = 692, Name = "yuno_dun02", DisplayName = "Yuno Dungeon 02", Type = MapType.Dungeon, BaseLevelMin = 80, BaseLevelMax = 110 },
                new MapEntry { Id = 693, Name = "yuno_dun03", DisplayName = "Yuno Dungeon 03", Type = MapType.Dungeon, BaseLevelMin = 90, BaseLevelMax = 120 },

                // ── Spezielle Dungeons ──
                new MapEntry { Id = 701, Name = "glast_heim", DisplayName = "Glast Heim", Type = MapType.Dungeon, BaseLevelMin = 80, BaseLevelMax = 120 },
                new MapEntry { Id = 702, Name = "thor_camp01", DisplayName = "Thor Camp", Type = MapType.Field, BaseLevelMin = 90, BaseLevelMax = 150 },
                new MapEntry { Id = 703, Name = "prison", DisplayName = "Prison", Type = MapType.Dungeon, BaseLevelMin = 30, BaseLevelMax = 60 },
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
