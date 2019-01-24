using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class OurFoodChainBot {

        private const string DEFAULT_PREFIX = "?";
        private const string DEFAULT_PLAYING = "";

        public OurFoodChainBot() {

            // Set up a default configuration.

            _config = new Config();
            _config.prefix = DEFAULT_PREFIX;
            _config.playing = DEFAULT_PLAYING;

            _discord_client = new DiscordSocketClient();
            _command_service = new CommandService();
            _service_provider = new ServiceCollection().BuildServiceProvider();

            _discord_client.Log += _log;
            _discord_client.MessageReceived += _messageReceived;
            _discord_client.ReactionAdded += _reactionReceived;
            _discord_client.ReactionRemoved += _reactionRemoved;

            _instance = this;

        }

        public void LoadSettings(string filePath) {

            Debug.Assert(System.IO.File.Exists(filePath));

            _config = JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText(filePath));

            Debug.Assert(!string.IsNullOrEmpty(_config.token));

            if (string.IsNullOrEmpty(_config.prefix))
                _config.token = DEFAULT_PREFIX;

        }

        public async Task Connect() {

            // Install commands.

            await _command_service.AddModulesAsync(System.Reflection.Assembly.GetEntryAssembly(), _service_provider);

            // Login to Discord.

            await _discord_client.LoginAsync(TokenType.Bot, _config.token);
            await _discord_client.StartAsync();

            // Set the bot's "Now Playing".

            await _discord_client.SetGameAsync(_config.playing);

        }
        public async Task Log(LogSeverity severity, string source, string message) {
            await _log(new LogMessage(severity, source, message));
        }

        public Config GetConfig() {
            return _config;
        }
        public ulong GetUserId() {

            return _discord_client.CurrentUser.Id;

        }
        public IDiscordClient GetClient() {

            return _discord_client;

        }

        public struct Config {
            public string[] adminIds;
            public string token;
            public string prefix;
            public string playing;
            public ulong scratch_server;
            public ulong scratch_channel;
        }

        public static OurFoodChainBot GetInstance() {
            return _instance;
        }

        static OurFoodChainBot _instance = null;
        Config _config;

        private DiscordSocketClient _discord_client;
        private CommandService _command_service;
        private IServiceProvider _service_provider;

        private async Task _log(LogMessage message) {

            Console.WriteLine(message.ToString());

            await Task.FromResult(false);

        }
        private async Task _messageReceived(SocketMessage message) {

            // If the message was not sent by a user (e.g., Discord, bot, etc.), ignore it.
            if (!_isUserMessage(message))
                return;

            if (BotUtils.TWO_PART_COMMAND_WAIT_PARAMS.ContainsKey(message.Author.Id)) {

                await BotUtils.HandleTwoPartCommandResponse(message);

                return;

            }

            if (!_isBotCommand(message as SocketUserMessage))
                return;

            await _executeCommand(message as SocketUserMessage);

        }
        private async Task _reactionReceived(Cacheable<IUserMessage, ulong> cached, ISocketMessageChannel channel, SocketReaction reaction) {
            await CommandUtils.HandlePaginatedMessageReaction(cached, channel, reaction, true);
        }
        private async Task _reactionRemoved(Cacheable<IUserMessage, ulong> cached, ISocketMessageChannel channel, SocketReaction reaction) {
            await CommandUtils.HandlePaginatedMessageReaction(cached, channel, reaction, false);
        }

        private bool _isUserMessage(SocketMessage message) {

            var m = message as SocketUserMessage;

            return (m != null);

        }
        private int _getCommandArgumentsPosition(SocketUserMessage message) {

            int pos = 0;

            if (message == null)
                return pos;

            message.HasStringPrefix(_config.prefix, ref pos, StringComparison.InvariantCultureIgnoreCase);
            message.HasMentionPrefix(_discord_client.CurrentUser, ref pos);

            return pos;

        }
        private bool _isBotCommand(SocketUserMessage message) {

            return _getCommandArgumentsPosition(message) != 0;

        }
        private async Task<bool> _executeCommand(SocketUserMessage message) {

            // If the message is just the bot's prefix, don't attempt to respond to it (this reduces "Unknown command" spam).

            if (message.Content == _config.prefix)
                return false;

            int pos = _getCommandArgumentsPosition(message);
            var context = new CommandContext(_discord_client, message);

            var result = await _command_service.ExecuteAsync(context, pos, _service_provider);

            if (result.IsSuccess)
                return true;

            await BotUtils.ReplyAsync_Error(context, result.ErrorReason);

            return false;

        }

    }

}