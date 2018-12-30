using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    class StringUtils {

        public static string ToTitleCase(string str) {

            return System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());

        }
        public static bool IsNumeric(string str) {

            double num;

            return double.TryParse(str, out num);

        }
        public static string DisjunctiveJoin(string separator, IEnumerable<string> values) {
            return _joinWithUniqueEndSeparator(separator, " or ", values);
        }
        public static string ConjunctiveJoin(string separator, IEnumerable<string> values) {
            return _joinWithUniqueEndSeparator(separator, " and ", values);
        }

        private static string _joinWithUniqueEndSeparator(string separator, string endSeparator, IEnumerable<string> values) {

            if (values.Count() <= 0)
                return "";

            if (values.Count() == 1)
                return values.First();

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
