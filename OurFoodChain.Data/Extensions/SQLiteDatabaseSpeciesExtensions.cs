using OurFoodChain.Common;
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

        public static async Task<IConservationStatus> GetConservationStatusAsync(this SQLiteDatabase database, ISpecies species) {

            IConservationStatus result = new ConservationStatus();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Extinctions WHERE species_id = $species_id")) {

                cmd.Parameters.AddWithValue("$species_id", species.Id);

                DataRow row = await database.GetRowAsync(cmd);

                if (row != null) {

                    string reason = row.Field<string>("reason");
                    long timestamp = (long)row.Field<decimal>("timestamp");

                    result = new ConservationStatus() {
                        ExtinctionDate = DateUtilities.TimestampToOffset(timestamp),
                        ExtinctionReason = reason
                    };

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
        public static async Task<ISpecies> CreateSpeciesFromDataRowAsync(this SQLiteDatabase database, DataRow row, ITaxon genus) {

            ISpecies species = new Species {
                Id = row.Field<long>("id"),
                ParentId = row.Field<long>("genus_id"),
                Name = row.Field<string>("name"),
                // The genus should never be null, but there was instance where a user manually edited the database and the genus ID was invalid.
                // We should at least try to handle this situation gracefully.
                Genus = genus ?? new Taxon("?", TaxonRankType.Genus),
                Description = row.Field<string>("description"),
                Creator = new Creator(row.IsNull("user_id") ? default(ulong?) : row.Field<ulong>("user_id"), row.Field<string>("owner") ?? "?"),
                CreationDate = DateUtilities.TimestampToOffset((long)row.Field<decimal>("timestamp"))
            };

            if (!row.IsNull("common_name") && !string.IsNullOrWhiteSpace(row.Field<string>("common_name")))
                species.CommonNames.Add(row.Field<string>("common_name"));

            if (!row.IsNull("pics") && !string.IsNullOrWhiteSpace(row.Field<string>("pics")))
                species.Pictures.Add(new Picture(row.Field<string>("pics")));

            species.Status = await database.GetConservationStatusAsync(species);

            return species;

        }

    }

}