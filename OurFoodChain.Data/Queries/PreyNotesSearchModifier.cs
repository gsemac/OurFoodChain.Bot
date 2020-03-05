using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("preynote", "preynotes")]
    public class PreyNotesSearchModifier :
          SearchModifierBase {

        public async override Task ApplyAsync(ISearchContext context, ISearchResult result) {

            // Filters out species that don't have the given keyword in the prey notes.

            await result.FilterByAsync(async (species) => {

                return !(await context.Database.GetPreyAsync(species))
                   .Where(info => info.Notes.ToLowerInvariant().Contains(Value.ToLowerInvariant()))
                   .Any();

            }, Invert);

        }

    }

}