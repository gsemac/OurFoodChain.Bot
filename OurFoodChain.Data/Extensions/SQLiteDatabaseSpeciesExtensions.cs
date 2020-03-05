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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Extensions {

    public static class SQLiteDatabaseSpeciesExtensions {

        // Public members

        public static async Task<IEnumerable<ISpecies>> GetSpeciesAsync(this SQLiteDatabase database) {

            List<ISpecies> species = new List<ISpecies>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species"))
                foreach (DataRow row in await database.GetRowsAsync(cmd))
                    species.Add(await database.CreateSpeciesFromDataRowAsync(row));

            return species;

        }
        public static async Task<IEnumerable<ISpecies>> GetSpeciesAsync(this SQLiteDatabase database, string name) {

            // We don't know for sure if the user passed in a binomial name or a common name/species name.
            // If the input is a valid binomial name, we'll use it to find the requested species.
            // Otherwise, or if the first attempt doesn't return a match, we'll treat it as a common name/species name.

            IBinomialName input = BinomialName.Parse(name);

            List<ISpecies> result = new List<ISpecies>();

            if (!string.IsNullOrEmpty(input.GenusName)) {

                // Attempt to get the species using name/genus.

                result.AddRange(await database.GetSpeciesAsync(input.GenusName, input.SpeciesName));

            }

            if (result.Count() <= 0) {

                // Attempt to get the species by common name/species name.

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE name = $name OR common_name = $name OR id IN (SELECT species_id FROM SpeciesCommonNames where name = $name)")) {

                    cmd.Parameters.AddWithValue("$name", name.ToLowerInvariant());

                    foreach (DataRow row in await database.GetRowsAsync(cmd))
                        result.Add(await database.CreateSpeciesFromDataRowAsync(row));

                }

            }

            return result;

        }
        public static async Task<IEnumerable<ISpecies>> GetSpeciesAsync(this SQLiteDatabase database, string genus, string species) {

            IBinomialName input = BinomialName.Parse(genus, species);

            // If the species is the empty string, don't bother trying to find any matches.
            // This prevents species with an empty, but non-null common name (set to "") from being returned.

            if (string.IsNullOrEmpty(input.SpeciesName))
                return Array.Empty<ISpecies>();

            if (string.IsNullOrEmpty(input.GenusName)) {

                // If the user didn't pass in a genus, we'll look up the species by name (name or common name).
                return await database.GetSpeciesAsync(input.SpeciesName);

            }
            else if (input.IsAbbreviated) {

                // If the genus is abbreviated (e.g. "Cornu" -> "C.") but not null, we'll look for matching species and determine the genus afterwards.
                // Notice that this case does not allow for looking up the species by common name, which should not occur when the genus is included.

                IEnumerable<ISpecies> result = await database.GetSpeciesAsync(input.SpeciesName);

                return result.Where(s => s.Genus != null && s.Genus.Name.StartsWith(input.GenusName, StringComparison.OrdinalIgnoreCase));

            }
            else {

                // If we've been given full genus and species names, we can attempt to get the species directly.
                // Although genera can have common names, only allow the genus to be looked up by its scientific name. Generally users wouldn't use the common name in this context.

                ISpecies speciesResult = await database.GetSpeciesByGenusAndSpeciesNameAsync(input.GenusName, input.SpeciesName);

                return (speciesResult is null) ? Array.Empty<ISpecies>() : new ISpecies[] { speciesResult };

            }

        }
        public static async Task<ISpecies> GetSpeciesAsync(this SQLiteDatabase database, long? speciesId) {

            if (!speciesId.HasValue)
                return null;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id = $id")) {

                cmd.Parameters.AddWithValue("$id", speciesId);

                DataRow row = await database.GetRowAsync(cmd);

                return row is null ? null : await database.CreateSpeciesFromDataRowAsync(row);

            }

        }

        public static async Task<IEnumerable<ISpecies>> GetBaseSpeciesAsync(this SQLiteDatabase database) {

            List<ISpecies> species = new List<ISpecies>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id NOT IN (SELECT species_id FROM Ancestors)"))
                foreach (DataRow row in await database.GetRowsAsync(cmd))
                    species.Add(await database.CreateSpeciesFromDataRowAsync(row));

            return species;

        }
        public static async Task<bool> IsBaseSpeciesAsync(this SQLiteDatabase database, ISpecies species) {

            return await database.GetAncestorAsync(species) is null;

        }

        public static async Task<IEnumerable<long>> GetAncestorIdsAsync(this SQLiteDatabase database, long? speciesId) {

            if (!speciesId.HasValue)
                return Enumerable.Empty<long>();

            List<long> ancestor_ids = new List<long>();

            while (true) {

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT ancestor_id FROM Ancestors WHERE species_id = $species_id")) {

                    cmd.Parameters.AddWithValue("$species_id", speciesId);

                    DataRow row = await database.GetRowAsync(cmd);

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
        public static async Task<ISpecies> GetAncestorAsync(this SQLiteDatabase database, ISpecies species) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT ancestor_id FROM Ancestors WHERE species_id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", species.Id);

                DataRow row = await database.GetRowAsync(cmd);

                return row is null ? null : await database.GetSpeciesAsync(row.Field<long>("ancestor_id"));

            }

        }
        public static async Task<IEnumerable<ISpecies>> GetAncestorsAsync(this SQLiteDatabase database, ISpecies species) {

            if (!species.Id.HasValue)
                return Enumerable.Empty<ISpecies>();

            return await database.GetAncestorsAsync(species.Id.Value);

        }
        public static async Task<IEnumerable<ISpecies>> GetAncestorsAsync(this SQLiteDatabase database, long speciesId) {

            List<ISpecies> ancestor_species = new List<ISpecies>();

            foreach (long species_id in await database.GetAncestorIdsAsync(speciesId))
                ancestor_species.Add(await database.GetSpeciesAsync(species_id));

            return ancestor_species.ToArray();

        }
        public static async Task<IEnumerable<ISpecies>> GetDirectDescendantsAsync(this SQLiteDatabase database, ISpecies species) {

            List<ISpecies> result = new List<ISpecies>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM Ancestors WHERE ancestor_id = $ancestor_id)")) {

                cmd.Parameters.AddWithValue("$ancestor_id", species.Id);

                foreach (DataRow row in await database.GetRowsAsync(cmd))
                    result.Add(await database.CreateSpeciesFromDataRowAsync(row));

            }

            return result;

        }

        public static async Task<IConservationStatus> GetConservationStatusAsync(this SQLiteDatabase database, ISpecies species) {

            IConservationStatus result = new ConservationStatus();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Extinctions WHERE species_id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", species.Id);

                DataRow row = await database.GetRowAsync(cmd);

                if (row != null) {

                    string reason = row.Field<string>("reason");
                    long timestamp = (long)row.Field<decimal>("timestamp");

                    result = new ConservationStatus() {
                        ExtinctionDate = DateUtilities.TimestampToDate(timestamp),
                        ExtinctionReason = reason
                    };

                }

            }

            return result;

        }
        public static async Task<bool> IsEndangeredAsync(this SQLiteDatabase database, ISpecies species) {

            // Consider a species "endangered" if:
            // - All of its prey has gone extinct.
            // - It has a descendant in the same zone that consumes all of the same prey items.

            bool isEndangered = false;

            if (species.Status.IsExinct)
                return isEndangered;

            if (!isEndangered) {

                // Check if all of this species' prey species have gone extinct.

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Species WHERE id = $id AND id NOT IN (SELECT species_id FROM Predates WHERE eats_id NOT IN (SELECT species_id FROM Extinctions)) AND id IN (SELECT species_id from Predates)")) {

                    cmd.Parameters.AddWithValue("$id", species.Id);

                    isEndangered = await database.GetScalarAsync<long>(cmd) > 0;

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

                    isEndangered = await database.GetScalarAsync<long>(cmd) > 0;

                }

            }

            return isEndangered;

        }

        public static async Task SetPictureAsync(this SQLiteDatabase database, ISpecies species, IPicture picture) {

            // Set the given picture as the default picture for the species.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET pics = $url WHERE id = $species_id")) {

                cmd.Parameters.AddWithValue("$url", picture is null ? string.Empty : picture.Url);
                cmd.Parameters.AddWithValue("$species_id", species.Id);

                await database.ExecuteNonQueryAsync(cmd);

            }

            // Update the "pics" value for the species so we don't run into an infinite loop below.
            // "AddPicture" will call this function if the "pics" value is empty.
            species.Pictures.Add(picture);

            // Add the picture to this species' picture gallery (does nothing if it's already been added).

            if (picture != null)
                await database.AddPictureAsync(species, picture);

        }
        public static async Task AddPictureAsync(this SQLiteDatabase database, ISpecies species, IPicture picture) {

            if (picture is null)
                throw new ArgumentNullException(nameof(picture));

            // Add the picture to this species' picture gallery (does nothing if it's already been added).

            await database.AddPictureGalleryAsync(species);

            IPictureGallery gallery = await database.GetPictureGalleryAsync(species);

            await database.AddPictureAsync(gallery, picture);

            // If the species doesn't have a default picture yet, use this picture as the default picture.

            if (species.Pictures.Count() <= 0)
                await database.SetPictureAsync(species, picture);

        }
        public static async Task<bool> RemovePictureAsync(this SQLiteDatabase database, ISpecies species, IPicture picture) {

            // Remove this picture from the species' picture gallery.
            // Additionally, if this picture is the species' default picture, remove that as well.

            if (picture is null)
                return false;

            bool success = false;

            // Remove the picture from this species' picture gallery.

            IPictureGallery gallery = await database.GetPictureGalleryAsync(species) ?? new PictureGallery();

            if (gallery.Count() >= 0 && gallery.Any(p => p.Id == picture.Id)) {

                await database.RemovePictureAsync(gallery, picture);

                success = true;

            }

            // If this picture is the default picture for the species, remove it from there as well.

            if (species.Pictures.Count() > 0 && species.Pictures.First().Url.Equals(picture.Url)) {

                await database.SetPictureAsync(species, null);

                success = true;

            }

            return success;

        }
        public static async Task<IEnumerable<IPicture>> GetPicturesAsync(this SQLiteDatabase database, ISpecies species) {

            List<IPicture> pictures = new List<IPicture>();

            IPictureGallery gallery = await database.GetPictureGalleryAsync(species);

            if (gallery != null)
                pictures.AddRange(gallery);

            if (species.Pictures.Count() > 0 && !string.IsNullOrEmpty(species.Pictures.First().Url) && !pictures.Any(p => species.Pictures.First().Url.Equals(p.Url)))
                pictures.Insert(0, species.Pictures.First());

            pictures.ForEach(p => {
                p.Caption = string.Format("Depiction of {0}", species.ShortName);
            });

            return pictures;

        }

        public static async Task<IEnumerable<ISpecies>> GetSpeciesAsync(this SQLiteDatabase database, IZone zone) {

            if (zone is null || zone.Id <= 0)
                return Enumerable.Empty<ISpecies>();

            List<ISpecies> species = new List<ISpecies>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM SpeciesZones WHERE zone_id = $zone_id)")) {

                cmd.Parameters.AddWithValue("$zone_id", zone.Id);

                foreach (DataRow row in await database.GetRowsAsync(cmd))
                    species.Add(await CreateSpeciesFromDataRowAsync(database, row));

            }

            return species;

        }
        public static async Task AddZonesAsync(this SQLiteDatabase database, ISpecies species, IEnumerable<IZone> zones) {

            await AddZonesAsync(database, species, zones, string.Empty);

        }
        public static async Task AddZonesAsync(this SQLiteDatabase database, ISpecies species, IEnumerable<IZone> zones, string notes) {

            foreach (IZone zone in zones) {

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO SpeciesZones(species_id, zone_id, notes, timestamp) VALUES($species_id, $zone_id, $notes, $timestamp)")) {

                    cmd.Parameters.AddWithValue("$species_id", species.Id);
                    cmd.Parameters.AddWithValue("$zone_id", zone.Id);
                    cmd.Parameters.AddWithValue("$notes", string.IsNullOrEmpty(notes) ? "" : notes.Trim().ToLowerInvariant());
                    cmd.Parameters.AddWithValue("$timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                    await database.ExecuteNonQueryAsync(cmd);

                }

            }

        }
        public static async Task<IEnumerable<ISpeciesZoneInfo>> GetZonesAsync(this SQLiteDatabase database, ISpecies species) {

            return await database.GetZonesAsync(species.Id);

        }
        public static async Task<IEnumerable<ISpeciesZoneInfo>> GetZonesAsync(this SQLiteDatabase database, long? speciesId) {

            List<ISpeciesZoneInfo> results = new List<ISpeciesZoneInfo>();

            if (speciesId.HasValue) {

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM SpeciesZones WHERE species_id = $species_id")) {

                    cmd.Parameters.AddWithValue("$species_id", speciesId);

                    foreach (DataRow row in await database.GetRowsAsync(cmd)) {

                        IZone zone = await database.GetZoneAsync(row.Field<long>("zone_id"));

                        if (zone is null)
                            continue;

                        if (zone != null) {

                            ISpeciesZoneInfo zoneInfo = new SpeciesZoneInfo {
                                Zone = zone,
                                Notes = row.Field<string>("notes"),
                            };

                            if (!row.IsNull("timestamp"))
                                zoneInfo.Date = DateUtilities.TimestampToDate(row.Field<long>("timestamp"));

                            results.Add(zoneInfo);

                        }

                    }

                }

            }

            results.Sort((lhs, rhs) => new NaturalStringComparer().Compare(lhs.Zone.Name, rhs.Zone.Name));

            return results.ToArray();

        }
        public static async Task RemoveZonesAsync(this SQLiteDatabase database, ISpecies species, IEnumerable<IZone> zones) {

            foreach (IZone zone in zones) {

                using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM SpeciesZones WHERE species_id = $species_id AND zone_id = $zone_id")) {

                    cmd.Parameters.AddWithValue("$species_id", species.Id);
                    cmd.Parameters.AddWithValue("$zone_id", zone.Id);

                    await database.ExecuteNonQueryAsync(cmd);

                }

            }

        }

        public static async Task<IEnumerable<ISpecies>> GetPredatorsAsync(this SQLiteDatabase database, ISpecies species) {

            List<ISpecies> result = new List<ISpecies>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM Predates WHERE eats_id = $species_id)")) {

                cmd.Parameters.AddWithValue("$species_id", species.Id);

                foreach (DataRow row in await database.GetRowsAsync(cmd))
                    result.Add(await database.CreateSpeciesFromDataRowAsync(row));

            }

            return result.ToArray();

        }
        public static async Task<IEnumerable<IPreyInfo>> GetPreyAsync(this SQLiteDatabase database, ISpecies species) {

            return await database.GetPreyAsync(species.Id);

        }
        public static async Task<IEnumerable<IPreyInfo>> GetPreyAsync(this SQLiteDatabase database, long? speciesId) {

            if (!speciesId.HasValue)
                return Enumerable.Empty<IPreyInfo>();

            List<IPreyInfo> result = new List<IPreyInfo>();

            using (SQLiteCommand cmd = new SQLiteCommand(@"SELECT * FROM (SELECT * FROM Species WHERE id IN (SELECT eats_id FROM Predates WHERE species_id = $species_id)) INNER JOIN Predates WHERE eats_id = id AND species_id = $species_id;")) {

                cmd.Parameters.AddWithValue("$species_id", speciesId);

                foreach (DataRow row in await database.GetRowsAsync(cmd)) {

                    result.Add(new PreyInfo {
                        Prey = await database.CreateSpeciesFromDataRowAsync(row),
                        Notes = row.Field<string>("notes")
                    });

                }

            }

            return result;

        }

        // Private members

        private static async Task<ISpecies> CreateSpeciesFromDataRowAsync(this SQLiteDatabase database, DataRow row) {

            long genusId = row.Field<long>("genus_id");
            ITaxon genus = await database.GetTaxonAsync(genusId, TaxonRankType.Genus);

            return await database.CreateSpeciesFromDataRowAsync(row, genus);

        }
        private static async Task<ISpecies> CreateSpeciesFromDataRowAsync(this SQLiteDatabase database, DataRow row, ITaxon genus) {

            ISpecies species = new Species {
                Id = row.Field<long>("id"),
                ParentId = row.Field<long>("genus_id"),
                Name = row.Field<string>("name"),
                // The genus should never be null, but there was instance where a user manually edited the database and the genus ID was invalid.
                // We should at least try to handle this situation gracefully.
                Genus = genus ?? new Taxon("?", TaxonRankType.Genus),
                Description = row.Field<string>("description"),
                Creator = new Creator(row.IsNull("user_id") ? default(ulong?) : row.Field<ulong>("user_id"), row.Field<string>("owner") ?? "?"),
                CreationDate = DateUtilities.TimestampToDate((long)row.Field<decimal>("timestamp"))
            };

            if (!row.IsNull("common_name") && !string.IsNullOrWhiteSpace(row.Field<string>("common_name")))
                species.CommonNames.Add(row.Field<string>("common_name"));

            if (!row.IsNull("pics") && !string.IsNullOrWhiteSpace(row.Field<string>("pics")))
                species.Pictures.Add(new Picture(row.Field<string>("pics")));

            species.Status = await database.GetConservationStatusAsync(species);

            return species;

        }
        private static async Task<ISpecies> GetSpeciesByGenusAndSpeciesNameAsync(this SQLiteDatabase database, string genus, string species) {

            ITaxon genusInfo = await database.GetTaxonAsync(genus, TaxonRankType.Genus);

            // If the genus doesn't exist, the species cannot possibly exist either.

            if (genusInfo.IsNull())
                return null;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE genus_id = $genus_id AND name = $species")) {

                cmd.Parameters.AddWithValue("$genus_id", genusInfo.Id);
                cmd.Parameters.AddWithValue("$species", species.ToLowerInvariant());

                DataRow result = await database.GetRowAsync(cmd);

                if (result is null)
                    return null;

                return await database.CreateSpeciesFromDataRowAsync(result, genusInfo);

            }

        }

    }

}