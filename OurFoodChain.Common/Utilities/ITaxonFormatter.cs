using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public enum ExtinctNameFormat {
        Strikethrough,
        Dagger,
        Default = Strikethrough
    }

    public interface ITaxonFormatter {

        string GetString(ISpecies species);
        string GetString(ITaxon taxon);

        string GetString(ISpecies species, bool isExtinct);
        string GetString(ITaxon taxon, bool isExtinct);

    }

}