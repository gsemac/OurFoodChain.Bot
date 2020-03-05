using OurFoodChain.Common;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public class PredatorSearchModifier :
         SearchModifierBase {

        public async override Task ApplyAsync(ISearchContext context, ISearchResult result) {

            // Filters out all species that are not in the prey list of the given species.

            ISpecies predatorSpecies = (await context.Database.GetSpeciesAsync(Value)).FirstOrDefault();
            IEnumerable<IPreyInfo> preySpecies = predatorSpecies != null ? await context.Database.GetPreyAsync(predatorSpecies) : Enumerable.Empty<IPreyInfo>();

            await result.FilterByAsync(async (species) => await Task.FromResult(!preySpecies.Any(info => info.Prey.Id == species.Id)), Invert);

        }

    }

}