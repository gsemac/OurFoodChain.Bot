using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("random")]
    public class RandomSearchModifier :
        SearchModifierBase {

        public async override Task ApplyAsync(ISearchContext context, ISearchResult result) {

            if (int.TryParse(Value, out int count) && count > 0) {

                IEnumerable<ISpecies> results = await result.GetResultsAsync();

                // Take N random IDs from the results.

                IEnumerable<long> randomIds = results
                    .Where(species => species.Id.HasValue)
                    .OrderBy(species => NumberUtilities.GetRandomInteger(int.MaxValue))
                    .Take(count)
                    .Select(species => (long)species.Id);

                // Filter all but those results.

                await result.FilterByAsync(async (species) => await Task.FromResult(!randomIds.Any(id => id == species.Id)),
                    Invert);

            }

        }

    }

}