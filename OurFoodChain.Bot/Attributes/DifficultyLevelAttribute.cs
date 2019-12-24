using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OurFoodChain.Bot {

    public enum DifficultyLevel {
        Basic,
        Advanced
    }

}

namespace OurFoodChain.Bot.Attributes {

    public sealed class DifficultyLevelAttribute :
        PreconditionAttribute {

        // Public members

        public DifficultyLevel DifficultyLevel { get; set; }

        public DifficultyLevelAttribute(DifficultyLevel difficultyLevel) {
            DifficultyLevel = difficultyLevel;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services) {

            if (services.GetService<IOurFoodChainBotConfiguration>().AdvancedCommandsEnabled)
                return Task.FromResult(PreconditionResult.FromSuccess());
            else
                return Task.FromResult(PreconditionResult.FromError("`advanced_commands_enabled` must be set to `true` to use this command."));

        }

        // Private members

        private static DifficultyLevel GetDifficultyLevel(CommandInfo commandInfo) {

            if (commandInfo != null) {

                foreach (Attribute attribute in commandInfo.Preconditions) {

                    if (attribute is DifficultyLevelAttribute difficultyLevelAttribute)
                        return difficultyLevelAttribute.DifficultyLevel;

                }

            }

            return DifficultyLevel.Basic;

        }

    }

}