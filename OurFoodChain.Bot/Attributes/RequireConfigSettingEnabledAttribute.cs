using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Attributes {

    public sealed class RequireConfigSettingEnabledAttribute :
        PreconditionAttribute {

        // Public members

        public string SettingName { get; set; }

        public RequireConfigSettingEnabledAttribute(string settingName) => SettingName = settingName;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services) {

            if (services.GetService<IOfcBotConfiguration>().GetProperty(SettingName, false))
                return Task.FromResult(PreconditionResult.FromSuccess());
            else
                return Task.FromResult(PreconditionResult.FromError(string.Format("You must enable the `{0}` setting to use this command.", SettingName)));

        }

    }

}