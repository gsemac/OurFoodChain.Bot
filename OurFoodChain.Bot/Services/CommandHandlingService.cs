using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
            IOurFoodChainBotConfiguration configuration,
            IServiceProvider serviceProvider,
            DiscordSocketClient discordClient,
            CommandService commandService
            ) :
            base(configuration, serviceProvider, discordClient, commandService) {

            _configuration = configuration;

        }

        public override async Task InstallCommandsAsync() {

            await base.InstallCommandsAsync();

            if (!_configuration.TrophiesEnabled)
                await CommandService.RemoveModuleAsync<Modules.TrophiesModule>();

            if (!_configuration.GotchisEnabled)
                await CommandService.RemoveModuleAsync<Modules.GotchiModule>();

        }

        // Protected members

        protected override async Task MessageReceivedAsync(SocketMessage rawMessage) {

            if (await DiscordUtils.HandleMultiPartMessageResponseAsync(rawMessage))
                return;

            if (rawMessage.Content == _configuration.Prefix)
                return;

            if (MessageIsUserMessage(rawMessage) && MessageIsCommand(rawMessage)) {

                IResult commandResult = await HandleCommandAsync(rawMessage);

                if (!commandResult.IsSuccess) {

                    bool showErrorMessage = true;

                    if (commandResult.Error == CommandError.BadArgCount) {

                        // Get the name of the command that the user attempted to use.

                        string commandName = GetCommandName(rawMessage);

                        // If help documentation exists for this command, display it.

                        CommandHelpInfo commandHelpInfo = HelpUtils.GetCommandInfo(commandName);

                        if (commandHelpInfo != null) {

                            EmbedBuilder embed = new EmbedBuilder();

                            embed.WithColor(Color.Red);
                            embed.WithTitle(string.Format("Incorrect usage of \"{0}\" command", commandName));
                            embed.WithDescription("❌ " + commandResult.ErrorReason);
                            embed.AddField("Example(s) of correct usage:", commandHelpInfo.ExamplesToString(_configuration.Prefix));

                            await rawMessage.Channel.SendMessageAsync("", false, embed.Build());

                            showErrorMessage = false;

                        }

                    }

                    if (showErrorMessage)
                        await Discord.DiscordUtilities.ReplyErrorAsync(rawMessage.Channel, commandResult.ErrorReason);

                }

            }

        }

        // Private members

        private readonly IOurFoodChainBotConfiguration _configuration;

    }

}