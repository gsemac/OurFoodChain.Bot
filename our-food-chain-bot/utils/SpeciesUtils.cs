using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public enum SpeciesNameFormat {
        Full,
        Abbreviated
    }

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

        public static async Task<Species[]> GetSpeciesAsync() {

            List<Species> species = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species"))
            using (DataTable table = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in table.Rows)
                    species.Add(await Species.FromDataRow(row));

            return species.ToArray();

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

        public static async Task<Species[]> GetBaseSpeciesAsync() {

            List<Species> species = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id NOT IN (SELECT species_id FROM Ancestors)"))
            using (DataTable table = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in table.Rows)
                    species.Add(await Species.FromDataRow(row));

            return species.ToArray();

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

        public static async Task AddCommonNameAsync(Species species, string commonName, bool overwriteSpeciesTable) {

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
        public static async Task RemoveCommonNameAsync(Species species, string commonName) {

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
        public static async Task RemoveCommonNamesAsync(Species species) {

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM SpeciesCommonNames WHERE species_id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await _setCommonNameInSpeciesTable(species, "");

        }

        public static async Task<SpeciesZone[]> GetZonesAsync(Species species) {

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

            zones.Sort((lhs, rhs) => new ArrayUtils.NaturalStringComparer().Compare(lhs.Zone.name, rhs.Zone.name));

            return zones.ToArray();

        }
        public static async Task AddZonesAsync(Species species, Zone[] zones) {
            await AddZonesAsync(species, zones, string.Empty);
        }
        public static async Task AddZonesAsync(Species species, Zone[] zones, string notes) {

            foreach (Zone zone in zones) {

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO SpeciesZones(species_id, zone_id, notes) VALUES($species_id, $zone_id, $notes)")) {

                    cmd.Parameters.AddWithValue("$species_id", species.id);
                    cmd.Parameters.AddWithValue("$zone_id", zone.id);
                    cmd.Parameters.AddWithValue("$notes", string.IsNullOrEmpty(notes) ? "" : notes.Trim().ToLower());

                    await Database.ExecuteNonQuery(cmd);

                }

            }

        }
        public static async Task RemoveZonesAsync(Species species, Zone[] zones) {

            foreach (Zone zone in zones) {

                using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM SpeciesZones WHERE species_id = $species_id AND zone_id = $zone_id")) {

                    cmd.Parameters.AddWithValue("$species_id", species.id);
                    cmd.Parameters.AddWithValue("$zone_id", zone.id);

                    await Database.ExecuteNonQuery(cmd);

                }

            }

        }

        public static async Task SetPictureAsync(Species species, Picture picture) {

            // Set the given picture as the default picture for the species.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET pics = $url WHERE id = $species_id")) {

                cmd.Parameters.AddWithValue("$url", picture is null ? string.Empty : picture.url);
                cmd.Parameters.AddWithValue("$species_id", species.id);

                await Database.ExecuteNonQuery(cmd);

            }

            // Update the "pics" value for the species so we don't run into an infinite loop below.
            // "AddPicture" will call this function if the "pics" value is empty.
            species.pics = picture is null ? string.Empty : picture.url;

            // Add the picture to this species' picture gallery (does nothing if it's already been added).

            if (!(picture is null))
                await AddPictureAsync(species, picture);

        }
        public static async Task AddPictureAsync(Species species, Picture picture) {

            if (!(picture is null)) {

                // Add the picture to this species' picture gallery (does nothing if it's already been added).

                await GalleryUtils.AddGalleryAsync(species);

                Gallery gallery = await GalleryUtils.GetGalleryAsync(species);

                await GalleryUtils.AddPictureAsync(gallery, picture);

                // If the species doesn't have a default picture yet, use this picture as the default picture.

                if (string.IsNullOrEmpty(species.pics))
                    await SetPictureAsync(species, picture);

            }

        }
        public static async Task<bool> RemovePictureAsync(Species species, Picture picture) {

            // Remove this picture from the species' picture gallery.
            // Additionally, if this picture is the species' default picture, remove that as well.

            if (picture is null)
                return false;

            bool success = false;

            Gallery gallery = await GalleryUtils.GetGalleryAsync(species);
            Picture[] gallery_pictures = await GalleryUtils.GetPicturesAsync(gallery);

            if (gallery_pictures.Count() >= 0 && gallery_pictures.Any(x => x.id == picture.id)) {

                await GalleryUtils.RemovePictureAsync(gallery, picture);

                success = true;

            }

            if (species.pics == picture.url) {

                await SetPictureAsync(species, null);

                success = true;

            }

            return success;

        }
        public static async Task<Picture[]> GetPicturesAsync(Species species) {

            List<Picture> pictures = new List<Picture>();

            Gallery gallery = await GalleryUtils.GetGalleryAsync(species);

            pictures.AddRange(await GalleryUtils.GetPicturesAsync(gallery));

            if (!string.IsNullOrEmpty(species.pics) && !pictures.Any(x => x.url == species.pics))
                pictures.Insert(0, new Picture {
                    url = species.pics,
                    artist = species.owner
                });

            pictures.ForEach(x => {
                x.footer = string.Format("Depiction of {0}", species.GetShortName());
            });

            return pictures.ToArray();

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
            return await GetRolesAsync(species.id);
        }

        public static async Task<ExtinctionInfo> GetExtinctionInfoAsync(Species species) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Extinctions WHERE species_id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", species.id);

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

                        cmd.Parameters.AddWithValue("$species_id", species.id);
                        cmd.Parameters.AddWithValue("$reason", extinctionInfo.Reason);
                        cmd.Parameters.AddWithValue("$timestamp", extinctionInfo.Timestamp);

                        await Database.ExecuteNonQuery(cmd);

                    }

                }
                else {

                    // If the species has an existing extinction record, update the description only.

                    using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Extinctions SET reason = $reason WHERE species_id = $species_id")) {

                        cmd.Parameters.AddWithValue("$species_id", species.id);
                        cmd.Parameters.AddWithValue("$reason", extinctionInfo.Reason);

                        await Database.ExecuteNonQuery(cmd);

                    }

                }

            }
            else {

                // If the species is no longer extinct, delete its extinction record (if it exists).

                using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Extinctions WHERE species_id = $species_id")) {

                    cmd.Parameters.AddWithValue("$species_id", species.id);

                    await Database.ExecuteNonQuery(cmd);

                }

            }

        }

        public static string FormatSpeciesName(string speciesName, SpeciesNameFormat format) {

            string[] words = speciesName.Split(' ')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToArray();

            if (words.Count() <= 1)
                // If the name only contains one word, assume it's the specific epithet.
                return speciesName.ToLower();
            else if (words.Count() > 2)
                // If the name contains more than two words, treat it as a common name.
                return StringUtils.ToTitleCase(string.Join(" ", words));

            switch (format) {

                case SpeciesNameFormat.Full:
                    return string.Format("{0} {1}", StringUtils.ToTitleCase(words[0]), words[1].ToLower());

                case SpeciesNameFormat.Abbreviated:
                    return string.Format("{0}. {1}", StringUtils.ToTitleCase(words[0]).First(), words[1].ToLower());

            }

            return speciesName;

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