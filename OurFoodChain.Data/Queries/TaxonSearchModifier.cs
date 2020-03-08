using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("s", "species", "g", "genus", "f", "family", "o", "order", "c", "class", "p", "phylum", "k", "kingdom", "d", "domain", "t", "taxon")]
    public class TaxonSearchModifier :
        FilterSearchModifierBase {

        // Public members

        public override async Task<bool> IsFilteredAsync(ISearchContext context, ISpecies species, string value) {

            TaxonRankType rank = ParseRankType(Name);

            switch (rank) {

                case TaxonRankType.Species:

                    return await Task.FromResult(!species.Name.Equals(value, StringComparison.OrdinalIgnoreCase));

                case TaxonRankType.Genus:

                    if (species.Genus != null)
                        return await Task.FromResult(!species.Genus.Name.Equals(value, StringComparison.OrdinalIgnoreCase));
                    else
                        return true;

                case TaxonRankType.Any:

                    return !(await context.Database.GetTaxaAsync(species)).Values.Any(taxon => taxon.Name.Equals(value, StringComparison.OrdinalIgnoreCase));

                default: {

                        ITaxon taxon = (await context.Database.GetTaxaAsync(species)).GetOrDefault(rank);

                        return taxon is null || !taxon.Name.Equals(value, StringComparison.OrdinalIgnoreCase);

                    }

            }

        }

        // Private members

        private TaxonRankType ParseRankType(string input) {

            switch (input.ToLowerInvariant()) {

                case "s":
                case "species":
                    return TaxonRankType.Species;

                case "g":
                case "genus":
                    return TaxonRankType.Genus;

                case "f":
                case "family":
                    return TaxonRankType.Family;

                case "o":
                case "order":
                    return TaxonRankType.Order;

                case "c":
                case "class":
                    return TaxonRankType.Class;

                case "p":
                case "phylum":
                    return TaxonRankType.Phylum;

                case "k":
                case "kingdom":
                    return TaxonRankType.Kingdom;

                case "d":
                case "domain":
                    return TaxonRankType.Domain;

                case "t":
                case "taxon":
                    return TaxonRankType.Any;

                default:
                    return TaxonRankType.None;

            }

        }

    }

}