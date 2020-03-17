using Discord.Commands;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Extensions;
using System;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class RecentModule :
        OfcModuleBase {

        [Command("recent")]
        public async Task Recent() {

            await Recent("48h");

        }
        [Command("recent")]
        public async Task Recent(string timespanStr) {

            if (DateUtilities.TryParseTimeSpan(timespanStr, out TimeSpan timespan)) {

                DateTimeOffset start = DateUtilities.GetCurrentDateUtc() - timespan;
                DateTimeOffset end = DateUtilities.GetCurrentDateUtc();

                await this.ReplyRecentEventsAsync(start, end);

            }
            else
                await ReplyErrorAsync("Unrecognized timespan.");

        }

    }

}