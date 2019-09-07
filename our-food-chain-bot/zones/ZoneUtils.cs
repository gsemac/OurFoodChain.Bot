using Discord;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain {

    public static class ZoneUtils {

        public static async Task<Zone> GetZoneAsync(long zoneId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Zones WHERE id = $zone_id")) {

                cmd.Parameters.AddWithValue("$zone_id", zoneId);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return ZoneUtils.ZoneFromDataRow(row);

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
                    return ZoneFromDataRow(row);

            }

            return null;

        }

        public static async Task<Zone[]> GetZonesAsync() {

            List<Zone> zone_list = new List<Zone>();

            using (SQLiteConnection conn = await Database.GetConnectionAsync())
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Zones"))
            using (DataTable rows = await Database.GetRowsAsync(conn, cmd))
                foreach (DataRow row in rows.Rows)
                    zone_list.Add(ZoneFromDataRow(row));

            zone_list.Sort((lhs, rhs) => new ArrayUtils.NaturalStringComparer().Compare(lhs.Name, rhs.Name));

            return zone_list.ToArray();

        }
        public static async Task<Zone[]> GetZonesAsync(ZoneType zoneType) {

            // Returns all zones of the given type.
            // If the zone type is invalid (null or has an invalid id), returns all zones.

            return (await GetZonesAsync())
                .Where(x => zoneType is null || zoneType.Id == ZoneType.NullZoneTypeId || x.ZoneTypeId == zoneType.Id)
                .ToArray();

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

        public static async Task AddZoneAsync(Zone zone) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Zones(name, type_id, description) VALUES($name, $type_id, $description)")) {

                cmd.Parameters.AddWithValue("$name", zone.Name.ToLower());
                cmd.Parameters.AddWithValue("$type_id", zone.ZoneTypeId);
                cmd.Parameters.AddWithValue("$description", zone.Description);

                await Database.ExecuteNonQuery(cmd);

            }

        }
        public static async Task UpdateZoneAsync(Zone zone) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Zones SET name = $name, type_id = $type_id, description = $description, parent_id = $parent_id WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", zone.Id);
                cmd.Parameters.AddWithValue("$parent_id", zone.ParentId);
                cmd.Parameters.AddWithValue("$name", zone.Name.ToLower());
                cmd.Parameters.AddWithValue("$type_id", zone.ZoneTypeId);
                cmd.Parameters.AddWithValue("$description", zone.Description);

                await Database.ExecuteNonQuery(cmd);

            }

        }

        public static async Task<ZoneType> GetZoneTypeAsync(string zoneTypeName) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM ZoneTypes WHERE name = $name")) {

                cmd.Parameters.AddWithValue("$name", zoneTypeName.ToLower());

                DataRow row = await Database.GetRowAsync(cmd);

                if (row != null)
                    return ZoneTypeFromDataRow(row);

            }

            return null;

        }
        public static async Task<ZoneType> GetZoneTypeAsync(long zoneTypeId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM ZoneTypes WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", zoneTypeId);

                DataRow row = await Database.GetRowAsync(cmd);

                if (row != null)
                    return ZoneTypeFromDataRow(row);

            }

            return null;

        }
        public static async Task<ZoneType> GetDefaultZoneTypeAsync(string zoneName) {

            if (Regex.Match(zoneName, @"\d+$").Success)
                return await GetZoneTypeAsync("aquatic");
            else if (Regex.Match(zoneName, "[a-z]+$").Success)
                return await GetZoneTypeAsync("terrestrial");
            else
                return new ZoneType();

        }

        public static async Task AddZoneTypeAsync(ZoneType type) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO ZoneTypes(name, icon, color, description) VALUES($name, $icon, $color, $description)")) {

                cmd.Parameters.AddWithValue("$name", type.Name.ToLower());
                cmd.Parameters.AddWithValue("$icon", type.Icon);
                cmd.Parameters.AddWithValue("$color", ColorTranslator.ToHtml(type.Color).ToLower());
                cmd.Parameters.AddWithValue("$description", type.Description);

                await Database.ExecuteNonQuery(cmd);

            }

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

        public static Zone ZoneFromDataRow(DataRow row) {

            Zone zone = new Zone {
                Id = row.Field<long>("id"),
                ParentId = row.IsNull("parent_id") ? -1 : row.Field<long>("parent_id"),
                Name = StringUtils.ToTitleCase(row.Field<string>("name")),
                Description = row.Field<string>("description"),
                Pics = row.Field<string>("pics"),
                ZoneTypeId = row.IsNull("type_id") ? ZoneType.NullZoneTypeId : row.Field<long>("type_id")
            };

            // Since the "pics" column was added later, it may be null for some zones.
            // To prevent issues witha accessing a null string, replace it with the empty string.

            if (string.IsNullOrEmpty(zone.Pics))
                zone.Pics = "";

            return zone;

        }
        public static ZoneType ZoneTypeFromDataRow(DataRow row) {

            ZoneType result = new ZoneType {
                Id = row.Field<long>("id"),
                Name = row.Field<string>("name"),
                Description = row.Field<string>("description"),
                Icon = row.Field<string>("icon")
            };

            string color_string = row.Field<string>("color");

            try {
                result.Color = ColorTranslator.FromHtml(color_string);
            }
            catch (Exception) { }

            return result;

        }
        public static bool ZoneIsValid(Zone zone) {

            if (zone is null || zone.Id <= 0)
                return false;

            return true;

        }
        public static bool ZoneTypeIsValid(ZoneType zoneType) {

            if (zoneType is null || zoneType.Id == ZoneType.NullZoneTypeId)
                return false;

            return true;

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