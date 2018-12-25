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

    }

}
