using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace OurFoodChain.Common.Zones {

    public abstract class ZoneTypeBase :
        IZoneType {

        // Public members

        public const string DefaultIcon = "❓";
        public static readonly Color DefaultColor = ColorTranslator.FromHtml("#4f545c"); // Discord.Color.DarkGrey

        public long? Id { get; set; }

        public string Icon { get; set; } = DefaultIcon;
        public string Name { get; set; } = "Unclassified";
        public string Description { get; set; }

        public Color Color { get; set; } = DefaultColor;

    }

}