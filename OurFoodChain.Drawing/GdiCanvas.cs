using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace OurFoodChain.Drawing {

    public sealed class GdiCanvas :
        ICanvas {

        // Public members

        public GdiCanvas(int width, int height) {

            canvas = new Bitmap(width, height);
            graphics = Graphics.FromImage(canvas);

            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        }

        public void Clear(Color color) {

            graphics.Clear(color);

        }
        public void DrawLine(Pen pen, PointF start, PointF end) {

            graphics.DrawLine(pen, start, end);

        }
        public void DrawText(string text, PointF position, Font font, Color color) {

            using (Brush brush = new SolidBrush(color))
                graphics.DrawString(text, font, brush, position);

        }

        public SizeF MeasureText(string text, Font font) {

            return graphics.MeasureString(text, font);

        }

        public void Save(Stream stream) {

            canvas.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

        }

        public void Dispose() {

            if (graphics != null)
                graphics.Dispose();

            if (canvas != null)
                canvas.Dispose();

            graphics = null;
            canvas = null;

        }

        // Private members

        private Bitmap canvas;
        private Graphics graphics;

    }

}