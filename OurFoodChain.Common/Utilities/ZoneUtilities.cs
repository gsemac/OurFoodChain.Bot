using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OurFoodChain.Common.Utilities {

    public static class ZoneUtilities {

        public static string GetShortName(string zoneName) {

            return Regex.Replace(zoneName, "^zone\\s+", "", RegexOptions.IgnoreCase);

        }
        public static string GetFullName(string zoneName) {

            if (StringUtilities.IsNumeric(zoneName) || zoneName.Length == 1)
                zoneName = "zone " + zoneName;

            zoneName = StringUtilities.ToTitleCase(zoneName);

            return zoneName;

        }

        public static IEnumerable<string> ParseZoneNameList(string delimitedZoneNames) {

            if (string.IsNullOrWhiteSpace(delimitedZoneNames))
                return Enumerable.Empty<string>();

            string[] names = delimitedZoneNames.Split(',', '/');

            for (int i = 0; i < names.Count(); ++i)
                names[i] = names[i].Trim().ToLowerInvariant();

            return names.Where(name => !string.IsNullOrEmpty(name));

        }

    }

}