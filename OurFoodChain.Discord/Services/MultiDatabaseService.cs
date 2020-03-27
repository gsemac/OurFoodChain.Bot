using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OurFoodChain.Data;
using OurFoodChain.Discord.Utilities;
using System;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public class MultiDatabaseService :
        DatabaseServiceBase {

        // Public members

        public MultiDatabaseService(DiscordSocketClient discordClient) {

            this.discordClient = discordClient;

        }

        public override async Task InitializeAsync() {

            discordClient.GuildAvailable += GuildAvailableAsync;

            await base.InitializeAsync();

        }
        public override async Task<SQLiteDatabase> GetDatabaseAsync(IGuild guild) {

            // Each guild should have access to their own database, saved inside of their guild folder.

            string databaseFilePath = GetDatabaseFilePathForGuild(guild);

            if (string.IsNullOrWhiteSpace(databaseFilePath)) {

                string exceptionMessage = guild is null ?
                    "Database must be accessed through a guild." :
                    "The database path could not be determined.";

                throw new Exception(exceptionMessage);

            }

            return await GetDatabaseAsync(databaseFilePath);

        }
        public override async Task UploadDatabaseBackupAsync(IMessageChannel channel, IGuild guild) {

            await UploadDatabaseBackupAsync(channel, GetDatabaseFilePathForGuild(guild));

        }

        // Private members

        private readonly DiscordSocketClient discordClient;

        private string GetDatabaseFilePathForGuild(IGuild guild) {

            string databaseFilePath = string.Empty;

            if (guild != null) {

                string databaseDirectory = guild.Id.ToString();

                if (!System.IO.Directory.Exists(databaseDirectory))
                    System.IO.Directory.CreateDirectory(databaseDirectory);

                databaseFilePath = System.IO.Path.Combine(databaseDirectory, "data.db");

            }

            return databaseFilePath;

        }

        private async Task GuildAvailableAsync(IGuild guild) {

            // Create a database backup and initialize the database.

            await BackupDatabaseAsync(GetDatabaseFilePathForGuild(guild));

            await GetDatabaseAsync(guild); // initialize the database by accessing it

        }

    }

}