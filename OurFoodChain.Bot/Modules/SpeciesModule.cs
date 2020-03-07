﻿using Discord;
using Discord.Commands;
using OurFoodChain.Adapters;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Data.Queries;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Messaging;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Modules {

    public class SpeciesModule :
        OfcModuleBase {

        // Public members       

        [Command("info", RunMode = RunMode.Async), Alias("i")]
        public async Task GetInfo([Remainder]string taxonName) {

            // Prioritize species first.

            IEnumerable<ISpecies> matchingSpecies = await Db.GetSpeciesAsync(taxonName);

            if (matchingSpecies.Count() > 0) {

                ISpecies species = await ReplyValidateSpeciesAsync(matchingSpecies);

                if (species.IsValid())
                    await ReplySpeciesAsync(species);

            }
            else {

                // Otherwise, show other taxon.

                IEnumerable<ITaxon> taxa = await Db.GetTaxaAsync(taxonName);

                if (taxa.Count() <= 0) {

                    // This command was traditionally used with species, so show the user species suggestions in the event of no matches.

                    ISpecies species = await ReplySpeciesSuggestionAsync(string.Empty, taxonName);

                    if (species.IsValid())
                        await ReplySpeciesAsync(species);

                }
                else {

                    ITaxon taxon = await ReplyValidateTaxaAsync(taxa);

                    if (taxon.IsValid())
                        await ReplyTaxonAsync(taxon);

                }

            }

        }

        [Command("species"), Alias("sp", "s")]
        public async Task SpeciesInfo() {

            await ListSpecies();

        }
        [Command("species", RunMode = RunMode.Async), Alias("sp", "s")]
        public async Task SpeciesInfo([Remainder]string speciesName) {

            ISpecies species = await GetSpeciesOrReplyAsync(speciesName);

            if (species.IsValid())
                await ReplySpeciesAsync(species);

        }

        [Command("listspecies"), Alias("specieslist", "listsp", "splist")]
        public async Task ListSpecies() {

            // Get all species.

            List<ISpecies> species = new List<ISpecies>((await Db.GetSpeciesAsync()).OrderBy(s => s.GetShortName()));

            if (species.Count <= 0) {

                await ReplyInfoAsync("No species have been added yet.");

            }
            else {

                // Create embed pages.

                IEnumerable<Discord.Messaging.IEmbed> pages = EmbedUtilities.CreateEmbedPages($"All species ({species.Count()}):", species, options: EmbedPaginationOptions.AddPageNumbers);
                IPaginatedMessage message = new Discord.Messaging.PaginatedMessage(pages);

                await ReplyAsync(message);

            }

        }
        [Command("listspecies"), Alias("specieslist", "listsp", "splist")]
        public async Task ListSpecies(string taxonName) {

            // Get the taxon.

            ITaxon taxon = await ReplyValidateTaxaAsync((await Db.GetTaxaAsync(taxonName)).Where(t => t.GetRank() != TaxonRankType.Species));

            if (taxon.IsValid()) {

                // Get all species under that taxon.

                List<ISpecies> species = new List<ISpecies>((await Db.GetSpeciesAsync(taxon)).OrderBy(s => s.GetShortName()));

                // Create embed pages.

                IEnumerable<Discord.Messaging.IEmbed> pages = EmbedUtilities.CreateEmbedPages($"Species in this {taxon.GetRank().GetName()} ({species.Count()}):", species, options: EmbedPaginationOptions.AddPageNumbers);

                StringBuilder descriptionBuilder = new StringBuilder();

                descriptionBuilder.AppendLine(taxon.GetDescriptionOrDefault());

                if (species.Count() <= 0) {

                    descriptionBuilder.AppendLine();
                    descriptionBuilder.AppendLine($"This {taxon.GetRank().GetName()} contains no species.");

                }

                foreach (Discord.Messaging.IEmbed page in pages) {

                    page.Title = taxon.CommonNames.Count() <= 0 ? taxon.GetName() : $"{taxon.GetName()} ({taxon.GetCommonName()})";
                    page.Description = descriptionBuilder.ToString();
                    page.ThumbnailUrl = taxon.GetPictureUrl();

                }

                // Send the result.

                await ReplyAsync(new Discord.Messaging.PaginatedMessage(pages));

            }

        }

        [Command("setspecies", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetSpecies(string speciesName, string newSpeciesName) {

            await SetSpecies(string.Empty, speciesName, newSpeciesName);

        }
        [Command("setspecies", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetSpecies(string genusName, string speciesName, string newSpeciesName) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            newSpeciesName = newSpeciesName.SafeTrim();

            if (species.IsValid() && !string.IsNullOrWhiteSpace(newSpeciesName)) {

                string oldSpeciesName = species.GetShortName();

                // Update the species.

                species.Name = newSpeciesName;

                await Db.UpdateSpeciesAsync(species);

                await ReplySuccessAsync($"**{oldSpeciesName}** has been successfully renamed to **{BinomialName.Parse(genusName, newSpeciesName)}**.");

            }

        }

        [Command("addspecies"), Alias("addsp"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AddSpecies(string genusName, string speciesName, string zoneNames = "", string description = "") {

            if ((await Db.GetSpeciesAsync(genusName, speciesName)).Count() > 0) {

                // If the species already exists, do nothing.

                await ReplyWarningAsync($"The species **{BinomialName.Parse(genusName, speciesName)}** already exists.");

            }
            else {

                // Get or add the species' genus.

                ITaxon genus = await Db.GetTaxonAsync(genusName, TaxonRankType.Genus);

                if (genus is null) {

                    await Db.AddTaxonAsync(new Common.Taxa.Taxon(TaxonRankType.Genus, genusName));

                    genus = await Db.GetTaxonAsync(genusName, TaxonRankType.Genus);

                }

                // Add the species.

                ISpecies species = new Common.Taxa.Species() {
                    Name = speciesName,
                    Description = description,
                    Genus = genus,
                    Creator = Context.User.ToCreator()
                };

                await Db.AddSpeciesAsync(species);

                // Make sure the species was added successfully.

                species = (await Db.GetSpeciesAsync(genusName, speciesName)).FirstOrDefault();

                if (species.IsValid()) {

                    // Add the species to the given zones.

                    await AddSpeciesToZonesAsync(species, zoneNames, string.Empty, onlyShowErrors: true);

                    // Add the user to the trophy scanner queue in case their species earned them any new trophies.

                    if (Config.TrophiesEnabled)
                        await TrophyScanner.EnqueueAsync(Context.User.ToCreator(), Context);

                    await ReplySuccessAsync($"Successfully created new species, **{species.GetFullName()}**.");

                }
                else {

                    await ReplyErrorAsync("Failed to add species (invalid Species ID).");

                }

            }

        }

        [Command("setzone", RunMode = RunMode.Async), Alias("setzones"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetZone(string arg0, string arg1, string arg2 = "") {

            // Possible cases:
            // 1. <genus> <species> <zone>
            // 2. <species> <zone>

            // If the zone argument is empty, assume the user omitted the genus (2).

            if (string.IsNullOrEmpty(arg2)) {

                arg2 = arg1;
                arg1 = arg0;
                arg0 = string.Empty;

            }

            // Get the specified species.

            ISpecies species = await GetSpeciesOrReplyAsync(arg0, arg1);

            if (species.IsValid()) {

                // Delete existing zone information for the species.

                await Db.RemoveZonesAsync(species, (await Db.GetZonesAsync(species)).Select(info => info.Zone));

                // Add new zone information for the species.

                await AddSpeciesToZonesAsync(species, arg2, string.Empty, onlyShowErrors: false);

            }

        }

        [Command("+zone", RunMode = RunMode.Async), Alias("+zones"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task PlusZone(string arg0, string arg1, string arg2) {

            // Possible cases:
            // 1. <species> <zone> <notes>
            // 2. <genus> <species> <zone>

            // If a species exists with the given genus/species, assume the user intended case (2).

            IEnumerable<ISpecies> matchingSpecies = await Db.GetSpeciesAsync(arg0, arg1);

            if (matchingSpecies.Count() == 1) {

                // If there is a unqiue species match, proceed with the assumption of case (2).

                await AddSpeciesToZonesAsync(matchingSpecies.First(), zoneList: arg2, notes: string.Empty, onlyShowErrors: false);

            }
            else if (matchingSpecies.Count() > 1) {

                // If there are species matches but no unique result, show the user.

                await ReplyMatchingSpeciesAsync(matchingSpecies);

            }
            else if (matchingSpecies.Count() <= 0) {

                // If there were no matches, assume the user intended case (1).

                ISpecies species = await GetSpeciesOrReplyAsync(string.Empty, arg0);

                if (species.IsValid())
                    await AddSpeciesToZonesAsync(species, zoneList: arg1, notes: arg2, onlyShowErrors: false);

            }

        }
        [Command("+zone", RunMode = RunMode.Async), Alias("+zones"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task PlusZone(string speciesName, string zoneNames) {

            await PlusZone(string.Empty, speciesName, zoneNames, string.Empty);

        }
        [Command("+zone", RunMode = RunMode.Async), Alias("+zones"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task PlusZone(string genusName, string speciesName, string zoneNames, string notes) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid())
                await AddSpeciesToZonesAsync(species, zoneList: zoneNames, notes: notes, onlyShowErrors: false);

        }

        [Command("-zone", RunMode = RunMode.Async), Alias("-zones"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task MinusZone(string speciesName, string zoneNames) {

            await MinusZone(string.Empty, speciesName, zoneNames);

        }
        [Command("-zone", RunMode = RunMode.Async), Alias("-zones"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task MinusZone(string genusName, string speciesName, string zoneNames) {

            // Get the specified species.

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                // Get the zones that the species currently resides in.
                // These will be used to show warning messages (e.g., doesn't exist in the given zone).

                IEnumerable<long> currentZoneIds = (await Db.GetZonesAsync(species.Id))
                    .Where(info => info.Zone.Id.HasValue)
                    .Select(info => info.Zone.Id.Value);

                // Get the zones from user input.

                IEnumerable<string> parsedZoneNames = ZoneUtilities.ParseZoneNameList(zoneNames);
                IEnumerable<IZone> zones = await Db.GetZonesAsync(parsedZoneNames);
                IEnumerable<string> invalidZones = parsedZoneNames.Except(zones.Select(zone => zone.Name), StringComparer.OrdinalIgnoreCase);

                // Remove the zones from the species.

                await Db.RemoveZonesAsync(species, zones);

                if (invalidZones.Count() > 0) {

                    // Show a warning if the user provided any invalid zones.

                    await ReplyWarningAsync(string.Format("{0} {1} not exist.",
                        StringUtilities.ConjunctiveJoin(", ", invalidZones.Select(x => string.Format("**{0}**", ZoneUtilities.GetFullName(x))).ToArray()),
                        invalidZones.Count() == 1 ? "does" : "do"));

                }

                if (zones.Any(zone => !currentZoneIds.Any(id => id == zone.Id))) {

                    // Show a warning if the species wasn't in one or more of the zones provided.

                    await ReplyWarningAsync(string.Format("**{0}** is already absent from {1}.",
                        species.GetShortName(),
                        StringUtilities.ConjunctiveJoin(", ", zones.Where(zone => !currentZoneIds.Any(id => id == zone.Id)).Select(zone => string.Format("**{0}**", zone.GetFullName())).ToArray())));

                }

                if (zones.Any(zone => currentZoneIds.Any(id => id == zone.Id))) {

                    // Show a confirmation of all valid zones.

                    await ReplySuccessAsync(string.Format("**{0}** no longer inhabits {1}.",
                        species.GetShortName(),
                        StringUtilities.DisjunctiveJoin(", ", zones.Where(zone => currentZoneIds.Any(id => id == zone.Id)).Select(zone => string.Format("**{0}**", zone.GetFullName())).ToArray())));

                }

            }

        }

        [Command("setowner", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetOwner(string speciesName, IUser user) {

            await SetOwner(string.Empty, speciesName, user);

        }
        [Command("setowner", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetOwner(string genusName, string speciesName, IUser user) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                species.Creator = user.ToCreator();

                await Db.UpdateSpeciesAsync(species);

                // Add the new owner to the trophy scanner queue in case their species earned them any new trophies.

                if (Config.TrophiesEnabled)
                    await TrophyScanner.EnqueueAsync(user.ToCreator(), Context);

                await ReplySuccessAsync($"**{species.GetShortName()}** is now owned by **{species.Creator}**.");

            }

        }
        [Command("setowner", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetOwner(string speciesName, string ownerName) {

            await SetOwner(string.Empty, speciesName, ownerName);

        }
        [Command("setowner", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetOwner(string genusName, string speciesName, string ownerName) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                // If we've seen this user before, get their user ID from the database.

                ICreator creator = await Db.GetCreatorAsync(ownerName);

                if (creator.IsValid()) {

                    species.Creator = creator;

                }
                else {

                    species.Creator = new Creator(ownerName);

                    await ReplyWarningAsync($"**{species.Creator}** is not an existing user. Was this intentional?");

                }

                await Db.UpdateSpeciesAsync(species);

                await ReplySuccessAsync($"**{species.GetShortName()}** is now owned by **{species.Creator}**.");

            }

        }

        [Command("addedby"), Alias("ownedby", "own", "owned")]
        public async Task AddedBy() {

            await AddedBy(Context.User);

        }
        [Command("addedby"), Alias("ownedby", "own", "owned")]
        public async Task AddedBy(IUser user) {

            ICreator creator = (user ?? Context.User).ToCreator();

            // Get all species belonging to this user.

            IEnumerable<ISpecies> species = (await Db.GetSpeciesAsync(creator)).OrderBy(s => s.GetShortName());

            // Display the species belonging to this user.

            await ReplySpeciesAddedByAsync(creator, user.GetAvatarUrl(size: 32), species);

        }
        [Command("addedby"), Alias("ownedby", "own", "owned")]
        public async Task AddedBy(string owner) {

            // If we get this overload, then the requested user does not currently exist in the guild.

            // If we've seen the user before, we can get their information from the database.

            ICreator creator = await Db.GetCreatorAsync(owner);

            if (creator.IsValid()) {

                // The user exists in the database, so create a list of all species they own.

                IEnumerable<ISpecies> species = (await Db.GetSpeciesAsync(creator)).OrderBy(s => s.GetShortName());

                // Display the species list.

                await ReplySpeciesAddedByAsync(creator, string.Empty, species);

            }
            else {

                // The user does not exist in the database.

                await ReplyErrorAsync("No such user exists.");

            }

        }

        [Command("random"), Alias("rand")]
        public async Task Random() {

            // Get a random species from the database.

            ISpecies species = await Db.GetRandomSpeciesAsync();

            if (species.IsValid())
                await ReplySpeciesAsync(species);
            else
                await ReplyInfoAsync("There are currently no extant species.");

        }
        [Command("random"), Alias("rand")]
        public async Task Random(string taxonName) {

            // Get the taxon.

            ITaxon taxon = await ReplyValidateTaxaAsync((await Db.GetTaxaAsync(taxonName)).Where(t => t.GetRank() != TaxonRankType.Species));

            if (taxon.IsValid()) {

                // Get all species under that taxon.

                IEnumerable<ISpecies> species = (await Db.GetSpeciesAsync(taxon)).Where(s => !s.IsExtinct());

                if (species.Count() <= 0)
                    await ReplyInfoAsync($"{taxon.GetRank().GetName().ToTitle()} **{taxon.GetName()}** does not contain any extant species.");
                else
                    await ReplySpeciesAsync(species.ElementAt(NumberUtilities.GetRandomInteger(species.Count())));

            }

        }

        [Command("search")]
        public async Task Search([Remainder]string queryString) {

            // Create and execute the search query.

            ISearchQuery query = new SearchQuery(queryString);
            ISearchResult result = await Db.GetSearchResultsAsync(SearchContext, query);

            if (result.Count() <= 0) {

                // There are no results to display.

                await ReplyInfoAsync("No species matching this query could be found.");

            }
            else {

                if (result.DisplayFormat == SearchResultDisplayFormat.Gallery) {

                    // Display the result as a picture gallery.

                    List<IPicture> pictures = new List<IPicture>();

                    foreach (ISpecies species in await result.GetResultsAsync())
                        pictures.AddRange(species.Pictures);

                    await ReplyGalleryAsync($"search results ({result.Count()})", pictures);

                }
                else if (result.DisplayFormat == SearchResultDisplayFormat.Leaderboard) {

                    // Display the result as a leaderboard.

                    ILeaderboard leaderboard = new Leaderboard("Search results");

                    foreach (ISearchResultGroup group in result.Groups)
                        leaderboard.Add(group.Name, group.Count());

                    await ReplyLeaderboardAsync(leaderboard);

                }
                else {

                    if (result.Count() == 1) {

                        // If there's only one result, just show that species.

                        ISpecies species = (await result.GetResultsAsync()).First();

                        await ReplySpeciesAsync(species);

                    }
                    else {

                        IPaginatedMessage message;

                        if (result.ContainsGroup(Data.Queries.SearchResult.DefaultGroupName)) {

                            // If there's only one group, just list the species without creating separate fields.

                            message = new Discord.Messaging.PaginatedMessage(EmbedUtilities.CreateEmbedPages(
                                $"Search results ({result.Count()})",
                                result.DefaultGroup.GetStringResults(),
                                options: EmbedPaginationOptions.AddPageNumbers));

                        }
                        else {

                            message = new Discord.Messaging.PaginatedMessage(EmbedUtilities.CreateEmbedPages(result));

                        }

                        await ReplyAsync(message);

                    }

                }

            }


        }

        public static async Task ShowSpeciesInfoAsync(ICommandContext context, IOfcBotConfiguration botConfiguration, SQLiteDatabase db, string speciesName) {
            await ShowSpeciesInfoAsync(context, botConfiguration, db, string.Empty, speciesName);
        }
        public static async Task ShowSpeciesInfoAsync(ICommandContext context, IOfcBotConfiguration botConfiguration, SQLiteDatabase db, string genusName, string speciesName) {

            Species sp = await BotUtils.ReplyAsync_FindSpecies(context, genusName, speciesName,
            async (BotUtils.ConfirmSuggestionArgs args) => await ShowSpeciesInfoAsync(context, botConfiguration, db, args.Suggestion));

            if (sp is null)
                return;

            await ShowSpeciesInfoAsync(context, botConfiguration, db, sp);

        }
        public static async Task ShowSpeciesInfoAsync(ICommandContext context, IOfcBotConfiguration botConfiguration, SQLiteDatabase db, Species species) {

            if (await BotUtils.ReplyValidateSpeciesAsync(context, species)) {

                EmbedBuilder embed = new EmbedBuilder();
                StringBuilder descriptionBuilder = new StringBuilder();

                string embed_title = species.FullName;
                Color embed_color = Color.Blue;

                CommonName[] common_names = await SpeciesUtils.GetCommonNamesAsync(species);

                if (common_names.Count() > 0)
                    embed_title += string.Format(" ({0})", string.Join(", ", (object[])common_names));

                // Show generation only if generations are enabled.

                if (botConfiguration.GenerationsEnabled) {

                    Generation gen = await GenerationUtils.GetGenerationByTimestampAsync(species.Timestamp);

                    embed.AddField("Gen", gen is null ? "???" : gen.Number.ToString(), inline: true);

                }

                embed.AddField("Owner", await SpeciesUtils.GetOwnerOrDefaultAsync(species, context), inline: true);

                IEnumerable<ISpeciesZoneInfo> zone_list = await db.GetZonesAsync(new SpeciesAdapter(species));

                if (zone_list.Count() > 0) {

                    embed_color = DiscordUtils.ConvertColor((await db.GetZoneTypeAsync(zone_list
                        .GroupBy(x => x.Zone.TypeId)
                        .OrderBy(x => x.Count())
                        .Last()
                        .Key)).Color);

                }

                string zones_value = new SpeciesZoneInfoCollection(zone_list).ToString(SpeciesZoneInfoCollectionToStringOptions.Default, DiscordUtils.MaxFieldLength);

                embed.AddField("Zone(s)", string.IsNullOrEmpty(zones_value) ? "None" : zones_value, inline: true);

                // Check if the species is extinct.
                using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Extinctions WHERE species_id=$species_id;")) {

                    cmd.Parameters.AddWithValue("$species_id", species.Id);

                    DataRow row = await Database.GetRowAsync(cmd);

                    if (!(row is null)) {

                        embed_title = "[EXTINCT] " + embed_title;
                        embed_color = Color.Red;

                        string reason = row.Field<string>("reason");
                        long timestamp = (long)row.Field<decimal>("timestamp");

                        if (!string.IsNullOrEmpty(reason))
                            descriptionBuilder.AppendLine(string.Format("**Extinct ({0}):** _{1}_\n", await BotUtils.TimestampToDateStringAsync(timestamp, new OfcBotContext(context, botConfiguration, db)), reason));

                    }

                }

                descriptionBuilder.Append(species.GetDescriptionOrDefault());

                embed.WithTitle(embed_title);
                embed.WithThumbnailUrl(species.Picture);
                embed.WithColor(embed_color);

                if (!string.IsNullOrEmpty(botConfiguration.WikiUrlFormat)) {

                    // Discord automatically encodes certain characters in URIs, which doesn't allow us to update the config via Discord when we have "{0}" in the URL.
                    // Replace this with the proper string before attempting to call string.Format.
                    string format = botConfiguration.WikiUrlFormat.Replace("%7B0%7D", "{0}");

                    embed.WithUrl(string.Format(format, Uri.EscapeUriString(GetWikiPageTitleForSpecies(species, common_names))));

                }

                if (embed.Length + descriptionBuilder.Length > DiscordUtils.MaxEmbedLength) {

                    // If the description puts us over the character limit, we'll paginate.

                    int pageLength = DiscordUtils.MaxEmbedLength - embed.Length;

                    List<EmbedBuilder> pages = new List<EmbedBuilder>();

                    foreach (string pageText in new StringPaginator(descriptionBuilder.ToString()) { MaxPageLength = pageLength }) {

                        EmbedBuilder page = new EmbedBuilder();

                        page.WithTitle(embed.Title);
                        page.WithThumbnailUrl(embed.ThumbnailUrl);
                        page.WithFields(embed.Fields);
                        page.WithDescription(pageText);

                        pages.Add(page);

                    }

                    PaginatedMessageBuilder builder = new Bot.PaginatedMessageBuilder(pages);
                    builder.AddPageNumbers();
                    builder.SetColor(embed_color);

                    await DiscordUtils.SendMessageAsync(context, builder.Build());

                }
                else {

                    embed.WithDescription(descriptionBuilder.ToString());

                    await context.Channel.SendMessageAsync("", false, embed.Build());

                }

            }

        }

        // Private members

        private async Task AddSpeciesToZonesAsync(ISpecies species, string zoneList, string notes, bool onlyShowErrors = false) {

            // Get the zones from user input.

            IEnumerable<string> zoneNames = ZoneUtilities.ParseZoneNameList(zoneList);
            List<string> invalidZoneNames = new List<string>();

            List<IZone> zones = new List<IZone>();

            foreach (string zoneName in zoneNames) {

                IZone zone = await Db.GetZoneAsync(zoneName);

                if (zone.IsValid())
                    zones.Add(zone);
                else
                    invalidZoneNames.Add(zoneName);

            }

            // Add the zones to the species.

            await Db.AddZonesAsync(species, zones, notes);

            if (invalidZoneNames.Count() > 0) {

                // Show a warning if the user provided any invalid zones.

                await ReplyWarningAsync(
                    string.Format("{0} {1} not exist.",
                    StringUtilities.ConjunctiveJoin(", ", invalidZoneNames.Select(x => string.Format("**{0}**", ZoneUtilities.GetFullName(x)))),
                    invalidZoneNames.Count() == 1 ? "does" : "do"));

            }

            if (zones.Count() > 0 && !onlyShowErrors) {

                // Show a confirmation of all valid zones.

                await ReplySuccessAsync(
                    string.Format("**{0}** now inhabits {1}.",
                    species.GetShortName(),
                    StringUtilities.ConjunctiveJoin(", ", zones.Select(x => string.Format("**{0}**", x.GetFullName())).ToArray())));

            }

        }
        private async Task ReplySpeciesAddedByAsync(ICreator creator, string thumbnailUrl, IEnumerable<ISpecies> species) {

            if (species.Count() <= 0) {

                await ReplyInfoAsync($"**{creator}** has not submitted any species yet.");

            }
            else {

                IEnumerable<Discord.Messaging.IEmbed> pages = EmbedUtilities.CreateEmbedPages($"Species owned by {creator} ({species.Count()})", species, options: EmbedPaginationOptions.AddPageNumbers);

                foreach (Discord.Messaging.IEmbed page in pages)
                    page.ThumbnailUrl = thumbnailUrl;

                await ReplyAsync(new Discord.Messaging.PaginatedMessage(pages));

            }

        }
        private static string GetWikiPageTitleForSpecies(Species species, CommonName[] commonNames) {

            // This is the same process as used in SpeciesPageBuilder.BuildTitleAsync.
            // #todo Instead of being copy-pasted, this process should be in its own function used by both classes.

            string title = string.Empty;

            if (!string.IsNullOrWhiteSpace(species.CommonName))
                title = species.CommonName;
            else {

                if (commonNames.Count() > 0)
                    title = commonNames.First().Value;
                else
                    title = species.FullName;

            }

            if (string.IsNullOrWhiteSpace(title))
                title = species.FullName;

            if (!string.IsNullOrEmpty(title))
                title = title.Trim();

            return title;

        }

    }

}