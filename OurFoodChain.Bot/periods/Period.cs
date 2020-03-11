using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class Period {

        public const string DATE_FORMAT = "d/m/yyyy";

        public long id = -1;
        public string name = "";
        public string description = "";
        public string start_ts = "";
        public string end_ts = "";

        public string GetName() {

            return StringUtilities.ToTitleCase(name);

        }
        public string GetDescriptionOrDefault() {

            if (string.IsNullOrEmpty(description))
                return DEFAULT_DESCRIPTION;

            return description;

        }
        public long GetStartTimestamp() {

            return _parseTimestamp(start_ts);

        }
        public string GetStartTimestampString() {

            if (start_ts == "now")
                return "Now";

            return DateUtils.TimestampToDateString(_parseTimestamp(start_ts), "MMM dd, yyyy");

        }
        public long GetEndTimestamp() {

            return _parseTimestamp(end_ts);

        }
        public string GetEndTimestampString() {

            if (end_ts == "now")
                return "Now";

            return DateUtils.TimestampToDateString(_parseTimestamp(end_ts), "MMM dd, yyyy");

        }
        public string GetHowLongString() {

            long ts = GetEndTimestamp() - GetStartTimestamp();
            TimeSpan span = TimeSpan.FromSeconds(ts);

            return DateUtils.TimeSpanToString(span);

        }
        public string GetHowLongAgoString() {

            if (end_ts == "now")
                return "current";

            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - GetEndTimestamp();

            if (ts < 0)
                return "in the future";

            TimeSpan span = TimeSpan.FromSeconds(ts);

            return string.Format("{0} ago", DateUtils.TimeSpanToString(span));

        }

        public static bool TryParseDate(string dateString, out DateTime dateTime) {

            if (dateString == "now") {

                dateTime = DateTime.UtcNow;

                return true;

            }

            return DateTime.TryParseExact(dateString, DATE_FORMAT, null, System.Globalization.DateTimeStyles.None, out dateTime);

        }
        
        private const string DEFAULT_DESCRIPTION = BotUtils.DEFAULT_DESCRIPTION;

        private long _parseTimestamp(string timestamp) {

            if (long.TryParse(timestamp, out long result))
                return result;

            if (timestamp == "now")
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            return 0;

        }

    }

}