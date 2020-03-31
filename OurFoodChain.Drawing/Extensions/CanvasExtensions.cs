using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OurFoodChain.Drawing.Extensions {

    public static class CanvasExtensions {

        public static void Save(this ICanvas canvas, string filePath) {

            string directoryPath = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                canvas.Save(fs);

        }

    }

}
