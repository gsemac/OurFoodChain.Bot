using Discord;
using Discord.Commands;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class RecentModule :
        OfcModuleBase {

        [Command("recent")]
        public async Task Recent() {
            await Recent("48h");
        }
        [Command("recent")]
        public async Task Recent(string timespan) {

            TimeAmount time_amount = TimeAmount.Parse(timespan);

            if (time_amount != null) {

                long start_ts = DateUtilities.GetCurrentTimestampUtc() - time_amount.ToUnixTimeSeconds();
                long end_ts = DateUtilities.GetCurrentTimestampUtc();

                await ShowRecentEventsAsync(start_ts, end_ts, timeUnit: time_amount.Unit);

            }
            else
                await BotUtils.ReplyAsync_Error(Context, "Invalid timespan provided.");

        }

        public async Task<Discord.Messaging.IPaginatedMessage> BuildRecentEventsEmbedAsync(long startTimestamp, long endTimestamp, TimeUnits timeUnit = 0) {

            // Get all species created within the given timespan.

            List<ISpecies> new_species = new List<ISpecies>();
            TimeAmount time_amount = new TimeAmount(endTimestamp - startTimestamp, TimeUnits.Seconds);

            if (timeUnit != 0)
                time_amount = time_amount.ConvertTo(timeUnit);
            else
                time_amount = time_amount.Reduce();

            new_species.AddRange(await Db.GetSpeciesAsync(DateUtilities.GetDateFromTimestamp(startTimestamp), DateUtilities.GetDateFromTimestamp(endTimestamp)));

            new_species.Sort();

            // Get all extinctions that occurred recently.

            List<ISpecies> extinct_species = new List<ISpecies>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Extinctions WHERE timestamp >= $start_ts AND timestamp < $end_ts")) {

                cmd.Parameters.AddWithValue("$start_ts", startTimestamp);
                cmd.Parameters.AddWithValue("$end_ts", endTimestamp);

                foreach (DataRow row in await Db.GetRowsAsync(cmd))
                    extinct_species.Add(await Db.GetSpeciesAsync(row.Field<long>("species_id")));

            }

            extinct_species.Sort();

            // Build embed.

            List<Discord.Messaging.IEmbed> pages = new List<Discord.Messaging.IEmbed>();
            List<string> field_lines = new List<string>();

            if (new_species.Count() > 0) {

                foreach (ISpecies sp in new_species)
                    field_lines.Add(sp.GetFullName());

                EmbedUtilities.AppendEmbedPages(pages, EmbedUtilities.CreateEmbedPages(string.Format("New species ({0})", new_species.Count()), field_lines));

                field_lines.Clear();

            }

            if (extinct_species.Count() > 0) {

                foreach (ISpecies sp in extinct_species)
                    field_lines.Add(sp.GetFullName());

                EmbedUtilities.AppendEmbedPages(pages, EmbedUtilities.CreateEmbedPages(string.Format("Extinctions ({0})", extinct_species.Count()), field_lines));

                field_lines.Clear();

            }

            foreach (Discord.Messaging.IEmbed page in pages) {

                page.Title = string.Format("Recent events ({0})", time_amount.ToString());

                if (page.Fields.Count() <= 0)
                    page.Description = "No events";

            }

            EmbedUtilities.AddPageNumbers(pages);

            return new Discord.Messaging.PaginatedMessage(pages);

        }
        public async Task ShowRecentEventsAsync(long startTimestamp, long endTimestamp, TimeUnits timeUnit = 0) {

            Discord.Messaging.IPaginatedMessage embed = await BuildRecentEventsEmbedAsync(startTimestamp, endTimestamp, timeUnit);

            await ReplyAsync(embed);

        }

    }

}