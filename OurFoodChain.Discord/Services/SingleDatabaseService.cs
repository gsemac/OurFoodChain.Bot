using Discord;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public class SingleDatabaseService :
        DatabaseServiceBase {

        // Public members

        public string DatabaseFilePath { get; set; } = "data.db";

        public override async Task InitializeAsync() {

            await BackupDatabaseAsync();

            await GetDatabaseAsync(0); // initialize the database by accessing it

            await base.InitializeAsync();

        }
        public override async Task<SQLiteDatabase> GetDatabaseAsync(ulong serverId) {

            // Each guild uses the same database.

            return await GetDatabaseAsync(DatabaseFilePath);

        }
        public override async Task UploadDatabaseBackupAsync(IMessageChannel channel, ulong serverId) {

            await UploadDatabaseBackupAsync(channel, DatabaseFilePath);

        }

        // Private members

        private async Task BackupDatabaseAsync() {

            if (System.IO.File.Exists(DatabaseFilePath)) {

                string backupFilename = string.Format("{0}-{1}", DateUtilities.GetCurrentTimestamp(), System.IO.Path.GetFileName(DatabaseFilePath));
                string backupFilePath = System.IO.Path.Combine("backups", backupFilename);

                await OnLogAsync(Debug.LogSeverity.Info, "Creating database backup: " + backupFilePath);

                System.IO.Directory.CreateDirectory("backups");

                System.IO.File.Copy(DatabaseFilePath, backupFilePath);

            }

            await Task.CompletedTask;

        }

    }

}