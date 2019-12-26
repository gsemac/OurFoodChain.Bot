using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public interface IHelpService {

        Task<ICommandHelpInfo> GetCommandHelpInfoAsync(string commandName);
        Task<IEnumerable<ICommandHelpInfo>> GetCommandHelpInfoAsync();
        Task<IEnumerable<ICommandHelpInfo>> GetCommandHelpInfoAsync(ICommandContext context);
        Task<bool> IsCommandAvailableAsync(ICommandContext context, string commandName);

    }

}