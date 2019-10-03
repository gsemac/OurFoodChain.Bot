using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class CommandUtils {

        public const int MAX_EMBED_LENGTH = 2048;

        private const int MAX_PAGINATED_MESSAGES = 50;

        public class PaginatedMessage {

            public List<Embed> pages = new List<Embed>();
            public int index = 0;

            public string emojiPrev = "◀";
            public string emojiNext = "▶";
            public string emojiToggle;
            public bool paginationEnabled = true;
            public Action<PaginatedMessageCallbackArgs> callback;
            public string message = "";

            public ICommandContext Context { get; set; } = null;
            public bool RespondToSenderOnly { get; set; } = false;
            public bool Enabled { get; set; } = true;

        }

        public static Dictionary<ulong, PaginatedMessage> PAGINATED_MESSAGES = new Dictionary<ulong, PaginatedMessage>();

        public static async Task SendMessageAsync(ICommandContext context, PaginatedMessage message, string defaultMessage = "", bool respondToSenderOnly = false) {

            // If the message does not have any pages and does not have a message, quit.

            if (message.pages.Count() <= 0 && string.IsNullOrEmpty(message.message)) {

                if (!string.IsNullOrEmpty(defaultMessage))
                    await BotUtils.ReplyAsync_Info(context, defaultMessage);

                return;

            }

            message.Context = context;
            message.RespondToSenderOnly = respondToSenderOnly;

            IUserMessage msg = await context.Channel.SendMessageAsync(message.message, false, message.pages.Count() > 0 ? message.pages[0] : null);

            // Only add reactions if there's more than one page.

            if (message.pages.Count() > 1 || !(message.callback is null)) {

                if (message.pages.Count() > 1 && !string.IsNullOrEmpty(message.emojiPrev))
                    await msg.AddReactionAsync(new Emoji(message.emojiPrev));

                if (message.pages.Count() > 1 && !string.IsNullOrEmpty(message.emojiNext))
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

            if (reaction.UserId == OurFoodChainBot.Instance.UserId)
                return;

            // Get the paginated message data.

            PaginatedMessage message = PAGINATED_MESSAGES[reaction.MessageId];

            if (!message.Enabled)
                return;

            // Ignore the reaction if it's not from the sender and we only accept reactions from the sender.

            if (message.RespondToSenderOnly && reaction.UserId != message.Context.User.Id)
                return;

            string emote = reaction.Emote.Name;
            int index_prev = message.index;
            bool pagination_enabled = message.paginationEnabled;

            if (!(message.callback is null))
                message.callback(new PaginatedMessageCallbackArgs {
                    DiscordMessage = await cached.DownloadAsync(),
                    PaginatedMessage = message,
                    ReactionAdded = added,
                    Reaction = emote
                });

            if (message.pages is null || message.pages.Count() <= 0)
                return;

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

        public static PrivilegeLevel GetPrivilegeLevel(IUser user) {

            // If there are no privileges set up in the configuration file, grant all users full privileges.

            Config config = OurFoodChainBot.Instance.Config;

            if ((config.BotAdminUserIds is null || config.BotAdminUserIds.Count() <= 0) &&
               (config.ModRoleIds is null || config.ModRoleIds.Count() <= 0))
                return 0;

            // Check for Bot Admin privileges.

            if (!(config.BotAdminUserIds is null) && config.BotAdminUserIds.Contains(user.Id))
                return PrivilegeLevel.BotAdmin;

            // Attempt to case the user to a guild user so we can get their roles.
            // If this is not possible, default privileges are assumed.

            IGuildUser g_user = user as IGuildUser;

            if (!(g_user is null)) {

                // Check for Server Moderator privileges.

                if (!(config.ModRoleIds is null))
                    foreach (ulong id in config.ModRoleIds)
                        if (g_user.RoleIds.Contains(id))
                            return PrivilegeLevel.ServerModerator;

            }

            // Return basic privilege level.

            return PrivilegeLevel.ServerMember;

        }
        public static bool HasPrivilege(IUser user, PrivilegeLevel level) {

            return GetPrivilegeLevel(user) <= level;

        }
        public static PrivilegeLevel GetPrivilegeLevel(CommandInfo commandInfo) {

            if (commandInfo != null) {

                foreach (Attribute attribute in commandInfo.Preconditions) {

                    if (attribute is RequirePrivilegeAttribute privilege_attribute)
                        return privilege_attribute.PrivilegeLevel;

                }

            }

            return PrivilegeLevel.ServerMember;

        }
        public static DifficultyLevel GetDifficultyLevel(CommandInfo commandInfo) {

            if (commandInfo != null) {

                foreach (Attribute attribute in commandInfo.Preconditions) {

                    if (attribute is DifficultyLevelAttribute difficultyLevelAttribute)
                        return difficultyLevelAttribute.DifficultyLevel;

                }

            }

            return DifficultyLevel.Basic;

        }

        public static string PrivilegeLevelToString(PrivilegeLevel privilegeLevel) {

            switch (privilegeLevel) {

                case PrivilegeLevel.BotAdmin:
                    return "Bot Admin";

                case PrivilegeLevel.ServerAdmin:
                    return "Admin";

                case PrivilegeLevel.ServerMember:
                    return "Member";

                case PrivilegeLevel.ServerModerator:
                    return "Moderator";

                default:
                    return "Unknown";

            }

        }

        public static async Task<bool> CommandIsEnabledAsync(ICommandContext context, string commandName) {

            CommandInfo commandInfo = OurFoodChainBot.Instance.GetInstalledCommandByName(commandName);

            if (commandInfo is null)
                return false;

            if (!HasPrivilege(context.User, GetPrivilegeLevel(commandInfo)))
                return false;

            if (!OurFoodChainBot.Instance.Config.AdvancedCommandsEnabled && GetDifficultyLevel(commandInfo) >= DifficultyLevel.Advanced)
                return false;

            if (!(await commandInfo.CheckPreconditionsAsync(context, OurFoodChainBot.Instance.ServiceProvider)).IsSuccess)
                return false;

            return true;

        }

        public static async Task<IUser> GetUserFromUsernameOrMentionAsync(ICommandContext context, string usernameOrMention) {

            if (context is null || context.Guild is null)
                return null;

            IReadOnlyCollection<IGuildUser> users = await context.Guild.GetUsersAsync();

            foreach (IGuildUser user in users)
                if (_userMatchesUsernameOrMention(user, usernameOrMention))
                    return user;

            return null;

        }
        private static bool _userMatchesUsernameOrMention(IGuildUser user, string usernameOrMention) {

            if (user is null)
                return false;

            usernameOrMention = usernameOrMention.ToLower();

            string username = string.IsNullOrEmpty(user.Username) ? string.Empty : user.Username.ToLower();
            string nickname = string.IsNullOrEmpty(user.Nickname) ? string.Empty : user.Nickname.ToLower();
            string full_username = string.Format("{0}#{1}", username, user.Discriminator).ToLower();

            return username == usernameOrMention ||
                nickname == usernameOrMention ||
                full_username == usernameOrMention ||
                Regex.Matches(usernameOrMention, @"^<@(\d+)\>$").Cast<Match>().Any(x => x.Groups[1].Value == user.Id.ToString());

        }

    }

}