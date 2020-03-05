using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Extensions {

    public static class TaxonExtensions {

        public static string GetDescriptionOrDefault(this ITaxon taxon) {

            if (taxon is null || string.IsNullOrWhiteSpace(taxon.Description))
                return Constants.DefaultDescription;

            return taxon.Description;

        }

        public static bool IsValid(this ITaxon taxon) {

            return taxon != null && taxon.Id.HasValue && taxon.Id >= 0;

        }

    }

}