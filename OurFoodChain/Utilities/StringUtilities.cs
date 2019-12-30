using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OurFoodChain.Utilities {

    public static class StringUtilities {

        public static string ToTitleCase(string input) {

            if (string.IsNullOrEmpty(input))
                return string.Empty;

            string output = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());

            // Fix possessive "'s" so it's not capitalized (since "ToTitleCase" capitalizes it).
            // E.g. Widow'S Peak -> Widow's Peak
            output = Regex.Replace(output, @"\b(['’])S\b", "$1s");

            // Fix Roman numerals so that they are completely capitalized (e.g. III, IV).
            // Regex adapted from: https://www.oreilly.com/library/view/regular-expressions-cookbook/9780596802837/ch06s09.html
            output = Regex.Replace(output, @"\b(?=[MDCLXVI])M*(C[MD]|D?C{0,3})(X[CL]|L?X{0,3})(I[XV]|V?I{0,3})\b", x => x.Value.ToUpper(), RegexOptions.IgnoreCase);

            return output;

        }

        public static string AfterSubstring(string input, string substring) {

            int index = input.IndexOf(substring);

            if (index >= 0)
                return input.Substring(index + substring.Length);
            else
                return input;

        }

        public static string FirstWord(string input) {

            if (string.IsNullOrEmpty(input))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(input))
                return "";

            input = input.TrimStart();

            return Regex.Match(input, @"[^\s]+").Value;

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

    }

}