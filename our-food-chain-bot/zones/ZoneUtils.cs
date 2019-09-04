using Discord;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public static class ZoneUtils {

        public static async Task<Zone> GetZoneAsync(long zoneId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Zones WHERE id = $zone_id")) {

                cmd.Parameters.AddWithValue("$zone_id", zoneId);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return ZoneUtils.FromDataRow(row);

            }

            return null;

        }
        public static async Task<Zone> GetZoneAsync(string name) {

            if (string.IsNullOrEmpty(name))
                return null;

            name = FormatZoneName(name.Trim()).ToLower();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Zones WHERE name = $name")) {

                cmd.Parameters.AddWithValue("$name", name);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return FromDataRow(row);

            }

            return null;

        }

        public static async Task<Zone[]> GetZonesAsync() {

            List<Zone> zone_list = new List<Zone>();

            using (SQLiteConnection conn = await Database.GetConnectionAsync())
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Zones"))
            using (DataTable rows = await Database.GetRowsAsync(conn, cmd))
                foreach (DataRow row in rows.Rows)
                    zone_list.Add(FromDataRow(row));

            zone_list.Sort((lhs, rhs) => new ArrayUtils.NaturalStringComparer().Compare(lhs.Name, rhs.Name));

            return zone_list.ToArray();

        }

        public static async Task<ZoneListResult> GetZonesByZoneListAsync(string zoneList) {

            List<Zone> valid = new List<Zone>();
            List<string> invalid = new List<string>();

            foreach (string zone_name in _parseZoneList(zoneList)) {

                Zone zone = await GetZoneAsync(zone_name);

                if (zone is null)
                    invalid.Add(zone_name);
                else
                    valid.Add(zone);

            }

            return new ZoneListResult {
                Zones = valid.ToArray(),
                Invalid = invalid.ToArray()
            };

        }

        public static async Task<Species[]> GetSpeciesAsync(Zone zone) {

            List<Species> species = new List<Species>();

            if (zone is null || zone.Id <= 0)
                return species.ToArray();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM SpeciesZones WHERE zone_id = $zone_id)")) {

                cmd.Parameters.AddWithValue("$zone_id", zone.Id);

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        species.Add(await Species.FromDataRow(row));

            }

            return species.ToArray();

        }

        public static string FormatZoneName(string name) {

            if (StringUtils.IsNumeric(name) || name.Length == 1)
                name = "zone " + name;

            name = StringUtils.ToTitleCase(name);

            return name;

        }
        public static ZoneType ParseZoneType(string input) {

            foreach (ZoneType value in Enum.GetValues(typeof(ZoneType)))
                if (ZoneTypeToString(value).ToLower() == input.ToLower())
                    return value;

            return ZoneType.Unknown;

        }
        public static string ZoneTypeToString(ZoneType zoneType) {
            return zoneType.ToString().ToLower();
        }
        public static Color GetZoneColor(ZoneType zoneType) {

            switch (zoneType) {

                case ZoneType.Aquatic:
                    return Color.Blue;

                case ZoneType.Terrestrial:
                    return Color.DarkGreen;

                default:
                    return Color.DarkGrey;

            }

        }

        public static Zone FromDataRow(DataRow row) {

            Zone zone = new Zone {
                Id = row.Field<long>("id"),
                Name = StringUtils.ToTitleCase(row.Field<string>("name")),
                Description = row.Field<string>("description"),
                Pics = row.Field<string>("pics"),
                Type = ParseZoneType(row.Field<string>("type"))
            };

            // Since the "pics" column was added later, it may be null for some zones.
            // To prevent issues witha accessing a null string, replace it with the empty string.

            if (string.IsNullOrEmpty(zone.Pics))
                zone.Pics = "";

            return zone;

        }

        private static string[] _parseZoneList(string zoneList) {

            if (string.IsNullOrEmpty(zoneList))
                return new string[] { };

            string[] result = zoneList.Split(',', '/');

            for (int i = 0; i < result.Count(); ++i)
                result[i] = result[i].Trim().ToLower();

            return result;

        }

    }

}