using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace OurFoodChain.Drawing {

    public interface ICanvas :
        IDisposable {

        void DrawLine(Pen pen, PointF start, PointF end);
        void DrawText(string text, PointF position, Font font, Color color);
        void Clear(Color color);

        SizeF MeasureText(string text, Font font);

        void SaveTo(Stream stream);

    }

}