using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Discord.Commands;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Modules {

    public class HelpModule :
        ModuleBase {

        // Public members

        public IOfcBotConfiguration BotConfiguration { get; set; }
        public Discord.Services.ICommandHandlingService CommandHandlingService { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
        public Discord.Services.IHelpService HelpService { get; set; }

        [Command("help"), Alias("h")]
        public async Task Help() {

            IEnumerable<ICommandHelpInfo> helpInfos = await HelpService.GetCommandHelpInfoAsync(Context);

            await ReplyAsync(embed: EmbedUtilities.BuildCommandHelpInfoEmbed(helpInfos, BotConfiguration).ToDiscordEmbed());

        }
        [Command("help"), Alias("h")]
        public async Task Help([Remainder]string commandName) {

            ICommandHelpInfo helpInfo = await HelpService.GetCommandHelpInfoAsync(commandName.Trim());

            await ReplyAsync(embed: EmbedUtilities.BuildCommandHelpInfoEmbed(helpInfo, BotConfiguration).ToDiscordEmbed());

        }

    }

}