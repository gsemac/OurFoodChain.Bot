using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class DatabaseUpdaterConfig {

        public string UpdatesDirectory { get; set; } = "";

        [JsonProperty("file_name_format")]
        public string FileNameFormat { get; set; } = "";
        [JsonProperty("latest_version")]
        public int LatestVersion { get; set; } = 0;

    }

    public class DatabaseUpdater {

        public int LatestVersion {
            get {
                return Config.LatestVersion;
            }
        }

        public DatabaseUpdater(SQLiteConnection conn, DatabaseUpdaterConfig config) {
            Connection = conn;
            Config = config;
        }

        public SQLiteConnection Connection { get; set; } = null;
        public DatabaseUpdaterConfig Config { get; set; } = null;

        public async Task<int> GetDatabaseVersionAsync() {

            if (Connection is null)
                throw new Exception("Database connection was null.");

            // Create metadata table if it doesn't already exist.

            using (SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Meta (version INTEGER);", Connection))
                await cmd.ExecuteNonQueryAsync();

            // Get the current table version from the database. If the query was successful, return the result.

            using (SQLiteCommand cmd = new SQLiteCommand("SELECT version FROM Meta;", Connection))
            using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                    return reader.GetInt32(0);

            // If no version information exists, insert it.

            using (SQLiteCommand cmd = new SQLiteCommand("INSERT INTO Meta VALUES (0);", Connection))
                await cmd.ExecuteNonQueryAsync();

            // Return 0 to indicate the database has not yet been initialized.
            return 0;

        }
        public async Task<int> UpdateToLatestVersionAsync() {

            if (Connection is null)
                throw new Exception("Database connection was null.");

            int version = await GetDatabaseVersionAsync();

            // Update the database to the latest version.

            for (int i = ++version; i <= LatestVersion; ++i)
                await _applyDatabaseUpdateAsync(i);

            return LatestVersion;

        }

        private async Task _setDatabaseVersionAsync(int versionNumber) {

            using (SQLiteCommand cmd = new SQLiteCommand("UPDATE Meta SET version = $version WHERE version < $version;", Connection)) {

                cmd.Parameters.AddWithValue("$version", versionNumber);

                await cmd.ExecuteNonQueryAsync();

            }

        }
        private async Task _applyDatabaseUpdateAsync(int versionNumber) {

            if (versionNumber <= 0)
                return;

            await Bot.OurFoodChainBot.Instance.LogAsync(Discord.LogSeverity.Info, "Database", string.Format("Updating database to version {0}", versionNumber));

            string update_fname = System.IO.Path.Combine(Config.UpdatesDirectory, string.Format(Config.FileNameFormat, versionNumber));

            if (!System.IO.File.Exists(update_fname))
                throw new Exception(string.Format("Update file {0} does not exist.", System.IO.Path.GetFileName(update_fname)));

            using (SQLiteCommand cmd = new SQLiteCommand(System.IO.File.ReadAllText(update_fname), Connection))
                await cmd.ExecuteNonQueryAsync();

            // Update the database version.

            await _setDatabaseVersionAsync(versionNumber);

            await Bot.OurFoodChainBot.Instance.LogAsync(Discord.LogSeverity.Info, "Database", string.Format("Updated database to version {0}", versionNumber));

        }

    }

}