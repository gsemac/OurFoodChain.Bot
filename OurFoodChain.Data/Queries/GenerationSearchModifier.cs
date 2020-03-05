using OurFoodChain.Common.Generations;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("gen", "generation")]
    public class GenerationSearchModifier :
        SearchModifierBase {

        public async override Task ApplyAsync(ISearchContext context, ISearchResult result) {

            await result.FilterByAsync(async (species) => {

                IGeneration gen = await context.Database.GetGenerationByDateAsync(species.CreationDate);

                return gen == null || gen.Number.ToString() != Value;

            }, Invert);

        }

    }

}