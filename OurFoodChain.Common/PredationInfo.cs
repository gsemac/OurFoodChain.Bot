using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public class PredationInfo :
        IPredationInfo {

        public ISpecies Species { get; set; }
        public string Notes { get; set; } = "";

    }

}