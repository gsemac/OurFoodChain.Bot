using Discord;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data;
using OurFoodChain.Debug;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public abstract class DatabaseServiceBase :
        IDatabaseService {

        // Public members

        public event Func<ILogMessage, Task> Log;

        public string DatabaseUpdatesDirectory { get; set; } = "data/updates/";

        public virtual async Task InitializeAsync() {

            await Task.CompletedTask;

        }
        public abstract Task<SQLiteDatabase> GetDatabaseAsync(ulong serverId);
        public abstract Task UploadDatabaseBackupAsync(IMessageChannel channel, ulong serverId);

        // Protected members

        protected async Task OnLogAsync(ILogMessage logMessage) {

            if (Log != null)
                await Log.Invoke(logMessage);

        }
        protected async Task OnLogAsync(Debug.LogSeverity severity, string message) {

            await OnLogAsync(new Debug.LogMessage(severity, "Database", message));

        }

        protected async Task<SQLiteDatabase> GetDatabaseAsync(string databaseFilePath) {

            if (!GetDatabaseStatus(databaseFilePath).Initialized)
                await InitializeDatabaseAsync(databaseFilePath);

            return SQLiteDatabase.FromFile(databaseFilePath);

        }
        protected async Task UploadDatabaseBackupAsync(IMessageChannel channel, string databaseFilePath) {

            bool backupInProgress = GetDatabaseStatus(databaseFilePath).BackupInProgress;

            if (backupInProgress) {

                await DiscordUtilities.ReplyErrorAsync(channel, "A backup is already in progress. Please wait until it has completed.");

            }
            else {

                GetDatabaseStatus(databaseFilePath).BackupInProgress = true;

                if (System.IO.File.Exists(databaseFilePath)) {

                    try {

                        await DiscordUtilities.ReplyInfoAsync(channel,
                            string.Format("Uploading database backup ({0:0.##} MB).\nThe backup will be posted in this channel when it is complete.",
                            new System.IO.FileInfo(databaseFilePath).Length / 1024000.0));

                        await channel.SendFileAsync(databaseFilePath, string.Format("`Database backup ({0})`", DateUtilities.GetCurrentDateUtc()));

                    }
                    catch (Exception) {

                        await DiscordUtilities.ReplyErrorAsync(channel, "Database file cannot be accessed.");

                    }

                }
                else {

                    await DiscordUtilities.ReplyErrorAsync(channel, "Database file does not exist at the specified path.");

                }

                GetDatabaseStatus(databaseFilePath).BackupInProgress = false;

            }

        }

        // Private members

        private class DatabaseStatus {
            public bool BackupInProgress { get; set; } = false;
            public bool Initialized { get; set; } = false;
        }

        private readonly ConcurrentDictionary<string, DatabaseStatus> databaseStatuses = new ConcurrentDictionary<string, DatabaseStatus>();
        private readonly object statusesLock = new object();

        private async Task<SQLiteDatabase> InitializeDatabaseAsync(string databaseFilePath) {

            await OnLogAsync(Debug.LogSeverity.Info, "Initializing database: " + databaseFilePath);

            SQLiteDatabase database = SQLiteDatabase.FromFile(databaseFilePath);
            SQLiteDatabaseUpdater updater = new SQLiteDatabaseUpdater(DatabaseUpdatesDirectory);

            updater.Log += async (sender, e) => await OnLogAsync(new Debug.LogMessage(e.Severity, e.Source, e.Message));

            await updater.ApplyUpdatesAsync(database);

            return database;

        }
        private DatabaseStatus GetDatabaseStatus(string databaseFilePath) {

            lock (statusesLock) {

                if (databaseStatuses.GetOrDefault(databaseFilePath) is null)
                    databaseStatuses[databaseFilePath] = new DatabaseStatus();

            }

            return databaseStatuses[databaseFilePath];

        }

    }

}