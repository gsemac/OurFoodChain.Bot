using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Discord.Extensions {

    public static class ColorExtensions {

        public static System.Drawing.Color ToSystemDrawingColor(this Color color) {

            return System.Drawing.Color.FromArgb(color.R, color.G, color.B);

        }
        public static Color ToDiscordColor(this System.Drawing.Color color) {

            return new Color(color.R, color.G, color.B);

        }

    }

}