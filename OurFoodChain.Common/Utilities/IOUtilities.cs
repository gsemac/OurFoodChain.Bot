using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public static class IOUtilities {

        public static bool TryDeleteFile(string filePath) {

            if (!System.IO.File.Exists(filePath))
                return false;

            try {

                System.IO.File.Delete(filePath);

                return true;

            }
            catch (Exception) {

                return false;

            }

        }

    }

}
