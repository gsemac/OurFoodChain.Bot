using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace OurFoodChain.Drawing {

    public static class DrawingUtilities {

        public static SizeF MeasureText(string text, Font font) {

            using (Bitmap image = new Bitmap(1, 1))
            using (Graphics graphics = Graphics.FromImage(image))
                return graphics.MeasureString(text, font);

        }

    }

}