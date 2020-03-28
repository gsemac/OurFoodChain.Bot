using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("ztype", "zonetype")]
    public class ZoneTypeSearchModifier :
     FilterSearchModifierBase {

        // Public members

        public override async Task ApplyAsync(ISearchContext context, ISearchResult result) {

            foreach (string value in Values)
                zoneTypes.Add(await context.Database.GetZoneTypeAsync(value));

            await base.ApplyAsync(context, result);

        }

        // Protected members

        protected override async Task<bool> IsFilteredAsync(ISearchContext context, ISpecies species, string value) {

            if (zoneTypes.Count() <= 0)
                return true;

            IEnumerable<ISpeciesZoneInfo> speciesZoneInfos = await context.Database.GetZonesAsync(species, GetZoneOptions.Fast);

            return !speciesZoneInfos.Any(zoneInfo => zoneTypes.Any(zoneType => zoneInfo.Zone.TypeId == zoneType.Id));

        }

        // Private members

        private readonly List<IZoneType> zoneTypes = new List<IZoneType>();

    }

}