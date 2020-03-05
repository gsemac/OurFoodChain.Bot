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
                            DateUnlocked = DateUtilities.TimestampToDate(row.Field<long>("timestamp"))
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
        public static async Task<long> GetTimesTrophyUnlockedAsync(this SQLiteDatabase database, ITrophy trophy) {

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM Trophies WHERE trophy_name = $trophy_name")) {

                cmd.Parameters.AddWithValue("$trophy_name", trophy.Identifier);

                return await database.GetScalarAsync<long>(cmd);

            }

        }
        public static async Task<IEnumerable<IUnlockedTrophyInfo>> GetCreatorsWithTrophyAsync(this SQLiteDatabase database, ITrophy trophy) {

            List<IUnlockedTrophyInfo> results = new List<IUnlockedTrophyInfo>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT user_id, timestamp FROM Trophies WHERE trophy_name = $trophy_name")) {

                cmd.Parameters.AddWithValue("$trophy_name", trophy.Identifier);

                IEnumerable<DataRow> rows = await database.GetRowsAsync(cmd);

                foreach (DataRow row in rows) {

                    ICreator creator = new Creator((ulong)row.Field<long>("user_id"), string.Empty);
                    DateTimeOffset dateEarned = DateUtilities.TimestampToDate(row.Field<long>("timestamp"));

                    results.Add(new UnlockedTrophyInfo(creator, trophy) {
                        DateUnlocked = dateEarned,
                        TimesUnlocked = rows.Count()
                    });

                }

            }

            return results;

        }
        public static async Task<double> GetTrophyCompletionRateAsync(this SQLiteDatabase database, ITrophy trophy) {

            // The completion rate is determined from the number of users who have earned the trophy and the number of users who have submitted species.

            long times_unlocked = await database.GetTimesTrophyUnlockedAsync(trophy);
            long total_users = 0;

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(*) FROM (SELECT user_id FROM Species GROUP BY user_id)"))
                total_users = await database.GetScalarAsync<long>(cmd);

            return (total_users <= 0) ? 0.0 : (100.0 * times_unlocked / total_users);

        }
        public static async Task<double> GetTrophyCompletionRateAsync(this SQLiteDatabase database, ICreator creator, IEnumerable<ITrophy> trophyList, bool includeOneTimeTrophies = false) {

            IEnumerable<IUnlockedTrophyInfo> unlocked = await database.GetUnlockedTrophiesAsync(creator, trophyList);

            int unlocked_count = unlocked
                .Where(x => {

                    if (includeOneTimeTrophies)
                        return true;

                    ITrophy t = x.Trophy;

                    return t != null && !t.Flags.HasFlag(TrophyFlags.OneTime);

                })
                .Count();
            int trophy_count = trophyList.Where(x => includeOneTimeTrophies || !x.Flags.HasFlag(TrophyFlags.OneTime)).Count();

            return trophy_count <= 0 ? 0.0 : (100.0 * unlocked_count / trophy_count);

        }

    }

}