using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public enum DifficultyLevel {
        Basic,
        Advanced
    }

    public class DifficultyLevelAttribute :
        PreconditionAttribute {

        public DifficultyLevel DifficultyLevel { get; set; }

        public DifficultyLevelAttribute(DifficultyLevel difficultyLevel) {
            DifficultyLevel = difficultyLevel;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services) {

            if (OurFoodChainBot.Instance.Config.AdvancedCommandsEnabled)
                return Task.FromResult(PreconditionResult.FromSuccess());
            else
                return Task.FromResult(PreconditionResult.FromError("`advanced_commands_enabled` must be set to `true` to use this command."));

        }

    }

}