using OurFoodChain.Common.Utilities;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public class ZoneSearchModifier :
         ISearchModifier {

        // Public members

        public string Name { get; set; }
        public string Value { get; set; }
        public bool Invert { get; set; } = false;

        public async Task ApplyAsync(ISearchContext context, ISearchResult result) {

            IEnumerable<string> zoneNames = ZoneUtilities.ParseZoneNameList(Value);
            IEnumerable<long> ZoneIds = (await context.Database.GetZonesAsync(zoneNames))
                .Where(zone => zone.Id.HasValue)
                .Select(zone => (long)zone.Id);

            await result.FilterByAsync(async (species) => {

                return !(await context.Database.GetZonesAsync(species)).Any(zone => ZoneIds.Any(id => id == zone.Zone.Id));

            }, Invert);

        }

    }

}