using Discord;
using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Messaging;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OurFoodChain.Common.Extensions;

namespace OurFoodChain.Bot.Modules {

    public class MapModule :
         OfcModuleBase {

        // Public members

        [Command("map")]
        public async Task Map() {

            await ShowMapAsync(string.Empty);

        }
        [Command("map")]
        public async Task Map([Remainder]string mapName) {

            await ShowMapAsync(mapName);

        }

        [Command("addmap"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddMap(string imageUrl, string mapName = "", string description = "") {

            IPictureGallery gallery = await GetMapGalleryAsync();

            // Remove any pictures with the same name.

            foreach (IPicture pictureWithSameName in gallery.Where(picture => picture.Name?.Equals(mapName, StringComparison.OrdinalIgnoreCase) ?? false).ToArray())
                gallery.Pictures.Remove(pictureWithSameName);

            // Add the new picture to the gallery.

            IPicture newPicture = new Picture(imageUrl) {
                Name = mapName,
                Description = description
            };

            gallery.Pictures.Add(newPicture);

            await Db.UpdateGalleryAsync(gallery);

            if (string.IsNullOrEmpty(mapName))
                await ReplySuccessAsync($"Successfully added new {"map".FromLink(imageUrl)}.");
            else
                await ReplySuccessAsync($"Successfully added new {"map".FromLink(imageUrl)}, {mapName.ToTitle().ToBold()}.");

        }
        [Command("setmap"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetMap(string imageUrl) {

            await AddMap(imageUrl, PrimaryMapName);

        }

        // Private members

        private const string MapGalleryName = "map";
        private const string PrimaryMapName = "primary";
        private const string LabeledMapName = "labeled";

        private async Task<IPictureGallery> GetMapGalleryAsync() {

            IPictureGallery gallery = await Db.GetGalleryAsync(MapGalleryName);

            if (gallery is null)
                gallery = new PictureGallery(null, MapGalleryName, Enumerable.Empty<IPicture>());

            return gallery;

        }
        private async Task ShowMapAsync(string mapName = "") {

            // Display a paginated list of maps.
            // If a "primary" map is specified, show that map first.
            // If a "labeled" map is specified, omit that map from pagination and show it when the user presses the "Z" reaction.

            IPictureGallery gallery = await GetMapGalleryAsync();

            if (gallery is null || !gallery.Pictures.Any()) {

                await ReplyInfoAsync($"No map images have been set. Use the `{Config.Prefix}addmap` or `{Config.Prefix}setmap` command to add map images.");

            }
            else {

                IPaginatedMessage message = BuildMapEmbed(gallery);

                if (!string.IsNullOrWhiteSpace(mapName)) {

                    IPicture selectedMapPicture = gallery.GetPicture(mapName);

                    if (selectedMapPicture is null) {

                        await ReplyErrorAsync($"No map with the name {mapName.ToTitle().ToBold()} exists.");

                    }
                    else {

                        for (int i = 0; i < message.Count(); ++i) {

                            if (message.ElementAt(i).Embed?.ImageUrl?.Equals(selectedMapPicture.Url, StringComparison.OrdinalIgnoreCase) ?? false) {

                                await message.GoToAsync(i);

                                break;

                            }

                        }

                        await ReplyAsync(message);

                    }

                }
                else {

                    await ReplyAsync(message);

                }

            }

        }

        private IPaginatedMessage BuildMapEmbed(IPictureGallery gallery) {

            IPicture primaryMap = GetPictureByNames(gallery, new string[] { PrimaryMapName, "" }) ?? gallery.First();
            IPicture labeledMap = GetPictureByNames(gallery, new string[] { LabeledMapName, "zones" });

            List<IPicture> mapPictures = new List<IPicture>();

            mapPictures.AddRange(gallery.Where(picture => picture.Id != primaryMap?.Id && picture.Id != labeledMap?.Id));

            string primaryMapTitle = string.IsNullOrEmpty(Config.WorldName) ? "World Map" : $"Map of {Config.WorldName.ToTitle()}";
            string primaryMapFooter = labeledMap is null ? string.Empty : "Click the Z reaction to toggle zone labels.";

            IPaginatedMessage message = new PaginatedMessage();

            if (primaryMap != null) {

                message.AddPage(new Discord.Messaging.Embed() {
                    Title = primaryMapTitle,
                    Footer = primaryMapFooter,
                    ImageUrl = primaryMap.Url
                });

            }

            foreach (IPicture mapPicture in mapPictures) {

                message.AddPage(new Discord.Messaging.Embed() {
                    Title = $"{mapPicture.GetName().ToTitle()} Map",
                    ImageUrl = mapPicture.Url
                });

            }

            if (labeledMap != null) {

                message.AddPage(new Discord.Messaging.Embed() {
                    Title = primaryMapTitle,
                    Footer = primaryMapFooter,
                    ImageUrl = labeledMap.Url
                });

                message.MaximumIndex -= 1;

                message.AddReaction("🇿", async (args) => {

                    int targetIndex = args.Message.Count() - 1;

                    if (args.Message.CurrentIndex == targetIndex)
                        targetIndex = 0;

                    if (args.Message.CurrentIndex == 0 || targetIndex == 0)
                        await args.Message.GoToAsync(targetIndex);

                });

            }

            return message;

        }
        private IPicture GetPictureByNames(IPictureGallery gallery, IEnumerable<string> names) {

            return gallery
                .Where(picture => names.Any(name => picture.GetName().Equals(name, StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault();


        }

    }

}