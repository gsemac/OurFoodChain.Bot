using Discord;
using OurFoodChain.Common;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using OurFoodChain.Data.Queries;
using OurFoodChain.Discord.Bots;
using OurFoodChain.Discord.Commands;
using OurFoodChain.Discord.Extensions;
using OurFoodChain.Discord.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Discord.Utilities {

    [Flags]
    public enum EmbedPaginationOptions {
        None = 0,
        AddPageNumbers = 1,
        NoStrikethrough = 2,
        CopyFields = 4,
        Default = None
    }

    public static class EmbedUtilities {

        // Public members

        public const int DefaultItemsPerPage = 40;
        public const int DefaultColumnsPerPage = 2;

        public static Messaging.IEmbed BuildSuccessEmbed(string message) {

            Messaging.Embed embed = new Messaging.Embed {
                Description = string.Format("✅ {0}", message),
                Color = Color.Green.ToSystemDrawingColor()
            };

            return embed;

        }
        public static Messaging.IEmbed BuildWarningEmbed(string message) {

            Messaging.Embed embed = new Messaging.Embed {
                Description = string.Format("⚠️ {0}", message),
                Color = Color.Orange.ToSystemDrawingColor()
            };

            return embed;

        }
        public static Messaging.IEmbed BuildErrorEmbed(string message) {

            Messaging.Embed embed = new Messaging.Embed {
                Description = string.Format("❌ {0}", message),
                Color = Color.Red.ToSystemDrawingColor()
            };

            return embed;

        }
        public static Messaging.IEmbed BuildInfoEmbed(string message) {

            Messaging.Embed embed = new Messaging.Embed {
                Description = message,
                Color = Color.LightGrey.ToSystemDrawingColor()
            };

            return embed;

        }
        public static Messaging.IEmbed BuildCommandHelpInfoEmbed(ICommandHelpInfo helpInfo, IBotConfiguration botConfiguration) {

            if (helpInfo is null)
                return BuildInfoEmbed("The given command does not exist, or is not yet documented.");
            else {

                Messaging.Embed embed = new Messaging.Embed {
                    Title = string.Format("Help: {0}", helpInfo.Name)
                };

                if (!string.IsNullOrEmpty(helpInfo.Summary))
                    embed.AddField("Summary", (helpInfo.Summary ?? "No documentation available.").Replace("\\prefix", botConfiguration.Prefix));

                if (helpInfo.Aliases != null && helpInfo.Aliases.Count() > 0)
                    embed.AddField("Aliases", string.Join(", ", helpInfo.Aliases.OrderBy(x => x)));

                if (helpInfo.Examples != null && helpInfo.Examples.Count() > 0) {

                    embed.AddField("Example(s)", string.Join(Environment.NewLine, helpInfo.Examples
                        .Select(i => string.Format("`{0}{1}{2}`", botConfiguration.Prefix, helpInfo.Group + (helpInfo.Group.Length > 0 ? " " : string.Empty), i))));

                }

                return embed;

            }

        }
        public static Messaging.IEmbed BuildCommandHelpInfoEmbed(IEnumerable<ICommandHelpInfo> helpInfos, IBotConfiguration botConfiguration) {

            return BuildCommandHelpInfoEmbed(helpInfos, botConfiguration, string.Empty);

        }
        public static Messaging.IEmbed BuildCommandHelpInfoEmbed(IEnumerable<ICommandHelpInfo> helpInfos, IBotConfiguration botConfiguration, string groupName) {

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

                Messaging.Embed embed = new Messaging.Embed();
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

                embed.Title = string.IsNullOrEmpty(groupName) ? "Commands list" : string.Format("{0} commands list", groupName.ToTitle());
                embed.Footer = string.Format("{0} v.{1} — github.com/gsemac/ourfoodchain-bot", projectName, versionString);
                embed.Description = descriptionBuilder.ToString();

                foreach (string categoryName in commandCategories.Keys) {

                    string commandsList = string.Join("  ", commandCategories[categoryName]
                        .Select(i => i.Name.After(groupName).Trim())
                        .Select(s => string.Format("`{0}`", s))
                        .OrderBy(s => s));

                    if (commandsList.Length > DiscordUtilities.MaxFieldLength)
                        commandsList = commandsList.Substring(0, DiscordUtilities.MaxFieldLength);

                    if (commandsList.Length > 0)
                        embed.AddField(categoryName.ToTitle(), commandsList);

                }

                return embed;

            }

        }

        public static IEnumerable<Messaging.IEmbed> CreateEmbedPages(Messaging.IEmbed embed, EmbedPaginationOptions options = EmbedPaginationOptions.Default) {

            List<Messaging.IEmbed> pages = new List<Messaging.IEmbed>();
            string description = embed.Description;

            if (embed.Length > DiscordUtilities.MaxEmbedLength) {

                // The length of the embed is greater than the maximum embed length.

                int maxDescriptionLengthPerPage = DiscordUtilities.MaxEmbedLength - (embed.Length - description.Length);

                if (maxDescriptionLengthPerPage <= 0)
                    throw new Exception("The embed is too long to be paginated.");

                foreach (string pageDescription in new StringPaginator(description, maxDescriptionLengthPerPage)) {

                    Messaging.IEmbed page = new Messaging.Embed {
                        Title = embed.Title,
                        ThumbnailUrl = embed.ThumbnailUrl,
                        Description = pageDescription,
                        Color = embed.Color,
                        Footer = embed.Footer,
                        Url = embed.Url
                    };

                    if (pages.Count() < 1 || options.HasFlag(EmbedPaginationOptions.CopyFields)) {

                        foreach (IEmbedField field in embed.Fields)
                            page.AddField(field);

                    }

                    pages.Add(page);

                }

                if (options.HasFlag(EmbedPaginationOptions.AddPageNumbers))
                    AddPageNumbers(pages);

            }
            else {

                pages.Add(embed);

            }

            return pages;

        }
        public static IEnumerable<Messaging.IEmbed> CreateEmbedPages(string listTitle, IEnumerable<string> listItems, int itemsPerPage = DefaultItemsPerPage, int columnsPerPage = DefaultColumnsPerPage, EmbedPaginationOptions options = EmbedPaginationOptions.None) {

            if (string.IsNullOrWhiteSpace(listTitle))
                listTitle = Messaging.EmbedField.EmptyName;

            IEnumerable<IEnumerable<string>> columns = ListToColumns(listItems, itemsPerPage / columnsPerPage);
            List<Messaging.IEmbed> pages = new List<Messaging.IEmbed>();

            Messaging.IEmbed currentPage = new Messaging.Embed();
            int fieldCount = 0;

            bool hasTitle = !(string.IsNullOrWhiteSpace(listTitle) || listTitle.Equals(Messaging.EmbedField.EmptyName));
            bool usingDescription = columnsPerPage == 1 || (columns.Count() == 1 && !hasTitle);

            foreach (IEnumerable<string> column in columns) {

                StringBuilder builder = new StringBuilder();

                foreach (string item in column)
                    builder.AppendLine(item);

                if (fieldCount <= 0) {

                    if (usingDescription) {

                        // If there's only one column, add text directly to the description.
                        // The page should be changed out immediately.

                        currentPage.Description = builder.ToString();

                        pages.Add(currentPage);

                        currentPage = new Messaging.Embed();

                    }
                    else {

                        currentPage.AddField(listTitle, builder.ToString(), inline: true);

                        ++fieldCount;

                    }

                }
                else {

                    currentPage.AddField(Messaging.EmbedField.EmptyName, builder.ToString(), inline: true);

                    pages.Add(currentPage);

                    if (++fieldCount >= columnsPerPage) {

                        currentPage = new Messaging.Embed();
                        fieldCount = 0;

                    }

                }

            }

            if (currentPage.Fields.Count() > 0 || currentPage.Description?.Length > 0)
                pages.Add(currentPage);

            if (usingDescription)
                foreach (Messaging.IEmbed page in pages)
                    page.Title = listTitle;

            if (options.HasFlag(EmbedPaginationOptions.AddPageNumbers))
                AddPageNumbers(pages);

            return pages;

        }
        public static IEnumerable<Messaging.IEmbed> CreateEmbedPages(string listTitle, IEnumerable<ISpecies> listItems, ITaxonFormatter formatter = null, int itemsPerPage = DefaultItemsPerPage, int columnsPerPage = DefaultColumnsPerPage, EmbedPaginationOptions options = EmbedPaginationOptions.Default) {

            if (formatter is null)
                formatter = new BinomialNameTaxonFormatter();

            IEnumerable<string> stringListItems = listItems.Select(species => {

                string name = formatter.GetString(species, species.IsExtinct() && !options.HasFlag(EmbedPaginationOptions.NoStrikethrough));

                return name;

            });

            return CreateEmbedPages(listTitle, stringListItems, itemsPerPage, columnsPerPage, options);

        }
        public static IEnumerable<Messaging.IEmbed> CreateEmbedPages(ISearchResult searchResult, EmbedPaginationOptions options = EmbedPaginationOptions.Default) {

            List<Messaging.IEmbed> pages = new List<Messaging.IEmbed>();

            int itemsPerField = 10;
            int fieldsPerPage = 6;

            foreach (ISearchResultGroup group in searchResult.Groups) {

                IEnumerable<string> items = group.GetStringResults();

                IEnumerable<IEnumerable<string>> columns = ListToColumns(items, itemsPerField);
                int columnIndex = 1;

                foreach (IEnumerable<string> column in columns) {

                    // Create the field for this column.

                    string title = group.Name.Length > 25 ? group.Name.Substring(0, 22) + "..." : group.Name;
                    string fieldName = columnIndex == 1 ? string.Format("{0} ({1})", title.ToTitle(), items.Count()) : string.Format("...", title.ToTitle());
                    string fieldValue = string.Join(Environment.NewLine, column);

                    IEmbedField field = new Messaging.EmbedField(fieldName, fieldValue) { Inline = true };

                    ++columnIndex;

                    // Add the field to the embed, creating a new page if needed.

                    int fieldLength = field.Length;

                    if (pages.Count() <= 0 || pages.Last().Fields.Count() >= fieldsPerPage || pages.Last().Length + fieldLength > DiscordUtilities.MaxEmbedLength)
                        pages.Add(new Messaging.Embed());

                    pages.Last().AddField(field);

                }

            }

            if (options.HasFlag(EmbedPaginationOptions.AddPageNumbers))
                AddPageNumbers(pages);

            return pages;

        }

        public static void AppendEmbedPages(ICollection<Messaging.IEmbed> existingPages, IEnumerable<Messaging.IEmbed> newPages) {

            if (existingPages.Count <= 0 || existingPages.Last().Fields.Count() >= 2) {

                // If the last page of the existing range is full, or the range is empty, simply append the pages.

                existingPages.AddRange(newPages);

            }
            else {

                // If the last page has space, copy the first field to that page, and then create new pages from thereon out.

                IEnumerable<IEmbedField> fields = newPages.SelectMany(p => p.Fields);

                foreach (IEmbedField field in fields) {

                    if (existingPages.Count() <= 0 || existingPages.Last().Fields.Count() >= 2)
                        existingPages.Add(new Messaging.Embed());

                    existingPages.Last().AddField(field);

                }

            }

        }

        public static void AddPageNumbers(IEnumerable<Messaging.IEmbed> pages) {

            int num = 1;

            foreach (Messaging.IEmbed page in pages) {

                string pageNumberString = string.Format("Page {0} of {1}", num, pages.Count());

                page.Footer = string.IsNullOrEmpty(page.Footer) ? pageNumberString : string.Format("{0} — {1}", pageNumberString, page.Footer);

                ++num;

            }

        }

        // Private members

        private static IEnumerable<IEnumerable<string>> ListToColumns(IEnumerable<string> items, int itemsPerColumn) {

            List<List<string>> columns = new List<List<string>>();

            foreach (string item in items) {

                if (columns.Count <= 0 || columns.Last().Count >= itemsPerColumn)
                    columns.Add(new List<string>());

                columns.Last().Add(item);

            }

            return columns;

        }

    }

}