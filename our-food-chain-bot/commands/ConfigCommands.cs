using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Commands {

    [Group("config")]
    public class ConfigCommands :
        ModuleBase {

        [Command("set")]
        public async Task Set(string key, string value) {

            if (await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.BotAdmin)) {

                if (OurFoodChainBot.Instance.Config.SetProperty(key, value))
                    await BotUtils.ReplyAsync_Success(Context, string.Format("Successfully set **{0}** to **{1}**.", key, value));
                else
                    await BotUtils.ReplyAsync_Error(Context, string.Format("No setting with the name **{0}** exists.", key));

                // Reload the config to apply the changes.
                await OurFoodChainBot.Instance.ReloadConfigAsync();

            }

        }

        [Command("save")]
        public async Task Save() {

            if (await BotUtils.ReplyHasPrivilegeAsync(Context, PrivilegeLevel.BotAdmin)) {

                OurFoodChainBot.Instance.Config.Save("config.json");

                await BotUtils.ReplyAsync_Success(Context, "Successfully saved config to **config.json**.");

            }

        }

    }

}
