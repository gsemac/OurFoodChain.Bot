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
        ModuleBase {

        [Command("backup", RunMode = RunMode.Async), RequirePrivilege(PrivilegeLevel.BotAdmin)]
        public async Task Backup() {

            if (currentlyCreatingDatabaseBackup) {

                await DiscordUtilities.ReplyErrorAsync(Context.Channel, "A backup is already in progress. Please wait until it has completed.");

            }
            else {

                currentlyCreatingDatabaseBackup = true;

                if (System.IO.File.Exists(Database.FilePath)) {

                    try {

                        await DiscordUtilities.ReplyInfoAsync(Context.Channel,
                            string.Format("Uploading database backup ({0:0.##} MB).\nThe backup will be posted in this channel when it is complete.",
                            new System.IO.FileInfo(Database.FilePath).Length / 1024000.0));

                        await Context.Channel.SendFileAsync(Database.FilePath, string.Format("`Database backup ({0})`", DateUtilities.GetCurrentUtcDate()));

                    }
                    catch (Exception) {

                        await DiscordUtilities.ReplyErrorAsync(Context.Channel, "Database file cannot be accessed.");

                    }

                }
                else {

                    await DiscordUtilities.ReplyErrorAsync(Context.Channel, "Database file does not exist at the specified path.");

                }

                currentlyCreatingDatabaseBackup = false;

            }

        }

        private static bool currentlyCreatingDatabaseBackup = false;

    }

}