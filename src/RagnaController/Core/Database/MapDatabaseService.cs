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
            _maps.AddRange(BuildAllMaps());
        }

        #region Map Factory (Pre-Renewal + Renewal)

        private static IEnumerable<MapEntry> BuildAllMaps()
        {
            foreach (var m in BuildCities()) yield return m;
            foreach (var m in BuildPayonFields()) yield return m;
            foreach (var m in BuildPronteraFields()) yield return m;
            foreach (var m in BuildGeffenFields()) yield return m;
            foreach (var m in BuildAldebaranFields()) yield return m;
            foreach (var m in BuildMorrocFields()) yield return m;
            foreach (var m in BuildPayonDungeons()) yield return m;
            foreach (var m in BuildPronteraDungeons()) yield return m;
            foreach (var m in BuildGeffenDungeons()) yield return m;
            foreach (var m in BuildComodoDungeons()) yield return m;
            foreach (var m in BuildIceDungeons()) yield return m;
            foreach (var m in BuildAntHellDungeons()) yield return m;
            foreach (var m in BuildOrcDungeons()) yield return m;
            foreach (var m in BuildTurtleDungeons()) yield return m;
            foreach (var m in BuildSphinxDungeons()) yield return m;
            foreach (var m in BuildYunoDungeons()) yield return m;
            foreach (var m in BuildSpecialDungeons()) yield return m;
        }

        // ── Städte ──
        private static IEnumerable<MapEntry> BuildCities()
        {
            yield return City(1, "prontera", "Prontera", 1, 15, 1, 50, new[] { 2, 3, 4, 5, 6, 8 });
            yield return City(2, "payon", "Payon", 1, 15, 1, 40, new[] { 101, 102, 601 });
            yield return City(3, "geffen", "Geffen", 1, 15, 1, 60, new[] { 1, 301, 621 });
            yield return City(4, "aldebaran", "AldeBaran", 1, 15, 1, 50, new[] { 1, 401 });
            yield return City(5, "alberta", "Alberta", 1, 15, 1, 30, new[] { 1 });
            yield return City(6, "comodo", "Comodo", 40, 70, 40, 90, new[] { 631 });
            yield return City(7, "lighthalzen", "Lighthalzen", 70, 120, 70, 150, Array.Empty<int>());
            yield return City(8, "morroc", "Morroc", 20, 60, 20, 100, new[] { 501, 651 });
            yield return City(9, "rachel", "Rachel", 70, 120, 70, 150, Array.Empty<int>());
            yield return City(10, "yuno", "Yuno", 70, 120, 70, 150, new[] { 691 });
            yield return City(11, "einbech", "Einbech", 60, 100, 60, 120, Array.Empty<int>());
            yield return City(12, "einbroch", "Einbroch", 60, 100, 60, 120, Array.Empty<int>());
            yield return City(13, "hugel", "Hugel", 40, 80, 40, 100, Array.Empty<int>());
            yield return City(14, "rache", "Rache", 70, 120, 70, 150, Array.Empty<int>());
            yield return City(15, "veins", "Veins", 70, 120, 70, 150, Array.Empty<int>());
            yield return City(16, "al_debaran", "AldeBaran (Alt)", 1, 15, 1, 50, new[] { 1 });
        }

        private static MapEntry City(int id, string name, string displayName, int prMin, int prMax, int reMin, int reMax, int[] warps)
        {
            return new MapEntry
            {
                Id = id,
                Name = name,
                DisplayName = displayName,
                Type = MapType.City,
                PreRenewalLevelMin = prMin,
                PreRenewalLevelMax = prMax,
                RenewalLevelMin = reMin,
                RenewalLevelMax = reMax,
                IsKafra = true,
                WarpDestinations = warps.ToList(),
                SupportedGameMode = GameMode.Both
            };
        }

        // ── Felder (Payon-Region) ──
        private static IEnumerable<MapEntry> BuildPayonFields()
        {
            yield return Field(101, "pay_fild01", "Payon Field 01", 1, 25, 1, 60, new[] { 2 }, new[] { 1002, 1001, 1008, 1011 }, new[] { 60, 80, 90, 70 });
            yield return Field(102, "pay_fild02", "Payon Field 02", 5, 30, 5, 70, new[] { 2, 103 }, new[] { 1002, 1001, 1038, 1046 }, new[] { 50, 70, 60, 50 });
            yield return Field(103, "pay_fild03", "Payon Field 03", 10, 35, 10, 80, new[] { 2, 104 }, new[] { 1004, 1010, 1033 }, new[] { 70, 60, 80 });
            yield return Field(104, "pay_fild04", "Payon Field 04", 10, 40, 10, 90, new[] { 2, 105 }, new[] { 1005, 1014, 1033 }, new[] { 80, 70, 90 });
            yield return Field(105, "pay_fild05", "Payon Field 05", 15, 45, 15, 100, new[] { 2, 106 }, new[] { 1009, 1014, 1038 }, new[] { 60, 80, 70 });
            yield return Field(106, "pay_fild06", "Payon Field 06", 20, 50, 20, 110, new[] { 2, 107 }, new[] { 1013, 1033, 1090 }, new[] { 90, 80, 60 });
            yield return Field(107, "pay_fild07", "Payon Field 07", 25, 55, 25, 120, new[] { 2, 108 }, new[] { 1013, 1090, 1096 }, new[] { 80, 70, 90 });
            yield return Field(108, "pay_fild08", "Payon Field 08", 30, 60, 30, 130, new[] { 2 }, new[] { 1047, 1092, 1146 }, new[] { 10, 20, 15 });
            yield return Field(109, "pay_fild09", "Payon Field 09", 35, 65, 35, 140, new[] { 2 }, new[] { 1096, 1148, 1149 }, new[] { 80, 90, 70 });
            yield return Field(110, "pay_fild10", "Payon Field 10", 40, 70, 40, 150, new[] { 2 }, new[] { 1148, 1149, 1154 }, new[] { 70, 80, 90 });
            yield return Field(111, "pay_fild11", "Payon Field 11", 45, 75, 45, 160, new[] { 2 }, new[] { 1154, 1155, 1149 }, new[] { 80, 70, 90 });
        }

        // ── Felder (Prontera-Region) ──
        private static IEnumerable<MapEntry> BuildPronteraFields()
        {
            yield return Field(201, "prt_fild01", "Prontera Field 01", 10, 50, 10, 90, new[] { 1 }, new[] { 1002, 1001, 1008 }, new[] { 60, 80, 90 });
            yield return Field(202, "prt_fild02", "Prontera Field 02", 15, 55, 15, 100, new[] { 1 }, new[] { 1004, 1038, 1010 }, new[] { 70, 60, 80 });
            yield return Field(203, "prt_fild03", "Prontera Field 03", 20, 60, 20, 110, new[] { 1 }, new[] { 1013, 1014, 1033 }, new[] { 90, 80, 70 });
            yield return Field(204, "prt_fild04", "Prontera Field 04", 25, 65, 25, 120, new[] { 1 }, new[] { 1013, 1033, 1090 }, new[] { 80, 90, 60 });
            yield return Field(205, "prt_fild05", "Prontera Field 05", 30, 70, 30, 130, new[] { 1 }, new[] { 1090, 1092, 1096 }, new[] { 70, 80, 90 });
            yield return Field(206, "prt_fild06", "Prontera Field 06", 35, 75, 35, 140, new[] { 1 }, new[] { 1096, 1148, 1149 }, new[] { 80, 70, 90 });
            yield return Field(207, "prt_fild07", "Prontera Field 07", 40, 80, 40, 150, new[] { 1 }, new[] { 1148, 1149, 1154 }, new[] { 90, 80, 70 });
            yield return Field(208, "prt_fild08", "Prontera Field 08", 45, 85, 45, 160, new[] { 1 }, new[] { 1154, 1155, 1149 }, new[] { 80, 90, 60 });
            yield return Field(209, "prt_fild09", "Prontera Field 09", 50, 90, 50, 165, new[] { 1 }, new[] { 1149, 1155, 1157 }, new[] { 70, 80, 90 });
            yield return Field(210, "prt_fild10", "Prontera Field 10", 55, 95, 55, 170, new[] { 1 }, new[] { 1157, 1148, 1149 }, new[] { 90, 70, 80 });
            yield return Field(211, "prt_fild11", "Prontera Field 11", 60, 100, 60, 175, new[] { 1 }, new[] { 1148, 1149, 1154 }, new[] { 80, 90, 70 });
        }

        // ── Felder (Geffen-Region) ──
        private static IEnumerable<MapEntry> BuildGeffenFields()
        {
            yield return Field(301, "gef_fild00", "Geffen Field 00", 1, 25, 1, 50, new[] { 3 }, new[] { 1002, 1001, 1008 }, new[] { 60, 80, 90 });
            yield return Field(302, "gef_fild01", "Geffen Field 01", 10, 40, 10, 80, new[] { 3 }, new[] { 1112, 1118, 1121 }, new[] { 80, 70, 60 });
            yield return Field(303, "gef_fild02", "Geffen Field 02", 15, 45, 15, 90, new[] { 3 }, new[] { 1121, 1122, 1123 }, new[] { 70, 80, 60 });
            yield return Field(304, "gef_fild03", "Geffen Field 03", 20, 50, 20, 100, new[] { 3 }, new[] { 1123, 1122, 1118 }, new[] { 80, 70, 90 });
            yield return Field(305, "gef_fild04", "Geffen Field 04", 25, 55, 25, 110, new[] { 3 }, new[] { 1157, 1112, 1118 }, new[] { 90, 80, 70 });
            yield return Field(306, "gef_fild05", "Geffen Field 05", 30, 60, 30, 120, new[] { 3 }, new[] { 1157, 1090, 1096 }, new[] { 80, 90, 60 });
            yield return Field(307, "gef_fild06", "Geffen Field 06", 35, 65, 35, 130, new[] { 3 }, new[] { 1096, 1148, 1149 }, new[] { 70, 80, 90 });
            yield return Field(308, "gef_fild07", "Geffen Field 07", 40, 70, 40, 140, new[] { 3 }, new[] { 1148, 1149, 1154 }, new[] { 90, 70, 80 });
            yield return Field(309, "gef_fild08", "Geffen Field 08", 45, 75, 45, 150, new[] { 3 }, new[] { 1154, 1155, 1149 }, new[] { 80, 90, 70 });
            yield return Field(310, "gef_fild09", "Geffen Field 09", 50, 80, 50, 160, new[] { 3 }, new[] { 1149, 1155, 1157 }, new[] { 70, 80, 90 });
            yield return Field(311, "gef_fild10", "Geffen Field 10", 55, 85, 55, 165, new[] { 3 }, new[] { 1157, 1148, 1149 }, new[] { 90, 70, 80 });
            yield return Field(312, "gef_fild11", "Geffen Field 11", 60, 90, 60, 170, new[] { 3 }, new[] { 1148, 1149, 1154 }, new[] { 80, 90, 70 });
            yield return Field(313, "gef_fild12", "Geffen Field 12", 65, 95, 65, 175, new[] { 3 }, new[] { 1148, 1149, 1154 }, new[] { 80, 90, 70 });
        }

        // ── Felder (AldeBaran-Region) ──
        private static IEnumerable<MapEntry> BuildAldebaranFields()
        {
            yield return Field(401, "ald_fild01", "AldeBaran Field 01", 1, 20, 1, 40, new[] { 4 }, new[] { 1002, 1001, 1008 }, new[] { 60, 80, 90 });
            yield return Field(402, "ald_fild02", "AldeBaran Field 02", 5, 25, 5, 50, new[] { 4 }, new[] { 1038, 1010, 1004 }, new[] { 70, 60, 80 });
            yield return Field(403, "ald_fild03", "AldeBaran Field 03", 10, 30, 10, 60, new[] { 4 }, new[] { 1013, 1014, 1033 }, new[] { 90, 80, 70 });
            yield return Field(404, "ald_fild04", "AldeBaran Field 04", 15, 35, 15, 70, new[] { 4 }, new[] { 1033, 1090, 1013 }, new[] { 80, 90, 60 });
            yield return Field(405, "ald_fild05", "AldeBaran Field 05", 20, 40, 20, 80, new[] { 4 }, new[] { 1090, 1092, 1096 }, new[] { 70, 80, 90 });
        }

        // ── Felder (Morroc-Region) ──
        private static IEnumerable<MapEntry> BuildMorrocFields()
        {
            yield return Field(501, "mor_fild01", "Morroc Field 01", 20, 50, 20, 80, new[] { 8 }, new[] { 1112, 1118, 1121 }, new[] { 80, 70, 60 });
            yield return Field(502, "mor_fild02", "Morroc Field 02", 25, 55, 25, 90, new[] { 8 }, new[] { 1121, 1122, 1123 }, new[] { 70, 80, 60 });
            yield return Field(503, "mor_fild03", "Morroc Field 03", 30, 60, 30, 100, new[] { 8 }, new[] { 1123, 1122, 1118 }, new[] { 80, 70, 90 });
            yield return Field(504, "mor_fild04", "Morroc Field 04", 35, 65, 35, 110, new[] { 8 }, new[] { 1157, 1112, 1118 }, new[] { 90, 80, 70 });
            yield return Field(505, "mor_fild05", "Morroc Field 05", 40, 70, 40, 120, new[] { 8 }, new[] { 1157, 1090, 1096 }, new[] { 80, 90, 60 });
            yield return Field(506, "mor_fild06", "Morroc Field 06", 45, 75, 45, 130, new[] { 8 }, new[] { 1096, 1148, 1149 }, new[] { 70, 80, 90 });
            yield return Field(507, "mor_fild07", "Morroc Field 07", 50, 80, 50, 140, new[] { 8 }, new[] { 1148, 1149, 1154 }, new[] { 90, 70, 80 });
            yield return Field(508, "mor_fild08", "Morroc Field 08", 55, 85, 55, 150, new[] { 8 }, new[] { 1154, 1155, 1149 }, new[] { 80, 90, 70 });
            yield return Field(509, "mor_fild09", "Morroc Field 09", 60, 90, 60, 160, new[] { 8 }, new[] { 1149, 1155, 1157 }, new[] { 70, 80, 90 });
            yield return Field(510, "mor_fild10", "Morroc Field 10", 65, 95, 65, 165, new[] { 8 }, new[] { 1157, 1148, 1149 }, new[] { 90, 70, 80 });
            yield return Field(511, "mor_fild11", "Morroc Field 11", 70, 100, 70, 170, new[] { 8 }, new[] { 1148, 1149, 1154 }, new[] { 80, 90, 70 });
            yield return Field(512, "mor_fild12", "Morroc Field 12", 75, 105, 75, 175, new[] { 8 }, new[] { 1148, 1149, 1154 }, new[] { 80, 90, 70 });
            yield return Field(513, "mor_fild13", "Morroc Field 13", 80, 110, 80, 175, new[] { 8 }, new[] { 1148, 1149, 1154 }, new[] { 80, 90, 70 });
            yield return Field(514, "mor_fild14", "Morroc Field 14", 85, 115, 85, 175, new[] { 8 }, new[] { 1148, 1149, 1154 }, new[] { 80, 90, 70 });
            yield return Field(515, "mor_fild15", "Morroc Field 15", 90, 120, 90, 175, new[] { 8 }, new[] { 1148, 1149, 1154 }, new[] { 80, 90, 70 });
            yield return Field(516, "mor_fild16", "Morroc Field 16", 95, 125, 95, 175, new[] { 8 }, new[] { 1148, 1149, 1154 }, new[] { 80, 90, 70 });
        }

        private static MapEntry Field(int id, string name, string displayName, int prMin, int prMax, int reMin, int reMax, int[] warps, int[] monsterIds, int[] chances)
        {
            var spawns = new Dictionary<int, int>();
            for (int i = 0; i < monsterIds.Length && i < chances.Length; i++)
                spawns[monsterIds[i]] = chances[i];

            return new MapEntry
            {
                Id = id,
                Name = name,
                DisplayName = displayName,
                Type = MapType.Field,
                PreRenewalLevelMin = prMin,
                PreRenewalLevelMax = prMax,
                RenewalLevelMin = reMin,
                RenewalLevelMax = reMax,
                IsKafra = false,
                WarpDestinations = warps.ToList(),
                MonsterSpawns = spawns,
                SupportedGameMode = GameMode.Both
            };
        }

        // ── Dungeons (Payon) ──
        private static IEnumerable<MapEntry> BuildPayonDungeons()
        {
            yield return Dungeon(601, "pay_dun00", "Payon Dungeon 00", 30, 60, 30, 80, new[] { 2 }, new[] { 1091, 1092, 1046 }, new[] { 70, 60, 80 });
            yield return Dungeon(602, "pay_dun01", "Payon Dungeon 01", 50, 90, 50, 110, new[] { 2 }, new[] { 1092, 1046, 1016 }, new[] { 80, 70, 90 });
            yield return Dungeon(603, "pay_dun02", "Payon Dungeon 02", 60, 100, 60, 120, new[] { 2 }, new[] { 1016, 1092, 1091 }, new[] { 90, 80, 70 });
            yield return Dungeon(604, "pay_dun03", "Payon Dungeon 03", 70, 110, 70, 130, new[] { 2 }, new[] { 1092, 1091, 1016 }, new[] { 70, 90, 80 });
            yield return Dungeon(605, "pay_dun04", "Payon Dungeon 04", 80, 120, 80, 140, new[] { 2 }, new[] { 1091, 1016, 1092 }, new[] { 80, 70, 90 });
        }

        // ── Dungeons (Prontera) ──
        private static IEnumerable<MapEntry> BuildPronteraDungeons()
        {
            yield return Dungeon(611, "prt_dun01", "Prontera Dungeon 01", 30, 70, 30, 90, new[] { 1 }, new[] { 1016, 1091, 1092 }, new[] { 80, 70, 90 });
            yield return Dungeon(612, "prt_dun02", "Prontera Dungeon 02", 50, 90, 50, 100, new[] { 1 }, new[] { 1092, 1091, 1016 }, new[] { 90, 80, 70 });
            yield return Dungeon(613, "prt_dun03", "Prontera Dungeon 03", 60, 100, 60, 110, new[] { 1 }, new[] { 1091, 1016, 1092 }, new[] { 70, 90, 80 });
            yield return Dungeon(614, "prt_dun04", "Prontera Dungeon 04", 70, 110, 70, 120, new[] { 1 }, new[] { 1092, 1016, 1091 }, new[] { 80, 70, 90 });
            yield return Dungeon(615, "prt_dun05", "Prontera Dungeon 05", 80, 120, 80, 130, new[] { 1 }, new[] { 1091, 1092, 1016 }, new[] { 90, 80, 70 });
        }

        // ── Dungeons (Geffen) ──
        private static IEnumerable<MapEntry> BuildGeffenDungeons()
        {
            yield return Dungeon(621, "gef_dun00", "Geffen Dungeon 00", 40, 70, 40, 80, new[] { 3 }, new[] { 1091, 1092, 1159 }, new[] { 70, 60, 80 });
            yield return Dungeon(622, "gef_dun01", "Geffen Dungeon 01", 50, 80, 50, 90, new[] { 3 }, new[] { 1092, 1159, 1091 }, new[] { 80, 70, 90 });
            yield return Dungeon(623, "gef_dun02", "Geffen Dungeon 02", 60, 90, 60, 100, new[] { 3 }, new[] { 1159, 1091, 1092 }, new[] { 90, 80, 70 });
            yield return Dungeon(624, "gef_dun03", "Geffen Dungeon 03", 70, 100, 70, 110, new[] { 3 }, new[] { 1092, 1159, 1091 }, new[] { 70, 90, 80 });
        }

        // ── Dungeons (Comodo) ──
        private static IEnumerable<MapEntry> BuildComodoDungeons()
        {
            yield return Dungeon(631, "comodo_dun00", "Comodo Dungeon 00", 30, 60, 30, 70, new[] { 6 }, new[] { 1165, 1163, 1164 }, new[] { 60, 70, 50 });
            yield return Dungeon(632, "comodo_dun01", "Comodo Dungeon 01", 40, 70, 40, 80, new[] { 6 }, new[] { 1164, 1165, 1163 }, new[] { 70, 60, 80 });
            yield return Dungeon(633, "comodo_dun02", "Comodo Dungeon 02", 50, 80, 50, 90, new[] { 6 }, new[] { 1163, 1164, 1165 }, new[] { 80, 50, 60 });
        }

        // ── Dungeons (Eis-Region) ──
        private static IEnumerable<MapEntry> BuildIceDungeons()
        {
            yield return Dungeon(641, "ice_dun01", "Ice Dungeon 01", 45, 75, 45, 85, Array.Empty<int>(), new[] { 1138, 1139, 1129 }, new[] { 70, 60, 80 });
            yield return Dungeon(642, "ice_dun02", "Ice Dungeon 02", 55, 85, 55, 95, Array.Empty<int>(), new[] { 1139, 1138, 1129 }, new[] { 80, 70, 90 });
            yield return Dungeon(643, "ice_dun03", "Ice Dungeon 03", 65, 95, 65, 105, Array.Empty<int>(), new[] { 1129, 1138, 1139 }, new[] { 90, 80, 70 });
            yield return Dungeon(644, "ice_dun04", "Ice Dungeon 04", 75, 105, 75, 115, Array.Empty<int>(), new[] { 1138, 1139, 1129 }, new[] { 70, 90, 80 });
        }

        // ── Dungeons (Ameisen-Nest) ──
        private static IEnumerable<MapEntry> BuildAntHellDungeons()
        {
            yield return Dungeon(651, "ant_hell01", "Ant Hell 01", 20, 40, 20, 50, new[] { 8 }, new[] { 1119, 1120 }, new[] { 70, 60 });
            yield return Dungeon(652, "ant_hell02", "Ant Hell 02", 30, 50, 30, 60, new[] { 8 }, new[] { 1120, 1119, 1124 }, new[] { 80, 70, 90 });
            yield return Dungeon(653, "ant_hell03", "Ant Hell 03", 40, 60, 40, 70, new[] { 8 }, new[] { 1124, 1120, 1132 }, new[] { 90, 80, 20 });
        }

        // ── Dungeons (Orc) ──
        private static IEnumerable<MapEntry> BuildOrcDungeons()
        {
            yield return Dungeon(661, "orc_dun01", "Orc Dungeon 01", 30, 50, 30, 60, Array.Empty<int>(), new[] { 1096, 1148, 1149 }, new[] { 70, 60, 80 });
            yield return Dungeon(662, "orc_dun02", "Orc Dungeon 02", 40, 60, 40, 70, Array.Empty<int>(), new[] { 1148, 1149, 1096 }, new[] { 80, 70, 90 });
            yield return Dungeon(663, "orc_dun03", "Orc Dungeon 03", 50, 70, 50, 80, Array.Empty<int>(), new[] { 1149, 1096, 1148 }, new[] { 90, 80, 70 });
        }

        // ── Dungeons (Turmschildkröte) ──
        private static IEnumerable<MapEntry> BuildTurtleDungeons()
        {
            yield return Dungeon(671, "tur_dun01", "Turtle Dungeon 01", 40, 70, 40, 80, Array.Empty<int>(), new[] { 1094, 1012 }, new[] { 30, 70 });
            yield return Dungeon(672, "tur_dun02", "Turtle Dungeon 02", 50, 80, 50, 90, Array.Empty<int>(), new[] { 1094, 1012, 1091 }, new[] { 40, 50, 30 });
            yield return Dungeon(673, "tur_dun03", "Turtle Dungeon 03", 60, 90, 60, 100, Array.Empty<int>(), new[] { 1091, 1094, 1012 }, new[] { 50, 40, 30 });
            yield return Dungeon(674, "tur_dun04", "Turtle Dungeon 04", 70, 100, 70, 110, Array.Empty<int>(), new[] { 1012, 1091, 1094 }, new[] { 40, 30, 50 });
            yield return Dungeon(675, "tur_dun05", "Turtle Dungeon 05", 80, 110, 80, 120, Array.Empty<int>(), new[] { 1094, 1012, 1091 }, new[] { 30, 40, 30 });
            yield return Dungeon(676, "tur_dun06", "Turtle Dungeon 06", 90, 120, 90, 130, Array.Empty<int>(), new[] { 1091, 1094, 1012 }, new[] { 40, 30, 30 });
        }

        // ── Dungeons (Sphinx) ──
        private static IEnumerable<MapEntry> BuildSphinxDungeons()
        {
            yield return Dungeon(681, "sphinx_dun01", "Sphinx Dungeon 01", 50, 80, 50, 90, Array.Empty<int>(), new[] { 1090, 1092, 1096 }, new[] { 60, 70, 50 });
            yield return Dungeon(682, "sphinx_dun02", "Sphinx Dungeon 02", 60, 90, 60, 100, Array.Empty<int>(), new[] { 1096, 1090, 1092 }, new[] { 70, 60, 80 });
            yield return Dungeon(683, "sphinx_dun03", "Sphinx Dungeon 03", 70, 100, 70, 110, Array.Empty<int>(), new[] { 1092, 1096, 1090 }, new[] { 80, 50, 60 });
            yield return Dungeon(684, "sphinx_dun04", "Sphinx Dungeon 04", 80, 110, 80, 120, Array.Empty<int>(), new[] { 1090, 1092, 1096 }, new[] { 60, 70, 50 });
        }

        // ── Dungeons (Yuno) ──
        private static IEnumerable<MapEntry> BuildYunoDungeons()
        {
            yield return Dungeon(691, "yuno_dun01", "Yuno Dungeon 01", 70, 100, 70, 110, new[] { 10 }, new[] { 1148, 1149, 1154 }, new[] { 70, 80, 60 });
            yield return Dungeon(692, "yuno_dun02", "Yuno Dungeon 02", 80, 110, 80, 120, new[] { 10 }, new[] { 1154, 1155, 1149 }, new[] { 80, 70, 90 });
            yield return Dungeon(693, "yuno_dun03", "Yuno Dungeon 03", 90, 120, 90, 130, new[] { 10 }, new[] { 1155, 1149, 1154 }, new[] { 90, 60, 80 });
        }

        // ── Spezielle Dungeons ──
        private static IEnumerable<MapEntry> BuildSpecialDungeons()
        {
            yield return Dungeon(701, "glast_heim", "Glast Heim", 80, 120, 80, 150, Array.Empty<int>(), new[] { 1260, 1092, 1091 }, new[] { 20, 30, 50 });
            yield return Field(702, "thor_camp01", "Thor Camp", 90, 150, 90, 175, Array.Empty<int>(), new[] { 1252, 1148, 1149 }, new[] { 10, 40, 50 });
            yield return Dungeon(703, "prison", "Prison", 30, 60, 30, 70, new[] { 5 }, new[] { 1127, 1129 }, new[] { 70, 60 });
        }

        private static MapEntry Dungeon(int id, string name, string displayName, int prMin, int prMax, int reMin, int reMax, int[] warps, int[] monsterIds, int[] chances)
        {
            var spawns = new Dictionary<int, int>();
            for (int i = 0; i < monsterIds.Length && i < chances.Length; i++)
                spawns[monsterIds[i]] = chances[i];

            return new MapEntry
            {
                Id = id,
                Name = name,
                DisplayName = displayName,
                Type = MapType.Dungeon,
                PreRenewalLevelMin = prMin,
                PreRenewalLevelMax = prMax,
                RenewalLevelMin = reMin,
                RenewalLevelMax = reMax,
                IsKafra = false,
                WarpDestinations = warps.ToList(),
                MonsterSpawns = spawns,
                SupportedGameMode = GameMode.Both
            };
        }

        #endregion

        #region Abfragen

        public MapEntry? GetMap(int id) => _maps.FirstOrDefault(m => m.Id == id);

        public MapEntry? GetMap(string name) => _maps.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        /// <summary>Gibt alle Maps zurück, die für ein Base-Level im angegebenen GameMode geeignet sind.</summary>
        public List<MapEntry> GetMapsForLevelRange(int baseLevel, GameMode mode = GameMode.Both)
        {
            return mode switch
            {
                GameMode.PreRenewal => _maps
                    .Where(m => (m.SupportedGameMode == GameMode.PreRenewal || m.SupportedGameMode == GameMode.Both)
                              && m.PreRenewalLevelMin <= baseLevel && m.PreRenewalLevelMax >= baseLevel)
                    .ToList(),
                GameMode.Renewal => _maps
                    .Where(m => (m.SupportedGameMode == GameMode.Renewal || m.SupportedGameMode == GameMode.Both)
                              && m.RenewalLevelMin <= baseLevel && m.RenewalLevelMax >= baseLevel)
                    .ToList(),
                _ => _maps
                    .Where(m => m.SupportedGameMode == GameMode.Both
                              && ((m.PreRenewalLevelMin <= baseLevel && m.PreRenewalLevelMax >= baseLevel)
                               || (m.RenewalLevelMin <= baseLevel && m.RenewalLevelMax >= baseLevel)))
                    .ToList()
            };
        }

        /// <summary>Gibt alle Maps für Pre-Renewal zurück, die für ein Level geeignet sind.</summary>
        public List<MapEntry> GetMapsForLevelRangePreRenewal(int baseLevel) =>
            GetMapsForLevelRange(baseLevel, GameMode.PreRenewal);

        /// <summary>Gibt alle Maps für Renewal zurück, die für ein Level geeignet sind.</summary>
        public List<MapEntry> GetMapsForLevelRangeRenewal(int baseLevel) =>
            GetMapsForLevelRange(baseLevel, GameMode.Renewal);

        public List<MapEntry> GetKafraMaps() => _maps.Where(m => m.IsKafra).ToList();

        public List<MapEntry> GetMapsByType(MapType type) => _maps.Where(m => m.Type == type).ToList();

        /// <summary>Gibt alle Monster-IDs zurück, die auf einer Map spawnen.</summary>
        public IReadOnlyDictionary<int, int> GetMonsterSpawns(string mapName) =>
            GetMap(mapName)?.MonsterSpawns ?? new Dictionary<int, int>();

        /// <summary>Gibt alle Maps zurück, auf denen ein bestimmtes Monster spawnt.</summary>
        public List<MapEntry> GetMapsWithMonster(int monsterId) =>
            _maps.Where(m => m.MonsterSpawns.ContainsKey(monsterId)).ToList();

        #endregion
    }

    public class MapEntry
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public MapType Type { get; set; }

        // ── Pre-Renewal Level-Range ──
        public int PreRenewalLevelMin { get; set; }
        public int PreRenewalLevelMax { get; set; }

        // ── Renewal Level-Range ──
        public int RenewalLevelMin { get; set; }
        public int RenewalLevelMax { get; set; }

        public bool IsKafra { get; set; }
        public List<int> WarpDestinations { get; set; } = new();

        // ── Monster-Spawns auf dieser Map (Monster-ID → Spawn-Chance %) ──
        public Dictionary<int, int> MonsterSpawns { get; set; } = new();

        // ── Unterstützter GameMode ──
        public GameMode SupportedGameMode { get; set; } = GameMode.Both;
    }

    /// <summary>Spielmodus für Map-Daten.</summary>
    public enum GameMode
    {
        PreRenewal,
        Renewal,
        Both
    }

    public enum MapType
    {
        City,
        Field,
        Dungeon,
        Instance,
    }
}
