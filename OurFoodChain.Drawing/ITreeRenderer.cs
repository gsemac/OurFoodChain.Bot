using OurFoodChain.Common.Collections;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace OurFoodChain.Drawing {

    public interface ITreeRenderer<T> :
        IDisposable {

        Color BackgroundColor { get; set; }
        Color TextColor { get; set; }
        Color LineColor { get; set; }
        SizeF Size { get; }

        void DrawTo(ICanvas canvas);

    }

}