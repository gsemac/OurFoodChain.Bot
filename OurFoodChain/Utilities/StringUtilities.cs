using System;
using System.Collections.Generic;
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

    }

}