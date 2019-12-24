using Discord;
using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class GenerationCommands :
        ModuleBase {

        [Command("gen"), RequireConfigSettingEnabled("generations_enabled")]
        public async Task Gen() {

            await _showGenerationAsync(Context, await GenerationUtils.GetCurrentGenerationAsync());

        }
        [Command("gen"), RequireConfigSettingEnabled("generations_enabled")]
        public async Task Gen(int number) {

            await _showGenerationAsync(Context, await GenerationUtils.GetGenerationAsync(number));

        }

        [Command("advancegen"), Alias("advgen"), RequireConfigSettingEnabled("generations_enabled")]
        public async Task AdvanceGen() {

            Generation generation = await GenerationUtils.AdvanceGenerationAsync();

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully advanced generation to **{0}**.", generation.Name));

        }
        [Command("revertgen"), Alias("revgen"), RequireConfigSettingEnabled("generations_enabled")]
        public async Task RevertGen() {

            if (await GenerationUtils.RevertGenerationAsync())
                await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully set generation back to **{0}**.", (await GenerationUtils.GetCurrentGenerationAsync()).Name));
            else
                await BotUtils.ReplyAsync_Warning(Context, "The current generation cannot be reverted further.");

        }

        private async Task<Bot.PaginatedMessageBuilder> _buildGenerationEmbedAsync(Generation generation) {

            Bot.PaginatedMessageBuilder embed = await RecentCommands.BuildRecentEventsEmbedAsync(generation.StartTimestamp, generation.EndTimestamp);
            TimeAmount time_amount_since = new TimeAmount(DateUtils.GetCurrentTimestamp() - generation.EndTimestamp, TimeUnits.Seconds);

            embed.SetTitle(string.Format("{0} ({1})",
                generation.Name,
                generation.EndTimestamp == DateUtils.GetMaxTimestamp() ? "Current" : time_amount_since.Reduce().ToString() + " ago"));

            return embed;

        }
        private async Task _showGenerationAsync(ICommandContext context, Generation generation) {

            Bot.PaginatedMessageBuilder embed = await _buildGenerationEmbedAsync(generation);

            await Bot.DiscordUtils.SendMessageAsync(context, embed.Build());

        }

    }

}