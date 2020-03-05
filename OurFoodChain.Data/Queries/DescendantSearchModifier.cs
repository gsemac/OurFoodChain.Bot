using OurFoodChain.Common.Taxa;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("evo", "descendant")]
    public class DescendantSearchModifier :
        SearchModifierBase {

        public async override Task ApplyAsync(ISearchContext context, ISearchResult result) {

            // Filter all species that don't have the given species as a descendant.

            ISpecies descendantSpecies = (await context.Database.GetSpeciesAsync(Value)).FirstOrDefault();
            IEnumerable<long> ancestorIds = await context.Database.GetAncestorIdsAsync(descendantSpecies.Id);

            await result.FilterByAsync(async (species) => {

                return await Task.FromResult(ancestorIds.Count() <= 0 || !ancestorIds.Any(id => id == species.Id));

            }, Invert);

        }

    }

}