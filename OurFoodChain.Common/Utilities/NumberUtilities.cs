using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public static class NumberUtilities {

        // Public members

        public static int GetRandomInteger() {

            return random.Next();

        }
        public static int GetRandomInteger(int max) {

            return random.Next(max);

        }
        public static int GetRandomInteger(int min, int max) {

            return random.Next(min, max);

        }

        public static bool GetRandomBoolean() {

            return GetRandomInteger(2) == 0;

        }

        // Private members

        private static readonly Random random = new Random();

    }

}