using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Extensions {

    public static class StringExtensions {

        public static string ToBold(this string input) {

            return string.Format("**{0}**", input);

        }

    }

}