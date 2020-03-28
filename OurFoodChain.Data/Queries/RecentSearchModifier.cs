using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("recent")]
    public class RecentSearchModifier :
         FilterSearchModifierBase {

        protected override async Task<bool> IsFilteredAsync(ISearchContext context, ISpecies species, string value) {

            if (DateUtilities.TryParseTimeSpan(value, out TimeSpan timeSpan)) {

                TimeSpan timeSpanSinceCreation = DateTimeOffset.UtcNow - species.CreationDate;

                return await Task.FromResult(timeSpanSinceCreation > timeSpan);

            }
            else
                return await Task.FromResult(true);

        }

    }

}