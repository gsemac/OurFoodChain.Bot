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

            await BackupDatabaseAsync(DatabaseFilePath);

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

    }

}