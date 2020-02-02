using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data {

    public sealed class SQLiteDatabaseUpdater {

        // Public members

        public event EventHandler<Debug.ILogMessage> Log;

        public int LatestVersion => GetUpdateFiles().Length;

        public SQLiteDatabaseUpdater(string updatesDirectoryPath) {

            this.updatesDirectoryPath = updatesDirectoryPath;

        }

        public async Task<long> ApplyUpdatesAsync(SQLiteDatabase database) {

            IEnumerable<string> updateFiles = GetUpdateFiles()
               .OrderBy(f => f);

            long version = await GetDatabaseVersionAsync(database);

            // Update the database to the latest version.

            using (SQLiteConnection conn = await database.GetConnectionAsync()) {

                for (long i = version + 1; i <= LatestVersion; ++i) {

                    OnLog(string.Format("Updating database to version {0}", i));

                    await ApplyDatabaseUpdateAsync(conn, updateFiles.ElementAt((int)(i - 1)));

                    await database.SetUserVersionAsync(i);

                }

            }

            // Return the number of updates applied.

            return Math.Max(0, LatestVersion - version);

        }

        // Protected members

        void OnLog(Debug.ILogMessage logMessage) {

            Log?.Invoke(this, logMessage);

        }
        void OnLog(string logMessage) {

            OnLog(new Debug.LogMessage("Database", logMessage));

        }

        // Private members

        private readonly string updatesDirectoryPath;

        private string[] GetUpdateFiles() {

            return System.IO.Directory.GetFiles(updatesDirectoryPath, "*.sql", System.IO.SearchOption.TopDirectoryOnly);

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "<Pending>")]
        private async Task ApplyDatabaseUpdateAsync(SQLiteConnection conn, string filePath) {

            using (SQLiteCommand cmd = new SQLiteCommand(System.IO.File.ReadAllText(filePath), conn))
                await cmd.ExecuteNonQueryAsync();

        }
        private async Task<long> GetDatabaseVersionAsync(SQLiteDatabase database) {

            // Attempt to get the version from user_version first.

            long userVersion = await database.GetUserVersionAsync();

            // If we fail to get the version from user_version, check for a "Meta" table (for database versions <= 26).
            // If the "Meta" table does not exist, ignore the error.

            if (userVersion <= 0) {

                try {

                    using (SQLiteCommand cmd = new SQLiteCommand("SELECT version FROM Meta"))
                        userVersion = await database.GetScalarAsync<long>(cmd);

                }
                catch (Exception) { }

            }

            return userVersion;

        }

    }

}