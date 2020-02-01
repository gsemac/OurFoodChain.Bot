using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;

namespace OurFoodChain.Drawing {

    public sealed class SkiaCanvas :
        ICanvas {

        // Public members

        public SkiaCanvas(int width, int height) {

            SKImageInfo imageInfo = new SKImageInfo(width, height);

            this.surface = SKSurface.Create(imageInfo);
            this.paint = new SKPaint();

            paint.IsAntialias = true;

        }

        public void Clear(Color color) {

            surface.Canvas.Clear(ConvertColor(color));

        }
        public void DrawLine(Pen pen, PointF start, PointF end) {

            paint.Color = ConvertColor(pen.Color);
            paint.StrokeWidth = pen.Width;
            paint.StrokeCap = ConvertStrokeCap(pen.EndCap);

            surface.Canvas.DrawLine(ConvertPoint(start), ConvertPoint(end), paint);

        }
        public void DrawText(string text, PointF position, Font font, Color color) {

            paint.Color = ConvertColor(color);

            SetFontIfNew(font);

            surface.Canvas.DrawText(text, position.X, position.Y, paint);

        }

        public SizeF MeasureText(string text, Font font) {

            SetFontIfNew(font);

            float w = paint.FontMetrics.XHeight;
            float h = paint.MeasureText(text);

            return new SizeF(w, h);

        }

        public void SaveTo(Stream stream) {

            using (SKImage image = surface.Snapshot())
            using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                data.SaveTo(stream);

        }

        public void Dispose() {

            if (surface != null)
                surface.Dispose();

            if (paint != null)
                paint.Dispose();

            surface = null;
            paint = null;

        }

        // Private members

        private SKSurface surface;
        private SKPaint paint;

        private void SetFontIfNew(Font font) {

            if (paint.Typeface is null || paint.Typeface.FamilyName != font.FontFamily.Name || paint.TextSize != font.Size) {

                if (paint.Typeface != null)
                    paint.Typeface.Dispose();

                paint.Typeface = SKTypeface.FromFamilyName(font.FontFamily.Name);
                paint.TextSize = font.Size;

            }

        }

        private static SKColor ConvertColor(Color color) {

            return new SKColor(color.R, color.G, color.B);

        }
        private static SKPoint ConvertPoint(PointF point) {

            return new SKPoint(point.X, point.Y);

        }
        private static SKStrokeCap ConvertStrokeCap(LineCap cap) {

            switch (cap) {

                case LineCap.Round:
                    return SKStrokeCap.Round;

                case LineCap.Flat:
                    return SKStrokeCap.Butt;

                case LineCap.Square:
                    return SKStrokeCap.Square;

                default:
                    return SKStrokeCap.Butt;

            }

        }

    }

}