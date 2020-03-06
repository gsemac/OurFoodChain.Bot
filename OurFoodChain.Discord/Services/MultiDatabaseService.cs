using Discord;
using OurFoodChain.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public class MultiDatabaseService :
        DatabaseServiceBase {

        // Public members

        public async override Task<SQLiteDatabase> GetDatabaseAsync(ulong serverId) {

            // Each guild should have access to their own database, saved inside of their guild folder.

            return await GetDatabaseAsync(GetDatabasePathForGuild(serverId));

        }
        public async override Task UploadDatabaseBackupAsync(IMessageChannel channel, ulong serverId) {

            await UploadDatabaseBackupAsync(channel, GetDatabasePathForGuild(serverId));

        }

        // Private members

        private string GetDatabasePathForGuild(ulong serverId) {

            return string.Format("{0}/{0}.db", serverId);

        }

    }

}