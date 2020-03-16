using OurFoodChain.Common.Taxa;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class TrademarkedTrophy :
        TrophyBase {

        public TrademarkedTrophy() :
            base("Trademarked", "Create a new genus.") {
        }

        public async override Task<bool> CheckTrophyAsync(ITrophyScannerContext context) {

            if (context.Creator.UserId.HasValue) {

                // Check if the user has any species that were the first in their genus.
                // If the ancestor species has a different genus (or doesn't exist), the trophy will be awarded.

                IEnumerable<ISpecies> ownedSpecies = await context.Database.GetSpeciesAsync(context.Creator);

                foreach (ISpecies species in ownedSpecies) {

                    if (species.Genus != null) {

                        ISpecies ancestor = await context.Database.GetAncestorAsync(species);

                        if (ancestor is null || (ancestor.Genus != null && !ancestor.Genus.Name.Equals(species.Genus.Name, StringComparison.OrdinalIgnoreCase)))
                            return true;

                    }

                }

            }

            return false;

        }

    }

}