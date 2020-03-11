using Discord;
using Discord.Commands;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class RecentCommands :
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

                await ShowRecentEventsAsync(Context, start_ts, end_ts, timeUnit: time_amount.Unit);

            }
            else
                await BotUtils.ReplyAsync_Error(Context, "Invalid timespan provided.");

        }

        public async Task<Bot.PaginatedMessageBuilder> BuildRecentEventsEmbedAsync(long startTimestamp, long endTimestamp, TimeUnits timeUnit = 0) {

            // Get all species created within the given timespan.

            List<Species> new_species = new List<Species>();
            TimeAmount time_amount = new TimeAmount(endTimestamp - startTimestamp, TimeUnits.Seconds);

            if (timeUnit != 0)
                time_amount = time_amount.ConvertTo(timeUnit);
            else
                time_amount = time_amount.Reduce();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE timestamp >= $start_ts AND timestamp < $end_ts")) {

                cmd.Parameters.AddWithValue("$start_ts", startTimestamp);
                cmd.Parameters.AddWithValue("$end_ts", endTimestamp);

                foreach (DataRow row in await Db.GetRowsAsync(cmd))
                    new_species.Add(await SpeciesUtils.SpeciesFromDataRow(row));

            }

            new_species.Sort();

            // Get all extinctions that occurred recently.

            List<Species> extinct_species = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Extinctions WHERE timestamp >= $start_ts AND timestamp < $end_ts")) {

                cmd.Parameters.AddWithValue("$start_ts", startTimestamp);
                cmd.Parameters.AddWithValue("$end_ts", endTimestamp);

                foreach (DataRow row in await Db.GetRowsAsync(cmd))
                    extinct_species.Add(await BotUtils.GetSpeciesFromDb(row.Field<long>("species_id")));

            }

            extinct_species.Sort();

            // Build embed.

            Bot.PaginatedMessageBuilder embed = new Bot.PaginatedMessageBuilder();
            List<EmbedBuilder> pages = new List<EmbedBuilder>();
            List<string> field_lines = new List<string>();

            if (new_species.Count() > 0) {

                foreach (Species sp in new_species)
                    field_lines.Add(sp.FullName);

                EmbedUtils.AddLongFieldToEmbedPages(pages, field_lines, fieldName: string.Format("New species ({0})", new_species.Count()));

                field_lines.Clear();

            }

            if (extinct_species.Count() > 0) {

                foreach (Species sp in extinct_species)
                    field_lines.Add(sp.FullName);

                EmbedUtils.AddLongFieldToEmbedPages(pages, field_lines, fieldName: string.Format("Extinctions ({0})", extinct_species.Count()));

                field_lines.Clear();

            }

            embed.AddPages(pages);

            embed.SetTitle(string.Format("Recent events ({0})", time_amount.ToString()));
            embed.SetFooter(string.Empty); // remove page numbers added automatically
            embed.AddPageNumbers();

            if (embed.FieldCount <= 0)
                embed.SetDescription("No events");

            return embed;

        }
        public async Task ShowRecentEventsAsync(ICommandContext context, long startTimestamp, long endTimestamp, TimeUnits timeUnit = 0) {

            Bot.PaginatedMessageBuilder embed = await BuildRecentEventsEmbedAsync(startTimestamp, endTimestamp, timeUnit);

            await Bot.DiscordUtils.SendMessageAsync(context, embed.Build());

        }

    }

}