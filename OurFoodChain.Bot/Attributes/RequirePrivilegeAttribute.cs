using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace OurFoodChain.Bot {

    public enum PrivilegeLevel {
        BotAdmin = 0,
        ServerAdmin,
        ServerModerator,
        ServerMember
    }

}

namespace OurFoodChain.Bot.Attributes {

    public sealed class RequirePrivilegeAttribute :
        PreconditionAttribute {

        // Public members

        public PrivilegeLevel PrivilegeLevel { get; set; }

        public RequirePrivilegeAttribute(PrivilegeLevel privilegeLevel) => PrivilegeLevel = privilegeLevel;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services) {

            IOfcBotConfiguration botConfiguration = services.GetRequiredService<IOfcBotConfiguration>();

            if (botConfiguration.HasPrivilegeLevel(context.User, PrivilegeLevel))
                return Task.FromResult(PreconditionResult.FromSuccess());
            else
                return Task.FromResult(PreconditionResult.FromError(string.Format("You must have **{0}** privileges to use this command.", OfcBotConfiguration.PrivilegeLevelToString(PrivilegeLevel))));

        }

        // Private members

        private static PrivilegeLevel GetPrivilegeLevel(CommandInfo commandInfo) {

            if (commandInfo != null) {

                foreach (Attribute attribute in commandInfo.Preconditions) {

                    if (attribute is RequirePrivilegeAttribute privilege_attribute)
                        return privilege_attribute.PrivilegeLevel;

                }

            }

            return PrivilegeLevel.ServerMember;

        }

    }

}