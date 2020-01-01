using OurFoodChain.Utilities;
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
        public string Name {
            get {
                return StringUtilities.ToTitleCase(_name);
            }
            set {
                _name = value;
            }
        }
        public string Icon { get; set; } = DefaultIcon;
        public Color Color { get; set; } = DefaultColor;
        public string Description { get; set; } = "";

        public bool SetColor(string colorHex) {

            if (StringUtilities.TryParseColor(colorHex, out Color result)) {

                Color = result;

                return true;

            }

            return false;

        }
        public bool SetColor(Color color) {

            Color = color;

            return true;

        }

        private string _name = "Unclassified";

    }

}