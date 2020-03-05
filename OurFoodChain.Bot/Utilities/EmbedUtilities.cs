using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Generations;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Messaging;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Utilities {

    public static class EmbedUtilities {

        public static async Task<IPaginatedMessage> BuildSpeciesMessageAsync(ISpecies species, IOfcBotContext botContext) {

            if (!species.IsValid())
                return null;

            Embed embed = new Embed {
                Title = species.FullName,
                Color = global::Discord.Color.Blue.ToSystemDrawingColor()
            };

            if (species.CommonNames.Count() > 0)
                embed.Title += string.Format(" ({0})", string.Join(", ", species.CommonNames));

            if (botContext.Configuration.GenerationsEnabled) {

                // Add a field for the generation.

                IGeneration gen = await botContext.Database.GetGenerationByDateAsync(species.CreationDate);

                embed.AddField("Gen", gen is null ? "???" : gen.Number.ToString(), inline: true);

            }

            // Add a field for the species owner.

            embed.AddField("Owner", await DiscordUtilities.GetDiscordUserFromCreatorAsync(botContext.CommandContext, species.Creator), inline: true);

            // Add a field for the species' zones.

            IEnumerable<ISpeciesZoneInfo> speciesZoneList = await botContext.Database.GetZonesAsync(species);

            if (speciesZoneList.Count() > 0)
                embed.Color = (await botContext.Database.GetZoneTypeAsync(speciesZoneList.GroupBy(x => x.Zone.TypeId).OrderBy(x => x.Count()).Last().Key)).Color;

            string zonesFieldValue = speciesZoneList.ToString(ZoneListToStringOptions.None, DiscordUtilities.MaxFieldLength);

            embed.AddField("Zone(s)", string.IsNullOrEmpty(zonesFieldValue) ? "None" : zonesFieldValue, inline: true);

            // Add the species' description.

            StringBuilder descriptionBuilder = new StringBuilder();

            if (species.Status.IsExinct) {

                embed.Title = "[EXTINCT] " + embed.Title;
                embed.Color = Color.Red;

                if (!string.IsNullOrEmpty(species.Status.ExtinctionReason))
                    descriptionBuilder.AppendLine(string.Format("**Extinct ({0}):** _{1}_\n", await BotUtils.TimestampToDateStringAsync(DateUtilities.GetTimestampFromDate((DateTimeOffset)species.Status.ExtinctionDate), botContext), species.Status.ExtinctionReason));

            }

            descriptionBuilder.Append(species.GetDescriptionOrDefault());

            embed.Description = descriptionBuilder.ToString();

            // Add the species' picture.

            embed.ThumbnailUrl = species.Pictures.FirstOrDefault()?.Url;

            if (!string.IsNullOrEmpty(botContext.Configuration.WikiUrlFormat)) {

                // Discord automatically encodes certain characters in URIs, which doesn't allow us to update the config via Discord when we have "{0}" in the URL.
                // Replace this with the proper string before attempting to call string.Format.

                // string format = botContext.Configuration.WikiUrlFormat.Replace("%7B0%7D", "{0}");

                // embed.Url = string.Format(format, Uri.EscapeUriString(GetWikiPageTitleForSpecies(species, common_names)));

            }

            // Create embed pages.

            IEnumerable<IEmbed> embedPages = Discord.Utilities.EmbedUtilities.CreateEmbedPages(embed, EmbedPaginationOptions.AddPageNumbers);
            IPaginatedMessage paginatedMessage = new PaginatedMessage(embedPages);

            return paginatedMessage;

        }

    }

}