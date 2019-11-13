using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class CommandUtils {

        public static PrivilegeLevel GetPrivilegeLevel(IUser user) {

            // If there are no privileges set up in the configuration file, grant all users full privileges.

            Bot.BotConfig config = OurFoodChainBot.Instance.Config;

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

    }

}