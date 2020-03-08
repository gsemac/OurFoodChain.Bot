using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Data {

    public interface ISpeciesAmbiguityResolverResult {

        bool Success { get; }

        ISpecies First { get; }
        ISpecies Second { get; }

    }

}