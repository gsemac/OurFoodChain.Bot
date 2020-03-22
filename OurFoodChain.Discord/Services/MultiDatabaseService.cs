using Discord;
using Discord.Commands;
using OurFoodChain.Data;
using OurFoodChain.Discord.Utilities;
using System;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public class MultiDatabaseService :
        DatabaseServiceBase {

        // Public members

        public async override Task<SQLiteDatabase> GetDatabaseAsync(ICommandContext context) {

            // Each guild should have access to their own database, saved inside of their guild folder.

            string databaseFilePath = GetDatabasePathForGuild(context.Guild);

            if (string.IsNullOrWhiteSpace(databaseFilePath)) {

                string exceptionMessage = context.Guild is null ?
                    "Database must be accessed through a guild." :
                    "The database path could not be determined.";

                await DiscordUtilities.ReplyErrorAsync(context.Channel, exceptionMessage);

                throw new Exception(exceptionMessage);

            }

            return await GetDatabaseAsync(databaseFilePath);

        }
        public async override Task UploadDatabaseBackupAsync(ICommandContext context) {

            await UploadDatabaseBackupAsync(context, GetDatabasePathForGuild(context.Guild));

        }

        // Private members

        private string GetDatabasePathForGuild(IGuild guild) {

            string databaseFilePath = string.Empty;

            if (guild != null) {

                string databaseDirectory = guild.Id.ToString();

                if (!System.IO.Directory.Exists(databaseDirectory))
                    System.IO.Directory.CreateDirectory(databaseDirectory);

                databaseFilePath = System.IO.Path.Combine(databaseDirectory, "data.db");

            }

            return databaseFilePath;

        }

    }

}