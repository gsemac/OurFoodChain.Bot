using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace OurFoodChain.Drawing {

    public abstract class CladogramRendererBase :
        ICladogramRenderer {

        // Public members

        public ITaxonFormatter TaxonFormatter { get; set; } = new BinomialNameTaxonFormatter();

        public Color BackgroundColor { get; set; } = Color.FromArgb(54, 57, 63);
        public Color TextColor { get; set; } = Color.White;
        public Color LineColor { get; set; } = Color.White;
        public Color HighlightColor { get; set; } = Color.Yellow;

        public SizeF Size => GetTreeBounds().Size;

        public abstract void Render(ICanvas canvas);

        // Protected members

        protected abstract RectangleF GetTreeBounds();

    }

}