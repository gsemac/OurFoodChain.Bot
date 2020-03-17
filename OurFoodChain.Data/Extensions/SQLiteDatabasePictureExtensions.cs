using OurFoodChain.Common;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Extensions {

    public static class SQLiteDatabasePictureExtensions {

        // Public members

        public static async Task AddGalleryAsync(this SQLiteDatabase database, ISpecies species) {

            await AddGalleryAsync(database, GetGalleryNameFromSpecies(species));

        }
        public static async Task AddGalleryAsync(this SQLiteDatabase database, IPictureGallery gallery) {

            await database.AddGalleryAsync(gallery.Name);

            IPictureGallery newGallery = await database.GetGalleryAsync(gallery.Name);

            await database.UpdateGalleryAsync(new PictureGallery(newGallery.Id, gallery.Name, gallery.Pictures));

        }

        public static async Task<IPictureGallery> GetGalleryAsync(this SQLiteDatabase database, ISpecies species) {

            return await GetGalleryAsync(database, GetGalleryNameFromSpecies(species));

        }
        public static async Task<IPictureGallery> GetGalleryAsync(this SQLiteDatabase database, string name) {

            IPictureGallery gallery = null;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gallery WHERE name = $name")) {

                cmd.Parameters.AddWithValue("$name", name);

                DataRow row = await database.GetRowAsync(cmd);

                if (row != null)
                    return await database.CreateGalleryFromDataRowAsync(row);

            }

            return gallery;

        }
        public static async Task<IPictureGallery> GetGalleryAsync(this SQLiteDatabase database, long? id) {

            IPictureGallery gallery = null;

            if (id.HasValue) {

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gallery WHERE id = $id")) {

                    cmd.Parameters.AddWithValue("$id", id);

                    DataRow row = await database.GetRowAsync(cmd);

                    if (row != null)
                        return await database.CreateGalleryFromDataRowAsync(row);

                }

            }

            return gallery;

        }

        public static async Task UpdateGalleryAsync(this SQLiteDatabase database, IPictureGallery gallery) {

            if (!gallery.Id.HasValue) {

                // If the gallery doesn't have an ID, assume that it's new, and attempt to add it.

                await database.AddGalleryAsync(gallery);

            }
            else {

                // Update the gallery in the database.

                IPictureGallery oldGallery = await database.GetGalleryAsync(gallery.Id);
                IEnumerable<IPicture> deletedPictures = oldGallery.Where(oldPicture => !gallery.Any(picture => oldPicture.Id == picture.Id));
                IEnumerable<IPicture> newPictures = gallery.Where(picture => !oldGallery.Any(oldPicture => picture.Id == oldPicture.Id));

                foreach (IPicture picture in deletedPictures)
                    await database.RemovePictureAsync(gallery, picture);

                foreach (IPicture picture in newPictures)
                    await database.AddPictureAsync(gallery, picture);

            }

        }

        public static async Task AddPictureAsync(this SQLiteDatabase database, IPictureGallery gallery, IPicture picture) {

            if (gallery is null)
                throw new ArgumentNullException(nameof(gallery));

            if (picture is null)
                throw new ArgumentNullException(nameof(picture));

            if (!gallery.Id.HasValue)
                throw new ArgumentException(nameof(gallery));

            if (!picture.Id.HasValue) {

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Picture(url, gallery_id, artist, name, description) VALUES($url, $gallery_id, $artist, $name, $description)")) {

                    cmd.Parameters.AddWithValue("$url", picture.Url);
                    cmd.Parameters.AddWithValue("$gallery_id", gallery.Id);
                    cmd.Parameters.AddWithValue("$artist", picture.Artist?.Name ?? "");
                    cmd.Parameters.AddWithValue("$name", picture.Name);
                    cmd.Parameters.AddWithValue("$description", picture.Description);

                    await database.ExecuteNonQueryAsync(cmd);

                }

            }
            else {

                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Picture SET url = $url, artist = $artist, description = $description WHERE id = $id")) {

                    cmd.Parameters.AddWithValue("$id", picture.Id);
                    cmd.Parameters.AddWithValue("$url", picture.Url);
                    cmd.Parameters.AddWithValue("$artist", picture.Artist?.Name ?? "");
                    cmd.Parameters.AddWithValue("$description", picture.Description);

                    await database.ExecuteNonQueryAsync(cmd);

                }

            }

        }
        public static async Task RemovePictureAsync(this SQLiteDatabase database, IPictureGallery gallery, IPicture picture) {

            if (gallery is null)
                throw new ArgumentNullException(nameof(gallery));

            if (picture is null)
                throw new ArgumentNullException(nameof(picture));

            if (!picture.Id.HasValue)
                throw new ArgumentException(nameof(picture));

            if (!gallery.Id.HasValue)
                throw new ArgumentException(nameof(gallery));

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Picture WHERE gallery_id = $gallery_id AND id = $id")) {

                cmd.Parameters.AddWithValue("$id", picture.Id);
                cmd.Parameters.AddWithValue("$gallery_id", gallery.Id);

                await database.ExecuteNonQueryAsync(cmd);

            }

        }

        // Private members

        private static string GetGalleryNameFromSpecies(ISpecies species) {

            if (species is null)
                throw new ArgumentNullException(nameof(species));

            if (!species.Id.HasValue)
                throw new ArgumentException(nameof(species));

            string galleryName = "species" + species.Id.ToString();

            return galleryName;

        }

        private static IPicture CreatePictureFromDataRow(DataRow row) {

            Picture result = new Picture {
                Id = row.Field<long>("id"),
                Url = row.Field<string>("url"),
                Name = row.Field<string>("name"),
                Description = row.Field<string>("description"),
                Artist = new Creator(row.Field<string>("artist"))
            };

            if (!row.IsNull("gallery_id"))
                result.GalleryId = row.Field<long>("gallery_id");

            return result;

        }
        private static async Task<IPictureGallery> CreateGalleryFromDataRowAsync(this SQLiteDatabase database, DataRow row) {

            IPictureGallery result = new PictureGallery {
                Id = row.Field<long>("id"),
                Name = row.Field<string>("name")
            };

            IEnumerable<IPicture> pictures = await database.GetPicturesFromGalleryAsync(result);

            result = new PictureGallery(result.Id, result.Name, pictures);

            return result;

        }

        private static async Task AddGalleryAsync(this SQLiteDatabase database, string name) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Gallery(name) VALUES($name)")) {

                cmd.Parameters.AddWithValue("$name", name);

                await database.ExecuteNonQueryAsync(cmd);

            }

        }

        public static async Task<IEnumerable<IPicture>> GetPicturesFromGalleryAsync(this SQLiteDatabase database, IPictureGallery gallery) {

            List<IPicture> pictures = new List<IPicture>();

            if (gallery != null && gallery.Id.HasValue) {

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Picture WHERE gallery_id = $gallery_id ORDER BY id")) {

                    cmd.Parameters.AddWithValue("$gallery_id", gallery.Id);

                    foreach (DataRow row in await database.GetRowsAsync(cmd))
                        pictures.Add(CreatePictureFromDataRow(row));

                }

            }

            return pictures;

        }

    }

}