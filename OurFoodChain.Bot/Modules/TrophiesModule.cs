using Discord;
using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Trophies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Modules {

    public class TrophiesModule :
        ModuleBase {

        public IOurFoodChainBotConfiguration BotConfiguration { get; set; }

        [Command("trophies"), Alias("achievements")]
        public async Task Trophies(IUser user = null) {

            if (user is null)
                user = Context.User;

            UnlockedTrophyInfo[] unlocked = await Global.TrophyRegistry.GetUnlockedTrophiesAsync(user.Id);

            Array.Sort(unlocked, (x, y) => x.timestamp.CompareTo(y.timestamp));

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(string.Format("{0}'s Trophies ({1:0.#}%)", user.Username, await Global.TrophyRegistry.GetUserCompletionRateAsync(user.Id)));
            embed.WithColor(new Color(255, 204, 77));

            StringBuilder description_builder = new StringBuilder();

            description_builder.AppendLine(string.Format("See a list of all available trophies with `{0}trophylist`.", BotConfiguration.Prefix));

            foreach (UnlockedTrophyInfo info in unlocked) {

                Trophy trophy = await Global.TrophyRegistry.GetTrophyByIdentifierAsync(info.identifier);

                if (trophy is null)
                    continue;

                description_builder.AppendLine(string.Format("{0} **{1}** - Earned {2} ({3:0.#}%)",
                   trophy.GetIcon(),
                   trophy.GetName(),
                   DateUtils.TimestampToShortDateString(info.timestamp),
                   await Global.TrophyRegistry.GetCompletionRateAsync(trophy)
                  ));

            }

            embed.WithDescription(description_builder.ToString());

            await ReplyAsync("", false, embed.Build());

        }

        [Command("trophylist"), Alias("achievementlist")]
        public async Task TrophyList() {

            int total_trophies = (await Global.TrophyRegistry.GetTrophiesAsync()).Count;
            int trophies_per_page = 8;
            int total_pages = (int)Math.Ceiling((float)total_trophies / trophies_per_page);
            int current_page = 0;
            int current_page_trophy_count = 0;

            Bot.PaginatedMessage message = new Bot.PaginatedMessage();
            EmbedBuilder embed = null;

            IReadOnlyCollection<Trophy> trophy_list = await Global.TrophyRegistry.GetTrophiesAsync();

            foreach (Trophy trophy in trophy_list) {

                if (current_page_trophy_count == 0) {

                    ++current_page;

                    embed = new EmbedBuilder();
                    embed.WithTitle(string.Format("All Trophies ({0})", (await Global.TrophyRegistry.GetTrophiesAsync()).Count));
                    embed.WithDescription(string.Format("For more details about a trophy, use `?trophy <name>` (e.g. `{0}trophy \"{1}\"`).",
                        BotConfiguration.Prefix,
                        trophy_list.First().GetName()));
                    embed.WithFooter(string.Format("Page {0} of {1}", current_page, total_pages));
                    embed.WithColor(new Color(255, 204, 77));

                }

                double completion_rate = await Global.TrophyRegistry.GetCompletionRateAsync(trophy);
                string description = (trophy.Flags.HasFlag(TrophyFlags.Hidden) && completion_rate <= 0.0) ? string.Format("_{0}_", OurFoodChain.Trophies.Trophy.HIDDEN_TROPHY_DESCRIPTION) : trophy.GetDescription();

                // If this was a first-time trophy, show who unlocked it.

                if (trophy.Flags.HasFlag(TrophyFlags.OneTime) && completion_rate > 0.0) {

                    TrophyUser[] user_ids = await Global.TrophyRegistry.GetUsersUnlockedAsync(trophy);

                    if (user_ids.Count() > 0 && !(Context.Guild is null)) {

                        IGuildUser user = await Context.Guild.GetUserAsync(user_ids.First().UserId);

                        if (!(user is null))
                            description += string.Format(" (unlocked by {0})", user.Mention);

                    }

                }

                embed.AddField(string.Format("{0} **{1}** ({2:0.#}%)", trophy.GetIcon(), trophy.name, completion_rate), description);

                ++current_page_trophy_count;

                if (current_page_trophy_count >= trophies_per_page) {

                    message.Pages.Add(embed.Build());

                    current_page_trophy_count = 0;

                }

            }

            // Add the last embed to the message.
            if (!(embed is null))
                message.Pages.Add(embed.Build());

            await Bot.DiscordUtils.SendMessageAsync(Context, message);

        }

        [Command("trophy"), Alias("achievement")]
        public async Task Trophy(string name) {

            // Find the trophy with this name.
            Trophy trophy = await Global.TrophyRegistry.GetTrophyByNameAsync(name);

            // If no such trophy exists, return an error.

            if (trophy is null) {

                await BotUtils.ReplyAsync_Error(Context, "No such trophy exists.");

                return;

            }

            // Show trophy information.

            double completion_rate = await Global.TrophyRegistry.GetCompletionRateAsync(trophy);
            bool hide_description = trophy.Flags.HasFlag(TrophyFlags.Hidden) && completion_rate <= 0.0;

            string embed_title = string.Format("{0} {1} ({2:0.#}%)", trophy.GetIcon(), trophy.GetName(), completion_rate);
            string embed_description = string.Format("_{0}_", hide_description ? OurFoodChain.Trophies.Trophy.HIDDEN_TROPHY_DESCRIPTION : trophy.GetDescription());
            long times_unlocked = await Global.TrophyRegistry.GetTimesUnlockedAsync(trophy);

            embed_description += string.Format("\n\nThis trophy has been earned by **{0}** user{1} ({2:0.#}%).",
                times_unlocked,
                times_unlocked == 1 ? "" : "s",
                completion_rate);

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(embed_title);
            embed.WithDescription(embed_description);
            embed.WithColor(new Color(255, 204, 77));

            // Show first/latest earners.

            TrophyUser[] earners = (await Global.TrophyRegistry.GetUsersUnlockedAsync(trophy)).OrderBy(x => x.EarnedTimestamp).ToArray();
            string date_format = "MMMM dd, yyyy";

            if (!(Context.Guild is null)) {

                foreach (TrophyUser trophy_user in earners) {

                    IUser user = await Context.Guild.GetUserAsync(trophy_user.UserId);

                    if (!(user is null)) {

                        embed.AddField("First earned", string.Format("**{0}** ({1})", user.Username, trophy_user.EarnedDate.ToString(date_format)), inline: true);

                        break;

                    }

                }

                foreach (TrophyUser trophy_user in earners.Reverse()) {

                    IUser user = await Context.Guild.GetUserAsync(trophy_user.UserId);

                    if (!(user is null)) {

                        embed.AddField("Latest earned", string.Format("**{0}** ({1})", user.Username, trophy_user.EarnedDate.ToString(date_format)), inline: true);

                        break;

                    }

                }

            }

            await ReplyAsync("", false, embed.Build());

        }

        [Command("awardtrophy"), Alias("award", "awardachievement"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AwardTrophy(IGuildUser user, string trophy) {

            Trophy t = await Global.TrophyRegistry.GetTrophyByNameAsync(trophy);

            if (t is null) {

                await BotUtils.ReplyAsync_Error(Context, "No such trophy exists.");

                return;

            }

            // #todo Show warning and do nothing if the user already has the trophy

            await Global.TrophyRegistry.UnlockAsync(user.Id, t);

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully awarded **{0}** trophy to {1}.", t.GetName(), user.Mention));

        }
        [Command("scantrophies"), Alias("trophyscan")]
        public async Task ScanTrophies(IGuildUser user = null) {

            if (user is null)
                user = (IGuildUser)Context.User;
            else if (!await BotUtils.ReplyHasPrivilegeAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator)) // Mod privileges are required to scan someone else's trophies
                return;

            if (BotConfiguration.TrophiesEnabled)
                await Global.TrophyScanner.AddToQueueAsync(Context, user.Id, TrophyScanner.NO_DELAY);

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully added user **{0}** to the trophy scanner queue.", user.Username));

        }

    }

}