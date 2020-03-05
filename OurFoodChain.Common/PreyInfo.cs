using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public class PreyInfo :
        IPreyInfo {

        public ISpecies Prey { get; set; }
        public string Notes { get; set; }

    }

}