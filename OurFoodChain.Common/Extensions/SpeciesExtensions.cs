using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Common.Extensions {

    public static class SpeciesExtensions {

        public static string GetShortName(this ISpecies species) {

            return species.BinomialName.ToString(BinomialNameFormat.Abbreviated);

        }
        public static string GetFullName(this ISpecies species) {

            return species.BinomialName.ToString(BinomialNameFormat.Full);

        }

        public static bool IsExtinct(this ISpecies species) {

            return species?.Status?.IsExinct ?? false;

        }

    }

}