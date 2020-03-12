using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public static class GeneralUtilities {

        public static void Swap<T>(ref T lhs, ref T rhs) {

            T temp;

            temp = lhs;
            lhs = rhs;

            rhs = temp;

        }

    }

}