using Discord;
using System;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Bots {

    public interface IDiscordBot :
        IDisposable {

        string Name { get; }

        Task StartAsync();
        Task StopAsync();
        Task RestartAsync(IMessageChannel channel = null);

    }

}