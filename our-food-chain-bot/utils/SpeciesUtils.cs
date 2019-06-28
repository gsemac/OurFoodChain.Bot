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

                cmd.Parameters.AddWithValue("$species_id", species.id);

                using (DataTable table = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in table.Rows)
                        common_names.Add(new CommonName(row.Field<string>("name")));

            }

            // Add the common name from the "Species" table if one was provided.

            if (!string.IsNullOrEmpty(species.commonName))
                common_names.Add(new CommonName(species.commonName));

            // Return a sorted array of common names without any duplicates.
            return common_names.GroupBy(x => x.Value).Select(x => x.First()).OrderBy(x => x.Value).ToArray();

        }

        public static async Task<Species[]> GetSpeciesAsync(string name) {

            GenusSpeciesPair input = _parseGenusAndSpeciesFromUserInput(string.Empty, name);

            if (string.IsNullOrEmpty(input.GenusName)) {

                // Returns species by name and/or common name.

                List<Species> species = new List<Species>();

                if (!string.IsNullOrEmpty(name)) {

                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE name = $name OR common_name = $name OR id IN (SELECT species_id FROM SpeciesCommonNames where name = $name)")) {

                        cmd.Parameters.AddWithValue("$name", input.SpeciesName.ToLower());

                        using (DataTable table = await Database.GetRowsAsync(cmd))
                            foreach (DataRow row in table.Rows)
                                species.Add(await Species.FromDataRow(row));

                    }

                }

                return species.ToArray();

            }
            else {

                return await GetSpeciesAsync(input.GenusName, input.SpeciesName);

            }

        }
        public static async Task<Species[]> GetSpeciesAsync(string genus, string species) {

            GenusSpeciesPair input = _parseGenusAndSpeciesFromUserInput(genus, species);

            // If the species is the empty string, don't bother trying to find any matches.
            // This prevents species with an empty, but non-null common name (set to "") from being returned.

            if (string.IsNullOrEmpty(input.SpeciesName))
                return new Species[] { };

            if (string.IsNullOrEmpty(input.GenusName)) {

                // If the user didn't pass in a genus, we'll look up the species by name (name or common name).
                return await GetSpeciesAsync(input.SpeciesName);

            }
            else if (input.IsAbbreviated) {

                // If the genus is abbreviated (e.g. "Cornu" -> "C.") but not null, we'll look for matching species and determine the genus afterwards.
                // Notice that this case does not allow for looking up the species by common name, which should not occur when the genus is included.

                Species[] result = await GetSpeciesAsync(input.SpeciesName);

                return result.Where(x => !string.IsNullOrEmpty(x.genus) && x.genus.ToLower().StartsWith(input.GenusName.ToLower())).ToArray();

            }
            else {

                // If we've been given full genus and species names, we can attempt to get the species directly.
                // Although genera can have common names, only allow the genus to be looked up by its scientific name. Generally users wouldn't use the common name in this context.

                Species species_result = await _getSpeciesByGenusAndSpeciesNameAsync(input.GenusName, input.SpeciesName);

                return (species_result is null) ? new Species[] { } : new Species[] { species_result };

            }

        }
        public static async Task<Species> GetSpeciesAsync(long speciesId) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", speciesId);

                DataRow row = await Database.GetRowAsync(cmd);

                return row is null ? null : await Species.FromDataRow(row);

            }

        }

        public static async Task<Species[]> GetPredatorsAsync(Species species) {

            List<Species> result = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM Predates WHERE eats_id = $species_id)")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);

                using (DataTable table = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in table.Rows)
                        result.Add(await Species.FromDataRow(row));

            }

            return result.ToArray();

        }
        public static async Task<Species[]> GetPreyAsync(Species species) {

            List<Species> result = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT eats_id FROM Predates WHERE species_id = $species_id)")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);

                using (DataTable table = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in table.Rows)
                        result.Add(await Species.FromDataRow(row));

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

            return ancestor_ids.ToArray();

        }
        public static async Task<Species> GetAncestorAsync(Species species) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT ancestor_id FROM Ancestors WHERE species_id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);

                DataRow row = await Database.GetRowAsync(cmd);

                return row is null ? null : await GetSpeciesAsync(row.Field<long>("ancestor_id"));

            }

        }
        public static async Task<Species[]> GetAncestorsAsync(Species species) {

            List<Species> ancestor_species = new List<Species>();

            foreach (long species_id in await GetAncestorIdsAsync(species.id))
                ancestor_species.Add(await GetSpeciesAsync(species_id));

            return ancestor_species.ToArray();

        }

        public static async Task AddCommonName(Species species, string commonName, bool overwriteSpeciesTable) {

            commonName = _formatCommonNameForDatabase(commonName);

            // Insert the common name into the dedicated common name table.

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO SpeciesCommonNames(species_id, name) VALUES($species_id, $name)")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);
                cmd.Parameters.AddWithValue("$name", commonName);

                await Database.ExecuteNonQuery(cmd);

            }

            // If the species doesn't already have a common name set in the "Species" table, update it there.

            if (overwriteSpeciesTable || string.IsNullOrEmpty(species.commonName))
                await _setCommonNameInSpeciesTable(species, commonName);

        }
        public static async Task RemoveCommonName(Species species, string commonName) {

            commonName = _formatCommonNameForDatabase(commonName);

            // Remove the common name from the dedicated common name table.

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM SpeciesCommonNames WHERE name = $common_name AND species_id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);
                cmd.Parameters.AddWithValue("$common_name", commonName);

                await Database.ExecuteNonQuery(cmd);

            }

            // If this name is also recorded in the "Species" table, remove it from there as well.

            if (commonName == species.commonName.ToLower())
                await _setCommonNameInSpeciesTable(species, string.Empty);

        }
        public static async Task RemoveCommonNames(Species species) {

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM SpeciesCommonNames WHERE species_id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await _setCommonNameInSpeciesTable(species, "");

        }

        public static async Task<SpeciesZone[]> GetZones(Species species) {

            List<SpeciesZone> zones = new List<SpeciesZone>();

            using (SQLiteConnection conn = await Database.GetConnectionAsync())
            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM SpeciesZones WHERE species_id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);

                using (DataTable rows = await Database.GetRowsAsync(conn, cmd))
                    foreach (DataRow row in rows.Rows) {

                        Zone zone = await ZoneUtils.GetZoneAsync(row.Field<long>("zone_id"));

                        if (zone is null)
                            continue;

                        zones.Add(new SpeciesZone {
                            Zone = zone,
                            Notes = row.Field<string>("notes")
                        });

                    }

            }

            return zones.ToArray();

        }
        public static async Task AddZones(Species species, Zone[] zones) {
            await AddZones(species, zones, string.Empty);
        }
        public static async Task AddZones(Species species, Zone[] zones, string notes) {

            foreach (Zone zone in zones) {

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO SpeciesZones(species_id, zone_id, notes) VALUES($species_id, $zone_id, $notes)")) {

                    cmd.Parameters.AddWithValue("$species_id", species.id);
                    cmd.Parameters.AddWithValue("$zone_id", zone.id);
                    cmd.Parameters.AddWithValue("$notes", string.IsNullOrEmpty(notes) ? "" : notes.Trim().ToLower());

                    await Database.ExecuteNonQuery(cmd);

                }

            }

        }
        public static async Task RemoveZones(Species species, Zone[] zones) {

            foreach (Zone zone in zones) {

                using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM SpeciesZones WHERE species_id = $species_id AND zone_id = $zone_id")) {

                    cmd.Parameters.AddWithValue("$species_id", species.id);
                    cmd.Parameters.AddWithValue("$zone_id", zone.id);

                    await Database.ExecuteNonQuery(cmd);

                }

            }

        }

        private static GenusSpeciesPair _parseGenusAndSpeciesFromUserInput(string inputGenus, string inputSpecies) {

            string genus = inputGenus;
            string species = inputSpecies;

            if (string.IsNullOrEmpty(genus) && species.Contains('.')) {

                // If the genus is empty but the species contains a period, assume everything to the left of the period is the genus.
                // This allows us to process inputs like "C.aspersum" where the user forgot to put a space between the genus and species names.

                int split_index = species.IndexOf('.');

                genus = species.Substring(0, split_index);
                species = species.Substring(split_index, species.Length - split_index);

            }

            // Strip all periods from the genus/species names.
            // This allows us to process inputs like "c aspersum" and "c. asperum" in the same way.
            // At the same time, convert to lowercase to match how the values are stored in the database, and trim any excess whitespace.

            if (!string.IsNullOrEmpty(genus))
                genus = genus.Trim().Trim('.').ToLower();

            if (!string.IsNullOrEmpty(species))
                species = species.Trim().Trim('.').ToLower();

            return new GenusSpeciesPair {
                GenusName = genus,
                SpeciesName = species
            };

        }
        private static async Task<Species> _getSpeciesByGenusAndSpeciesNameAsync(string genus, string species) {

            Genus genus_info = await BotUtils.GetGenusFromDb(genus);

            // If the genus doesn't exist, the species cannot possibly exist either.

            if (genus_info is null)
                return null;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE genus_id = $genus_id AND name = $species")) {

                cmd.Parameters.AddWithValue("$genus_id", genus_info.id);
                cmd.Parameters.AddWithValue("$species", species.ToLower());

                DataRow result = await Database.GetRowAsync(cmd);

                if (result is null)
                    return null;

                return await Species.FromDataRow(result, genus_info);

            }

        }

        private static string _formatCommonNameForDatabase(string commonName) {

            if (string.IsNullOrEmpty(commonName))
                return "";

            return commonName.Trim().ToLower();

        }
        private static async Task _setCommonNameInSpeciesTable(Species species, string commonName) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET common_name = $common_name WHERE id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);
                cmd.Parameters.AddWithValue("$common_name", _formatCommonNameForDatabase(commonName));

                await Database.ExecuteNonQuery(cmd);

            }

        }

    }

}