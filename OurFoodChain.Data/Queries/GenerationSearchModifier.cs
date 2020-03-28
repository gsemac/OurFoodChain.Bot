using OurFoodChain.Common.Generations;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("gen", "generation")]
    public class GenerationSearchModifier :
        FilterSearchModifierBase {

        protected override async Task<bool> IsFilteredAsync(ISearchContext context, ISpecies species, string value) {

            IGeneration gen = await context.Database.GetGenerationByDateAsync(species.CreationDate);

            return gen == null || gen.Number.ToString() != value;

        }

    }

}