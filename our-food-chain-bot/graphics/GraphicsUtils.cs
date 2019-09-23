using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    public class GraphicsUtils {

        // https://stackoverflow.com/questions/1003370/measure-a-string-without-using-a-graphics-object

        public static SizeF MeasureString(string s, Font font) {

            SizeF result;

            using (var image = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(image))
                result = g.MeasureString(s, font);

            return result;
        }

        // https://stackoverflow.com/questions/33853434/how-to-draw-a-rounded-rectangle-in-c-sharp

        public static GraphicsPath RoundedRect(Rectangle bounds, int radius) {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0) {
                path.AddRectangle(bounds);
                return path;
            }

            // top left arc  
            path.AddArc(arc, 180, 90);

            // top right arc  
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right arc  
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left arc 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        // https://stackoverflow.com/questions/33853434/how-to-draw-a-rounded-rectangle-in-c-sharp

        public static void DrawRoundedRectangle(Graphics graphics, Pen pen, Rectangle bounds, int cornerRadius) {

            if (graphics == null)
                throw new ArgumentNullException("graphics");

            if (pen == null)
                throw new ArgumentNullException("pen");


            using (GraphicsPath path = RoundedRect(bounds, cornerRadius)) {
                graphics.DrawPath(pen, path);
            }

        }

        // https://stackoverflow.com/questions/33853434/how-to-draw-a-rounded-rectangle-in-c-sharp

        public static void FillRoundedRectangle(Graphics graphics, Brush brush, Rectangle bounds, int cornerRadius) {

            if (graphics == null)
                throw new ArgumentNullException("graphics");

            if (brush == null)
                throw new ArgumentNullException("brush");

            using (GraphicsPath path = RoundedRect(bounds, cornerRadius)) {
                graphics.FillPath(brush, path);
            }

        }

        // https://stackoverflow.com/questions/3722307/is-there-an-easy-way-to-blend-two-system-drawing-color-values

        public static Color Blend(Color color, Color backColor, double amount) {

            byte r = (byte)((color.R * amount) + backColor.R * (1 - amount));
            byte g = (byte)((color.G * amount) + backColor.G * (1 - amount));
            byte b = (byte)((color.B * amount) + backColor.B * (1 - amount));

            return Color.FromArgb(r, g, b);

        }

    }

}