using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public enum UserInfoQueryFlags {
        // If a user ID is provided, only return results that match that user ID.
        PreferUserIdMatch = 0,
        // Returns results that match the user ID or the username.
        MatchBoth = 1,
        Default = PreferUserIdMatch
    }

    public static class UserUtils {

        public static async Task<UserInfo> GetUserInfoAsync(string username) {
            return await GetUserInfoAsync(username, UserInfo.NullId);
        }
        public static async Task<UserInfo> GetUserInfoAsync(ulong userId) {
            return await GetUserInfoAsync(UserInfo.NullUsername, userId);
        }
        public static async Task<UserInfo> GetUserInfoAsync(string username, ulong userId, UserInfoQueryFlags flags = UserInfoQueryFlags.Default) {

            // Note that we may have a null username or a null user ID.
            // At least one of them has to be non-null.

            UserInfo result = null;

            if (username != UserInfo.NullUsername || userId != UserInfo.NullId) {

                // If we've been given a user ID, get all species records where that user is the owner.
                // If we've just been given a username, we'll go by username instead.

                string query;

                if (flags.HasFlag(UserInfoQueryFlags.MatchBoth)) {

                    query = "SELECT owner, user_id, timestamp FROM Species WHERE user_id = $user_id OR owner = $owner";

                }
                else {

                    if (userId != UserInfo.NullId)
                        query = "SELECT owner, user_id, timestamp FROM Species WHERE user_id = $user_id";
                    else
                        query = "SELECT owner, user_id, timestamp FROM Species WHERE owner = $owner COLLATE NOCASE";

                }

                using (SQLiteCommand cmd = new SQLiteCommand(query)) {

                    cmd.Parameters.AddWithValue("$user_id", userId);
                    cmd.Parameters.AddWithValue("$owner", username);

                    using (DataTable table = await Database.GetRowsAsync(cmd))
                        if (table.Rows.Count > 0) {

                            userId = table.Rows.Cast<DataRow>()
                                .Select(x => x.IsNull("user_id") ? UserInfo.NullId : (ulong)x.Field<long>("user_id"))
                                .FirstOrDefault(x => x != UserInfo.NullId);

                            username = table.Rows.Cast<DataRow>()
                                .Select(x => x.IsNull("owner") ? UserInfo.NullUsername : x.Field<string>("owner"))
                                .FirstOrDefault(x => x != UserInfo.NullUsername);

                            result = new UserInfo {
                                Id = userId == default(ulong) ? UserInfo.NullId : userId,
                                Username = username == default(string) ? UserInfo.NullUsername : username,
                                SubmissionCount = table.Rows.Count,
                                FirstSubmissionTimestamp = table.Rows.Cast<DataRow>().Select(x => (long)x.Field<decimal>("timestamp")).OrderBy(x => x).FirstOrDefault(),
                                LastSubmissionTimestamp = table.Rows.Cast<DataRow>().Select(x => (long)x.Field<decimal>("timestamp")).OrderByDescending(x => x).FirstOrDefault()
                            };

                        }

                }

            }

            return result;

        }

        public static async Task<UserRank[]> GetRanksAsync() {

            List<UserRank> result = new List<UserRank>();

            long place = 1;
            long lastCount = -1;

            // Get the users and their species counts, ordered by species count.

            using (SQLiteCommand cmd = new SQLiteCommand(@"SELECT owner, user_id, COUNT(id) AS count FROM Species WHERE user_id IS NOT NULL GROUP BY user_id UNION 
                SELECT owner, user_id, COUNT(id) AS count FROM Species WHERE user_id IS NULL GROUP BY owner ORDER BY count DESC")) {

                using (DataTable table = await Database.GetRowsAsync(cmd))
                    foreach (DataRow row in table.Rows) {

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
        public static async Task<UserRank> GetRankAsync(UserInfo userInfo, UserInfoQueryFlags flags = UserInfoQueryFlags.Default) {

            return (await GetRanksAsync())
                .Where(x => x.User.Id == userInfo.Id || ((flags.HasFlag(UserInfoQueryFlags.MatchBoth) || x.User.Id == UserInfo.NullId) && x.User.Username == userInfo.Username))
                .FirstOrDefault();

        }

        public static async Task<Species[]> GetSpeciesAsync(UserInfo userInfo, UserInfoQueryFlags flags = UserInfoQueryFlags.Default) {

            string query = "SELECT * FROM Species WHERE user_id = $user_id";

            if (flags.HasFlag(UserInfoQueryFlags.MatchBoth))
                query = "SELECT * FROM Species WHERE owner = $owner OR user_id = $user_id";

            List<Species> result = new List<Species>();

            using (SQLiteCommand cmd = new SQLiteCommand(query)) {

                cmd.Parameters.AddWithValue("$owner", userInfo.Username);
                cmd.Parameters.AddWithValue("$user_id", userInfo.Id);

                using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                    foreach (DataRow row in rows.Rows)
                        result.Add(await Species.FromDataRow(row));

                    result.Sort((lhs, rhs) => lhs.GetShortName().CompareTo(rhs.GetShortName()));

                }

            }

            return result.ToArray();

        }

    }

}