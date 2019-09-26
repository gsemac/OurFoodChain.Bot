using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChainWikiBot {

    public enum TextFormatting {
        Bold,
        Italic
    }

    public class PageUtils {

        // Public members

        public const string UnlinkedWikiTextPatternFormat = @"(?<!\[\[|\|)\b{0}\b(?!\||\]\])";

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

        public static string FormatPageLinks(string content, Dictionary<string, string> linkDictionary) {
            return FormatPageLinksIf(content, linkDictionary, x => true);
        }
        public static string FormatPageLinksIf(string content, Dictionary<string, string> linkDictionary, Func<string, bool> condition) {

            // Keys to be replaced are sorted by length so longer strings are replaced before their substrings.
            // For example, "one two" should have higher priority over "one" and "two" individually.

            foreach (string key in linkDictionary.Keys.OrderByDescending(x => x.Length)) {

                if (!condition(key))
                    continue;

                content = Regex.Replace(content, string.Format(UnlinkedWikiTextPatternFormat, Regex.Escape(key)), m => {

                    string page_title = linkDictionary[key];
                    string match_value = m.Value;

                    if (page_title == match_value)
                        return string.Format("[[{0}]]", match_value);
                    else
                        return string.Format("[[{0}|{1}]]", page_title, match_value);

                }, RegexOptions.IgnoreCase);

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
