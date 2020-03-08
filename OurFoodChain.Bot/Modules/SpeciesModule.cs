using Discord;
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

        [Command("+extinct"), Alias("setextinct")]
        public async Task SetExtinct(string species) {
            await SetExtinct("", species, "");
        }
        [Command("+extinct"), Alias("setextinct")]
        public async Task SetExtinct(string arg0, string arg1) {

            // We either have a genus/species, or a species/description.

            Species[] species_list = await SpeciesUtils.GetSpeciesAsync(arg0, arg1);

            if (species_list.Count() > 0)
                // If such a species does exist, assume we have a genus/species.
                await SetExtinct(arg0, arg1, string.Empty);
            else
                // If no such species exists, assume we have a species/description.
                await SetExtinct(string.Empty, arg0, arg1);

        }
        [Command("+extinct"), Alias("setextinct")]
        public async Task SetExtinct(string genus, string species, string reason) {

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            await SetExtinct(sp, reason);

        }
        private async Task SetExtinct(Species species, string reason) {

            // Ensure that the user has necessary privileges to use this command.
            if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator, species))
                return;

            await SpeciesUtils.SetExtinctionInfoAsync(species, new ExtinctionInfo {
                IsExtinct = true,
                Reason = reason,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });

            await BotUtils.ReplyAsync_Success(Context, string.Format(
                species.IsExtinct ?
                "Updated extinction details for **{0}**." :
                "The last **{0}** has perished, and the species is now extinct.",
                species.ShortName));

        }

        [Command("-extinct"), Alias("setextant", "unextinct"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task MinusExtinct(string species) {
            await MinusExtinct("", species);
        }
        [Command("-extinct"), Alias("setextant", "unextinct"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task MinusExtinct(string genus, string species) {

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            // If the species is not extinct, don't do anything.

            if (!sp.IsExtinct) {

                await BotUtils.ReplyAsync_Warning(Context, string.Format("**{0}** is not extinct.", sp.ShortName));

                return;

            }

            // Delete the extinction from the database.

            await SpeciesUtils.SetExtinctionInfoAsync(sp, new ExtinctionInfo { IsExtinct = false });

            await BotUtils.ReplyAsync_Success(Context, string.Format("A population of **{0}** has been discovered! The species is no longer considered extinct.", sp.ShortName));

        }

        [Command("extinct")]
        public async Task Extinct() {

            List<Species> sp_list = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM Extinctions);"))
            using (DataTable rows = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in rows.Rows)
                    sp_list.Add(await SpeciesUtils.SpeciesFromDataRow(row));

            sp_list.Sort((lhs, rhs) => lhs.ShortName.CompareTo(rhs.ShortName));

            PaginatedMessageBuilder embed = new PaginatedMessageBuilder();
            embed.AddPages(EmbedUtils.SpeciesListToEmbedPages(sp_list.Select(s => new SpeciesAdapter(s)), fieldName: string.Format("Extinct species ({0})", sp_list.Count()), flags: EmbedPagesFlag.None));

            await DiscordUtils.SendMessageAsync(Context, embed.Build(), "There are currently no extinct species.");

        }

        [Command("setancestor"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetAncestor(string species, string ancestorSpecies) {
            await SetAncestor(string.Empty, species, string.Empty, ancestorSpecies);
        }
        [Command("setancestor"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetAncestor(string genus, string species, string ancestorSpecies) {
            await SetAncestor(genus, species, genus, ancestorSpecies);
        }
        [Command("setancestor"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task SetAncestor(string genus, string species, string ancestorGenus, string ancestorSpecies) {

            // Get the descendant and ancestor species.

            Species[] descendant_list = await BotUtils.GetSpeciesFromDb(genus, species);
            Species[] ancestor_list = await BotUtils.GetSpeciesFromDb(ancestorGenus, ancestorSpecies);

            if (descendant_list.Count() > 1)
                await BotUtils.ReplyAsync_Error(Context, string.Format("The child species \"{0}\" is too vague (there are multiple matches). Try including the genus.", species));
            else if (ancestor_list.Count() > 1)
                await BotUtils.ReplyAsync_Error(Context, string.Format("The ancestor species \"{0}\" is too vague (there are multiple matches). Try including the genus.", ancestorSpecies));
            else if (descendant_list.Count() == 0)
                await BotUtils.ReplyAsync_Error(Context, "The child species does not exist.");
            else if (ancestor_list.Count() == 0)
                await BotUtils.ReplyAsync_Error(Context, "The parent species does not exist.");
            else if (descendant_list[0].Id == ancestor_list[0].Id)
                await BotUtils.ReplyAsync_Error(Context, "A species cannot be its own ancestor.");
            else {

                Species descendant = descendant_list[0];
                Species ancestor = ancestor_list[0];

                // Check if an ancestor has already been set for this species. If so, update the ancestor, but we'll show a different message later notifying the user of the change.

                Species existing_ancestor_sp = null;

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT ancestor_id FROM Ancestors WHERE species_id=$species_id;")) {

                    cmd.Parameters.AddWithValue("$species_id", descendant.Id);

                    DataRow row = await Database.GetRowAsync(cmd);

                    if (!(row is null)) {

                        long ancestor_id = row.Field<long>("ancestor_id");

                        existing_ancestor_sp = await BotUtils.GetSpeciesFromDb(ancestor_id);

                    }

                }

                // If the ancestor has already been set to the species specified, quit.

                if (!(existing_ancestor_sp is null) && existing_ancestor_sp.Id == ancestor.Id) {

                    await BotUtils.ReplyAsync_Warning(Context, string.Format("**{0}** has already been set as the ancestor of **{1}**.", ancestor.ShortName, descendant.ShortName));

                    return;

                }

                // Insert the new relationship into the database.

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Ancestors(species_id, ancestor_id) VALUES($species_id, $ancestor_id);")) {

                    cmd.Parameters.AddWithValue("$species_id", descendant.Id);
                    cmd.Parameters.AddWithValue("$ancestor_id", ancestor.Id);

                    await Database.ExecuteNonQuery(cmd);

                }

                if (existing_ancestor_sp is null)
                    await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** has been set as the ancestor of **{1}**.", ancestor.ShortName, descendant.ShortName));
                else
                    await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** has replaced **{1}** as the ancestor of **{2}**.", ancestor.ShortName, existing_ancestor_sp.ShortName, descendant.ShortName));

            }

        }

        [Command("ancestry"), Alias("lineage", "ancestors", "anc")]
        public async Task Lineage(string speciesName) {
            await Lineage(string.Empty, speciesName);
        }
        [Command("ancestry"), Alias("lineage", "ancestors", "anc")]
        public async Task Lineage(string genusName, string speciesName) {

            Species species = await BotUtils.ReplyFindSpeciesAsync(Context, genusName, speciesName);

            if (species != null) {

                TreeNode<AncestryTree.NodeData> tree = await AncestryTree.GenerateTreeAsync(species, AncestryTreeGenerationFlags.AncestorsOnly);

                AncestryTreeTextRenderer renderer = new AncestryTreeTextRenderer {
                    Tree = tree,
                    DrawLines = false,
                    MaxLength = Bot.DiscordUtils.MaxMessageLength - 6, // account for code block markup
                    TimestampFormatter = x => BotUtils.TimestampToDateStringAsync(x, new OfcBotContext(Context, BotConfiguration, Db), TimestampToDateStringFormat.Short).Result
                };

                await ReplyAsync(string.Format("```{0}```", renderer.ToString()));

            }

        }
        [Command("ancestry2"), Alias("lineage2", "anc2")]
        public async Task Lineage2(string species) {
            await Lineage2("", species);
        }
        [Command("ancestry2"), Alias("lineage2", "anc2")]
        public async Task Lineage2(string genus, string species) {

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            string image = await AncestryTreeImageRenderer.Save(sp, AncestryTreeGenerationFlags.Full);

            await Context.Channel.SendFileAsync(image);

        }

        [Command("evolution"), Alias("evo")]
        public async Task Evolution(string speciesName) {
            await Evolution(string.Empty, speciesName);
        }
        [Command("evolution"), Alias("evo")]
        public async Task Evolution(string genusName, string speciesName) {

            Species species = await BotUtils.ReplyFindSpeciesAsync(Context, genusName, speciesName);

            if (species != null) {

                TreeNode<AncestryTree.NodeData> tree = await AncestryTree.GenerateTreeAsync(species, AncestryTreeGenerationFlags.DescendantsOnly);

                AncestryTreeTextRenderer renderer = new AncestryTreeTextRenderer {
                    Tree = tree,
                    MaxLength = Bot.DiscordUtils.MaxMessageLength - 6, // account for code block markup
                    TimestampFormatter = x => BotUtils.TimestampToDateStringAsync(x, new OfcBotContext(Context, BotConfiguration, Db), TimestampToDateStringFormat.Short).Result
                };

                await ReplyAsync(string.Format("```{0}```", renderer.ToString()));

            }

        }
        [Command("evolution2"), Alias("evo2")]
        public async Task Evolution2(string species) {
            await Evolution2("", species);
        }
        [Command("evolution2"), Alias("evo2")]
        public async Task Evolution2(string genus, string species) {

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            string image = await AncestryTreeImageRenderer.Save(sp, AncestryTreeGenerationFlags.DescendantsOnly);

            await Context.Channel.SendFileAsync(image);

        }

        [Command("migration"), Alias("spread")]
        public async Task Migration(string speciesName) {
            await Migration("", speciesName);
        }
        [Command("migration"), Alias("spread")]
        public async Task Migration(string genusName, string speciesName) {

            Species species = await BotUtils.ReplyFindSpeciesAsync(Context, genusName, speciesName);

            if (species is null)
                return;

            ISpeciesZoneInfo[] zones = (await Db.GetZonesAsync(new SpeciesAdapter(species))).OrderBy(x => x.Date).ToArray();

            if (zones.Count() <= 0) {

                await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** is not present in any zones.", species.ShortName));

            }
            else {

                // Group zones changes that happened closely together (12 hours).

                List<List<ISpeciesZoneInfo>> zone_groups = new List<List<ISpeciesZoneInfo>>();

                DateTimeOffset? last_timestamp = zones.Count() > 0 ? zones.First().Date : default;

                foreach (ISpeciesZoneInfo zone in zones) {

                    if (zone_groups.Count() <= 0)
                        zone_groups.Add(new List<ISpeciesZoneInfo>());

                    if (zone_groups.Last().Count() <= 0 || Math.Abs((zone_groups.Last().Last().Date - zone.Date).Value.TotalSeconds) < 60 * 60 * 12)
                        zone_groups.Last().Add(zone);
                    else {

                        last_timestamp = zone.Date;
                        zone_groups.Add(new List<ISpeciesZoneInfo> { zone });

                    }

                }


                StringBuilder result = new StringBuilder();

                for (int i = 0; i < zone_groups.Count(); ++i) {

                    if (zone_groups[i].Count() <= 0)
                        continue;

                    DateTimeOffset? ts = i == 0 ? DateUtilities.GetDateFromTimestamp(species.Timestamp) : zone_groups[i].First().Date;

                    if (!ts.HasValue)
                        ts = DateUtilities.GetDateFromTimestamp(species.Timestamp);

                    result.Append(string.Format("{0} - ", await BotUtils.TimestampToDateStringAsync(ts.Value.ToUnixTimeSeconds(), new OfcBotContext(Context, BotConfiguration, Db), TimestampToDateStringFormat.Short)));
                    result.Append(i == 0 ? "Started in " : "Spread to ");
                    result.Append(zone_groups[i].Count() == 1 ? "Zone " : "Zones ");
                    result.Append(StringUtilities.ConjunctiveJoin(", ", zone_groups[i].Select(x => x.Zone.GetShortName())));

                    result.AppendLine();

                }

                await ReplyAsync(string.Format("```{0}```", result.ToString()));

            }

        }

        [Command("size"), Alias("sz")]
        public async Task Size(string species) {
            await Size("", species);
        }
        [Command("size"), Alias("sz")]
        public async Task Size(string genusOrSpecies, string speciesOrUnits) {

            // This command can be used in a number of ways:
            // <genus> <species>    -> returns size for that species
            // <species> <units>    -> returns size for that species, using the given units

            Species species = null;
            ILengthUnit units = null;

            // Attempt to get the specified species, assuming the user passed in <genus> <species>.

            IEnumerable<Species> speciesResults = await BotUtils.GetSpeciesFromDb(genusOrSpecies, speciesOrUnits);

            if (speciesResults.Count() > 1)
                await BotUtils.ReplyValidateSpeciesAsync(Context, speciesResults);
            else if (speciesResults.Count() == 1)
                species = speciesResults.First();
            else if (speciesResults.Count() <= 0) {

                // If we didn't get any species by treating the arguments as <genus> <species>, attempt to get the species by <species> only.         
                species = await BotUtils.ReplyFindSpeciesAsync(Context, "", genusOrSpecies);

                // If this still fails, there's nothing left to do.

                if (species is null)
                    return;

                // Assume the second argument was the desired units.
                // Make sure the units given are valid.

                LengthUnit.TryParse(speciesOrUnits, out units);

                if (units is null) {

                    await BotUtils.ReplyAsync_Error(Context, string.Format("Invalid units (\"{0}\").", speciesOrUnits));

                    return;

                }

            }

            if (species != null)
                await Size(species, units);

        }
        public async Task Size(Species species, string units) {

            LengthUnit.TryParse(units, out ILengthUnit lengthUnits);

            if (lengthUnits is null)
                await BotUtils.ReplyAsync_Error(Context, string.Format("Invalid units (\"{0}\").", units));
            else
                await Size(species, lengthUnits);

        }
        public async Task Size(Species species, ILengthUnit units) {

            // Attempt to get the size of the species.

            SpeciesSizeMatch match = SpeciesSizeMatch.Find(species.Description);

            // Output the result.

            EmbedBuilder embed = new EmbedBuilder();
            embed.Title = string.Format("Size of {0}", species.FullName);
            embed.WithDescription(units is null ? match.ToString() : match.ToString(units));
            embed.WithFooter("Size is determined from species description, and may not be accurate.");

            await ReplyAsync("", false, embed.Build());

        }
        [Command("size"), Alias("sz")]
        public async Task Size(string genus, string species, string units) {

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (!(species is null))
                await Size(sp, units);

        }

        [Command("taxonomy"), Alias("taxon")]
        public async Task Taxonomy(string species) {
            await Taxonomy("", species);
        }
        [Command("taxonomy"), Alias("taxon")]
        public async Task Taxonomy(string genus, string species) {

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(string.Format("Taxonomy of {0}", sp.ShortName));
            embed.WithThumbnailUrl(sp.Picture);

            TaxonSet set = await BotUtils.GetFullTaxaFromDb(sp);

            string unknown = "Unknown";
            string genus_name = set.Genus is null ? unknown : set.Genus.GetName();
            string family_name = set.Family is null ? unknown : set.Family.GetName();
            string order_name = set.Order is null ? unknown : set.Order.GetName();
            string class_name = set.Class is null ? unknown : set.Class.GetName();
            string phylum_name = set.Phylum is null ? unknown : set.Phylum.GetName();
            string kingdom_name = set.Kingdom is null ? unknown : set.Kingdom.GetName();
            string domain_name = set.Domain is null ? unknown : set.Domain.GetName();

            embed.AddField("Domain", domain_name, inline: true);
            embed.AddField("Kingdom", kingdom_name, inline: true);
            embed.AddField("Phylum", phylum_name, inline: true);
            embed.AddField("Class", class_name, inline: true);
            embed.AddField("Order", order_name, inline: true);
            embed.AddField("Family", family_name, inline: true);
            embed.AddField("Genus", genus_name, inline: true);
            embed.AddField("Species", StringUtilities.ToTitleCase(sp.Name), inline: true);

            await ReplyAsync("", false, embed.Build());

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