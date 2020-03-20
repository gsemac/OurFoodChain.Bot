using Discord;
using Newtonsoft.Json;
using OurFoodChain.Discord.Bots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class OfcBotConfiguration :
        BotConfiguration,
        IOfcBotConfiguration {

        public ulong[] BotAdminUserIds { get; set; }
        public ulong[] ModRoleIds { get; set; }

        public ulong ScratchChannel { get; set; }
        public ulong ScratchServer { get; set; }
        public ulong[][] ReviewChannels { get; set; }

        public string WorldName { get; set; } = "";

        public string WikiUrlFormat { get; set; } = "";

        public bool SingleDatabase { get; set; } = true;

        public bool TrophiesEnabled { get; set; } = true;
        public bool GotchisEnabled { get; set; } = true;
        public bool GenerationsEnabled { get; set; } = false;
        public bool AdvancedCommandsEnabled { get; set; } = false;

        public PrivilegeLevel GetPrivilegeLevel(IUser user) {

            // If there are no privileges set up in the configuration file, grant all users full privileges.

            if ((BotAdminUserIds is null || BotAdminUserIds.Count() <= 0) &&
               (ModRoleIds is null || ModRoleIds.Count() <= 0))
                return 0;

            // Check for Bot Admin privileges.

            if (!(BotAdminUserIds is null) && BotAdminUserIds.Contains(user.Id))
                return PrivilegeLevel.BotAdmin;

            // Attempt to case the user to a guild user so we can get their roles.
            // If this is not possible, default privileges are assumed.

            if (user is IGuildUser guildUser) {

                // Check for Server Moderator privileges.

                if (!(ModRoleIds is null))
                    foreach (ulong id in ModRoleIds)
                        if (guildUser.RoleIds.Contains(id))
                            return PrivilegeLevel.ServerModerator;

            }

            // Return basic privilege level.

            return PrivilegeLevel.ServerMember;

        }
        public bool HasPrivilegeLevel(IUser user, PrivilegeLevel privilegeLevel) {

            return GetPrivilegeLevel(user) <= privilegeLevel;

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

    }

}