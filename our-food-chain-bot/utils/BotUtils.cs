using Discord;
using Discord.Commands;
using Discord.WebSocket;
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

    enum TwoPartCommandWaitParamsType {
        Description
    }

    class TwoPartCommandWaitParams {

        public TwoPartCommandWaitParams(ICommandContext context) {

            this.context = context;

        }

        public TwoPartCommandWaitParamsType type;
        public ICommandContext context;
        public string[] args;
        public DateTime timestamp;
        public ulong channelId = 0;

    }

    public enum ZoneType {
        Unknown,
        Aquatic,
        Terrestrial
    }

    public class Zone {

        public long id;
        public string name;
        public string description;
        public ZoneType type;
        public string pics;

        public static Zone FromDataRow(DataRow row) {

            Zone zone = new Zone();
            zone.id = row.Field<long>("id");
            zone.name = StringUtils.ToTitleCase(row.Field<string>("name"));
            zone.description = row.Field<string>("description");
            zone.pics = row.Field<string>("pics");

            // Since the "pics" column was added later, it may be null for some zones.
            // To prevent issues witha accessing a null string, replace it with the empty string.

            if (string.IsNullOrEmpty(zone.pics))
                zone.pics = "";

            switch (row.Field<string>("type")) {
                case "aquatic":
                    zone.type = ZoneType.Aquatic;
                    break;
                case "terrestrial":
                    zone.type = ZoneType.Terrestrial;
                    break;
                default:
                    zone.type = ZoneType.Unknown;
                    break;
            }

            return zone;

        }
        public static string[] ParseZoneList(string zoneList) {

            if (string.IsNullOrEmpty(zoneList))
                return new string[] { };

            string[] result = zoneList.Split(',', '/');

            for (int i = 0; i < result.Count(); ++i)
                result[i] = result[i].Trim().ToLower();

            return result;

        }

        public string GetShortDescription() {
            return GetShortDescription(GetDescriptionOrDefault());
        }
        public string GetDescriptionOrDefault() {

            if (string.IsNullOrEmpty(description))
                return BotUtils.DEFAULT_ZONE_DESCRIPTION;

            return description;

        }
        public string GetShortName() {

            return Regex.Replace(name, "^zone\\s+", "", RegexOptions.IgnoreCase);

        }
        public string GetFullName() {
            return GetFullName(name);
        }

        public static string GetShortDescription(string description) {
            return Regex.Match(description, "^[a-zA-Z0-9 ,;:\\-\"’']+(?:\\.+|[!\\?])").Value;
        }
        public static string GetFullName(string name) {

            if (StringUtils.IsNumeric(name) || name.Length == 1)
                name = "zone " + name;

            name = StringUtils.ToTitleCase(name);

            return name;

        }

    }

    class Family {

        public long id;
        public long order_id;
        public string name;
        public string description;

        public Family() {

            id = -1;
            order_id = 0;

        }

        public string GetDescriptionOrDefault() {

            if (string.IsNullOrEmpty(description))
                return BotUtils.DEFAULT_DESCRIPTION;

            return description;

        }

        public static Family FromDataRow(DataRow row) {

            Family result = new Family {
                id = row.Field<long>("id"),
                name = row.Field<string>("name"),
                description = row.Field<string>("description")
            };

            result.order_id = (row["order_id"] == DBNull.Value) ? 0 : row.Field<long>("order_id");

            return result;

        }

    }

    class Genus {

        public long id;
        public long family_id;
        public string name;
        public string description;
        public string pics;

        public static Genus FromDataRow(DataRow row) {

            Genus result = new Genus {
                id = row.Field<long>("id"),
                name = row.Field<string>("name"),
                description = row.Field<string>("description"),
                pics = row.Field<string>("pics")
            };

            result.family_id = (row["family_id"] == DBNull.Value) ? 0 : row.Field<long>("family_id");

            return result;

        }

    }

    public class Species :
        IComparable<Species> {

        public long id;
        public long genusId;
        public string name;
        public string description;
        public string owner;
        public long user_id;
        public long timestamp;
        public string pics;
        public string commonName;

        // fields that stored directly in the table
        public string genus;
        public bool isExtinct;

        public static async Task<Species> FromDataRow(DataRow row) {

            long genus_id = row.Field<long>("genus_id");

            Genus genus_info = await BotUtils.GetGenusFromDb(genus_id);

            Species species = new Species {
                id = row.Field<long>("id"),
                genusId = row.Field<long>("genus_id"),
                name = row.Field<string>("name"),
                genus = genus_info.name,
                description = row.Field<string>("description"),
                owner = row.Field<string>("owner"),
                timestamp = (long)row.Field<decimal>("timestamp"),
                commonName = row.Field<string>("common_name"),
                pics = row.Field<string>("pics")
            };

            species.user_id = row.IsNull("user_id") ? -1 : row.Field<long>("user_id");
            species.isExtinct = false;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Extinctions WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);

                if (!(await Database.GetRowAsync(cmd) is null))
                    species.isExtinct = true;

            }

            return species;

        }

        public string GetShortName() {

            return BotUtils.GenerateSpeciesName(this);

        }
        public string GetFullName() {

            return string.Format("{0} {1}", StringUtils.ToTitleCase(genus), name);

        }
        public string GetTimeStampAsDateString() {
            return BotUtils.GetTimeStampAsDateString(timestamp);
        }
        public string GetDescriptionOrDefault() {

            if (string.IsNullOrEmpty(description))
                return BotUtils.DEFAULT_SPECIES_DESCRIPTION;

            return description;

        }
        public async Task<string> GetOwnerOrDefault(ICommandContext context) {

            string result = owner;

            if (user_id > 0) {

                IUser user = await context.Guild.GetUserAsync((ulong)user_id);

                if (!(user is null))
                    result = user.Username;

            }

            if (string.IsNullOrEmpty(result))
                result = "?";

            return result;

        }

        public int CompareTo(Species other) {

            return GetShortName().CompareTo(other.GetShortName());

        }
    }

    public class Role {

        public long id;
        public string name;
        public string description;

        public string notes;

        public string GetDescriptionOrDefault() {

            if (string.IsNullOrEmpty(description))
                return BotUtils.DEFAULT_DESCRIPTION;

            return description;

        }

        public static Role FromDataRow(DataRow row) {

            Role role = new Role();
            role.id = row.Field<long>("id");
            role.name = row.Field<string>("name");
            role.description = row.Field<string>("description");

            return role;

        }

    }

    class BotUtils {

        public const string DEFAULT_SPECIES_DESCRIPTION = "No description provided.";
        public const string DEFAULT_GENUS_DESCRIPTION = "No description provided.";
        public const string DEFAULT_ZONE_DESCRIPTION = "No description provided.";
        public const string DEFAULT_DESCRIPTION = "No description provided.";
        private static Random RANDOM = new Random();

        public static Dictionary<ulong, TwoPartCommandWaitParams> TWO_PART_COMMAND_WAIT_PARAMS = new Dictionary<ulong, TwoPartCommandWaitParams>();

        public static async Task<bool> SpeciesExistsInDb(string genus, string species) {

            return (await GetSpeciesFromDb(genus, species)).Count() > 0;

        }
        public static async Task<Zone[]> GetZonesFromDb() {

            List<Zone> zones = new List<Zone>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Zones;"))
            using (DataTable rows = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in rows.Rows)
                    zones.Add(Zone.FromDataRow(row));

            return zones.ToArray();

        }
        public static async Task<Zone[]> GetZonesFromDb(long speciesId) {

            List<Zone> zones = new List<Zone>();

            using (SQLiteConnection conn = await Database.GetConnectionAsync())
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM SpeciesZones WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", speciesId);

                using (DataTable rows = await Database.GetRowsAsync(conn, cmd))
                    foreach (DataRow row in rows.Rows) {

                        Zone zone = await GetZoneFromDb(row.Field<long>("zone_id"));

                        if (zone is null)
                            continue;

                        zones.Add(zone);

                    }

            }

            return zones.ToArray();

        }
        public static async Task<Species[]> GetSpeciesFromDb(string genus, string species) {

            // If the genus is empty but the species contains a period, assume everything to the left of the period is the genus.
            // This allows us to process inputs like "C.aspersum" where the user forgot to put a space between the genus and species names.

            if (string.IsNullOrEmpty(genus) && species.Contains('.')) {

                int split_index = species.IndexOf('.');

                genus = species.Substring(0, split_index);
                species = species.Substring(split_index, species.Length - split_index);

            }

            // Strip all periods from the genus/species names.
            // This allows us to process inputs like "c aspersum" and "c. asperum" in the same way.

            if (!string.IsNullOrEmpty(genus))
                genus = genus.Trim('.');

            if (!string.IsNullOrEmpty(species))
                species = species.Trim('.');

            // Convert both names to lowercase, since this is how they're stored in the database.

            genus = genus.ToLower();
            species = species.ToLower();

            Genus genus_info = null;
            List<Species> matches = new List<Species>();

            bool genus_is_abbreviated = false;
            string sql_command_str = "SELECT * FROM Species WHERE genus_id=$genus_id AND name=$species;";

            if (string.IsNullOrEmpty(genus)) {

                // If the genus is empty, it's possible that the user either omitted the genus or just past in the species name.
                // In this case, we'll allow for the species to be looked up via its common name as well.

                sql_command_str = "SELECT * FROM Species WHERE name=$species OR common_name=$species;";

            }
            else if (Regex.IsMatch(genus, @"^[a-zA-Z]\.?$")) {

                // If the genus is abbreviated (e.g. "Cornu" -> "C.") but not null, we'll look for matching species and determine the genus afterwards.
                // Notice that this case does not allow for looking up the species by common name, which should not occur when the genus is included.

                sql_command_str = "SELECT * FROM Species WHERE name=$species;";
                genus_is_abbreviated = true;

            }
            else {

                // Since the genus is not abbreviated, we can get genus information immediately.
                genus_info = await GetGenusFromDb(genus);

                // If the genus doesn't exist, then the species doesn't exist.

                if (genus_info is null)
                    return matches.ToArray();

            }

            using (SQLiteConnection conn = await Database.GetConnectionAsync())
            using (SQLiteCommand cmd = new SQLiteCommand(sql_command_str)) {

                cmd.Parameters.AddWithValue("$species", species);

                if (!genus_is_abbreviated && !(genus_info is null))
                    cmd.Parameters.AddWithValue("$genus_id", genus_info.id);

                using (DataTable rows = await Database.GetRowsAsync(conn, cmd))
                    foreach (DataRow row in rows.Rows) {

                        if (!string.IsNullOrEmpty(genus) && genus_is_abbreviated) {

                            // If the user passed in an abbreviated genus, get the genus for this species and see if matches.
                            // If so, this species will be added to the list of candidates.

                            genus_info = await GetGenusFromDb(row.Field<long>("genus_id"));

                            if (genus_info != null && !genus_info.name.StartsWith(genus[0].ToString()))
                                continue;

                        }

                        matches.Add(await Species.FromDataRow(row));

                    }

            }

            return matches.ToArray();

        }
        public static async Task<Species[]> GetSpeciesFromDbByRole(Role role) {

            // Return all species with the given role.

            List<Species> species = new List<Species>();

            if (role is null || role.id <= 0)
                return species.ToArray();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM SpeciesRoles WHERE role_id=$role_id) ORDER BY name ASC;")) {

                cmd.Parameters.AddWithValue("$role_id", role.id);

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        species.Add(await Species.FromDataRow(row));

            }

            return species.ToArray();

        }
        public static async Task<Species[]> GetSpeciesFromDbByZone(Zone zone, bool extantOnly = true) {

            // Return all species in the given zone.

            List<Species> species = new List<Species>();

            if (zone is null || zone.id <= 0)
                return species.ToArray();

            string query_all = "SELECT * FROM Species WHERE id IN (SELECT species_id FROM SpeciesZones WHERE zone_id=$zone_id) ORDER BY name ASC;";
            string query_extant = "SELECT * FROM Species WHERE id IN (SELECT species_id FROM SpeciesZones WHERE zone_id=$zone_id) AND id NOT IN (SELECT species_id FROM Extinctions) ORDER BY name ASC;";

            using (SQLiteCommand cmd = new SQLiteCommand(extantOnly ? query_extant : query_all)) {

                cmd.Parameters.AddWithValue("$zone_id", zone.id);

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        species.Add(await Species.FromDataRow(row));

            }

            return species.ToArray();

        }

        public static async Task AddGenusToDb(string genus) {

            Genus genus_info = new Genus();
            genus_info.name = genus;

            await AddGenusToDb(genus_info);

        }
        public static async Task AddGenusToDb(Genus genus) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Genus(name, description) VALUES($name, $description);")) {

                cmd.Parameters.AddWithValue("$name", genus.name.ToLower());
                cmd.Parameters.AddWithValue("$description", genus.description);

                await Database.ExecuteNonQuery(cmd);

            }

        }
        public static async Task<Genus> GetGenusFromDb(string genus) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Genus WHERE name=$genus;")) {

                cmd.Parameters.AddWithValue("$genus", genus.ToLower());

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return Genus.FromDataRow(row);

            }

            return null;

        }
        public static async Task<Genus> GetGenusFromDb(long genusId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Genus WHERE id=$genus_id;")) {

                cmd.Parameters.AddWithValue("$genus_id", genusId);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return Genus.FromDataRow(row);

            }

            return null;

        }
        public static async Task<Genus[]> GetGeneraFromDb(Family family) {

            List<Genus> genera = new List<Genus>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Genus WHERE family_id=$family_id ORDER BY name ASC;")) {

                cmd.Parameters.AddWithValue("$family_id", family.id);

                using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                    foreach (DataRow row in rows.Rows)
                        genera.Add(Genus.FromDataRow(row));

                }

            }

            return genera.ToArray();

        }
        public static async Task UpdateGenusInDb(Genus genus) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Genus SET name=$name, description=$description, family_id=$family_id WHERE id=$genus_id;")) {

                cmd.Parameters.AddWithValue("$name", genus.name);
                cmd.Parameters.AddWithValue("$description", genus.description);
                cmd.Parameters.AddWithValue("$family_id", genus.family_id);
                cmd.Parameters.AddWithValue("$genus_id", genus.id);

                await Database.ExecuteNonQuery(cmd);

            }

        }
        public static async Task<Species> GetSpeciesFromDb(long speciesId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", speciesId);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return await Species.FromDataRow(row);

            }

            return null;

        }
        public static async Task<Species[]> GetAncestorsFromDb(long speciesId) {

            List<Species> ancestors = new List<Species>();

            while (true) {

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT ancestor_id FROM Ancestors WHERE species_id=$species_id;")) {

                    cmd.Parameters.AddWithValue("$species_id", speciesId);

                    DataRow row = await Database.GetRowAsync(cmd);

                    if (row is null)
                        break;

                    speciesId = row.Field<long>("ancestor_id");

                    Species ancestor = await GetSpeciesFromDb(speciesId);

                    ancestors.Add(ancestor);

                }

            }

            ancestors.Reverse();

            return ancestors.ToArray();

        }

        public static async Task<Zone> GetZoneFromDb(long zoneId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Zones WHERE id=$zone_id;")) {

                cmd.Parameters.AddWithValue("$zone_id", zoneId);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return Zone.FromDataRow(row);

            }

            return null;

        }
        public static async Task<Zone> GetZoneFromDb(string zoneName) {

            if (string.IsNullOrEmpty(zoneName))
                return null;

            zoneName = Zone.GetFullName(zoneName.Trim()).ToLower();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Zones WHERE name=$name;")) {

                cmd.Parameters.AddWithValue("$name", zoneName);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return Zone.FromDataRow(row);

            }

            return null;

        }
        public static async Task<Role> GetRoleFromDb(long roleId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Roles WHERE id=$role_id;")) {

                cmd.Parameters.AddWithValue("$role_id", roleId);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return Role.FromDataRow(row);

            }

            return null;


        }

        public static async Task<Role[]> GetRolesFromDb() {

            List<Role> roles = new List<Role>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Roles;"))
            using (DataTable rows = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in rows.Rows)
                    roles.Add(Role.FromDataRow(row));

            // Sort roles by name in alphabetical order.
            roles.Sort((lhs, rhs) => lhs.name.CompareTo(rhs.name));

            return roles.ToArray();

        }
        public static async Task<Role[]> GetRolesFromDbBySpecies(long speciesId) {

            // Return all roles assigned to the given species.

            List<Role> roles = new List<Role>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Roles WHERE id IN (SELECT role_id FROM SpeciesRoles WHERE species_id=$species_id) ORDER BY name ASC;")) {

                cmd.Parameters.AddWithValue("$species_id", speciesId);

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        roles.Add(Role.FromDataRow(row));

            }

            // Get role notes.
            // #todo Get the roles and notes using a single query.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM SpeciesRoles WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", speciesId);

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows) {

                        long role_id = row.Field<long>("role_id");
                        string notes = row.Field<string>("notes");

                        foreach (Role role in roles)
                            if (role.id == role_id) {
                                role.notes = notes;
                                break;
                            }

                    }

            }

            return roles.ToArray();

        }
        public static async Task<Role[]> GetRolesFromDbBySpecies(Species species) {

            return await GetRolesFromDbBySpecies(species.id);

        }
        public static async Task<Role> GetRoleFromDb(string roleName) {

            // Allow for querying using the plural of the role (e.g., "producers").
            string role_name_plural = roleName.TrimEnd('s');

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Roles WHERE name=$name OR name=$plural;")) {

                cmd.Parameters.AddWithValue("$name", roleName.ToLower());
                cmd.Parameters.AddWithValue("$plural", role_name_plural.ToLower());

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return Role.FromDataRow(row);

            }

            return null;

        }

        public static async Task<Taxon[]> GetTaxaFromDb(string name, TaxonType type) {

            List<Taxon> taxa = new List<Taxon>();
            string table_name = Taxon.TypeToDatabaseTableName(type);

            if (string.IsNullOrEmpty(table_name))
                return null;

            using (SQLiteCommand cmd = new SQLiteCommand(string.Format("SELECT * FROM {0} WHERE name=$name;", table_name))) {

                cmd.Parameters.AddWithValue("$name", name.ToLower());

                using (DataTable table = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in table.Rows)
                        taxa.Add(Taxon.FromDataRow(row, type));

            }

            return taxa.ToArray();

        }
        public static async Task<Taxon> GetTaxonFromDb(long id, TaxonType type) {

            string table_name = Taxon.TypeToDatabaseTableName(type);

            if (string.IsNullOrEmpty(table_name))
                return null;

            Taxon taxon_info = null;

            using (SQLiteCommand cmd = new SQLiteCommand(string.Format("SELECT * FROM {0} WHERE id=$id;", table_name))) {

                cmd.Parameters.AddWithValue("$id", id);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    taxon_info = Taxon.FromDataRow(row, type);

            }

            return taxon_info;

        }
        public static async Task<Taxon> GetTaxonFromDb(string name) {

            foreach (TaxonType type in new TaxonType[] { TaxonType.Domain, TaxonType.Kingdom, TaxonType.Phylum, TaxonType.Class, TaxonType.Order, TaxonType.Family, TaxonType.Genus, TaxonType.Species }) {

                Taxon[] taxa = await GetTaxaFromDb(name, type);

                if (taxa.Count() > 0)
                    return taxa[0];

            }

            return null;

        }
        public static async Task<Taxon> GetTaxonFromDb(string name, TaxonType type) {

            Taxon[] taxa = await GetTaxaFromDb(name, type);

            if (taxa.Count() > 0)
                return taxa[0];

            return null;

        }
        public static async Task<Taxon[]> GetTaxaFromDb(TaxonType type) {

            List<Taxon> result = new List<Taxon>();
            string table_name = Taxon.TypeToDatabaseTableName(type);

            if (string.IsNullOrEmpty(table_name))
                return result.ToArray();

            string query = "SELECT * FROM {0};";

            using (SQLiteCommand cmd = new SQLiteCommand(string.Format(query, table_name))) {

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        result.Add(Taxon.FromDataRow(row, type));

            }

            // Sort taxa alphabetically by name.
            result.Sort((lhs, rhs) => lhs.name.CompareTo(rhs.name));

            return result.ToArray();

        }
        public static async Task<Taxon[]> GetTaxaFromDb(string name) {

            // Return all taxa that have the given name.

            List<Taxon> result = new List<Taxon>();

            foreach (TaxonType type in new TaxonType[] { TaxonType.Domain, TaxonType.Kingdom, TaxonType.Phylum, TaxonType.Class, TaxonType.Order, TaxonType.Family, TaxonType.Genus, TaxonType.Species })
                result.AddRange(await GetTaxaFromDb(name, type));

            return result.ToArray();

        }
        public static async Task<Taxon[]> GetSubTaxaFromDb(Taxon parentTaxon) {

            List<Taxon> result = new List<Taxon>();

            string table_name = Taxon.TypeToDatabaseTableName(parentTaxon.GetChildType());
            string parent_column_name = Taxon.TypeToDatabaseColumnName(parentTaxon.type);

            if (string.IsNullOrEmpty(table_name))
                return result.ToArray();

            string query = "SELECT * FROM {0} WHERE {1}=$parent_id;";

            using (SQLiteCommand cmd = new SQLiteCommand(string.Format(query, table_name, parent_column_name))) {

                cmd.Parameters.AddWithValue("$parent_id", parentTaxon.id);

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        result.Add(Taxon.FromDataRow(row, parentTaxon.GetChildType()));

            }

            // Sort taxa alphabetically by name.
            result.Sort((lhs, rhs) => lhs.name.CompareTo(rhs.name));

            return result.ToArray();

        }
        public static async Task UpdateTaxonInDb(Taxon taxon, TaxonType type) {

            string table_name = Taxon.TypeToDatabaseTableName(type);

            if (string.IsNullOrEmpty(table_name))
                return;

            string parent_column_name = Taxon.TypeToDatabaseColumnName(Taxon.TypeToParentType(type));
            string update_parent_column_name_str = string.Empty;

            if (!string.IsNullOrEmpty(parent_column_name))
                update_parent_column_name_str = string.Format(", {0}=$parent_id", parent_column_name);


            using (SQLiteCommand cmd = new SQLiteCommand(string.Format(
                "UPDATE {0} SET name=$name, description=$description, pics=$pics{1}, common_name=$common_name WHERE id=$id;",
                table_name,
                update_parent_column_name_str))) {

                cmd.Parameters.AddWithValue("$name", taxon.name);
                cmd.Parameters.AddWithValue("$description", taxon.description);
                cmd.Parameters.AddWithValue("$pics", taxon.pics);
                cmd.Parameters.AddWithValue("$id", taxon.id);

                // Because this field was added in a database update, it's possible for it to be null rather than the empty string.
                cmd.Parameters.AddWithValue("$common_name", string.IsNullOrEmpty(taxon.common_name) ? "" : taxon.common_name.ToLower());

                if (!string.IsNullOrEmpty(parent_column_name) && taxon.parent_id != -1) {
                    cmd.Parameters.AddWithValue("$parent_column_name", parent_column_name);
                    cmd.Parameters.AddWithValue("$parent_id", taxon.parent_id);
                }

                await Database.ExecuteNonQuery(cmd);

            }

        }
        public static async Task AddTaxonToDb(Taxon taxon, TaxonType type) {

            string table_name = Taxon.TypeToDatabaseTableName(type);

            if (string.IsNullOrEmpty(table_name))
                return;

            string parent_column_name = Taxon.TypeToDatabaseColumnName(Taxon.TypeToParentType(type));
            string query;

            if (!string.IsNullOrEmpty(parent_column_name) && taxon.parent_id > 0)
                query = string.Format("INSERT INTO {0}(name, description, pics, {1}) VALUES($name, $description, $pics, $parent_id);", table_name, parent_column_name);
            else
                query = string.Format("INSERT INTO {0}(name, description, pics) VALUES($name, $description, $pics);", table_name);

            using (SQLiteCommand cmd = new SQLiteCommand(query)) {

                cmd.Parameters.AddWithValue("$name", taxon.name.ToLower());
                cmd.Parameters.AddWithValue("$description", taxon.description);
                cmd.Parameters.AddWithValue("$pics", taxon.pics);

                if (!string.IsNullOrEmpty(parent_column_name) && taxon.parent_id > 0) {
                    cmd.Parameters.AddWithValue("$parent_column", parent_column_name);
                    cmd.Parameters.AddWithValue("$parent_id", taxon.parent_id);
                }

                await Database.ExecuteNonQuery(cmd);

            }

        }
        public static async Task<Species[]> GetSpeciesInTaxonFromDb(Taxon taxon) {

            List<Species> species = new List<Species>();

            if (taxon.type == TaxonType.Species) {

                // Return all species with the same name as the taxon.

                species.AddRange(await GetSpeciesFromDb("", taxon.name));

            }
            else if (taxon.type == TaxonType.Genus) {

                // Return all species within this genus (rather than recursively calling this function for each species).

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE genus_id=$genus_id;")) {

                    cmd.Parameters.AddWithValue("$genus_id", taxon.id);

                    using (DataTable table = await Database.GetRowsAsync(cmd))
                        foreach (DataRow row in table.Rows)
                            species.Add(await Species.FromDataRow(row));

                }

            }
            else {

                // Get all subtaxa and call this function recursively to get the species from each of them.

                Taxon[] subtaxa = await GetSubTaxaFromDb(taxon);

                foreach (Taxon t in subtaxa)
                    species.AddRange(await GetSpeciesInTaxonFromDb(t));

            }

            return species.ToArray();

        }
        public static async Task<long> CountSpeciesInTaxonFromDb(Taxon taxon) {

            long species_count = 0;

            if (taxon.type == TaxonType.Species) {

                // If a species was passed in, count it as a single species.
                species_count += 1;

            }
            else if (taxon.type == TaxonType.Genus) {

                // Count all species within this genus.

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Species WHERE genus_id=$genus_id;")) {

                    cmd.Parameters.AddWithValue("$genus_id", taxon.id);

                    species_count += await Database.GetScalar<long>(cmd);

                }

            }
            else {

                // Get all subtaxa and call this function recursively to get the species from each of them.

                Taxon[] subtaxa = await GetSubTaxaFromDb(taxon);

                foreach (Taxon t in subtaxa)
                    species_count += await CountSpeciesInTaxonFromDb(t);

            }

            return species_count;

        }
        public static async Task<Species[]> GetSpeciesInTaxonFromDb(string taxonName) {

            List<Species> species = new List<Species>();
            Taxon taxon = await GetTaxonFromDb(taxonName);

            if (!(taxon is null))
                species.AddRange(await GetSpeciesInTaxonFromDb(taxon));

            return species.ToArray();

        }
        public static async Task<TaxonSet> GetFullTaxaFromDb(Species sp) {

            TaxonSet set = new TaxonSet();

            set.Genus = await GetTaxonFromDb(sp.genusId, TaxonType.Genus);

            if (!(set.Genus is null))
                set.Family = await GetTaxonFromDb(set.Genus.parent_id, TaxonType.Family);

            if (!(set.Family is null))
                set.Order = await GetTaxonFromDb(set.Family.parent_id, TaxonType.Order);

            if (!(set.Order is null))
                set.Class = await GetTaxonFromDb(set.Order.parent_id, TaxonType.Class);

            if (!(set.Class is null))
                set.Phylum = await GetTaxonFromDb(set.Class.parent_id, TaxonType.Phylum);

            if (!(set.Phylum is null))
                set.Kingdom = await GetTaxonFromDb(set.Phylum.parent_id, TaxonType.Kingdom);

            if (!(set.Kingdom is null))
                set.Domain = await GetTaxonFromDb(set.Kingdom.parent_id, TaxonType.Domain);

            return set;

        }

        public static async Task AddRoleToDb(Role role) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Roles(name, description) VALUES($name, $description);")) {

                cmd.Parameters.AddWithValue("$name", role.name.ToLower());
                cmd.Parameters.AddWithValue("$description", role.description);

                await Database.ExecuteNonQuery(cmd);

            }

        }

        public static async Task<Gallery> GetGalleryFromDb(string name) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gallery WHERE name=$name;")) {

                cmd.Parameters.AddWithValue("$name", name);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return Gallery.FromDataRow(row);

            }

            return null;

        }
        public static async Task<Picture[]> GetPicsFromDb(Gallery gallery) {

            List<Picture> pictures = new List<Picture>();

            if (!(gallery is null) && gallery.id > 0) {

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Picture WHERE gallery_id=$gallery_id;")) {

                    cmd.Parameters.AddWithValue("$gallery_id", gallery.id);

                    using (DataTable rows = await Database.GetRowsAsync(cmd))
                        foreach (DataRow row in rows.Rows)
                            pictures.Add(Picture.FromDataRow(row));

                }

            }

            return pictures.ToArray();

        }
        public static async Task<Picture> GetPicFromDb(Gallery gallery, string name) {

            if (!(gallery is null) && gallery.id > 0) {

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Picture WHERE gallery_id=$gallery_id AND name=$name;")) {

                    cmd.Parameters.AddWithValue("$gallery_id", gallery.id);
                    cmd.Parameters.AddWithValue("$name", name);

                    DataRow row = await Database.GetRowAsync(cmd);

                    if (!(row is null))
                        return Picture.FromDataRow(row);

                }

            }

            return null;

        }

        public static async Task<Period> GetPeriodFromDb(string name) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Period WHERE name = $name;")) {

                cmd.Parameters.AddWithValue("$name", name.ToLower());

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return Period.FromDataRow(row);

            }

            return null;

        }
        public static async Task<Period[]> GetPeriodsFromDb() {

            List<Period> results = new List<Period>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Period;"))
            using (DataTable table = await Database.GetRowsAsync(cmd)) {

                foreach (DataRow row in table.Rows)
                    results.Add(Period.FromDataRow(row));
            }

            // Have more recent periods listed first.
            results.Sort((lhs, rhs) => rhs.GetStartTimestamp().CompareTo(lhs.GetStartTimestamp()));

            return results.ToArray();

        }

        public static string GenerateSpeciesName(string genus, string species) {

            return string.Format("{0}. {1}", genus.ToUpper()[0], species);

        }
        public static string GenerateSpeciesName(Species species) {

            return GenerateSpeciesName(species.genus, species.name);

        }
        public static string GetTimeStampAsDateString(long ts) {

            return DateTimeOffset.FromUnixTimeSeconds(ts).Date.ToUniversalTime().ToShortDateString();

        }
        public static string GetTimeStampAsDateString(long ts, string format) {

            return DateTimeOffset.FromUnixTimeSeconds(ts).Date.ToUniversalTime().ToString(format);

        }
        public static string Strikeout(string str) {

            return string.Format("~~{0}~~", str);

        }
        public static async Task UpdateSpeciesDescription(Species species, string description) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET description=$description WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);
                cmd.Parameters.AddWithValue("$description", description);

                await Database.ExecuteNonQuery(cmd);

            }

        }
        public static async Task UpdateSpeciesDescription(string genus, string species, string description) {

            Species[] sp_list = await GetSpeciesFromDb(genus, species);

            if (sp_list.Count() <= 0)
                return;

            await UpdateSpeciesDescription(sp_list[0], description);

        }
        public static async Task HandleTwoPartCommandResponse(SocketMessage message) {

            if (!TWO_PART_COMMAND_WAIT_PARAMS.ContainsKey(message.Author.Id))
                return;

            TwoPartCommandWaitParams p = TWO_PART_COMMAND_WAIT_PARAMS[message.Author.Id];

            // The second half of a two-part message must be sent in the same channel as the first part.
            if (message.Channel.Id != p.channelId)
                return;

            switch (p.type) {

                case TwoPartCommandWaitParamsType.Description:

                    if (message.Content.Equals("cancel", StringComparison.OrdinalIgnoreCase)) {

                        await message.Channel.SendMessageAsync("Description update canceled.");

                    }
                    else {

                        await UpdateSpeciesDescription(p.args[0], p.args[1], message.Content);
                        await ReplyAsync_Success(p.context, "Description updated successfully.");

                    }

                    break;

            }

            TWO_PART_COMMAND_WAIT_PARAMS.Remove(message.Author.Id);

        }

        public static async Task<Species> ReplyAsync_FindSpecies(ICommandContext context, string genus, string species) {

            Species[] sp_list = await GetSpeciesFromDb(genus, species);

            if (sp_list.Count() <= 0) {

                // The species could not be found. Check all species to find a suggestion.
                await ReplyAsync_SpeciesSuggestions(context, genus, species);

                return null;

            }
            else if (sp_list.Count() > 1) {

                await ReplyAsync_MatchingSpecies(context, sp_list);
                return null;

            }

            return sp_list[0];

        }
        public static async Task ReplyAsync_SpeciesSuggestions(ICommandContext context, string genus, string species) {

            List<Species> sp_list = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species;")) {

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        sp_list.Add(await Species.FromDataRow(row));

            }

            int min_dist = int.MaxValue;
            string suggestion = string.Empty;

            foreach (Species sp in sp_list) {

                int dist = LevenshteinDistance.Compute(species, sp.name);

                if (dist < min_dist) {
                    min_dist = dist;
                    suggestion = sp.GetShortName();
                }

            }

            await ReplyAsync_NoSuchSpeciesExists(context, suggestion);

        }
        public static async Task ReplyAsync_NoSuchSpeciesExists(ICommandContext context, string suggestion = "") {

            StringBuilder sb = new StringBuilder();

            sb.Append("No such species exists.");

            if (!string.IsNullOrEmpty(suggestion))
                sb.Append(string.Format(" Did you mean **{0}**?", suggestion));

            await context.Channel.SendMessageAsync(sb.ToString());

        }
        public static async Task ReplyAsync_MatchingSpecies(ICommandContext context, Species[] speciesList) {

            EmbedBuilder embed = new EmbedBuilder();
            List<string> lines = new List<string>();

            embed.WithTitle("Matching species");

            foreach (Species sp in speciesList)
                lines.Add(GenerateSpeciesName(sp));

            embed.WithDescription(string.Join(Environment.NewLine, lines));

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task<bool> ReplyAsync_ValidateSpecies(ICommandContext context, Species species) {

            if (species is null || species.id < 0) {

                await ReplyAsync_NoSuchSpeciesExists(context);

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyAsync_ValidateSpecies(ICommandContext context, Species[] speciesList) {

            if (speciesList.Count() <= 0) {
                await ReplyAsync_NoSuchSpeciesExists(context);
                return false;
            }
            else if (speciesList.Count() > 1) {
                await ReplyAsync_MatchingSpecies(context, speciesList);
                return false;
            }

            return true;

        }
        public static async Task<bool> ReplyAsync_ValidateRole(ICommandContext context, Role role) {

            if (role is null || role.id <= 0) {

                await context.Channel.SendMessageAsync("No such role exists.");

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyAsync_ValidateZone(ICommandContext context, Zone zone) {

            if (zone is null || zone.id <= 0) {

                await context.Channel.SendMessageAsync("No such zone exists.");

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyAsync_ValidateGenus(ICommandContext context, Genus genus) {

            if (genus is null || genus.id <= 0) {

                await context.Channel.SendMessageAsync("No such genus exists.");

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyAsync_ValidateTaxon(ICommandContext context, Taxon taxon) {

            return await ReplyAsync_ValidateTaxon(context, taxon.type, taxon);

        }
        public static async Task<bool> ReplyAsync_ValidateTaxon(ICommandContext context, TaxonType type, Taxon taxon) {

            if (taxon is null || taxon.id <= 0) {

                await context.Channel.SendMessageAsync(string.Format("No such {0} exists.", Taxon.TypeToName(type)));

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyAsync_ValidateTaxa(ICommandContext context, Taxon[] taxa) {

            if (taxa is null || taxa.Count() <= 0) {

                // There must be at least one taxon in the list.

                await context.Channel.SendMessageAsync("No such taxon exists.");

                return false;

            }

            if (taxa.Count() > 1) {

                // There must be exactly one taxon in the list.

                SortedDictionary<TaxonType, List<Taxon>> taxa_dict = new SortedDictionary<TaxonType, List<Taxon>>();

                foreach (Taxon taxon in taxa) {

                    if (!taxa_dict.ContainsKey(taxon.type))
                        taxa_dict[taxon.type] = new List<Taxon>();

                    taxa_dict[taxon.type].Add(taxon);

                }

                EmbedBuilder embed = new EmbedBuilder();

                if (taxa_dict.Keys.Count() > 1)
                    embed.WithTitle(string.Format("Matching taxa ({0})", taxa.Count()));

                foreach (TaxonType type in taxa_dict.Keys) {

                    taxa_dict[type].Sort((lhs, rhs) => lhs.name.CompareTo(rhs.name));

                    StringBuilder field_content = new StringBuilder();

                    foreach (Taxon taxon in taxa_dict[type])
                        field_content.AppendLine(type == TaxonType.Species ? (await GetSpeciesFromDb(taxon.id)).GetShortName() : taxon.GetName());

                    embed.AddField(string.Format("{0}{1} ({2})",
                        taxa_dict.Keys.Count() == 1 ? "Matching " : "",
                        taxa_dict.Keys.Count() == 1 ? Taxon.TypeToName(type, true).ToLower() : StringUtils.ToTitleCase(Taxon.TypeToName(type, true)), taxa_dict[type].Count()),
                        field_content.ToString());

                }

                await context.Channel.SendMessageAsync("", false, embed.Build());

                return false;

            }

            return true;

        }
        public static async Task ReplyAsync_AddZonesToSpecies(ICommandContext context, Species sp, string zones, bool showErrorsOnly = false) {

            List<string> valid_zones = new List<string>();
            List<string> invalid_zones = new List<string>();

            foreach (string zoneName in Zone.ParseZoneList(zones)) {

                Zone zone_info = await GetZoneFromDb(zoneName);

                // If the given zone does not exist, add it to the list of invalid zones.

                if (zone_info is null) {

                    invalid_zones.Add(string.Format("**{0}**", StringUtils.ToTitleCase(Zone.GetFullName(zoneName))));

                    continue;

                }

                // Add the zone relationship into the database (do nothing if the relationship already exists).

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO SpeciesZones(species_id, zone_id) VALUES($species_id, $zone_id);")) {

                    cmd.Parameters.AddWithValue("$species_id", sp.id);
                    cmd.Parameters.AddWithValue("$zone_id", (zone_info.id));

                    await Database.ExecuteNonQuery(cmd);

                }

                valid_zones.Add(string.Format("**{0}**", zone_info.GetFullName()));

            }

            if (invalid_zones.Count() > 0)
                await ReplyAsync_Warning(context, string.Format("{0} {1} not exist.", StringUtils.ConjunctiveJoin(", ", invalid_zones),
                    invalid_zones.Count() == 1 ? "does" : "do"));

            if (valid_zones.Count() > 0 && !showErrorsOnly) {

                await ReplyAsync_Success(context, string.Format("**{0}** now inhabits {1}.",
                      sp.GetShortName(),
                      StringUtils.ConjunctiveJoin(", ", valid_zones)));

            }

        }
        public static async Task<bool> ReplyAsync_ValidateImageUrl(ICommandContext context, string imageUrl) {

            if (!Regex.Match(imageUrl, "^https?:").Success) {

                await ReplyAsync_Error(context, "The image URL is invalid.");

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyAsync_ValidatePeriod(ICommandContext context, Period period) {

            if (period is null || period.id <= 0) {

                await context.Channel.SendMessageAsync("No such period exists.");

                return false;

            }

            return true;

        }

        public static async Task ReplyAsync_Warning(ICommandContext context, string text) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(string.Format("⚠️ {0}", text));
            embed.WithColor(Discord.Color.Orange);

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task ReplyAsync_Error(ICommandContext context, string text) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(string.Format("❌ {0}", text));
            embed.WithColor(Discord.Color.Red);

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task ReplyAsync_Success(ICommandContext context, string text) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(string.Format("✅ {0}", text));
            embed.WithColor(Discord.Color.Green);

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task ReplyAsync_Info(ICommandContext context, string text) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(text);
            embed.WithColor(Discord.Color.LightGrey);

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task<bool> ReplyAsync_CheckPrivilege(ICommandContext context, IGuildUser user, PrivilegeLevel level) {

            if (CommandUtils.CheckPrivilege(user, level))
                return true;

            string privilege_name = "";

            switch (level) {

                case PrivilegeLevel.BotAdmin:
                    privilege_name = "Bot Admin";
                    break;

                case PrivilegeLevel.ServerAdmin:
                    privilege_name = "Admin";
                    break;

                case PrivilegeLevel.ServerModerator:
                    privilege_name = "Moderator";
                    break;

            }

            await ReplyAsync_Error(context, string.Format("You must have **{0}** privileges to use this command.", privilege_name));

            return false;

        }
        public static async Task<bool> ReplyAsync_CheckPrivilegeOrOwnership(ICommandContext context, IGuildUser user, PrivilegeLevel level, Species species) {

            if (user.Id == (ulong)species.user_id)
                return true;

            return await ReplyAsync_CheckPrivilege(context, user, level);

        }

        public static async Task Command_ShowTaxon(ICommandContext context, TaxonType type) {

            // If no taxon name was provided, list everything under the taxon.

            Taxon[] all_taxa = await GetTaxaFromDb(type);
            List<string> items = new List<string>();
            int taxon_count = 0;

            foreach (Taxon taxon in all_taxa) {

                // Count the number of items under this taxon.

                int sub_taxa_count = (await GetSubTaxaFromDb(taxon)).Count();

                if (sub_taxa_count <= 0)
                    continue;

                items.Add(string.Format("{0} ({1})", StringUtils.ToTitleCase(taxon.name), sub_taxa_count));

                ++taxon_count;

            }

            string title = string.Format("All {0} ({1})", Taxon.TypeToName(type, plural: true), taxon_count);
            List<EmbedBuilder> embed_pages = EmbedUtils.ListToEmbedPages(items, fieldName: title);

            PaginatedEmbedBuilder embed = new PaginatedEmbedBuilder(embed_pages);

            if (embed_pages.Count <= 0) {

                embed.SetTitle(title);
                embed.SetDescription(string.Format("No {0} have been added yet.", Taxon.TypeToName(type, plural: true)));

            }
            else
                embed.AppendFooter(string.Format(" — Empty {0} are not listed.", Taxon.TypeToName(type, plural: true)));

            await CommandUtils.ReplyAsync_SendPaginatedMessage(context, embed.Build());

        }
        public static async Task Command_ShowTaxon(ICommandContext context, TaxonType type, string name) {

            if (string.IsNullOrEmpty(name))
                await Command_ShowTaxon(context, type);

            else {

                // Get the specified taxon.

                Taxon taxon = await GetTaxonFromDb(name, type);

                if (!await ReplyAsync_ValidateTaxon(context, type, taxon))
                    return;

                List<string> items = new List<string>();

                if (taxon.type == TaxonType.Genus) {

                    // For genera, get all species underneath it.
                    // This will let us check if the species is extinct, and cross it out if that's the case.

                    Species[] species = await GetSpeciesInTaxonFromDb(taxon);

                    Array.Sort(species, (lhs, rhs) => lhs.name.ToLower().CompareTo(rhs.name.ToLower()));

                    foreach (Species s in species)
                        if (s.isExtinct)
                            items.Add(string.Format("~~{0}~~", s.name.ToLower()));
                        else
                            items.Add(s.name.ToLower());

                }
                else {

                    // Get all subtaxa under this taxon.
                    Taxon[] subtaxa = await GetSubTaxaFromDb(taxon);

                    // Add all subtaxa to the list.

                    foreach (Taxon t in subtaxa) {

                        if (t.type == TaxonType.Species)
                            // Do not attempt to count sub-taxa for species.
                            items.Add(t.GetName().ToLower());

                        else {

                            // Count the number of species under this taxon.
                            // Taxa with no species under them will not be displayed.

                            long species_count = await CountSpeciesInTaxonFromDb(t);

                            if (species_count <= 0)
                                continue;

                            // Count the sub-taxa under this taxon.

                            long subtaxa_count = 0;

                            using (SQLiteCommand cmd = new SQLiteCommand(string.Format("SELECT COUNT(*) FROM {0} WHERE {1}=$parent_id;",
                                Taxon.TypeToDatabaseTableName(t.GetChildType()),
                                Taxon.TypeToDatabaseColumnName(t.type)
                                ))) {

                                cmd.Parameters.AddWithValue("$parent_id", t.id);

                                subtaxa_count = await Database.GetScalar<long>(cmd);

                            }

                            // Add the taxon to the list.

                            if (subtaxa_count > 0)
                                items.Add(string.Format("{0} ({1})", t.GetName(), subtaxa_count));

                        }

                    }

                }

                // Generate embed pages.

                string title = string.IsNullOrEmpty(taxon.common_name) ? taxon.GetName() : string.Format("{0} ({1})", taxon.GetName(), taxon.GetCommonName());
                string field_title = string.Format("{0} in this {1} ({2}):", StringUtils.ToTitleCase(Taxon.TypeToName(Taxon.TypeToChildType(type), plural: true)), Taxon.TypeToName(type), items.Count());
                string thumbnail_url = taxon.pics;

                StringBuilder description = new StringBuilder();
                description.AppendLine(taxon.GetDescriptionOrDefault());

                if (items.Count() <= 0) {

                    description.AppendLine();
                    description.AppendLine(string.Format("This {0} contains no {1}.", Taxon.TypeToName(type), Taxon.TypeToName(Taxon.TypeToChildType(type), plural: true)));

                }

                List<EmbedBuilder> embed_pages = EmbedUtils.ListToEmbedPages(items, fieldName: field_title);
                PaginatedEmbedBuilder embed = new PaginatedEmbedBuilder(embed_pages);

                embed.SetTitle(title);
                embed.SetThumbnailUrl(thumbnail_url);
                embed.SetDescription(description.ToString());

                if (items.Count() > 0 && taxon.type != TaxonType.Genus)
                    embed.AppendFooter(string.Format(" — Empty {0} are not listed.", Taxon.TypeToName(taxon.GetChildType(), plural: true)));

                await CommandUtils.ReplyAsync_SendPaginatedMessage(context, embed.Build());

            }

        }
        public static async Task Command_AddTaxon(ICommandContext context, TaxonType type, string name, string description) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await ReplyAsync_CheckPrivilege(context, (IGuildUser)context.User, PrivilegeLevel.ServerModerator))
                return;

            // Make sure that the taxon does not already exist before trying to add it.

            Taxon taxon = await GetTaxonFromDb(name, type);

            if (!(taxon is null)) {

                await ReplyAsync_Warning(context, string.Format("The {0} **{1}** already exists.", Taxon.TypeToName(type), taxon.GetName()));

                return;

            }

            taxon = new Taxon(type) {
                name = name,
                description = description
            };

            await AddTaxonToDb(taxon, type);

            await ReplyAsync_Success(context, string.Format("Successfully created new {0}, **{1}**.",
                Taxon.TypeToName(type),
                taxon.GetName()));

        }
        public static async Task Command_SetTaxon(ICommandContext context, TaxonType type, string childTaxonName, string parentTaxonName) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await ReplyAsync_CheckPrivilege(context, (IGuildUser)context.User, PrivilegeLevel.ServerModerator))
                return;

            // Get the specified child taxon.

            Taxon child = await GetTaxonFromDb(childTaxonName, Taxon.TypeToChildType(type));

            if (!await ReplyAsync_ValidateTaxon(context, Taxon.TypeToChildType(type), child))
                return;

            // Get the specified parent taxon.

            Taxon parent = await GetTaxonFromDb(parentTaxonName, type);

            if (!await ReplyAsync_ValidateTaxon(context, type, parent))
                return;

            // Update the taxon.

            child.parent_id = parent.id;

            await UpdateTaxonInDb(child, Taxon.TypeToChildType(type));

            await ReplyAsync_Success(context, string.Format("{0} **{1}** has sucessfully been placed under the {2} **{3}**.",
                    StringUtils.ToTitleCase(Taxon.TypeToName(Taxon.TypeToChildType(type))),
                    child.GetName(),
                    Taxon.TypeToName(type),
                    parent.GetName()
                ));

        }
        public static async Task Command_SetTaxonDescription(ICommandContext context, Taxon taxon, string description) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await ReplyAsync_CheckPrivilege(context, (IGuildUser)context.User, PrivilegeLevel.ServerModerator))
                return;

            taxon.description = description;

            await UpdateTaxonInDb(taxon, taxon.type);

            string success_message = string.Format("Successfully updated description for {0} **{1}**.", Taxon.TypeToName(taxon.type), taxon.GetName());

            await ReplyAsync_Success(context, success_message);


        }
        public static async Task Command_SetTaxonDescription(ICommandContext context, TaxonType type, string name, string description) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await ReplyAsync_CheckPrivilege(context, (IGuildUser)context.User, PrivilegeLevel.ServerModerator))
                return;

            Taxon taxon = await GetTaxonFromDb(name, type);

            if (!await ReplyAsync_ValidateTaxon(context, type, taxon))
                return;

            await Command_SetTaxonDescription(context, taxon, description);

        }
        public static async Task Command_SetTaxonCommonName(ICommandContext context, TaxonType type, string name, string commonName) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await ReplyAsync_CheckPrivilege(context, (IGuildUser)context.User, PrivilegeLevel.ServerModerator))
                return;

            Taxon taxon = await GetTaxonFromDb(name, type);

            if (!await ReplyAsync_ValidateTaxon(context, type, taxon))
                return;

            taxon.common_name = commonName;

            await UpdateTaxonInDb(taxon, type);

            await ReplyAsync_Success(context, string.Format("Members of the {0} **{1}** are now commonly known as **{2}**.",
                Taxon.TypeToName(type),
                taxon.GetName(),
                taxon.GetCommonName()
                ));

        }

        private class SpeciesInfo {
            public Species species = null;
            public bool isAncestor = false;
        }

        private static async Task<TreeNode<SpeciesInfo>> _generateAncestryTree(Species species, bool descendantsOnly = false) {

            // Start by finding the earliest ancestor of this species.

            long id = species.id;
            List<long> ancestor_ids = new List<long>();

            ancestor_ids.Add(id);

            if (!descendantsOnly) {

                while (true) {

                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT ancestor_id FROM Ancestors WHERE species_id = $species_id;")) {

                        cmd.Parameters.AddWithValue("$species_id", id);

                        DataRow row = await Database.GetRowAsync(cmd);

                        if (!(row is null)) {

                            id = row.Field<long>("ancestor_id");

                            ancestor_ids.Add(id);

                        }
                        else
                            break;

                    }

                }

            }

            // Starting from the earliest ancestor, generate all tiers, down to the latest descendant.

            TreeNode<SpeciesInfo> root = new TreeNode<SpeciesInfo> {
                value = new SpeciesInfo {
                    species = await GetSpeciesFromDb(id),
                    isAncestor = true
                }
            };

            Queue<TreeNode<SpeciesInfo>> queue = new Queue<TreeNode<SpeciesInfo>>();
            queue.Enqueue(root);

            while (queue.Count() > 0) {

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM Ancestors WHERE ancestor_id = $ancestor_id);")) {

                    cmd.Parameters.AddWithValue("$ancestor_id", queue.First().value.species.id);

                    using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                        // Add each species in this tier to the list.

                        foreach (DataRow row in rows.Rows) {

                            Species sp = await Species.FromDataRow(row);

                            TreeNode<SpeciesInfo> node = new TreeNode<SpeciesInfo> {
                                value = new SpeciesInfo {
                                    species = sp,
                                    isAncestor = ancestor_ids.Contains(sp.id)
                                }
                            };

                            queue.First().children.Add(node);
                            queue.Enqueue(node);

                        }

                    }

                }

                queue.Dequeue();

            }

            return root;

        }
        private static void _drawSpeciesTreeNode(Graphics gfx, TreeNode<SpeciesInfo> node, Species selectedSpecies, Font font) {

            // Cross-out the species if it's extinct.

            if (node.value.species.isExtinct)
                using (Brush brush = new SolidBrush(System.Drawing.Color.White))
                using (Pen pen = new Pen(brush, 1.0f))
                    gfx.DrawLine(pen,
                        new PointF(node.bounds.X, node.bounds.Y + node.bounds.Height / 2.0f),
                        new PointF(node.bounds.X + node.bounds.Width - 5.0f, node.bounds.Y + node.bounds.Height / 2.0f));

            // Draw the name of the species.

            using (Brush brush = new SolidBrush(node.value.species.id == selectedSpecies.id ? System.Drawing.Color.Yellow : System.Drawing.Color.White))
                gfx.DrawString(node.value.species.GetShortName(), font, brush, new PointF(node.bounds.X, node.bounds.Y));

            // Draw child nodes.

            foreach (TreeNode<SpeciesInfo> child in node.children) {

                using (Brush brush = new SolidBrush(child.value.isAncestor ? System.Drawing.Color.Yellow : System.Drawing.Color.FromArgb(162, 164, 171)))
                using (Pen pen = new Pen(brush, 2.0f)) {

                    pen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;

                    gfx.DrawLine(pen,
                        new PointF(node.bounds.X + (node.bounds.Width / 2.0f), node.bounds.Y + node.bounds.Height),
                        new PointF(child.bounds.X + (child.bounds.Width / 2.0f), child.bounds.Y));

                }

                _drawSpeciesTreeNode(gfx, child, selectedSpecies, font);

            }

        }

        public static async Task<string> GenerateEvolutionTreeImage(Species sp, bool descendantsOnly = false) {

            // Generate the ancestry tree.

            TreeNode<SpeciesInfo> root = await _generateAncestryTree(sp, descendantsOnly);

            // Generate the evolution tree image.

            using (Font font = new Font("Calibri", 12)) {

                // Calculate the size of each node.

                float horizontal_padding = 5.0f;

                TreeUtils.PostOrderTraverse(root, (node) => {

                    SizeF size = GraphicsUtils.MeasureString(node.value.species.GetShortName(), font);

                    node.bounds.Width = size.Width + horizontal_padding;
                    node.bounds.Height = size.Height;

                });

                // Calculate node placements.

                TreeUtils.CalculateNodePlacements(root);

                // Calculate the size of the tree.

                RectangleF bounds = TreeUtils.CalculateTreeBounds(root);

                // Shift the tree so that the entire thing is visible.

                float min_x = 0.0f;

                TreeUtils.PostOrderTraverse(root, (node) => {

                    if (node.bounds.X < min_x)
                        min_x = bounds.X;

                });

                TreeUtils.ShiftTree(root, -min_x, 0.0f);

                // Create the bitmap.

                using (Bitmap bmp = new Bitmap((int)bounds.Width, (int)bounds.Height))
                using (Graphics gfx = Graphics.FromImage(bmp)) {

                    gfx.Clear(System.Drawing.Color.FromArgb(54, 57, 63));
                    gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    _drawSpeciesTreeNode(gfx, root, sp, font);

                    // Save the result.

                    string out_dir = "temp";

                    if (!System.IO.Directory.Exists(out_dir))
                        System.IO.Directory.CreateDirectory(out_dir);

                    string fpath = System.IO.Path.Combine(out_dir, sp.GetShortName() + ".png");

                    bmp.Save(fpath);

                    return fpath;

                }

            }

        }

        public static async Task<string> Reply_UploadFileToScratchServerAsync(ICommandContext context, string filePath) {

            var client = OurFoodChainBot.GetInstance().GetClient();
            ulong serverId = OurFoodChainBot.GetInstance().GetConfig().scratch_server;
            ulong channelId = OurFoodChainBot.GetInstance().GetConfig().scratch_channel;

            if (serverId <= 0 || channelId <= 0) {

                await ReplyAsync_Error(context, "Cannot upload images because no scratch server/channel has been specified in the configuration file.");

                return string.Empty;

            }

            IGuild guild = await client.GetGuildAsync(serverId);

            if (guild is null) {

                await ReplyAsync_Error(context, "Cannot upload images because the scratch server is inaccessible.");

                return string.Empty;

            }

            ITextChannel channel = await guild.GetTextChannelAsync(channelId);

            if (channel is null) {

                await ReplyAsync_Error(context, "Cannot upload images because the scratch channel is inaccessible.");

                return string.Empty;

            }

            IUserMessage result = await channel.SendFileAsync(filePath, "");

            var enumerator = result.Attachments.GetEnumerator();
            enumerator.MoveNext();

            string url = enumerator.Current.Url;

            return url;

        }

        public static int RandomInteger(int max) {

            return RANDOM.Next(max);

        }
        public static int RandomInteger(int min, int max) {

            return RANDOM.Next(min, max);

        }

    }

}