using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common {

    public interface IPreyInfo {

        ISpecies Prey { get; set; }
        string Notes { get; set; }

    }

}