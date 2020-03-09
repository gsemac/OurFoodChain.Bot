using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OurFoodChain.Common.Utilities {

    public enum DateStringFormat {
        Short,
        Long
    }

    public enum TimeSpanFormat {
        Years,
        Months,
        Weeks,
        Days,
        Hours,
        Minutes,
        Seconds,
        Smallest
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

        public static string GetTimeSpanString(TimeSpan timeSpan, TimeSpanFormat format = TimeSpanFormat.Smallest) {

            int years = timeSpan.Days / 365;
            int months = timeSpan.Days / 30;
            int weeks = timeSpan.Days / 7;
            int days = timeSpan.Days;
            int hours = timeSpan.Hours;
            int minutes = timeSpan.Minutes;
            int seconds = timeSpan.Seconds;

            if (years >= 1 || format == TimeSpanFormat.Years)
                return string.Format("{0} {1}", years, years == 1 ? "year" : "years");

            if (months >= 1 || format == TimeSpanFormat.Months)
                return string.Format("{0} {1}", months, months == 1 ? "month" : "months");

            if (weeks >= 1 || format == TimeSpanFormat.Weeks)
                return string.Format("{0} {1}", weeks, weeks == 1 ? "week" : "weeks");

            if (days >= 1 || format == TimeSpanFormat.Days)
                return string.Format("{0} {1}", days, days == 1 ? "day" : "days");

            if (hours >= 1 || format == TimeSpanFormat.Hours)
                return string.Format("{0} {1}", hours, hours == 1 ? "hour" : "hours");

            if (minutes >= 1 || format == TimeSpanFormat.Minutes)
                return string.Format("{0} {1}", minutes, minutes == 1 ? "minute" : "minutes");

            return string.Format("{0} {1}", seconds, seconds == 1 ? "second" : "seconds");

        }
        public static bool TryParseTimeSpan(string input, out TimeSpan result) {

            Match m = Regex.Match(input, @"(\d+)(\w*)");

            if (!m.Success || !int.TryParse(m.Groups[1].Value, out int amount))
                return false;

            switch (m.Groups[2].Value.ToLowerInvariant()) {

                case "s":
                case "sec":
                case "secs":
                case "second":
                case "seconds":
                    result = TimeSpan.FromSeconds(amount);
                    break;

                case "m":
                case "min":
                case "mins":
                case "minute":
                case "minutes":
                    result = TimeSpan.FromMinutes(amount);
                    break;

                case "h":
                case "hour":
                case "hours":
                    result = TimeSpan.FromHours(amount);
                    break;

                case "d":
                case "day":
                case "days":
                    result = TimeSpan.FromDays(amount);
                    break;

                case "w":
                case "week":
                case "weeks":
                    result = TimeSpan.FromDays(amount * 7);
                    break;

                case "mo":
                case "mos":
                case "month":
                case "months":
                    result = TimeSpan.FromDays(amount * 30);
                    break;

                case "y":
                case "year":
                case "years":
                    result = TimeSpan.FromDays(amount * 365);
                    break;

            }

            return result != null;

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