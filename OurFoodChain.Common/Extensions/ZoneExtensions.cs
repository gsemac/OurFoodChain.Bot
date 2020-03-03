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
        public static string GetDescriptionOrDefault(this IZone zone) {

            if (zone is null || string.IsNullOrWhiteSpace(zone.Description))
                return "No description provided.";

            return zone.Description;

        }

        public static bool IsNull(this IZone zone) {

            return zone is null || !zone.Id.HasValue;

        }

    }

}