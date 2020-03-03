using Discord;
using Discord.Commands;
using OurFoodChain.Common;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public static class SpeciesUtils {

        // Ideally, all utility functions related to species should be located here.

        public static async Task<CommonName[]> GetCommonNamesAsync(Species species) {

            List<CommonName> common_names = new List<CommonName>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT name FROM SpeciesCommonNames WHERE species_id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", species.Id);

                using (DataTable table = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in table.Rows)
                        common_names.Add(new CommonName(row.Field<string>("name")));

            }

            // Add the common name from the "Species" table if one was provided.

            if (!string.IsNullOrEmpty(species.CommonName))
                common_names.Add(new CommonName(species.CommonName));

            // Return a sorted array of common names without any duplicates.
            return common_names.GroupBy(x => x.Value).Select(x => x.First()).OrderBy(x => x.Value).ToArray();

        }

        public static async Task<Species[]> GetSpeciesAsync() {

            List<Species> species = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species"))
            using (DataTable table = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in table.Rows)
                    species.Add(await SpeciesUtils.SpeciesFromDataRow(row));

            return species.ToArray();

        }
        public static async Task<Species[]> GetSpeciesAsync(string name) {

            // We don't know for sure if the user passed in a binomial name or a common name/species name.
            // If the input is a valid binomial name, we'll use it to find the requested species.
            // Otherwise, or if the first attempt doesn't return a match, we'll treat it as a common name/species name.

            BinomialName input = BinomialName.Parse(name);

            List<Species> result = new List<Species>();

            if (!string.IsNullOrEmpty(input.Genus)) {

                // Attempt to get the species using name/genus.

                result.AddRange(await GetSpeciesAsync(input.Genus, input.Species));

            }

            if (result.Count() <= 0) {

                // Attempt to get the species by common name/species name.

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE name = $name OR common_name = $name OR id IN (SELECT species_id FROM SpeciesCommonNames where name = $name)")) {

                    cmd.Parameters.AddWithValue("$name", name.ToLowerInvariant());

                    using (DataTable table = await Database.GetRowsAsync(cmd))
                        foreach (DataRow row in table.Rows)
                            result.Add(await SpeciesFromDataRow(row));

                }

            }

            return result.ToArray();

        }
        public static async Task<Species[]> GetSpeciesAsync(string genus, string species) {

            BinomialName input = BinomialName.Parse(genus, species);

            // If the species is the empty string, don't bother trying to find any matches.
            // This prevents species with an empty, but non-null common name (set to "") from being returned.

            if (string.IsNullOrEmpty(input.Species))
                return Array.Empty<Species>();

            if (string.IsNullOrEmpty(input.Genus)) {

                // If the user didn't pass in a genus, we'll look up the species by name (name or common name).
                return await GetSpeciesAsync(input.Species);

            }
            else if (input.IsAbbreviated) {

                // If the genus is abbreviated (e.g. "Cornu" -> "C.") but not null, we'll look for matching species and determine the genus afterwards.
                // Notice that this case does not allow for looking up the species by common name, which should not occur when the genus is included.

                Species[] result = await GetSpeciesAsync(input.Species);

                return result.Where(x => !string.IsNullOrEmpty(x.GenusName) && x.GenusName.ToLower().StartsWith(input.Genus.ToLower())).ToArray();

            }
            else {

                // If we've been given full genus and species names, we can attempt to get the species directly.
                // Although genera can have common names, only allow the genus to be looked up by its scientific name. Generally users wouldn't use the common name in this context.

                Species species_result = await _getSpeciesByGenusAndSpeciesNameAsync(input.Genus, input.Species);

                return (species_result is null) ? Array.Empty<Species>() : new Species[] { species_result };

            }

        }
        public static async Task<Species> GetSpeciesAsync(long speciesId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", speciesId);

                DataRow row = await Database.GetRowAsync(cmd);

                return row is null ? null : await SpeciesUtils.SpeciesFromDataRow(row);

            }

        }

        public static async Task<Species> GetUniqueSpeciesAsync(string name) {

            Species[] result = await GetSpeciesAsync(name);

            return result.Count() == 1 ? result[0] : null;

        }
        public static async Task<Species> GetUniqueSpeciesAsync(string genus, string species) {

            Species[] result = await GetSpeciesAsync(genus, species);

            return result.Count() == 1 ? result[0] : null;

        }

        public static async Task<int> GetSpeciesCount() {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Species"))
                return (int)await Database.GetScalar<long>(cmd);
        }

        public static async Task<Species[]> GetBaseSpeciesAsync() {

            List<Species> species = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id NOT IN (SELECT species_id FROM Ancestors)"))
            using (DataTable table = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in table.Rows)
                    species.Add(await SpeciesUtils.SpeciesFromDataRow(row));

            return species.ToArray();

        }
        public static async Task<bool> IsBaseSpeciesAsync(Species species) {
            return await GetAncestorAsync(species) is null;
        }

        public static async Task<Species[]> GetPredatorsAsync(Species species) {

            List<Species> result = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM Predates WHERE eats_id = $species_id)")) {

                cmd.Parameters.AddWithValue("$species_id", species.Id);

                using (DataTable table = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in table.Rows)
                        result.Add(await SpeciesFromDataRow(row));

            }

            return result.ToArray();

        }
        public static async Task<PreyInfo[]> GetPreyAsync(Species species) {
            return await GetPreyAsync(species.Id);
        }
        public static async Task<PreyInfo[]> GetPreyAsync(long speciesId) {

            List<PreyInfo> result = new List<PreyInfo>();

            using (SQLiteCommand cmd = new SQLiteCommand(@"SELECT * FROM (SELECT * FROM Species WHERE id IN (SELECT eats_id FROM Predates WHERE species_id = $species_id)) INNER JOIN Predates WHERE eats_id = id AND species_id = $species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", speciesId);

                using (DataTable table = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in table.Rows) {

                        result.Add(new PreyInfo {
                            Prey = await SpeciesFromDataRow(row),
                            Notes = row.Field<string>("notes")
                        });

                    }

            }

            return result.ToArray();

        }

        public static async Task<long[]> GetAncestorIdsAsync(long speciesId) {

            List<long> ancestor_ids = new List<long>();

            while (true) {

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT ancestor_id FROM Ancestors WHERE species_id = $species_id")) {

                    cmd.Parameters.AddWithValue("$species_id", speciesId);

                    DataRow row = await Database.GetRowAsync(cmd);

                    if (row is null)
                        break;

                    speciesId = row.Field<long>("ancestor_id");

                    ancestor_ids.Add(speciesId);

                }

            }

            // Reverse the array so that earliest ancestors are listed first.
            ancestor_ids.Reverse();

            return ancestor_ids.ToArray();

        }
        public static async Task<Species> GetAncestorAsync(Species species) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT ancestor_id FROM Ancestors WHERE species_id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", species.Id);

                DataRow row = await Database.GetRowAsync(cmd);

                return row is null ? null : await GetSpeciesAsync(row.Field<long>("ancestor_id"));

            }

        }
        public static async Task<Species[]> GetAncestorsAsync(Species species) {
            return await GetAncestorsAsync(species.Id);
        }
        public static async Task<Species[]> GetAncestorsAsync(long speciesId) {

            List<Species> ancestor_species = new List<Species>();

            foreach (long species_id in await GetAncestorIdsAsync(speciesId))
                ancestor_species.Add(await GetSpeciesAsync(species_id));

            return ancestor_species.ToArray();

        }

        public static async Task<Species[]> GetDirectDescendantsAsync(Species species) {

            List<Species> result = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM Ancestors WHERE ancestor_id = $ancestor_id)")) {

                cmd.Parameters.AddWithValue("$ancestor_id", species.Id);

                using (DataTable rows = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in rows.Rows)
                        result.Add(await SpeciesUtils.SpeciesFromDataRow(row));

            }

            return result.ToArray();

        }

        public static async Task AddCommonNameAsync(Species species, string commonName, bool overwriteSpeciesTable) {

            commonName = _formatCommonNameForDatabase(commonName);

            // Insert the common name into the dedicated common name table.

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO SpeciesCommonNames(species_id, name) VALUES($species_id, $name)")) {

                cmd.Parameters.AddWithValue("$species_id", species.Id);
                cmd.Parameters.AddWithValue("$name", commonName);

                await Database.ExecuteNonQuery(cmd);

            }

            // If the species doesn't already have a common name set in the "Species" table, update it there.

            if (overwriteSpeciesTable || string.IsNullOrEmpty(species.CommonName))
                await _setCommonNameInSpeciesTable(species, commonName);

        }
        public static async Task RemoveCommonNameAsync(Species species, string commonName) {

            commonName = _formatCommonNameForDatabase(commonName);

            // Remove the common name from the dedicated common name table.

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM SpeciesCommonNames WHERE name = $common_name AND species_id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", species.Id);
                cmd.Parameters.AddWithValue("$common_name", commonName);

                await Database.ExecuteNonQuery(cmd);

            }

            // If this name is also recorded in the "Species" table, remove it from there as well.

            if (commonName == species.CommonName.ToLower())
                await _setCommonNameInSpeciesTable(species, string.Empty);

        }
        public static async Task RemoveCommonNamesAsync(Species species) {

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM SpeciesCommonNames WHERE species_id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", species.Id);

                await Database.ExecuteNonQuery(cmd);

            }

            await _setCommonNameInSpeciesTable(species, "");

        }

        public static async Task SetOwnerAsync(Species species, string ownerName) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET owner = $owner, user_id = NULL WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", species.Id);
                cmd.Parameters.AddWithValue("$owner", ownerName);

                await Database.ExecuteNonQuery(cmd);

            }

        }
        public static async Task SetOwnerAsync(Species species, string ownerName, ulong ownerId) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET owner = $owner, user_id = $user_id WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", species.Id);
                cmd.Parameters.AddWithValue("$owner", ownerName);

                if (ownerId == UserInfo.NullId)
                    cmd.Parameters.AddWithValue("$user_id", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("$user_id", ownerId);

                await Database.ExecuteNonQuery(cmd);

            }

        }

        public static async Task<Role[]> GetRolesAsync(long speciesId) {

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
        public static async Task<Role[]> GetRolesAsync(Species species) {
            return await GetRolesAsync(species.Id);
        }

        public static async Task<ExtinctionInfo> GetExtinctionInfoAsync(Species species) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Extinctions WHERE species_id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", species.Id);

                DataRow row = await Database.GetRowAsync(cmd);

                if (row is null)
                    return new ExtinctionInfo { IsExtinct = false };
                else {

                    return new ExtinctionInfo {
                        IsExtinct = true,
                        Reason = row.Field<string>("reason"),
                        Timestamp = (long)row.Field<decimal>("timestamp")
                    };

                }

            }

        }
        public static async Task SetExtinctionInfoAsync(Species species, ExtinctionInfo extinctionInfo) {

            if (extinctionInfo.IsExtinct) {

                if (!(await GetExtinctionInfoAsync(species)).IsExtinct) {

                    // If the species is not already extinct, add a new extinction record for this species.

                    using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Extinctions(species_id, reason, timestamp) VALUES($species_id, $reason, $timestamp)")) {

                        cmd.Parameters.AddWithValue("$species_id", species.Id);
                        cmd.Parameters.AddWithValue("$reason", extinctionInfo.Reason);
                        cmd.Parameters.AddWithValue("$timestamp", extinctionInfo.Timestamp);

                        await Database.ExecuteNonQuery(cmd);

                    }

                }
                else {

                    // If the species has an existing extinction record, update the description only.

                    using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Extinctions SET reason = $reason WHERE species_id = $species_id")) {

                        cmd.Parameters.AddWithValue("$species_id", species.Id);
                        cmd.Parameters.AddWithValue("$reason", extinctionInfo.Reason);

                        await Database.ExecuteNonQuery(cmd);

                    }

                }

            }
            else {

                // If the species is no longer extinct, delete its extinction record (if it exists).

                using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Extinctions WHERE species_id = $species_id")) {

                    cmd.Parameters.AddWithValue("$species_id", species.Id);

                    await Database.ExecuteNonQuery(cmd);

                }

            }

        }

        public static string FormatSpeciesName(string speciesName, BinomialNameFormat format) {

            string[] words = speciesName.Split(' ')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToArray();

            if (words.Count() <= 1)
                // If the name only contains one word, assume it's the specific epithet.
                return speciesName.ToLower();
            else if (words.Count() > 2)
                // If the name contains more than two words, treat it as a common name.
                return StringUtilities.ToTitleCase(string.Join(" ", words));

            switch (format) {

                case BinomialNameFormat.Full:
                    return string.Format("{0} {1}", StringUtilities.ToTitleCase(words[0]), words[1].ToLower());

                case BinomialNameFormat.Abbreviated:
                    return string.Format("{0}. {1}", StringUtilities.ToTitleCase(words[0]).First(), words[1].ToLower());

            }

            return speciesName;

        }

        public static async Task<Species> SpeciesFromDataRow(DataRow row, Taxon genusInfo) {

            Species species = new Species {
                Id = row.Field<long>("id"),
                GenusId = row.Field<long>("genus_id"),
                Name = row.Field<string>("name"),
                // The genus should never be null, but there was instance where a user manually edited the database and the genus ID was invalid.
                // We should at least try to handle this situation gracefully.
                GenusName = genusInfo is null ? "?" : genusInfo.name,
                Description = row.Field<string>("description"),
                OwnerName = row.Field<string>("owner"),
                Timestamp = (long)row.Field<decimal>("timestamp"),
                CommonName = row.Field<string>("common_name"),
                Picture = row.Field<string>("pics")
            };

            species.OwnerUserId = row.IsNull("user_id") ? -1 : row.Field<long>("user_id");
            species.IsExtinct = false;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Extinctions WHERE species_id=$species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", species.Id);

                if (!(await Database.GetRowAsync(cmd) is null))
                    species.IsExtinct = true;

            }

            return species;

        }
        public static async Task<Species> SpeciesFromDataRow(DataRow row) {

            long genus_id = row.Field<long>("genus_id");
            Taxon genus_info = await BotUtils.GetGenusFromDb(genus_id);

            return await SpeciesFromDataRow(row, genus_info);

        }

        public static async Task<string> GetOwnerOrDefaultAsync(Species species, ICommandContext context) {

            string result = species.OwnerName;

            if (!(context is null || context.Guild is null) && species.OwnerUserId > 0) {

                IUser user = await context.Guild.GetUserAsync((ulong)species.OwnerUserId);

                if (!(user is null))
                    result = user.Username;

            }

            if (string.IsNullOrEmpty(result))
                result = "?";

            return result;

        }

        private static async Task<Species> _getSpeciesByGenusAndSpeciesNameAsync(string genus, string species) {

            Taxon genus_info = await BotUtils.GetGenusFromDb(genus);

            // If the genus doesn't exist, the species cannot possibly exist either.

            if (genus_info is null)
                return null;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE genus_id = $genus_id AND name = $species")) {

                cmd.Parameters.AddWithValue("$genus_id", genus_info.id);
                cmd.Parameters.AddWithValue("$species", species.ToLower());

                DataRow result = await Database.GetRowAsync(cmd);

                if (result is null)
                    return null;

                return await SpeciesFromDataRow(result, genus_info);

            }

        }

        private static string _formatCommonNameForDatabase(string commonName) {

            if (string.IsNullOrEmpty(commonName))
                return "";

            return commonName.Trim().ToLower();

        }
        private static async Task _setCommonNameInSpeciesTable(Species species, string commonName) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET common_name = $common_name WHERE id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", species.Id);
                cmd.Parameters.AddWithValue("$common_name", _formatCommonNameForDatabase(commonName));

                await Database.ExecuteNonQuery(cmd);

            }

        }

    }

}