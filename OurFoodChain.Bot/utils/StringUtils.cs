using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class StringUtils {

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
        public static string ToSentenceCase(string str) {

            if (string.IsNullOrEmpty(str) || str.Length < 1)
                return str;

            str = str.ToLower();

            return str[0].ToString().ToUpper() + str.Substring(1);

        }
        public static string ToPossessive(string input) {

            if (string.IsNullOrEmpty(input))
                return string.Empty;

            if (input.ToLower().EndsWith("s"))
                return input + "'";

            return input + "'s";

        }
        public static bool IsNumeric(string str) {

            double num;

            return double.TryParse(str, out num);

        }
        public static string DisjunctiveJoin(string separator, IEnumerable<string> values) {
            return _joinWithUniqueEndSeparator(separator, " or ", values);
        }
        public static string ConjunctiveJoin(string separator, IEnumerable<string> values) {
            return _joinWithUniqueEndSeparator(separator, separator.EndsWith(" ") && values.Count() > 2 ? "and " : " and ", values);
        }
        public static string GetFirstSentence(string value) {

            string result = value;
            Match match = Regex.Match(value, @"^.+?(?:\.+|[;!\?])");

            if (match.Success && match.Length > 0)
                result = match.Value;

            if (result.Length > 0 && result.Last() == ';')
                result = result.Substring(0, result.Length - 1) + ".";

            return result;

        }
        public static string ReplaceWhitespaceCharacters(string value, string with = "_") {

            if (string.IsNullOrEmpty(value))
                return value;

            return Regex.Replace(value, @"\s", with);

        }

        public static bool IsUrl(string input) {
            return Regex.Match(input, "^https?:").Success;
        }

        // https://stackoverflow.com/questions/11454004/calculate-a-md5-hash-from-a-string
        public static string CreateMD5(string input) {

            if (string.IsNullOrEmpty(input))
                input = "";

            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++) {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }

        }
        public static int SumStringChars(string value) {

            int sum = 0;

            for (int i = 0; i < value.Length; ++i)
                sum += value[i];

            return sum;

        }

        public static string UnitsToAbbreviation(string value) {

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

                    if ((int.TryParse(value, out int intValue) && int.TryParse(last_part, out int intPrev) && intValue == intPrev + 1) ||
                        (value.Length == 1 && last_part.Length == 1 && value[0] == last_part[0] + 1)) {

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

        private static string _joinWithUniqueEndSeparator(string separator, string endSeparator, IEnumerable<string> values) {

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