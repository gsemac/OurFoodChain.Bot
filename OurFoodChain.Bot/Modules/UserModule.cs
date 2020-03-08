using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Modules {

    public class UserModule :
        OfcModuleBase {

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

                long daysSinceFirstSubmission = (DateUtilities.GetCurrentTimestampUtc() - userInfo.FirstSubmissionTimestamp) / 60 / 60 / 24;
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

                IUnlockedTrophyInfo[] unlocked = (await Db.GetUnlockedTrophiesAsync(new Creator(user.Id, user.Username), TrophyScanner.GetTrophies())).ToArray();

                if (unlocked.Count() > 0) {

                    Array.Sort(unlocked, (lhs, rhs) => lhs.TimesUnlocked.CompareTo(rhs.TimesUnlocked));

                    ITrophy trophy = TrophyScanner.GetTrophies()
                        .Where(t => t.Identifier.Equals(unlocked[0].Trophy.Identifier))
                        .FirstOrDefault();

                    rarest_trophy = trophy.Name;

                }

                // Put together the user's profile.

                if (BotConfiguration.GenerationsEnabled) {

                    int generationsSinceFirstSubmission = (await GenerationUtils.GetGenerationsAsync()).Where(x => x.EndTimestamp > userInfo.FirstSubmissionTimestamp).Count();
                    double speciesPerGeneration = generationsSinceFirstSubmission <= 0 ? userSpeciesCount : (double)userSpeciesCount / generationsSinceFirstSubmission;

                    embed.WithDescription(string.Format("{0} made their first species during **{1}**.\nSince then, they have submitted **{2:0.0}** species per generation.\n\nTheir submissions make up **{3:0.0}%** of all species.",
                        user.Username,
                        await BotUtils.TimestampToDateStringAsync(userInfo.FirstSubmissionTimestamp, new OfcBotContext(Context, BotConfiguration, Db)),
                        speciesPerGeneration,
                        (double)userSpeciesCount / speciesCount * 100.0));

                }
                else {

                    embed.WithDescription(string.Format("{0} made their first species on **{1}**.\nSince then, they have submitted **{2:0.0}** species per day.\n\nTheir submissions make up **{3:0.0}%** of all species.",
                        user.Username,
                        await BotUtils.TimestampToDateStringAsync(userInfo.FirstSubmissionTimestamp, new OfcBotContext(Context, BotConfiguration, Db)),
                        daysSinceFirstSubmission == 0 ? userSpeciesCount : (double)userSpeciesCount / daysSinceFirstSubmission,
                        (double)userSpeciesCount / speciesCount * 100.0));

                }

                embed.AddField("Species", string.Format("{0} (Rank **#{1}**)", userSpeciesCount, userRank.Rank), inline: true);

                embed.AddField("Favorite genus", string.Format("{0} ({1} spp.)", StringUtilities.ToTitleCase(favoriteGenus), favoriteGenusCount), inline: true);

                if (BotConfiguration.TrophiesEnabled) {

                    embed.AddField("Trophies", string.Format("{0} ({1:0.0}%)",
                        (await Db.GetUnlockedTrophiesAsync(new Creator(user.Id, user.Username), TrophyScanner.GetTrophies())).Count(),
                        await Db.GetTrophyCompletionRateAsync(new Creator(user.Id, user.Username), TrophyScanner.GetTrophies())), inline: true);

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
