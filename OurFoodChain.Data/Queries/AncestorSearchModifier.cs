using OurFoodChain.Common.Taxa;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("anc", "ancestor")]
    public class AncestorSearchModifier :
        SearchModifierBase {

        public async override Task ApplyAsync(ISearchContext context, ISearchResult result) {

            // Filter all species that don't have the given species as an ancestor.

            ISpecies ancestorSpecies = (await context.Database.GetSpeciesAsync(Value)).FirstOrDefault();

            await result.FilterByAsync(async (species) => {

                return ancestorSpecies is null || !(await context.Database.GetAncestorIdsAsync(species.Id)).Any(id => id == ancestorSpecies.Id);

            }, Invert);

        }

    }

}