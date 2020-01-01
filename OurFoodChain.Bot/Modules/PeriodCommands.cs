using Discord;
using Discord.Commands;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class PeriodCommands :
        ModuleBase {

        [Command("periods")]
        public async Task Periods() {

            Period[] periods = await BotUtils.GetPeriodsFromDb();
            StringBuilder description_builder = new StringBuilder();

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(string.Format("📅 All periods ({0})", periods.Count()));
            embed.WithDescription(description_builder.ToString());

            foreach (Period period in periods) {

                embed.AddField(period.GetName(),
                    string.Format("{0}—{1} ({2}, {3})",
                    period.GetStartTimestampString(),
                    period.GetEndTimestampString(),
                    period.GetHowLongString(),
                    period.GetHowLongAgoString()
                    ));

            }



            await ReplyAsync("", false, embed.Build());

        }
        [Command("period")]
        public async Task GetPeriod(string name) {

            // Get period from the database.

            Period period = await BotUtils.GetPeriodFromDb(name);

            if (!await BotUtils.ReplyAsync_ValidatePeriod(Context, period))
                return;

            await GetPeriod(period);

        }
        public async Task GetPeriod(Period period) {

            string embed_title = string.Format("📅 {0} ({1}—{2}, {3})", period.GetName(), period.GetStartTimestampString(), period.GetEndTimestampString(), period.GetHowLongAgoString());

            // Get all species that were born during this time period.

            List<Species> born_species = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE timestamp > $start_ts AND timestamp <= $end_ts;")) {

                cmd.Parameters.AddWithValue("$start_ts", period.GetStartTimestamp());
                cmd.Parameters.AddWithValue("$end_ts", period.GetEndTimestamp());

                using (DataTable table = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in table.Rows)
                        born_species.Add(await SpeciesUtils.SpeciesFromDataRow(row));

            }

            // Get all species that went extinct during this time period.

            List<Species> died_species = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Species WHERE id IN (SELECT species_id FROM Extinctions WHERE timestamp > $start_ts AND timestamp <= $end_ts);")) {

                cmd.Parameters.AddWithValue("$start_ts", period.GetStartTimestamp());
                cmd.Parameters.AddWithValue("$end_ts", period.GetEndTimestamp());

                using (DataTable table = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in table.Rows)
                        died_species.Add(await SpeciesUtils.SpeciesFromDataRow(row));

            }

            // Create the embed pages.

            List<EmbedBuilder> pages = new List<EmbedBuilder>();

            // Create the first page, showing the period name and description.

            EmbedBuilder page_1 = new EmbedBuilder();
            page_1.WithTitle(embed_title);
            page_1.WithDescription(period.GetDescriptionOrDefault());
            page_1.AddField("New species", born_species.Count(), inline: true);
            page_1.AddField("Extinctions", died_species.Count(), inline: true);

            pages.Add(page_1);

            // #todo Add additional pages listing species names?

            //CommandUtils.PaginatedMessage message = new CommandUtils.PaginatedMessage();

            //for (int i = 0; i < pages.Count(); ++i)
            //    message.pages.Add(pages[i].Build());

            //await CommandUtils.ReplyAsync_SendPaginatedMessage(Context, message);

            await ReplyAsync("", false, page_1.Build());

        }
        [Command("addperiod")]
        public async Task AddPeriod(string name, string startDate) {
            await AddPeriod(name, startDate, "now");
        }
        [Command("addperiod")]
        public async Task AddPeriod(string name, string startDate, string endDate) {

            // Make sure we don't already have a period with the same name.

            Period p = await BotUtils.GetPeriodFromDb(name);

            if (!(p is null)) {

                await BotUtils.ReplyAsync_Warning(Context, string.Format("The period **{0}** already exists.", p.GetName()));

                return;

            }

            startDate = startDate.ToLower();
            endDate = endDate.ToLower();

            long start_ts = 0;
            long end_ts = 0;

            // Try to parse the dates and convert them to timestamps.

            if (Period.TryParseDate(startDate, out DateTime start_dt) && Period.TryParseDate(endDate, out DateTime end_dt)) {

                start_ts = new DateTimeOffset(start_dt).ToUnixTimeSeconds();
                end_ts = new DateTimeOffset(end_dt).ToUnixTimeSeconds();

                if (end_ts >= start_ts) {

                    // If the end date is "now", update the previous period that ended "now" to end at the start date of this period.

                    if (endDate == "now") {

                        using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Period SET end_ts = $end_ts WHERE end_ts=\"now\";")) {

                            cmd.Parameters.AddWithValue("$end_ts", start_ts.ToString());

                            await Database.ExecuteNonQuery(cmd);

                        }

                    }

                    // Insert the new period into the database.

                    using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Period(name, start_ts, end_ts) VALUES ($name, $start_ts, $end_ts);")) {

                        cmd.Parameters.AddWithValue("$name", name.ToLower());
                        cmd.Parameters.AddWithValue("$start_ts", start_ts.ToString()); // Period can't start with "now", but can use the "now" timestamp
                        cmd.Parameters.AddWithValue("$end_ts", endDate == "now" ? "now" : end_ts.ToString());

                        await Database.ExecuteNonQuery(cmd);

                    }

                    await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully created new period **{0}**.", StringUtilities.ToTitleCase(name)));

                }
                else
                    await BotUtils.ReplyAsync_Error(Context, "The period end date must occur after the period start date.");

            }
            else
                await BotUtils.ReplyAsync_Error(Context, string.Format("Invalid date format provided. Please use the format \"{0}\".", Period.DATE_FORMAT));

        }
        [Command("setperioddescription"), Alias("setperioddesc")]
        public async Task SetPeriodDescription(string name, string description) {

            // Make sure that the given period exists.

            Period period = await BotUtils.GetPeriodFromDb(name);

            if (!await BotUtils.ReplyAsync_ValidatePeriod(Context, period))
                return;

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Period SET description = $description WHERE id = $id;")) {

                cmd.Parameters.AddWithValue("$id", period.id);
                cmd.Parameters.AddWithValue("$description", description);

                await Database.ExecuteNonQuery(cmd);

            }

            await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully updated description for period **{0}**.", period.GetName()));

        }

    }

}
