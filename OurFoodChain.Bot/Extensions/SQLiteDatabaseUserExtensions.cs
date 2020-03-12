using OurFoodChain.Common;
using OurFoodChain.Data;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Extensions {

    public static class SQLiteDatabaseUserExtensions {

        public static async Task<IEnumerable<UserRank>> GetRanksAsync(this SQLiteDatabase database) {

            List<UserRank> result = new List<UserRank>();

            long place = 1;
            long lastCount = -1;

            // Get the users and their species counts, ordered by species count.

            using (SQLiteCommand cmd = new SQLiteCommand(@"SELECT owner, user_id, COUNT(id) AS count FROM Species WHERE user_id IS NOT NULL AND user_id != 0 GROUP BY user_id UNION 
                SELECT owner, user_id, COUNT(id) AS count FROM Species WHERE user_id IS NULL OR user_id = 0 GROUP BY owner ORDER BY count DESC")) {

                foreach (DataRow row in await database.GetRowsAsync(cmd)) {

                    // Get information about the user.

                    string username = row.IsNull("owner") ? UserInfo.NullUsername : row.Field<string>("owner");
                    ulong user_id = row.IsNull("user_id") ? UserInfo.NullId : (ulong)row.Field<long>("user_id");
                    long count = row.Field<long>("count");

                    if (lastCount != -1 && count < lastCount)
                        ++place;

                    lastCount = count;

                    result.Add(new UserRank {
                        User = new UserInfo {
                            Id = user_id,
                            Username = username,
                            SubmissionCount = (int)count
                        },
                        Rank = place
                    });

                }


            }

            return result.ToArray();

        }
        public static async Task<UserRank> GetRankAsync(this SQLiteDatabase database, ICreator creator, UserInfoQueryFlags flags = UserInfoQueryFlags.Default) {

            return (await database.GetRanksAsync())
                .Where(x => x.User.Id == creator.UserId || ((flags.HasFlag(UserInfoQueryFlags.MatchEither) || x.User.Id == UserInfo.NullId) && x.User.Username == creator.Name))
                .FirstOrDefault();

        }

    }

}