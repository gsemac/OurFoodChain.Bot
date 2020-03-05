using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public static class DateUtilities {

        public static DateTimeOffset GetCurrentDate() {

            return DateTimeOffset.Now;

        }
        public static DateTimeOffset GetCurrentDateUtc() {

            return DateTimeOffset.UtcNow;

        }

        public static long GetCurrentTimestamp() {

            return GetCurrentDate().ToUnixTimeSeconds();

        }
        public static long GetCurrentTimestampUtc() {

            return GetCurrentDateUtc().ToUnixTimeSeconds();

        }

        public static DateTimeOffset GetDateFromTimestamp(long timestamp) {

            return DateTimeOffset.FromUnixTimeSeconds(timestamp);

        }
        public static DateTimeOffset GetDateFromTimestamp(string timestamp) {

            if (long.TryParse(timestamp, out long result))
                return GetDateFromTimestamp(result);

            if (timestamp.Equals("now", StringComparison.OrdinalIgnoreCase))
                return GetCurrentDateUtc();

            throw new ArgumentException(nameof(timestamp));

        }

        public static long GetTimestampFromDate(DateTimeOffset offset) {

            return offset.ToUnixTimeSeconds();

        }

    }

}