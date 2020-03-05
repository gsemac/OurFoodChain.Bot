using OurFoodChain.Common.Taxa;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class ScrapThatTrophy :
        TrophyBase {

        public ScrapThatTrophy() :
            base("Scrap That", "Create an evolution to your own species.") {
        }

        public async override Task<bool> CheckTrophyAsync(ICheckTrophyContext context) {

            if (context.Creator.UserId.HasValue) {

                // Check if the user has any species with a direct descendant created by that user.

                IEnumerable<ISpecies> ownedSpecies = await context.Database.GetSpeciesAsync(context.Creator);

                foreach (ISpecies species in ownedSpecies) {

                    if ((await context.Database.GetDirectDescendantsAsync(species)).Any(evo => evo.Creator?.UserId == context.Creator.UserId))
                        return true;

                }

            }

            return false;

        }

    }

}