using Discord.Commands;
using OurFoodChain.Data;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public class SingleDatabaseService :
        DatabaseServiceBase {

        // Public members

        public string DatabaseFilePath { get; set; } = "data.db";

        public override async Task InitializeAsync() {

            await BackupDatabaseAsync(DatabaseFilePath);

            await GetDatabaseAsync((ICommandContext)null); // initialize the database by accessing it

            await base.InitializeAsync();

        }

        public override async Task<SQLiteDatabase> GetDatabaseAsync(ICommandContext context) {

            // Each guild uses the same database.

            return await GetDatabaseAsync(DatabaseFilePath);

        }

        public override async Task UploadDatabaseBackupAsync(ICommandContext context) {

            await UploadDatabaseBackupAsync(context, DatabaseFilePath);

        }

    }

}