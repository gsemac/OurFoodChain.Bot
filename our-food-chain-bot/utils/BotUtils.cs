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
        public TwoPartCommandWaitParamsType type;
        public string[] args;
        public DateTime timestamp;
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

        public static Zone FromDataRow(DataRow row) {

            Zone zone = new Zone();
            zone.id = row.Field<long>("id");
            zone.name = StringUtils.ToTitleCase(row.Field<string>("name"));
            zone.description = row.Field<string>("description");

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
            return GetShortDescription(description);
        }
        public string GetDescriptionOrDefault() {

            if (string.IsNullOrEmpty(description))
                return BotUtils.DEFAULT_ZONE_DESCRIPTION;

            return description;

        }
        public string GetShortName() {

            return Regex.Replace(name, "^zone\\s+", "", RegexOptions.IgnoreCase);

        }

        public static string GetShortDescription(string description) {
            return Regex.Match(description, "^[a-zA-Z0-9 ,;:\\-\"]+(?:\\.+|[!\\?])").Value;
        }
        public static string GetFullName(string name) {

            if (StringUtils.IsNumeric(name) || name.Length == 1)
                name = "zone " + name;

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

    class Species {

        public long id;
        public string name;
        public string description;
        public string owner;
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
                name = row.Field<string>("name"),
                genus = genus_info.name,
                description = row.Field<string>("description"),
                owner = row.Field<string>("owner"),
                timestamp = (long)row.Field<decimal>("timestamp"),
                commonName = row.Field<string>("common_name"),
                pics = row.Field<string>("pics")
            };

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

    }

    class Role {

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

        public static readonly string DEFAULT_SPECIES_DESCRIPTION = "No description provided.";
        public static readonly string DEFAULT_GENUS_DESCRIPTION = "No description provided.";
        public static readonly string DEFAULT_ZONE_DESCRIPTION = "No description provided.";
        public static readonly string DEFAULT_DESCRIPTION = "No description provided.";

        public static Dictionary<ulong, TwoPartCommandWaitParams> TWO_PART_COMMAND_WAIT_PARAMS = new Dictionary<ulong, TwoPartCommandWaitParams>();

        public static async Task<bool> SpeciesExistsInDb(string genus, string species) {

            return (await GetSpeciesFromDb(genus, species)).Count() > 0;

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

            // Ignore any stray periods in the genus/species names.

            if (!string.IsNullOrEmpty(genus))
                genus = genus.Trim('.');

            if (!string.IsNullOrEmpty(species))
                species = species.Trim('.');

            // If the genus is empty but the species contains a period, assume everything to the left of the period is the genus.

            if (string.IsNullOrEmpty(genus) && species.Contains('.')) {

                string[] parts = species.Split('.');

                genus = parts[0];
                species = parts[1];

            }

            genus = genus.ToLower();
            species = species.ToLower();

            Genus genus_info = null;

            List<Species> matches = new List<Species>();

            bool genus_is_abbrev = false;
            string selection_str = "SELECT * FROM Species WHERE genus_id=$genus_id AND (name=$species;";

            if (string.IsNullOrEmpty(genus) || Regex.Match(genus, @"[a-z]\.?$").Success) {

                selection_str = "SELECT * FROM Species WHERE name=$species;";
                genus_is_abbrev = true;

            }
            else
                // Since the genus is not abbreviated, we can get genus information immediately.
                genus_info = await GetGenusFromDb(genus);

            using (SQLiteConnection conn = await Database.GetConnectionAsync())
            using (SQLiteCommand cmd = new SQLiteCommand(selection_str)) {

                cmd.Parameters.AddWithValue("$species", species);

                if (!genus_is_abbrev)
                    cmd.Parameters.AddWithValue("$genus_id", genus_info.id);

                using (DataTable rows = await Database.GetRowsAsync(conn, cmd))
                    foreach (DataRow row in rows.Rows) {

                        if (!string.IsNullOrEmpty(genus) && genus_is_abbrev) {

                            genus_info = await GetGenusFromDb(row.Field<long>("genus_id"));

                            if (genus_info != null && !genus_info.name.StartsWith(genus[0].ToString()))
                                continue;

                        }

                        Species cur_species = await Species.FromDataRow(row);

                        matches.Add(cur_species);

                    }

            }

            return matches.ToArray();

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
        public static async Task<Role[]> GetRolesFromDbBySpecies(Species species) {

            // Return all roles assigned to the given species.

            List<Role> roles = new List<Role>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Roles WHERE id IN (SELECT role_id FROM SpeciesRoles WHERE species_id=$species_id) ORDER BY name ASC;")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        roles.Add(Role.FromDataRow(row));

            }

            // Get role notes.
            // #todo Get the roles and notes using a single query.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM SpeciesRoles WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);

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
        public static async Task<Family> GetFamilyFromDb(string family) {

            Family family_info = null;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Family WHERE name=$family;")) {

                cmd.Parameters.AddWithValue("$family", family.ToLower());

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    family_info = Family.FromDataRow(row);

            }

            return family_info;

        }
        public static async Task<Family[]> GetFamiliesFromDb() {

            List<Family> result = new List<Family>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Family;"))
            using (DataTable rows = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in rows.Rows)
                    result.Add(Family.FromDataRow(row));

            // Sort roles by name in alphabetical order.
            result.Sort((lhs, rhs) => lhs.name.CompareTo(rhs.name));

            return result.ToArray();

        }
        public static async Task UpdateFamilyInDb(Family family) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Family SET name=$name, description=$description, order_id=$order_id WHERE id=$family_id;")) {

                cmd.Parameters.AddWithValue("$name", family.name.ToLower());
                cmd.Parameters.AddWithValue("$description", family.description);
                cmd.Parameters.AddWithValue("$order_id", family.order_id);
                cmd.Parameters.AddWithValue("$family_id", family.id);

                await Database.ExecuteNonQuery(cmd);

            }

        }
        public static async Task AddFamilyToDb(Family family) {

            string query = "INSERT INTO Family(name, description, order_id) VALUES($name, $description, $order_id);";

            if (family.order_id <= 0)
                query = "INSERT INTO Family(name, description) VALUES($name, $description);";

            using (SQLiteCommand cmd = new SQLiteCommand(query)) {

                cmd.Parameters.AddWithValue("$name", family.name.ToLower());
                cmd.Parameters.AddWithValue("$description", family.description);

                if (family.order_id > 0)
                    cmd.Parameters.AddWithValue("$order_id", family.order_id);

                await Database.ExecuteNonQuery(cmd);

            }

        }

        public static async Task AddRoleToDb(Role role) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Roles(name, description) VALUES($name, $description);")) {

                cmd.Parameters.AddWithValue("$name", role.name.ToLower());
                cmd.Parameters.AddWithValue("$description", role.description);

                await Database.ExecuteNonQuery(cmd);

            }

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
        public static string Strikeout(string str) {

            return string.Format("~~{0}~~", str);

        }
        public static async Task UpdateSpeciesDescription(string genus, string species, string description) {

            Species[] sp_list = await GetSpeciesFromDb(genus, species);

            if (sp_list.Count() <= 0)
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET description=$description WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", sp_list[0].id);
                cmd.Parameters.AddWithValue("$description", description);

                await Database.ExecuteNonQuery(cmd);

            }

        }
        public static async Task HandleTwoPartCommandResponse(SocketMessage message) {

            if (!TWO_PART_COMMAND_WAIT_PARAMS.ContainsKey(message.Author.Id))
                return;

            TwoPartCommandWaitParams p = TWO_PART_COMMAND_WAIT_PARAMS[message.Author.Id];

            switch (p.type) {

                case TwoPartCommandWaitParamsType.Description:

                    await UpdateSpeciesDescription(p.args[0], p.args[1], message.Content);

                    await message.Channel.SendMessageAsync("Description updated successfully.");

                    break;

            }

            TWO_PART_COMMAND_WAIT_PARAMS.Remove(message.Author.Id);

        }

        public static async Task<Species> ReplyAsync_FindSpecies(ICommandContext context, string genus, string species) {

            Species[] sp_list = await GetSpeciesFromDb(genus, species);

            if (sp_list.Count() <= 0) {

                // The species could not be find. Check all species to find a suggestion.

                List<Species> sp_list_2 = new List<Species>();

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species;")) {

                    using (DataTable rows = await Database.GetRowsAsync(cmd))
                        foreach (DataRow row in rows.Rows)
                            sp_list_2.Add(await Species.FromDataRow(row));

                }

                sp_list = sp_list_2.ToArray();

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

                return null;

            }
            else if (sp_list.Count() > 1) {

                await ReplyAsync_MatchingSpecies(context, sp_list);
                return null;

            }

            return sp_list[0];

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
        public static async Task<bool> ReplyAsync_ValidateFamily(ICommandContext context, Family family) {

            if (family is null || family.id <= 0) {

                await context.Channel.SendMessageAsync("No such family exists.");

                return false;

            }

            return true;

        }
        public static async Task ReplyAsync_AddZonesToSpecies(ICommandContext context, long speciesId, string zones, bool showErrorsOnly = false) {

            List<string> invalid_zones = new List<string>();

            foreach (string zoneName in Zone.ParseZoneList(zones)) {

                Zone zone_info = await GetZoneFromDb(zoneName);

                // If the given zone does not exist, add it to the list of invalid zones.

                if (zone_info is null) {

                    invalid_zones.Add(StringUtils.ToTitleCase(Zone.GetFullName(zoneName)));

                    continue;

                }

                // Add the zone relationship into the database (do nothing if the relationship already exists).

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO SpeciesZones(species_id, zone_id) VALUES($species_id, $zone_id);")) {

                    cmd.Parameters.AddWithValue("$species_id", speciesId);
                    cmd.Parameters.AddWithValue("$zone_id", (zone_info.id));

                    await Database.ExecuteNonQuery(cmd);

                }

            }

            if (invalid_zones.Count() <= 0) {

                if (!showErrorsOnly)
                    await ReplyAsync_Success(context, string.Format("Zones updated successfully."));

            }
            else
                await ReplyAsync_Warning(context, string.Format("The following zones could not be added (because they don't exist): {0}", string.Join(", ", invalid_zones)));

        }
        public static async Task<bool> ReplyAsync_ValidateImageUrl(ICommandContext context, string imageUrl) {

            if (!Regex.Match(imageUrl, "^https?:").Success) {

                await ReplyAsync_Error(context, "The image URL is invalid.");

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

        private static int _getSpeciesTreeWidth(Tree<Species>.TreeNode root, Font font) {

            int width = (int)GraphicsUtils.MeasureString(root.value.GetShortName(), font).Width;

            int child_width = 0;

            foreach (Tree<Species>.TreeNode i in root.childNodes)
                child_width += _getSpeciesTreeWidth(i, font);

            return Math.Max(width, child_width);

        }
        private static void _drawSpeciesTree(Tree<Species>.TreeNode root, Font font, Graphics gfx, Species highlightSpecies, int x, int y, int w) {

            // Draw the tree using DFS.

            SizeF size = GraphicsUtils.MeasureString(root.value.GetShortName(), font);
            int dx = x + (w / 2) - ((int)size.Width / 2);
            int dy = y;

            if (root.value.isExtinct && System.IO.File.Exists("res/x.png"))
                using (Bitmap extinctIcon = new Bitmap("res/x.png")) {

                    float icon_scale = size.Height / extinctIcon.Height;
                    int icon_w = (int)(extinctIcon.Width * icon_scale);
                    int icon_h = (int)(extinctIcon.Height * icon_scale);
                    int icon_x = x + (w / 2) - (icon_w / 2);
                    int icon_y = dy + ((int)size.Height / 2) - (icon_h / 2);

                    gfx.DrawImage(extinctIcon, new Rectangle(icon_x, icon_y, icon_w, icon_h));

                }

            using (Brush brush = new SolidBrush(root.value.id == highlightSpecies.id ? System.Drawing.Color.Yellow : System.Drawing.Color.Black))
                gfx.DrawString(root.value.GetShortName(), font, brush, new Point(dx, dy));

            int cx = 0;
            int cy = y + (int)size.Height * 3;
            int cw = root.childNodes.Count() > 0 ? w / root.childNodes.Count() : w;

            foreach (Tree<Species>.TreeNode n in root.childNodes) {

                using (Brush brush = new SolidBrush(System.Drawing.Color.Black))
                using (Pen pen = new Pen(brush, 2.0f)) {

                    pen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;

                    gfx.DrawLine(pen, new Point(x + (w / 2), y + (int)size.Height), new Point(cx + (cw / 2), cy));

                }

                _drawSpeciesTree(n, font, gfx, highlightSpecies, cx, cy, cw);

                cx += cw;

            }

        }

        public static async Task<string> GenerateEvolutionTreeImage(Species sp) {

            // Start by finding the earliest ancestor of this species.

            long id = sp.id;

            while (true) {

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT ancestor_id FROM Ancestors WHERE species_id = $species_id;")) {

                    cmd.Parameters.AddWithValue("$species_id", id);

                    DataRow row = await Database.GetRowAsync(cmd);

                    if (!(row is null))
                        id = row.Field<long>("ancestor_id");
                    else
                        break;

                }

            }

            // Starting from the earliest ancestor, generate all tiers, down to the latest descendant.

            Tree<Species>.TreeNode root = new Tree<Species>.TreeNode();
            root.value = await BotUtils.GetSpeciesFromDb(id);

            Queue<Tree<Species>.TreeNode> queue = new Queue<Tree<Species>.TreeNode>();
            queue.Enqueue(root);

            while (queue.Count() > 0) {

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM Ancestors WHERE ancestor_id = $ancestor_id);")) {

                    cmd.Parameters.AddWithValue("$ancestor_id", queue.First().value.id);

                    using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                        // Add each species in this tier to the list.

                        foreach (DataRow row in rows.Rows) {

                            Tree<Species>.TreeNode node = new Tree<Species>.TreeNode();
                            node.value = await Species.FromDataRow(row);

                            queue.First().childNodes.Add(node);
                            queue.Enqueue(node);

                        }

                    }

                }

                queue.Dequeue();

            }

            // Generate the evolution tree image.

            using (Font font = new Font("Calibri", 12)) {

                // Determine the dimensions of the image.

                int padding = 30;
                int width = _getSpeciesTreeWidth(root, font) + padding;
                int depth = Tree<Species>.Depth(root);
                int line_height = (int)GraphicsUtils.MeasureString("test", font).Height;
                int height = (depth - 1) * (line_height * 3) + line_height;

                // Create the bitmap.

                using (Bitmap bmp = new Bitmap(width, height))
                using (Graphics gfx = Graphics.FromImage(bmp)) {

                    gfx.Clear(System.Drawing.Color.Transparent);
                    gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    _drawSpeciesTree(root, font, gfx, sp, 0, 0, width);

                    // Save the result.

                    string fname = sp.GetShortName() + ".png";

                    bmp.Save(fname);

                    return fname;

                }

            }

        }

    }

}
