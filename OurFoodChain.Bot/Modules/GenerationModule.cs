using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common.Generations;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Messaging;
using System;
using System.Linq;
using System.Threading.Tasks;
using OurFoodChain.Discord.Extensions;

namespace OurFoodChain.Bot {

    public class GenerationModule :
        OfcModuleBase {

        // Public members

        [Command("gen"), RequireConfigSettingEnabled("generations_enabled")]
        public async Task Gen() {

            await ShowGenerationAsync(await Db.GetCurrentGenerationAsync());

        }
        [Command("gen"), RequireConfigSettingEnabled("generations_enabled")]
        public async Task Gen(int number) {

            await ShowGenerationAsync(await Db.GetGenerationAsync(number));

        }

        [Command("advancegen"), Alias("advgen"), RequireConfigSettingEnabled("generations_enabled"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task AdvanceGen() {

            IGeneration generation = await Db.AdvanceGenerationAsync();

            await ReplySuccessAsync($"Successfully advanced generation to **{generation.Name}**.");

        }
        [Command("revertgen"), Alias("revgen"), RequireConfigSettingEnabled("generations_enabled"), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task RevertGen() {

            if (await Db.RevertGenerationAsync())
                await ReplySuccessAsync(string.Format("Successfully set generation back to **{0}**.", (await Db.GetCurrentGenerationAsync()).Name));
            else
                await ReplySuccessAsync("The current generation cannot be reverted further.");

        }

        // Private members

        private async Task ShowGenerationAsync(IGeneration generation) {

            IPaginatedMessage message = await BuildGenerationEmbedAsync(generation);

            await ReplyAsync(message);

        }

        private async Task<IPaginatedMessage> BuildGenerationEmbedAsync(IGeneration generation) {

            IPaginatedMessage message = await BuildRecentEventsMessageAsync(generation.StartDate, generation.EndDate);
            TimeSpan span = DateUtilities.GetCurrentDateUtc() - generation.EndDate;

            string timeSpanString = DateUtilities.GetTimestampFromDate(generation.EndDate) == DateUtilities.GetMaxTimestamp() ? "Current" : DateUtilities.GetTimeSpanString(span) + " ago";

            message.SetTitle(string.Format("{0} ({1})", generation.Name, timeSpanString));

            return message;

        }

    }

}