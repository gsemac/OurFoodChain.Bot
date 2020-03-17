using Discord;
using Discord.Commands;
using OurFoodChain.Common;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Trophies;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OurFoodChain.Trophies.Extensions;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Utilities;
using OurFoodChain.Extensions;
using OurFoodChain.Discord.Messaging;

namespace OurFoodChain.Modules {

    public class UserModule :
        OfcModuleBase {

        // Public members

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

            ICreator userInfo = await Db.GetCreatorAsync(user.ToCreator(), UserInfoQueryFlags.MatchEither);

            if (userInfo is null) {

                embed.WithDescription(string.Format("{0} has not submitted any species.", user.Username));

            }
            else {

                long daysSinceFirstSubmission = (DateUtilities.GetCurrentDateUtc() - userInfo.FirstSpeciesDate).Value.Days;
                UserRank userRank = await Db.GetRankAsync(userInfo, UserInfoQueryFlags.MatchEither);

                // Get the user's most active genus.

                IEnumerable<ISpecies> userSpecies = await Db.GetSpeciesAsync(userInfo, UserInfoQueryFlags.MatchEither);

                IGrouping<string, string> favoriteGenusGrouping = userSpecies
                    .Select(x => x.Genus.GetName())
                    .GroupBy(x => x)
                    .OrderByDescending(x => x.Count())
                    .FirstOrDefault();

                string favoriteGenus = favoriteGenusGrouping is null ? "N/A" : favoriteGenusGrouping.First();
                int favoriteGenusCount = favoriteGenusGrouping is null ? 0 : favoriteGenusGrouping.Count();

                int userSpeciesCount = userSpecies.Count();
                int speciesCount = (int)await Db.GetSpeciesCountAsync();

                // Get the user's rarest trophy.

                string rarest_trophy = "N/A";

                IUnlockedTrophyInfo[] unlocked = (await Db.GetUnlockedTrophiesAsync(new Creator(user.Id, user.Username), TrophyService.GetTrophies())).ToArray();

                if (unlocked.Count() > 0) {

                    Array.Sort(unlocked, (lhs, rhs) => lhs.TimesUnlocked.CompareTo(rhs.TimesUnlocked));

                    ITrophy trophy = TrophyService.GetTrophies()
                        .Where(t => t.Identifier.Equals(unlocked[0].Trophy.Identifier))
                        .FirstOrDefault();

                    rarest_trophy = trophy.Name;

                }

                // Put together the user's profile.

                if (Config.GenerationsEnabled) {

                    int generationsSinceFirstSubmission = (await Db.GetGenerationsAsync()).Where(x => x.EndDate > userInfo.FirstSpeciesDate).Count();
                    double speciesPerGeneration = generationsSinceFirstSubmission <= 0 ? userSpeciesCount : (double)userSpeciesCount / generationsSinceFirstSubmission;

                    embed.WithDescription(string.Format("{0} made their first species during **{1}**.\nSince then, they have submitted **{2:0.0}** species per generation.\n\nTheir submissions make up **{3:0.0}%** of all species.",
                        user.Username,
                        await GetDateStringAsync(userInfo.FirstSpeciesDate),
                        speciesPerGeneration,
                        (double)userSpeciesCount / speciesCount * 100.0));

                }
                else {

                    embed.WithDescription(string.Format("{0} made their first species on **{1}**.\nSince then, they have submitted **{2:0.0}** species per day.\n\nTheir submissions make up **{3:0.0}%** of all species.",
                        user.Username,
                        await GetDateStringAsync(userInfo.FirstSpeciesDate),
                        daysSinceFirstSubmission == 0 ? userSpeciesCount : (double)userSpeciesCount / daysSinceFirstSubmission,
                        (double)userSpeciesCount / speciesCount * 100.0));

                }

                embed.AddField("Species", string.Format("{0} (Rank **#{1}**)", userSpeciesCount, userRank.Rank), inline: true);

                embed.AddField("Favorite genus", string.Format("{0} ({1} spp.)", StringUtilities.ToTitleCase(favoriteGenus), favoriteGenusCount), inline: true);

                if (Config.TrophiesEnabled) {

                    embed.AddField("Trophies", string.Format("{0} ({1:0.0}%)",
                        (await Db.GetUnlockedTrophiesAsync(new Creator(user.Id, user.Username), TrophyService.GetTrophies())).Count(),
                        await Db.GetTrophyCompletionRateAsync(new Creator(user.Id, user.Username), TrophyService.GetTrophies())), inline: true);

                    embed.AddField("Rarest trophy", rarest_trophy, inline: true);

                }

            }

            await ReplyAsync("", false, embed.Build());

        }
        [Command("leaderboard")]
        public async Task Leaderboard() {

            IEnumerable<UserRank> userRanks = await Db.GetRanksAsync();

            ILeaderboard leaderboard = new Leaderboard();

            foreach (UserRank userRank in userRanks) {

                IUser user = Context.Guild is null ? null : await Context.Guild.GetUserAsync(userRank.User.Id);

                leaderboard.Add(user?.Username ?? userRank.User.Username, userRank.User.SubmissionCount);

            }

            await ReplyLeaderboardAsync(leaderboard);

        }

        [Command("+fav"), Alias("addfav")]
        public async Task AddFav(string species) {

            await AddFav("", species);

        }
        [Command("+fav"), Alias("addfav")]
        public async Task AddFav(string genus, string species) {

            // Get the requested species.

            ISpecies sp = await GetSpeciesOrReplyAsync(genus, species);

            if (!sp.IsValid())
                return;

            // Add this species to the user's favorites list.

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Favorites(user_id, species_id) VALUES($user_id, $species_id);")) {

                cmd.Parameters.AddWithValue("$user_id", Context.User.Id);
                cmd.Parameters.AddWithValue("$species_id", sp.Id);

                await Db.ExecuteNonQueryAsync(cmd);

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

            ISpecies sp = await GetSpeciesOrReplyAsync(genus, species);

            if (!sp.IsValid())
                return;

            // Remove this species from the user's favorites list.

            using (SQLiteCommand cmd = new SQLiteCommand("DELETE FROM Favorites WHERE user_id = $user_id AND species_id = $species_id;")) {

                cmd.Parameters.AddWithValue("$user_id", Context.User.Id);
                cmd.Parameters.AddWithValue("$species_id", sp.Id);

                await Db.ExecuteNonQueryAsync(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully removed **{0}** from **{1}**'s favorites list.", sp.GetShortName(), Context.User.Username));

        }
        [Command("favs"), Alias("fav", "favorites", "favourites")]
        public async Task Favs() {

            // Get all species fav'd by this user.

            List<string> lines = new List<string>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM Favorites WHERE user_id = $user_id);")) {

                cmd.Parameters.AddWithValue("$user_id", Context.User.Id);

                foreach (DataRow row in await Db.GetRowsAsync(cmd)) {

                    ISpecies sp = await Db.CreateSpeciesFromDataRowAsync(row);
                    long fav_count = 0;

                    // Get the number of times this species has been favorited.

                    using (SQLiteCommand cmd2 = new SQLiteCommand("SELECT COUNT(*) FROM Favorites WHERE species_id = $species_id;")) {

                        cmd2.Parameters.AddWithValue("$species_id", sp.Id);

                        fav_count = await Db.GetScalarAsync<long>(cmd2);

                    }

                    lines.Add(sp.GetShortName() + (fav_count > 1 ? string.Format(" (+{0})", fav_count) : ""));

                }

                lines.Sort();

            }

            // Display the species list.

            if (lines.Count() <= 0) {

                await BotUtils.ReplyAsync_Info(Context, string.Format("**{0}** has not favorited any species.", Context.User.Username));

            }
            else {

                IPaginatedMessage message = new PaginatedMessage();

                message.AddLines(lines);
                message.SetTitle($"⭐ Species favorited by {Context.User.Username} ({lines.Count()})");
                message.SetThumbnailUrl(Context.User.GetAvatarUrl(size: 32));
                message.AddPageNumbers();

                await ReplyAsync(message);

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

        // Private members

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

    }

}