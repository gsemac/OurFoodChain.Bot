using OurFoodChain.Common.Extensions;
using OurFoodChain.Common.Taxa;
using OurFoodChain.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Data.Queries {

    [SearchModifier("orderby", "sortby", "sort", "ordering")]
    public class OrderBySearchModifier :
        SearchModifierBase {

        // Public members

        public override async Task ApplyAsync(ISearchContext context, ISearchResult result) {

            switch (ParseOrderBy(Value)) {

                case OrderBy.Newest:
                    await result.OrderByAsync(Comparer<ISpecies>.Create((lhs, rhs) => rhs.CreationDate.CompareTo(lhs.CreationDate)));
                    break;

                case OrderBy.Oldest:
                    await result.OrderByAsync(Comparer<ISpecies>.Create((lhs, rhs) => lhs.CreationDate.CompareTo(rhs.CreationDate)));
                    break;

                case OrderBy.Smallest:
                    await result.OrderByAsync(Comparer<ISpecies>.Create((lhs, rhs) => SpeciesSizeMatch.Match(lhs.Description).MaxSize.ToMeters().CompareTo(SpeciesSizeMatch.Match(rhs.Description).MaxSize.ToMeters())));
                    break;

                case OrderBy.Largest:
                    await result.OrderByAsync(Comparer<ISpecies>.Create((lhs, rhs) => SpeciesSizeMatch.Match(rhs.Description).MaxSize.ToMeters().CompareTo(SpeciesSizeMatch.Match(lhs.Description).MaxSize.ToMeters())));
                    break;

                case OrderBy.Count:
                    await result.OrderByAsync(Comparer<ISearchResultGroup>.Create((lhs, rhs) => rhs.Count().CompareTo(lhs.Count())));
                    break;

                case OrderBy.Suffix:
                    await result.OrderByAsync(Comparer<ISpecies>.Create((lhs, rhs) => {

                        string lhsStr = new string(result.TaxonFormatter.GetString(lhs, false).Reverse().ToArray());
                        string rhsStr = new string(result.TaxonFormatter.GetString(rhs, false).Reverse().ToArray());

                        return lhsStr.CompareTo(rhsStr);

                    }));
                    break;

                default:
                case OrderBy.Default:
                    await result.OrderByAsync(Comparer<ISpecies>.Create((lhs, rhs) => result.TaxonFormatter.GetString(lhs, false).CompareTo(result.TaxonFormatter.GetString(rhs, false))));
                    break;

            }

        }

        // Private members

        private enum OrderBy {

            Unknown = 0,

            Default,

            Newest,
            Oldest,
            Smallest,
            Largest,
            Count,
            Suffix

        }

        private OrderBy ParseOrderBy(string value) {

            switch (value.ToLowerInvariant()) {

                case "smallest":
                    return OrderBy.Smallest;

                case "largest":
                case "biggest":
                case "size":
                    return OrderBy.Largest;

                case "newest":
                case "recent":
                    return OrderBy.Newest;

                case "age":
                case "date":
                case "oldest":
                    return OrderBy.Oldest;

                case "number":
                case "total":
                case "count":
                    return OrderBy.Count;

                case "suffix":
                    return OrderBy.Suffix;

                default:
                    return OrderBy.Unknown;

            }

        }

    }

}