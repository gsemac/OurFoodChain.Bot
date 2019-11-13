using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class RequirePrivilegeAttribute :
        PreconditionAttribute {

        // Public members

        public PrivilegeLevel PrivilegeLevel { get; set; }

        public RequirePrivilegeAttribute(PrivilegeLevel privilegeLevel) => PrivilegeLevel = privilegeLevel;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services) {

            if (Bot.CommandUtils.HasPrivilege(context.User, PrivilegeLevel))
                return Task.FromResult(PreconditionResult.FromSuccess());
            else
                return Task.FromResult(PreconditionResult.FromError(string.Format("You must have **{0}** privileges to use this command.", Bot.CommandUtils.PrivilegeLevelToString(PrivilegeLevel))));

        }

    }

}