using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data {

    public interface ISpeciesAmbiguityResolver {

        Task<ISpeciesAmbiguityResolverResult> ResolveAsync(string arg0, string arg1, string arg2);

    }

}