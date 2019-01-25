using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.trophies {

    public class TrophyCommands :
        ModuleBase {

        [Command("trophies"), Alias("achievements")]
        public async Task Trophies(IUser user = null) {

            if (user is null)
                user = Context.User;

            UnlockedTrophyInfo[] unlocked = await TrophyRegistry.GetUnlockedTrophiesAsync(user.Id);

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(string.Format("{0}'s Trophies ({1:0.##}%)", user.Username, 100.0 * unlocked.Count() / (await TrophyRegistry.GetTrophiesAsync()).Count));
            embed.WithFooter(string.Format("See a list of all available trophies with the \"{0}trophylist\" command.", OurFoodChainBot.GetInstance().GetConfig().prefix));
            embed.WithColor(new Color(255, 204, 77));

            StringBuilder description_builder = new StringBuilder();
            int total_users = (await Context.Guild.GetUsersAsync()).Count;

            foreach (UnlockedTrophyInfo info in unlocked) {

                Trophy trophy = await TrophyRegistry.GetTrophyByIdentifierAsync(info.identifier);

                if (trophy is null)
                    continue;

                description_builder.AppendLine(string.Format("🏆 **{0}** - Earned {1} ({2:0.##}%)",
                   trophy.GetName(),
                   BotUtils.GetTimeStampAsDateString(info.timestamp),
                   100.0 * (double)info.timesUnlocked / total_users
                  ));

            }

            embed.WithDescription(description_builder.ToString());

            await ReplyAsync("", false, embed.Build());

        }

        [Command("trophylist"), Alias("achievementlist")]
        public async Task TrophyList() {

            int total_users = (await Context.Guild.GetUsersAsync()).Count;
            int total_trophies = (await TrophyRegistry.GetTrophiesAsync()).Count;
            int trophies_per_page = 8;
            int total_pages = (int)Math.Ceiling((float)total_trophies / trophies_per_page);
            int current_page = 0;
            int current_page_trophy_count = 0;

            CommandUtils.PaginatedMessage message = new CommandUtils.PaginatedMessage();
            EmbedBuilder embed = null;

            foreach (Trophy trophy in await TrophyRegistry.GetTrophiesAsync()) {

                if (current_page_trophy_count == 0) {

                    ++current_page;

                    embed = new EmbedBuilder();
                    embed.WithTitle(string.Format("All Trophies ({0})", (await TrophyRegistry.GetTrophiesAsync()).Count));
                    embed.WithFooter(string.Format("Page {0} of {1} — Want to know more about a trophy? Use the \"{2}trophy\" command, e.g.: {2}trophy \"polar power\"", current_page, total_pages, OurFoodChainBot.GetInstance().GetConfig().prefix));
                    embed.WithColor(new Color(255, 204, 77));

                }

                long times_unlocked = await TrophyRegistry.GetTimesUnlocked(trophy);
                string description = trophy.Flags.HasFlag(TrophyFlags.Hidden) ? "_This is a hidden trophy. Unlock it for details!_" : trophy.GetDescription();
                string icon = "🏆";

                if (trophy.Flags.HasFlag(TrophyFlags.OneTime))
                    icon = "🥇";
                else if (trophy.Flags.HasFlag(TrophyFlags.Hidden))
                    icon = "❓";

                embed.AddField(string.Format("{0} **{1}** ({2:0.##}%)", icon, trophy.name, 100.0 * times_unlocked / total_users), description);

                ++current_page_trophy_count;

                if (current_page_trophy_count >= trophies_per_page) {

                    message.pages.Add(embed.Build());

                    current_page_trophy_count = 0;

                }

            }

            // Add the last embed to the message.
            if (!(embed is null))
                message.pages.Add(embed.Build());

            await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, message);

        }

        [Command("trophy"), Alias("achievement")]
        public async Task Trophy(string name) {

            // Find the trophy with this name.

            Trophy trophy = null;

            foreach (Trophy t in await TrophyRegistry.GetTrophiesAsync())
                if (t.GetName().ToLower() == name.ToLower()) {

                    trophy = t;

                    break;

                }

            // If no such trophy exists, return an error.

            if (trophy is null) {

                await BotUtils.ReplyAsync_Error(Context, "No such trophy exists.");

                return;

            }

            // Show trophy information.

            int total_users = (await Context.Guild.GetUsersAsync()).Count;
            long times_unlocked = await TrophyRegistry.GetTimesUnlocked(trophy);

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(string.Format("🏆 {0} ({1:0.##}%)", trophy.GetName(), 100.0 * times_unlocked / total_users));
            embed.WithDescription(trophy.Flags.HasFlag(TrophyFlags.Hidden) && times_unlocked <= 0 ? "This is a secret trophy." : trophy.GetDescription());
            embed.WithColor(new Color(255, 204, 77));

            await ReplyAsync("", false, embed.Build());

        }

        [Command("awardtrophy"), Alias("award", "awardachievement")]
        public async Task AwardTrophy(IGuildUser user, string trophy) {

            if (!await BotUtils.ReplyAsync_CheckPrivilege(Context, user, PrivilegeLevel.Moderator))
                return;

            Trophy t = await TrophyRegistry.GetTrophyByNameAsync(trophy);

            if (trophy is null) {

                await BotUtils.ReplyAsync_Error(Context, "No such trophy exists.");

                return;

            }

            await TrophyRegistry.SetUnlocked(user.Id, t);

        }
        [Command("scantrophies")]
        public async Task ScanTrophies(IGuildUser user) {

            if (!await BotUtils.ReplyAsync_CheckPrivilege(Context, user, PrivilegeLevel.Moderator))
                return;

            await TrophyScanner.AddToQueueAsync(Context, user.Id);

        }

    }

}