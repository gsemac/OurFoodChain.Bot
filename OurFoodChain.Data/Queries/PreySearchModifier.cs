using OurFoodChain.Common.Taxa;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public class PreySearchModifier :
         SearchModifierBase {

        public async override Task ApplyAsync(ISearchContext context, ISearchResult result) {

            // Filters out all species that do not prey upon the given species.

            ISpecies preySpecies = (await context.Database.GetSpeciesAsync(Value)).FirstOrDefault();
            IEnumerable<ISpecies> predatorSpecies = preySpecies != null ? await context.Database.GetPredatorsAsync(preySpecies) : Enumerable.Empty<ISpecies>();

            await result.FilterByAsync(async (species) => await Task.FromResult(!predatorSpecies.Any(predator => predator.Id == species.Id)), Invert);

        }

    }

}