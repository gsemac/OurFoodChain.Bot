using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain {

    public enum TimeUnits {
        Seconds = 1,
        Minutes,
        Hours,
        Days,
        Weeks,
        Months,
        Years
    }

    public class TimeAmount {

        public TimeUnits Unit { get; set; } = 0;
        public long Amount { get; set; } = 0;

        public TimeAmount() {
        }
        public TimeAmount(long amount, TimeUnits unit) {
            Amount = amount;
            Unit = unit;
        }

        public override string ToString() {

            StringBuilder sb = new StringBuilder();

            sb.Append(Amount);
            sb.Append(" ");

            switch (Unit) {

                case TimeUnits.Seconds:
                    sb.Append(Amount == 1 ? "second" : "seconds");
                    break;

                case TimeUnits.Minutes:
                    sb.Append(Amount == 1 ? "minute" : "minutes");
                    break;

                case TimeUnits.Hours:
                    sb.Append(Amount == 1 ? "hour" : "hours");
                    break;

                case TimeUnits.Days:
                    sb.Append(Amount == 1 ? "day" : "days");
                    break;

                case TimeUnits.Weeks:
                    sb.Append(Amount == 1 ? "week" : "weeks");
                    break;

                case TimeUnits.Months:
                    sb.Append(Amount == 1 ? "month" : "months");
                    break;

                case TimeUnits.Years:
                    sb.Append(Amount == 1 ? "year" : "years");
                    break;

                default:
                    sb.Append("units");
                    break;

            }

            return sb.ToString();

        }

        public long ToUnixTimeSeconds() {

            switch (Unit) {

                case TimeUnits.Seconds:
                    return Amount;

                case TimeUnits.Minutes:
                    return Amount * 60;

                case TimeUnits.Hours:
                    return Amount * 60 * 60;

                case TimeUnits.Days:
                    return Amount * 60 * 60 * 24;

                case TimeUnits.Weeks:
                    return Amount * 60 * 60 * 24 * 7;

                case TimeUnits.Months:
                    return Amount * 60 * 60 * 24 * 30;

                case TimeUnits.Years:
                    return Amount * 60 * 60 * 24 * 365;

                default:
                    throw new Exception("Invalid time unit");

            }

        }

        public TimeAmount Reduce() {

            long seconds = ToUnixTimeSeconds();
            TimeSpan span = TimeSpan.FromSeconds(seconds);

            if (span.TotalDays >= 365)
                return ConvertTo(TimeUnits.Years);
            else if (span.TotalDays >= 30)
                return ConvertTo(TimeUnits.Months);
            else if (span.TotalDays >= 7)
                return ConvertTo(TimeUnits.Weeks);
            else if (span.TotalDays >= 1)
                return ConvertTo(TimeUnits.Days);
            else if (span.TotalHours >= 1)
                return ConvertTo(TimeUnits.Hours);
            else if (span.TotalMinutes >= 1)
                return ConvertTo(TimeUnits.Minutes);
            else
                return ConvertTo(TimeUnits.Seconds);

        }
        public TimeAmount ConvertTo(TimeUnits unit) {

            long seconds = ToUnixTimeSeconds();
            TimeSpan span = TimeSpan.FromSeconds(seconds);

            switch (unit) {

                case TimeUnits.Seconds:
                    return new TimeAmount((long)span.TotalSeconds, TimeUnits.Seconds);

                case TimeUnits.Minutes:
                    return new TimeAmount((long)span.TotalMinutes, TimeUnits.Minutes);

                case TimeUnits.Hours:
                    return new TimeAmount((long)span.TotalHours, TimeUnits.Hours);

                case TimeUnits.Days:
                    return new TimeAmount((long)span.TotalDays, TimeUnits.Days);

                case TimeUnits.Weeks:
                    return new TimeAmount((long)span.TotalDays / 7, TimeUnits.Weeks);

                case TimeUnits.Months:
                    return new TimeAmount((long)span.TotalDays / 30, TimeUnits.Months);

                case TimeUnits.Years:
                    return new TimeAmount((long)span.TotalDays / 365, TimeUnits.Years);

                default:
                    throw new Exception("Invalid time unit");

            }

        }

        public static TimeAmount Parse(string input) {

            Match m = Regex.Match(input, @"(\d+)(\w*)");

            if (!m.Success || !int.TryParse(m.Groups[1].Value, out int amount))
                return null;

            TimeUnits unit = 0;

            switch (m.Groups[2].Value.ToLower()) {

                case "s":
                case "sec":
                case "secs":
                case "second":
                case "seconds":
                    unit = TimeUnits.Seconds;
                    break;

                case "m":
                case "min":
                case "mins":
                case "minute":
                case "minutes":
                    unit = TimeUnits.Minutes;
                    break;

                case "h":
                case "hour":
                case "hours":
                    unit = TimeUnits.Hours;
                    break;

                case "d":
                case "day":
                case "days":
                    unit = TimeUnits.Days;
                    break;

                case "w":
                case "week":
                case "weeks":
                    unit = TimeUnits.Weeks;
                    break;

                case "mo":
                case "mos":
                case "month":
                case "months":
                    unit = TimeUnits.Months;
                    break;

                case "y":
                case "year":
                case "years":
                    unit = TimeUnits.Years;
                    break;

            }

            if (unit == 0)
                return null;

            return new TimeAmount {
                Amount = amount,
                Unit = unit
            };

        }

    }

}