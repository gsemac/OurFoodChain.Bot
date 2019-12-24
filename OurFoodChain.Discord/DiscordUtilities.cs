using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord {

    public static class DiscordUtilities {

        public static async Task ReplySuccessAsync(IMessageChannel channel, string message) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(string.Format("✅ {0}", message));
            embed.WithColor(Color.Green);

            await channel.SendMessageAsync("", false, embed.Build());

        }
        public static async Task ReplyErrorAsync(IMessageChannel channel, string message) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(string.Format("❌ {0}", message));
            embed.WithColor(Color.Red);

            await channel.SendMessageAsync("", false, embed.Build());

        }

    }

}