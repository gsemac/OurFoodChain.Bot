using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OurFoodChain.Data {

    public class SpeciesAmbiguityResolverResult :
        ISpeciesAmbiguityResolverResult {

        public bool Success => First != null && First.Count() == 1 && Second != null && Second.Count() == 1;
        public IEnumerable<ISpecies> First { get; }
        public IEnumerable<ISpecies> Second { get; }
        public string SuggestionHint { get; }
        public string Extra { get; }

        public SpeciesAmbiguityResolverResult(IEnumerable<ISpecies> first, IEnumerable<ISpecies> second, string suggestionHint) {

            this.First = first;
            this.Second = second;
            this.SuggestionHint = suggestionHint;

        }
        public SpeciesAmbiguityResolverResult(IEnumerable<ISpecies> first, IEnumerable<ISpecies> second, string suggestionHint, string extra) :
            this(first, second, suggestionHint) {

            this.Extra = extra;

        }

    }

}