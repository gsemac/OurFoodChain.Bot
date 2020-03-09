using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Discord.Bots;
using OurFoodChain.Discord.Commands;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public class CommandHandlingService :
        ICommandHandlingService {

        // Public members

        public CommandHandlingService(
            IBotConfiguration configuration,
            IServiceProvider serviceProvider,
            IHelpService helpService,
            IResponsiveMessageService responsiveMessageService,
            DiscordSocketClient discordClient,
            CommandService commandService
            ) {

            this.configuration = configuration;
            this.serviceProvider = serviceProvider;
            this.helpService = helpService;
            this.responsiveMessageService = responsiveMessageService;
            this.discordClient = discordClient;
            CommandService = commandService;

            this.discordClient.MessageReceived += OnMessageReceivedAsync;
            CommandService.CommandExecuted += OnCommandExecutedAsync;

        }

        public async Task InitializeAsync(IServiceProvider provider) {

            serviceProvider = provider;

            await InstallCommandsAsync();

        }
        public virtual async Task InstallCommandsAsync() {

            foreach (ModuleInfo moduleInfo in CommandService.Modules.ToArray())
                await CommandService.RemoveModuleAsync(moduleInfo);

            await CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);

        }

        // Protected members

        protected CommandService CommandService { get; private set; }

        protected virtual async Task OnMessageReceivedAsync(SocketMessage rawMessage) {

            bool handled = false;

            if (responsiveMessageService != null)
                handled = await responsiveMessageService.HandleMessageAsync(rawMessage);

            if (rawMessage.Content != configuration.Prefix && !handled) {

                if (MessageIsUserMessage(rawMessage) && MessageIsCommand(rawMessage))
                    await HandleCommandAsync(rawMessage);

            }

        }
        protected async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result) {

            if (!string.IsNullOrEmpty(result?.ErrorReason))
                await ShowCommandErrorAsync(command, context, result);

        }

        protected async Task<IResult> HandleCommandAsync(IMessage rawMessage) {

            IUserMessage message = rawMessage as IUserMessage;

            int argumentsIndex = GetCommmandArgumentsStartIndex(message);
            ICommandContext context = new CommandContext(discordClient, message);

            IResult result = await CommandService.ExecuteAsync(context, argumentsIndex, serviceProvider);

            return result;

        }
        protected async Task ShowCommandErrorAsync(Optional<CommandInfo> command, ICommandContext context, IResult result) {

            if (result is null) {

                await ShowGenericCommandErrorAsync(command, context, result);

            }
            else if (result.Error == CommandError.BadArgCount) {

                // Get the name of the command that the user attempted to use.

                string commandName = GetCommandName(context.Message);

                // If help documentation exists for this command, display it.

                ICommandHelpInfo commandHelpInfo = await helpService.GetCommandHelpInfoAsync(commandName);

                if (commandHelpInfo != null) {

                    EmbedBuilder embed = new EmbedBuilder();

                    embed.WithColor(Color.Red);
                    embed.WithTitle(string.Format("Incorrect use of \"{0}\" command", commandName.ToLower()));
                    embed.WithDescription("❌ " + result.ErrorReason);
                    embed.AddField("Example(s) of correct usage:", string.Join(Environment.NewLine, commandHelpInfo.Examples
                        .Select(e => string.Format("`{0}{1}{2}`", configuration.Prefix, commandName, e.SkipWords(1)))));

                    await context.Channel.SendMessageAsync("", false, embed.Build());

                }
                else
                    await ShowGenericCommandErrorAsync(command, context, result);

            }
            else if (result.Error == CommandError.UnknownCommand) {

                // Suggest the most-similar command as a possible misspelling.

                string messageContent = context.Message.Content.Substring(GetCommmandArgumentsStartIndex(context.Message));
                string commandName = messageContent.GetFirstWord();

                if (!string.IsNullOrEmpty(commandName)) {

                    string suggestedCommandName = StringUtilities.GetBestMatch(commandName, GetCommandNames());
                    ICommandHelpInfo commandHelpInfo = await helpService.GetCommandHelpInfoAsync(suggestedCommandName);

                    await DiscordUtilities.ReplyErrorAsync(context.Channel, string.Format($"Unknown command. Did you mean **{commandHelpInfo.Name}**?"));

                }
                else
                    await ShowGenericCommandErrorAsync(command, context, result);

            }
            else
                await ShowGenericCommandErrorAsync(command, context, result);

        }
        private async Task ShowGenericCommandErrorAsync(Optional<CommandInfo> command, ICommandContext context, IResult result) {

            if (result is null) {

                // Show a generic message if we don't have a result indicating what happened.

                if (command.IsSpecified)
                    await DiscordUtilities.ReplyErrorAsync(context.Channel, $"Something went wrong while executing the **{command.Value.Name}** command.");
                else
                    await DiscordUtilities.ReplyErrorAsync(context.Channel, $"Something went wrong while executing the command.");

            }
            else {

                await DiscordUtilities.ReplyErrorAsync(context.Channel, result.ErrorReason);

            }

        }

        protected string GetCommandName(IMessage rawMessage) {

            string content = rawMessage.Content.Substring(GetCommmandArgumentsStartIndex(rawMessage));

            string commandName = GetCommandNames()
                .OrderByDescending(c => c.Length)
                .Where(c => content.StartsWith(c, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            return string.IsNullOrEmpty(commandName) ? commandName : commandName.ToLowerInvariant();

        }
        protected IEnumerable<string> GetCommandNames() {

            return CommandService.Commands
               .SelectMany(c => new string[] { c.Name }.Union(c.Aliases));

        }
        protected int GetCommmandArgumentsStartIndex(IMessage rawMessage) {

            int index = 0;

            if (rawMessage is IUserMessage message) {

                message.HasStringPrefix(configuration.Prefix, ref index, StringComparison.InvariantCultureIgnoreCase);
                message.HasMentionPrefix(discordClient.CurrentUser, ref index);

            }

            return index;

        }
        protected bool MessageIsCommand(IMessage rawMessage) {

            if (rawMessage is IUserMessage message)
                return GetCommmandArgumentsStartIndex(message) != 0;
            else
                return false;

        }
        protected static bool MessageIsUserMessage(IMessage rawMessage) {

            return rawMessage is IUserMessage message && message.Source == MessageSource.User;

        }

        // Private members

        private IServiceProvider serviceProvider;
        private readonly IBotConfiguration configuration;
        private readonly IHelpService helpService;
        private readonly DiscordSocketClient discordClient;
        private readonly IResponsiveMessageService responsiveMessageService;

    }

}