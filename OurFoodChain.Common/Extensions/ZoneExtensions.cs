using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Extensions {

    public static class ZoneExtensions {

        public static string GetShortName(this IZone zone) {

            return ZoneUtilities.GetShortName(zone.Name);

        }
        public static string GetFullName(this IZone zone) {

            return ZoneUtilities.GetFullName(zone.Name);

        }
        public static string GetShortDescription(this IZone zone) {

            return zone.GetDescriptionOrDefault().GetFirstSentence();

        }
        public static string GetDescriptionOrDefault(this IZone zone) {

            if (zone is null || string.IsNullOrWhiteSpace(zone.Description))
                return Constants.DefaultDescription;

            return zone.Description;

        }

        public static bool IsValid(this IZone zone) {

            return zone != null && zone.Id.HasValue && zone.Id >= 0;

        }

    }

}