using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;

namespace OurFoodChain.Data {

    public class SpeciesAmbiguityResolverResult :
        ISpeciesAmbiguityResolverResult {

        public bool Success => First != null && Second != null;
        public ISpecies First { get; }
        public ISpecies Second { get; }

        public SpeciesAmbiguityResolverResult(ISpecies first, ISpecies second) {

            this.First = first;
            this.Second = second;

        }

    }

}