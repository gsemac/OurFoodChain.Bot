using Discord;
using Discord.Commands;
using Discord.WebSocket;
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

        public bool CommandIsRegistered(string commandName) {

            return GetCommandInfo(commandName) != null;

        }
        public CommandInfo GetCommandInfo(string commandName) {

            foreach (CommandInfo info in CommandService.Commands)
                if (info.Name.ToLower() == commandName.ToLower() || info.Aliases.Any(y => y.ToLower() == commandName.ToLower()))
                    return info;

            return null;

        }

        // Protected members

        protected CommandService CommandService { get; private set; }

        protected virtual async Task MessageReceivedAsync(SocketMessage rawMessage) {

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

                        ICommandHelpInfo commandHelpInfo = await _helpService.GetCommandHelpInfoAsync(commandName);

                        if (commandHelpInfo != null) {

                            EmbedBuilder embed = new EmbedBuilder();

                            embed.WithColor(Color.Red);
                            embed.WithTitle(string.Format("Incorrect usage of \"{0}\" command", commandName.ToLower()));
                            embed.WithDescription("❌ " + commandResult.ErrorReason);
                            embed.AddField("Example(s) of correct usage:", string.Join(Environment.NewLine, commandHelpInfo.Examples
                                .Select(e => string.Format("`{0}{1}`", _configuration.Prefix, e))));

                            await rawMessage.Channel.SendMessageAsync("", false, embed.Build());

                            showErrorMessage = false;

                        }

                    }

                    if (showErrorMessage)
                        await DiscordUtilities.ReplyErrorAsync(rawMessage.Channel, commandResult.ErrorReason);

                }

            }

        }

        protected async Task<IResult> HandleCommandAsync(IMessage rawMessage) {

            IUserMessage message = rawMessage as IUserMessage;

            int argumentsIndex = GetCommmandArgumentsStartIndex(message);
            ICommandContext context = new CommandContext(_discordClient, message);

            IResult result = await CommandService.ExecuteAsync(context, argumentsIndex, _serviceProvider);

            return result;

        }

        protected string GetCommandName(IMessage rawMessage) {

            string content = rawMessage.Content.Substring(GetCommmandArgumentsStartIndex(rawMessage));
            string pattern = @"^[^\s]+";

            System.Text.RegularExpressions.Match match =
                System.Text.RegularExpressions.Regex.Match(content, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success)
                return match.Value;
            else
                return string.Empty;

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