using Discord;
using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common;
using OurFoodChain.Common.Collections;
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
using OurFoodChain.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Modules {

    public class SpeciesModule :
        OfcModuleBase {

        // Public members       

        [Command("info", RunMode = RunMode.Async), Alias("i")]
        public async Task GetInfo([Remainder]string taxonName) {

            taxonName = StringUtilities.StripOuterQuotes(taxonName);

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

            ISpecies species = await GetSpeciesOrReplyAsync(StringUtilities.StripOuterQuotes(speciesName));

            if (species.IsValid())
                await ReplySpeciesAsync(species);

        }

        [Command("listspecies"), Alias("specieslist", "listsp", "splist")]
        public async Task ListSpecies() {

            // Get all species.

            List<ISpecies> species = new List<ISpecies>((await Db.GetSpeciesAsync(GetSpeciesOptions.Default | GetSpeciesOptions.IgnoreCommonNames))
                .OrderBy(s => TaxonFormatter.GetString(s, false)));

            if (species.Count <= 0) {

                await ReplyInfoAsync("No species have been added yet.");

            }
            else {

                // Create embed pages.

                IEnumerable<Discord.Messaging.IEmbed> pages = EmbedUtilities.CreateEmbedPages($"All species ({species.Count()}):", species, formatter: TaxonFormatter, options: EmbedPaginationOptions.AddPageNumbers);
                IPaginatedMessage message = new PaginatedMessage(pages);

                await ReplyAsync(message);

            }

        }
        [Command("listspecies"), Alias("specieslist", "listsp", "splist")]
        public async Task ListSpecies(string taxonName) {

            // Get the taxon.

            ITaxon taxon = await ReplyValidateTaxaAsync((await Db.GetTaxaAsync(taxonName))
                .Where(t => t.GetRank() != TaxonRankType.Species));

            if (taxon.IsValid()) {

                // Get all species under that taxon.

                List<ISpecies> species = new List<ISpecies>((await Db.GetSpeciesAsync(taxon))
                    .OrderBy(s => TaxonFormatter.GetString(s, false)));

                // Create embed pages.

                IEnumerable<Discord.Messaging.IEmbed> pages = EmbedUtilities.CreateEmbedPages($"Species in this {taxon.GetRank().GetName()} ({species.Count()}):", species, formatter: TaxonFormatter, options: EmbedPaginationOptions.AddPageNumbers);

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

                    await ReplyAddSpeciesToZonesAsync(species, zoneNames, string.Empty, onlyShowErrors: true);

                    // Add the user to the trophy scanner queue in case their species earned them any new trophies.

                    await this.ScanTrophiesAsync(Context.User.ToCreator());

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

                await ReplyAddSpeciesToZonesAsync(species, arg2, string.Empty, onlyShowErrors: false);

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

                await ReplyAddSpeciesToZonesAsync(matchingSpecies.First(), zoneList: arg2, notes: string.Empty, onlyShowErrors: false);

            }
            else if (matchingSpecies.Count() > 1) {

                // If there are species matches but no unique result, show the user.

                await ReplyMatchingSpeciesAsync(matchingSpecies);

            }
            else if (matchingSpecies.Count() <= 0) {

                // If there were no matches, assume the user intended case (1).

                ISpecies species = await GetSpeciesOrReplyAsync(string.Empty, arg0);

                if (species.IsValid())
                    await ReplyAddSpeciesToZonesAsync(species, zoneList: arg1, notes: arg2, onlyShowErrors: false);

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
                await ReplyAddSpeciesToZonesAsync(species, zoneList: zoneNames, notes: notes, onlyShowErrors: false);

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
                IEnumerable<string> invalidZones = parsedZoneNames
                    .Select(name => ZoneUtilities.GetFullName(name).ToLowerInvariant())
                    .Except(zones.Select(zone => zone.GetFullName().ToLowerInvariant()), StringComparer.OrdinalIgnoreCase);

                // Remove the zones from the species.

                await Db.RemoveZonesAsync(species, zones);

                if (invalidZones.Count() > 0) {

                    // Show a warning if the user provided any invalid zones.

                    await ReplyWarningAsync(string.Format("{0} {1} not exist.",
                        StringUtilities.ConjunctiveJoin(invalidZones.Select(x => string.Format("**{0}**", ZoneUtilities.GetFullName(x))).ToArray()),
                        invalidZones.Count() == 1 ? "does" : "do"));

                }

                if (zones.Any(zone => !currentZoneIds.Any(id => id == zone.Id))) {

                    // Show a warning if the species wasn't in one or more of the zones provided.

                    await ReplyWarningAsync(string.Format("**{0}** is already absent from {1}.",
                        species.GetShortName(),
                        StringUtilities.ConjunctiveJoin(zones.Where(zone => !currentZoneIds.Any(id => id == zone.Id)).Select(zone => string.Format("**{0}**", zone.GetFullName())).ToArray())));

                }

                if (zones.Any(zone => currentZoneIds.Any(id => id == zone.Id))) {

                    // Show a confirmation of all valid zones.

                    await ReplySuccessAsync(string.Format("**{0}** no longer inhabits {1}.",
                        species.GetShortName(),
                        StringUtilities.DisjunctiveJoin(zones.Where(zone => currentZoneIds.Any(id => id == zone.Id)).Select(zone => string.Format("**{0}**", zone.GetFullName())).ToArray())));

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

                await this.ScanTrophiesAsync(user.ToCreator());

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

        [Command("search", RunMode = RunMode.Async)]
        public async Task Search([Remainder]string queryString) {

            // Create and execute the search query.

            ISearchQuery query = new SearchQuery(queryString);
            ISearchResult result = await Db.GetSearchResultsAsync(SearchContext, query);

            if (result.TotalResults() <= 0) {

                // There are no results to display.

                await ReplyInfoAsync("No species matching this query could be found.");

            }
            else {

                if (result.DisplayFormat == SearchResultDisplayFormat.Gallery) {

                    // Display the result as a picture gallery.

                    List<IPicture> pictures = new List<IPicture>();

                    foreach (ISpecies species in await result.GetResultsAsync())
                        pictures.AddRange(species.Pictures);

                    await ReplyGalleryAsync($"search results ({result.TotalResults()})", pictures);

                }
                else if (result.DisplayFormat == SearchResultDisplayFormat.Leaderboard) {

                    // Display the result as a leaderboard.

                    ILeaderboard leaderboard = new Leaderboard("Search results");

                    foreach (ISearchResultGroup group in result.Groups)
                        leaderboard.Add(group.Name, group.Count());

                    await ReplyLeaderboardAsync(leaderboard);

                }
                else {

                    if (result.TotalResults() == 1) {

                        // If there's only one result, just show that species.

                        ISpecies species = (await result.GetResultsAsync()).First();

                        await ReplySpeciesAsync(species);

                    }
                    else {

                        IPaginatedMessage message;

                        if (result.ContainsGroup(Data.Queries.SearchResult.DefaultGroupName)) {

                            // If there's only one group, just list the species without creating separate fields.

                            message = new PaginatedMessage(EmbedUtilities.CreateEmbedPages(
                                $"Search results ({result.TotalResults()})",
                                result.DefaultGroup.GetStringResults(),
                                options: EmbedPaginationOptions.AddPageNumbers));

                        }
                        else {

                            message = new PaginatedMessage(EmbedUtilities.CreateEmbedPages(result, options: EmbedPaginationOptions.AddPageNumbers));

                        }

                        foreach (Discord.Messaging.IEmbed page in message.Select(m => m.Embed))
                            page.Footer += $" — {result.TotalResults()} results in {DateUtilities.GetTimeSpanString(DateTimeOffset.UtcNow - result.Date)}";

                        await ReplyAsync(message);

                    }

                }

            }


        }

        [Command("+extinct", RunMode = RunMode.Async), Alias("setextinct"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetExtinct(string speciesName) {

            await SetExtinct(string.Empty, speciesName, string.Empty);

        }
        [Command("+extinct", RunMode = RunMode.Async), Alias("setextinct"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetExtinct(string arg0, string arg1) {

            // Possible cases:
            // 1. <genus> <species>
            // 2. <species> <reason>

            IEnumerable<ISpecies> matchingSpecies = await Db.GetSpeciesAsync(arg0, arg1);

            if (matchingSpecies.Count() > 0) {

                // One or more matching species exists, so we have case (1).

                await SetExtinct(arg0, arg1, string.Empty);

            }
            else {

                // No matching species exist, so we have case (2).

                await SetExtinct(string.Empty, arg0, arg1);

            }

        }
        [Command("+extinct", RunMode = RunMode.Async), Alias("setextinct"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetExtinct(string genusName, string speciesName, string reason) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                bool alreadyExtinct = species.IsExtinct();

                species.Status.ExtinctionDate = DateUtilities.GetCurrentDateUtc();
                species.Status.ExtinctionReason = reason;

                await Db.UpdateSpeciesAsync(species);

                string confirmationMessage = alreadyExtinct ?
                    $"Updated extinction details for **{species.GetShortName()}**." :
                    $"The last **{species.GetShortName()}** has perished, and the species is now extinct.";

                await ReplySuccessAsync(confirmationMessage);

            }

        }

        [Command("-extinct", RunMode = RunMode.Async), Alias("setextant", "unextinct"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task MinusExtinct(string speciesName) {

            await MinusExtinct(string.Empty, speciesName);

        }
        [Command("-extinct", RunMode = RunMode.Async), Alias("setextant", "unextinct"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task MinusExtinct(string genusName, string speciesName) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                if (species.IsExtinct()) {

                    // The species is extinct.

                    species.Status.ExtinctionDate = null;

                    await Db.UpdateSpeciesAsync(species);

                    await ReplySuccessAsync($"A population of **{species.GetShortName()}** has been discovered! The species is no longer considered extinct.");

                }
                else {

                    // The species was not extinct.

                    await ReplyWarningAsync($"**{species.GetShortName()}** is not extinct.");

                }

            }

        }

        [Command("extinct")]
        public async Task Extinct() {

            IEnumerable<ISpecies> species = await Db.GetExtinctSpeciesAsync();

            if (species.Count() > 0) {

                IEnumerable<Discord.Messaging.IEmbed> pages = EmbedUtilities.CreateEmbedPages($"Extinct species ({species.Count()})", species, options: EmbedPaginationOptions.NoStrikethrough | EmbedPaginationOptions.AddPageNumbers);

                await ReplyAsync(new Discord.Messaging.PaginatedMessage(pages));

            }
            else {

                await ReplyAsync("There are currently no extinct species.");

            }

        }
        [Command("extant")]
        public async Task Extant() {

            IEnumerable<ISpecies> species = await Db.GetExtantSpeciesAsync();

            if (species.Count() > 0) {

                IEnumerable<Discord.Messaging.IEmbed> pages = EmbedUtilities.CreateEmbedPages($"Extant species ({species.Count()})", species, options: EmbedPaginationOptions.AddPageNumbers);

                await ReplyAsync(new Discord.Messaging.PaginatedMessage(pages));

            }
            else {

                await ReplyAsync("There are currently no extant species.");

            }

        }

        [Command("setancestor", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetAncestor(string childSpeciesName, string ancestorSpeciesName) {

            await SetAncestor(string.Empty, childSpeciesName, string.Empty, ancestorSpeciesName);

        }
        [Command("setancestor", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetAncestor(string arg0, string arg1, string arg2) {

            // Possible cases:
            // 1. <childGenus> <childSpecies> <ancestorSpecies>
            // 2. <childSpecies> <ancestorGenus> <ancestorSpecies>

            ISpeciesAmbiguityResolverResult result = await ReplyResolveAmbiguityAsync(arg0, arg1, arg2);

            if (result.Success)
                await ReplySetAncestorAsync(result.First.First(), result.Second.First());

        }
        [Command("setancestor", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetAncestor(string childGenusName, string childSpeciesName, string ancestorGenusName, string ancestorSpeciesName) {

            ISpecies childSpecies = await GetSpeciesOrReplyAsync(childGenusName, childSpeciesName);
            ISpecies ancestorSpecies = childSpecies.IsValid() ? await GetSpeciesOrReplyAsync(ancestorGenusName, ancestorSpeciesName) : null;

            if (childSpecies.IsValid() && ancestorSpecies.IsValid())
                await ReplySetAncestorAsync(childSpecies, ancestorSpecies);

        }

        [Command("ancestry", RunMode = RunMode.Async), Alias("lineage", "ancestors", "anc")]
        public async Task Lineage(string speciesName) {

            await Lineage(string.Empty, speciesName);

        }
        [Command("ancestry", RunMode = RunMode.Async), Alias("lineage", "ancestors", "anc")]
        public async Task Lineage(string genusName, string speciesName) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                TreeNode<AncestryTree.NodeData> tree = await AncestryTree.GenerateTreeAsync(Db, species, AncestryTreeGenerationFlags.AncestorsOnly);

                AncestryTreeTextRenderer renderer = new AncestryTreeTextRenderer {
                    Tree = tree,
                    DrawLines = false,
                    MaxLength = DiscordUtilities.MaxMessageLength - 6, // account for code block markup
                    TimestampFormatter = x => GetDateStringAsync(DateUtilities.GetDateFromTimestamp(x), DateStringFormat.Short).Result,
                    TaxonFormatter = TaxonFormatter
                };

                await ReplyAsync(string.Format("```{0}```", renderer.ToString()));

            }

        }
        [Command("ancestry2", RunMode = RunMode.Async), Alias("lineage2", "anc2")]
        public async Task Lineage2(string species) {

            await Lineage2(string.Empty, species);

        }
        [Command("ancestry2", RunMode = RunMode.Async), Alias("lineage2", "anc2")]
        public async Task Lineage2(string genusName, string speciesName) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                string image = await AncestryTreeImageRenderer.Save(Db, species, AncestryTreeGenerationFlags.Full, formatter: TaxonFormatter);

                await Context.Channel.SendFileAsync(image);

            }

        }

        [Command("evolution", RunMode = RunMode.Async), Alias("evo")]
        public async Task Evolution(string speciesName) {

            await Evolution(string.Empty, speciesName);

        }
        [Command("evolution", RunMode = RunMode.Async), Alias("evo")]
        public async Task Evolution(string genusName, string speciesName) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                TreeNode<AncestryTree.NodeData> tree = await AncestryTree.GenerateTreeAsync(Db, species, AncestryTreeGenerationFlags.DescendantsOnly);

                AncestryTreeTextRenderer renderer = new AncestryTreeTextRenderer {
                    Tree = tree,
                    MaxLength = DiscordUtils.MaxMessageLength - 6, // account for code block markup
                    TimestampFormatter = x => GetDateStringAsync(DateUtilities.GetDateFromTimestamp(x), DateStringFormat.Short).Result,
                    TaxonFormatter = TaxonFormatter
                };

                await ReplyAsync(string.Format("```{0}```", renderer.ToString()));

            }

        }
        [Command("evolution2", RunMode = RunMode.Async), Alias("evo2")]
        public async Task Evolution2(string speciesName) {

            await Evolution2(string.Empty, speciesName);

        }
        [Command("evolution2", RunMode = RunMode.Async), Alias("evo2")]
        public async Task Evolution2(string genusName, string speciesName) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                string image = await AncestryTreeImageRenderer.Save(Db, species, AncestryTreeGenerationFlags.DescendantsOnly, formatter: TaxonFormatter);

                await Context.Channel.SendFileAsync(image);

            }

        }

        [Command("migration", RunMode = RunMode.Async), Alias("spread")]
        public async Task Migration(string speciesName) {

            await Migration(string.Empty, speciesName);

        }
        [Command("migration", RunMode = RunMode.Async), Alias("spread")]
        public async Task Migration(string genusName, string speciesName) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                IEnumerable<ISpeciesZoneInfo> zones = (await Db.GetZonesAsync(species)).OrderBy(x => x.Date);

                if (zones.Count() <= 0) {

                    await ReplyInfoAsync($"**{species.GetShortName()}** is not present in any zones.");

                }
                else {

                    // Group zones changes that happened closely together (12 hours).

                    List<List<ISpeciesZoneInfo>> zoneGroups = new List<List<ISpeciesZoneInfo>>();

                    DateTimeOffset? lastTimestamp = zones.Count() > 0 ? zones.First().Date : default;

                    foreach (ISpeciesZoneInfo zone in zones) {

                        if (zoneGroups.Count() <= 0)
                            zoneGroups.Add(new List<ISpeciesZoneInfo>());

                        if (zoneGroups.Last().Count() <= 0 || Math.Abs((zoneGroups.Last().Last().Date - zone.Date).Value.TotalSeconds) < 60 * 60 * 12)
                            zoneGroups.Last().Add(zone);
                        else {

                            lastTimestamp = zone.Date;
                            zoneGroups.Add(new List<ISpeciesZoneInfo> { zone });

                        }

                    }

                    StringBuilder result = new StringBuilder();

                    for (int i = 0; i < zoneGroups.Count(); ++i) {

                        if (zoneGroups[i].Count() <= 0)
                            continue;

                        DateTimeOffset? ts = i == 0 ? species.CreationDate : zoneGroups[i].First().Date;

                        if (!ts.HasValue)
                            ts = species.CreationDate;

                        result.Append(string.Format("{0} - ", await GetDateStringAsync((DateTimeOffset)ts, DateStringFormat.Short)));
                        result.Append(i == 0 ? "Started in " : "Spread to ");
                        result.Append(zoneGroups[i].Count() == 1 ? "Zone " : "Zones ");
                        result.Append(StringUtilities.ConjunctiveJoin(zoneGroups[i].Select(x => x.Zone.GetShortName())));

                        result.AppendLine();

                    }

                    await ReplyAsync(string.Format("```{0}```", result.ToString()));

                }

            }

        }

        [Command("size", RunMode = RunMode.Async), Alias("sz")]
        public async Task Size(string speciesName) {

            await Size(string.Empty, speciesName);

        }
        [Command("size", RunMode = RunMode.Async), Alias("sz")]
        public async Task Size(string arg0, string arg1) {

            // This command can be used in a number of ways:
            // <genus> <species>    -> returns size for that species
            // <species> <units>    -> returns size for that species, using the given units

            ISpecies species = null;
            ILengthUnit units = null;
            bool invalidUnits = false;

            // Attempt to get the specified species, assuming the user passed in <genus> <species>.

            IEnumerable<ISpecies> speciesResults = await Db.GetSpeciesAsync(arg0, arg1);

            if (speciesResults.Count() > 1)
                await ReplyValidateSpeciesAsync(speciesResults);
            else if (speciesResults.Count() == 1)
                species = speciesResults.First();
            else if (speciesResults.Count() <= 0) {

                // If we didn't get any species by treating the arguments as <genus> <species>, attempt to get the species by <species> only.      

                species = await GetSpeciesOrReplyAsync(string.Empty, arg0);

                if (species.IsValid()) {

                    // Assume the second argument was the desired units.
                    // Make sure the units given are valid.

                    invalidUnits = !LengthUnit.TryParse(arg1, out units);


                }

            }

            if (invalidUnits)
                await ReplyErrorAsync($"Invalid units ({arg1}).");
            else if (species.IsValid())
                await ReplySizeAsync(species, units);

        }
        [Command("size", RunMode = RunMode.Async), Alias("sz")]
        public async Task Size(string genusName, string speciesName, string units) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid())
                await ReplySizeAsync(species, units);

        }

        [Command("taxonomy", RunMode = RunMode.Async), Alias("taxon")]
        public async Task Taxonomy(string speciesName) {

            await Taxonomy(string.Empty, speciesName);

        }
        [Command("taxonomy", RunMode = RunMode.Async), Alias("taxon")]
        public async Task Taxonomy(string genusName, string speciesName) {

            ISpecies species = await GetSpeciesOrReplyAsync(genusName, speciesName);

            if (species.IsValid()) {

                Discord.Messaging.IEmbed embed = new Discord.Messaging.Embed {
                    Title = string.Format("Taxonomy of {0}", TaxonFormatter.GetString(species, false)),
                    ThumbnailUrl = species.GetPictureUrl()
                };

                IDictionary<TaxonRankType, ITaxon> taxonomy = await Db.GetTaxaAsync(species);

                string unknown = "Unknown";

                string tGenusName = taxonomy.GetOrDefault(TaxonRankType.Genus)?.GetName() ?? unknown;
                string tFamilyName = taxonomy.GetOrDefault(TaxonRankType.Family)?.GetName() ?? unknown;
                string tOrderName = taxonomy.GetOrDefault(TaxonRankType.Order)?.GetName() ?? unknown;
                string tClassName = taxonomy.GetOrDefault(TaxonRankType.Class)?.GetName() ?? unknown;
                string tPhylumName = taxonomy.GetOrDefault(TaxonRankType.Phylum)?.GetName() ?? unknown;
                string tKingdomName = taxonomy.GetOrDefault(TaxonRankType.Kingdom)?.GetName() ?? unknown;
                string tDomainName = taxonomy.GetOrDefault(TaxonRankType.Domain)?.GetName() ?? unknown;

                embed.AddField("Domain", tDomainName, inline: true);
                embed.AddField("Kingdom", tKingdomName, inline: true);
                embed.AddField("Phylum", tPhylumName, inline: true);
                embed.AddField("Class", tClassName, inline: true);
                embed.AddField("Order", tOrderName, inline: true);
                embed.AddField("Family", tFamilyName, inline: true);
                embed.AddField("Genus", tGenusName, inline: true);
                embed.AddField("Species", species.Name.ToTitle(), inline: true);

                await ReplyAsync(embed);

            }

        }

        // Private members

        private async Task ReplyAddSpeciesToZonesAsync(ISpecies species, string zoneList, string notes, bool onlyShowErrors = false) {

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

                string zoneListString = StringUtilities.ConjunctiveJoin(invalidZoneNames.Select(x => string.Format("**{0}**", ZoneUtilities.GetFullName(x))));

                StringBuilder warningBuilder = new StringBuilder();

                if (invalidZoneNames.Count() > 1)
                    warningBuilder.Append("Zones ");
                else if (!zoneListString.StartsWith("**zone", StringComparison.OrdinalIgnoreCase))
                    warningBuilder.Append("Zone ");

                warningBuilder.Append(zoneListString);

                if (invalidZoneNames.Count() == 1)
                    warningBuilder.Append(" does ");
                else
                    warningBuilder.Append(" do ");

                warningBuilder.Append("not exist.");

                await ReplyWarningAsync(warningBuilder.ToString());

            }

            if (zones.Count() > 0 && !onlyShowErrors) {

                // Show a confirmation of all valid zones.

                await ReplySuccessAsync(
                    string.Format("**{0}** now inhabits {1}.",
                    species.GetShortName(),
                    StringUtilities.ConjunctiveJoin(zones.Select(x => string.Format("**{0}**", x.GetFullName())).ToArray())));

            }

        }

        private async Task ReplySetAncestorAsync(ISpecies childSpecies, ISpecies ancestorSpecies) {

            if (childSpecies.IsValid() && ancestorSpecies.IsValid()) {

                if (childSpecies.Id == ancestorSpecies.Id) {

                    await ReplyErrorAsync("A species cannot be its own ancestor.");

                }
                else if ((await Db.GetDescendantIdsAsync(childSpecies.Id, GetSpeciesOptions.BreakOnCycle)).Contains(ancestorSpecies.Id.Value)) {

                    await ReplyErrorAsync($"Setting {childSpecies.GetShortName().ToPossessive().ToBold()} ancestor to {ancestorSpecies.GetShortName().ToBold()} would produce a cycle.");

                }
                else {

                    // Check if an ancestor has already been set for this species. If so, update the ancestor, but we'll show a different message later notifying the user of the change.

                    ISpecies existingAncestorSpecies = await Db.GetAncestorAsync(childSpecies);

                    if (existingAncestorSpecies.IsValid() && existingAncestorSpecies.Id == ancestorSpecies.Id) {

                        // If the ancestor has already been set to the species specified, quit.

                        await ReplyWarningAsync($"**{ancestorSpecies.GetShortName()}** has already been set as the ancestor of **{childSpecies.GetShortName()}**.");

                    }
                    else {

                        await Db.SetAncestorAsync(childSpecies, ancestorSpecies);

                        if (!existingAncestorSpecies.IsValid())
                            await ReplySuccessAsync($"**{ancestorSpecies.GetShortName()}** has been set as the ancestor of **{childSpecies.GetShortName()}**.");
                        else
                            await ReplySuccessAsync($"**{ancestorSpecies.GetShortName()}** has replaced **{existingAncestorSpecies.GetShortName()}** as the ancestor of **{childSpecies.GetShortName()}**.");

                    }

                }

            }

        }

        public async Task ReplySizeAsync(ISpecies species, string units) {

            LengthUnit.TryParse(units, out ILengthUnit lengthUnits);

            if (lengthUnits is null)
                await ReplyErrorAsync(string.Format("Invalid units (\"{0}\").", units));
            else
                await ReplySizeAsync(species, lengthUnits);

        }
        public async Task ReplySizeAsync(ISpecies species, ILengthUnit units) {

            // Attempt to get the size of the species.

            SpeciesSizeMatch match = SpeciesSizeMatch.Match(species.Description);

            // Output the result.

            Discord.Messaging.IEmbed embed = new Discord.Messaging.Embed {
                Title = string.Format("Size of {0}", species.GetFullName()),
                Description = units is null ? match.ToString() : match.ToString(units),
                Footer = "Size is determined from species description, and may not be accurate."
            };

            await ReplyAsync(embed);

        }

    }

}