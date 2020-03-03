using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Extensions {

    public static class StringExtensions {

        public static string ToTitle(this string input) {

            return StringUtilities.ToTitleCase(input);

        }

        public static string After(this string input, string substring) {

            return StringUtilities.After(input, substring);

        }
        public static string Before(this string input, string substring) {

            return StringUtilities.Before(input, substring);

        }
        public static string ReplaceLast(this string input, string substring, string replacement) {

            return StringUtilities.ReplaceLast(input, substring, replacement);

        }

        public static string GetFirstWord(this string input) {

            return StringUtilities.GetFirstWord(input);

        }
        public static string SkipWords(this string input, int numWords) {

            return StringUtilities.SkipWords(input, numWords);

        }
        public static string GetFirstSentence(this string input) {

            return StringUtilities.GetFirstSentence(input);

        }

        public static string SafeTrim(this string input) {

            return StringUtilities.SafeTrim(input);

        }

        public static string Truncate(this string input, int maxLength) {

            return StringUtilities.Truncate(input, maxLength);

        }

    }

}