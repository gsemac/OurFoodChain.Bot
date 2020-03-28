using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace OurFoodChain.Attributes {

    public sealed class RequireDMChannelAttribute :
       PreconditionAttribute {

        // Public members

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services) {

            if (context.Channel is IDMChannel)
                return Task.FromResult(PreconditionResult.FromSuccess());
            else
                return Task.FromResult(PreconditionResult.FromError("This command can only be used in DMs."));

        }

    }

}