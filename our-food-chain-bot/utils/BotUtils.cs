using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
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

        public string GetShortDescription() {
            return GetShortDescription(description);
        }
        public string GetDescriptionOrDefault() {

            if (string.IsNullOrEmpty(description))
                return BotUtils.DEFAULT_ZONE_DESCRIPTION;

            return description;

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

    class Genus {

        public long id;
        public string name;

        public static Genus FromDataRow(DataRow row) {

            Genus result = new Genus {
                id = row.Field<long>("id"),
                name = row.Field<string>("name")
            };

            return result;

        }

    }

    class Species {

        public long id;
        public string genus;
        public string name;
        public string description;
        public string owner;
        public long timestamp;
        public string pics;
        public string commonName;

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

            return species;

        }

        public string GetShortName() {

            return BotUtils.GenerateSpeciesName(this);

        }
        public string GetTimeStampAsDateString() {

            return DateTimeOffset.FromUnixTimeSeconds(timestamp).Date.ToUniversalTime().ToShortDateString();

        }

    }

    class Role {

        public long id;
        public string name;
        public string description;

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

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Genus(name) VALUES($name);")) {

                cmd.Parameters.AddWithValue("$name", genus.ToLower());

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
        public static async Task<long> GetSpeciesIdFromDb(long genusId, string species) {

            long species_id = -1;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT id FROM Species WHERE name=$species AND genus_id=$genus_id;")) {

                cmd.Parameters.AddWithValue("$species", species);
                cmd.Parameters.AddWithValue("$genus_id", genusId);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    species_id = row.Field<long>("id");

            }

            return species_id;

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
        public static async Task<Species[]> GetSpeciesFromDbByZone(Zone zone) {

            // Return all species in the given zone.

            List<Species> species = new List<Species>();

            if (zone is null || zone.id <= 0)
                return species.ToArray();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM SpeciesZones WHERE zone_id=$zone_id) ORDER BY name ASC;")) {

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

            zoneName = Zone.GetFullName(zoneName).ToLower();

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

            return roles.ToArray();

        }
        public static async Task<Role> GetRoleFromDb(string roleName) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Roles WHERE name=$name;")) {

                cmd.Parameters.AddWithValue("$name", roleName.ToLower());

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return Role.FromDataRow(row);

            }

            return null;

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

                    await message.Channel.SendMessageAsync("Description added successfully.");

                    break;

            }

            TWO_PART_COMMAND_WAIT_PARAMS.Remove(message.Author.Id);

        }

        public static async Task ReplyAsync_NoSuchSpeciesExists(ICommandContext context) {

            await context.Channel.SendMessageAsync("No such species exists.");

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

    }

}
