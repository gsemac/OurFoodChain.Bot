using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("limit")]
    public class LimitSearchModifier :
        SearchModifierBase {

        public async override Task ApplyAsync(ISearchContext context, ISearchResult result) {

            // Filter all species that aren't in the first n results.

            if (int.TryParse(Value, out int limit)) {

                IEnumerable<ISpecies> searchResults = (await result.GetResultsAsync())
                    .Take(limit);

                await result.FilterByAsync(async (species) => await Task.FromResult(!searchResults.Any(sp => sp.Id == species.Id)), Invert);

            }

        }

    }

}