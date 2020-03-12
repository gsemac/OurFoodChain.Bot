using Discord;
using OurFoodChain.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public class MultiDatabaseService :
        DatabaseServiceBase {

        // Public members

        public async override Task<SQLiteDatabase> GetDatabaseAsync(ulong serverId) {

            // Each guild should have access to their own database, saved inside of their guild folder.

            string databaseFilePath = GetDatabasePathForGuild(serverId);

            return await GetDatabaseAsync(databaseFilePath);

        }
        public async override Task UploadDatabaseBackupAsync(IMessageChannel channel, ulong serverId) {

            await UploadDatabaseBackupAsync(channel, GetDatabasePathForGuild(serverId));

        }

        // Private members

        private string GetDatabasePathForGuild(ulong serverId) {

            string databaseDirectory = serverId.ToString();
            string databaseFilePath = System.IO.Path.Combine(databaseDirectory, "data.db");

            if (!System.IO.Directory.Exists(databaseDirectory))
                System.IO.Directory.CreateDirectory(databaseDirectory);

            return databaseFilePath;

        }

    }

}