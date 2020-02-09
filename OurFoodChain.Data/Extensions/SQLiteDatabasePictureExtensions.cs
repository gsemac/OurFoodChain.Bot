using OurFoodChain.Common;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Extensions {

    public static class SQLiteDatabasePictureExtensions {

        // Public members

        public static async Task AddPictureGalleryAsync(this SQLiteDatabase database, ISpecies species) {

            await AddPictureGalleryAsync(database, GetGalleryNameFromSpecies(species));

        }
        public static async Task AddPictureGalleryAsync(this SQLiteDatabase database, string name) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Gallery(name) VALUES($name)")) {

                cmd.Parameters.AddWithValue("$name", name);

                await database.ExecuteNonQueryAsync(cmd);

            }

        }

        public static async Task<IPictureGallery> GetPictureGalleryAsync(this SQLiteDatabase database, ISpecies species) {

            return await GetGalleryAsync(database, GetGalleryNameFromSpecies(species));

        }
        public static async Task<IPictureGallery> GetGalleryAsync(this SQLiteDatabase database, string name) {

            IPictureGallery gallery = null;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gallery WHERE name = $name")) {

                cmd.Parameters.AddWithValue("$name", name);

                DataRow row = await database.GetRowAsync(cmd);

                if (row != null) {

                    gallery = CreateGalleryFromDataRow(row);

                    IEnumerable<IPicture> pictures = await GetPicturesFromGalleryAsync(database, gallery);

                    gallery = new PictureGallery(gallery.Id, gallery.Name, pictures);

                }

            }

            return gallery;

        }

        public static async Task AddPictureAsync(this SQLiteDatabase database, IPictureGallery gallery, IPicture picture) {

            if (gallery is null)
                throw new ArgumentNullException(nameof(gallery));

            if (picture is null)
                throw new ArgumentNullException(nameof(picture));

            if (!gallery.Id.HasValue)
                throw new ArgumentException(nameof(gallery));

            if (!picture.Id.HasValue) {

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Picture(url, gallery_id, artist, description) VALUES($url, $gallery_id, $artist, $description)")) {

                    cmd.Parameters.AddWithValue("$url", picture.Url);
                    cmd.Parameters.AddWithValue("$gallery_id", gallery.Id);
                    cmd.Parameters.AddWithValue("$artist", picture.Artist?.Name ?? "");
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
        private static IPictureGallery CreateGalleryFromDataRow(DataRow row) {

            PictureGallery result = new PictureGallery {
                Id = row.Field<long>("id"),
                Name = row.Field<string>("name")
            };

            return result;

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