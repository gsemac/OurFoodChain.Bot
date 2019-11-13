using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class GalleryCommands :
        ModuleBase {

        [Command("setpic"), Alias("setspeciespic", "setspic")]
        public async Task SetPic(string species, string imageUrl) {
            await SetPic("", species, imageUrl);
        }
        [Command("setpic"), Alias("setspeciespic", "setspic")]
        public async Task SetPic(string genusName, string speciesName, string imageUrl) {

            // Updates the default picture for the given species.
            // While any users can add pictures for species, only moderators can update the default picture.

            Species species = await BotUtils.ReplyFindSpeciesAsync(Context, genusName, speciesName);

            if (species is null)
                return;

            if (await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, PrivilegeLevel.ServerModerator, species) && await BotUtils.ReplyIsImageUrlValidAsync(Context, imageUrl)) {

                Picture[] pictures = await GalleryUtils.GetPicturesAsync(await GalleryUtils.GetGalleryAsync(species));
                bool first_picture = pictures.Count() <= 0;

                await SpeciesUtils.SetPictureAsync(species, new Picture {
                    url = imageUrl,
                    artist = first_picture ? species.OwnerName : Context.User.Username
                });

                await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully set the picture for **{0}**.", species.ShortName));

            }

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
        public async Task PlusPic(string genusName, string speciesName, string imageUrl, string description) {

            // Get the species.

            Species species = await BotUtils.ReplyFindSpeciesAsync(Context, genusName, speciesName);

            if (species is null)
                return;

            // Validate the image URL.

            if (!await BotUtils.ReplyIsImageUrlValidAsync(Context, imageUrl))
                return;

            // Add the new picture to the gallery.

            // If this is the first picture we've added to the species, set the artist as the species' owner.
            // Otherwise, set the artist to the person submitting the image.

            Picture[] pictures = await GalleryUtils.GetPicturesAsync(await GalleryUtils.GetGalleryAsync(species));

            bool firstPicture = pictures.Count() <= 0;
            bool pictureAlreadyExists = pictures.Any(x => x.url == imageUrl);

            Picture picture = pictures.Where(x => x.url == imageUrl).FirstOrDefault() ?? new Picture();

            picture.url = imageUrl;
            picture.description = description;

            if (string.IsNullOrEmpty(picture.artist))
                picture.artist = firstPicture ? species.OwnerName : Context.User.Username;

            await SpeciesUtils.AddPictureAsync(species, picture);

            if (pictureAlreadyExists)
                await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated [picture]({1}) for **{0}**.", species.ShortName, imageUrl));
            else
                await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully added new [picture]({1}) for **{0}**.", species.ShortName, imageUrl));

        }

        [Command("-pic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task MinusPic(string speciesName) {
            await MinusPic(string.Empty, speciesName);
        }
        [Command("-pic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task MinusPic(string genusName, string speciesName) {
            await MinusPic(genusName, speciesName, 1);
        }
        [Command("-pic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task MinusPic(string speciesName, int pictureIndex) {
            await MinusPic(string.Empty, speciesName, pictureIndex);
        }
        [Command("-pic"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task MinusPic(string genusName, string speciesName, int pictureIndex) {

            // Decrease the picture index by 1 (since users are expected to use the indices as shown by the "gallery" command, which begin at 1).
            --pictureIndex;

            if (await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.ServerModerator)) {

                // Get the species.

                Species species = await BotUtils.ReplyFindSpeciesAsync(Context, genusName, speciesName);

                if (species is null)
                    return;

                Picture[] pictures = await SpeciesUtils.GetPicturesAsync(species);
                Picture picture = (pictureIndex >= 0 && pictureIndex < pictures.Count()) ? pictures[pictureIndex] : null;

                // If the image was removed from anywhere (gallery or default species picture), this is set to "true".
                bool success = await SpeciesUtils.RemovePictureAsync(species, picture);

                if (success)
                    await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully removed [picture]({1}) from **{0}**.", species.ShortName, picture.url));
                else
                    await BotUtils.ReplyAsync_Warning(Context, string.Format("**{0}** has no picture at this index.", species.ShortName));

            }

        }

        [Command("setartist"), Alias("setcredit"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetArtist(string speciesName, int pictureIndex, string artist) {
            await SetArtist(string.Empty, speciesName, pictureIndex, artist);
        }
        [Command("setartist"), Alias("setcredit"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetArtist(string speciesName, string artist) {
            await SetArtist(string.Empty, speciesName, 1, artist);
        }
        [Command("setartist"), Alias("setcredit"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetArtist(string genusName, string speciesName, string artist) {
            await SetArtist(genusName, speciesName, 1, artist);
        }
        [Command("setartist"), Alias("setcredit"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetArtist(string genusName, string speciesName, int pictureIndex, string artist) {

            // Decrease the picture index by 1 (since users are expected to use the indices as shown by the "gallery" command, which begin at 1).
            --pictureIndex;

            Species species = await BotUtils.ReplyFindSpeciesAsync(Context, genusName, speciesName);

            if (species is null)
                return;

            Picture[] pictures = await SpeciesUtils.GetPicturesAsync(species);

            if (pictureIndex >= 0 && pictureIndex < pictures.Count()) {

                Picture picture = pictures[pictureIndex];
                picture.artist = artist;

                await SpeciesUtils.AddPictureAsync(species, picture);

                await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated artist for {0} [picture]({1}) to **{2}**.",
                    StringUtils.ToPossessive(species.ShortName),
                    picture.url,
                    artist));

            }
            else
                await BotUtils.ReplyAsync_Error(Context, string.Format("**{0}** has no picture at this index.", species.ShortName));

        }
        [Command("setartist"), Alias("setcredit"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetArtist(string speciesName, int pictureIndex, IUser user) {
            await SetArtist(string.Empty, speciesName, pictureIndex, user);
        }
        [Command("setartist"), Alias("setcredit"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetArtist(string speciesName, IUser user) {
            await SetArtist(string.Empty, speciesName, user);
        }
        [Command("setartist"), Alias("setcredit"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetArtist(string genusName, string speciesName, IUser user) {
            await SetArtist(genusName, speciesName, 1, user);
        }
        [Command("setartist"), Alias("setcredit"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetArtist(string genusName, string speciesName, int pictureIndex, IUser user) {
            await SetArtist(genusName, speciesName, pictureIndex, user.Username);
        }

        [Command("gallery"), Alias("pic", "pics", "picture", "pictures", "image", "images")]
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
                        pictures.AddRange(await SpeciesUtils.GetPicturesAsync(sp));

                    await ShowGalleryAsync(Context, taxon.GetName(), pictures.ToArray());

                }

            }
            else if (await BotUtils.ReplyValidateSpeciesAsync(Context, species)) {

                // The requested species does exist, so show its gallery.
                await _showGallery(species[0]);

            }

        }
        [Command("gallery"), Alias("pic", "pics", "picture", "pictures", "image", "images")]
        public async Task Gallery(string genus, string species) {

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            await _showGallery(sp);

        }

        private async Task _showGallery(Species species) {

            Picture[] pictures = await SpeciesUtils.GetPicturesAsync(species);

            await ShowGalleryAsync(Context, species.ShortName, pictures);

        }

        public static async Task ShowGalleryAsync(ICommandContext context, string galleryName, Picture[] pictures) {

            // If there were no images for this query, show a message and quit.

            if (pictures.Count() <= 0) {

                await BotUtils.ReplyAsync_Info(context, string.Format("**{0}** does not have any pictures.", galleryName));

            }
            else {

                // Display a paginated image gallery.

                Bot.PaginatedMessage message = new Bot.PaginatedMessage();
                int index = 1;

                foreach (Picture p in pictures) {

                    EmbedBuilder embed = new EmbedBuilder();

                    string title = string.Format("Pictures of {0} ({1} of {2})", galleryName, index, pictures.Count());
                    string footer = string.Format("\"{0}\" by {1} — {2}", p.GetName(), p.GetArtist(), p.footer);

                    embed.WithTitle(title);
                    embed.WithImageUrl(p.url);
                    embed.WithDescription(p.description);
                    embed.WithFooter(footer);

                    message.Pages.Add(embed.Build());

                    ++index;

                }

                await Bot.DiscordUtils.SendMessageAsync(context, message);

            }

        }

    }

}