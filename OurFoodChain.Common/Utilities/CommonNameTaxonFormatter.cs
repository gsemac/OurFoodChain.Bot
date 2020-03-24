using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public class CommonNameTaxonFormatter :
        BinomialNameTaxonFormatter {

        // Public members

        public override string GetString(ITaxon taxon) {

            string name = taxon.GetCommonName();

            if (string.IsNullOrEmpty(name))
                return base.GetString(taxon);

            return name;

        }

    }

}