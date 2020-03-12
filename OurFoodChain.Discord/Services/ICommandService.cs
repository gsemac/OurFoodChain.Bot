using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public interface ICommandService {

        Task InitializeAsync(IServiceProvider provider);
        Task InstallCommandsAsync();

    }

}