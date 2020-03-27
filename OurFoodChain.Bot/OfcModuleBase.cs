using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OurFoodChain.Bot;
using OurFoodChain.Common;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Generations;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Bots;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Messaging;
using OurFoodChain.Discord.Services;
using OurFoodChain.Discord.Utilities;
using OurFoodChain.Services;
using OurFoodChain.Trophies;
using OurFoodChain.Wiki.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Discord.Color;

namespace OurFoodChain {

    public abstract class OfcModuleBase :
        ModuleBase {

        // Public members

        public IDiscordBot Bot { get; set; }
        public IDatabaseService DatabaseService { get; set; }
        public IPaginatedMessageService PaginatedMessageService { get; set; }
        public IResponsiveMessageService ResponsiveMessageService { get; set; }
        public IOfcBotConfiguration Config { get; set; }
        public ITrophyService TrophyService { get; set; }
        public ITrophyScanner TrophyScanner { get; set; }
        public DiscordSocketClient DiscordClient { get; set; }
        public IHelpService HelpService { get; set; }
        public GotchiService GotchiService { get; set; }

        public SQLiteDatabase Db => GetDatabaseAsync().Result;
        public SearchContext SearchContext => new SearchContext(Context, Db);
        public OfcBotContext BotContext => new OfcBotContext(Context, Config, Db);

        public ITaxonFormatter TaxonFormatter {
            get {

                BinomialNameTaxonFormatter formatter;

                if (Config.PreferCommonNames)
                    formatter = new CommonNameTaxonFormatter();
                else
                    formatter = new BinomialNameTaxonFormatter();

                if (Config.PreferFullNames)
                    formatter.NameFormat = BinomialNameFormat.Full;

                formatter.ExtinctNameFormat = Config.ExtinctNameFormat;

                return formatter;

            }
        }

        public async Task<SQLiteDatabase> GetDatabaseAsync() {

            try {

                return await DatabaseService.GetDatabaseAsync(Context.Guild);

            }
            catch (Exception ex) {

                await ReplyErrorAsync(ex.Message);

                throw ex;

            }

        }

        // Status replies

        public async Task ReplyInfoAsync(string message) => await DiscordUtilities.ReplyInfoAsync(Context.Channel, message);
        public async Task ReplyWarningAsync(string message) => await DiscordUtilities.ReplyWarningAsync(Context.Channel, message);
        public async Task ReplyErrorAsync(string message) => await DiscordUtilities.ReplyErrorAsync(Context.Channel, message);
        public async Task ReplySuccessAsync(string message) => await DiscordUtilities.ReplySuccessAsync(Context.Channel, message);

        // Privilege replies

        public async Task<bool> ReplyValidatePrivilegeAsync(PrivilegeLevel level, ISpecies species = null) {

            if (species.IsValid() && Context.User.Id == species.Creator?.UserId)
                return true;

            if (Config.HasPrivilegeLevel(Context.User, level))
                return true;

            string privilegeName = "";

            switch (level) {

                case PrivilegeLevel.BotAdmin:
                    privilegeName = "Bot Admin";
                    break;

                case PrivilegeLevel.ServerAdmin:
                    privilegeName = "Admin";
                    break;

                case PrivilegeLevel.ServerModerator:
                    privilegeName = "Moderator";
                    break;

            }

            await ReplyErrorAsync($"You must have {privilegeName.ToTitle().ToBold()} privileges to use this command.");

            return false;

        }

        // Service replies

        public async Task ReplyAsync(IPaginatedMessage message) => await PaginatedMessageService.SendMessageAsync(Context, message);
        public async Task ReplyAndWaitAsync(IPaginatedMessage message) => await PaginatedMessageService.SendMessageAndWaitAsync(Context, message);
        public async Task ReplyAsync(Discord.Messaging.IEmbed message) => await ReplyAsync("", false, message.ToDiscordEmbed());

        // Species replies

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

        public async Task ReplySpeciesAsync(ISpecies species) {

            IPaginatedMessage message = await BuildSpeciesMessageAsync(species);

            await ReplyAsync(message);

        }
        public async Task ReplyMatchingSpeciesAsync(IEnumerable<ISpecies> matchingSpecies) {

            IPaginatedMessage message = new PaginatedMessage();

            List<string> lines = new List<string>();

            foreach (ISpecies species in matchingSpecies.OrderBy(species => species.GetFullName()))
                lines.Add(species.GetFullName());

            message.AddLines($"Matching species ({matchingSpecies.Count()})", lines, options: EmbedPaginationOptions.AddPageNumbers);

            await ReplyAsync(message);

        }
        public async Task<ISpecies> ReplyValidateSpeciesAsync(IEnumerable<ISpecies> matchingSpecies) {

            ISpecies species = null;

            if (matchingSpecies.Count() <= 0) {

                await ReplyNoSuchTaxonExistsAsync(string.Empty, null, rank: TaxonRankType.Species);

            }
            else if (matchingSpecies.Count() > 1) {

                await ReplyMatchingSpeciesAsync(matchingSpecies);

            }
            else {

                species = matchingSpecies.First();

            }

            return species;

        }
        public async Task<ISpecies> ReplySpeciesSuggestionAsync(string genusName, string speciesName) {

            int minimumDistance = int.MaxValue;
            ISpecies suggestion = null;

            foreach (ISpecies species in await Db.GetSpeciesAsync()) {

                int dist = StringUtilities.GetLevenshteinDistance(speciesName.ToLowerInvariant(), species.Name.ToLowerInvariant());

                if (dist < minimumDistance) {

                    minimumDistance = dist;

                    suggestion = species;

                }

            }

            return await ReplyNoSuchTaxonExistsAsync(BinomialName.Parse(genusName, speciesName).ToString(), suggestion) as ISpecies;

        }

        public async Task<ISpeciesAmbiguityResolverResult> ReplyResolveAmbiguityAsync(string arg0, string arg1, string arg2, string arg3 = "", AmbiguityResolverOptions options = AmbiguityResolverOptions.None) {

            ISpeciesAmbiguityResolver resolver = new SpeciesAmbiguityResolver(await GetDatabaseAsync());
            ISpeciesAmbiguityResolverResult result = string.IsNullOrWhiteSpace(arg3) ? await resolver.ResolveAsync(arg0, arg1, arg2, options) : await resolver.ResolveAsync(arg0, arg1, arg2, arg3, options);

            ISpecies species1 = null;
            ISpecies species2 = null;

            if (result.First.Count() > 1)
                await ReplyValidateSpeciesAsync(result.First); // show matching species
            else if (!result.First.Any())
                await ReplyErrorAsync("The first species could not be determined.");
            else
                species1 = result.First.First();

            if (species1.IsValid()) {

                if (result.Second.Count() > 1) {

                    await ReplyValidateSpeciesAsync(result.Second); // show matching species

                }
                else if (!result.Second.Any()) {

                    if (!string.IsNullOrWhiteSpace(result.SuggestionHint))
                        species2 = await GetSpeciesOrReplyAsync(result.SuggestionHint);

                    if (!species2.IsValid())
                        await ReplyErrorAsync("The second species could not be determined.");

                }
                else {

                    species2 = result.Second.First();

                }

            }

            if (species1.IsValid() && species2.IsValid())
                return new SpeciesAmbiguityResolverResult(new ISpecies[] { species1 }, new ISpecies[] { species2 }, result.SuggestionHint, result.Extra);

            return result; // not success

        }

        public async Task<IPaginatedMessage> BuildSpeciesMessageAsync(ISpecies species) {

            if (!species.IsValid())
                return null;

            Discord.Messaging.Embed embed = new Discord.Messaging.Embed {
                Title = species.GetFullName()
            };

            if (species.CommonNames.Count() > 0)
                embed.Title += string.Format(" ({0})", string.Join(", ", species.CommonNames.Select(name => name.ToTitle())));

            if (Config.GenerationsEnabled) {

                // Add a field for the generation.

                IGeneration gen = await Db.GetGenerationByDateAsync(species.CreationDate);

                embed.AddField("Gen", gen is null ? "???" : gen.Number.ToString(), inline: true);

            }

            // Add a field for the species owner.

            embed.AddField("Owner", await GetCreatorAsync(species.Creator), inline: true);

            // Add a field for the species' zones.

            IEnumerable<ISpeciesZoneInfo> speciesZoneList = (await Db.GetZonesAsync(species))
                .Where(info => !info.Zone.Flags.HasFlag(ZoneFlags.Retired));

            string zonesFieldValue = speciesZoneList.ToString(ZoneListToStringOptions.Default, DiscordUtilities.MaxFieldLength);

            embed.AddField("Zone(s)", string.IsNullOrEmpty(zonesFieldValue) ? "None" : zonesFieldValue, inline: true);

            // Add the species' description.

            StringBuilder descriptionBuilder = new StringBuilder();

            if (species.IsExtinct()) {

                embed.Title = "[EXTINCT] " + embed.Title;

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

                string format = Config.WikiUrlFormat.Replace("%7B0%7D", "{0}");

                embed.Url = string.Format(format, Uri.EscapeUriString(WikiUtilities.GetWikiPageTitle(species)));

            }

            // Create embed pages.

            IEnumerable<Discord.Messaging.IEmbed> embedPages = EmbedUtilities.CreateEmbedPages(embed, EmbedPaginationOptions.AddPageNumbers | EmbedPaginationOptions.CopyFields);
            IPaginatedMessage paginatedMessage = new PaginatedMessage(embedPages);

            if (speciesZoneList.Count() > 0)
                paginatedMessage.SetColor((await Db.GetZoneTypeAsync(speciesZoneList.GroupBy(x => x.Zone.TypeId).OrderBy(x => x.Count()).Last().Key)).Color);

            if (species.IsExtinct())
                paginatedMessage.SetColor(Color.Red);

            return paginatedMessage;

        }

        // Taxon replies

        public async Task<ITaxon> GetTaxonOrReplyAsync(string taxonName) {

            return await GetTaxonOrReplyAsync(TaxonRankType.Any, taxonName);

        }
        public async Task<ITaxon> GetTaxonOrReplyAsync(TaxonRankType rank, string taxonName) {

            IEnumerable<ITaxon> taxa = await Db.GetTaxaAsync(taxonName, rank);

            return await GetTaxonOrReplyAsync(taxa, rank, taxonName);

        }

        public async Task ReplyTaxonAsync(ITaxon taxon) {

            IPaginatedMessage message = await BuildTaxonMessageAsync(taxon);

            await ReplyAsync(message);

        }
        public async Task ReplyTaxonAsync(TaxonRankType rank) {

            // List all taxa of the given rank.

            IEnumerable<ITaxon> taxa = (await Db.GetTaxaAsync(rank)).OrderBy(t => t.GetName());
            List<string> lines = new List<string>();

            foreach (ITaxon taxon in taxa) {

                // Count the number of items under this taxon.

                int subtaxaCount = (await Db.GetSubtaxaAsync(taxon)).Count();

                if (subtaxaCount > 0)
                    lines.Add($"{taxon.GetName().ToTitle()} ({subtaxaCount})");

            }

            if (lines.Count() <= 0) {

                await ReplyInfoAsync($"No {rank.GetName(true)} have been added yet.");

            }
            else {

                string title = $"All {rank.GetName(true)} ({lines.Count()})";

                IEnumerable<Discord.Messaging.IEmbed> pages = EmbedUtilities.CreateEmbedPages(title.ToTitle(), lines, options: EmbedPaginationOptions.AddPageNumbers);

                foreach (Discord.Messaging.IEmbed page in pages)
                    page.Footer += $" — Empty {rank.GetName(true)} are not listed.";

                await ReplyAsync(new PaginatedMessage(pages));

            }

        }
        public async Task ReplyMatchingTaxaAsync(IEnumerable<ITaxon> matchingTaxa) {

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
                    fieldContent.AppendLine(type == TaxonRankType.Species ? (await Db.GetSpeciesAsync(taxon.Id))?.GetShortName() : taxon.GetName());

                embed.AddField(string.Format("{0}{1} ({2})",
                    taxaDict.Keys.Count() == 1 ? "Matching " : "",
                    taxaDict.Keys.Count() == 1 ? type.GetName(true).ToLowerInvariant() : type.GetName(true).ToTitle(),
                    taxaDict[type].Count()),
                    fieldContent.ToString());

            }

            await ReplyAsync(embed);

        }
        public async Task<ITaxon> ReplyValidateTaxaAsync(IEnumerable<ITaxon> matchingTaxa) {

            ITaxon result = null;

            if (matchingTaxa is null || matchingTaxa.Count() <= 0) {

                // No taxa exist in the list.

                await ReplyNoSuchTaxonExistsAsync(string.Empty, null);

            }

            if (matchingTaxa.Count() > 1) {

                // Multiple taxa are in the list.

                await ReplyMatchingTaxaAsync(matchingTaxa);

            }
            else if (matchingTaxa.Count() == 1) {

                // We have a single taxon.

                result = matchingTaxa.First();

            }

            return result;

        }
        public async Task<ITaxon> ReplyTaxonSuggestionAsync(TaxonRankType rank, string taxonName) {

            IEnumerable<ITaxon> taxa = await Db.GetTaxaAsync(rank);

            int minimumDistance = int.MaxValue;
            ITaxon suggestion = null;

            foreach (ITaxon taxon in taxa) {

                int dist = StringUtilities.GetLevenshteinDistance(taxonName.ToLowerInvariant(), taxon.Name.ToLowerInvariant());

                if (dist < minimumDistance) {

                    minimumDistance = dist;

                    suggestion = taxon;

                }

            }

            return await ReplyNoSuchTaxonExistsAsync(taxonName, suggestion);

        }

        public async Task<ITaxon> ReplyNoSuchTaxonExistsAsync(string input, ITaxon suggestion, TaxonRankType rank = TaxonRankType.None) {

            string taxonName = rank == TaxonRankType.None ? "taxon" : rank.GetName();

            if (suggestion != null)
                taxonName = suggestion.GetRank().GetName();

            StringBuilder sb = new StringBuilder();

            if (string.IsNullOrWhiteSpace(input))
                sb.Append($"No such {taxonName} exists.");
            else
                sb.Append($"No {taxonName} named \"{input}\" exists.");

            if (suggestion != null) {

                string suggestionText = (suggestion is ISpecies species) ? species.GetFullName() : suggestion.GetName().ToTitle();

                sb.Append($" Did you mean **{suggestionText}**?");

            }

            IPaginatedMessage message = new PaginatedMessage(sb.ToString()) {
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

                speciesList.Sort((lhs, rhs) => TaxonFormatter.GetString(lhs, false).CompareTo(TaxonFormatter.GetString(rhs, false)));

                foreach (ISpecies species in speciesList.Where(s => s.IsValid()))
                    subItems.Add(TaxonFormatter.GetString(species));

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

                        long speciesCount = await Db.GetSpeciesCountAsync(subtaxon);

                        if (speciesCount > 0) {

                            // Count the sub-taxa under this taxon.

                            long subtaxaCount = (await Db.GetSubtaxaAsync(subtaxon)).Count();

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

            List<Discord.Messaging.IEmbed> pages = new List<Discord.Messaging.IEmbed>(EmbedUtilities.CreateEmbedPages(fieldTitle, subItems, options: EmbedPaginationOptions.AddPageNumbers));

            if (!pages.Any())
                pages.Add(new Discord.Messaging.Embed());

            IPaginatedMessage paginatedMessage = new PaginatedMessage(pages);

            foreach (Discord.Messaging.IEmbed page in paginatedMessage.Select(m => m.Embed)) {

                page.Title = title;
                page.ThumbnailUrl = thumbnailUrl;
                page.Description = description.ToString();

                if (subItems.Count() > 0 && taxon.GetRank() != TaxonRankType.Genus)
                    page.Footer += string.Format(" — Empty {0} are not listed.", taxon.GetChildRank().GetName(true));

            }

            return paginatedMessage;

        }

        // Other replies

        public async Task ReplyGalleryAsync(string galleryName, IEnumerable<IPicture> pictures) {

            // If there were no images for this query, show a message and quit.

            if (pictures.Count() <= 0) {

                await ReplyInfoAsync($"**{galleryName.ToTitle()}** does not have any pictures.");

            }
            else {

                // Display a paginated image gallery.

                List<Discord.Messaging.IEmbed> pages = new List<Discord.Messaging.IEmbed>(pictures.Select((picture, i) => {

                    Discord.Messaging.IEmbed embed = new Discord.Messaging.Embed();

                    string title = string.Format("Pictures of {0} ({1} of {2})", galleryName.ToTitle(), i + 1, pictures.Count());
                    string footer = string.Format("\"{0}\" by {1} — {2}", picture.GetName(), picture.Artist, picture.Caption);

                    embed.Title = title;
                    embed.ImageUrl = picture.Url;
                    embed.Description = picture.Description;
                    embed.Footer = footer;

                    return embed;


                }));

                await ReplyAsync(new PaginatedMessage(pages));

            }

        }
        public async Task ReplyLeaderboardAsync(ILeaderboard leaderboard) {

            List<string> lines = new List<string>(leaderboard.Select(item => {

                int rankWidth = leaderboard.Count().ToString().Length;
                int scoreWidth = leaderboard.Max(i => i.Score).ToString().Length;

                return string.Format("**`{0}.`**{1}`{2}` {3}",
                    item.Rank.ToString("0".PadRight(rankWidth, '0')),
                    item.Icon,
                    item.Score.ToString("0".PadRight(scoreWidth, '0')),
                    string.Format(item.Rank <= 3 ? "**{0}**" : "{0}", string.IsNullOrEmpty(item.Name) ? "Results" : item.Name.ToTitle())
                );

            }));

            IEnumerable<Discord.Messaging.IEmbed> pages = EmbedUtilities.CreateEmbedPages(string.Empty, lines, itemsPerPage: 20, columnsPerPage: 1, options: EmbedPaginationOptions.AddPageNumbers);

            string title = leaderboard.Title;

            if (string.IsNullOrWhiteSpace(title))
                title = "Leaderboard";

            title = $"🏆 {title.ToTitle()} ({lines.Count()})";

            foreach (Discord.Messaging.IEmbed page in pages)
                page.Title = title;

            await ReplyAsync(new PaginatedMessage(pages));

        }
        public async Task<bool> ReplyValidateImageUrlAsync(string imageUrl) {

            if (!StringUtilities.IsImageUrl(imageUrl)) {

                await ReplyErrorAsync("The image URL is invalid.");

                return false;

            }

            return true;

        }

        public async Task<ICreator> GetCreatorAsync(ICreator creator) {

            IUser user = await DiscordUtilities.GetDiscordUserFromCreatorAsync(Context, creator);

            return user?.ToCreator() ?? creator;

        }
        public async Task<string> GetDateStringAsync(DateTimeOffset? date, DateStringFormat format = DateStringFormat.Long) {

            if (date.HasValue) {

                if (Config.GenerationsEnabled) {

                    IGeneration gen = await Db.GetGenerationByDateAsync(date.Value);

                    return gen is null ? "Gen ???" : gen.Name;

                }

                return DateUtilities.GetDateString(date.Value, format);

            }
            else {

                return "???";

            }

        }

        // Private members

        private async Task<ITaxon> GetTaxonOrReplyAsync(IEnumerable<ITaxon> matchingTaxa, TaxonRankType rank, string taxonName) {

            ITaxon taxon = null;

            if (matchingTaxa.Count() <= 0) {

                // The taxon could not be found.

                taxon = await ReplyTaxonSuggestionAsync(rank, taxonName);

            }
            else if (matchingTaxa.Count() > 1) {

                // Multiple taxa were found.

                await ReplyMatchingTaxaAsync(matchingTaxa);

            }
            else {

                // We have a single taxon.

                taxon = matchingTaxa.First();

            }

            return taxon;

        }

    }

}