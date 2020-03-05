using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord.Services {

    public class PaginatedMessageService :
        IPaginatedMessageService {

        // Public members

        public int MaxPaginatedMessages { get; set; } = 50;

        public PaginatedMessageService(DiscordSocketClient client) {

            this.client = client;

            client.ReactionAdded += ReactionAddedAsync;
            client.ReactionRemoved += ReactionRemovedAsync;

        }

        public async Task SendMessageAsync(ICommandContext context, IPaginatedMessage message) {

            if (message.Count() > 0) {

                IUserMessage sentMessage = await context.Channel.SendMessageAsync(message.First().Text, false, message.First().Embed.ToDiscordEmbed());

                foreach (string reaction in message.Reactions)
                    await sentMessage.AddReactionAsync(new Emoji(reaction));

                if (paginatedMessages.Count() >= MaxPaginatedMessages) {

                    // Remove the oldest message.

                    ulong oldestMessageId = paginatedMessages.Keys.Min();

                    paginatedMessages.TryRemove(oldestMessageId, out _);

                }

                paginatedMessages.TryAdd(sentMessage.Id, new PaginatedMessageInfo {
                    SentMessage = sentMessage,
                    Context = context,
                    Message = message
                });

            }

        }

        // Private members

        private class PaginatedMessageInfo {

            public IUserMessage SentMessage { get; set; }
            public IPaginatedMessage Message { get; set; }
            public ICommandContext Context { get; set; }

        }

        private readonly DiscordSocketClient client;
        private readonly ConcurrentDictionary<ulong, PaginatedMessageInfo> paginatedMessages = new ConcurrentDictionary<ulong, PaginatedMessageInfo>();

        private async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> cached, ISocketMessageChannel channel, SocketReaction reaction) {

            await ReactionChangedAsync(cached, channel, reaction, true);

        }
        private async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> cached, ISocketMessageChannel channel, SocketReaction reaction) {

            await ReactionChangedAsync(cached, channel, reaction, false);

        }
        private async Task ReactionChangedAsync(Cacheable<IUserMessage, ulong> cached, ISocketMessageChannel channel, SocketReaction reaction, bool added) {

            // If this reaction wasn't performed on a paginated message, quit.

            if (!paginatedMessages.ContainsKey(reaction.MessageId))
                return;

            // If the reaction was added by the bot, quit.

            if (reaction.UserId == client.CurrentUser.Id)
                return;

            // Get the paginated message.

            PaginatedMessageInfo messageInfo = paginatedMessages[reaction.MessageId];

            if (!messageInfo.Message.Enabled)
                return;

            // Ignore the reaction if it's not from the sender and we only accept reactions from the sender.

            if (messageInfo.Message.Restricted && reaction.UserId != messageInfo.Context.User.Id)
                return;

            string emoji = reaction.Emote.Name;
            Messaging.IMessage currentPage = messageInfo.Message.CurrentPage;

            await messageInfo.Message.HandleReactionAsync(new PaginatedMessageReactionArgs(messageInfo.Message, emoji, added));

            if (currentPage != messageInfo.Message.CurrentPage) {

                await cached.DownloadAsync().Result.ModifyAsync(msg => {

                    msg.Content = messageInfo.Message.CurrentPage.Text;
                    msg.Embed = messageInfo.Message.CurrentPage.Embed.ToDiscordEmbed();

                });

            }

        }

    }

}