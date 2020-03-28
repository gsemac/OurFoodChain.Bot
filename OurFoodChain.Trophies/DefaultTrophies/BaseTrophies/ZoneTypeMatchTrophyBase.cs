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

    public abstract class ZoneTypeMatchTrophyBase :
         TrophyBase {

        // Public members

        protected ZoneTypeMatchTrophyBase(string name, string description, TrophyFlags flags, string pattern) :
            this(name, description, flags, new[] { pattern }) {
        }
        protected ZoneTypeMatchTrophyBase(string name, string description, TrophyFlags flags, IEnumerable<string> patterns) :
             base(name, description, flags) {

            this.patterns = patterns;

        }
        public async override Task<bool> CheckTrophyAsync(ITrophyScannerContext context) {

            // Get zone types.

            IEnumerable<IZoneType> zoneTypes = (await context.Database.GetZoneTypesAsync())
                .Where(type => patterns.Any(pattern => Regex.IsMatch(type.Name, pattern, RegexOptions.IgnoreCase)));

            // Get zones.

            IEnumerable<IZone> zones = (await context.Database.GetZonesAsync())
                .Where(zone => zoneTypes.Any(type => type.Id == zone.TypeId));

            // Check if the user has any species in these zones.

            IEnumerable<ISpecies> ownedSpecies = await context.Database.GetSpeciesAsync(context.Creator);

            foreach (ISpecies species in ownedSpecies) {

                IEnumerable<ISpeciesZoneInfo> speciesZones = await context.Database.GetZonesAsync(species, GetZoneOptions.Fast);

                if (zoneTypes.All(type => speciesZones.Any(zoneInfo => zoneInfo.Zone.TypeId == type.Id)))
                    return true;

            }

            return false;

        }

        // Private members

        private readonly IEnumerable<string> patterns;

    }

}