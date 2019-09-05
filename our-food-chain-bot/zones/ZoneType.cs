using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class ZoneType {

        public const int NullZoneTypeId = -1;
        public static readonly Color DefaultColor = ColorTranslator.FromHtml("#4f545c"); // Discord.Color.DarkGrey

        public long Id { get; set; } = NullZoneTypeId;
        public string Name { get; set; } = "Unclassified";
        public string Icon { get; set; } = "❓";
        public Color Color { get; set; } = DefaultColor;
        public string Description { get; set; } = "";

    }

}