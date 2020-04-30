using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Attributes {

    public sealed class RequireGuildChannelAttribute :
       PreconditionAttribute {

        // Public members

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services) {

            if (context.Channel is IGuildChannel)
                return await Task.FromResult(PreconditionResult.FromSuccess());
            else
                return await Task.FromResult(PreconditionResult.FromError("This command can only be used in guilds."));

        }

    }

}