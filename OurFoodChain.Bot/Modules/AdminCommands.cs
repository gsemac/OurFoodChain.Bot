using Discord.Commands;
using OurFoodChain.Bot.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class AdminCommands :
        ModuleBase {

        [Command("backup", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.BotAdmin)]
        public async Task Backup() {

            if (_running_backup) {

                await BotUtils.ReplyAsync_Error(Context, "A backup is already in progress. Please wait until it has completed.");

            }
            else {

                _running_backup = true;

                if (System.IO.File.Exists(Database.FilePath))
                    try {

                        await BotUtils.ReplyAsync_Info(Context, string.Format(
                            "Uploading database backup ({0:0.##} MB).\nThe backup will be posted in this channel when it is complete.",
                            new System.IO.FileInfo(Database.FilePath).Length / 1024000.0));

                        await Context.Channel.SendFileAsync(Database.FilePath, string.Format(string.Format("`Database backup ({0})`", DateTime.UtcNow.ToString())));

                    }
                    catch (Exception) {
                        await BotUtils.ReplyAsync_Error(Context, "Database file cannot be accessed.");
                    }
                else
                    await BotUtils.ReplyAsync_Error(Context, "Database file does not exist at the specified path.");

                _running_backup = false;

            }

        }

        private static bool _running_backup = false;

    }

}