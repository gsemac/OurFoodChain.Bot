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
            Dictionary<long, bool> resultCache = new Dictionary<long, bool>();

            await result.FilterByAsync(async (species) => {

                bool isFiltered = true;

                if (ancestorSpecies != null && species.Id != ancestorSpecies.Id && !resultCache.TryGetValue(species.Id.Value, out isFiltered)) {

                    // Get all of the ancestor IDs for this species, ordered from the oldest to the latest.
                    // Skip until we find the ancestor ID we're looking for.

                    IEnumerable<long> ancestorIds = (await context.Database.GetAncestorIdsAsync(species.Id)).OrderBy(id => id);

                    // Skip until we find the ID of the ancestor we're looking for.
                    // If we find it, all species beyond this point also have the ancestor.
                    // If we don't find it, none of these species has the ancestor.

                    IEnumerable<long> idsWithAncestor = ancestorIds.SkipWhile(id => id != ancestorSpecies.Id).Skip(1);

                    if (idsWithAncestor.Any()) {

                        // If the species does have the ancestor, do not filter it.

                        isFiltered = false;

                        foreach (long id in idsWithAncestor)
                            resultCache[id] = isFiltered;

                    }
                    else {

                        // If the species does not have the ancestor, filter it.

                        isFiltered = true;

                        foreach (long id in ancestorIds)
                            resultCache[id] = isFiltered;

                    }

                }

                return isFiltered;

            }, Invert);

        }

    }

}