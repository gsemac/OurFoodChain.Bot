using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public enum DateStringFormat {
        Short,
        Long
    }

    public static class DateUtilities {

        // Public members

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

        public static string GetDateString(long timestamp, DateStringFormat format) {

            return GetDateString(GetDateFromTimestamp(timestamp), format);

        }
        public static string GetDateString(DateTimeOffset date, DateStringFormat format) {

            switch (format) {

                default:
                case DateStringFormat.Short:
                    return GetShortDateString(date);

                case DateStringFormat.Long:
                    return GetLongDateString(date);

            }

        }

        // Private members

        private static string GetShortDateString(DateTimeOffset date) {

            return date.Date.ToShortDateString();

        }
        private static string GetLongDateString(DateTimeOffset date) {

            string dayString = date.Day.ToString();

            if (dayString.Last() == '1' && !dayString.EndsWith("11"))
                dayString += "st";
            else if (dayString.Last() == '2' && !dayString.EndsWith("12"))
                dayString += "nd";

            else if (dayString.Last() == '3' && !dayString.EndsWith("13"))
                dayString += "rd";
            else
                dayString += "th";

            return string.Format("{1:MMMM} {0}, {1:yyyy}", dayString, date);

        }

    }

}