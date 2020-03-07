using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Common.Extensions {

    public static class TaxonExtensions {

        public static string GetDescriptionOrDefault(this ITaxon taxon) {

            if (taxon is null || string.IsNullOrWhiteSpace(taxon.Description))
                return Constants.DefaultDescription;

            return taxon.Description;

        }

        public static string GetName(this ITaxon taxon) {

            if (taxon is null)
                return "?";

            if (taxon.Rank?.Type == TaxonRankType.Species)
                return taxon.Name.ToLowerInvariant();
            else
                return taxon.Name.ToTitle();

        }
        public static string GetCommonName(this ITaxon taxon) {

            if (taxon is null || taxon.CommonNames.Count() <= 0)
                return string.Empty;

            return taxon.CommonNames.First().ToTitle();

        }

        public static TaxonRankType GetRank(this ITaxon taxon) {

            return taxon?.Rank?.Type ?? TaxonRankType.None;

        }
        public static TaxonRankType GetChildRank(this ITaxon taxon) {

            return TaxonUtilities.GetChildRank(taxon.GetRank());

        }
        public static TaxonRankType GetParentRank(this ITaxon taxon) {

            return TaxonUtilities.GetParentRank(taxon.GetRank());

        }

        public static string GetPictureUrl(this ITaxon taxon) {

            return taxon?.Pictures?.FirstOrDefault()?.Url;

        }

        public static bool IsValid(this ITaxon taxon) {

            return taxon != null && taxon.Id.HasValue && taxon.Id >= 0;

        }

        public static string GetName(this TaxonRankType rankType, bool plural = false) {

            return TaxonUtilities.GetNameForRank(rankType, plural);

        }
        public static TaxonRankType GetChildRank(this TaxonRankType rankType) {

            return TaxonUtilities.GetChildRank(rankType);

        }
        public static TaxonRankType GetParentRank(this TaxonRankType rankType) {

            return TaxonUtilities.GetParentRank(rankType);

        }

    }

}