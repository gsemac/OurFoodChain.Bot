using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class GalleryCommands :
        ModuleBase {

        [Command("gallery"), Alias("pic", "pics")]
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
        [Command("gallery"), Alias("pic", "pics")]
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
                    description = string.Format("Depiction of {0}", species.GetShortName())
                });

            // Check the database for additional pictures to add to the gallery.
            // We'll do this by generating a default gallery name for the species, and then checking that gallery.

            string gallery_name = "species" + species.id.ToString();

            Gallery gallery = await BotUtils.GetGalleryFromDb(gallery_name);

            if (!(gallery is null))
                foreach (Picture p in await BotUtils.GetPicsFromDb(gallery))
                    if (p.url != species.pics)
                        pictures.Add(p);

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
                    string footer = string.Format("\"{0}\" by {2} — {1}", p.GetName(), p.GetDescriptionOrDefault(), p.GetArtist());

                    embed.WithTitle(title);
                    embed.WithImageUrl(p.url);
                    embed.WithFooter(footer);

                    message.pages.Add(embed.Build());

                    ++index;

                }

                await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, message);

            }

        }

    }

}