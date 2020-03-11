using OurFoodChain.Common;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Extensions {

    [Flags]
    public enum UserInfoQueryFlags {
        // If a user ID is provided, only return results that match that user ID.
        PreferUserIdMatch = 0,
        // Returns results that match the user ID or the username.
        MatchEither = 1,
        Default = PreferUserIdMatch
    }

    public static class SQLiteDatabaseCreatorExtensions {

        public static async Task<ICreator> GetCreatorAsync(this SQLiteDatabase database, string name) {

            return await database.GetCreatorAsync(new Creator(name));

        }
        public static async Task<ICreator> GetCreatorAsync(this SQLiteDatabase database, ulong? userId) {

            return await database.GetCreatorAsync(new Creator(userId, string.Empty));

        }
        public static async Task<ICreator> GetCreatorAsync(this SQLiteDatabase database, ICreator creator, UserInfoQueryFlags flags = UserInfoQueryFlags.Default) {

            ICreator result = null;

            // Note that we may have a null username or a null user ID.
            // At least one of them has to be non-null.

            if (!string.IsNullOrEmpty(creator.Name) || creator.UserId.HasValue) {

                // If we've been given a user ID, get all species records where that user is the owner.
                // If we've just been given a username, we'll go by username instead.

                string query;

                if (flags.HasFlag(UserInfoQueryFlags.MatchEither)) {

                    query = "SELECT owner, user_id, timestamp FROM Species WHERE user_id = $user_id OR owner = $owner";

                }
                else {

                    if (creator.UserId.HasValue)
                        query = "SELECT owner, user_id, timestamp FROM Species WHERE user_id = $user_id";
                    else
                        query = "SELECT owner, user_id, timestamp FROM Species WHERE owner = $owner COLLATE NOCASE";

                }

                using (SQLiteCommand cmd = new SQLiteCommand(query)) {

                    cmd.Parameters.AddWithValue("$user_id", creator.UserId);
                    cmd.Parameters.AddWithValue("$owner", creator.Name);

                    IEnumerable<DataRow> rows = await database.GetRowsAsync(cmd);

                    if (rows.Count() > 0) {

                        ulong? userId = rows.Select(row => row.IsNull("user_id") ? null : (ulong?)row.Field<long>("user_id"))
                            .FirstOrDefault(id => id.HasValue);

                        string username = rows.Select(row => row.IsNull("owner") ? string.Empty : row.Field<string>("owner"))
                            .FirstOrDefault(name => !string.IsNullOrEmpty(name));

                        if (userId.HasValue || !string.IsNullOrEmpty(username)) {

                            long firstSpeciesTimestamp = rows.Select(row => (long)row.Field<decimal>("timestamp")).OrderBy(timestamp => timestamp).FirstOrDefault();
                            long lastSpeciesTimestamp = rows.Select(rowx => (long)rowx.Field<decimal>("timestamp")).OrderByDescending(timestamp => timestamp).FirstOrDefault();

                            result = new Creator(userId, username) {
                                SpeciesCount = rows.Count(),
                                FirstSpeciesDate = firstSpeciesTimestamp > 0 ? DateUtilities.GetDateFromTimestamp(firstSpeciesTimestamp) : default(DateTimeOffset?),
                                LastSpeciesDate = lastSpeciesTimestamp > 0 ? DateUtilities.GetDateFromTimestamp(lastSpeciesTimestamp) : default(DateTimeOffset?)
                            };

                        }

                    }

                }

            }

            return result;

        }

    }

}