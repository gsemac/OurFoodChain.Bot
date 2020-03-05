using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Extensions {

    public static class TaxonExtensions {

        public static bool IsValid(this ITaxon taxon) {

            return taxon != null && taxon.Id.HasValue && taxon.Id >= 0;

        }

    }

}