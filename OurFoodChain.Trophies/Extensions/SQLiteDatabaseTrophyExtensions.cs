using OurFoodChain.Common;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.Extensions {

    public static class SQLiteDatabaseTrophyExtensions {

        public static async Task<IEnumerable<IUnlockedTrophyInfo>> GetUnlockedTrophiesAsync(this SQLiteDatabase database, ICreator creator, IEnumerable<ITrophy> trophyList) {

            List<IUnlockedTrophyInfo> unlocked = new List<IUnlockedTrophyInfo>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Trophies WHERE user_id = $user_id ")) {

                cmd.Parameters.AddWithValue("$user_id", creator.UserId);

                foreach (DataRow row in await database.GetRowsAsync(cmd)) {

                    string trophyName = row.Field<string>("trophy_name");
                    long timesUnlocked = 0;

                    using (SQLiteCommand cmd2 = new SQLiteCommand("SELECT COUNT(*) FROM Trophies WHERE trophy_name = $trophy_name ")) {

                        cmd2.Parameters.AddWithValue("$trophy_name", trophyName);

                        timesUnlocked = await database.GetScalarAsync<long>(cmd2);

                    }

                    ITrophy trophy = trophyList
                        .Where(t => t.Identifier.Equals(trophyName, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();

                    if (trophy != null) {

                        IUnlockedTrophyInfo info = new UnlockedTrophyInfo(creator, trophy) {
                            TimesUnlocked = (int)timesUnlocked,
                            DateFirstUnlocked = DateUtilities.TimestampToDate(row.Field<long>("timestamp"))
                        };

                        unlocked.Add(info);

                    }

                }

            }

            return unlocked;

        }
        public static async Task UnlockTrophyAsync(this SQLiteDatabase database, ICreator creator, ITrophy trophy) {

            if (creator.UserId.HasValue) {

                using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Trophies(user_id, trophy_name, timestamp) VALUES($user_id, $trophy_name, $timestamp);")) {

                    cmd.Parameters.AddWithValue("$user_id", creator.UserId);
                    cmd.Parameters.AddWithValue("$trophy_name", trophy.Identifier);
                    cmd.Parameters.AddWithValue("$timestamp", DateUtilities.GetCurrentUtcTimestamp());

                    await database.ExecuteNonQueryAsync(cmd);

                }

            }

        }

    }

}