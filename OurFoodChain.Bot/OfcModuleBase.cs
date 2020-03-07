using Discord;
using Discord.Commands;
using OurFoodChain.Bot;
using OurFoodChain.Common;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Generations;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Messaging;
using OurFoodChain.Discord.Services;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class OfcModuleBase :
        ModuleBase {

        // Public members

        public IDatabaseService DatabaseService { get; set; }
        public IPaginatedMessageService PaginatedMessageService { get; set; }
        public IOfcBotConfiguration Config { get; set; }
        public Services.TrophyScanner TrophyScanner { get; set; }

        public SQLiteDatabase Db => GetDatabaseAsync().Result;
        public SearchContext SearchContext => new SearchContext(Context, Db);
        public OfcBotContext BotContext => new OfcBotContext(Context, Config, Db);

        public async Task<SQLiteDatabase> GetDatabaseAsync() => await DatabaseService.GetDatabaseAsync(Context.Guild.Id);

        public async Task ReplyInfoAsync(string message) => await DiscordUtilities.ReplyInfoAsync(Context.Channel, message);
        public async Task ReplyWarningAsync(string message) => await DiscordUtilities.ReplyWarningAsync(Context.Channel, message);
        public async Task ReplyErrorAsync(string message) => await DiscordUtilities.ReplyErrorAsync(Context.Channel, message);
        public async Task ReplySuccessAsync(string message) => await DiscordUtilities.ReplySuccessAsync(Context.Channel, message);

        public async Task ReplyAsync(IPaginatedMessage message) => await PaginatedMessageService.SendMessageAsync(Context, message);
        public async Task ReplyAndWaitAsync(IPaginatedMessage message) => await PaginatedMessageService.SendMessageAndWaitAsync(Context, message);
        public async Task ReplyAsync(Discord.Messaging.IEmbed message) => await ReplyAsync("", false, message.ToDiscordEmbed());

        public async Task<ISpecies> GetSpeciesOrReplyAsync(string speciesName) {

            return await GetSpeciesOrReplyAsync(string.Empty, speciesName);

        }
        public async Task<ISpecies> GetSpeciesOrReplyAsync(string genusName, string speciesName) {

            ISpecies species = null;
            IEnumerable<ISpecies> matchingSpecies = await Db.GetSpeciesAsync(genusName, speciesName);

            if (matchingSpecies.Count() <= 0) {

                // The species could not be found.

                species = await ReplySpeciesSuggestionAsync(genusName, speciesName);

            }
            else if (matchingSpecies.Count() > 1) {

                // Multiple species were found.

                await ReplyMatchingSpeciesAsync(matchingSpecies);

            }
            else {

                species = matchingSpecies.First();

            }

            return species;

        }
        public async Task<ISpecies> ReplyValidateSpeciesAsync(IEnumerable<ISpecies> matchingSpecies) {

            ISpecies species = null;

            if (matchingSpecies.Count() <= 0) {

                await ReplyNoSuchSpeciesExistsAsync(null);

            }
            else if (matchingSpecies.Count() > 1) {

                await ReplyMatchingSpeciesAsync(matchingSpecies);

            }
            else {

                species = matchingSpecies.First();

            }

            return species;

        }
        public async Task<ITaxon> ReplyValidateTaxaAsync(IEnumerable<ITaxon> matchingTaxa) {

            ITaxon result = null;

            if (matchingTaxa is null || matchingTaxa.Count() <= 0) {

                // No taxa exist in the list.

                await ReplyErrorAsync("No such taxon exists.");

            }

            if (matchingTaxa.Count() > 1) {

                // Multiple taxa are in the list.

                SortedDictionary<TaxonRankType, List<ITaxon>> taxaDict = new SortedDictionary<TaxonRankType, List<ITaxon>>();

                foreach (ITaxon taxon in matchingTaxa) {

                    if (!taxaDict.ContainsKey(taxon.Rank.Type))
                        taxaDict[taxon.Rank.Type] = new List<ITaxon>();

                    taxaDict[taxon.Rank.Type].Add(taxon);

                }

                Discord.Messaging.Embed embed = new Discord.Messaging.Embed();

                if (taxaDict.Keys.Count() > 1)
                    embed.Title = string.Format("Matching taxa ({0})", matchingTaxa.Count());

                foreach (TaxonRankType type in taxaDict.Keys) {

                    taxaDict[type].Sort((lhs, rhs) => lhs.Name.CompareTo(rhs.Name));

                    StringBuilder fieldContent = new StringBuilder();

                    foreach (ITaxon taxon in taxaDict[type])
                        fieldContent.AppendLine(type == TaxonRankType.Species ? (await Db.GetSpeciesAsync(taxon.Id))?.ShortName : taxon.GetName());

                    embed.AddField(string.Format("{0}{1} ({2})",
                        taxaDict.Keys.Count() == 1 ? "Matching " : "",
                        taxaDict.Keys.Count() == 1 ? type.GetName(true).ToLowerInvariant() : type.GetName(true).ToTitle(),
                        taxaDict[type].Count()),
                        fieldContent.ToString());

                }

                await ReplyAsync(embed);

            }
            else if (matchingTaxa.Count() == 1) {

                // We have a single taxon.

                result = matchingTaxa.First();

            }

            return result;

        }

        public async Task<ISpecies> ReplySpeciesSuggestionAsync(string genusName, string speciesName) {

            int minimumDistance = int.MaxValue;
            ISpecies suggestion = null;

            foreach (ISpecies species in await Db.GetSpeciesAsync()) {

                int dist = StringUtilities.GetLevenshteinDistance(speciesName, species.Name);

                if (dist < minimumDistance) {

                    minimumDistance = dist;

                    suggestion = species;

                }

            }

            return await ReplyNoSuchSpeciesExistsAsync(suggestion);

        }
        public async Task<ISpecies> ReplyNoSuchSpeciesExistsAsync(ISpecies suggestion) {

            StringBuilder sb = new StringBuilder();

            sb.Append("No such species exists.");

            if (suggestion != null)
                sb.Append(string.Format(" Did you mean **{0}**?", suggestion));

            IPaginatedMessage message = new Discord.Messaging.PaginatedMessage(sb.ToString()) {
                Restricted = true
            };

            if (suggestion != null) {

                bool confirmed = false;

                message.AddReaction(PaginatedMessageReactionType.Yes, async (args) => {

                    confirmed = true;

                    await Task.CompletedTask;

                });

                await ReplyAndWaitAsync(message);

                if (!confirmed)
                    suggestion = null;

            }
            else
                await ReplyAsync(message);

            return suggestion;

        }
        public async Task ReplyMatchingSpeciesAsync(IEnumerable<ISpecies> speciesList) {

            Discord.Messaging.Embed embed = new Discord.Messaging.Embed();

            List<string> lines = new List<string>();

            embed.Title = string.Format("Matching species ({0})", speciesList.Count());

            foreach (ISpecies sp in speciesList)
                lines.Add(sp.FullName);

            embed.Description = (string.Join(Environment.NewLine, lines));

            await ReplyAsync(embed);

        }

        public async Task ReplySpeciesAsync(ISpecies species) {

            IPaginatedMessage message = await BuildSpeciesMessageAsync(species);

            await ReplyAsync(message);

        }
        public async Task ReplyTaxonAsync(ITaxon taxon) {

            IPaginatedMessage message = await BuildTaxonMessageAsync(taxon);

            await ReplyAsync(message);

        }

        // Private members

        public async Task<IPaginatedMessage> BuildSpeciesMessageAsync(ISpecies species) {

            if (!species.IsValid())
                return null;

            Discord.Messaging.Embed embed = new Discord.Messaging.Embed {
                Title = species.FullName
            };

            if (species.CommonNames.Count() > 0)
                embed.Title += string.Format(" ({0})", string.Join(", ", species.CommonNames));

            if (Config.GenerationsEnabled) {

                // Add a field for the generation.

                IGeneration gen = await Db.GetGenerationByDateAsync(species.CreationDate);

                embed.AddField("Gen", gen is null ? "???" : gen.Number.ToString(), inline: true);

            }

            // Add a field for the species owner.

            embed.AddField("Owner", await GetCreatorAsync(species.Creator), inline: true);

            // Add a field for the species' zones.

            IEnumerable<ISpeciesZoneInfo> speciesZoneList = await Db.GetZonesAsync(species);

            if (speciesZoneList.Count() > 0)
                embed.Color = (await Db.GetZoneTypeAsync(speciesZoneList.GroupBy(x => x.Zone.TypeId).OrderBy(x => x.Count()).Last().Key)).Color;

            string zonesFieldValue = speciesZoneList.ToString(ZoneListToStringOptions.None, DiscordUtilities.MaxFieldLength);

            embed.AddField("Zone(s)", string.IsNullOrEmpty(zonesFieldValue) ? "None" : zonesFieldValue, inline: true);

            // Add the species' description.

            StringBuilder descriptionBuilder = new StringBuilder();

            if (species.Status.IsExinct) {

                embed.Title = "[EXTINCT] " + embed.Title;
                embed.Color = System.Drawing.Color.Red;

                if (!string.IsNullOrEmpty(species.Status.ExtinctionReason))
                    descriptionBuilder.AppendLine(string.Format("**Extinct ({0}):** _{1}_\n", await BotUtils.TimestampToDateStringAsync(DateUtilities.GetTimestampFromDate((DateTimeOffset)species.Status.ExtinctionDate), BotContext), species.Status.ExtinctionReason));

            }

            descriptionBuilder.Append(species.GetDescriptionOrDefault());

            embed.Description = descriptionBuilder.ToString();

            // Add the species' picture.

            embed.ThumbnailUrl = species.GetPictureUrl();

            if (!string.IsNullOrEmpty(Config.WikiUrlFormat)) {

                // Discord automatically encodes certain characters in URIs, which doesn't allow us to update the config via Discord when we have "{0}" in the URL.
                // Replace this with the proper string before attempting to call string.Format.

                // string format = botContext.Configuration.WikiUrlFormat.Replace("%7B0%7D", "{0}");

                // embed.Url = string.Format(format, Uri.EscapeUriString(GetWikiPageTitleForSpecies(species, common_names)));

            }

            // Create embed pages.

            IEnumerable<Discord.Messaging.IEmbed> embedPages = EmbedUtilities.CreateEmbedPages(embed, EmbedPaginationOptions.AddPageNumbers);
            IPaginatedMessage paginatedMessage = new Discord.Messaging.PaginatedMessage(embedPages);

            return paginatedMessage;

        }
        public async Task<IPaginatedMessage> BuildTaxonMessageAsync(ITaxon taxon) {

            if (!taxon.IsValid())
                return null;

            List<string> subItems = new List<string>();

            if (taxon.Rank.Type == TaxonRankType.Species) {

                ISpecies species = await Db.GetSpeciesAsync(taxon.Id);

                return await BuildSpeciesMessageAsync(species);

            }
            else if (taxon.Rank.Type == TaxonRankType.Genus) {

                // For genera, get all species underneath it.
                // This will let us check if the species is extinct, and cross it out if that's the case.

                List<ISpecies> speciesList = new List<ISpecies>();

                foreach (ITaxon subtaxon in await Db.GetSubtaxaAsync(taxon))
                    speciesList.Add(await Db.GetSpeciesAsync(subtaxon.Id));

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

                IEnumerable<ITaxon> subtaxa = await Db.GetSubtaxaAsync(taxon);

                // Add all subtaxa to the list.

                foreach (ITaxon subtaxon in subtaxa) {

                    if (subtaxon.Rank.Type == TaxonRankType.Species) {

                        // Do not attempt to count sub-taxa for species.

                        subItems.Add(subtaxon.GetName());

                    }
                    else {

                        // Count the number of species under this taxon.
                        // Taxa with no species under them will not be displayed.

                        long speciesCount = await Db.GetSpeciesCountAsync(taxon);

                        if (speciesCount > 0) {

                            // Count the sub-taxa under this taxon.

                            long subtaxaCount = (await Db.GetSubtaxaAsync(taxon)).Count();

                            // Add the taxon to the list.

                            if (subtaxaCount > 0)
                                subItems.Add(string.Format("{0} ({1})", subtaxon.GetName(), subtaxaCount));

                        }

                    }

                }

            }

            // Generate embed pages.

            string title = taxon.CommonNames.Count() <= 0 ? taxon.GetName() : string.Format("{0} ({1})", taxon.GetName(), taxon.GetCommonName());
            string fieldTitle = string.Format("{0} in this {1} ({2}):", taxon.GetChildRank().GetName(true).ToTitle(), taxon.GetRank().GetName().ToLowerInvariant(), subItems.Count());
            string thumbnailUrl = taxon.GetPictureUrl();

            StringBuilder description = new StringBuilder();
            description.AppendLine(taxon.GetDescriptionOrDefault());

            if (subItems.Count() <= 0) {

                description.AppendLine();
                description.AppendLine(string.Format("This {0} contains no {1}.", taxon.GetRank().GetName(), taxon.GetChildRank().GetName(true)));

            }

            IEnumerable<Discord.Messaging.IEmbed> pages = EmbedUtilities.CreateEmbedPages(fieldTitle, subItems, options: EmbedPaginationOptions.AddPageNumbers);
            IPaginatedMessage paginatedMessage = new Discord.Messaging.PaginatedMessage(pages);

            foreach (Discord.Messaging.IEmbed page in paginatedMessage.Select(m => m.Embed)) {

                page.Title = title;
                page.ThumbnailUrl = thumbnailUrl;
                page.Description = description.ToString();

                if (subItems.Count() > 0 && taxon.GetRank() != TaxonRankType.Genus)
                    page.Footer += string.Format(" — Empty {0} are not listed.", taxon.GetChildRank().GetName(true));

            }

            return paginatedMessage;

        }

        public async Task<ICreator> GetCreatorAsync(ICreator creator) {

            IUser user = await DiscordUtilities.GetDiscordUserFromCreatorAsync(Context, creator);

            return user?.ToCreator() ?? creator;

        }

        // Private members

    }

}