using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public abstract class TaxonFormatterBase :
        ITaxonFormatter {

        public virtual string GetString(ISpecies species) {

            return GetString((ITaxon)species);

        }
        public abstract string GetString(ITaxon taxon);

    }

}