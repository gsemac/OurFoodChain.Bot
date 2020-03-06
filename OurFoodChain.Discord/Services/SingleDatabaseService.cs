using Discord;
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

        public async override Task<SQLiteDatabase> GetDatabaseAsync(ulong serverId) {

            // Each guild uses the same database.

            return await GetDatabaseAsync(DatabaseFilePath);

        }
        public async override Task UploadDatabaseBackupAsync(IMessageChannel channel, ulong serverId) {

            await UploadDatabaseBackupAsync(channel, DatabaseFilePath);

        }

    }

}