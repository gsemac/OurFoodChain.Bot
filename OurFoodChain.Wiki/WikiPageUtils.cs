using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Wiki {

    public enum TextFormatting {
        Bold,
        Italic
    }

    public static class WikiPageUtils {

        // Public members

        public const string UnlinkedWikiTextPatternFormat = @"(?<!\[\[|\|)\b{0}\b(?!\||\]\])"; // no links
        public const string UnformattedWikiTextPatternFormat = @"(?<!\[\[|\||'')\b{0}\b(?!\||\]\]|'')"; // no links/italics/emboldening

        public static string ReplaceMarkdownWithWikiMarkup(string content) {

            // Replace lines beginning with whitespace, which get formatted as code blocks by MediaWiki.
            content = Regex.Replace(content, @"^[ \t]+", string.Empty, RegexOptions.Multiline);

            // Replace bold text (formatted with markdown).
            content = Regex.Replace(content, @"\*\*(.+?)\*\*", m => m.Value.Contains("'") ? m.Groups[1].Value : string.Format("'''{0}'''", m.Groups[1].Value));

            // Replace underlined text (formatted with markdown).
            content = Regex.Replace(content, @"__(.+?)__", m => string.Format("<u>{0}</u>", m.Groups[1].Value));

            // Replace strike-through text (formatted with markdown).
            content = Regex.Replace(content, @"~~(.+?)~~", m => string.Format("<s>{0}</s>", m.Groups[1].Value));

            // Replace italic text (formatted with markdown).

            content = Regex.Replace(content, @"\*(.+?)\*", m => m.Value.Contains("'") ? m.Groups[1].Value : string.Format("''{0}''", m.Groups[1].Value));
            content = Regex.Replace(content, @"_(.+?)_", m => m.Value.Contains("'") ? m.Groups[1].Value : string.Format("''{0}''", m.Groups[1].Value));

            // Replace single newlines with line breaks so that MediaWiki will create a line break.
            content = Regex.Replace(content, @"(?<!\n)\n[^\n]", m => string.Format("<br />{0}", m.Value));

            // Format lists made with "-" instead of "*".
            content = Regex.Replace(content, @"^-\s*", "* ", RegexOptions.Multiline);

            return content;

        }

        public static string FormatPageLinks(string content, WikiLinkList linkifyList) {
            return FormatPageLinksIf(content, linkifyList, x => true);
        }
        public static string FormatPageLinksIf(string content, WikiLinkList linkifyList, Func<WikiLinkListData, bool> condition) {

            // Keys to be replaced are sorted by length so longer strings are replaced before their substrings.
            // For example, "one two" should have higher priority over "one" and "two" individually.

            // Additionally, filter the list so that we only have unique values to avoid performing replacements more than once.
            // This can cause some incorrect mappings, but it's the best we can do.
            // Note that "GroupBy" preserves the order of the elements.

            foreach (WikiLinkListData data in linkifyList.GroupBy(x => x.Value).Select(x => x.First()).OrderByDescending(x => x.Value.Length)) {

                if (!condition(data))
                    continue;

                string pageTitle = data.Target;
                Regex regex = data.Type == WikiLinkListDataType.Find ? new Regex(string.Format(UnlinkedWikiTextPatternFormat, Regex.Escape(data.Value)), RegexOptions.IgnoreCase)
                    : new Regex(data.Value);

                content = regex.Replace(content, m => {

                    string matchValue = m.Value;

                    if (pageTitle == matchValue)
                        return string.Format("[[{0}]]", matchValue);
                    else
                        return string.Format("[[{0}|{1}]]", pageTitle, matchValue);

                });

            }

            return content;

        }

        public static string FormatFirstMatch(string content, Regex regex, TextFormatting textFormatting) {

            return regex.Replace(content, m => FormatString(m.Value, textFormatting), 1);

        }
        public static string FormatAllMatches(string content, Regex regex, TextFormatting textFormatting) {

            content = regex.Replace(content, m => FormatString(m.Value, textFormatting));

            return content;

        }

        public static string FormatString(string input, TextFormatting textFormatting) {

            switch (textFormatting) {

                case TextFormatting.Bold:
                    return string.Format("'''{0}'''", input);

                case TextFormatting.Italic:
                    return string.Format("''{0}''", input);

            }

            return input;

        }

    }

}
