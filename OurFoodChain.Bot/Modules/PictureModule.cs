using Discord;
using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Messaging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class PictureModule :
        OfcModuleBase {

        // Public members

        [Command("setpic"), Alias("setspeciespic", "setspic")]
        public async Task SetPic(string speciesName, string imageUrl) {

            await SetPic(string.Empty, speciesName, imageUrl);

        }
        [Command("setpic"), Alias("setspeciespic", "setspic")]
        public async Task SetPic(string genusName, string speciesName, string imageUrl) {

            // Updates the default picture for the given species.
            // While any users can add pictures for species, only moderators can update the default picture.

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                if (await ReplyValidatePrivilegeAsync(PrivilegeLevel.ServerModerator, species) && await ReplyValidateImageUrlAsync(imageUrl)) {

                    IPictureGallery gallery = await Db.GetPictureGalleryAsync(species) ?? new PictureGallery();
                    bool isFirstPicture = gallery.Count() <= 0;

                    await Db.SetPictureAsync(species, new Picture {
                        Url = imageUrl,
                        Artist = isFirstPicture ? species.Creator : Context.User.ToCreator()
                    });

                    await ReplySuccessAsync($"Successfully set the picture for {species.GetShortName().ToBold()}.");

                }

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

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid() && await ReplyValidateImageUrlAsync(imageUrl)) {

                // Add the new picture to the gallery.

                // If this is the first picture we've added to the species, set the artist as the species' owner.
                // Otherwise, set the artist to the person submitting the image.

                IPictureGallery gallery = await Db.GetPictureGalleryAsync(species) ?? new PictureGallery();

                bool isFirstPicture = gallery.Count() <= 0;
                bool pictureAlreadyExists = gallery
                    .Any(x => x.Url == imageUrl);

                IPicture picture = gallery
                    .Where(p => p.Url == imageUrl)
                    .FirstOrDefault() ?? new Picture();

                picture.Url = imageUrl;
                picture.Description = description;

                if (string.IsNullOrEmpty(picture.Artist?.Name))
                    picture.Artist = isFirstPicture ? species.Creator : Context.User.ToCreator();

                await Db.AddPictureAsync(species, picture);

                if (pictureAlreadyExists)
                    await ReplySuccessAsync($"Successfully updated {imageUrl.ToLink("picture")} for {species.GetShortName().ToBold()}.");
                else
                    await ReplySuccessAsync($"Successfully added new {imageUrl.ToLink("picture")} for {species.GetShortName().ToBold()}.");

            }

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

            // Get the species.

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                IEnumerable<IPicture> pictures = await Db.GetPicturesAsync(species);
                IPicture picture = (pictureIndex >= 0 && pictureIndex < pictures.Count()) ? pictures.ElementAt(pictureIndex) : null;

                // If the image was removed from anywhere (gallery or default species picture), this is set to "true".

                bool success = await Db.RemovePictureAsync(species, picture);

                if (success)
                    await ReplySuccessAsync($"Successfully removed {picture.Url.ToLink("picture")} from {species.GetShortName().ToBold()}.");
                else
                    await ReplySuccessAsync($"{species.GetShortName().ToBold()} has no picture at this index.");

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

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                IEnumerable<IPicture> pictures = await Db.GetPicturesAsync(species);

                if (pictureIndex >= 0 && pictureIndex < pictures.Count()) {

                    IPicture picture = pictures.ElementAt(pictureIndex);
                    picture.Artist = new Creator(artist);

                    await Db.AddPictureAsync(species, picture);

                    await ReplySuccessAsync(string.Format("Successfully updated artist for {0} [picture]({1}) to **{2}**.",
                        StringUtilities.ToPossessive(species.GetShortName()),
                        picture.Url,
                        artist));

                }
                else
                    await ReplyErrorAsync(string.Format("**{0}** has no picture at this index.", species.GetShortName()));

            }

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
        public async Task Gallery(string arg0) {

            // Possible cases:
            // 1. <species>
            // 2. <taxon>

            // Prioritize species galleries first.

            IEnumerable<ISpecies> matchingSpecies = await Db.GetSpeciesAsync(arg0);

            if (matchingSpecies.Count() <= 0) {

                // No such species exists, so check if a taxon exists.

                ITaxon taxon = (await Db.GetTaxaAsync(arg0)).FirstOrDefault();

                if (!taxon.IsValid()) {

                    // If no such taxon exists, show species recommendations to the user.

                    await ReplyValidateSpeciesAsync(matchingSpecies);

                }
                else {

                    // The taxon does exist, so we'll generate a gallery from this taxon.
                    // First, images for this taxon will be added, followed by the galleries for all species under it.

                    List<IPicture> pictures = new List<IPicture>();

                    pictures.AddRange(taxon.Pictures);

                    foreach (ISpecies species in await Db.GetSpeciesAsync(taxon))
                        pictures.AddRange(await Db.GetPicturesAsync(species));

                    await ShowGalleryAsync(taxon.GetName(), pictures);

                }

            }
            else {

                // We got one or more matching species.

                ISpecies species = await ReplyValidateSpeciesAsync(matchingSpecies);

                if (species.IsValid())
                    await ShowGalleryAsync(species);

            }

        }
        [Command("gallery"), Alias("pic", "pics", "picture", "pictures", "image", "images")]
        public async Task Gallery(string genusName, string speciesName) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid())
                await ShowGalleryAsync(species);

        }

        // Private members

        public async Task ShowGalleryAsync(string galleryName, IEnumerable<IPicture> pictures) {

            if (pictures.Count() <= 0) {

                // If there were no images for this query, show a message and quit.

                await ReplyInfoAsync($"{galleryName.ToTitle().ToBold()} does not have any pictures.");

            }
            else {

                // Display a paginated image gallery.

                IPaginatedMessage message = new PaginatedMessage();

                int index = 1;

                foreach (Picture p in pictures) {

                    Discord.Messaging.IEmbed embed = new Discord.Messaging.Embed();

                    string title = string.Format("Pictures of {0} ({1} of {2})", galleryName, index, pictures.Count());
                    string footer = string.Format("\"{0}\" by {1} — {2}", p.Name, p.Artist, p.Caption);

                    embed.Title = title;
                    embed.ImageUrl = p.Url;
                    embed.Description = p.Description;
                    embed.Footer = footer;

                    message.AddPage(embed);

                    ++index;

                }

                await ReplyAsync(message);

            }

        }
        private async Task ShowGalleryAsync(ISpecies species) {

            IEnumerable<IPicture> pictures = await Db.GetPicturesAsync(species);

            await ShowGalleryAsync(species.GetShortName(), pictures);

        }

    }

}