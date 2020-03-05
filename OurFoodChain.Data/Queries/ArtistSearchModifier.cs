using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    class ArtistSearchModifier :
        SearchModifierBase {

        public async override Task ApplyAsync(ISearchContext context, ISearchResult result) {

            await result.FilterByAsync(async (species) => {

                return !(await context.Database.GetPicturesAsync(species)).Any(picture => picture.Artist.ToString().Equals(Value, StringComparison.OrdinalIgnoreCase));

            }, Invert);

        }

    }

}