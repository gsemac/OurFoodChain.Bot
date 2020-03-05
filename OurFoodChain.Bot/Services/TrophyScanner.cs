using Discord;
using Discord.Commands;
using OurFoodChain.Common;
using OurFoodChain.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Trophies;

namespace OurFoodChain.Services {

    public class TrophyScanner :
        TrophyScannerBase {

        // Public members

        public TrophyScanner(SQLiteDatabase database) :
            base(database) {

            TrophyUnlocked += PopTrophyAsync;

        }

        public async Task<bool> EnqueueAsync(ICreator creator, ICommandContext commandContext, bool scanImmediately = false) {

            bool userAdded = await EnqueueAsync(creator, scanImmediately);

            if (userAdded && creator.UserId.HasValue)
                userContexts[creator.UserId.Value] = commandContext;

            return userAdded;

        }

        // Private members

        private readonly ConcurrentDictionary<ulong, ICommandContext> userContexts = new ConcurrentDictionary<ulong, ICommandContext>();

        private async Task PopTrophyAsync(IUnlockedTrophyInfo unlocked) {

            if (unlocked.Creator.UserId.HasValue) {

                ICommandContext commandContext = userContexts.GetOrDefault(unlocked.Creator.UserId.Value);

                if (commandContext != null) {

                    EmbedBuilder embed = new EmbedBuilder();

                    embed.WithTitle(string.Format("🏆 Trophy unlocked!"));
                    embed.WithDescription(string.Format("Congratulations {0}! You've earned the **{1}** trophy.",
                       (await commandContext.Guild.GetUserAsync(unlocked.Creator.UserId.Value)).Mention, unlocked.Trophy.Name));
                    embed.WithFooter(unlocked.Trophy.Description);
                    embed.WithColor(new Color(255, 204, 77));

                    await commandContext.Channel.SendMessageAsync("", false, embed.Build());

                }

            }

        }

    }

}