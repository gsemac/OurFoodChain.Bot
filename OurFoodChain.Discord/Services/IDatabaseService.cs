using Discord;
using OurFoodChain.Data;
using OurFoodChain.Debug;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public interface IDatabaseService {

        event Func<ILogMessage, Task> Log;

        Task InitializeAsync();

        Task<SQLiteDatabase> GetDatabaseAsync(IGuild guild);
        Task<IEnumerable<SQLiteDatabase>> GetDatabasesAsync();

        Task UploadDatabaseBackupAsync(IMessageChannel channel, IGuild guild);

    }

}