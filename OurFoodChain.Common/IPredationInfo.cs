using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public interface IPredationInfo {

        ISpecies Species { get; set; }
        string Notes { get; set; }

    }

}