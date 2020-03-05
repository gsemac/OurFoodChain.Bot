using OurFoodChain.Common.Roles;
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

    public abstract class RoleMatchTrophyBase :
         TrophyBase {

        // Public members

        protected RoleMatchTrophyBase(string name, string description, TrophyFlags flags, string pattern) :
            this(name, description, flags, new string[] { pattern }) {
        }
        protected RoleMatchTrophyBase(string name, string description, TrophyFlags flags, IEnumerable<string> patterns) :
           base(name, description, flags) {

            this.patterns = patterns;

        }

        public async override Task<bool> CheckTrophyAsync(ICheckTrophyContext context) {

            // Check if the user has any species with these roles.

            IEnumerable<ISpecies> ownedSpecies = await context.Database.GetSpeciesAsync(context.Creator);

            foreach (ISpecies species in ownedSpecies) {

                IEnumerable<IRole> roles = await context.Database.GetRolesAsync(species);
                bool allPatternsMatched = true;

                foreach (string pattern in patterns) {

                    if (!roles.Any(role => Regex.IsMatch(role.Name, pattern, RegexOptions.IgnoreCase))) {

                        allPatternsMatched = false;

                        break;

                    }

                }

                if (allPatternsMatched)
                    return true;

            }

            return false;

        }

        // Private members

        private readonly IEnumerable<string> patterns;

    }

}