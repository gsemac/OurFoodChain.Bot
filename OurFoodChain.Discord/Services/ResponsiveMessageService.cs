using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Messaging;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IMessage = Discord.IMessage;

namespace OurFoodChain.Discord.Services {

    public class ResponsiveMessageService :
        IResponsiveMessageService {

        // Public members

        public int MaxResponsiveMessages { get; set; } = 50;
        public string CancellationString { get; set; } = "cancel";

        public ResponsiveMessageService() {

            //client.MessageReceived += OnMessageReceivedAsync;

        }

        public async Task<IResponsiveMessageResponse> GetResponseAsync(ICommandContext context, string message, bool allowCancellation = true) {

            return await GetResponseAsync(context, new Message(message), allowCancellation);

        }
        public async Task<IResponsiveMessageResponse> GetResponseAsync(ICommandContext context, Messaging.IMessage message, bool allowCancellation = true) {

            ResponsiveMessageInfo info = await SendMessageAndReturnInfoAsync(context, message, allowCancellation);
            IResponsiveMessageResponse response = new ResponseMessageResponse(null, true);

            if (info != null) {

                // Block until we get a response.

                info.Waiter = new ManualResetEvent(false);
                info.Waiter.WaitOne();

                response = info.Response;

                if (response is null)
                    response = new ResponseMessageResponse(null, true);

                if (response.Canceled)
                    await context.Channel.SendMessageAsync(embed: EmbedUtilities.BuildInfoEmbed("The command has been canceled.").ToDiscordEmbed());

            }

            return response;

        }

        public async Task<bool> HandleMessageAsync(IMessage message) {

            ResponsiveMessageInfo info = messages.GetOrDefault(message.Author.Id);

            if (info != null && message.Channel.Id == info.Context.Channel.Id) {

                Messaging.IMessage responseMessage = new Message(message.Content) {
                    Attachments = message.Attachments.Select(attachment => new Messaging.Attachment() { Url = attachment.Url, Filename = attachment.Filename })
                };

                info.Response = new ResponseMessageResponse(responseMessage, info.AllowCancellation && message.Content.Equals(CancellationString, StringComparison.OrdinalIgnoreCase));

                RemoveMessageInfo(message.Author.Id);

                info.Waiter.Set();

                return await Task.FromResult(true);

            }

            return await Task.FromResult(false);

        }

        // Private members

        private class ResponsiveMessageInfo {
            public bool AllowCancellation { get; set; } = true;
            public ICommandContext Context { get; set; }
            public ManualResetEvent Waiter { get; set; }
            public IResponsiveMessageResponse Response { get; set; }
        }

        private readonly ConcurrentDictionary<ulong, ResponsiveMessageInfo> messages = new ConcurrentDictionary<ulong, ResponsiveMessageInfo>();

        private async Task<ResponsiveMessageInfo> SendMessageAndReturnInfoAsync(ICommandContext context, Messaging.IMessage message, bool allowCancellation) {

            ResponsiveMessageInfo info = null;

            if (message != null) {

                if (allowCancellation)
                    message.Text += $"\nTo cancel, reply with \"{CancellationString}\".";

                IUserMessage sentMessage = await context.Channel.SendMessageAsync(message?.Text, false, message?.Embed?.ToDiscordEmbed());

                if (messages.Count() >= MaxResponsiveMessages) {

                    // Remove the oldest message.

                    ulong oldestMessageUserId = messages
                        .OrderBy(pair => pair.Value.Context.Message.Timestamp)
                        .Select(pair => pair.Key)
                        .First();

                    RemoveMessageInfo(oldestMessageUserId);

                }

                info = new ResponsiveMessageInfo {
                    AllowCancellation = allowCancellation,
                    Context = context
                };

                if (messages.TryAdd(context.User.Id, info))
                    return info;

            }

            return info;

        }

        private void RemoveMessageInfo(ulong userId) {

            if (messages.TryRemove(userId, out ResponsiveMessageInfo removedInfo)) {

                // Unblock the message.

                removedInfo.Waiter?.Set();
                removedInfo.Waiter?.Dispose();

            }

        }

    }

}