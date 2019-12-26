using OurFoodChain.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Extensions {

    public static class StringExtensions {

        public static string ToTitleCase(this string input) {

            return StringUtilities.ToTitleCase(input);

        }

        public static string AfterSubstring(this string input, string substring) {

            return StringUtilities.AfterSubstring(input, substring);

        }

    }

}