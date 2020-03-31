using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace OurFoodChain.Drawing {

    public interface ICladogramRenderer {

        Color BackgroundColor { get; set; }
        Color TextColor { get; set; }
        Color LineColor { get; set; }
        Color HighlightColor { get; set; }
        SizeF Size { get; }

        ITaxonFormatter TaxonFormatter { get; set; }

        void Render(ICanvas canvas);

    }

}