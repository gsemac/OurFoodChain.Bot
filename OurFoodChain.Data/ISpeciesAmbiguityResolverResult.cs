using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Data {

    public interface ISpeciesAmbiguityResolverResult {

        bool Success { get; }

        IEnumerable<ISpecies> First { get; }
        IEnumerable<ISpecies> Second { get; }
        string SuggestionHint { get; }
        string Extra { get; }

    }

}