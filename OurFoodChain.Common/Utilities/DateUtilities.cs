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

            return DateTimeOffset.FromUnixTimeSeconds(timestamp).ToUniversalTime();

        }
        public static DateTimeOffset GetDateFromTimestamp(string timestamp) {

            return GetDateFromTimestamp(ParseTimestamp(timestamp));

        }

        public static long GetTimestampFromDate(DateTimeOffset offset) {

            return offset.ToUnixTimeSeconds();

        }

        public static long ParseTimestamp(string timestamp) {

            if (long.TryParse(timestamp, out long result))
                return result;

            if (timestamp.Equals("now", StringComparison.OrdinalIgnoreCase))
                return GetCurrentTimestampUtc();

            throw new ArgumentException(nameof(timestamp));

        }

        public static long GetMinTimestamp() {

            return GetTimestampFromDate(DateTimeOffset.MinValue);

        }
        public static long GetMaxTimestamp() {

            return GetTimestampFromDate(DateTimeOffset.MaxValue);

        }

    }

}