using Discord;
using Discord.Commands;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data;
using OurFoodChain.Debug;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public enum DatabaseBackupFrequency {
        Always,
        Daily,
        Default = Always
    }

    public abstract class DatabaseServiceBase :
        IDatabaseService {

        // Public members

        public event Func<ILogMessage, Task> Log;

        public string DatabaseUpdatesDirectory { get; set; } = "data/updates/";

        public virtual async Task InitializeAsync() {

            await Task.CompletedTask;

        }

        public abstract Task<SQLiteDatabase> GetDatabaseAsync(IGuild guild);

        public abstract Task UploadDatabaseBackupAsync(IMessageChannel channel, IGuild guild);

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
        protected async Task BackupDatabaseAsync(string databaseFilePath, DatabaseBackupFrequency backupFrequency = DatabaseBackupFrequency.Default) {

            if (File.Exists(databaseFilePath)) {

                string databaseDirectoryPath = Path.GetDirectoryName(databaseFilePath);
                string backupDirectoryPath = Path.Combine(databaseDirectoryPath, "backups");

                if (!Directory.Exists(backupDirectoryPath))
                    Directory.CreateDirectory(backupDirectoryPath);

                bool doBackup = true;

                DateTimeOffset lastWriteTime = GetFileLastWriteTime(databaseFilePath);
                DateTimeOffset lastBackupWriteTime = GetLastDatabaseBackupTime(backupDirectoryPath);

                if (backupFrequency == DatabaseBackupFrequency.Daily && (DateTimeOffset.UtcNow.Date == lastBackupWriteTime.Date))
                    doBackup = false;

                doBackup = doBackup && lastWriteTime != lastBackupWriteTime;

                if (doBackup) {

                    string backupFilename = string.Format("{0}-{1}", DateUtilities.GetCurrentTimestamp(), Path.GetFileName(databaseFilePath));
                    string backupFilePath = Path.Combine(backupDirectoryPath, backupFilename);

                    await OnLogAsync(Debug.LogSeverity.Info, "Creating database backup " + backupFilePath);

                    Directory.CreateDirectory("backups");

                    File.Copy(databaseFilePath, backupFilePath);

                }

            }

            await Task.CompletedTask;

        }

        // Private members

        private class DatabaseStatus {
            public bool BackupInProgress { get; set; } = false;
            public bool Initialized { get; set; } = false;
        }

        private readonly ConcurrentDictionary<string, DatabaseStatus> databaseStatuses = new ConcurrentDictionary<string, DatabaseStatus>();
        private readonly object statusesLock = new object();

        private async Task<SQLiteDatabase> InitializeDatabaseAsync(string databaseFilePath) {

            await OnLogAsync(Debug.LogSeverity.Info, "Initializing database " + databaseFilePath);

            SQLiteDatabase database = SQLiteDatabase.FromFile(databaseFilePath);
            SQLiteDatabaseUpdater updater = new SQLiteDatabaseUpdater(DatabaseUpdatesDirectory);

            updater.Log += async (sender, e) => await OnLogAsync(new Debug.LogMessage(e.Severity, e.Source, e.Message));

            await updater.ApplyUpdatesAsync(database);

            GetDatabaseStatus(databaseFilePath).Initialized = true;

            return database;

        }
        private DatabaseStatus GetDatabaseStatus(string databaseFilePath) {

            lock (statusesLock) {

                if (databaseStatuses.GetOrDefault(databaseFilePath) is null)
                    databaseStatuses[databaseFilePath] = new DatabaseStatus();

            }

            return databaseStatuses[databaseFilePath];

        }

        private DateTimeOffset GetFileLastWriteTime(string filePath) {

            return new FileInfo(filePath).LastWriteTimeUtc;

        }
        private DateTimeOffset GetLastDatabaseBackupTime(string backupDirectoryPath) {

            if (!Directory.Exists(backupDirectoryPath))
                return DateTimeOffset.MinValue;

            DirectoryInfo directoryInfo = new DirectoryInfo(backupDirectoryPath);

            return directoryInfo.GetFiles("*.db", SearchOption.TopDirectoryOnly)
                .Select(fileInfo => new DateTimeOffset(fileInfo.LastWriteTimeUtc))
                .OrderByDescending(date => date)
                .FirstOrDefault();

        }

    }

}