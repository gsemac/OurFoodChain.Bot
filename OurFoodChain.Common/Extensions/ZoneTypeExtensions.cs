using OurFoodChain.Common.Utilities;
using OurFoodChain.Common.Zones;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace OurFoodChain.Common.Extensions {

    public static class ZoneTypeExtensions {

        public static bool SetColor(this IZoneType zoneType, string hexColor) {

            if (StringUtilities.TryParseColor(hexColor, out Color result)) {

                zoneType.Color = result;

                return true;

            }

            return false;

        }
        public static void SetColor(this IZoneType zoneType, Color color) {

            zoneType.Color = color;

        }

        public static bool IsValid(this IZoneType zoneType) {

            return zoneType != null && zoneType.Id.HasValue && zoneType.Id >= 0;

        }

    }

}