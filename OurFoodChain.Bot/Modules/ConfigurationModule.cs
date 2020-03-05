using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot.Modules {

    [Group("config")]
    public class ConfigurationModule :
        ModuleBase {

        public IOfcBotConfiguration BotConfiguration { get; set; }
        public Discord.Services.ICommandHandlingService CommandHandlingService { get; set; }
        public DiscordSocketClient DiscordClient { get; set; }

        [Command("set"), RequirePrivilege(PrivilegeLevel.BotAdmin)]
        public async Task Set(string key, string value) {

            if (BotConfiguration.SetProperty(key, value))
                await DiscordUtilities.ReplySuccessAsync(Context.Channel,
                    string.Format("Successfully set **{0}** to **{1}**.", key, value));
            else
                await DiscordUtilities.ReplyErrorAsync(Context.Channel,
                    string.Format("No setting with the name **{0}** exists.", key));

            // Reload commands (the commands available are dependent on configuration settings).

            await CommandHandlingService.InstallCommandsAsync();

            // Update the bot's "Playing" status.

            await DiscordClient.SetGameAsync(BotConfiguration.Playing);

        }

        [Command("save"), RequirePrivilege(PrivilegeLevel.BotAdmin)]
        public async Task Save() {

            string configFilename = "config.json";

            BotConfiguration.Save(configFilename);

            await DiscordUtilities.ReplySuccessAsync(Context.Channel,
                string.Format("Successfully saved config to **{0}**.", configFilename));

        }

    }

}