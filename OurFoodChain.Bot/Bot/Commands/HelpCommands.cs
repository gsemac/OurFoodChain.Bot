using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Bot {

    public class HelpCommands :
        ModuleBase {

        [Command("help"), Alias("h")]
        public async Task Help(string command = "", string nestedCommand = "") {

            await ShowHelp(Context, command, nestedCommand);

        }

        private static async Task _showHelpCategory(ICommandContext context, CommandHelpInfoCollection commands, string category) {

            SortedDictionary<string, List<CommandHelpInfo>> commands_lists = new SortedDictionary<string, List<CommandHelpInfo>>();

            foreach (CommandHelpInfo command in commands) {

                if (!commands_lists.ContainsKey(command.Category))
                    commands_lists[command.Category] = new List<CommandHelpInfo>();

                commands_lists[command.Category].Add(command);

            }

            EmbedBuilder builder = new EmbedBuilder();
            StringBuilder description_builder = new StringBuilder();

            if (!string.IsNullOrEmpty(category))
                description_builder.AppendLine(string.Format("Commands listed must be prefaced with `{0}` (e.g. `{2}{0} {1}`).",
                    category,
                    commands.First().Name,
                    OurFoodChainBot.Instance.Config.Prefix));

            description_builder.AppendLine(string.Format("To learn more about a command, use `{0}help <command>` (e.g. `{0}help {1}{2}`).",
                OurFoodChainBot.Instance.Config.Prefix,
                string.IsNullOrEmpty(category) ? "" : category + " ",
                commands.First().Name));

            string version_string = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            // Remove any trailing ".0"s from the version number, so they don't appear when unused.
            while (version_string.EndsWith(".0"))
                version_string = version_string.Substring(0, version_string.Length - 2);

            builder.WithTitle("Commands list");
            builder.WithDescription(description_builder.ToString());
            builder.WithFooter(string.Format("ourfoodchain-bot v.{0} — github.com/gsemac/ourfoodchain-bot", version_string));

            foreach (string cat in commands_lists.Keys) {

                // Sort commands, and filter out commands that aren't currently loaded.
                // As well, filter out commands that the current user does not have access to.

                List<string> command_str_list = new List<string>();

                foreach (CommandHelpInfo c in commands_lists[cat]) {

                    if (await Bot.CommandUtils.CommandIsEnabledAsync(context, c.Name))
                        command_str_list.Add(string.Format("`{0}`", c.Name));

                }

                command_str_list.Sort((lhs, rhs) => lhs.CompareTo(rhs));

                if (command_str_list.Count() > 0)
                    builder.AddField(StringUtils.ToTitleCase(cat), string.Join("  ", command_str_list));

            }

            await context.Channel.SendMessageAsync("", false, builder.Build());

        }
        public static async Task ShowHelpCategory(ICommandContext context, string commandInfoDirectory, string category) {

            if (!System.IO.Directory.Exists(commandInfoDirectory)) {

                await BotUtils.ReplyAsync_Error(context, string.Format("Help information cannot be displayed, because the help directory \"{0}\" does not exist.", commandInfoDirectory));

                return;

            }

            await _showHelpCategory(context, HelpUtils.GetCommandInfoFromDirectory(commandInfoDirectory), category);

        }
        public static async Task ShowHelp(ICommandContext context, string command, string nestedCommand) {

            // Load the .json files containing command information.
            // If there is a subdirectory in the help directory with the same name as the command, load files in that subdirectory instead.

            string help_directory = HelpUtils.HELP_DIRECTORY;

            if (!string.IsNullOrEmpty(nestedCommand) && System.IO.Directory.Exists(System.IO.Path.Combine(help_directory, command)))
                help_directory = System.IO.Path.Combine(help_directory, command);

            if (System.IO.Directory.Exists(help_directory)) {

                CommandHelpInfoCollection commands = HelpUtils.GetCommandInfoFromDirectory(help_directory);

                if (!string.IsNullOrEmpty(command)) {

                    // If the user provided a specific command name, show information about that command.

                    CommandHelpInfo info = commands.FindCommandByName(string.IsNullOrEmpty(nestedCommand) ? command : nestedCommand);

                    if (info is null)
                        await context.Channel.SendMessageAsync("The given command does not exist, or is not yet documented.");
                    else {

                        // Prefix the command with is parent if applicable.

                        if (!string.IsNullOrEmpty(nestedCommand))
                            info.Name = command.Trim() + " " + info.Name;

                        EmbedBuilder builder = new EmbedBuilder();

                        builder.WithTitle(string.Format("Help: {0}", info.Name));

                        builder.AddField("Description", info.Description.Replace("\\prefix", OurFoodChainBot.Instance.Config.Prefix));

                        if (info.Aliases.Count() > 0)
                            builder.AddField("Aliases", string.Join(", ", info.Aliases.OrderBy(x => x)));

                        if (info.Examples.Count() > 0) {

                            for (int i = 0; i < info.Examples.Count(); ++i)
                                info.Examples[i] = "`" + OurFoodChainBot.Instance.Config.Prefix + (string.IsNullOrEmpty(nestedCommand) ? "" : command.Trim() + " ") + info.Examples[i] + "`";

                            builder.AddField("Example(s)", string.Join(Environment.NewLine, info.Examples));

                        }

                        await context.Channel.SendMessageAsync("", false, builder.Build());

                    }

                }
                else
                    await _showHelpCategory(context, commands, "");

            }
            else
                await BotUtils.ReplyAsync_Error(context, string.Format("Help information cannot be displayed, because the help directory \"{0}\" does not exist.", help_directory));

        }

    }

}