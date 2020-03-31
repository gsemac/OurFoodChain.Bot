using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace OurFoodChain.Drawing.Extensions {

    public static class CladogramRendererExtensions {

        public static void Save(this ICladogramRenderer cladogramRenderer, string filePath) {

            SizeF bounds = cladogramRenderer.Size;

            using (GdiCanvas canvas = new GdiCanvas((int)bounds.Width, (int)bounds.Height)) {

                canvas.Clear(Color.FromArgb(54, 57, 63));

                cladogramRenderer.Render(canvas);

                canvas.Save(filePath);

            }

        }

    }

}