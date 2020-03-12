using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OurFoodChain.Discord.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Services {

    public class OurFoodChainBotCommandHandlingService :
        Discord.Services.CommandHandlingService {

        // Public members

        public OurFoodChainBotCommandHandlingService(
            IOfcBotConfiguration configuration,
            IServiceProvider serviceProvider,
            IHelpService helpService,
            IResponsiveMessageService responsiveMessageService,
            DiscordSocketClient discordClient,
            CommandService commandService
            ) :
            base(configuration, serviceProvider, helpService, responsiveMessageService, discordClient, commandService) {

            _configuration = configuration;

        }

        public override async Task InstallCommandsAsync() {

            await base.InstallCommandsAsync();

            if (!_configuration.TrophiesEnabled)
                await CommandService.RemoveModuleAsync<Modules.TrophyModule>();

            if (!_configuration.GotchisEnabled)
                await CommandService.RemoveModuleAsync<Modules.GotchiModule>();

        }

        // Protected members

        protected override async Task OnMessageReceivedAsync(SocketMessage rawMessage) {

            await base.OnMessageReceivedAsync(rawMessage);

        }

        // Private members

        private readonly IOfcBotConfiguration _configuration;

    }

}