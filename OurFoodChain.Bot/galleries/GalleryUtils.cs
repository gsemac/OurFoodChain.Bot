using OurFoodChain.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public static class GalleryUtils {

        public static bool IsImageUrl(string imageUrl) {
            return StringUtilities.IsUrl(imageUrl);
        }
        public static string SpeciesToGalleryName(Species species) {

            string gallery_name = "species" + species.Id.ToString();

            return gallery_name;


        }

        public static async Task AddGalleryAsync(Species species) {
            await AddGalleryAsync(SpeciesToGalleryName(species));
        }
        public static async Task AddGalleryAsync(string name) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Gallery(name) VALUES($name)")) {

                cmd.Parameters.AddWithValue("$name", name);

                await Database.ExecuteNonQuery(cmd);

            }

        }

        public static async Task<Gallery> GetGalleryAsync(Species species) {
            return await GetGalleryAsync(SpeciesToGalleryName(species));
        }
        public static async Task<Gallery> GetGalleryAsync(string name) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Gallery WHERE name = $name")) {

                cmd.Parameters.AddWithValue("$name", name);

                DataRow row = await Database.GetRowAsync(cmd);

                if (!(row is null))
                    return Gallery.FromDataRow(row);

            }

            return null;

        }

        public static async Task AddPictureAsync(Gallery gallery, Picture picture) {

            if (picture.id == Picture.NULL_ID) {

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Picture(url, gallery_id, artist, description) VALUES($url, $gallery_id, $artist, $description)")) {

                    cmd.Parameters.AddWithValue("$url", picture.url);
                    cmd.Parameters.AddWithValue("$gallery_id", gallery.id);
                    cmd.Parameters.AddWithValue("$artist", picture.artist);
                    cmd.Parameters.AddWithValue("$description", picture.description);

                    await Database.ExecuteNonQuery(cmd);

                }

            }
            else {

                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Picture SET url = $url, artist = $artist, description = $description WHERE id = $id")) {

                    cmd.Parameters.AddWithValue("$id", picture.id);
                    cmd.Parameters.AddWithValue("$url", picture.url);
                    cmd.Parameters.AddWithValue("$artist", picture.artist);
                    cmd.Parameters.AddWithValue("$description", picture.description);

                    await Database.ExecuteNonQuery(cmd);

                }

            }

        }

        public static async Task RemovePictureAsync(Gallery gallery, Picture picture) {

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Picture WHERE gallery_id = $gallery_id AND id = $id")) {

                cmd.Parameters.AddWithValue("$id", picture.id);
                cmd.Parameters.AddWithValue("$gallery_id", gallery.id);

                await Database.ExecuteNonQuery(cmd);

            }

        }

        public static async Task<Picture[]> GetPicturesAsync(Gallery gallery) {

            List<Picture> pictures = new List<Picture>();

            if (!(gallery is null) && gallery.id > 0) {

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Picture WHERE gallery_id = $gallery_id ORDER BY id")) {

                    cmd.Parameters.AddWithValue("$gallery_id", gallery.id);

                    using (DataTable rows = await Database.GetRowsAsync(cmd))
                        foreach (DataRow row in rows.Rows)
                            pictures.Add(Picture.FromDataRow(row));

                }

            }

            return pictures.ToArray();

        }

    }

}