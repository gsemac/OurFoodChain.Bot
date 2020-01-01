using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OurFoodChain.Bot;
using OurFoodChain.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public enum Resolve3ArgumentFindSpeciesAmbiguityCase {
        Unknown,
        GenusSpeciesSpecies,
        SpeciesGenusSpecies
    }

    public class Resolve3ArgumentFindSpeciesAmbiguityResult {

        public Resolve3ArgumentFindSpeciesAmbiguityCase Case { get; set; } = Resolve3ArgumentFindSpeciesAmbiguityCase.Unknown;
        public Species Species1 { get; set; } = null;
        public Species Species2 { get; set; } = null;

    }

    class BotUtils {

        public const string DEFAULT_GENUS_DESCRIPTION = "No description provided.";
        public const string DEFAULT_ZONE_DESCRIPTION = "No description provided.";
        public const string DEFAULT_DESCRIPTION = "No description provided.";
        private static Random RANDOM = new Random();

        public static async Task<bool> SpeciesExistsInDb(string genus, string species) {

            return (await GetSpeciesFromDb(genus, species)).Count() > 0;

        }
        public static async Task<Zone[]> GetZonesFromDb() {

            List<Zone> zones = new List<Zone>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Zones;"))
            using (DataTable rows = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in rows.Rows)
                    zones.Add(ZoneUtils.ZoneFromDataRow(row));

            return zones.ToArray();

        }
        public static async Task<Zone[]> GetZonesFromDb(long speciesId) {

            List<Zone> zones = new List<Zone>();

            using (SQLiteConnection conn = await Database.GetConnectionAsync())
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM SpeciesZones WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", speciesId);

                using (DataTable rows = await Database.GetRowsAsync(conn, cmd))
                    foreach (DataRow row in rows.Rows) {

                        Zone zone = await ZoneUtils.GetZoneAsync(row.Field<long>("zone_id"));

                        if (zone is null)
                            continue;

                        zones.Add(zone);

                    }

            }

            return zones.ToArray();

        }
        public static async Task<Species[]> GetSpeciesFromDb(string genus, string species) {
            return await SpeciesUtils.GetSpeciesAsync(genus, species);
        } // deprecated
        public static async Task<Species[]> GetSpeciesFromDbByRole(Role role) {

            // Return all species with the given role.

            List<Species> species = new List<Species>();

            if (role is null || role.id <= 0)
                return species.ToArray();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM SpeciesRoles WHERE role_id=$role_id) ORDER BY name ASC;")) {

                cmd.Parameters.AddWithValue("$role_id", role.id);

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        species.Add(await SpeciesUtils.SpeciesFromDataRow(row));

            }

            return species.ToArray();

        }
        public static async Task<Species[]> GetSpeciesFromDbByZone(Zone zone, bool extantOnly = true) {

            // Return all species in the given zone.

            List<Species> species = new List<Species>();

            if (zone is null || zone.Id <= 0)
                return species.ToArray();

            string query_all = "SELECT * FROM Species WHERE id IN (SELECT species_id FROM SpeciesZones WHERE zone_id=$zone_id) ORDER BY name ASC;";
            string query_extant = "SELECT * FROM Species WHERE id IN (SELECT species_id FROM SpeciesZones WHERE zone_id=$zone_id) AND id NOT IN (SELECT species_id FROM Extinctions) ORDER BY name ASC;";

            using (SQLiteCommand cmd = new SQLiteCommand(extantOnly ? query_extant : query_all)) {

                cmd.Parameters.AddWithValue("$zone_id", zone.Id);

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        species.Add(await SpeciesUtils.SpeciesFromDataRow(row));

            }

            return species.ToArray();

        }
        public static async Task<bool> IsEndangeredSpeciesAsync(Species species) {

            // Consider a species "endangered" if:
            // - All of its prey has gone extinct.
            // - It has a descendant in the same zone that consumes all of the same prey items.

            bool isEndangered = false;

            if (species.IsExtinct)
                return isEndangered;

            if (!isEndangered) {

                // Check if all of this species' prey species have gone extinct.

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Species WHERE id = $id AND id NOT IN (SELECT species_id FROM Predates WHERE eats_id NOT IN (SELECT species_id FROM Extinctions)) AND id IN (SELECT species_id from Predates)")) {

                    cmd.Parameters.AddWithValue("$id", species.Id);

                    isEndangered = await Database.GetScalar<long>(cmd) > 0;

                }

            }

            if (!isEndangered) {

                // Check if this species has a direct descendant in the same zone that consumes all of the same prey items.

                string query =
                    @"SELECT COUNT(*) FROM Species WHERE id IN (SELECT species_id FROM Ancestors WHERE ancestor_id = $ancestor_id) AND EXISTS (
                        SELECT * FROM (SELECT COUNT(*) AS prey_count FROM Predates WHERE species_id = $ancestor_id) WHERE prey_count > 0 AND prey_count = (
			                SELECT COUNT(*) FROM ((SELECT * FROM Predates WHERE species_id = Species.id) Predates1 INNER JOIN (SELECT * FROM Predates WHERE species_id = $ancestor_id) Predates2 ON Predates1.eats_id = Predates2.eats_id)
                        )
	                )
                    AND EXISTS (SELECT * FROM SpeciesZones WHERE species_id = Species.id AND zone_id IN (SELECT zone_id FROM SpeciesZones WHERE species_id = $ancestor_id))";

                using (SQLiteCommand cmd = new SQLiteCommand(query)) {

                    cmd.Parameters.AddWithValue("$ancestor_id", species.Id);

                    isEndangered = await Database.GetScalar<long>(cmd) > 0;

                }

            }

            return isEndangered;

        }

        public static async Task AddGenusToDb(string genus) {

            Taxon genus_info = new Taxon(TaxonRank.Genus) {
                name = genus
            };

            await AddGenusToDb(genus_info);

        }
        public static async Task AddGenusToDb(Taxon genus) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Genus(name, description) VALUES($name, $description);")) {

                cmd.Parameters.AddWithValue("$name", genus.name.ToLower());
                cmd.Parameters.AddWithValue("$description", genus.description);

                await Database.ExecuteNonQuery(cmd);

            }

        }
        public static async Task<Taxon> GetGenusFromDb(string genus) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Genus WHERE name=$genus;")) {

                cmd.Parameters.AddWithValue("$genus", genus.ToLower());

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return Taxon.FromDataRow(row, TaxonRank.Genus);

            }

            return null;

        }
        public static async Task<Taxon> GetGenusFromDb(long genusId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Genus WHERE id=$genus_id;")) {

                cmd.Parameters.AddWithValue("$genus_id", genusId);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return Taxon.FromDataRow(row, TaxonRank.Genus);

            }

            return null;

        }
        public static async Task<Taxon[]> GetGeneraFromDb(Taxon family) {

            List<Taxon> genera = new List<Taxon>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Genus WHERE family_id=$family_id ORDER BY name ASC;")) {

                cmd.Parameters.AddWithValue("$family_id", family.id);

                using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                    foreach (DataRow row in rows.Rows)
                        genera.Add(Taxon.FromDataRow(row, TaxonRank.Genus));

                }

            }

            return genera.ToArray();

        }
        public static async Task UpdateGenusInDb(Taxon genus) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Genus SET name=$name, description=$description, family_id=$family_id WHERE id=$genus_id;")) {

                cmd.Parameters.AddWithValue("$name", genus.name);
                cmd.Parameters.AddWithValue("$description", genus.description);
                cmd.Parameters.AddWithValue("$family_id", genus.parent_id);
                cmd.Parameters.AddWithValue("$genus_id", genus.id);

                await Database.ExecuteNonQuery(cmd);

            }

        }
        public static async Task<Species> GetSpeciesFromDb(long speciesId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", speciesId);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return await SpeciesUtils.SpeciesFromDataRow(row);

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

        public static async Task<Taxon[]> GetTaxaFromDb(string name, TaxonRank type) {
            return await TaxonUtils.GetTaxaAsync(name, type);
        } // deprecated
        public static async Task<Taxon> GetTaxonFromDb(long id, TaxonRank type) {

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

            foreach (TaxonRank type in new TaxonRank[] { TaxonRank.Domain, TaxonRank.Kingdom, TaxonRank.Phylum, TaxonRank.Class, TaxonRank.Order, TaxonRank.Family, TaxonRank.Genus, TaxonRank.Species }) {

                Taxon[] taxa = await GetTaxaFromDb(name, type);

                if (taxa.Count() > 0)
                    return taxa[0];

            }

            return null;

        }
        public static async Task<Taxon> GetTaxonFromDb(string name, TaxonRank type) {

            Taxon[] taxa = await GetTaxaFromDb(name, type);

            if (taxa.Count() > 0)
                return taxa[0];

            return null;

        }
        public static async Task<Taxon[]> GetTaxaFromDb(TaxonRank type) {

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
            return await TaxonUtils.GetTaxaAsync(name);
        } // deprecated
        public static async Task<Taxon[]> GetSubTaxaFromDb(Taxon parentTaxon) {
            return await TaxonUtils.GetSubtaxaAsync(parentTaxon);
        } // deprecated
        public static async Task UpdateTaxonInDb(Taxon taxon, TaxonRank type) {

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
                cmd.Parameters.AddWithValue("$common_name", string.IsNullOrEmpty(taxon.CommonName) ? "" : taxon.CommonName.ToLower());

                if (!string.IsNullOrEmpty(parent_column_name) && taxon.parent_id != -1) {
                    cmd.Parameters.AddWithValue("$parent_column_name", parent_column_name);
                    cmd.Parameters.AddWithValue("$parent_id", taxon.parent_id);
                }

                await Database.ExecuteNonQuery(cmd);

            }

        }
        public static async Task AddTaxonToDb(Taxon taxon, TaxonRank type) {

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
            return await TaxonUtils.GetSpeciesAsync(taxon);
        } // deprecated
        public static async Task<long> CountSpeciesInTaxonFromDb(Taxon taxon) {

            long species_count = 0;

            if (taxon.type == TaxonRank.Species) {

                // If a species was passed in, count it as a single species.
                species_count += 1;

            }
            else if (taxon.type == TaxonRank.Genus) {

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

            TaxonSet set = new TaxonSet {

                Species = new Taxon(TaxonRank.Species) {
                    id = sp.Id,
                    name = sp.Name,
                    description = sp.Description
                },

                Genus = await GetTaxonFromDb(sp.GenusId, TaxonRank.Genus)

            };

            if (set.Genus != null)
                set.Family = await GetTaxonFromDb(set.Genus.parent_id, TaxonRank.Family);

            if (set.Family != null)
                set.Order = await GetTaxonFromDb(set.Family.parent_id, TaxonRank.Order);

            if (set.Order != null)
                set.Class = await GetTaxonFromDb(set.Order.parent_id, TaxonRank.Class);

            if (set.Class != null)
                set.Phylum = await GetTaxonFromDb(set.Class.parent_id, TaxonRank.Phylum);

            if (set.Phylum != null)
                set.Kingdom = await GetTaxonFromDb(set.Phylum.parent_id, TaxonRank.Kingdom);

            if (set.Kingdom != null)
                set.Domain = await GetTaxonFromDb(set.Kingdom.parent_id, TaxonRank.Domain);

            return set;

        }

        public static async Task AddRoleToDb(Role role) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Roles(name, description) VALUES($name, $description);")) {

                cmd.Parameters.AddWithValue("$name", role.name.ToLower());
                cmd.Parameters.AddWithValue("$description", role.description);

                await Database.ExecuteNonQuery(cmd);

            }

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

            return GenerateSpeciesName(species.GenusName, species.Name);

        }

        public static string Strikeout(string str) {

            return string.Format("~~{0}~~", str);

        }
        public static async Task UpdateSpeciesDescription(Species species, string description) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET description=$description WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", species.Id);
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

        public static async Task<Species> ReplyFindSpeciesAsync(ICommandContext context, string genus, string species) {
            return await ReplyAsync_FindSpecies(context, genus, species, null);
        }
        public static async Task<Species> ReplyAsync_FindSpecies(ICommandContext context, string genus, string species, Func<ConfirmSuggestionArgs, Task> onConfirmSuggestion) {

            Species[] sp_list = await GetSpeciesFromDb(genus, species);

            if (sp_list.Count() <= 0) {

                // The species could not be found. Check all species to find a suggestion.
                await ReplyAsync_SpeciesSuggestions(context, genus, species, onConfirmSuggestion);

                return null;

            }
            else if (sp_list.Count() > 1) {

                await ReplyAsync_MatchingSpecies(context, sp_list);
                return null;

            }

            return sp_list[0];

        }

        public class ConfirmSuggestionArgs {

            public ConfirmSuggestionArgs(string suggestion) {
                Suggestion = suggestion;
            }

            public string Suggestion { get; }

        }

        public static async Task ReplyAsync_SpeciesSuggestions(ICommandContext context, string genus, string species) {
            await ReplyAsync_SpeciesSuggestions(context, genus, species, null);
        }
        public static async Task ReplyAsync_SpeciesSuggestions(ICommandContext context, string genus, string species, Func<ConfirmSuggestionArgs, Task> onConfirmSuggestion) {

            List<Species> sp_list = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species;")) {

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        sp_list.Add(await SpeciesUtils.SpeciesFromDataRow(row));

            }

            int min_dist = int.MaxValue;
            string suggestion = string.Empty;

            foreach (Species sp in sp_list) {

                int dist = StringUtilities.GetLevenshteinDistance(species, sp.Name);

                if (dist < min_dist) {
                    min_dist = dist;
                    suggestion = sp.ShortName;
                }

            }

            await ReplyAsync_NoSuchSpeciesExists(context, suggestion, onConfirmSuggestion);

        }

        public static async Task ReplyAsync_NoSuchSpeciesExists(ICommandContext context) {
            await ReplyAsync_NoSuchSpeciesExists(context, "");
        }
        public static async Task ReplyAsync_NoSuchSpeciesExists(ICommandContext context, string suggestion) {
            await ReplyAsync_NoSuchSpeciesExists(context, suggestion, null);
        }
        public static async Task ReplyAsync_NoSuchSpeciesExists(ICommandContext context, string suggestion, Func<ConfirmSuggestionArgs, Task> onConfirmSuggestion) {

            StringBuilder sb = new StringBuilder();

            sb.Append("No such species exists.");

            if (!string.IsNullOrEmpty(suggestion))
                sb.Append(string.Format(" Did you mean **{0}**?", suggestion));

            Bot.PaginatedMessageBuilder message_content = new Bot.PaginatedMessageBuilder {
                Message = sb.ToString(),
                Restricted = true
            };

            if (onConfirmSuggestion != null && !string.IsNullOrEmpty(suggestion)) {

                message_content.AddReaction(Bot.PaginatedMessageReaction.Yes);
                message_content.SetCallback(async (args) => {

                    if (args.ReactionType == Bot.PaginatedMessageReaction.Yes) {

                        args.PaginatedMessage.Enabled = false;

                        await onConfirmSuggestion(new ConfirmSuggestionArgs(suggestion));

                    }

                });

            }

            await Bot.DiscordUtils.SendMessageAsync(context, message_content.Build(), respondToSenderOnly: true);

        }
        public static async Task ReplyAsync_MatchingSpecies(ICommandContext context, Species[] speciesList) {

            EmbedBuilder embed = new EmbedBuilder();
            List<string> lines = new List<string>();

            embed.WithTitle(string.Format("Matching species ({0})", speciesList.Count()));

            foreach (Species sp in speciesList)
                lines.Add(sp.FullName);

            embed.WithDescription(string.Join(Environment.NewLine, lines));

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task<bool> ReplyValidateSpeciesAsync(ICommandContext context, Species species) {

            if (species is null || species.Id < 0) {

                await ReplyAsync_NoSuchSpeciesExists(context);

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyValidateSpeciesAsync(ICommandContext context, Species[] speciesList) {

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
        public static async Task<bool> ReplyValidateZoneAsync(ICommandContext context, Zone zone) {

            if (!ZoneUtils.ZoneIsValid(zone)) {

                await context.Channel.SendMessageAsync("No such zone exists.");

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyValidateZoneTypeAsync(ICommandContext context, ZoneType zoneType) {

            if (!ZoneUtils.ZoneTypeIsValid(zoneType)) {

                await context.Channel.SendMessageAsync("No such zone type exists.");

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyAsync_ValidateGenus(ICommandContext context, Taxon genus) {

            if (genus is null || genus.id <= 0) {

                await context.Channel.SendMessageAsync("No such genus exists.");

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyAsync_ValidateTaxon(ICommandContext context, Taxon taxon) {

            return await ReplyAsync_ValidateTaxon(context, taxon.type, taxon);

        }
        public static async Task<bool> ReplyAsync_ValidateTaxon(ICommandContext context, TaxonRank type, Taxon taxon) {

            return await ReplyAsync_ValidateTaxonWithSuggestion(context, type, taxon, string.Empty);

        }
        public static async Task<bool> ReplyAsync_ValidateTaxonWithSuggestion(ICommandContext context, TaxonRank type, Taxon taxon, string nameForSuggestions) {

            if (taxon is null || taxon.id <= 0) {

                // The taxon does not exist-- Get some suggestions to present to the user.

                Taxon suggestion = string.IsNullOrEmpty(nameForSuggestions) ? null : await GetTaxonSuggestionAsync(type, nameForSuggestions);

                await context.Channel.SendMessageAsync(string.Format("No such {0} exists.{1}",
                    Taxon.GetRankName(type),
                    suggestion is null ? "" : string.Format(" Did you mean **{0}**?", suggestion.GetName())));

                return false;

            }

            return true;

        }
        public static bool ValidateTaxa(Taxon[] taxa) {

            if (taxa is null || taxa.Count() != 1)
                return false;

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

                SortedDictionary<TaxonRank, List<Taxon>> taxa_dict = new SortedDictionary<TaxonRank, List<Taxon>>();

                foreach (Taxon taxon in taxa) {

                    if (!taxa_dict.ContainsKey(taxon.type))
                        taxa_dict[taxon.type] = new List<Taxon>();

                    taxa_dict[taxon.type].Add(taxon);

                }

                EmbedBuilder embed = new EmbedBuilder();

                if (taxa_dict.Keys.Count() > 1)
                    embed.WithTitle(string.Format("Matching taxa ({0})", taxa.Count()));

                foreach (TaxonRank type in taxa_dict.Keys) {

                    taxa_dict[type].Sort((lhs, rhs) => lhs.name.CompareTo(rhs.name));

                    StringBuilder field_content = new StringBuilder();

                    foreach (Taxon taxon in taxa_dict[type])
                        field_content.AppendLine(type == TaxonRank.Species ? (await GetSpeciesFromDb(taxon.id)).ShortName : taxon.GetName());

                    embed.AddField(string.Format("{0}{1} ({2})",
                        taxa_dict.Keys.Count() == 1 ? "Matching " : "",
                        taxa_dict.Keys.Count() == 1 ? Taxon.GetRankName(type, true).ToLower() : StringUtilities.ToTitleCase(Taxon.GetRankName(type, true)), taxa_dict[type].Count()),
                        field_content.ToString());

                }

                await context.Channel.SendMessageAsync("", false, embed.Build());

                return false;

            }

            return true;

        }
        public static async Task<bool> ReplyIsImageUrlValidAsync(ICommandContext context, string imageUrl) {

            if (!GalleryUtils.IsImageUrl(imageUrl)) {

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
            embed.WithColor(Color.Orange);

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task ReplyAsync_Error(ICommandContext context, string text) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(string.Format("❌ {0}", text));
            embed.WithColor(Color.Red);

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task ReplyAsync_Success(ICommandContext context, string text) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(string.Format("✅ {0}", text));
            embed.WithColor(Color.Green);

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task ReplyAsync_Info(ICommandContext context, string text) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(text);
            embed.WithColor(Color.LightGrey);

            await context.Channel.SendMessageAsync("", false, embed.Build());

        }

        public static async Task<bool> ReplyHasPrivilegeAsync(ICommandContext context, IOurFoodChainBotConfiguration botConfiguration, PrivilegeLevel level) {
            return await ReplyHasPrivilegeAsync(context, botConfiguration, context.User, level);
        }
        public static async Task<bool> ReplyHasPrivilegeAsync(ICommandContext context, IOurFoodChainBotConfiguration botConfiguration, IUser user, PrivilegeLevel level) {

            if (botConfiguration.HasPrivilegeLevel(user, level))
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
        public static async Task<bool> ReplyHasPrivilegeOrOwnershipAsync(ICommandContext context, IOurFoodChainBotConfiguration botConfiguration, PrivilegeLevel level, Species species) {
            return await ReplyHasPrivilegeOrOwnershipAsync(context, botConfiguration, context.User, level, species);
        }
        public static async Task<bool> ReplyHasPrivilegeOrOwnershipAsync(ICommandContext context, IOurFoodChainBotConfiguration botConfiguration, IUser user, PrivilegeLevel level, Species species) {

            if (user.Id == (ulong)species.OwnerUserId)
                return true;

            return await ReplyHasPrivilegeAsync(context, botConfiguration, user, level);

        }

        public static async Task Command_ShowTaxon(ICommandContext context, TaxonRank type) {

            // If no taxon name was provided, list everything under the taxon.

            Taxon[] all_taxa = await GetTaxaFromDb(type);
            List<string> items = new List<string>();
            int taxon_count = 0;

            foreach (Taxon taxon in all_taxa) {

                // Count the number of items under this taxon.

                int sub_taxa_count = (await GetSubTaxaFromDb(taxon)).Count();

                if (sub_taxa_count <= 0)
                    continue;

                items.Add(string.Format("{0} ({1})", StringUtilities.ToTitleCase(taxon.name), sub_taxa_count));

                ++taxon_count;

            }

            string title = string.Format("All {0} ({1})", Taxon.GetRankName(type, plural: true), taxon_count);
            List<EmbedBuilder> embed_pages = EmbedUtils.ListToEmbedPages(items, fieldName: title);

            Bot.PaginatedMessageBuilder embed = new Bot.PaginatedMessageBuilder(embed_pages);

            if (embed_pages.Count <= 0) {

                embed.SetTitle(title);
                embed.SetDescription(string.Format("No {0} have been added yet.", Taxon.GetRankName(type, plural: true)));

            }
            else
                embed.AppendFooter(string.Format(" — Empty {0} are not listed.", Taxon.GetRankName(type, plural: true)));

            await Bot.DiscordUtils.SendMessageAsync(context, embed.Build());

        }
        public static async Task Command_ShowTaxon(ICommandContext context, IOurFoodChainBotConfiguration botConfiguration, TaxonRank type, string name) {

            if (string.IsNullOrEmpty(name))
                await Command_ShowTaxon(context, type);

            else {

                // Get the specified taxon.

                Taxon taxon = await GetTaxonFromDb(name, type);

                if (!await ReplyAsync_ValidateTaxonWithSuggestion(context, type, taxon, name))
                    return;

                List<string> items = new List<string>();

                if (taxon.type == TaxonRank.Species) {

                    Species species = await SpeciesUtils.GetSpeciesAsync(taxon.id);

                    await Bot.Modules.SpeciesModule.ShowSpeciesInfoAsync(context, botConfiguration, species);

                    return;

                }
                else if (taxon.type == TaxonRank.Genus) {

                    // For genera, get all species underneath it.
                    // This will let us check if the species is extinct, and cross it out if that's the case.

                    Species[] species = await GetSpeciesInTaxonFromDb(taxon);

                    Array.Sort(species, (lhs, rhs) => lhs.Name.ToLower().CompareTo(rhs.Name.ToLower()));

                    foreach (Species s in species)
                        if (s.IsExtinct)
                            items.Add(string.Format("~~{0}~~", s.Name.ToLower()));
                        else
                            items.Add(s.Name.ToLower());

                }
                else {

                    // Get all subtaxa under this taxon.
                    Taxon[] subtaxa = await GetSubTaxaFromDb(taxon);

                    // Add all subtaxa to the list.

                    foreach (Taxon t in subtaxa) {

                        if (t.type == TaxonRank.Species)
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
                                Taxon.TypeToDatabaseTableName(t.GetChildRank()),
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

                string title = string.IsNullOrEmpty(taxon.CommonName) ? taxon.GetName() : string.Format("{0} ({1})", taxon.GetName(), taxon.GetCommonName());
                string field_title = string.Format("{0} in this {1} ({2}):", StringUtilities.ToTitleCase(Taxon.GetRankName(Taxon.TypeToChildType(type), plural: true)), Taxon.GetRankName(type), items.Count());
                string thumbnail_url = taxon.pics;

                StringBuilder description = new StringBuilder();
                description.AppendLine(taxon.GetDescriptionOrDefault());

                if (items.Count() <= 0) {

                    description.AppendLine();
                    description.AppendLine(string.Format("This {0} contains no {1}.", Taxon.GetRankName(type), Taxon.GetRankName(Taxon.TypeToChildType(type), plural: true)));

                }

                List<EmbedBuilder> embed_pages = EmbedUtils.ListToEmbedPages(items, fieldName: field_title);
                Bot.PaginatedMessageBuilder embed = new Bot.PaginatedMessageBuilder(embed_pages);

                embed.SetTitle(title);
                embed.SetThumbnailUrl(thumbnail_url);
                embed.SetDescription(description.ToString());

                if (items.Count() > 0 && taxon.type != TaxonRank.Genus)
                    embed.AppendFooter(string.Format(" — Empty {0} are not listed.", Taxon.GetRankName(taxon.GetChildRank(), plural: true)));

                await Bot.DiscordUtils.SendMessageAsync(context, embed.Build());

            }

        }
        public static async Task Command_AddTaxon(ICommandContext context, IOurFoodChainBotConfiguration botConfiguration, TaxonRank type, string name, string description) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await ReplyHasPrivilegeAsync(context, botConfiguration, PrivilegeLevel.ServerModerator))
                return;

            // Make sure that the taxon does not already exist before trying to add it.

            Taxon taxon = await GetTaxonFromDb(name, type);

            if (!(taxon is null)) {

                await ReplyAsync_Warning(context, string.Format("The {0} **{1}** already exists.", Taxon.GetRankName(type), taxon.GetName()));

                return;

            }

            taxon = new Taxon(type) {
                name = name,
                description = description
            };

            await AddTaxonToDb(taxon, type);

            await ReplyAsync_Success(context, string.Format("Successfully created new {0}, **{1}**.",
                Taxon.GetRankName(type),
                taxon.GetName()));

        }
        public static async Task Command_SetTaxon(ICommandContext context, IOurFoodChainBotConfiguration botConfiguration, TaxonRank type, string childTaxonName, string parentTaxonName) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await ReplyHasPrivilegeAsync(context, botConfiguration, PrivilegeLevel.ServerModerator))
                return;

            // Get the specified child taxon.

            Taxon child = await GetTaxonFromDb(childTaxonName, Taxon.TypeToChildType(type));

            if (!await ReplyAsync_ValidateTaxonWithSuggestion(context, Taxon.TypeToChildType(type), child, childTaxonName))
                return;

            // Get the specified parent taxon.

            Taxon parent = await GetTaxonFromDb(parentTaxonName, type);

            if (!await ReplyAsync_ValidateTaxonWithSuggestion(context, type, parent, parentTaxonName))
                return;

            // Update the taxon.

            child.parent_id = parent.id;

            await UpdateTaxonInDb(child, Taxon.TypeToChildType(type));

            await ReplyAsync_Success(context, string.Format("{0} **{1}** has sucessfully been placed under the {2} **{3}**.",
                    StringUtilities.ToTitleCase(Taxon.GetRankName(Taxon.TypeToChildType(type))),
                    child.GetName(),
                    Taxon.GetRankName(type),
                    parent.GetName()
                ));

        }
        public static async Task Command_SetTaxonDescription(ICommandContext context, IOurFoodChainBotConfiguration botConfiguration, Taxon taxon, string description) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await ReplyHasPrivilegeAsync(context, botConfiguration, PrivilegeLevel.ServerModerator))
                return;

            taxon.description = description;

            await UpdateTaxonInDb(taxon, taxon.type);

            string success_message = string.Format("Successfully updated description for {0} **{1}**.", Taxon.GetRankName(taxon.type), taxon.GetName());

            await ReplyAsync_Success(context, success_message);


        }
        public static async Task Command_SetTaxonDescription(ICommandContext context, IOurFoodChainBotConfiguration botConfiguration, TaxonRank type, string name) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await ReplyHasPrivilegeAsync(context, botConfiguration, PrivilegeLevel.ServerModerator))
                return;

            // Since the description wasn't provided directly, initiate a multistage update.

            Taxon taxon = await GetTaxonFromDb(name, type);

            if (!await ReplyAsync_ValidateTaxonWithSuggestion(context, type, taxon, name))
                return;

            Bot.MultiPartMessage p = new Bot.MultiPartMessage(context) {
                UserData = new string[] { name },
                Callback = async (args) => {

                    await BotUtils.Command_SetTaxonDescription(args.Message.Context, botConfiguration, taxon, args.ResponseContent);

                }
            };

            await Bot.DiscordUtils.SendMessageAsync(context, p,
                string.Format("Reply with the description for {0} **{1}**.\nTo cancel the update, reply with \"cancel\".", taxon.GetTypeName(), taxon.GetName()));


        }
        public static async Task Command_SetTaxonDescription(ICommandContext context, IOurFoodChainBotConfiguration botConfiguration, TaxonRank type, string name, string description) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await ReplyHasPrivilegeAsync(context, botConfiguration, PrivilegeLevel.ServerModerator))
                return;

            Taxon taxon = await GetTaxonFromDb(name, type);

            if (!await ReplyAsync_ValidateTaxonWithSuggestion(context, type, taxon, name))
                return;

            await Command_SetTaxonDescription(context, botConfiguration, taxon, description);

        }
        public static async Task Command_SetTaxonPic(ICommandContext context, IOurFoodChainBotConfiguration botConfiguration, Taxon taxon, string url) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await ReplyHasPrivilegeAsync(context, botConfiguration, PrivilegeLevel.ServerModerator))
                return;

            // Ensure that the image URL appears to be valid.
            if (!await ReplyIsImageUrlValidAsync(context, url))
                return;

            taxon.pics = url;

            await UpdateTaxonInDb(taxon, taxon.type);

            string success_message = string.Format("Successfully set the picture for for {0} **{1}**.", Taxon.GetRankName(taxon.type), taxon.GetName());

            await ReplyAsync_Success(context, success_message);

        }
        public static async Task Command_SetTaxonPic(ICommandContext context, IOurFoodChainBotConfiguration botConfiguration, TaxonRank type, string name, string url) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await ReplyHasPrivilegeAsync(context, botConfiguration, PrivilegeLevel.ServerModerator))
                return;

            // Ensure that the image URL appears to be valid.
            if (!await ReplyIsImageUrlValidAsync(context, url))
                return;

            Taxon taxon = await GetTaxonFromDb(name, type);

            if (!await ReplyAsync_ValidateTaxonWithSuggestion(context, type, taxon, name))
                return;

            await Command_SetTaxonPic(context, botConfiguration, taxon, url);

        }
        public static async Task Command_SetTaxonCommonName(ICommandContext context, IOurFoodChainBotConfiguration botConfiguration, TaxonRank type, string name, string commonName) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await ReplyHasPrivilegeAsync(context, botConfiguration, PrivilegeLevel.ServerModerator))
                return;

            Taxon taxon = await GetTaxonFromDb(name, type);

            if (!await ReplyAsync_ValidateTaxonWithSuggestion(context, type, taxon, name))
                return;

            taxon.CommonName = commonName;

            await UpdateTaxonInDb(taxon, type);

            await ReplyAsync_Success(context, string.Format("Members of the {0} **{1}** are now commonly known as **{2}**.",
                Taxon.GetRankName(type),
                taxon.GetName(),
                taxon.GetCommonName()
                ));

        }
        public static async Task<Taxon> GetTaxonSuggestionAsync(TaxonRank type, string name) {

            Taxon[] taxa = await GetTaxaFromDb(type);

            int min_dist = int.MaxValue;
            Taxon suggestion = null;

            foreach (Taxon t in taxa) {

                int dist = StringUtilities.GetLevenshteinDistance(t.name.ToLower(), name.ToLower());

                if (dist < min_dist) {

                    min_dist = dist;
                    suggestion = t;

                }

            }

            return suggestion;

        }

        public static async Task<string> Reply_UploadFileToScratchServerAsync(ICommandContext context, IOurFoodChainBotConfiguration botConfiguration, DiscordSocketClient client, string filePath, bool deleteAfterUpload = false) {

            ulong serverId = botConfiguration.ScratchServer;
            ulong channelId = botConfiguration.ScratchChannel;

            if (serverId <= 0 || channelId <= 0) {

                await ReplyAsync_Error(context, "Cannot upload images because no scratch server/channel has been specified in the configuration file.");

                return string.Empty;

            }

            IGuild guild = client.GetGuild(serverId);

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

            if (deleteAfterUpload)
                IoUtils.TryDeleteFile(filePath);

            return url;

        }

        public static int RandomInteger(int max) {

            return RANDOM.Next(max);

        }
        public static int RandomInteger(int min, int max) {

            return RANDOM.Next(min, max);

        }

        /// <summary>
        /// Given three arguments that may be genus/species/species or species/genus/species, resolves the ambiguity and returns the two species.
        /// </summary>
        /// <param name="context">Current command context.</param>
        /// <param name="arg0">First genus or first species.</param>
        /// <param name="arg1">First species or second genus.</param>
        /// <param name="arg2">Second species.</param>
        /// <returns>The pair of species matched by the query arguments.</returns>
        public static async Task<Resolve3ArgumentFindSpeciesAmbiguityResult> ReplyResolve3ArgumentSpeciesQueryAmbiguityAsync(ICommandContext context, string arg0, string arg1, string arg2) {

            // <genus> <species> <species>

            Species[] query_result = await SpeciesUtils.GetSpeciesAsync(arg0, arg1);
            Species species_1 = null;
            Species species_2 = null;
            Species[] species_2_ambiguous_matches = null;

            if (query_result.Count() > 1) {

                // If the first species is ambiguous even with the genus, it will be without as well.

                await ReplyValidateSpeciesAsync(context, query_result);

                return new Resolve3ArgumentFindSpeciesAmbiguityResult();

            }
            else if (query_result.Count() == 1) {

                species_1 = query_result[0];

                query_result = await SpeciesUtils.GetSpeciesAsync(arg2);

                if (query_result.Count() > 1) {

                    // If the second species is ambiguous, store the query result to show later.
                    // It's possible that it won't be ambiguous on the second attempt, so we won't show it for now.

                    species_2_ambiguous_matches = query_result;

                }
                else if (query_result.Count() == 1) {

                    species_2 = query_result[0];

                    if (species_1 != null && species_2 != null)
                        return new Resolve3ArgumentFindSpeciesAmbiguityResult {
                            Case = Resolve3ArgumentFindSpeciesAmbiguityCase.GenusSpeciesSpecies,
                            Species1 = species_1,
                            Species2 = species_2
                        };

                }

            }

            // <species> <genus> <species>

            query_result = await SpeciesUtils.GetSpeciesAsync(arg0);

            if (query_result.Count() > 1) {

                // If the first species is ambiguous, there's nothing we can do.

                await ReplyValidateSpeciesAsync(context, query_result);

                return new Resolve3ArgumentFindSpeciesAmbiguityResult();

            }
            else if (query_result.Count() == 1) {

                // In this case, we will show if the second species is ambiguous, as there are no further cases to check.

                species_1 = query_result[0];

                query_result = await SpeciesUtils.GetSpeciesAsync(arg1, arg2);

                if (query_result.Count() > 1) {

                    await ReplyValidateSpeciesAsync(context, query_result);

                    return new Resolve3ArgumentFindSpeciesAmbiguityResult();

                }
                else if (query_result.Count() == 1) {

                    species_2 = query_result[0];

                    return new Resolve3ArgumentFindSpeciesAmbiguityResult {
                        Case = Resolve3ArgumentFindSpeciesAmbiguityCase.SpeciesGenusSpecies,
                        Species1 = species_1,
                        Species2 = species_2
                    };

                }

            }

            // If we get here, we were not able to unambiguously figure out what the intended species are, or one of them didn't exist.

            if (species_1 is null && species_2 is null)
                await ReplyAsync_Error(context, "The given species could not be determined.");
            else if (species_1 is null)
                await ReplyAsync_Error(context, "The first species could not be determined.");
            else if (species_2 is null) {

                if (species_2_ambiguous_matches != null)
                    await ReplyValidateSpeciesAsync(context, species_2_ambiguous_matches);
                else
                    await ReplyAsync_Error(context, "The second species could not be determined.");

            }

            return new Resolve3ArgumentFindSpeciesAmbiguityResult();

        }

        public static async Task ZonesToEmbedPagesAsync(Bot.PaginatedMessageBuilder embed, Zone[] zones, bool showIcon = true) {

            List<string> lines = new List<string>();
            int zones_per_page = 20;
            int max_line_length = Math.Min(showIcon ? 78 : 80, (Bot.DiscordUtils.MaxEmbedLength - embed.Length) / zones_per_page);

            foreach (Zone zone in zones) {

                ZoneType type = await ZoneUtils.GetZoneTypeAsync(zone.ZoneTypeId);

                string line = string.Format("{1} **{0}**\t-\t{2}", StringUtilities.ToTitleCase(zone.Name), showIcon ? (type is null ? new ZoneType() : type).Icon : "", zone.GetShortDescription());

                if (line.Length > max_line_length)
                    line = line.Substring(0, max_line_length - 3) + "...";

                lines.Add(line);

            }

            embed.AddPages(EmbedUtils.LinesToEmbedPages(lines, 20));

        }

        public static async Task<string> TimestampToDateStringAsync(long timestamp, IOurFoodChainBotConfiguration botConfiguration, TimestampToDateStringFormat format = TimestampToDateStringFormat.Default) {

            if (botConfiguration.GenerationsEnabled) {

                Generation gen = await GenerationUtils.GetGenerationByTimestampAsync(timestamp);

                return gen is null ? "Gen ???" : gen.Name;

            }

            return DateUtils.TimestampToDateString(timestamp, format);

        }

    }

}