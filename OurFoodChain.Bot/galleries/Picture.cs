using OurFoodChain.Common.Utilities;
using System;
using System.Data;

namespace OurFoodChain {

    public class Picture {

        public const long NULL_ID = 0;

        public long id = NULL_ID;
        public string url;
        public long galleryId = 0;
        public string name;
        public string description;
        public string footer = "";
        public string artist;

        public Picture() { }
        public Picture(string url) {
            this.url = url;
        }

        public string GetName() {

            if (string.IsNullOrEmpty(name))
                if (!string.IsNullOrEmpty(url))
                    return System.IO.Path.GetFileNameWithoutExtension(url).Replace('_', ' ');
                else
                    return "Untitled";

            return StringUtilities.ToTitleCase(name);

        }
        public string GetArtist() {

            if (string.IsNullOrEmpty(artist))
                return "?";

            return artist;

        }
        public string GetDescriptionOrDefault() {

            if (string.IsNullOrEmpty(description))
                return BotUtils.DEFAULT_DESCRIPTION;

            return description;

        }

        public static Picture FromDataRow(DataRow row) {

            Picture result = new Picture {
                id = row.Field<long>("id"),
                url = row.Field<string>("url"),
                name = row.Field<string>("name"),
                description = row.Field<string>("description"),
                artist = row.Field<string>("artist")
            };

            result.galleryId = (row["gallery_id"] == DBNull.Value) ? 0 : row.Field<long>("gallery_id");

            return result;

        }

    }

}