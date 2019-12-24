using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public interface ICommandHandlingService {

        Task InitializeAsync(IServiceProvider provider);
        Task InstallCommandsAsync();

        bool CommandIsRegistered(string commandName);
        CommandInfo GetCommandInfo(string commandName);

    }

}