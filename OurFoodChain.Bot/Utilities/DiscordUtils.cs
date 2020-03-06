using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public static class DiscordUtils {

        // Public members

        public const int MaxFieldLength = 1024;
        public const int MaxMessageLength = 2000;
        public const int MaxFieldCount = 25;
        public const int MaxEmbedLength = 2048;

        public const int MaxPaginatedMessages = 50;

        public static async Task<IMessage[]> DownloadAllMessagesAsync(IMessageChannel channel, int limit = 0) {

            List<IMessage> messages = new List<IMessage>();

            if (channel is null)
                return messages.ToArray();

            IEnumerable<IMessage> next_messages = await channel.GetMessagesAsync().FlattenAsync();

            while (next_messages.Count() > 0 && (limit <= 0 || messages.Count() < limit)) {

                if (next_messages.Count() > 0)
                    messages.AddRange(next_messages);

                if (messages.Count() > 0)
                    next_messages = await channel.GetMessagesAsync(messages.Last(), Direction.Before).FlattenAsync();

            }

            messages.Reverse();

            if (limit <= 0)
                return messages.ToArray();
            else
                return messages.Take(limit).ToArray();

        }

        public static async Task SendMessageAsync(ICommandContext context, PaginatedMessage message, string defaultMessage = "", bool respondToSenderOnly = false) {

            // If the message does not have any pages and does not have a message, quit.

            if (message.Pages.Count() <= 0 && string.IsNullOrEmpty(message.Message)) {

                if (!string.IsNullOrEmpty(defaultMessage))
                    await BotUtils.ReplyAsync_Info(context, defaultMessage);

                return;

            }

            message.Context = context;
            message.RespondToSenderOnly = respondToSenderOnly;

            IUserMessage msg = await context.Channel.SendMessageAsync(message.Message, false, message.Pages.Count() > 0 ? message.Pages[0] : null);

            // Only add reactions if there's more than one page.

            if (message.Pages.Count() > 1 || !(message.ReactionCallback is null)) {

                if (message.Pages.Count() > 1 && !string.IsNullOrEmpty(message.PrevEmoji))
                    await msg.AddReactionAsync(new Emoji(message.PrevEmoji));

                if (message.Pages.Count() > 1 && !string.IsNullOrEmpty(message.NextEmoji))
                    await msg.AddReactionAsync(new Emoji(message.NextEmoji));

                if (!string.IsNullOrEmpty(message.ToggleEmoji))
                    await msg.AddReactionAsync(new Emoji(message.ToggleEmoji));

                paginatedMessages.Add(msg.Id, message);

            }

            // If there are now over the maximum number of paginated messages, delete an old one.

            while (paginatedMessages.Count > MaxPaginatedMessages) {

                ulong oldest_message_id = paginatedMessages.Keys.Min();

                paginatedMessages.Remove(oldest_message_id);

            }

        }
        public static async Task HandlePaginatedMessageReactionAsync(Cacheable<IUserMessage, ulong> cached, DiscordSocketClient discordClient, ISocketMessageChannel channel, SocketReaction reaction, bool added) {

            // If this reaction wasn't performed on a paginated message, quit.

            if (!paginatedMessages.ContainsKey(reaction.MessageId))
                return;

            // If the reaction was added by the bot, quit.

            if (reaction.UserId == discordClient.CurrentUser.Id)
                return;

            // Get the paginated message data.

            PaginatedMessage message = paginatedMessages[reaction.MessageId];

            if (!message.Enabled)
                return;

            // Ignore the reaction if it's not from the sender and we only accept reactions from the sender.

            if (message.RespondToSenderOnly && reaction.UserId != message.Context.User.Id)
                return;

            string emote = reaction.Emote.Name;
            int index_prev = message.PageIndex;
            bool pagination_enabled = message.PaginationEnabled;

            message.ReactionCallback?.Invoke(new Bot.PaginatedMessageReactionCallbackArgs {
                DiscordMessage = await cached.DownloadAsync(),
                PaginatedMessage = message,
                ReactionAdded = added,
                Reaction = emote
            });

            if (message.Pages is null || message.Pages.Count() <= 0)
                return;

            if (!pagination_enabled || !message.PaginationEnabled)
                return;

            if (emote == message.NextEmoji || (emote == message.ToggleEmoji && added)) {

                if (++message.PageIndex >= message.Pages.Count())
                    message.PageIndex = 0;

            }
            else if (emote == message.PrevEmoji || (emote == message.ToggleEmoji && !added)) {

                if (message.PageIndex <= 0)
                    message.PageIndex = Math.Max(0, message.Pages.Count() - 1);
                else
                    --message.PageIndex;

            }

            if (index_prev != message.PageIndex)
                await cached.DownloadAsync().Result.ModifyAsync(msg => msg.Embed = message.Pages[message.PageIndex]);

        }

        public static async Task SendMessageAsync(ICommandContext context, MultiPartMessage message, string text = "") {

            if (multiPartMessages.TryAdd(context.User.Id, message)) {

                message.Timestamp = DateUtilities.GetCurrentTimestampUtc();

                await context.Channel.SendMessageAsync(string.IsNullOrEmpty(text) ? message.Text : text);

            }

        }
        public static async Task<bool> HandleMultiPartMessageResponseAsync(SocketMessage message) {

            // If there is no multistage command in progress for the given user, do nothing.
            if (!multiPartMessages.ContainsKey(message.Author.Id))
                return false;

            // If we can't retrieve the multistage command data for this user, do nothing.
            if (!multiPartMessages.TryGetValue(message.Author.Id, out MultiPartMessage command))
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
            multiPartMessages.TryRemove(message.Author.Id, out _);

            return true;

        }

        public static async Task<IUser> GetUserFromUsernameOrMentionAsync(ICommandContext context, string usernameOrMention) {

            if (context is null || context.Guild is null)
                return null;

            IReadOnlyCollection<IGuildUser> users = await context.Guild.GetUsersAsync();

            foreach (IGuildUser user in users)
                if (UserMatchesUsernameOrMention(user, usernameOrMention))
                    return user;

            return null;

        }

        public static Color ConvertColor(System.Drawing.Color color) {
            return new Color(color.R, color.G, color.B);
        }
        public static bool IsEmoji(string input) {

            // This regex was taken from https://stackoverflow.com/a/40950241/5383169

            string pattern = "(?:0\x20E3|1\x20E3|2\x20E3|3\x20E3|4\x20E3|5\x20E3|6\x20E3|7\x20E3|8\x20E3|9\x20E3|#\x20E3|\\*\x20E3|\xD83C(?:\xDDE6\xD83C(?:\xDDE8|\xDDE9|\xDDEA|\xDDEB|\xDDEC|\xDDEE|\xDDF1|\xDDF2|\xDDF4|\xDDF6|\xDDF7|\xDDF8|\xDDF9|\xDDFA|\xDDFC|\xDDFD|\xDDFF)|\xDDE7\xD83C(?:\xDDE6|\xDDE7|\xDDE9|\xDDEA|\xDDEB|\xDDEC|\xDDED|\xDDEE|\xDDEF|\xDDF1|\xDDF2|\xDDF3|\xDDF4|\xDDF6|\xDDF7|\xDDF8|\xDDF9|\xDDFB|\xDDFC|\xDDFE|\xDDFF)|\xDDE8\xD83C(?:\xDDE6|\xDDE8|\xDDE9|\xDDEB|\xDDEC|\xDDED|\xDDEE|\xDDF0|\xDDF1|\xDDF2|\xDDF3|\xDDF4|\xDDF5|\xDDF7|\xDDFA|\xDDFB|\xDDFC|\xDDFD|\xDDFE|\xDDFF)|\xDDE9\xD83C(?:\xDDEA|\xDDEC|\xDDEF|\xDDF0|\xDDF2|\xDDF4|\xDDFF)|\xDDEA\xD83C(?:\xDDE6|\xDDE8|\xDDEA|\xDDEC|\xDDED|\xDDF7|\xDDF8|\xDDF9|\xDDFA)|\xDDEB\xD83C(?:\xDDEE|\xDDEF|\xDDF0|\xDDF2|\xDDF4|\xDDF7)|\xDDEC\xD83C(?:\xDDE6|\xDDE7|\xDDE9|\xDDEA|\xDDEB|\xDDEC|\xDDED|\xDDEE|\xDDF1|\xDDF2|\xDDF3|\xDDF5|\xDDF6|\xDDF7|\xDDF8|\xDDF9|\xDDFA|\xDDFC|\xDDFE)|\xDDED\xD83C(?:\xDDF0|\xDDF2|\xDDF3|\xDDF7|\xDDF9|\xDDFA)|\xDDEE\xD83C(?:\xDDE8|\xDDE9|\xDDEA|\xDDF1|\xDDF2|\xDDF3|\xDDF4|\xDDF6|\xDDF7|\xDDF8|\xDDF9)|\xDDEF\xD83C(?:\xDDEA|\xDDF2|\xDDF4|\xDDF5)|\xDDF0\xD83C(?:\xDDEA|\xDDEC|\xDDED|\xDDEE|\xDDF2|\xDDF3|\xDDF5|\xDDF7|\xDDFC|\xDDFE|\xDDFF)|\xDDF1\xD83C(?:\xDDE6|\xDDE7|\xDDE8|\xDDEE|\xDDF0|\xDDF7|\xDDF8|\xDDF9|\xDDFA|\xDDFB|\xDDFE)|\xDDF2\xD83C(?:\xDDE6|\xDDE8|\xDDE9|\xDDEA|\xDDEB|\xDDEC|\xDDED|\xDDF0|\xDDF1|\xDDF2|\xDDF3|\xDDF4|\xDDF5|\xDDF6|\xDDF7|\xDDF8|\xDDF9|\xDDFA|\xDDFB|\xDDFC|\xDDFD|\xDDFE|\xDDFF)|\xDDF3\xD83C(?:\xDDE6|\xDDE8|\xDDEA|\xDDEB|\xDDEC|\xDDEE|\xDDF1|\xDDF4|\xDDF5|\xDDF7|\xDDFA|\xDDFF)|\xDDF4\xD83C\xDDF2|\xDDF5\xD83C(?:\xDDE6|\xDDEA|\xDDEB|\xDDEC|\xDDED|\xDDF0|\xDDF1|\xDDF2|\xDDF3|\xDDF7|\xDDF8|\xDDF9|\xDDFC|\xDDFE)|\xDDF6\xD83C\xDDE6|\xDDF7\xD83C(?:\xDDEA|\xDDF4|\xDDF8|\xDDFA|\xDDFC)|\xDDF8\xD83C(?:\xDDE6|\xDDE7|\xDDE8|\xDDE9|\xDDEA|\xDDEC|\xDDED|\xDDEE|\xDDEF|\xDDF0|\xDDF1|\xDDF2|\xDDF3|\xDDF4|\xDDF7|\xDDF8|\xDDF9|\xDDFB|\xDDFD|\xDDFE|\xDDFF)|\xDDF9\xD83C(?:\xDDE6|\xDDE8|\xDDE9|\xDDEB|\xDDEC|\xDDED|\xDDEF|\xDDF0|\xDDF1|\xDDF2|\xDDF3|\xDDF4|\xDDF7|\xDDF9|\xDDFB|\xDDFC|\xDDFF)|\xDDFA\xD83C(?:\xDDE6|\xDDEC|\xDDF2|\xDDF8|\xDDFE|\xDDFF)|\xDDFB\xD83C(?:\xDDE6|\xDDE8|\xDDEA|\xDDEC|\xDDEE|\xDDF3|\xDDFA)|\xDDFC\xD83C(?:\xDDEB|\xDDF8)|\xDDFD\xD83C\xDDF0|\xDDFE\xD83C(?:\xDDEA|\xDDF9)|\xDDFF\xD83C(?:\xDDE6|\xDDF2|\xDDFC)))|[\xA9\xAE\x203C\x2049\x2122\x2139\x2194-\x2199\x21A9\x21AA\x231A\x231B\x2328\x23CF\x23E9-\x23F3\x23F8-\x23FA\x24C2\x25AA\x25AB\x25B6\x25C0\x25FB-\x25FE\x2600-\x2604\x260E\x2611\x2614\x2615\x2618\x261D\x2620\x2622\x2623\x2626\x262A\x262E\x262F\x2638-\x263A\x2648-\x2653\x2660\x2663\x2665\x2666\x2668\x267B\x267F\x2692-\x2694\x2696\x2697\x2699\x269B\x269C\x26A0\x26A1\x26AA\x26AB\x26B0\x26B1\x26BD\x26BE\x26C4\x26C5\x26C8\x26CE\x26CF\x26D1\x26D3\x26D4\x26E9\x26EA\x26F0-\x26F5\x26F7-\x26FA\x26FD\x2702\x2705\x2708-\x270D\x270F\x2712\x2714\x2716\x271D\x2721\x2728\x2733\x2734\x2744\x2747\x274C\x274E\x2753-\x2755\x2757\x2763\x2764\x2795-\x2797\x27A1\x27B0\x27BF\x2934\x2935\x2B05-\x2B07\x2B1B\x2B1C\x2B50\x2B55\x3030\x303D\x3297\x3299]|\xD83C[\xDC04\xDCCF\xDD70\xDD71\xDD7E\xDD7F\xDD8E\xDD91-\xDD9A\xDE01\xDE02\xDE1A\xDE2F\xDE32-\xDE3A\xDE50\xDE51\xDF00-\xDF21\xDF24-\xDF93\xDF96\xDF97\xDF99-\xDF9B\xDF9E-\xDFF0\xDFF3-\xDFF5\xDFF7-\xDFFF]|\xD83D[\xDC00-\xDCFD\xDCFF-\xDD3D\xDD49-\xDD4E\xDD50-\xDD67\xDD6F\xDD70\xDD73-\xDD79\xDD87\xDD8A-\xDD8D\xDD90\xDD95\xDD96\xDDA5\xDDA8\xDDB1\xDDB2\xDDBC\xDDC2-\xDDC4\xDDD1-\xDDD3\xDDDC-\xDDDE\xDDE1\xDDE3\xDDEF\xDDF3\xDDFA-\xDE4F\xDE80-\xDEC5\xDECB-\xDED0\xDEE0-\xDEE5\xDEE9\xDEEB\xDEEC\xDEF0\xDEF3]|\xD83E[\xDD10-\xDD18\xDD80-\xDD84\xDDC0]";

            if (Regex.IsMatch(input, pattern))
                return true;

            // Check for custom server emojis as well.

            if (input.StartsWith("<") && input.EndsWith(">"))
                return true;

            return false;

        }

        // Private members

        private static Dictionary<ulong, PaginatedMessage> paginatedMessages = new Dictionary<ulong, PaginatedMessage>();
        private static ConcurrentDictionary<ulong, MultiPartMessage> multiPartMessages = new ConcurrentDictionary<ulong, MultiPartMessage>();

        private static bool UserMatchesUsernameOrMention(IGuildUser user, string usernameOrMention) {

            if (user is null)
                return false;

            string username = string.IsNullOrEmpty(user.Username) ? string.Empty : user.Username.ToLower();
            string nickname = string.IsNullOrEmpty(user.Nickname) ? string.Empty : user.Nickname.ToLower();
            string full_username = string.Format("{0}#{1}", username, user.Discriminator).ToLower();

            // Mentions may look like either of the following:
            // <@id>
            // <@!id>
            // The exclamation mark means that they have a nickname: https://stackoverflow.com/questions/45269613/discord-userid-vs-userid

            return username == usernameOrMention ||
                nickname == usernameOrMention ||
                full_username == usernameOrMention ||
                Regex.Matches(usernameOrMention, @"^<@!?(\d+)\>$").Cast<Match>().Any(x => x.Groups[1].Value == user.Id.ToString());

        }

    }

}