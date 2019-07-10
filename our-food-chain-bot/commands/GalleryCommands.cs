using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class GalleryCommands :
        ModuleBase {

        [Command("setpic"), Alias("setspeciespic", "setspic")]
        public async Task SetPic(string species, string imageUrl) {
            await SetPic("", species, imageUrl);
        }
        [Command("setpic"), Alias("setspeciespic", "setspic")]
        public async Task SetPic(string genus, string species, string imageUrl) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, PrivilegeLevel.ServerModerator, sp))
                return;

            if (!await BotUtils.ReplyAsync_ValidateImageUrl(Context, imageUrl))
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET pics=$url WHERE id=$species_id;")) {

                cmd.Parameters.AddWithValue("$url", imageUrl);
                cmd.Parameters.AddWithValue("$species_id", sp.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully set the picture for **{0}**.", sp.GetShortName()));

        }

        [Command("+pic")]
        public async Task PlusPic(string species, string imageUrl) {
            await PlusPic("", species, imageUrl, "");
        }
        [Command("+pic")]
        public async Task PlusPic(string arg0, string arg1, string arg2) {

            // This command can be used in the following ways:
            // +pic <genus> <species> <url>
            // +pic <species> <url> <description>

            string genus = "";
            string species = "";
            string url = "";
            string description = "";

            if (StringUtils.IsUrl(arg1)) {

                // <species> <url> <description>

                genus = "";
                species = arg0;
                url = arg1;
                description = arg2;

            }
            else {

                // <genus> <species> <url>

                genus = arg0;
                species = arg1;
                url = arg2;

            }

            await PlusPic(genus, species, url, description);

        }
        [Command("+pic")]
        public async Task PlusPic(string genus, string species, string imageUrl, string description) {

            // Get the species.

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            // Validate the image URL.

            if (!await BotUtils.ReplyAsync_ValidateImageUrl(Context, imageUrl))
                return;

            // If the species doesn't have a picture yet, use this as the picture for that species.

            if (string.IsNullOrEmpty(sp.pics)) {

                using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Species SET pics=$url WHERE id=$species_id;")) {

                    cmd.Parameters.AddWithValue("$url", imageUrl);
                    cmd.Parameters.AddWithValue("$species_id", sp.id);

                    await Database.ExecuteNonQuery(cmd);

                }

            }

            // Create a gallery for the species if it doesn't already exist.

            string gallery_name = "species" + sp.id.ToString();

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Gallery(name) VALUES($name);")) {

                cmd.Parameters.AddWithValue("$name", gallery_name);

                await Database.ExecuteNonQuery(cmd);

            }

            // Get the gallery for the species.

            Gallery gallery = await BotUtils.GetGalleryFromDb(gallery_name);

            if (gallery is null) {

                await BotUtils.ReplyAsync_Error(Context, string.Format("Could not create a picture gallery for **{0}**.", sp.GetShortName()));

                return;

            }

            // Add the new picture to the gallery.

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Picture(url, gallery_id, artist, description) VALUES($url, $gallery_id, $artist, $description);")) {

                cmd.Parameters.AddWithValue("$url", imageUrl);
                cmd.Parameters.AddWithValue("$gallery_id", gallery.id);
                cmd.Parameters.AddWithValue("$artist", Context.User.Username);
                cmd.Parameters.AddWithValue("$description", description);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully added new picture for **{0}**.", sp.GetShortName()));

        }

        [Command("gallery"), Alias("pic", "pics", "pictures")]
        public async Task Gallery(string speciesOrTaxon) {

            // Prioritize species galleries first.

            Species[] species = await BotUtils.GetSpeciesFromDb("", speciesOrTaxon);

            if (species is null || species.Count() <= 0) {

                // No such species exists, so check if a taxon exists.

                Taxon taxon = await BotUtils.GetTaxonFromDb(speciesOrTaxon);

                if (taxon is null) {

                    // If no such taxon exists, show species recommendations to the user.
                    await BotUtils.ReplyAsync_SpeciesSuggestions(Context, "", speciesOrTaxon);

                }
                else {

                    // The taxon does exist, so we'll generate a gallery from this taxon.
                    // First, images for this taxon will be added, followed by the galleries for all species under it.

                    List<Picture> pictures = new List<Picture>();

                    if (!string.IsNullOrEmpty(taxon.pics))
                        pictures.Add(new Picture(taxon.pics));

                    foreach (Species sp in await BotUtils.GetSpeciesInTaxonFromDb(taxon))
                        pictures.AddRange(await _getGallery(sp));

                    await _showGallery(taxon.GetName(), pictures.ToArray());

                }

            }
            else if (await BotUtils.ReplyAsync_ValidateSpecies(Context, species)) {

                // The requested species does exist, so show its gallery.
                await _showGallery(species[0]);

            }

        }
        [Command("gallery"), Alias("pic", "pics", "pictures")]
        public async Task Gallery(string genus, string species) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(Context, genus, species);

            if (sp is null)
                return;

            await _showGallery(sp);

        }

        private async Task<List<Picture>> _getGallery(Species species) {

            List<Picture> pictures = new List<Picture>();

            // If the species has a picture assigned to it, add that as the first picture.

            if (!string.IsNullOrEmpty(species.pics))
                pictures.Add(new Picture {
                    url = species.pics,
                    artist = await species.GetOwnerOrDefault(Context),
                    footer = string.Format("Depiction of {0}", species.GetShortName())
                });

            // Check the database for additional pictures to add to the gallery.
            // We'll do this by generating a default gallery name for the species, and then checking that gallery.

            string gallery_name = "species" + species.id.ToString();

            Gallery gallery = await BotUtils.GetGalleryFromDb(gallery_name);

            if (!(gallery is null))
                foreach (Picture p in await BotUtils.GetPicsFromDb(gallery)) {

                    // Make sure we don't add the default picture twice.
                    // However, if we come across it and it has a description, set the description for the default picture.

                    p.footer = string.Format("Depiction of {0}", species.GetShortName());

                    if (p.url != species.pics)
                        pictures.Add(p);
                    else if (pictures.Count() > 0 && p.url == pictures[0].url)
                        pictures[0].description = p.description;

                }

            return pictures;

        }
        private async Task _showGallery(Species species) {

            List<Picture> pictures = await _getGallery(species);

            await _showGallery(species.GetShortName(), pictures.ToArray());

        }
        private async Task _showGallery(string galleryName, Picture[] pictures) {

            // If there were no images for this query, show a message and quit.

            if (pictures.Count() <= 0) {

                await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** does not have any pictures.", galleryName));

            }
            else {

                // Display a paginated image gallery.

                CommandUtils.PaginatedMessage message = new CommandUtils.PaginatedMessage();
                int index = 1;

                foreach (Picture p in pictures) {

                    EmbedBuilder embed = new EmbedBuilder();

                    string title = string.Format("Pictures of {0} ({1} of {2})", galleryName, index, pictures.Count());
                    string footer = string.Format("\"{0}\" by {1} — {2}", p.GetName(), p.GetArtist(), p.footer);

                    embed.WithTitle(title);
                    embed.WithImageUrl(p.url);
                    embed.WithDescription(p.description);
                    embed.WithFooter(footer);

                    message.pages.Add(embed.Build());

                    ++index;

                }

                await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, message);

            }

        }

    }

}