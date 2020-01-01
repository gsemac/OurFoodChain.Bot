using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public static class DateUtilities {

        public static DateTimeOffset GetCurrentDate() {

            return DateTimeOffset.UtcNow;

        }
        public static long GetCurrentTimestamp() {

            return GetCurrentDate().ToUnixTimeSeconds();

        }

    }

}
