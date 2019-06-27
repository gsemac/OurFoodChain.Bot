using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class MapCommands :
         ModuleBase {

        private const string MAP_GALLERY_NAME = "map";

        [Command("map")]
        public async Task Map() {

            // Get map images from the database.

            Gallery gallery = await BotUtils.GetGalleryFromDb(MAP_GALLERY_NAME);
            Picture primary = null;
            Picture labeled = null;

            if (!(gallery is null)) {

                primary = await BotUtils.GetPicFromDb(gallery, "primary");
                labeled = await BotUtils.GetPicFromDb(gallery, "labeled");

            }

            // If no primary image has been provided, display an error message.

            if (primary is null) {

                await BotUtils.ReplyAsync_Error(Context, string.Format("No map images have been set. Use the \"{0}setmap\" command to set map images.",
                    OurFoodChainBot.GetInstance().GetConfig().prefix));

                return;

            }

            // Build the embed.

            string footer = (labeled is null) ? "" : "Click the Z reaction to toggle zone labels.";

            CommandUtils.PaginatedMessage pagination_message = new CommandUtils.PaginatedMessage();

            // Add the first page (primary image without zone labels).

            pagination_message.pages.Add(new EmbedBuilder {
                ImageUrl = primary.url,
                Footer = new EmbedFooterBuilder { Text = footer }
            }.Build());

            // A second page (with zone labels) is only included in the case an image has been provided.

            if (!(labeled is null)) {

                pagination_message.pages.Add(new EmbedBuilder {
                    ImageUrl = labeled.url,
                    Footer = new EmbedFooterBuilder { Text = footer }
                }.Build());

            }

            // Send the embed.

            IUserMessage message = await ReplyAsync("", false, pagination_message.pages[0]);

            if (pagination_message.pages.Count > 1) {

                pagination_message.emojiToggle = "🇿";
                await message.AddReactionAsync(new Emoji("🇿"));

                CommandUtils.PAGINATED_MESSAGES.Add(message.Id, pagination_message);

            }

        }

        [Command("setmap")]
        public async Task SetMap(string primaryImageUrl) {
            await SetMap(primaryImageUrl, "");
        }
        [Command("setmap")]
        public async Task SetMap(string primaryImageUrl, string labeledImageUrl) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.ServerModerator))
                return;

            // Create an image gallery for storing the map images if one hasn't been created yet.

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Gallery(name) VALUES($name);")) {

                cmd.Parameters.AddWithValue("$name", MAP_GALLERY_NAME);

                await Database.ExecuteNonQuery(cmd);

            }

            Gallery gallery = await BotUtils.GetGalleryFromDb(MAP_GALLERY_NAME);

            // Remove existing images from the gallery.

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Picture WHERE gallery_id = $gallery_id;")) {

                cmd.Parameters.AddWithValue("$gallery_id", gallery.id);

                await Database.ExecuteNonQuery(cmd);

            }

            // Insert the primary map image.

            if (!await BotUtils.ReplyAsync_ValidateImageUrl(Context, primaryImageUrl))
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Picture(url, gallery_id, name, artist) VALUES($url, $gallery_id, $name, $artist);")) {

                cmd.Parameters.AddWithValue("$url", primaryImageUrl);
                cmd.Parameters.AddWithValue("$gallery_id", gallery.id);
                cmd.Parameters.AddWithValue("$name", "primary");
                cmd.Parameters.AddWithValue("$artist", Context.User.Username);

                await Database.ExecuteNonQuery(cmd);

            }

            // Insert the secondary map image (although it does not necessarily have to be provided).

            if (!string.IsNullOrEmpty(labeledImageUrl)) {

                if (!await BotUtils.ReplyAsync_ValidateImageUrl(Context, labeledImageUrl))
                    return;

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Picture(url, gallery_id, name, artist) VALUES($url, $gallery_id, $name, $artist);")) {

                    cmd.Parameters.AddWithValue("$url", labeledImageUrl);
                    cmd.Parameters.AddWithValue("$gallery_id", gallery.id);
                    cmd.Parameters.AddWithValue("$name", "labeled");
                    cmd.Parameters.AddWithValue("$artist", Context.User.Username);

                    await Database.ExecuteNonQuery(cmd);

                }

            }

            await BotUtils.ReplyAsync_Success(Context, "Successfully updated map images.");

        }

    }

}
