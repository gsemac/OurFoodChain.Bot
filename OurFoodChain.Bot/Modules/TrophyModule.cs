using Discord;
using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common;
using OurFoodChain.Data;
using OurFoodChain.Services;
using OurFoodChain.Trophies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OurFoodChain.Trophies.Extensions;
using OurFoodChain.Common.Utilities;

namespace OurFoodChain.Bot.Modules {

    public class TrophyModule :
        ModuleBase {

        public IOfcBotConfiguration BotConfiguration { get; set; }
        public OurFoodChain.Services.TrophyScanner TrophyScanner { get; set; }
        public SQLiteDatabase Db { get; set; }

        [Command("trophies"), Alias("achievements")]
        public async Task Trophies(IUser user = null) {

            if (user is null)
                user = Context.User;

            ICreator creator = new Creator(user.Id, user.Username);
            IUnlockedTrophyInfo[] unlocked = (await Db.GetUnlockedTrophiesAsync(creator, TrophyScanner.GetTrophies())).ToArray();

            Array.Sort(unlocked, (x, y) => x.DateUnlocked.CompareTo(y.DateUnlocked));

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(string.Format("{0}'s Trophies ({1:0.#}%)", user.Username, await Db.GetTrophyCompletionRateAsync(creator, TrophyScanner.GetTrophies())));
            embed.WithColor(new Color(255, 204, 77));

            StringBuilder description_builder = new StringBuilder();

            description_builder.AppendLine(string.Format("See a list of all available trophies with `{0}trophylist`.", BotConfiguration.Prefix));

            foreach (IUnlockedTrophyInfo info in unlocked) {

                ITrophy trophy = info.Trophy;

                if (trophy is null)
                    continue;

                description_builder.AppendLine(string.Format("{0} **{1}** - Earned {2} ({3:0.#}%)",
                   trophy.Icon,
                   trophy.Name,
                   DateUtils.TimestampToShortDateString(DateUtilities.DateToTimestamp(info.DateUnlocked)),
                   await Db.GetTrophyCompletionRateAsync(trophy)
                  ));

            }

            embed.WithDescription(description_builder.ToString());

            await ReplyAsync("", false, embed.Build());

        }

        [Command("trophylist"), Alias("achievementlist")]
        public async Task TrophyList() {

            int total_trophies = TrophyScanner.GetTrophies().Count();
            int trophies_per_page = 8;
            int total_pages = (int)Math.Ceiling((float)total_trophies / trophies_per_page);
            int current_page = 0;
            int current_page_trophy_count = 0;

            Bot.PaginatedMessage message = new Bot.PaginatedMessage();
            EmbedBuilder embed = null;

            IEnumerable<ITrophy> trophy_list = TrophyScanner.GetTrophies();

            foreach (ITrophy trophy in trophy_list) {

                if (current_page_trophy_count == 0) {

                    ++current_page;

                    embed = new EmbedBuilder();
                    embed.WithTitle(string.Format("All Trophies ({0})", TrophyScanner.GetTrophies().Count()));
                    embed.WithDescription(string.Format("For more details about a trophy, use `?trophy <name>` (e.g. `{0}trophy \"{1}\"`).",
                        BotConfiguration.Prefix,
                        trophy_list.First().Name));
                    embed.WithFooter(string.Format("Page {0} of {1}", current_page, total_pages));
                    embed.WithColor(new Color(255, 204, 77));

                }

                double completion_rate = await Db.GetTrophyCompletionRateAsync(trophy);
                string description = (trophy.Flags.HasFlag(TrophyFlags.Hidden) && completion_rate <= 0.0) ? string.Format("_{0}_", TrophyBase.HiddenTrophyDescription) : trophy.Description;

                // If this was a first-time trophy, show who unlocked it.

                if (trophy.Flags.HasFlag(TrophyFlags.OneTime) && completion_rate > 0.0) {

                    IEnumerable<IUnlockedTrophyInfo> user_ids = await Db.GetCreatorsWithTrophyAsync(trophy);

                    if (user_ids.Count() > 0 && Context.Guild != null) {

                        IGuildUser user = await Context.Guild.GetUserAsync(user_ids.First().Creator.UserId.Value);

                        if (user != null)
                            description += string.Format(" (unlocked by {0})", user.Mention);

                    }

                }

                embed.AddField(string.Format("{0} **{1}** ({2:0.#}%)", trophy.Icon, trophy.Name, completion_rate), description);

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

            ITrophy trophy = TrophyScanner.GetTrophies()
                .Where(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            // If no such trophy exists, return an error.

            if (trophy is null) {

                await BotUtils.ReplyAsync_Error(Context, "No such trophy exists.");

                return;

            }

            // Show trophy information.

            double completion_rate = await Db.GetTrophyCompletionRateAsync(trophy);
            bool hide_description = trophy.Flags.HasFlag(TrophyFlags.Hidden) && completion_rate <= 0.0;

            string embed_title = string.Format("{0} {1} ({2:0.#}%)", trophy.Icon, trophy.Name, completion_rate);
            string embed_description = string.Format("_{0}_", hide_description ? TrophyBase.HiddenTrophyDescription : trophy.Description);
            long times_unlocked = await Db.GetTimesTrophyUnlockedAsync(trophy);

            embed_description += string.Format("\n\nThis trophy has been earned by **{0}** user{1} ({2:0.#}%).",
                times_unlocked,
                times_unlocked == 1 ? "" : "s",
                completion_rate);

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(embed_title);
            embed.WithDescription(embed_description);
            embed.WithColor(new Color(255, 204, 77));

            // Show first/latest earners.

            IEnumerable<IUnlockedTrophyInfo> earners = (await Db.GetCreatorsWithTrophyAsync(trophy)).OrderBy(x => x.DateUnlocked);
            string date_format = "MMMM dd, yyyy";

            if (Context.Guild != null) {

                foreach (IUnlockedTrophyInfo trophy_user in earners) {

                    IUser user = await Context.Guild.GetUserAsync(trophy_user.Creator.UserId.Value);

                    if (!(user is null)) {

                        embed.AddField("First earned", string.Format("**{0}** ({1})", user.Username, trophy_user.DateUnlocked.ToString(date_format)), inline: true);

                        break;

                    }

                }

                foreach (IUnlockedTrophyInfo trophy_user in earners.Reverse()) {

                    IUser user = await Context.Guild.GetUserAsync(trophy_user.Creator.UserId.Value);

                    if (!(user is null)) {

                        embed.AddField("Latest earned", string.Format("**{0}** ({1})", user.Username, trophy_user.DateUnlocked.ToString(date_format)), inline: true);

                        break;

                    }

                }

            }

            await ReplyAsync("", false, embed.Build());

        }

        [Command("awardtrophy"), Alias("award", "awardachievement"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AwardTrophy(IGuildUser user, string trophyName) {

            ITrophy trophy = TrophyScanner.GetTrophies()
                .Where(t => t.Name.Equals(trophyName, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (trophy is null) {

                await BotUtils.ReplyAsync_Error(Context, "No such trophy exists.");

                return;

            }

            // #todo Show warning and do nothing if the user already has the trophy

            await Db.UnlockTrophyAsync(new Creator(user.Id, user.Username), trophy);

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully awarded **{0}** trophy to {1}.", trophy.Name, user.Mention));

        }
        [Command("scantrophies"), Alias("trophyscan")]
        public async Task ScanTrophies(IGuildUser user = null) {

            if (user is null)
                user = (IGuildUser)Context.User;
            else if (!await BotUtils.ReplyHasPrivilegeAsync(Context, BotConfiguration, PrivilegeLevel.ServerModerator)) // Mod privileges are required to scan someone else's trophies
                return;

            if (BotConfiguration.TrophiesEnabled)
                await TrophyScanner.EnqueueAsync(new Creator(user.Id, user.Username), Context, scanImmediately: true);

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully added user **{0}** to the trophy scanner queue.", user.Username));

        }

    }

}