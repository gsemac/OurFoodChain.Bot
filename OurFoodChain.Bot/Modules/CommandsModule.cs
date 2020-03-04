using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using OurFoodChain.Adapters;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common.Collections;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Modules {

    public class CommandsModule :
        ModuleBase {

        public IOfcBotConfiguration BotConfiguration { get; set; }
        public SQLiteDatabase Db { get; set; }

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
                    TimestampFormatter = x => BotUtils.TimestampToDateStringAsync(x, BotConfiguration, TimestampToDateStringFormat.Short).Result
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
                    TimestampFormatter = x => BotUtils.TimestampToDateStringAsync(x, BotConfiguration, TimestampToDateStringFormat.Short).Result
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

                    DateTimeOffset? ts = i == 0 ? DateUtilities.TimestampToDate(species.Timestamp) : zone_groups[i].First().Date;

                    if (!ts.HasValue)
                        ts = DateUtilities.TimestampToDate(species.Timestamp);

                    result.Append(string.Format("{0} - ", await BotUtils.TimestampToDateStringAsync(ts.Value.ToUnixTimeSeconds(), BotConfiguration, TimestampToDateStringFormat.Short)));
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

            SpeciesSizeMatch match = SpeciesSizeMatch.Match(species.Description);

            // Output the result.

            EmbedBuilder embed = new EmbedBuilder();
            embed.Title = string.Format("Size of {0}", species.FullName);
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

                if (BotConfiguration.GenerationsEnabled) {

                    int generationsSinceFirstSubmission = (await GenerationUtils.GetGenerationsAsync()).Where(x => x.EndTimestamp > userInfo.FirstSubmissionTimestamp).Count();
                    double speciesPerGeneration = generationsSinceFirstSubmission <= 0 ? userSpeciesCount : (double)userSpeciesCount / generationsSinceFirstSubmission;

                    embed.WithDescription(string.Format("{0} made their first species during **{1}**.\nSince then, they have submitted **{2:0.0}** species per generation.\n\nTheir submissions make up **{3:0.0}%** of all species.",
                        user.Username,
                        await BotUtils.TimestampToDateStringAsync(userInfo.FirstSubmissionTimestamp, BotConfiguration),
                        speciesPerGeneration,
                        (double)userSpeciesCount / speciesCount * 100.0));

                }
                else {

                    embed.WithDescription(string.Format("{0} made their first species on **{1}**.\nSince then, they have submitted **{2:0.0}** species per day.\n\nTheir submissions make up **{3:0.0}%** of all species.",
                        user.Username,
                        await BotUtils.TimestampToDateStringAsync(userInfo.FirstSubmissionTimestamp, BotConfiguration),
                        daysSinceFirstSubmission == 0 ? userSpeciesCount : (double)userSpeciesCount / daysSinceFirstSubmission,
                        (double)userSpeciesCount / speciesCount * 100.0));

                }

                embed.AddField("Species", string.Format("{0} (Rank **#{1}**)", userSpeciesCount, userRank.Rank), inline: true);

                embed.AddField("Favorite genus", string.Format("{0} ({1} spp.)", StringUtilities.ToTitleCase(favoriteGenus), favoriteGenusCount), inline: true);

                if (BotConfiguration.TrophiesEnabled) {

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

            Bot.PaginatedMessageBuilder embed = new Bot.PaginatedMessageBuilder();
            embed.AddPages(EmbedUtils.LinesToEmbedPages(lines));
            embed.SetTitle(string.Format("🏆 Leaderboard ({0})", lines.Count));
            embed.SetColor(255, 204, 77);
            embed.AddPageNumbers();

            // Send the embed.
            await Bot.DiscordUtils.SendMessageAsync(Context, embed.Build());

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
                cmd.Parameters.AddWithValue("$species_id", sp.Id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully added **{0}** to **{1}**'s favorites list.", sp.ShortName, Context.User.Username));

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
                cmd.Parameters.AddWithValue("$species_id", sp.Id);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully removed **{0}** from **{1}**'s favorites list.", sp.ShortName, Context.User.Username));

        }
        [Command("favs"), Alias("fav", "favorites", "favourites")]
        public async Task Favs() {

            // Get all species fav'd by this user.

            List<string> lines = new List<string>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM Favorites WHERE user_id = $user_id);")) {

                cmd.Parameters.AddWithValue("$user_id", Context.User.Id);

                using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                    foreach (DataRow row in rows.Rows) {

                        Species sp = await SpeciesUtils.SpeciesFromDataRow(row);
                        long fav_count = 0;

                        // Get the number of times this species has been favorited.

                        using (SQLiteCommand cmd2 = new SQLiteCommand("SELECT COUNT(*) FROM Favorites WHERE species_id = $species_id;")) {

                            cmd2.Parameters.AddWithValue("$species_id", sp.Id);

                            fav_count = await Database.GetScalar<long>(cmd2);

                        }

                        lines.Add(sp.ShortName + (fav_count > 1 ? string.Format(" (+{0})", fav_count) : ""));

                    }

                    lines.Sort();

                }

            }

            // Display the species list.

            if (lines.Count() <= 0) {

                await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** has not favorited any species.", Context.User.Username));

            }
            else {

                Bot.PaginatedMessageBuilder embed = new Bot.PaginatedMessageBuilder(EmbedUtils.LinesToEmbedPages(lines));
                embed.SetTitle(string.Format("⭐ Species favorited by {0} ({1})", Context.User.Username, lines.Count()));
                embed.SetThumbnailUrl(Context.User.GetAvatarUrl(size: 32));
                embed.AddPageNumbers();

                await Bot.DiscordUtils.SendMessageAsync(Context, embed.Build());

            }

        }

    }

}