using OurFoodChain.Common.Extensions;
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

            // Zone names should be formatted as "Zone X" if:
            // - they are a single character (e.g. "Zone A" or "Zone 1")
            // - they are two letters (DA), digits followed by a single letter (28A), or a letter followed by a single digit (A1)

            zoneName = zoneName.ToLowerInvariant();

            if (Regex.IsMatch(zoneName, @"^(?:zone\s+)?(?:\d+[a-z]{0,2}|[a-z]\d|[a-z]{1,2})$", RegexOptions.IgnoreCase) || zoneName.Length == 1)
                zoneName = "Zone " + zoneName.After("zone").Trim().ToUpperInvariant();
            else
                zoneName = zoneName.ToTitle();

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