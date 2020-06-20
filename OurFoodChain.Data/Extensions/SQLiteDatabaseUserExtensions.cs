using OurFoodChain.Common;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Extensions {

    public static class SQLiteDatabaseUserExtensions {

        // Public members

        public static async Task AddUserAsync(this ISQLiteDatabase database, IUser user) {

            // If the user is already in the database, make sure that we update that entry instead of generating a new ID.

            IUser oldUser = await database.GetUserAsync(user);

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR REPLACE INTO Users VALUES($id, $user_id, $name)")) {

                cmd.Parameters.AddWithValue("$id", user.Id ?? oldUser.Id);
                cmd.Parameters.AddWithValue("$user_id", user.UserId ?? oldUser.UserId);
                cmd.Parameters.AddWithValue("$name", user.Name ?? oldUser.Name);

                await database.ExecuteNonQueryAsync(cmd);

            }

        }
        public static async Task<IUser> GetUserAsync(this ISQLiteDatabase database, IUser user) {

            string whereClause;

            if (user.Id.HasValue)
                whereClause = "WHERE id = $id";
            else if (user.UserId.HasValue)
                whereClause = "WHERE user_id = $user_id";
            else
                whereClause = "WHERE name = $name";

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Users " + whereClause)) {

                cmd.Parameters.AddWithValue("$id", user.Id);
                cmd.Parameters.AddWithValue("$user_id", user.UserId);
                cmd.Parameters.AddWithValue("$name", user.Name);

                DataRow row = await database.GetRowAsync(cmd);

                if (row is null)
                    return null;

                return new User(row.Field<string>("name")) {
                    Id = row.Field<long>("id"),
                    UserId = (ulong)row.Field<long>("user_id"),
                };

            }

        }

    }

}