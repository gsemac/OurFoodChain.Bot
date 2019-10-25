using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class MultiPartMessageCallbackArgs {

        public MultiPartMessage Message { get; }
        public string ResponseContent { get; }

        public MultiPartMessageCallbackArgs(MultiPartMessage message, string responseContent) {

            Message = message;
            ResponseContent = responseContent;

        }

    }

    public class MultiPartMessage {

        // public members

        public MultiPartMessage(ICommandContext context) {
            Context = context;
        }

        public ICommandContext Context { get; } = null;
        public Func<MultiPartMessageCallbackArgs, Task> Callback { get; set; }

        public string Text { get; set; } = string.Empty;
        public string[] UserData { get; set; } = new string[] { };

        public long Timestamp { get; set; } = DateUtils.GetCurrentTimestamp();
        public bool AllowCancel { get; set; } = true;

        public static async Task SendMessageAsync(MultiPartMessage message, string text = "") {

            if (_message_registry.TryAdd(message.Context.User.Id, message)) {

                message.Timestamp = DateUtils.GetCurrentTimestamp();

                await message.Context.Channel.SendMessageAsync(string.IsNullOrEmpty(text) ? message.Text : text);

            }

        }
        public static async Task<bool> HandleResponseAsync(SocketMessage message) {

            // If there is no multistage command in progress for the given user, do nothing.
            if (!_message_registry.ContainsKey(message.Author.Id))
                return false;

            // If we can't retrieve the multistage command data for this user, do nothing.
            if (!_message_registry.TryGetValue(message.Author.Id, out MultiPartMessage command))
                return false;

            // The response to a multistage command must occur in the same channel as the original command.
            if (message.Channel.Id != command.Context.Channel.Id)
                return false;

            string message_content = message.Content;

            if (command.AllowCancel && message_content.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                await BotUtils.ReplyAsync_Info(command.Context, "The command has been canceled.");
            else if (!(command.Callback is null))
                await command.Callback(new MultiPartMessageCallbackArgs(command, message_content));

            // Remove the multistage command data now that we've handled it. 
            _message_registry.TryRemove(message.Author.Id, out _);

            return true;

        }

        // Private members

        private static ConcurrentDictionary<ulong, MultiPartMessage> _message_registry = new ConcurrentDictionary<ulong, MultiPartMessage>();

    }

}