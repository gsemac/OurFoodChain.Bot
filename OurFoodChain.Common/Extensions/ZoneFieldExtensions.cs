using OurFoodChain.Common.Zones;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OurFoodChain.Common.Extensions {

    public static class ZoneFieldExtensions {

        public static string GetName(this IZoneField field) {

            string result = field.Name.ToTitle();

            result = Regex.Replace(result, @"\b(?:ph)\b", "pH", RegexOptions.IgnoreCase);

            return result;

        }
        public static string GetValue(this IZoneField field) {

            string result = field.Value.ToLowerInvariant();

            result = Regex.Replace(result, @"°[cf]", m => m.Value.ToUpperInvariant(), RegexOptions.IgnoreCase);

            return result;

        }

    }

}
