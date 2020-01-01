using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Common.Extensions {

    public static class LinqExtensions {

        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> input, int count) {

            return input.Reverse().Skip(count).Reverse();

        }

        public static T Random<T>(this IEnumerable<T> input) {

            return input.ElementAt(_random.Next(input.Count()));

        }

        private static Random _random = new Random();

    }

}
