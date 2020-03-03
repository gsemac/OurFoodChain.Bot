using Discord;
using Discord.Commands;
using OurFoodChain.Adapters;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class GalleryCommands :
        ModuleBase {

        // Public members

        public IOfcBotConfiguration Config { get; set; }
        public SQLiteDatabase Db { get; set; }

        [Command("setpic"), Alias("setspeciespic", "setspic")]
        public async Task SetPic(string species, string imageUrl) {

            await SetPic(string.Empty, species, imageUrl);

        }
        [Command("setpic"), Alias("setspeciespic", "setspic")]
        public async Task SetPic(string genusName, string speciesName, string imageUrl) {

            // Updates the default picture for the given species.
            // While any users can add pictures for species, only moderators can update the default picture.

            Species species = await BotUtils.ReplyFindSpeciesAsync(Context, genusName, speciesName);

            if (species is null)
                return;

            if (await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, Config, PrivilegeLevel.ServerModerator, species) && await BotUtils.ReplyIsImageUrlValidAsync(Context, imageUrl)) {

                IPictureGallery gallery = await Db.GetPictureGalleryAsync(new SpeciesAdapter(species)) ?? new PictureGallery();
                bool isFirstPicture = gallery.Count() <= 0;

                await Db.SetDefaultPictureAsync(new SpeciesAdapter(species), new Picture {
                    Url = imageUrl,
                    Artist = new Creator(isFirstPicture ? species.OwnerName : Context.User.Username)
                });

                await DiscordUtilities.ReplySuccessAsync(Context.Channel, string.Format("Successfully set the picture for **{0}**.", species.ShortName));

            }

        }

        [Command("+pic")]
        public async Task PlusPic(string species, string imageUrl) {

            await PlusPic(string.Empty, species, imageUrl, string.Empty);

        }
        [Command("+pic")]
        public async Task PlusPic(string arg0, string arg1, string arg2) {

            // This command can be used in the following ways:
            // +pic <genus> <species> <url>
            // +pic <species> <url> <description>

            string genus;
            string species;
            string url;
            string description = "";

            if (StringUtilities.IsUrl(arg1)) {

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

            IPictureGallery gallery = await Db.GetPictureGalleryAsync(new SpeciesAdapter(species)) ?? new PictureGallery();

            bool isFirstPicture = gallery.Count() <= 0;
            bool pictureAlreadyExists = gallery
                .Any(x => x.Url == imageUrl);

            IPicture picture = gallery
                .Where(p => p.Url == imageUrl)
                .FirstOrDefault() ?? new Picture();

            picture.Url = imageUrl;
            picture.Description = description;

            if (string.IsNullOrEmpty(picture.Artist?.Name))
                picture.Artist = new Creator(isFirstPicture ? species.OwnerName : Context.User.Username);

            await Db.AddPictureAsync(new SpeciesAdapter(species), picture);

            if (pictureAlreadyExists)
                await DiscordUtilities.ReplySuccessAsync(Context.Channel, string.Format("Successfully updated [picture]({1}) for **{0}**.", species.ShortName, imageUrl));
            else
                await DiscordUtilities.ReplySuccessAsync(Context.Channel, string.Format("Successfully added new [picture]({1}) for **{0}**.", species.ShortName, imageUrl));

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

            if (await BotUtils.ReplyHasPrivilegeAsync(Context, Config, PrivilegeLevel.ServerModerator)) {

                // Get the species.

                Species species = await BotUtils.ReplyFindSpeciesAsync(Context, genusName, speciesName);

                if (species is null)
                    return;

                IEnumerable<IPicture> pictures = await Db.GetAllPicturesAsync(new SpeciesAdapter(species));
                IPicture picture = (pictureIndex >= 0 && pictureIndex < pictures.Count()) ? pictures.ElementAt(pictureIndex) : null;

                // If the image was removed from anywhere (gallery or default species picture), this is set to "true".
                bool success = await Db.RemovePictureAsync(new SpeciesAdapter(species), picture);

                if (success)
                    await DiscordUtilities.ReplySuccessAsync(Context.Channel, string.Format("Successfully removed [picture]({1}) from **{0}**.", species.ShortName, picture.Url));
                else
                    await DiscordUtilities.ReplyWarningAsync(Context.Channel, string.Format("**{0}** has no picture at this index.", species.ShortName));

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

            IEnumerable<IPicture> pictures = await Db.GetAllPicturesAsync(new SpeciesAdapter(species));

            if (pictureIndex >= 0 && pictureIndex < pictures.Count()) {

                IPicture picture = pictures.ElementAt(pictureIndex);
                picture.Artist = new Creator(artist);

                await Db.AddPictureAsync(new SpeciesAdapter(species), picture);

                await DiscordUtilities.ReplySuccessAsync(Context.Channel, string.Format("Successfully updated artist for {0} [picture]({1}) to **{2}**.",
                    StringUtilities.ToPossessive(species.ShortName),
                    picture.Url,
                    artist));

            }
            else
                await DiscordUtilities.ReplyErrorAsync(Context.Channel, string.Format("**{0}** has no picture at this index.", species.ShortName));

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

                    List<IPicture> pictures = new List<IPicture>();

                    if (!string.IsNullOrEmpty(taxon.pics))
                        pictures.Add(new Picture(taxon.pics));

                    foreach (Species sp in await BotUtils.GetSpeciesInTaxonFromDb(taxon))
                        pictures.AddRange(await Db.GetAllPicturesAsync(new SpeciesAdapter(sp)));

                    await ShowGalleryAsync(Context, taxon.GetName(), pictures);

                }

            }
            else if (await BotUtils.ReplyValidateSpeciesAsync(Context, species)) {

                // The requested species does exist, so show its gallery.
                await ShowGalleryAsync(species[0]);

            }

        }
        [Command("gallery"), Alias("pic", "pics", "picture", "pictures", "image", "images")]
        public async Task Gallery(string genus, string species) {

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            await ShowGalleryAsync(sp);

        }

        public static async Task ShowGalleryAsync(ICommandContext context, string galleryName, IEnumerable<IPicture> pictures) {

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
                    string footer = string.Format("\"{0}\" by {1} — {2}", p.Name, p.Artist, p.Caption);

                    embed.WithTitle(title);
                    embed.WithImageUrl(p.Url);
                    embed.WithDescription(p.Description);
                    embed.WithFooter(footer);

                    message.Pages.Add(embed.Build());

                    ++index;

                }

                await Bot.DiscordUtils.SendMessageAsync(context, message);

            }

        }

        // Private members

        private async Task ShowGalleryAsync(Species species) {

            IEnumerable<IPicture> pictures = await Db.GetAllPicturesAsync(new SpeciesAdapter(species));

            await ShowGalleryAsync(Context, species.ShortName, pictures);

        }

    }

}