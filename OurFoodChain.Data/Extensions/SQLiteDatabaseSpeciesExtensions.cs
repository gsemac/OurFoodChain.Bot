using OurFoodChain.Common;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Extensions {

    public static class SQLiteDatabaseSpeciesExtensions {

        public static async Task SetDefaultPictureAsync(this SQLiteDatabase database, ISpecies species, IPicture picture) {

            // Set the given picture as the default picture for the species.

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET pics = $url WHERE id = $species_id")) {

                cmd.Parameters.AddWithValue("$url", picture is null ? string.Empty : picture.Url);
                cmd.Parameters.AddWithValue("$species_id", species.Id);

                await database.ExecuteNonQueryAsync(cmd);

            }

            // Update the "pics" value for the species so we don't run into an infinite loop below.
            // "AddPicture" will call this function if the "pics" value is empty.
            species.Picture = picture;

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

            if (species.Picture is null)
                await database.SetDefaultPictureAsync(species, picture);

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

            if (species.Picture != null && species.Picture.Url == picture.Url) {

                await database.SetDefaultPictureAsync(species, null);

                success = true;

            }

            return success;

        }
        public static async Task<IEnumerable<IPicture>> GetAllPicturesAsync(this SQLiteDatabase database, ISpecies species) {

            List<IPicture> pictures = new List<IPicture>();

            IPictureGallery gallery = await database.GetPictureGalleryAsync(species);

            if (gallery != null)
                pictures.AddRange(gallery);

            if (species.Picture != null && !string.IsNullOrEmpty(species.Picture.Url) && !pictures.Any(p => p.Url == species.Picture.Url))
                pictures.Insert(0, species.Picture);

            pictures.ForEach(p => {
                p.Caption = string.Format("Depiction of {0}", species.ShortName);
            });

            return pictures;

        }

    }

}