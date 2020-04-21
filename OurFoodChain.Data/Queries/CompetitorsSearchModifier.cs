using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Roles;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Zones;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("competitors")]
    public class CompetitorsSearchModifier :
        SearchModifierBase {

        // Public members

        public async override Task ApplyAsync(ISearchContext context, ISearchResult result) {

            /* Filter all species that don't compete with the given species.
             * 
             * A species is considered a competitor if:
             * 
             * (1) It shares a zone with the given species, and
             * (2) It eats the same prey items, and
             * (3) It is currently extant
             * 
             * This is very similar to the "status:endangered" filter, without the requirement that one species derive from the other.
             */

            ISpecies species = (await context.Database.GetSpeciesAsync(Value)).FirstOrDefault();
            List<ISpecies> competitorSpeciesList = new List<ISpecies>();

            if (species.IsValid()) {

                IEnumerable<ISpecies> preySpecies = (await context.Database.GetPreyAsync(species)).Select(info => info.Species).Where(sp => !sp.IsExtinct());

                // Create a list of all species that exist in the same zone as the given species.

                List<ISpecies> sharedZoneSpeciesList = new List<ISpecies>();

                foreach (IZone zone in (await context.Database.GetZonesAsync(species, GetZoneOptions.IdsOnly)).Select(info => info.Zone))
                    sharedZoneSpeciesList.AddRange((await context.Database.GetSpeciesAsync(zone)).Where(sp => !sp.IsExtinct()));

                if (preySpecies.Any()) {

                    // If the species has prey, find all species that have the same prey.

                    foreach (ISpecies candidateSpecies in sharedZoneSpeciesList) {

                        IEnumerable<ISpecies> candidateSpeciesPreySpecies =
                            (await context.Database.GetPreyAsync(candidateSpecies)).Select(info => info.Species).Where(sp => !sp.IsExtinct());

                        if (candidateSpeciesPreySpecies.Any() && candidateSpeciesPreySpecies.All(sp1 => preySpecies.Any(sp2 => sp1.Id.Equals(sp2.Id))))
                            competitorSpeciesList.Add(candidateSpecies);

                    }

                }
                else {

                    // If the species does not have prey, find all species with the same roles.

                    IEnumerable<IRole> roles = await context.Database.GetRolesAsync(species);

                    if (roles.Any()) {

                        foreach (ISpecies candidateSpecies in sharedZoneSpeciesList)
                            if ((await context.Database.GetRolesAsync(candidateSpecies)).All(role1 => roles.Any(role2 => role1.Id.Equals(role2.Id))))
                                competitorSpeciesList.Add(candidateSpecies);

                    }

                }

            }

            // Filter all species that aren't in the competitor species list.

            await result.FilterByAsync(async (s) => {

                return await Task.FromResult(!species.IsValid() || species.IsExtinct() || s.Id.Equals(species.Id) || !competitorSpeciesList.Any(sp => sp.Id.Equals(s.Id)));

            }, Invert);

        }

    }

}
