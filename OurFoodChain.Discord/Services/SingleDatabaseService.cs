using Discord;
using Discord.Commands;
using OurFoodChain.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public class SingleDatabaseService :
        DatabaseServiceBase {

        // Public members

        public string DatabaseFilePath { get; set; } = "data.db";

        public override async Task InitializeAsync() {

            await BackupDatabaseAsync(DatabaseFilePath);

            await GetDatabaseAsync(); // initialize the database by accessing it

            await base.InitializeAsync();

        }

        public override async Task<SQLiteDatabase> GetDatabaseAsync(IGuild guild) {

            // Each guild uses the same database.

            return await GetDatabaseAsync(DatabaseFilePath);

        }
        public override async Task<IEnumerable<SQLiteDatabase>> GetDatabasesAsync() {

            return new[] { await GetDatabaseAsync() };

        }

        public override async Task UploadDatabaseBackupAsync(IMessageChannel channel, IGuild guild) {

            await UploadDatabaseBackupAsync(channel, DatabaseFilePath);

        }

        // Private members

        public async Task<SQLiteDatabase> GetDatabaseAsync() {

            return await GetDatabaseAsync((IGuild)null);

        }

    }

}