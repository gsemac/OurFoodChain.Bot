using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain {

    static class IoUtils {

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
