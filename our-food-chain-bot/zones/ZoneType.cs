using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class ZoneType {

        public const int NullZoneTypeId = -1;
        public const string DefaultColorHex = "#4f545c";
        public static readonly Color DefaultColor = ColorTranslator.FromHtml(DefaultColorHex); // Discord.Color.DarkGrey
        public const string DefaultIcon = "❓";

        public long Id { get; set; } = NullZoneTypeId;
        public string Name { get; set; } = "Unclassified";
        public string Icon { get; set; } = DefaultIcon;
        public Color Color { get; set; } = DefaultColor;
        public string Description { get; set; } = "";

        public bool SetColor(string colorHex) {

            try {

                Color = ColorTranslator.FromHtml(colorHex);

                return true;

            }
            catch (Exception) {

                Color = DefaultColor;

                return false;

            }

        }

    }

}