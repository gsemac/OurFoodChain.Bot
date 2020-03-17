using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data {

    public enum AmbiguityResolverOptions {
        None = 0,
        AllowExtra = 1
    }

    public interface ISpeciesAmbiguityResolver {

        Task<ISpeciesAmbiguityResolverResult> ResolveAsync(string arg0, string arg1, string arg2, AmbiguityResolverOptions options = AmbiguityResolverOptions.None);
        Task<ISpeciesAmbiguityResolverResult> ResolveAsync(string arg0, string arg1, string arg2, string arg3, AmbiguityResolverOptions options = AmbiguityResolverOptions.None);

    }

}