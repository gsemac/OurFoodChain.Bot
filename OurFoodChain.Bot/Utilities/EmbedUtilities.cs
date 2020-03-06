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
        public static async Task<IPaginatedMessage> BuildTaxonMessageAsync(ITaxon taxon, IOfcBotContext botContext) {

            if (!taxon.IsValid())
                return null;

            List<string> subItems = new List<string>();

            if (taxon.Rank.Type == TaxonRankType.Species) {

                ISpecies species = await botContext.Database.GetSpeciesAsync(taxon.Id);

                return await BuildSpeciesMessageAsync(species, botContext);

            }
            else if (taxon.Rank.Type == TaxonRankType.Genus) {

                // For genera, get all species underneath it.
                // This will let us check if the species is extinct, and cross it out if that's the case.

                List<ISpecies> speciesList = new List<ISpecies>();

                foreach (ITaxon subtaxon in await botContext.Database.GetSubtaxaAsync(taxon))
                    speciesList.Add(await botContext.Database.GetSpeciesAsync(subtaxon.Id));

                speciesList.Sort((lhs, rhs) => lhs.GetName().CompareTo(rhs.GetName()));

                foreach (ISpecies species in speciesList.Where(s => s.IsValid())) {

                    if (species.Status.IsExinct)
                        subItems.Add(string.Format("~~{0}~~", species.GetName()));
                    else
                        subItems.Add(species.GetName());

                }

            }
            else {

                // Get all subtaxa under this taxon.

                IEnumerable<ITaxon> subtaxa = await botContext.Database.GetSubtaxaAsync(taxon);

                // Add all subtaxa to the list.

                foreach (ITaxon subtaxon in subtaxa) {

                    if (subtaxon.Rank.Type == TaxonRankType.Species) {

                        // Do not attempt to count sub-taxa for species.

                        subItems.Add(subtaxon.GetName());

                    }
                    else {

                        // Count the number of species under this taxon.
                        // Taxa with no species under them will not be displayed.

                        long species_count = await CountSpeciesInTaxonFromDb(t);

                        if (species_count <= 0)
                            continue;

                        // Count the sub-taxa under this taxon.

                        long subtaxa_count = 0;

                        using (SQLiteCommand cmd = new SQLiteCommand(string.Format("SELECT COUNT(*) FROM {0} WHERE {1}=$parent_id;",
                            Taxon.TypeToDatabaseTableName(t.GetChildRank()),
                            Taxon.TypeToDatabaseColumnName(t.type)
                            ))) {

                            cmd.Parameters.AddWithValue("$parent_id", t.id);

                            subtaxa_count = await Database.GetScalar<long>(cmd);

                        }

                        // Add the taxon to the list.

                        if (subtaxa_count > 0)
                            subItems.Add(string.Format("{0} ({1})", t.GetName(), subtaxa_count));

                    }

                }

            }

            // Generate embed pages.

            string title = string.IsNullOrEmpty(taxon.CommonName) ? taxon.GetName() : string.Format("{0} ({1})", taxon.GetName(), taxon.GetCommonName());
            string field_title = string.Format("{0} in this {1} ({2}):", StringUtilities.ToTitleCase(Taxon.GetRankName(Taxon.TypeToChildType(type), plural: true)), Taxon.GetRankName(type), subItems.Count());
            string thumbnail_url = taxon.pics;

            StringBuilder description = new StringBuilder();
            description.AppendLine(taxon.GetDescriptionOrDefault());

            if (subItems.Count() <= 0) {

                description.AppendLine();
                description.AppendLine(string.Format("This {0} contains no {1}.", Taxon.GetRankName(type), Taxon.GetRankName(Taxon.TypeToChildType(type), plural: true)));

            }

            List<EmbedBuilder> embed_pages = EmbedUtils.ListToEmbedPages(subItems, fieldName: field_title);
            Bot.PaginatedMessageBuilder embed = new Bot.PaginatedMessageBuilder(embed_pages);

            embed.SetTitle(title);
            embed.SetThumbnailUrl(thumbnail_url);
            embed.SetDescription(description.ToString());

            if (subItems.Count() > 0 && taxon.type != TaxonRank.Genus)
                embed.AppendFooter(string.Format(" — Empty {0} are not listed.", Taxon.GetRankName(taxon.GetChildRank(), plural: true)));

            await Bot.DiscordUtils.SendMessageAsync(context, embed.Build());

        }

    }

}