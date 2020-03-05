using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Extensions {

    public static class TaxonExtensions {

        public static bool IsNull(this ITaxon taxon) {

            return taxon is null || !taxon.Id.HasValue || taxon.Id < 0;

        }

    }

}