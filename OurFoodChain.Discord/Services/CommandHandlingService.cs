using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Utilities;
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
            DiscordSocketClient discordClient,
            CommandService commandService
            ) {

            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _helpService = helpService;
            _discordClient = discordClient;
            CommandService = commandService;

            _discordClient.MessageReceived += MessageReceivedAsync;

        }

        public async Task InitializeAsync(IServiceProvider provider) {

            _serviceProvider = provider;

            await InstallCommandsAsync();

        }
        public virtual async Task InstallCommandsAsync() {

            foreach (ModuleInfo moduleInfo in CommandService.Modules.ToArray())
                await CommandService.RemoveModuleAsync(moduleInfo);

            await CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

        }

        // Protected members

        protected CommandService CommandService { get; private set; }

        protected virtual async Task MessageReceivedAsync(SocketMessage rawMessage) {

            if (rawMessage.Content == _configuration.Prefix)
                return;

            if (MessageIsUserMessage(rawMessage) && MessageIsCommand(rawMessage)) {

                IResult commandResult = await HandleCommandAsync(rawMessage);

                if (!commandResult.IsSuccess)
                    await ShowCommandErrorAsync(rawMessage, commandResult);

            }

        }

        protected async Task<IResult> HandleCommandAsync(IMessage rawMessage) {

            IUserMessage message = rawMessage as IUserMessage;

            int argumentsIndex = GetCommmandArgumentsStartIndex(message);
            ICommandContext context = new CommandContext(_discordClient, message);

            IResult result = await CommandService.ExecuteAsync(context, argumentsIndex, _serviceProvider);

            return result;

        }
        protected async Task ShowCommandErrorAsync(IMessage rawMessage, IResult result) {

            bool showDefaultErrorMessage = true;

            if (result.Error == CommandError.BadArgCount) {

                // Get the name of the command that the user attempted to use.

                string commandName = GetCommandName(rawMessage);

                // If help documentation exists for this command, display it.

                ICommandHelpInfo commandHelpInfo = await _helpService.GetCommandHelpInfoAsync(commandName);

                if (commandHelpInfo != null) {

                    EmbedBuilder embed = new EmbedBuilder();

                    embed.WithColor(Color.Red);
                    embed.WithTitle(string.Format("Incorrect usage of \"{0}\" command", commandName.ToLower()));
                    embed.WithDescription("❌ " + result.ErrorReason);
                    embed.AddField("Example(s) of correct usage:", string.Join(Environment.NewLine, commandHelpInfo.Examples
                        .Select(e => string.Format("`{0}{1}{2}`", _configuration.Prefix, commandName, e.SkipWords(1)))));

                    await rawMessage.Channel.SendMessageAsync("", false, embed.Build());

                    showDefaultErrorMessage = false;

                }

            }
            else if (result.Error == CommandError.UnknownCommand) {

                // Suggest the most-similar command as a possible misspelling.

                string messageContent = rawMessage.Content.Substring(GetCommmandArgumentsStartIndex(rawMessage));
                string commandName = messageContent.FirstWord();

                if (!string.IsNullOrEmpty(commandName)) {

                    string suggestedCommandName = StringUtilities.GetBestMatch(commandName, GetCommandNames());
                    ICommandHelpInfo commandHelpInfo = await _helpService.GetCommandHelpInfoAsync(suggestedCommandName);

                    await DiscordUtilities.ReplyErrorAsync(rawMessage.Channel, string.Format("Unknown command. Did you mean **{0}**?",
                        commandHelpInfo.Name));

                    showDefaultErrorMessage = false;

                }

            }

            if (showDefaultErrorMessage)
                await DiscordUtilities.ReplyErrorAsync(rawMessage.Channel, result.ErrorReason);

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

                message.HasStringPrefix(_configuration.Prefix, ref index, StringComparison.InvariantCultureIgnoreCase);
                message.HasMentionPrefix(_discordClient.CurrentUser, ref index);

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

        private IServiceProvider _serviceProvider;
        private readonly IBotConfiguration _configuration;
        private readonly IHelpService _helpService;
        private readonly DiscordSocketClient _discordClient;

    }

}