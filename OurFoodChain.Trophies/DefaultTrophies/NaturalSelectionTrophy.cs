using OurFoodChain.Common.Taxa;
using OurFoodChain.Data.Extensions;
using OurFoodChain.Trophies.DefaultTrophies.BaseTrophies;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies {

    public class NaturalSelectionTrophy :
        TrophyBase {

        public NaturalSelectionTrophy() :
            base("Natural Selection", "Have a species you own go extinct.") {
        }

        public async override Task<bool> CheckTrophyAsync(ITrophyScannerContext context) {

            IEnumerable<ISpecies> ownedSpecies = await context.Database.GetSpeciesAsync(context.Creator);

            foreach (ISpecies species in ownedSpecies)
                if (species.Status.IsExinct)
                    return true;

            return false;

        }

    }

}