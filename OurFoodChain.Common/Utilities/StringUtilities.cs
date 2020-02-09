using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OurFoodChain.Common.Utilities {

    public enum TitleOptions {
        None = 0,
        CapitalizeRomanNumerals = 1
    }

    public enum SentenceOptions {
        None = 0,
        BreakOnInitials = 1
    }

    public static class StringUtilities {

        // Public members

        public static string ToTitleCase(string input, TitleOptions options = TitleOptions.None) {

            if (string.IsNullOrEmpty(input))
                return string.Empty;

            string output = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(input.ToLowerInvariant());

            // Fix possessive "'s" so it's not capitalized (since "ToTitleCase" capitalizes it).
            // E.g. Widow'S Peak -> Widow's Peak
            output = Regex.Replace(output, @"\b(['’])S\b", "$1s");

            if (options.HasFlag(TitleOptions.CapitalizeRomanNumerals)) {

                // Fix Roman numerals so that they are completely capitalized (e.g. III, IV).
                // Regex adapted from: https://www.oreilly.com/library/view/regular-expressions-cookbook/9780596802837/ch06s09.html

                output = Regex.Replace(output, @"\b(?=[MDCLXVI])M*(C[MD]|D?C{0,3})(X[CL]|L?X{0,3})(I[XV]|V?I{0,3})\b", x => x.Value.ToUpper(), RegexOptions.IgnoreCase);

            }

            return output;

        }
        public static string ToSentenceCase(string input) {

            if (string.IsNullOrEmpty(input) || input.Length < 1)
                return input;

            input = input.ToLower();

            return input[0].ToString().ToUpper() + input.Substring(1);

        }

        public static string After(string input, string substring) {

            int index = input.IndexOf(substring);

            if (index >= 0)
                return input.Substring(index + substring.Length);
            else
                return input;

        }
        public static string Before(string input, string substring) {

            int index = input.IndexOf(substring);

            if (index >= 0)
                return input.Substring(0, index);
            else
                return input;

        }
        public static string ReplaceLast(string input, string substring, string replacement) {

            int index = input.LastIndexOf(substring);

            if (index < 0)
                return input;

            return input.Remove(index, substring.Length).Insert(index, replacement);

        }
        public static string ReplaceWhitespaceCharacters(string value, string with = "_") {

            if (string.IsNullOrEmpty(value))
                return value;

            return Regex.Replace(value, @"\s", with);

        }

        public static string SkipWords(string input, int numWords) {

            if (string.IsNullOrEmpty(input))
                return string.Empty;

            for (int i = 0; i < numWords; ++i) {

                if (string.IsNullOrWhiteSpace(input))
                    break;

                input = input.TrimStart();

                Match firstWordMatch = Regex.Match(input, @"[^\s]+");
                int index = firstWordMatch.Index + firstWordMatch.Length;

                input = input.Substring(index);

            }

            return input;

        }
        public static string GetFirstWord(string input) {

            if (string.IsNullOrEmpty(input))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(input))
                return "";

            input = input.TrimStart();

            return Regex.Match(input, @"[^\s]+").Value;

        }
        public static string GetFirstSentence(string value, SentenceOptions options = SentenceOptions.None) {

            string pattern = @"^.+?(?:\s\w{2,}\.+|[;!\?]+)";
            string result = value;

            // By default, we don't break on initializations (e.g. the period in "C. aspersum").
            // The user can optionally enable this behavior.

            if (options.HasFlag(SentenceOptions.BreakOnInitials))
                pattern = @"^.+?[\.;!\?]+";

            Match match = Regex.Match(value, pattern, RegexOptions.Multiline);

            if (match.Success && match.Length > 0)
                result = match.Value;

            if (result.Length > 0 && result.Last() == ';')
                result = result.Substring(0, result.Length - 1) + ".";

            return result;

        }
        public static string ToPossessive(string input) {

            if (string.IsNullOrEmpty(input))
                return string.Empty;

            if (input.ToLower().EndsWith("s"))
                return input + "'";

            return input + "'s";

        }

        public static string DisjunctiveJoin(string separator, IEnumerable<string> values) {
            return JoinWithUniqueEndSeparator(separator, " or ", values);
        }
        public static string ConjunctiveJoin(string separator, IEnumerable<string> values) {
            return JoinWithUniqueEndSeparator(separator, separator.EndsWith(" ") && values.Count() > 2 ? "and " : " and ", values);
        }

        public static int GetLevenshteinDistance(string input, string other) {

            // https://stackoverflow.com/questions/6944056/c-sharp-compare-string-similarity

            if (string.IsNullOrEmpty(input)) {

                if (string.IsNullOrEmpty(other))
                    return 0;

                return other.Length;

            }

            if (string.IsNullOrEmpty(other)) {

                return input.Length;

            }

            int n = input.Length;
            int m = other.Length;
            int[,] d = new int[n + 1, m + 1];

            // initialize the top and right of the table to 0, 1, 2, ...

            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 1; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++) {

                for (int j = 1; j <= m; j++) {

                    int cost = (other[j - 1] == input[i - 1]) ? 0 : 1;

                    int min1 = d[i - 1, j] + 1;
                    int min2 = d[i, j - 1] + 1;
                    int min3 = d[i - 1, j - 1] + cost;

                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);

                }

            }

            return d[n, m];

        }
        public static string GetBestMatch(string input, IEnumerable<string> strings) {

            if (strings is null)
                throw new ArgumentNullException(nameof(strings));

            if (strings.Count() <= 0)
                throw new ArgumentException(nameof(strings));

            string bestMatch = strings.First();
            int bestDistance = GetLevenshteinDistance(input, bestMatch);

            foreach (string str in strings.Skip(1)) {

                int distance = GetLevenshteinDistance(input, str);

                if (distance < bestDistance) {

                    bestDistance = distance;
                    bestMatch = str;

                }

                if (bestDistance <= 0)
                    break;

            }

            return bestMatch;

        }

        public static bool IsNumeric(string input) {

            return double.TryParse(input, out _);

        }
        public static bool IsUrl(string input) {

            return Regex.Match(input, "^https?:").Success;

        }
        public static bool IsImageUrl(string input) {

            return IsUrl(input);

        }
        public static bool TryParseColor(string input, out Color result) {

            try {

                result = ColorTranslator.FromHtml(input);

                return true;

            }
            catch (Exception) {

                result = Color.Black;

                return false;

            }

        }

        public static string GetMD5(string input) {

            // https://stackoverflow.com/questions/11454004/calculate-a-md5-hash-from-a-string

            if (string.IsNullOrEmpty(input))
                input = "";

            // Use input string to calculate MD5 hash

            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {

                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < hashBytes.Length; i++) {
                    sb.Append(hashBytes[i].ToString("X2"));
                }

                return sb.ToString();

            }

        }

        public static string GetAbbreviationForUnit(string value) {

            switch (value.ToLower()) {

                case "nm":
                case "nanometer":
                case "nanometers":
                    return "nm";

                case "μm":
                case "micrometer":
                case "micrometers":
                    return "μm";

                case "mm":
                case "millimeter":
                case "millimeters":
                    return "mm";

                case "cm":
                case "centimeter":
                case "centimeters":
                    return "cm";

                case "m":
                case "meter":
                case "meters":
                    return "m";

                case "in":
                case "inch":
                case "inches":
                    return "in";

                case "ft":
                case "foot":
                case "feet":
                    return "ft";


            }

            return value;

        }

        public static int SumCharacters(string value) {

            int sum = 0;

            for (int i = 0; i < value.Length; ++i)
                sum += value[i];

            return sum;

        }
        public static string CollapseAlphanumericList(string input, string delimiter = ",") {

            string[] values = input.Split(new string[] { delimiter }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToArray();

            List<List<string>> ranges = new List<List<string>>();

            foreach (string value in values) {

                if (ranges.Count() <= 0)
                    ranges.Add(new List<string> { value });
                else {

                    string last_part = ranges.Last().Last();

                    if (int.TryParse(value, out int intValue) && int.TryParse(last_part, out int intPrev) && intValue == intPrev + 1 ||
                        value.Length == 1 && last_part.Length == 1 && value[0] == last_part[0] + 1) {

                        // Merge values that are numerically sequential.

                        // Merge values that alphabetically sequential.
                        // For now, only do this for single characters.

                        ranges.Last().Add(value);

                    }
                    else
                        ranges.Add(new List<string> { value });

                }


            }

            return string.Join(", ", ranges
                .Select(x => x.Count() <= 2 ? string.Join(", ", x) : string.Format("{0}-{1}", x.First(), x.Last()))
                .ToArray());

        }

        public static IEnumerable<string> ParseArguments(string input) {

            if (!string.IsNullOrEmpty(input))
                input = input.Trim();

            List<string> arguments = new List<string>();

            string argument = "";
            bool insideQuotes = false;

            for (int i = 0; i < input.Length; ++i) {

                if (input[i] == '\"') {

                    insideQuotes = !insideQuotes;

                    argument += input[i];

                }
                else if (!insideQuotes && char.IsWhiteSpace(input[i])) {

                    arguments.Add(argument);
                    argument = "";

                }
                else
                    argument += input[i];

            }

            if (argument.Length > 0)
                arguments.Add(argument);

            // For each argument that begins and ends with a quote ("), strip the outer quotes.

            return arguments
                .Select(arg => (arg.Length > 1 && arg.StartsWith("\"") && arg.EndsWith("\"")) ? arg.Trim('"') : arg);

        }

        public static string SafeTrim(string input) {

            return input?.Trim() ?? "";

        }

        public static string Truncate(string input, int maxLength) {

            return input.Substring(0, Math.Min(input.Length, maxLength));

        }

        // Private members

        private static string JoinWithUniqueEndSeparator(string separator, string endSeparator, IEnumerable<string> values) {

            if (values.Count() <= 0)
                return "";

            if (values.Count() == 1)
                return values.First();

            if (values.Count() == 2)
                return string.Join(endSeparator, values);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i + 1 < values.Count(); ++i) {
                sb.Append(values.ElementAt(i));
                sb.Append(separator);
            }

            sb.Append(endSeparator);
            sb.Append(values.Last());

            return sb.ToString();


        }

    }

}