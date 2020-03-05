using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    public class TaxonSearchModifier :
        SearchModifierBase {

        // Public members

        public TaxonSearchModifier(TaxonRankType rank) {

            this.rank = rank;

        }

        public async override Task ApplyAsync(ISearchContext context, ISearchResult result) {

            switch (rank) {

                case TaxonRankType.Species:

                    await result.FilterByAsync(async (species) => await Task.FromResult(!species.Name.Equals(Value, StringComparison.OrdinalIgnoreCase)),
                        Invert);

                    break;

                case TaxonRankType.Genus:

                    await result.FilterByAsync(async (species) => {

                        if (species.Genus != null)
                            return await Task.FromResult(!species.Genus.Name.Equals(Value, StringComparison.OrdinalIgnoreCase));
                        else
                            return true;

                    }, Invert);

                    break;

                case TaxonRankType.Any:

                    await result.FilterByAsync(async (species) => !(await context.Database.GetTaxaAsync(species)).Values.Any(taxon => taxon.Name.Equals(Value, StringComparison.OrdinalIgnoreCase)),
                        Invert);

                    break;

                default:

                    await result.FilterByAsync(async (species) => {

                        ITaxon taxon = (await context.Database.GetTaxaAsync(species)).GetOrDefault(rank);

                        return taxon is null || !taxon.Name.Equals(Value, StringComparison.OrdinalIgnoreCase);

                    }, Invert);

                    break;

            }

        }

        // Private members

        private readonly TaxonRankType rank;

    }

}