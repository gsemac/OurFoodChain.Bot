using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OurFoodChain.Trophies.DefaultTrophies.BaseTrophies {

    public abstract class DescriptionMatchTrophyBase :
         TrophyBase {

        // Public members

        protected DescriptionMatchTrophyBase(string name, string description, TrophyFlags flags, string pattern) :
            base(name, description, flags) {

            this.pattern = pattern;

        }

        public async override Task<bool> CheckTrophyAsync(ICheckTrophyContext context) {

            // Find a species with a matching description.

            IEnumerable<ISpecies> ownedSpecies = await context.Database.GetSpeciesAsync(context.Creator);

            foreach (ISpecies species in ownedSpecies)
                if (Regex.IsMatch(species.Description, pattern, RegexOptions.IgnoreCase))
                    return true;

            return false;

        }

        // Private members

        private readonly string pattern;

    }

}