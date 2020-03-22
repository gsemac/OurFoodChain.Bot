using Discord.Commands;
using OurFoodChain.Debug;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public interface ICommandService {

        event Func<ILogMessage, Task> Log;

        Task InitializeAsync(IServiceProvider provider);
        Task InstallCommandsAsync();

    }

}