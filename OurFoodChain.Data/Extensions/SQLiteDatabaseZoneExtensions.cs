using OurFoodChain.Common;
using OurFoodChain.Common.Collections;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Extensions {

    public enum GetZoneOptions {
        None = 0,
        Fast = 1
    }

    public static class SQLiteDatabaseZoneExtensions {

        // Public members

        public static async Task<IZone> GetZoneAsync(this SQLiteDatabase database, long zoneId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Zones WHERE id = $zone_id")) {

                cmd.Parameters.AddWithValue("$zone_id", zoneId);

                DataRow row = await database.GetRowAsync(cmd);

                if (row != null)
                    return await database.CreateZoneFromDataRowAsync(row);

            }

            return null;

        }
        public static async Task<IZone> GetZoneAsync(this SQLiteDatabase database, string name, GetZoneOptions options = GetZoneOptions.None) {

            if (string.IsNullOrWhiteSpace(name))
                return null;

            string fullName = ZoneUtilities.GetFullName(name.Trim()).ToLowerInvariant();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Zones WHERE name = $name OR name = $fullName OR id IN (SELECT zone_id FROM ZoneAliases WHERE alias = $name OR alias = $fullName)")) {

                cmd.Parameters.AddWithValue("$name", name.ToLowerInvariant());
                cmd.Parameters.AddWithValue("$fullName", fullName.ToLowerInvariant());

                DataRow row = await database.GetRowAsync(cmd);

                if (row != null)
                    return await database.CreateZoneFromDataRowAsync(row, options);

            }

            return null;

        }

        public static async Task<IEnumerable<IZone>> GetZonesAsync(this SQLiteDatabase database) {

            List<IZone> results = new List<IZone>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Zones"))
                foreach (DataRow row in await database.GetRowsAsync(cmd))
                    results.Add(await database.CreateZoneFromDataRowAsync(row));

            results.Sort((lhs, rhs) => new NaturalStringComparer().Compare(lhs.Name, rhs.Name));

            return results;

        }
        public static async Task<IEnumerable<IZone>> GetZonesAsync(this SQLiteDatabase database, IZoneType zoneType) {

            // Returns all zones of the given type.
            // If the zone type is invalid (null or has an invalid id), returns all zones.

            return (await GetZonesAsync(database))
                .Where(zone => zoneType is null || !zoneType.Id.HasValue || zone.TypeId == zoneType.Id);

        }

        public static async Task<IEnumerable<IZone>> GetZonesAsync(this SQLiteDatabase database, IEnumerable<string> zoneNames) {

            List<IZone> results = new List<IZone>();

            foreach (string zoneName in zoneNames) {

                IZone zone = await GetZoneAsync(database, zoneName);

                if (zone != null)
                    results.Add(zone);

            }

            return results;

        }

        public static async Task AddZoneAsync(this SQLiteDatabase database, IZone zone) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Zones(name, type, type_id, description) VALUES($name, $type, $type_id, $description)")) {

                cmd.Parameters.AddWithValue("$name", zone.Name.ToLowerInvariant());
                cmd.Parameters.AddWithValue("$type", zone.Type?.Name.ToLowerInvariant() ?? string.Empty);
                cmd.Parameters.AddWithValue("$type_id", zone.TypeId);
                cmd.Parameters.AddWithValue("$description", zone.Description);

                await database.ExecuteNonQueryAsync(cmd);

            }

        }
        public static async Task UpdateZoneAsync(this SQLiteDatabase database, IZone zone) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Zones SET name = $name, type = $type, type_id = $type_id, description = $description, parent_id = $parent_id, flags = $flags WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", zone.Id);
                cmd.Parameters.AddWithValue("$parent_id", zone.ParentId);
                cmd.Parameters.AddWithValue("$name", zone.Name.ToLowerInvariant());
                cmd.Parameters.AddWithValue("$type", zone.Type?.Name.ToLowerInvariant() ?? string.Empty);
                cmd.Parameters.AddWithValue("$type_id", zone.TypeId);
                cmd.Parameters.AddWithValue("$description", zone.Description);
                cmd.Parameters.AddWithValue("$flags", (long)zone.Flags);

                await database.ExecuteNonQueryAsync(cmd);

            }

            await database.RemoveZoneAliasesAsync(zone);
            await database.AddZoneAliasesAsync(zone);

            await database.RemoveZoneFieldsAsync(zone);
            await database.AddZoneFieldsAsync(zone);

        }

        public static async Task<IEnumerable<IZoneType>> GetZoneTypesAsync(this SQLiteDatabase database) {

            List<IZoneType> results = new List<IZoneType>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM ZoneTypes"))
                foreach (DataRow row in await database.GetRowsAsync(cmd))
                    results.Add(CreateZoneTypeFromDataRow(row));

            return results;

        }
        public static async Task<IZoneType> GetZoneTypeAsync(this SQLiteDatabase database, string zoneTypeName) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM ZoneTypes WHERE name = $name")) {

                cmd.Parameters.AddWithValue("$name", zoneTypeName.ToLowerInvariant());

                DataRow row = await database.GetRowAsync(cmd);

                if (row != null)
                    return CreateZoneTypeFromDataRow(row);

            }

            return null;

        }
        public static async Task<IZoneType> GetZoneTypeAsync(this SQLiteDatabase database, long? zoneTypeId) {

            if (zoneTypeId.HasValue) {

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM ZoneTypes WHERE id = $id")) {

                    cmd.Parameters.AddWithValue("$id", zoneTypeId);

                    DataRow row = await database.GetRowAsync(cmd);

                    if (row != null)
                        return CreateZoneTypeFromDataRow(row);

                }

            }

            return null;

        }
        public static async Task<IZoneType> GetDefaultZoneTypeAsync(this SQLiteDatabase database, string zoneName) {

            if (Regex.Match(zoneName, @"\d+$").Success)
                return await GetZoneTypeAsync(database, "aquatic");
            else if (Regex.Match(zoneName, "[a-z]+$").Success)
                return await GetZoneTypeAsync(database, "terrestrial");
            else
                return new ZoneType();

        }

        public static async Task AddZoneTypeAsync(this SQLiteDatabase database, IZoneType type) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO ZoneTypes(name, icon, color, description) VALUES($name, $icon, $color, $description)")) {

                cmd.Parameters.AddWithValue("$name", type.Name.ToLowerInvariant());
                cmd.Parameters.AddWithValue("$icon", type.Icon);
                cmd.Parameters.AddWithValue("$color", ColorTranslator.ToHtml(type.Color).ToLowerInvariant());
                cmd.Parameters.AddWithValue("$description", type.Description);

                await database.ExecuteNonQueryAsync(cmd);

            }

        }

        // Private members

        private static async Task<IZone> CreateZoneFromDataRowAsync(this SQLiteDatabase database, DataRow row, GetZoneOptions options = GetZoneOptions.None) {

            IZone zone = new Zone {
                Id = row.Field<long>("id"),
                Name = StringUtilities.ToTitleCase(row.Field<string>("name")),
                Description = row.Field<string>("description")
            };

            if (!row.IsNull("parent_id"))
                zone.ParentId = row.Field<long>("parent_id");

            if (!row.IsNull("pics") && !string.IsNullOrEmpty(row.Field<string>("pics")))
                zone.Pictures.Add(new Picture(row.Field<string>("pics")));

            if (!row.IsNull("type_id"))
                zone.TypeId = row.Field<long>("type_id");

            if (!row.IsNull("flags"))
                zone.Flags = (ZoneFlags)row.Field<long>("flags");

            if (!options.HasFlag(GetZoneOptions.Fast)) {

                List<string> aliases = new List<string>();

                // Get aliases.

                aliases.AddRange(await database.GetZoneAliasesAsync(zone));

                aliases.Remove(zone.Name.ToLowerInvariant());

                zone.Aliases = new List<string>(aliases.Distinct());

                // Get fields.

                zone.Fields = new List<IZoneField>(await database.GetZoneFieldsAsync(zone));

            }

            return zone;

        }
        private static IZoneType CreateZoneTypeFromDataRow(DataRow row) {

            IZoneType result = new ZoneType {
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

        private static async Task<IEnumerable<string>> GetZoneAliasesAsync(this SQLiteDatabase database, IZone zone) {

            List<string> results = new List<string>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT alias FROM ZoneAliases WHERE zone_id = $zone_id")) {

                cmd.Parameters.AddWithValue("$zone_id", zone.Id);

                foreach (DataRow row in await database.GetRowsAsync(cmd))
                    results.Add(row.Field<string>("alias").ToLowerInvariant());

            }

            return results.OrderBy(alias => alias);

        }
        private static async Task RemoveZoneAliasesAsync(this SQLiteDatabase database, IZone zone) {

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM ZoneAliases WHERE zone_id = $zone_id")) {

                cmd.Parameters.AddWithValue("$zone_id", zone.Id);

                await database.ExecuteNonQueryAsync(cmd);

            }

        }
        private static async Task AddZoneAliasesAsync(this SQLiteDatabase database, IZone zone) {

            foreach (string alias in zone.Aliases.Select(alias => ZoneUtilities.GetFullName(alias).ToLowerInvariant()).Distinct()) {

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO ZoneAliases(zone_id, alias) VALUES($zone_id, $alias)")) {

                    cmd.Parameters.AddWithValue("$zone_id", zone.Id);
                    cmd.Parameters.AddWithValue("$alias", alias.ToLowerInvariant().SafeTrim());

                    await database.ExecuteNonQueryAsync(cmd);

                }

            }

        }

        private static async Task<IEnumerable<IZoneField>> GetZoneFieldsAsync(this SQLiteDatabase database, IZone zone) {

            List<IZoneField> results = new List<IZoneField>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM ZoneFields WHERE zone_id = $zone_id")) {

                cmd.Parameters.AddWithValue("$zone_id", zone.Id);

                foreach (DataRow row in await database.GetRowsAsync(cmd))
                    results.Add(new ZoneField(row.Field<string>("name"), row.Field<string>("value")));

            }

            return results.OrderBy(field => field.Name);

        }
        private static async Task RemoveZoneFieldsAsync(this SQLiteDatabase database, IZone zone) {

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM ZoneFields WHERE zone_id = $zone_id")) {

                cmd.Parameters.AddWithValue("$zone_id", zone.Id);

                await database.ExecuteNonQueryAsync(cmd);

            }

        }
        private static async Task AddZoneFieldsAsync(this SQLiteDatabase database, IZone zone) {

            foreach (IZoneField field in zone.Fields) {

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO ZoneFields(zone_id, name, value) VALUES($zone_id, $name, $value)")) {

                    cmd.Parameters.AddWithValue("$zone_id", zone.Id);
                    cmd.Parameters.AddWithValue("$name", field.Name.ToLowerInvariant().SafeTrim());
                    cmd.Parameters.AddWithValue("$value", field.Value.ToLowerInvariant().SafeTrim());

                    await database.ExecuteNonQueryAsync(cmd);

                }

            }

        }

    }

}