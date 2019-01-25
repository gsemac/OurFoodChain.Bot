using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.trophies {

    public class TrophyRegistry {

        public static async Task InitializeAsync() {

            await _registerAllAsync();

        }
        public static async Task<UnlockedTrophyInfo[]> GetUnlockedTrophiesAsync(ulong userId) {

            List<UnlockedTrophyInfo> unlocked = new List<UnlockedTrophyInfo>();

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM Trophies WHERE user_id=$user_id;")) {

                cmd.Parameters.AddWithValue("$user_id", userId);

                using (DataTable rows = await Database.GetRowsAsync(cmd)) {

                    foreach (DataRow row in rows.Rows) {

                        string trophy_name = row.Field<string>("trophy_name");
                        long times_unlocked = 0;

                        using (SQLiteCommand cmd2 = new SQLiteCommand("SELECT COUNT(*) FROM Trophies WHERE trophy_name=$trophy_name;")) {

                            cmd2.Parameters.AddWithValue("$trophy_name", trophy_name);

                            times_unlocked = await Database.GetScalar<long>(cmd2);

                        }

                        UnlockedTrophyInfo info = new UnlockedTrophyInfo {
                            identifier = trophy_name,
                            timesUnlocked = times_unlocked,
                            timestamp = row.Field<long>("timestamp")
                        };

                        unlocked.Add(info);

                    }

                }

            }

            return unlocked.ToArray();

        }
        public static async Task SetUnlocked(ulong userId, Trophy trophy) {

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT OR IGNORE INTO Trophies(user_id, trophy_name, timestamp) VALUES($user_id, $trophy_name, $timestamp);")) {

                cmd.Parameters.AddWithValue("$user_id", userId);
                cmd.Parameters.AddWithValue("$trophy_name", trophy.GetIdentifier());
                cmd.Parameters.AddWithValue("$timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                await Database.ExecuteNonQuery(cmd);

            }

        }

        public static IReadOnlyCollection<Trophy> Trophies => _registry.AsReadOnly();


        private static List<Trophy> _registry = new List<Trophy>();

        private static async Task _registerAllAsync() {

            // Don't bother if we've already registered the trophies.
            if (_registry.Count > 0)
                return;

            await OurFoodChainBot.GetInstance().Log(Discord.LogSeverity.Info, "Trophies", "Registering trophies");

            _registry.Add(new Trophy("Super Special Trophy", "This trophy is meaningless, and only exists for testing purposes. You're not special.", async (ulong userId) => {
                return true;
            }));

        }


    }

}