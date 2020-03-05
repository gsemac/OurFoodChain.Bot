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

    public abstract class ZoneDescriptionMatchTrophyBase :
         TrophyBase {

        // Public members

        protected ZoneDescriptionMatchTrophyBase(string name, string description, TrophyFlags flags, string pattern) :
            base(name, description, flags) {

            this.pattern = pattern;

        }

        public async override Task<bool> CheckTrophyAsync(ICheckTrophyContext context) {

            // Get all zones.

            List<IZone> zones = new List<IZone>(await context.Database.GetZonesAsync());

            // Filter list so we only have zones with matching descriptions.

            zones.RemoveAll(zone => !Regex.IsMatch(zone.Description, pattern, RegexOptions.IgnoreCase));

            // Check if the user has any species in these zones.

            IEnumerable<ISpecies> ownedSpecies = await context.Database.GetSpeciesAsync(context.Creator);

            foreach (ISpecies species in ownedSpecies)
                if ((await context.Database.GetZonesAsync(species)).Any(zone => zones.Any(z => z.Id == zone.Zone.Id)))
                    return true;

            return false;

        }

        // Private members

        private readonly string pattern;

    }

}