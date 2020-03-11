using OurFoodChain.Gotchis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Extensions {

    public static class GotchiExtensions {

        public static bool IsValid(this Gotchi gotchi) {

            return gotchi != null;

        }

    }

}