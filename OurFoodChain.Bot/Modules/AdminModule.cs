using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Discord.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class AdminModule :
        OfcModuleBase {

        [Command("backup", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.BotAdmin)]
        public async Task Backup() {

            await DatabaseService.UploadDatabaseBackupAsync(Context.Channel, Context.Guild.Id);

        }

    }

}