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
                    return Zone.FromDataRow(row);

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
                    return Zone.FromDataRow(row);

            }

            return null;

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

        public static string FormatZoneName(string name) {

            if (StringUtils.IsNumeric(name) || name.Length == 1)
                name = "zone " + name;

            name = StringUtils.ToTitleCase(name);

            return name;

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