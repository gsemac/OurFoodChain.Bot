using Discord;
using OurFoodChain.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Discord {

    public static class DiscordUtilities {

        public const int MaxFieldLength = 1024;
        public const int MaxMessageLength = 2000;
        public const int MaxFieldCount = 25;
        public const int MaxEmbedLength = 2048;

        public static async Task ReplySuccessAsync(IMessageChannel channel, string message) {

            await channel.SendMessageAsync("", false, BuildSuccessEmbed(message).Build());

        }
        public static async Task ReplyErrorAsync(IMessageChannel channel, string message) {

            await channel.SendMessageAsync("", false, BuildErrorEmbed(message).Build());

        }
        public static async Task ReplyInfoAsync(IMessageChannel channel, string message) {

            await channel.SendMessageAsync("", false, BuildInfoEmbed(message).Build());

        }

        public static EmbedBuilder BuildSuccessEmbed(string message) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(string.Format("✅ {0}", message));
            embed.WithColor(Color.Green);

            return embed;

        }
        public static EmbedBuilder BuildErrorEmbed(string message) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(string.Format("❌ {0}", message));
            embed.WithColor(Color.Red);

            return embed;

        }
        public static EmbedBuilder BuildInfoEmbed(string message) {

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithDescription(message);
            embed.WithColor(Color.LightGrey);

            return embed;

        }
        public static EmbedBuilder BuildCommandHelpInfoEmbed(ICommandHelpInfo helpInfo, IBotConfiguration botConfiguration) {

            if (helpInfo is null)
                return BuildInfoEmbed("The given command does not exist, or is not yet documented.");
            else {

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle(string.Format("Help: {0}", helpInfo.Name));

                if (!string.IsNullOrEmpty(helpInfo.Summary))
                    builder.AddField("Summary", (helpInfo.Summary ?? "No documentation available.").Replace("\\prefix", botConfiguration.Prefix));

                if (helpInfo.Aliases != null && helpInfo.Aliases.Count() > 0)
                    builder.AddField("Aliases", string.Join(", ", helpInfo.Aliases.OrderBy(x => x)));

                if (helpInfo.Examples != null && helpInfo.Examples.Count() > 0) {

                    builder.AddField("Example(s)", string.Join(Environment.NewLine, helpInfo.Examples
                        .Select(i => string.Format("`{0}{1}`", botConfiguration.Prefix, i))));

                }

                return builder;

            }

        }
        public static EmbedBuilder BuildCommandHelpInfoEmbed(IEnumerable<ICommandHelpInfo> helpInfos, IBotConfiguration botConfiguration) {

            return BuildCommandHelpInfoEmbed(helpInfos, botConfiguration, string.Empty);

        }
        public static EmbedBuilder BuildCommandHelpInfoEmbed(IEnumerable<ICommandHelpInfo> helpInfos, IBotConfiguration botConfiguration, string groupName) {

            if (helpInfos is null || helpInfos.Count() <= 0)
                return BuildInfoEmbed("No documentation is available.");
            else {

                // Filter out commands that show not be displayed.

                helpInfos = helpInfos
                    .Where(i => !string.IsNullOrEmpty(i.Category)) // hide uncategorized commands
                    .Where(i => i.Group.Equals(groupName, StringComparison.OrdinalIgnoreCase));

                // Sort the commands into groups based on their category.

                SortedDictionary<string, List<ICommandHelpInfo>> commandCategories = new SortedDictionary<string, List<ICommandHelpInfo>>();

                foreach (ICommandHelpInfo helpInfo in helpInfos) {

                    // Do not display uncategorized commands for now.

                    if (string.IsNullOrEmpty(helpInfo.Category))
                        continue;

                    if (!commandCategories.ContainsKey(helpInfo.Category))
                        commandCategories[helpInfo.Category] = new List<ICommandHelpInfo>();

                    commandCategories[helpInfo.Category].Add(helpInfo);

                }

                EmbedBuilder builder = new EmbedBuilder();
                StringBuilder descriptionBuilder = new StringBuilder();

                // If the user has not specified a group, we'll only show top-level (e.g. ungrouped) commands.
                // Otherwise, we will show commands that are part of that group.

                if (!string.IsNullOrEmpty(groupName)) {

                    descriptionBuilder.AppendLine(string.Format("Commands listed must be prefaced with `{0}` (e.g. `{1}{2}`).",
                        groupName,
                        botConfiguration.Prefix,
                        helpInfos.First().Name));

                }

                descriptionBuilder.AppendLine(string.Format("To learn more about a command, use `{0}help <command>` (e.g. `{0}help {1}`).",
                    botConfiguration.Prefix,
                    helpInfos.First().Name));

                string versionString = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
                string projectName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;

                // Remove any trailing ".0"s from the version number, so they don't appear when unused.

                while (versionString.EndsWith(".0"))
                    versionString = versionString.Substring(0, versionString.Length - 2);

                builder.WithTitle(string.IsNullOrEmpty(groupName) ? "Commands list" : string.Format("{0} commands list", groupName.ToTitleCase()));
                builder.WithFooter(string.Format("{0} v.{1} — github.com/gsemac/ourfoodchain-bot", projectName, versionString));
                builder.WithDescription(descriptionBuilder.ToString());

                foreach (string categoryName in commandCategories.Keys) {

                    string commandsList = string.Join("  ", commandCategories[categoryName]
                        .Select(i => i.Name.AfterSubstring(groupName).Trim())
                        .Select(s => string.Format("`{0}`", s))
                        .OrderBy(s => s));

                    if (commandsList.Length > MaxFieldLength)
                        commandsList = commandsList.Substring(0, MaxFieldLength);

                    if (commandsList.Length > 0)
                        builder.AddField(categoryName.ToTitleCase(), commandsList);

                }

                return builder;

            }

        }

    }

}