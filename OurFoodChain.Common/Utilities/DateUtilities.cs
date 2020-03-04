using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public static class DateUtilities {

        public static DateTimeOffset GetCurrentUtcDate() {

            return DateTimeOffset.UtcNow;

        }
        public static long GetCurrentUtcTimestamp() {

            return GetCurrentUtcDate().ToUnixTimeSeconds();

        }
        public static DateTimeOffset GetCurrentDate() {

            return DateTimeOffset.Now;

        }
        public static long GetCurrentTimestamp() {

            return GetCurrentDate().ToUnixTimeSeconds();

        }

        public static DateTimeOffset TimestampToDate(long timestamp) {

            return DateTimeOffset.FromUnixTimeSeconds(timestamp);

        }
        public static DateTimeOffset TimestampToDate(string timestamp) {

            if (long.TryParse(timestamp, out long result))
                return TimestampToDate(result);

            if (timestamp.Equals("now", StringComparison.OrdinalIgnoreCase))
                return GetCurrentUtcDate();

            throw new ArgumentException(nameof(timestamp));

        }
        public static long DateToTimestamp(DateTimeOffset offset) {

            return offset.ToUnixTimeSeconds();

        }

    }

}