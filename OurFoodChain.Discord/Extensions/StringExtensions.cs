using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Extensions {

    public static class StringExtensions {

        public static string ToBold(this string input) {

            return string.Format("**{0}**", input);

        }
        public static string ToStrikethrough(this string input) {

            return string.Format("~~{0}~~", input);

        }
        public static string FromLink(this string input, string url) {

            return string.Format("[{0}]({1})", input, url);

        }
        public static string ToLink(this string input, string text) {

            return string.Format("[{0}]({1})", text, input);

        }

    }

}