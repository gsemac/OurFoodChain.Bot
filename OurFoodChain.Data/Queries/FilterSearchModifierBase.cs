using OurFoodChain.Common.Taxa;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public abstract class FilterSearchModifierBase :
        SearchModifierBase {

        public abstract Task<bool> IsFilteredAsync(ISearchContext context, ISpecies species, string value);

        public async override Task ApplyAsync(ISearchContext context, ISearchResult result) {

            await result.FilterByAsync(async (species) => {

                // If any of the comma-separated values accept this species, include it in the results.

                foreach (string value in Values)
                    if (!await IsFilteredAsync(context, species, value))
                        return false;

                return true;

            }, Invert);

        }

    }

}