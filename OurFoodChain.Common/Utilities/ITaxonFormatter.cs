using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public interface ITaxonFormatter {

        string GetString(ISpecies species);
        string GetString(ITaxon taxon);

    }

}