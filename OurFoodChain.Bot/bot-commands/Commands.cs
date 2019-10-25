using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Commands {

    public class Commands :
        ModuleBase {

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
            if (!await BotUtils.ReplyHasPrivilegeOrOwnershipAsync(Context, PrivilegeLevel.ServerModerator, species))
                return;

            await SpeciesUtils.SetExtinctionInfoAsync(species, new ExtinctionInfo {
                IsExtinct = true,
                Reason = reason,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });

            await BotUtils.ReplyAsync_Success(Context, string.Format(
                species.isExtinct ?
                "Updated extinction details for **{0}**." :
                "The last **{0}** has perished, and the species is now extinct.",
                species.GetShortName()));

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

            if (!sp.isExtinct) {

                await BotUtils.ReplyAsync_Warning(Context, string.Format("**{0}** is not extinct.", sp.GetShortName()));

                return;

            }

            // Delete the extinction from the database.

            await SpeciesUtils.SetExtinctionInfoAsync(sp, new ExtinctionInfo { IsExtinct = false });

            await BotUtils.ReplyAsync_Success(Context, string.Format("A population of **{0}** has been discovered! The species is no longer considered extinct.", sp.GetShortName()));

        }

        [Command("extinct")]
        public async Task Extinct() {

            List<Species> sp_list = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM Extinctions);"))
            using (DataTable rows = await Database.GetRowsAsync(cmd))
                foreach (DataRow row in rows.Rows)
                    sp_list.Add(await Species.FromDataRow(row));

            sp_list.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

            PaginatedMessage embed = new PaginatedMessage();
            embed.AddPages(EmbedUtils.SpeciesListToEmbedPages(sp_list, fieldName: string.Format("Extinct species ({0})", sp_list.Count()), flags: EmbedPagesFlag.None));

            await CommandUtils.SendMessageAsync(Context, embed.Build(), "There are currently no extinct species.");

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
            else if (descendant_list[0].id == ancestor_list[0].id)
                await BotUtils.ReplyAsync_Error(Context, "A species cannot be its own ancestor.");
            else {

                Species descendant = descendant_list[0];
                Species ancestor = ancestor_list[0];

                // Check if an ancestor has already been set for this species. If so, update the ancestor, but we'll show a different message later notifying the user of the change.

                Species existing_ancestor_sp = null;

                using (SQLiteCommand cmd = new SQLiteCommand("SELECT ancestor_id FROM Ancestors WHERE species_id=$species_id;")) {

                    cmd.Parameters.AddWithValue("$species_id", descendant.id);

                    DataRow row = await Database.GetRowAsync(cmd);

                    if (!(row is null)) {

                        long ancestor_id = row.Field<long>("ancestor_id");

                        existing_ancestor_sp = await BotUtils.GetSpeciesFromDb(ancestor_id);

                    }

                }

                // If the ancestor has already been set to the species specified, quit.

                if (!(existing_ancestor_sp is null) && existing_ancestor_sp.id == ancestor.id) {

                    await BotUtils.ReplyAsync_Warning(Context, string.Format("**{0}** has already been set as the ancestor of **{1}**.", ancestor.GetShortName(), descendant.GetShortName()));

                    return;

                }

                // Insert the new relationship into the database.

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Ancestors(species_id, ancestor_id) VALUES($species_id, $ancestor_id);")) {

                    cmd.Parameters.AddWithValue("$species_id", descendant.id);
                    cmd.Parameters.AddWithValue("$ancestor_id", ancestor.id);

                    await Database.ExecuteNonQuery(cmd);

                }

                if (existing_ancestor_sp is null)
                    await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** has been set as the ancestor of **{1}**.", ancestor.GetShortName(), descendant.GetShortName()));
                else
                    await BotUtils.ReplyAsync_Success(Context, string.Format("**{0}** has replaced **{1}** as the ancestor of **{2}**.", ancestor.GetShortName(), existing_ancestor_sp.GetShortName(), descendant.GetShortName()));

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
                    MaxLength = DiscordUtils.MaxMessageLength - 6, // account for code block markup
                    TimestampFormatter = x => BotUtils.TimestampToDateStringAsync(x, TimestampToDateStringFormat.Short).Result
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
                    MaxLength = DiscordUtils.MaxMessageLength - 6, // account for code block markup
                    TimestampFormatter = x => BotUtils.TimestampToDateStringAsync(x, TimestampToDateStringFormat.Short).Result
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

            SpeciesZone[] zones = (await SpeciesUtils.GetZonesAsync(species)).OrderBy(x => x.Timestamp).ToArray();

            if (zones.Count() <= 0) {

                await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** is not present in any zones.", species.ShortName));

            }
            else {

                // Group zones changes that happened closely together (12 hours).

                List<List<SpeciesZone>> zone_groups = new List<List<SpeciesZone>>();

                long last_timestamp = zones.Count() > 0 ? zones.First().Timestamp : 0;

                foreach (SpeciesZone zone in zones) {

                    if (zone_groups.Count() <= 0)
                        zone_groups.Add(new List<SpeciesZone>());

                    if (zone_groups.Last().Count() <= 0 || Math.Abs(zone_groups.Last().Last().Timestamp - zone.Timestamp) < 60 * 60 * 12)
                        zone_groups.Last().Add(zone);
                    else {

                        last_timestamp = zone.Timestamp;
                        zone_groups.Add(new List<SpeciesZone> { zone });

                    }

                }


                StringBuilder result = new StringBuilder();

                for (int i = 0; i < zone_groups.Count(); ++i) {

                    if (zone_groups[i].Count() <= 0)
                        continue;

                    long ts = i == 0 ? species.timestamp : zone_groups[i].First().Timestamp;

                    if (ts <= 0)
                        ts = species.timestamp;

                    result.Append(string.Format("{0} - ", await BotUtils.TimestampToDateStringAsync(ts, TimestampToDateStringFormat.Short)));
                    result.Append(i == 0 ? "Started in " : "Spread to ");
                    result.Append(zone_groups[i].Count() == 1 ? "Zone " : "Zones ");
                    result.Append(StringUtils.ConjunctiveJoin(", ", zone_groups[i].Select(x => x.Zone.ShortName)));

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
            LengthUnit units = LengthUnit.Unknown;

            // Attempt to get the specified species, assuming the user passed in <genus> <species>.

            Species[] species_array = await BotUtils.GetSpeciesFromDb(genusOrSpecies, speciesOrUnits);

            if (species_array.Count() > 1)
                await BotUtils.ReplyValidateSpeciesAsync(Context, species_array);
            else if (species_array.Count() == 1)
                species = species_array[0];
            else if (species_array.Count() <= 0) {

                // If we didn't get any species by treating the arguments as <genus> <species>, attempt to get the species by <species> only.         
                species = await BotUtils.ReplyFindSpeciesAsync(Context, "", genusOrSpecies);

                // If this still fails, there's nothing left to do.

                if (species is null)
                    return;

                // Assume the second argument was the desired units.
                // Make sure the units given are valid.

                units = new Length(0.0, speciesOrUnits).Units;

                if (units == LengthUnit.Unknown) {

                    await BotUtils.ReplyAsync_Error(Context, string.Format("Invalid units (\"{0}\").", speciesOrUnits));

                    return;

                }

            }

            if (!(species is null))
                await Size(species, units);

        }
        public async Task Size(Species species, string units) {

            LengthUnit length_units = new Length(0.0, units).Units;

            if (length_units == LengthUnit.Unknown)
                await BotUtils.ReplyAsync_Error(Context, string.Format("Invalid units (\"{0}\").", units));
            else
                await Size(species, length_units);

        }
        public async Task Size(Species species, LengthUnit units) {

            // Attempt to get the size of the species.

            SpeciesSizeMatch match = SpeciesSizeMatch.Match(species.description);

            // Output the result.

            EmbedBuilder embed = new EmbedBuilder();
            embed.Title = string.Format("Size of {0}", species.GetFullName());
            embed.WithDescription(units == LengthUnit.Unknown ? match.ToString() : match.ToString(units));
            embed.WithFooter("Size is determined from species description, and may not be accurate.");

            await ReplyAsync("", false, embed.Build());

        }
        [Command("size"), Alias("sz")]
        public async Task Size(string genus, string species, string units) {

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (!(species is null))
                await Size(sp, units);

        }

        [Command("search")]
        public async Task Search([Remainder]string queryString) {

            // Create and execute the search query.

            SearchQuery query = new SearchQuery(Context, queryString);
            SearchQuery.FindResult result = await query.FindMatchesAsync();

            // Build the embed.

            if (result.Count() <= 0) {

                await BotUtils.ReplyAsync_Info(Context, "No species matching this query could be found.");

            }
            else {

                if (result.DisplayFormat == SearchQuery.DisplayFormat.Gallery) {

                    List<Picture> pictures = new List<Picture>();

                    foreach (Species species in result.ToArray())
                        pictures.AddRange(await SpeciesUtils.GetPicturesAsync(species));

                    await GalleryCommands.ShowGalleryAsync(Context, string.Format("search results ({0})", result.Count()), pictures.ToArray());

                }
                else {

                    if (result.Count() == 1) {

                        // If there's only one result, just show that species.
                        await SpeciesCommands.ShowSpeciesInfoAsync(Context, result.ToArray()[0]);

                    }
                    else {

                        PaginatedMessage embed;

                        if (result.HasGroup(SearchQuery.DEFAULT_GROUP)) {

                            // If there's only one group, just list the species without creating separate fields.
                            embed = new PaginatedMessage(EmbedUtils.ListToEmbedPages(result.DefaultGroup.ToList(), fieldName: string.Format("Search results ({0})", result.Count())));

                        }
                        else {

                            embed = new PaginatedMessage();
                            embed.AddPages(EmbedUtils.SearchQueryResultToEmbedPages(result));

                        }

                        embed.SetFooter("");
                        embed.AddPageNumbers();

                        await CommandUtils.SendMessageAsync(Context, embed.Build());
                    }

                }

            }


        }

        [Command("roll")]
        public async Task Roll() {
            await Roll(6);
        }
        [Command("roll")]
        public async Task Roll(int max) {

            if (max < 1)
                await BotUtils.ReplyAsync_Error(Context, "Value must be greater than or equal 1.");

            else
                await Roll(1, max);

        }
        [Command("roll")]
        public async Task Roll(int min, int max) {

            if (min < 0 || max < 0)
                await BotUtils.ReplyAsync_Error(Context, "Values must be greater than 1.");
            if (min > max + 1)
                await BotUtils.ReplyAsync_Error(Context, "Minimum value must be less than or equal to the maximum value.");
            else {

                int result = BotUtils.RandomInteger(min, max + 1);

                await ReplyAsync(result.ToString());

            }

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
            embed.WithTitle(string.Format("Taxonomy of {0}", sp.GetShortName()));
            embed.WithThumbnailUrl(sp.pics);

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
            embed.AddField("Species", StringUtils.ToTitleCase(sp.name), inline: true);

            await ReplyAsync("", false, embed.Build());

        }

        [Command("random"), Alias("rand")]
        public async Task Random() {

            // Get a random species from the database.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id NOT IN (SELECT species_id FROM Extinctions) ORDER BY RANDOM() LIMIT 1;")) {

                DataRow row = await Database.GetRowAsync(cmd);

                if (row is null)
                    await BotUtils.ReplyAsync_Info(Context, "There are currently no extant species.");
                else
                    await SpeciesCommands.ShowSpeciesInfoAsync(Context, await Species.FromDataRow(row));

            }

        }
        [Command("random"), Alias("rand")]
        public async Task Random(string taxonName) {

            // Get the taxon.

            Taxon taxon = await BotUtils.GetTaxonFromDb(taxonName);

            if (taxon is null) {

                await BotUtils.ReplyAsync_Error(Context, "No such taxon exists.");

                return;

            }

            // Get all species under that taxon.

            List<Species> species = new List<Species>();
            species.AddRange(await BotUtils.GetSpeciesInTaxonFromDb(taxon));
            species.RemoveAll(x => x.isExtinct);

            if (species.Count() <= 0)
                await BotUtils.ReplyAsync_Info(Context, string.Format("{0} **{1}** does not contain any extant species.", StringUtils.ToTitleCase(taxon.GetTypeName()), taxon.GetName()));
            else
                await SpeciesCommands.ShowSpeciesInfoAsync(Context, species[BotUtils.RandomInteger(species.Count())]);

        }

        [Command("profile")]
        public async Task Profile() {
            await Profile(Context.User);
        }
        [Command("profile")]
        public async Task Profile(IUser user) {

            // Begin building the embed (add default parameters).

            EmbedBuilder embed = new EmbedBuilder();

            embed.WithTitle(string.Format("{0}'s profile", user.Username));
            embed.WithThumbnailUrl(user.GetAvatarUrl(size: 64));

            // Get basic information about the user.
            // This will return null if the user hasn't been seen before.

            UserInfo userInfo = await UserUtils.GetUserInfoAsync(user.Username, user.Id, UserInfoQueryFlags.MatchEither);

            if (userInfo is null) {

                embed.WithDescription(string.Format("{0} has not submitted any species.", user.Username));

            }
            else {

                long daysSinceFirstSubmission = (DateUtils.GetCurrentTimestamp() - userInfo.FirstSubmissionTimestamp) / 60 / 60 / 24;
                UserRank userRank = await UserUtils.GetRankAsync(userInfo, UserInfoQueryFlags.MatchEither);

                // Get the user's most active genus.

                Species[] userSpecies = await UserUtils.GetSpeciesAsync(userInfo, UserInfoQueryFlags.MatchEither);

                IGrouping<string, string> favoriteGenusGrouping = userSpecies
                    .Select(x => x.GenusName)
                    .GroupBy(x => x)
                    .OrderByDescending(x => x.Count())
                    .FirstOrDefault();

                string favoriteGenus = favoriteGenusGrouping is null ? "N/A" : favoriteGenusGrouping.First();
                int favoriteGenusCount = favoriteGenusGrouping is null ? 0 : favoriteGenusGrouping.Count();

                int userSpeciesCount = userSpecies.Count();
                int speciesCount = await SpeciesUtils.GetSpeciesCount();

                // Get the user's rarest trophy.

                string rarest_trophy = "N/A";

                Trophies.UnlockedTrophyInfo[] unlocked = await Global.TrophyRegistry.GetUnlockedTrophiesAsync(user.Id);

                if (unlocked.Count() > 0) {

                    Array.Sort(unlocked, (lhs, rhs) => lhs.timesUnlocked.CompareTo(rhs.timesUnlocked));

                    Trophies.Trophy trophy = await Global.TrophyRegistry.GetTrophyByIdentifierAsync(unlocked[0].identifier);

                    rarest_trophy = trophy.GetName();

                }

                // Put together the user's profile.

                if (OurFoodChainBot.Instance.Config.GenerationsEnabled) {

                    int generationsSinceFirstSubmission = (await GenerationUtils.GetGenerationsAsync()).Where(x => x.EndTimestamp > userInfo.FirstSubmissionTimestamp).Count();
                    double speciesPerGeneration = generationsSinceFirstSubmission <= 0 ? userSpeciesCount : (double)userSpeciesCount / generationsSinceFirstSubmission;

                    embed.WithDescription(string.Format("{0} made their first species during **{1}**.\nSince then, they have submitted **{2:0.0}** species per generation.\n\nTheir submissions make up **{3:0.0}%** of all species.",
                        user.Username,
                        await BotUtils.TimestampToDateStringAsync(userInfo.FirstSubmissionTimestamp),
                        speciesPerGeneration,
                        (double)userSpeciesCount / speciesCount * 100.0));

                }
                else {

                    embed.WithDescription(string.Format("{0} made their first species on **{1}**.\nSince then, they have submitted **{2:0.0}** species per day.\n\nTheir submissions make up **{3:0.0}%** of all species.",
                        user.Username,
                        await BotUtils.TimestampToDateStringAsync(userInfo.FirstSubmissionTimestamp),
                        daysSinceFirstSubmission == 0 ? userSpeciesCount : (double)userSpeciesCount / daysSinceFirstSubmission,
                        (double)userSpeciesCount / speciesCount * 100.0));

                }

                embed.AddField("Species", string.Format("{0} (Rank **#{1}**)", userSpeciesCount, userRank.Rank), inline: true);

                embed.AddField("Favorite genus", string.Format("{0} ({1} spp.)", StringUtils.ToTitleCase(favoriteGenus), favoriteGenusCount), inline: true);

                if (OurFoodChainBot.Instance.Config.TrophiesEnabled) {

                    embed.AddField("Trophies", string.Format("{0} ({1:0.0}%)",
                        (await Global.TrophyRegistry.GetUnlockedTrophiesAsync(user.Id)).Count(),
                        await Global.TrophyRegistry.GetUserCompletionRateAsync(user.Id)), inline: true);

                    embed.AddField("Rarest trophy", rarest_trophy, inline: true);

                }

            }

            await ReplyAsync("", false, embed.Build());

        }
        [Command("leaderboard")]
        public async Task Leaderboard() {

            UserRank[] userRanks = await UserUtils.GetRanksAsync();

            List<string> lines = new List<string>();

            foreach (UserRank userRank in userRanks) {

                IUser user = Context.Guild is null ? null : await Context.Guild.GetUserAsync(userRank.User.Id);

                lines.Add(string.Format("**`{0}`**{1}`{2}` {3}",
                        string.Format("{0}.", userRank.Rank.ToString("000")),
                        userRank.Icon,
                        userRank.User.SubmissionCount.ToString("000"),
                        string.Format(userRank.Rank <= 3 ? "**{0}**" : "{0}", user is null ? userRank.User.Username : user.Username)
                       ));

            }

            // Create the embed.

            PaginatedMessage embed = new PaginatedMessage();
            embed.AddPages(EmbedUtils.LinesToEmbedPages(lines));
            embed.SetTitle(string.Format("🏆 Leaderboard ({0})", lines.Count));
            embed.SetColor(255, 204, 77);
            embed.AddPageNumbers();

            // Send the embed.
            await CommandUtils.SendMessageAsync(Context, embed.Build());

        }

        [Command("+fav"), Alias("addfav")]
        public async Task AddFav(string species) {

            await AddFav("", species);

        }
        [Command("+fav"), Alias("addfav")]
        public async Task AddFav(string genus, string species) {

            // Get the requested species.

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            // Add this species to the user's favorites list.

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Favorites(user_id, species_id) VALUES($user_id, $species_id);")) {

                cmd.Parameters.AddWithValue("$user_id", Context.User.Id);
                cmd.Parameters.AddWithValue("$species_id", sp.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully added **{0}** to **{1}**'s favorites list.", sp.GetShortName(), Context.User.Username));

        }
        [Command("-fav")]
        public async Task MinusFav(string species) {

            await MinusFav("", species);

        }
        [Command("-fav")]
        public async Task MinusFav(string genus, string species) {

            // Get the requested species.

            Species sp = await BotUtils.ReplyFindSpeciesAsync(Context, genus, species);

            if (sp is null)
                return;

            // Remove this species from the user's favorites list.

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Favorites WHERE user_id = $user_id AND species_id = $species_id;")) {

                cmd.Parameters.AddWithValue("$user_id", Context.User.Id);
                cmd.Parameters.AddWithValue("$species_id", sp.id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully removed **{0}** from **{1}**'s favorites list.", sp.GetShortName(), Context.User.Username));

        }
        [Command("favs"), Alias("fav", "favorites", "favourites")]
        public async Task Favs() {

            // Get all species fav'd by this user.

            List<string> lines = new List<string>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM Favorites WHERE user_id = $user_id);")) {

                cmd.Parameters.AddWithValue("$user_id", Context.User.Id);

                using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                    foreach (DataRow row in rows.Rows) {

                        Species sp = await Species.FromDataRow(row);
                        long fav_count = 0;

                        // Get the number of times this species has been favorited.

                        using (SQLiteCommand cmd2 = new SQLiteCommand("SELECT COUNT(*) FROM Favorites WHERE species_id = $species_id;")) {

                            cmd2.Parameters.AddWithValue("$species_id", sp.id);

                            fav_count = await Database.GetScalar<long>(cmd2);

                        }

                        lines.Add(sp.GetShortName() + (fav_count > 1 ? string.Format(" ({0} favs)", fav_count) : ""));

                    }

                    lines.Sort();

                }

            }

            // Display the species list.

            if (lines.Count() <= 0) {

                await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** has not favorited any species.", Context.User.Username));

            }
            else {

                PaginatedMessage embed = new PaginatedMessage(EmbedUtils.LinesToEmbedPages(lines));
                embed.SetTitle(string.Format("⭐ Species favorited by {0} ({1})", Context.User.Username, lines.Count()));
                embed.SetThumbnailUrl(Context.User.GetAvatarUrl(size: 32));
                embed.AddPageNumbers();

                await CommandUtils.SendMessageAsync(Context, embed.Build());

            }

        }

    }

}