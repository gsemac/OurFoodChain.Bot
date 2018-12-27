using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    class GraphicsUtils {

        // https://stackoverflow.com/questions/1003370/measure-a-string-without-using-a-graphics-object

        public static SizeF MeasureString(string s, Font font) {

            SizeF result;

            using (var image = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(image))
                result = g.MeasureString(s, font);

            return result;
        }

    }

}

