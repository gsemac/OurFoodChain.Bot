using OurFoodChain.Common.Taxa;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class SuperiorSurvivorTrophy :
        TrophyBase {

        public SuperiorSurvivorTrophy() :
            base("Superior Survivor", "Have a species you own survive an extinction event.") {
        }

        public async override Task<bool> CheckTrophyAsync(ITrophyScannerContext context) {

            // The minimum number of simultaneous extinctions to be considered an "exinction event"
            long extinction_threshold = 5;
            // The extinction threshold must be reached within the given number of hours
            long ts_threshold = 24 * 60 * 60; // 24 hours

            long current_threshold = 0;
            long current_ts = 0;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT timestamp FROM Extinctions ORDER BY timestamp ASC")) {

                int row_index = 0;

                IEnumerable<DataRow> rows = await context.Database.GetRowsAsync(cmd);

                foreach (DataRow row in rows) {

                    long ts = (long)row.Field<decimal>("timestamp");

                    if (current_ts == 0)
                        current_ts = ts;

                    if (ts - current_ts > ts_threshold || row_index == rows.Count() - 1) {

                        // To make this process more efficient, we'll check the trophy condition at the end of an extinction event.
                        // The check will also occur when we reach the end of the extinction records, in case it ended on an extinction event.

                        if (current_threshold >= extinction_threshold) {

                            // The user has a species that survived the extinction event if the species existed before the event, and still exists.

                            using (SQLiteCommand cmd2 = new SQLiteCommand("SELECT COUNT(*) FROM Species WHERE user_id = $user_id AND timestamp <= $timestamp AND id NOT IN (SELECT species_id FROM Extinctions)")) {

                                cmd2.Parameters.AddWithValue("$user_id", context.Creator.UserId);
                                cmd2.Parameters.AddWithValue("$timestamp", current_ts);

                                if (await context.Database.GetScalarAsync<long>(cmd2) > 0)
                                    return true;

                            }

                        }

                        current_ts = ts;
                        current_threshold = 0;

                    }
                    else
                        ++current_threshold;

                    ++row_index;

                }

            }

            return false;

        }

    }

}