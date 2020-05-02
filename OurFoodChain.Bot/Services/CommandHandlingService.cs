using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OurFoodChain.Discord.Services;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Services {

    public class OurFoodChainBotCommandHandlingService :
        Discord.Services.CommandService {

        // Public members

        public OurFoodChainBotCommandHandlingService(
            IOfcBotConfiguration configuration,
            IServiceProvider serviceProvider,
            IHelpService helpService,
            IResponsiveMessageService responsiveMessageService,
            DiscordSocketClient discordClient,
            global::Discord.Commands.CommandService commandService
            ) :
            base(configuration, serviceProvider, helpService, responsiveMessageService, discordClient, commandService) {

            botConfiguration = configuration;

        }

        public override async Task InstallCommandsAsync() {

            await base.InstallCommandsAsync();

            if (!botConfiguration.TrophiesEnabled)
                await DiscordCommandService.RemoveModuleAsync<Modules.TrophyModule>();

            if (!botConfiguration.GotchisEnabled)
                await DiscordCommandService.RemoveModuleAsync<Modules.GotchiModule>();

        }

        // Protected members

        protected override async Task OnMessageReceivedAsync(SocketMessage rawMessage) {

            ulong userId = rawMessage.Author.Id;

            // If the user has been banned, show an error message.
            // Bot admins cannot be banned.

            bool userIsBanned = botConfiguration.BannedUserIds?.Any(id => id.Equals(userId)) ?? false;
            bool userIsBotAdmin = botConfiguration.BotAdminUserIds?.Any(id => id.Equals(userId)) ?? false;

            if (userIsBanned && !userIsBotAdmin)
                await DiscordUtilities.ReplyErrorAsync(rawMessage.Channel, "You do not have permission to use this command.");
            else
                await base.OnMessageReceivedAsync(rawMessage);

        }

        // Private members

        private readonly IOfcBotConfiguration botConfiguration;

    }

}