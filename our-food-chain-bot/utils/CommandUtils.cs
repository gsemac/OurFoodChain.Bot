using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public enum PrivilegeLevel {
        Moderator
    }

    class CommandUtils {

        public class PaginatedMessage {

            public List<Embed> pages = new List<Embed>();
            public int index = 0;

            public string emojiPrev = "◀";
            public string emojiNext = "▶";
            public string emojiToggle;

        }

        public static Dictionary<ulong, PaginatedMessage> PAGINATED_MESSAGES = new Dictionary<ulong, PaginatedMessage>();

        public static async Task ReplyAsync_SendPaginatedMessage(ICommandContext context, PaginatedMessage message) {

            // If the message does not have any pages, quit.

            if (message.pages.Count() <= 0)
                return;

            IUserMessage msg = await context.Channel.SendMessageAsync("", false, message.pages[0]);

            // Only add reactions if there's more than one page.

            if (message.pages.Count() > 1) {

                if (!string.IsNullOrEmpty(message.emojiPrev))
                    await msg.AddReactionAsync(new Emoji(message.emojiPrev));

                if (!string.IsNullOrEmpty(message.emojiNext))
                    await msg.AddReactionAsync(new Emoji(message.emojiNext));

                if (!string.IsNullOrEmpty(message.emojiToggle))
                    await msg.AddReactionAsync(new Emoji(message.emojiToggle));

                PAGINATED_MESSAGES.Add(msg.Id, message);

            }

        }

        public static async Task HandlePaginatedMessageReaction(Cacheable<IUserMessage, ulong> cached, ISocketMessageChannel channel, SocketReaction reaction, bool added) {

            // If this reaction wasn't performed on a paginated message, quit.

            if (!PAGINATED_MESSAGES.ContainsKey(reaction.MessageId))
                return;

            // If the reaction was added by the bot, quit.

            if (reaction.UserId == OurFoodChainBot.GetInstance().GetUserId())
                return;

            // Get the paginated message data.

            PaginatedMessage message = PAGINATED_MESSAGES[reaction.MessageId];

            if (message.pages is null || message.pages.Count() <= 0)
                return;

            string emote = reaction.Emote.Name;
            int index_prev = message.index;

            if (emote == message.emojiNext || (emote == message.emojiToggle && added)) {

                if (++message.index >= message.pages.Count())
                    message.index = 0;

            }
            else if (emote == message.emojiPrev || (emote == message.emojiToggle && !added)) {

                if (message.index <= 0)
                    message.index = Math.Max(0, message.pages.Count() - 1);
                else
                    --message.index;

            }

            if (index_prev != message.index)
                await cached.DownloadAsync().Result.ModifyAsync(msg => msg.Embed = message.pages[message.index]);

        }

        public static bool CheckPrivilege(ICommandContext context, IGuildUser user, PrivilegeLevel level) {

            if (level == PrivilegeLevel.Moderator) {

                foreach (ulong id in OurFoodChainBot.GetInstance().GetConfig().mod_role_ids)
                    if (user.RoleIds.Contains(id))
                        return true;

            }

            return false;

        }

    }

}