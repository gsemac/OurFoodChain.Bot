using OurFoodChain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("owner", "creator")]
    public class CreatorSearchModifier :
        SearchModifierBase {

        // Public members

        public async override Task ApplyAsync(ISearchContext context, ISearchResult result) {

            ICreator creator = await context.GetCreatorAsync(new Creator(Value));

            await result.FilterByAsync(async (species) => {

                ICreator speciesCreator = await context.GetCreatorAsync(species.Creator);

                if (creator is null || !creator.UserId.HasValue || !speciesCreator.UserId.HasValue) {

                    // If we don't have an ID to work with, compare by username only.

                    return !creator.Name.Equals(speciesCreator.Name, StringComparison.OrdinalIgnoreCase);

                }
                else {

                    // If we have an ID to work with, compare by ID.

                    return creator.UserId != speciesCreator.UserId;

                }

            }, Invert);

        }

    }

}