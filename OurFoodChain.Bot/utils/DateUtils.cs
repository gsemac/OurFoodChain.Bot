using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public enum TimestampToDateStringFormat {
        Default,
        Short,
        Long
    }

    public class DateUtils {

        public static long GetCurrentTimestamp() {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        public static long GetMaxTimestamp() {

            // Allows us to use the TimeSpan class with timestamps.
            // If this were to be long.MaxValue, it would surpass the maximum number of ticks.

            return (long)TimeSpan.MaxValue.TotalSeconds;

        }
        public static long ParseTimestamp(string input) {

            if (long.TryParse(input, out long result))
                return result;

            if (input.ToLower() == "now")
                return GetCurrentTimestamp();

            throw new Exception("Unable to parse timestamp string");

        }

        public static DateTime TimestampToDateTime(long timestamp) {

            return DateTimeOffset.FromUnixTimeSeconds(timestamp).Date.ToUniversalTime();

        }
        public static DateTimeOffset TimestampToDateTimeOffset(long timestamp) {

            return DateTimeOffset.FromUnixTimeSeconds(timestamp).ToUniversalTime();

        }

        public static string TimestampToShortDateString(long timestamp) {

            return TimestampToDateTime(timestamp).ToShortDateString();

        }
        public static string TimestampToDateString(long timestamp, TimestampToDateStringFormat format) {

            switch (format) {

                case TimestampToDateStringFormat.Short:
                    return TimestampToShortDateString(timestamp);

                case TimestampToDateStringFormat.Long:
                    return TimestampToLongDateString(timestamp);

                default:
                    goto case TimestampToDateStringFormat.Long;

            }

        }
        public static string TimestampToDateString(long timestamp, string format) {

            return TimestampToDateTime(timestamp).ToString(format);

        }
        public static string TimestampToLongDateString(long timestamp) {

            DateTime date = TimestampToDateTime(timestamp);

            string day_string = date.Day.ToString();

            if (day_string.Last() == '1' && !day_string.EndsWith("11"))
                day_string += "st";
            else if (day_string.Last() == '2' && !day_string.EndsWith("12"))
                day_string += "nd";

            else if (day_string.Last() == '3' && !day_string.EndsWith("13"))
                day_string += "rd";
            else
                day_string += "th";

            return string.Format("{1:MMMM} {0}, {1:yyyy}", day_string, date);

        }

        public static string TimeSpanToString(TimeSpan span) {

            string format = "{0} {1}";

            if (span < TimeSpan.FromSeconds(60))
                return string.Format(format, span.Seconds, span.Seconds == 1 ? "second" : "seconds");
            else if (span < TimeSpan.FromMinutes(60))
                return string.Format(format, span.Minutes, span.Minutes == 1 ? "minute" : "minutes");
            else if (span < TimeSpan.FromHours(24))
                return string.Format(format, span.Hours, span.Hours == 1 ? "hour" : "hours");
            else if (span < TimeSpan.FromDays(30))
                return string.Format(format, span.Days, span.Days == 1 ? "day" : "days");
            else if (span < TimeSpan.FromDays(365))
                return string.Format(format, span.Days / 30, span.Days / 30 == 1 ? "day" : "days");
            else
                return string.Format(format, span.Days / 365, span.Days / 365 == 1 ? "year" : "years");

        }

    }

}
