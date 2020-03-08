using OurFoodChain.Common;
using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Data.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("groupby", "group")]
    public class GroupBySearchModifier :
        SearchModifierBase {

        // Public members

        public override async Task ApplyAsync(ISearchContext context, ISearchResult result) {

            await result.GroupByAsync(async (species) => {

                switch (ParseGroupBy(Value)) {

                    case GroupBy.Zone:
                        return (await context.Database.GetZonesAsync(species.Id)).Select(zoneInfo => zoneInfo.Zone.GetFullName());

                    case GroupBy.Genus:
                        return new string[] { species.Genus?.Name };

                    case GroupBy.Family:
                        return new string[] { (await context.Database.GetTaxaAsync(species)).GetOrDefault(TaxonRankType.Family)?.Name ?? "N/A" };

                    case GroupBy.Order:
                        return new string[] { (await context.Database.GetTaxaAsync(species)).GetOrDefault(TaxonRankType.Order)?.Name ?? "N/A" };

                    case GroupBy.Class:
                        return new string[] { (await context.Database.GetTaxaAsync(species)).GetOrDefault(TaxonRankType.Class)?.Name ?? "N/A" };

                    case GroupBy.Phylum:
                        return new string[] { (await context.Database.GetTaxaAsync(species)).GetOrDefault(TaxonRankType.Phylum)?.Name ?? "N/A" };

                    case GroupBy.Kingdom:
                        return new string[] { (await context.Database.GetTaxaAsync(species)).GetOrDefault(TaxonRankType.Kingdom)?.Name ?? "N/A" };

                    case GroupBy.Domain:
                        return new string[] { (await context.Database.GetTaxaAsync(species)).GetOrDefault(TaxonRankType.Domain)?.Name ?? "N/A" };

                    case GroupBy.Creator:
                        return new string[] { (await context.GetCreatorAsync(species.Creator))?.Name ?? new Creator("?").Name };

                    case GroupBy.Status:
                        return new string[] { await Task.FromResult(species.Status.IsExinct ? "extinct" : "extant") };

                    case GroupBy.Role:
                        return (await context.Database.GetRolesAsync(species.Id)).Select(role => role.Name);

                    case GroupBy.Generation:
                        return new string[] { (await context.Database.GetGenerationByDateAsync(species.CreationDate))?.Name ?? "Gen ?" };

                    default:
                        return new string[] { SearchResult.DefaultGroupName };

                }

            });

        }

        // Private members

        private enum GroupBy {

            Unknown = 0,

            Zone,
            Genus,
            Family,
            Order,
            Class,
            Phylum,
            Kingdom,
            Domain,
            Creator,
            Status,
            Role,
            Generation

        }

        private GroupBy ParseGroupBy(string value) {

            switch (value.ToLowerInvariant()) {

                case "z":
                case "zones":
                case "zone":
                    return GroupBy.Zone;

                case "g":
                case "genus":
                    return GroupBy.Genus;

                case "f":
                case "family":
                    return GroupBy.Family;

                case "o":
                case "order":
                    return GroupBy.Order;

                case "c":
                case "class":
                    return GroupBy.Class;

                case "p":
                case "phylum":
                    return GroupBy.Phylum;

                case "k":
                case "kingdom":
                    return GroupBy.Kingdom;

                case "d":
                case "domain":
                    return GroupBy.Kingdom;

                case "creator":
                case "owner":
                    return GroupBy.Creator;

                case "status":
                case "extant":
                case "extinct":
                    return GroupBy.Status;

                case "role":
                    return GroupBy.Role;

                case "gen":
                case "generation":
                    return GroupBy.Generation;

                default:
                    return GroupBy.Unknown;

            }

        }

    }

}