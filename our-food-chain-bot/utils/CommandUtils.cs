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
        BotAdmin = 0,
        ServerAdmin,
        ServerModerator,
        ServerMember
    }

    public class CommandUtils {

        private const int MAX_PAGINATED_MESSAGES = 50;

        public class PaginatedMessageCallbackArgs {

            public IUserMessage discordMessage = null;
            public PaginatedMessage paginatedMessage = null;
            public bool on = false;
            public string reaction = "";

        }

        public class PaginatedMessage {

            public List<Embed> pages = new List<Embed>();
            public int index = 0;

            public string emojiPrev = "◀";
            public string emojiNext = "▶";
            public string emojiToggle;
            public bool paginationEnabled = true;
            public Action<PaginatedMessageCallbackArgs> callback;

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

            // If there are now over the maximum number of paginated messages, delete an old one.

            while (PAGINATED_MESSAGES.Count > MAX_PAGINATED_MESSAGES) {

                ulong oldest_message_id = PAGINATED_MESSAGES.Keys.Min();

                PAGINATED_MESSAGES.Remove(oldest_message_id);

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
            bool pagination_enabled = message.paginationEnabled;

            if (!(message.callback is null))
                message.callback(new PaginatedMessageCallbackArgs {
                    discordMessage = (await cached.DownloadAsync()),
                    paginatedMessage = message,
                    on = added,
                    reaction = emote
                });

            if (!pagination_enabled || !message.paginationEnabled)
                return;

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

        public static PrivilegeLevel GetPrivilegeLevel(IGuildUser user) {

            // If there are no privileges set up in the configuration file, grant all users full privileges.

            OurFoodChainBot.Config config = OurFoodChainBot.GetInstance().GetConfig();

            if ((config.bot_admin_user_ids is null || config.bot_admin_user_ids.Count() <= 0) &&
               (config.mod_role_ids is null || config.mod_role_ids.Count() <= 0))
                return 0;

            // Check for Bot Admin privileges.

            if (!(config.bot_admin_user_ids is null) && config.bot_admin_user_ids.Contains(user.Id))
                return PrivilegeLevel.BotAdmin;

            // Check for Server Moderator privileges.

            if (!(config.mod_role_ids is null))
                foreach (ulong id in config.mod_role_ids)
                    if (user.RoleIds.Contains(id))
                        return PrivilegeLevel.ServerModerator;

            // Return basic privilege level.

            return PrivilegeLevel.ServerMember;

        }
        public static bool CheckPrivilege(IGuildUser user, PrivilegeLevel level) {

            return GetPrivilegeLevel(user) <= level;

        }

    }

}