using Discord;
using Discord.Commands;
using OurFoodChain.Data;
using OurFoodChain.Debug;
using System;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public interface IDatabaseService {

        event Func<ILogMessage, Task> Log;

        Task InitializeAsync();

        Task<SQLiteDatabase> GetDatabaseAsync(IGuild guild);

        Task UploadDatabaseBackupAsync(IMessageChannel channel, IGuild guild);

    }

}