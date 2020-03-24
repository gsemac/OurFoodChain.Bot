using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Utilities {

    public class BinomialNameTaxonFormatter :
        TaxonFormatterBase {

        public BinomialNameFormat NameFormat { get; set; } = BinomialNameFormat.Abbreviated;

        public override string GetString(ITaxon taxon) {

            if (taxon is ISpecies species)
                return species.BinomialName.ToString(NameFormat);

            return taxon.GetName();

        }

    }

}