using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain {

    public enum TimeUnit {
        Seconds = 1,
        Minutes,
        Hours,
        Days,
        Weeks,
        Months,
        Years
    }

    public class TimeAmount {

        public TimeUnit Unit { get; set; } = 0;
        public int Amount { get; set; } = 0;

        public override string ToString() {

            StringBuilder sb = new StringBuilder();

            sb.Append(Amount);
            sb.Append(" ");

            switch (Unit) {

                case TimeUnit.Seconds:
                    sb.Append(Amount == 1 ? "second" : "seconds");
                    break;

                case TimeUnit.Minutes:
                    sb.Append(Amount == 1 ? "minute" : "minutes");
                    break;

                case TimeUnit.Hours:
                    sb.Append(Amount == 1 ? "hour" : "hours");
                    break;

                case TimeUnit.Days:
                    sb.Append(Amount == 1 ? "day" : "days");
                    break;

                case TimeUnit.Weeks:
                    sb.Append(Amount == 1 ? "week" : "weeks");
                    break;

                case TimeUnit.Months:
                    sb.Append(Amount == 1 ? "month" : "months");
                    break;

                case TimeUnit.Years:
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

                case TimeUnit.Seconds:
                    return Amount;

                case TimeUnit.Minutes:
                    return Amount * 60;

                case TimeUnit.Hours:
                    return Amount * 60 * 60;

                case TimeUnit.Days:
                    return Amount * 60 * 60 * 24;

                case TimeUnit.Weeks:
                    return Amount * 60 * 60 * 24 * 7;

                case TimeUnit.Months:
                    return Amount * 60 * 60 * 24 * 30;

                case TimeUnit.Years:
                    return Amount * 60 * 60 * 24 * 365;

                default:
                    return 0;

            }

        }

        public static TimeAmount Parse(string input) {

            Match m = Regex.Match(input, @"(\d+)(\w*)");

            if (!m.Success || !int.TryParse(m.Groups[1].Value, out int amount))
                return null;

            TimeUnit unit = 0;

            switch (m.Groups[2].Value.ToLower()) {

                case "s":
                case "sec":
                case "secs":
                case "second":
                case "seconds":
                    unit = TimeUnit.Seconds;
                    break;

                case "m":
                case "min":
                case "mins":
                case "minute":
                case "minutes":
                    unit = TimeUnit.Minutes;
                    break;

                case "h":
                case "hour":
                case "hours":
                    unit = TimeUnit.Hours;
                    break;

                case "d":
                case "day":
                case "days":
                    unit = TimeUnit.Days;
                    break;

                case "w":
                case "week":
                case "weeks":
                    unit = TimeUnit.Weeks;
                    break;

                case "mo":
                case "mos":
                case "month":
                case "months":
                    unit = TimeUnit.Months;
                    break;

                case "y":
                case "year":
                case "years":
                    unit = TimeUnit.Years;
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
