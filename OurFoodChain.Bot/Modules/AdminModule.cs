using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class AdminModule :
        OfcModuleBase {

        [Command("backup", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.ServerModerator)]
        public async Task Backup() {

            try {

                await DatabaseService.UploadDatabaseBackupAsync(Context.Channel, Context.Guild);

            }
            catch (Exception ex) {

                await ReplyErrorAsync(ex.Message);

                throw ex;

            }

        }

        [Command("restart", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.BotAdmin)]
        public async Task Restart() {

            await Bot.RestartAsync(Context.Channel);

        }

    }

}