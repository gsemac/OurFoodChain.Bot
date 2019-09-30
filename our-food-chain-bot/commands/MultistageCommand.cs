using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class MultistageCommandCallbackArgs {

        public MultistageCommandCallbackArgs(MultistageCommand command, string messageContent) {

            Command = command;
            MessageContent = messageContent;

        }

        public MultistageCommand Command { get; }
        public string MessageContent { get; }

    }

    public class MultistageCommand {

        public MultistageCommand(ICommandContext context) {

            Context = context;
            Arguments = new string[] { };
            Timestamp = DateTime.Now;
            ChannelId = context.Channel.Id;
            AllowCancel = true;

        }

        public ICommandContext Context { get; }
        public string[] Arguments { get; set; }
        public DateTime Timestamp { get; set; }
        public ulong ChannelId { get; }
        public bool AllowCancel { get; set; }
        public Func<MultistageCommandCallbackArgs, Task> Callback { get; set; }

        public static async Task SendAsync(MultistageCommand multistageCommand, string message) {

            if (_multistage_commands.TryAdd(multistageCommand.Context.User.Id, multistageCommand)) {

                multistageCommand.Timestamp = DateTime.Now;

                await multistageCommand.Context.Channel.SendMessageAsync(message);

            }

        }
        public static async Task<bool> HandleResponseAsync(SocketMessage message) {

            // If there is no multistage command in progress for the given user, do nothing.
            if (!_multistage_commands.ContainsKey(message.Author.Id))
                return false;

            // If we can't retrieve the multistage command data for this user, do nothing.
            if (!_multistage_commands.TryGetValue(message.Author.Id, out MultistageCommand command))
                return false;

            // The response to a multistage command must occur in the same channel as the original command.
            if (message.Channel.Id != command.ChannelId)
                return false;

            string message_content = message.Content;

            if (command.AllowCancel && message_content.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                await BotUtils.ReplyAsync_Info(command.Context, "The command has been canceled.");
            else if (!(command.Callback is null))
                await command.Callback(new MultistageCommandCallbackArgs(command, message_content));

            // Remove the multistage command data now that we've handled it. 
            _multistage_commands.TryRemove(message.Author.Id, out _);

            return true;

        }

        private static ConcurrentDictionary<ulong, MultistageCommand> _multistage_commands = new ConcurrentDictionary<ulong, MultistageCommand>();

    }

}