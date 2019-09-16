using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public static class UserUtils {

        public static async Task<UserInfo> GetUserInfoAsync(string username) {

            // If this user has submitted a species before, get their information.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT owner, user_id FROM Species WHERE owner = $owner COLLATE NOCASE AND user_id IS NOT NULL")) {

                cmd.Parameters.AddWithValue("$owner", username);

                DataRow row = await Database.GetRowAsync(cmd);

                if (row != null)
                    return new UserInfo {
                        UserId = (ulong)row.Field<long>("user_id"),
                        Username = row.Field<string>("owner")
                    };

            }

            return null;

        }

    }

}
