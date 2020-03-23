using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public class CommonNameTaxonFormatter :
        BinomialNameTaxonFormatter {

        public override string GetString(ISpecies species) {

            return GetString((ITaxon)species);

        }
        public override string GetString(ITaxon taxon) {

            string result = taxon.GetCommonName();

            if (string.IsNullOrEmpty(result))
                return base.GetString(taxon);

            return result;

        }

    }

}