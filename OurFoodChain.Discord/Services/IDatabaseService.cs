using Discord;
using OurFoodChain.Data;
using OurFoodChain.Debug;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public interface IDatabaseService {

        event Func<ILogMessage, Task> Log;

        Task<SQLiteDatabase> GetDatabaseAsync(ulong serverId);
        Task UploadDatabaseBackupAsync(IMessageChannel channel, ulong serverId);

    }

}