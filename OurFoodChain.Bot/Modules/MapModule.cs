using Discord;
using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Modules {

    public class MapModule :
         OfcModuleBase {

        private const string MAP_GALLERY_NAME = "map";

        [Command("map")]
        public async Task Map() {

            // Get map images from the database.

            IPictureGallery gallery = await Db.GetGalleryAsync(MAP_GALLERY_NAME);
            IPicture primary = null;
            IPicture labeled = null;

            if (gallery != null) {

                primary = gallery.GetPicture("primary");
                labeled = gallery.GetPicture("labeled");

            }

            // If no primary image has been provided, display an error message.

            if (primary is null) {

                await BotUtils.ReplyAsync_Error(Context, string.Format("No map images have been set. Use the \"{0}setmap\" command to set map images.",
                    Config.Prefix));

                return;

            }

            // Build the embed.

            string worldName = Config.WorldName;
            string title = string.IsNullOrEmpty(worldName) ? "" : string.Format("Map of {0}", StringUtilities.ToTitleCase(worldName));
            string footer = (labeled is null) ? "" : "Click the Z reaction to toggle zone labels.";

            Bot.PaginatedMessage paginatedMessage = new Bot.PaginatedMessage();

            // Add the first page (primary image without zone labels).

            paginatedMessage.Pages.Add(new EmbedBuilder {
                Title = title,
                ImageUrl = primary.Url,
                Footer = new EmbedFooterBuilder { Text = footer }
            }.Build());

            // A second page (with zone labels) is only included in the case an image has been provided.

            if (!(labeled is null)) {

                paginatedMessage.Pages.Add(new EmbedBuilder {
                    Title = title,
                    ImageUrl = labeled.Url,
                    Footer = new EmbedFooterBuilder().WithText(footer)
                }.Build());

            }

            // Send the embed.

            paginatedMessage.PrevEmoji = string.Empty;
            paginatedMessage.NextEmoji = string.Empty;

            if (paginatedMessage.Pages.Count > 1)
                paginatedMessage.ToggleEmoji = "🇿";

            await Bot.DiscordUtils.SendMessageAsync(Context, paginatedMessage);

        }

        [Command("setmap"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetMap(string primaryImageUrl) {
            await SetMap(primaryImageUrl, "");
        }
        [Command("setmap"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetMap(string primaryImageUrl, string labeledImageUrl) {

            // Create an image gallery for storing the map images if one hasn't been created yet.

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Gallery(name) VALUES($name);")) {

                cmd.Parameters.AddWithValue("$name", MAP_GALLERY_NAME);

                await Db.ExecuteNonQueryAsync(cmd);

            }

            IPictureGallery gallery = await Db.GetGalleryAsync(MAP_GALLERY_NAME);

            // Remove existing images from the gallery.

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Picture WHERE gallery_id = $gallery_id;")) {

                cmd.Parameters.AddWithValue("$gallery_id", gallery.Id);

                await Db.ExecuteNonQueryAsync(cmd);

            }

            // Insert the primary map image.

            if (!await BotUtils.ReplyIsImageUrlValidAsync(Context, primaryImageUrl))
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Picture(url, gallery_id, name, artist) VALUES($url, $gallery_id, $name, $artist);")) {

                cmd.Parameters.AddWithValue("$url", primaryImageUrl);
                cmd.Parameters.AddWithValue("$gallery_id", gallery.Id);
                cmd.Parameters.AddWithValue("$name", "primary");
                cmd.Parameters.AddWithValue("$artist", Context.User.Username);

                await Db.ExecuteNonQueryAsync(cmd);

            }

            // Insert the secondary map image (although it does not necessarily have to be provided).

            if (!string.IsNullOrEmpty(labeledImageUrl)) {

                if (!await BotUtils.ReplyIsImageUrlValidAsync(Context, labeledImageUrl))
                    return;

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Picture(url, gallery_id, name, artist) VALUES($url, $gallery_id, $name, $artist);")) {

                    cmd.Parameters.AddWithValue("$url", labeledImageUrl);
                    cmd.Parameters.AddWithValue("$gallery_id", gallery.Id);
                    cmd.Parameters.AddWithValue("$name", "labeled");
                    cmd.Parameters.AddWithValue("$artist", Context.User.Username);

                    await Db.ExecuteNonQueryAsync(cmd);

                }

            }

            await BotUtils.ReplyAsync_Success(Context, "Successfully updated map images.");

        }

    }

}
