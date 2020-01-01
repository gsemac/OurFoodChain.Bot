using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Extensions {

    public static class StringExtensions {

        public static string ToTitleCase(this string input) {

            return StringUtilities.ToTitleCase(input);

        }

        public static string AfterSubstring(this string input, string substring) {

            return StringUtilities.AfterSubstring(input, substring);

        }

        public static string FirstWord(this string input) {

            return StringUtilities.GetFirstWord(input);

        }
        public static string SkipWords(this string input, int numWords) {

            return StringUtilities.SkipWords(input, numWords);

        }

    }

}