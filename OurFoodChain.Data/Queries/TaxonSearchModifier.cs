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
        SearchModifierBase {

        // Public members

        public async override Task ApplyAsync(ISearchContext context, ISearchResult result) {

            TaxonRankType rank = ParseRankType(Name);

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